using System.Collections.Generic;
using System.IO;
using BudeStudio.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// STUDIO LINK - Bigfoot builds inside your Unity without any relay, tunnel,
    /// or extra windows. The editor polls the Bigfoot agent conversation over
    /// normal HTTPS (outbound only - no firewall, no ports, no setup).
    ///
    /// When Bigfoot replies with a command block like:
    ///     ```bude-exec
    ///     {"ops":[{"op":"primitive","type":"Cube","name":"Block","pos":[0,1,0]}]}
    ///     ```
    /// this service picks it up, shows it for approval (or runs it automatically
    /// if you ticked that in SETTINGS), executes the ops with full Undo support,
    /// and posts the result back into the conversation so Bigfoot can verify and
    /// iterate. You watch it happen, then edit freely.
    /// </summary>
    public static class StudioLinkService
    {
        private const string FenceStart = "```bude-exec";
        private const string FenceEnd = "```";
        private const string LastHashPref = "BudeStudio.StudioLink.LastExecutedHash";
        private const float PollIntervalSeconds = 5f;

        // ---- state ----
        private static bool _polling;
        private static float _nextPoll;
        private static UnityWebRequest _pollRequest;
        private static string _pendingPayload = "";
        private static string _pendingPreview = "";
        private static string _lastResult = "";
        private static string _status = "";
        private static readonly List<UnityWebRequest> _inflight = new List<UnityWebRequest>();
        private static List<string> _consoleErrors = new List<string>();
        private static bool _capturingErrors;

        public static bool HasPending { get { return !string.IsNullOrEmpty(_pendingPayload); } }
        public static string LastResult { get { return _lastResult; } }
        public static string Status { get { return _status; } }

        // ===================== lifecycle =====================

        /// <summary>Call from the window's OnGUI/OnEnable. Starts or stops polling to match settings.</summary>
        public static void EnsurePolling()
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            bool want = settings.studioLinkEnabled && settings.HasApiKey &&
                        !string.IsNullOrEmpty(BigfootChatTransport.ConversationId);

            if (want && !_polling)
            {
                _polling = true;
                _nextPoll = 0f; // poll immediately
                EditorApplication.update += Tick;
                _status = "Studio Link: watching for Bigfoot build commands.";
            }
            else if (!want && _polling)
            {
                Stop();
            }
        }

        public static void Stop()
        {
            _polling = false;
            if (_pollRequest != null) { _pollRequest.Abort(); _pollRequest = null; }
            EditorApplication.update -= Tick;
            _status = "Studio Link: off.";
        }

        private static void Tick()
        {
            // pump result-posting requests
            for (int i = _inflight.Count - 1; i >= 0; i--)
            {
                var req = _inflight[i];
                if (!req.isDone) continue;
                if (!string.IsNullOrEmpty(req.error))
                    Debug.LogWarning("[Studio Link] Could not post result back: " + req.error);
                _inflight[i].Dispose();
                _inflight.RemoveAt(i);
            }

            PumpDownloads();

            if (_pollRequest == null)
            {
                if (EditorApplication.timeSinceStartup < _nextPoll) return;
                _nextPoll = (float)EditorApplication.timeSinceStartup + PollIntervalSeconds;
                _pollRequest = MakeConversationGet();
                if (_pollRequest != null) _pollRequest.SendWebRequest();
                return;
            }

            if (!_pollRequest.isDone) return;

            var poll = _pollRequest;
            _pollRequest = null;
            if (!string.IsNullOrEmpty(poll.error)) { poll.Dispose(); return; } // transient, retry next tick

            string body = poll.downloadHandler != null ? poll.downloadHandler.text : "";
            poll.Dispose();

            string payload = FindNewestExecPayload(body);
            if (payload == null) return;

            int hash = payload.GetHashCode();
            if (hash == EditorPrefs.GetInt(LastHashPref, int.MinValue)) return; // already handled
            _pendingPayload = payload;
            _pendingPreview = PreviewOf(payload);
            _status = "Studio Link: Bigfoot sent a build command. Review it below.";

            var settings = BudeStudioSettings.LoadOrCreate();
            if (settings.studioLinkAutoRun)
            {
                ExecutePending();
            }
            else
            {
                _status = "Studio Link: Bigfoot sent a build command - review and click EXECUTE.";
            }
        }

        // ===================== parsing =====================

        /// <summary>
        /// Scans a conversation JSON for the NEWEST assistant message containing a
        /// bude-exec fence. Returns the raw JSON payload, or null. Also: only ever
        /// acts on messages newer than the last one that was actually executed
        /// (hash-guarded by the caller).
        /// </summary>
        public static string FindNewestExecPayload(string conversationJson)
        {
            var root = MiniJson.GetDict(MiniJson.Deserialize(conversationJson));
            var messages = MiniJson.GetList(root, "messages");
            if (messages == null) return null;

            for (int i = messages.Count - 1; i >= 0; i--)
            {
                var m = MiniJson.GetDict(messages[i]);
                if (m == null) continue;
                if (m.ContainsKey("hidden") && m["hidden"] is bool hid && hid) continue;
                if (MiniJson.GetStr(m, "role") != "assistant") continue;

                string text = MiniJson.GetStr(m, "content");
                string payload = ExtractExecPayload(text);
                if (payload != null) return payload;
            }
            return null;
        }

        /// <summary>Pulls the JSON between ```bude-exec and the closing fence. Testable.</summary>
        public static string ExtractExecPayload(string messageText)
        {
            if (string.IsNullOrEmpty(messageText)) return null;
            int start = messageText.IndexOf(FenceStart, System.StringComparison.Ordinal);
            if (start < 0) return null;
            start += FenceStart.Length;
            int end = messageText.IndexOf(FenceEnd, start, System.StringComparison.Ordinal);
            string payload = end < 0
                ? messageText.Substring(start)
                : messageText.Substring(start, end - start);
            payload = payload.Trim();
            return payload.Length == 0 ? null : payload;
        }

        private static string PreviewOf(string payload)
        {
            var root = MiniJson.GetDict(MiniJson.Deserialize(payload));
            var ops = MiniJson.GetList(root, "ops");
            int count = ops != null ? ops.Count : 0;
            string preview = payload.Length > 500 ? payload.Substring(0, 500) + "..." : payload;
            return count + " op(s):\n" + preview;
        }

        // ===================== execution =====================

        /// <summary>Runs the pending command. Full Undo support, error capture, result posted back.</summary>
        public static void ExecutePending()
        {
            if (string.IsNullOrEmpty(_pendingPayload)) return;
            string payload = _pendingPayload;
            _pendingPayload = "";
            _pendingPreview = "";

            if (EditorApplication.isPlaying)
            {
                _lastResult = "Blocked: Unity is in Play mode. Exit Play mode first, then click EXECUTE again.";
                _status = _lastResult;
                return; // do NOT mark executed - let him retry
            }

            EditorPrefs.SetInt(LastHashPref, payload.GetHashCode());

            int ok = 0, fail = 0;
            var details = new List<string>();
            BeginErrorCapture();
            try
            {
                var root = MiniJson.GetDict(MiniJson.Deserialize(payload));
                var ops = MiniJson.GetList(root, "ops");
                if (ops == null)
                {
                    fail = 1;
                    details.Add("no 'ops' array found in payload");
                }
                else
                {
                    foreach (var opObj in ops)
                    {
                        var op = MiniJson.GetDict(opObj);
                        if (op == null) { fail++; details.Add("malformed op"); continue; }
                        string result = ExecuteOp(op);
                        if (result.StartsWith("OK")) { ok++; details.Add(result); }
                        else { fail++; details.Add(result); }
                    }
                }
            }
            catch (System.Exception e)
            {
                fail++;
                details.Add("EXCEPTION: " + e.Message);
            }
            finally
            {
                EndErrorCapture();
            }

            _lastResult = "ops ok:" + ok + " fail:" + fail +
                (_consoleErrors.Count > 0 ? " | console errors: " + string.Join("; ", _consoleErrors.ToArray()) : "");
            _status = "Studio Link: build finished. " + _lastResult;
            PostResult("STUDIO LINK RESULT: " + _lastResult +
                "\n" + string.Join("\n", details.ToArray()));
        }

        public static void SkipPending()
        {
            if (string.IsNullOrEmpty(_pendingPayload)) return;
            EditorPrefs.SetInt(LastHashPref, _pendingPayload.GetHashCode());
            _pendingPayload = "";
            _pendingPreview = "";
            _status = "Studio Link: command skipped.";
        }

        private static string ExecuteOp(IDictionary<string, object> op)
        {
            string kind = MiniJson.GetStr(op, "op").ToLowerInvariant();
            string name = MiniJson.GetStr(op, "name", "");

            switch (kind)
            {
                case "primitive":
                {
                    var type = ParsePrimitive(MiniJson.GetStr(op, "type", "Cube"));
                    var go = AssetFactory.CreatePrimitive(type, name, GetVec(op, "pos"));
                    ApplyScale(go, op);
                    return "OK primitive '" + go.name + "' created";
                }
                case "sprite":
                {
                    var sprite = AssetFactory.CreateSquareSprite(name, ParseColor(MiniJson.GetStr(op, "color", "#FFFFFF")));
                    var go = AssetFactory.CreateSpriteObject(name, sprite, GetVec(op, "pos"), Color.white);
                    ApplyScale(go, op);
                    return "OK sprite '" + go.name + "' created";
                }
                case "camera":
                {
                    bool ortho = MiniJson.GetStr(op, "ortho", "true").ToLowerInvariant() != "false";
                    var cam = AssetFactory.CreateCamera(name, ortho, GetF(op, "size", 5f), GetVec(op, "pos"));
                    return "OK camera '" + cam.name + "' created";
                }
                case "art":
                {
                    string url = MiniJson.GetStr(op, "url");
                    if (string.IsNullOrEmpty(url)) return "FAIL art op: missing url";
                    string category = MiniJson.GetStr(op, "category", "Imported");
                    StartArtDownload(url, name, category);
                    return "OK art download started ('" + (string.IsNullOrEmpty(name) ? url : name) + "')";
                }
                case "delete":
                {
                    var go = GameObject.Find(name);
                    if (go == null) return "FAIL delete: no object named '" + name + "'";
                    Undo.DestroyObjectImmediate(go);
                    return "OK deleted '" + name + "'";
                }
                case "build_2d_map":
                {
                    MapTemplate2D.BuildStarterMap(string.IsNullOrEmpty(name) ? "BS_StarterMap2D" : name);
                    return "OK built 2D starter map";
                }
                case "build_3d_map":
                {
                    MapTemplate3D.BuildStarterMap(string.IsNullOrEmpty(name) ? "BS_StarterMap3D" : name);
                    return "OK built 3D starter map";
                }
                case "save_scene":
                {
                    string path = MiniJson.GetStr(op, "path", "");
                    if (string.IsNullOrEmpty(path)) return "FAIL save_scene: missing path";
                    bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), path);
                    return saved ? "OK scene saved -> " + path : "FAIL save_scene failed";
                }
                default:
                    return "FAIL unknown op '" + kind + "'";
            }
        }

        private static void ApplyScale(GameObject go, IDictionary<string, object> op)
        {
            var scale = MiniJson.GetList(op, "scale");
            if (scale != null && scale.Count >= 3)
            {
                Undo.RecordObject(go.transform, "Bude Studio: scale");
                go.transform.localScale = new Vector3(
                    ToF(scale[0], 1f), ToF(scale[1], 1f), ToF(scale[2], 1f));
            }
        }

        private static Vector3 GetVec(IDictionary<string, object> op, string key)
        {
            var list = MiniJson.GetList(op, key);
            if (list == null || list.Count < 3) return Vector3.zero;
            return new Vector3(ToF(list[0]), ToF(list[1]), ToF(list[2]));
        }

        private static float GetF(IDictionary<string, object> op, string key, float def)
        {
            if (!op.ContainsKey(key)) return def;
            return ToF(op[key], def);
        }

        private static float ToF(object o, float def = 0f)
        {
            if (o is double d) return (float)d;
            if (o is float f) return f;
            if (o is long l) return l;
            if (o is int i) return i;
            float parsed;
            return float.TryParse(o == null ? "" : o.ToString(), out parsed) ? parsed : def;
        }

        private static PrimitiveType ParsePrimitive(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "sphere": return PrimitiveType.Sphere;
                case "capsule": return PrimitiveType.Capsule;
                case "cylinder": return PrimitiveType.Cylinder;
                case "quad": return PrimitiveType.Quad;
                case "plane": return PrimitiveType.Plane;
                default: return PrimitiveType.Cube;
            }
        }

        private static Color ParseColor(string hex)
        {
            if (!string.IsNullOrEmpty(hex) && hex.StartsWith("#") && hex.Length == 7)
            {
                byte r = System.Convert.ToByte(hex.Substring(1, 2), 16);
                byte g = System.Convert.ToByte(hex.Substring(3, 2), 16);
                byte b = System.Convert.ToByte(hex.Substring(5, 2), 16);
                return new Color32(r, g, b, 255);
            }
            return Color.white;
        }

        // ===================== console error capture =====================

        private static void BeginErrorCapture()
        {
            _consoleErrors = new List<string>();
            _capturingErrors = true;
            EditorApplication.logMessageReceived += OnLogReceived;
        }

        private static void EndErrorCapture()
        {
            if (_capturingErrors)
            {
                _capturingErrors = false;
                EditorApplication.logMessageReceived -= OnLogReceived;
            }
        }

        private static void OnLogReceived(string condition, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _consoleErrors.Add(condition);
        }

        // ===================== networking =====================

        private static UnityWebRequest MakeConversationGet()
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            string url = settings.agentApiBaseUrl.TrimEnd('/') + "/api/agents/" + settings.agentId +
                         "/conversations/" + BigfootChatTransport.ConversationId;
            var req = new UnityWebRequest(url, "GET");
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("api_key", settings.apiKey);
            req.timeout = 20;
            return req;
        }

        /// <summary>Posts the exec result back into the conversation so Bigfoot can verify and iterate.</summary>
        private static void PostResult(string text)
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            string url = settings.agentApiBaseUrl.TrimEnd('/') + "/api/agents/" + settings.agentId +
                         "/conversations/" + BigfootChatTransport.ConversationId + "/messages";
            var req = new UnityWebRequest(url, "POST");
            req.SetRequestHeader("api_key", settings.apiKey);
            req.SetRequestHeader("Content-Type", "application/json");
            string json = "{\"content\":" + JsonString(text) + "}";
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 30;
            req.SendWebRequest();
            _inflight.Add(req);
        }

        // ---- async art downloads (never block the editor loop: UnityWebRequest
        //      completion is pumped by the main thread, so busy-waiting would deadlock) ----

        private class ArtDownload
        {
            public UnityWebRequest Request;
            public string Name;
            public string Category;
        }

        private static readonly List<ArtDownload> _downloads = new List<ArtDownload>();

        private static void StartArtDownload(string url, string name, string category)
        {
            var req = UnityWebRequest.Get(url);
            req.timeout = 60;
            req.SendWebRequest();
            _downloads.Add(new ArtDownload { Request = req, Name = name, Category = category });
        }

        private static void PumpDownloads()
        {
            for (int i = _downloads.Count - 1; i >= 0; i--)
            {
                var dl = _downloads[i];
                if (!dl.Request.isDone) continue;
                string verdict;
                if (!string.IsNullOrEmpty(dl.Request.error))
                {
                    verdict = "STUDIO LINK RESULT (art): FAIL download failed - " + dl.Request.error;
                }
                else
                {
                    try
                    {
                        string ext = dl.Request.url != null && dl.Request.url.EndsWith(".jpg") ? ".jpg" : ".png";
                        string tempPath = Path.Combine(Path.GetTempPath(),
                            "bude_art_" + System.DateTime.Now.ToString("HHmmssfff") + ext);
                        File.WriteAllBytes(tempPath, dl.Request.downloadHandler.data);
                        var settings = BudeStudioSettings.LoadOrCreate();
                        string imported = AssetImporterTool.ImportFile(tempPath, dl.Category, settings.defaultPixelsPerUnit);
                        try { File.Delete(tempPath); } catch { }
                        verdict = string.IsNullOrEmpty(imported)
                            ? "STUDIO LINK RESULT (art): FAIL import failed"
                            : "STUDIO LINK RESULT (art): OK art imported '" + dl.Name + "' -> " + imported;
                    }
                    catch (System.Exception e)
                    {
                        verdict = "STUDIO LINK RESULT (art): FAIL " + e.Message;
                    }
                }
                _lastResult = verdict;
                _status = "Studio Link: " + verdict;
                PostResult(verdict);
                dl.Request.Dispose();
                _downloads.RemoveAt(i);
            }
        }

        private static string JsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            s = s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                 .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
            return "\"" + s + "\"";
        }

        // ===================== IMGUI =====================

        /// <summary>Drawn at the bottom of the CHAT tab.</summary>
        public static void DrawChatSection()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Studio Link", EditorStyles.boldLabel);

            var settings = BudeStudioSettings.LoadOrCreate();
            bool active = _polling;
            if (!settings.HasApiKey || string.IsNullOrEmpty(BigfootChatTransport.ConversationId))
            {
                EditorGUILayout.HelpBox(
                    "Studio Link lets Bigfoot build right here in Unity - no relay, no tunnels, " +
                    "nothing to run. It needs the API key (SETTINGS tab) and a connected conversation.",
                    MessageType.Info);
            }
            else if (!settings.studioLinkEnabled)
            {
                EditorGUILayout.HelpBox("Studio Link is off. Turn it on in SETTINGS.", MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField(_status, EditorStyles.miniLabel, GUILayout.Height(18));

                if (HasPending)
                {
                    EditorGUILayout.HelpBox(_pendingPreview, MessageType.Warning);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("EXECUTE", GUILayout.Height(36))) ExecutePending();
                        if (GUILayout.Button("SKIP", GUILayout.Height(36))) SkipPending();
                    }
                }
                else if (!string.IsNullOrEmpty(_lastResult))
                {
                    EditorGUILayout.HelpBox(_lastResult, MessageType.None);
                }
            }
        }

        /// <summary>Drawn in the SETTINGS tab.</summary>
        public static void DrawSettingsSection()
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            Undo.RecordObject(settings, "Bude Studio: settings");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Studio Link (Bigfoot builds in Unity)", EditorStyles.boldLabel);
            settings.studioLinkEnabled = EditorGUILayout.Toggle(
                new GUIContent("Enable Studio Link",
                    "Editor polls the Bigfoot conversation over normal HTTPS. No relay, no tunnel, no extra windows."),
                settings.studioLinkEnabled);
            settings.studioLinkAutoRun = EditorGUILayout.Toggle(
                new GUIContent("Run commands automatically",
                    "Off (recommended): each build command waits for your EXECUTE click. On: runs as soon as it arrives."),
                settings.studioLinkAutoRun);

            if (GUI.changed) EditorUtility.SetDirty(settings);
            EnsurePolling();
        }
    }
}
