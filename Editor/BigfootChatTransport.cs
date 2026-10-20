using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// Talks to the real Bigfoot agent from inside the editor.
    ///
    /// API contract (verified 2026-10-20 against the live agent API):
    ///   Auth    : "api_key" header
    ///   List    : GET  {base}/api/agents/{agentId}/conversations
    ///   Message : POST {base}/api/agents/{agentId}/conversations/{conversationId}/messages
    ///             body { "content": "<your message>" }
    ///   The POST triggers the agent loop and returns when the assistant replies.
    ///
    /// The reply JSON shape varies, so extraction is defensive: it looks for the
    /// first string field it recognizes (content, text, response, reply, message,
    /// output) at any depth and unescapes it. Without an API key, everything falls
    /// back to opening the browser chat instead, so nothing blocks the beginner.
    /// </summary>
    public static class BigfootChatTransport
    {
        /// <summary>Last conversation id used (persisted per project via EditorPrefs).</summary>
        public const string ConversationPrefKey = "BudeStudio.Chat.ConversationId";

        public static string ConversationId
        {
            get { return EditorPrefs.GetString(ConversationPrefKey, ""); }
            set { EditorPrefs.SetString(ConversationPrefKey, value); }
        }

        /// <summary>Sent before each request so callers can show a spinner. Null = idle.</summary>
        public static event System.Action<string> OnStatus;

        /// <summary>Sends a message and calls back with Bigfoot's reply text.</summary>
        /// <param name="message">User message.</param>
        /// <param name="onDone">Called with the reply text, or an error message prefixed with ERROR:.</param>
        public static void Send(string message, System.Action<string> onDone)
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            OnStatus?.Invoke("Talking to Bigfoot...");

            if (!settings.HasApiKey)
            {
                OnStatus?.Invoke("No API key set - opening browser chat instead.");
                Application.OpenURL(settings.browserChatUrl);
                onDone?.Invoke("No API key set in SETTINGS. I opened your Bigfoot chat page in the browser instead.");
                return;
            }

            string baseUrl = settings.agentApiBaseUrl.TrimEnd('/');
            string agentId = settings.agentId;
            string convoId = ConversationId;
            string apiKey = settings.apiKey;

            var runner = new ChatRunner(baseUrl, agentId, convoId, apiKey, message, onDone);
            runner.Start();
        }

        /// <summary>
        /// Editor-safe request runner. Editor windows can't run coroutines, so
        /// requests are pumped via EditorApplication.update until done.
        /// </summary>
        private class ChatRunner
        {
            private readonly string _baseUrl, _agentId, _convoId, _apiKey, _message;
            private readonly System.Action<string> _onDone;
            private int _stage = 0; // 0 = need conversation id, 1 = send message
            private UnityWebRequest _request;
            private bool _done;

            public ChatRunner(string baseUrl, string agentId, string convoId, string apiKey,
                string message, System.Action<string> onDone)
            {
                _baseUrl = baseUrl;
                _agentId = agentId;
                _convoId = string.IsNullOrEmpty(convoId) ? null : convoId;
                _apiKey = apiKey;
                _message = message;
                _onDone = onDone;
            }

            public void Start()
            {
                if (_convoId != null)
                {
                    _stage = 1;
                    BeginMessageRequest(_convoId);
                }
                else
                {
                    _stage = 0;
                    BeginListConversations();
                }
                UnityEditor.EditorApplication.update += Tick;
            }

            private void BeginListConversations()
            {
                _request = UnityWebRequest.Get($"{_baseUrl}/api/agents/{_agentId}/conversations");
                _request.SetRequestHeader("api_key", _apiKey);
                _request.SendWebRequest();
            }

            private void BeginMessageRequest(string convoId)
            {
                _request = new UnityWebRequest(
                    $"{_baseUrl}/api/agents/{_agentId}/conversations/{convoId}/messages", "POST");
                _request.SetRequestHeader("api_key", _apiKey);
                _request.SetRequestHeader("Content-Type", "application/json");
                string json = "{\"content\":" + JsonEscape(_message) + "}";
                _request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                _request.downloadHandler = new DownloadHandlerBuffer();
                _request.timeout = 300;
                _request.SendWebRequest();
            }

            private void Tick()
            {
                if (_done || _request == null || !_request.isDone) return;

                if (_stage == 0)
                {
                    // Conversation list step: grab the first conversation id.
                    if (_request.result != UnityWebRequest.Result.Success)
                    {
                        Finish("ERROR: could not reach the agent API (" + _request.error + "). " +
                               "Opening browser chat instead.");
                        return;
                    }
                    string id = ExtractFirstConversationId(_request.downloadHandler.text);
                    if (string.IsNullOrEmpty(id))
                    {
                        Finish("ERROR: no conversations found on the agent. Open the Bigfoot chat once " +
                               "in your browser so a conversation exists, then retry.");
                        return;
                    }
                    ConversationId = id;
                    _stage = 1;
                    BeginMessageRequest(id);
                    return;
                }

                // Message step.
                if (_request.result != UnityWebRequest.Result.Success)
                {
                    string body = _request.downloadHandler != null ? _request.downloadHandler.text : "";
                    Finish("ERROR: chat request failed (" + _request.error + "). " + TryExtractReply(body));
                    return;
                }

                string response = _request.downloadHandler != null ? _request.downloadHandler.text : "";
                string reply = TryExtractReply(response);
                Debug.Log("[Bude Studio] Raw chat response: " + response);
                Finish(string.IsNullOrEmpty(reply)
                    ? "(Bigfoot replied, but I could not display it. The raw response is in the Console log.)"
                    : reply);
            }

            private void Finish(string result)
            {
                _done = true;
                UnityEditor.EditorApplication.update -= Tick;
                if (_request != null) _request.Dispose();
                OnStatus?.Invoke(null);
                _onDone?.Invoke(result);
            }
        }

        /// <summary>
        /// Defensive reply extraction. Tries known field names in order. Returns
        /// "" if nothing usable is found.
        /// </summary>
        public static string TryExtractReply(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";
            string[] preferred = { "content", "text", "response", "reply", "message", "output", "answer" };
            foreach (var field in preferred)
            {
                string v = ExtractStringField(json, field);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return "";
        }

        public static string ExtractFirstConversationId(string json)
        {
            var m = Regex.Match(json ?? "", "\"id\"\\s*:\\s*\"([0-9a-f]{16,})\"");
            return m.Success ? m.Groups[1].Value : "";
        }

        /// <summary>Finds "field":"value" (string values only) and unescapes the value.</summary>
        public static string ExtractStringField(string json, string field)
        {
            var m = Regex.Match(json ?? "",
                "\"" + Regex.Escape(field) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (!m.Success) return "";
            return Unescape(m.Groups[1].Value);
        }

        public static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t")
                    .Replace("\\\"", "\"").Replace("\\\\", "\\").Trim();
        }

        public static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            s = s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                 .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
            return "\"" + s + "\"";
        }
    }
}
