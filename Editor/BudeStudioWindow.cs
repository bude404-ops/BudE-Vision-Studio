using UnityEditor;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// The Bude Studio main window. Big buttons, plain-English labels, tooltips.
    /// Tabs: PLAN (what we're building), BIBLE (asset library), FACTORY (make
    /// things), CREATE (starter maps), CHAT (talk to Bigfoot), SETTINGS.
    /// </summary>
    public class BudeStudioWindow : EditorWindow
    {
        private const string MenuPath = "Bude/Bude Studio";
        private const int BigButtonHeight = 40;

        private int _tab;
        private readonly string[] _tabs = { "PLAN", "BIBLE", "FACTORY", "CREATE", "CHAT", "SETTINGS" };

        private GamePlan _plan;
        private AssetBible _bible;
        private Vector2 _scroll;

        // CHAT tab state
        private string _chatInput = "Bigfoot, what should I build first?";
        private string _chatReply = "";
        private Vector2 _chatScroll;

        // BIBLE tab state
        private string _newCategory = "Props", _newName = "", _newDesc = "";

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var w = GetWindow<BudeStudioWindow>(false, "Bude Studio", true);
            w.minSize = new Vector2(520f, 560f);
        }

        private void OnEnable()
        {
            BudeStudioSettings.LoadOrCreate();
            ProjectState.Load();
            _plan = FindAsset<GamePlan>();
            _bible = FindAsset<AssetBible>();
        }

        private static T FindAsset<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            _tab = GUILayout.Toolbar(_tab, _tabs, GUILayout.Height(30));
            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_tab)
            {
                case 0: DrawPlanTab(); break;
                case 1: DrawBibleTab(); break;
                case 2: DrawFactoryTab(); break;
                case 3: DrawCreateTab(); break;
                case 4: DrawChatTab(); break;
                case 5: DrawSettingsTab(); break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Last action: " + ProjectState.Data.lastAction,
                EditorStyles.miniLabel);
        }

        // ------------------------------------------------- PLAN

        private void DrawPlanTab()
        {
            HelpBox("1. Describe your game. 2. Approve the tasks. 3. Work top to bottom. This plan is saved inside your project, so you never lose the thread.");

            _plan = (GamePlan)EditorGUILayout.ObjectField("Game Plan", _plan, typeof(GamePlan), false);
            if (_plan == null)
            {
                if (BigButton("Create a Game Plan", "Makes the plan asset in Assets/BudeStudio and fills in a starter task list."))
                {
                    if (!AssetDatabase.IsValidFolder(BudeStudioSettings.FolderPath))
                        AssetDatabase.CreateFolder("Assets", "BudeStudio");
                    _plan = CreateInstance<GamePlan>();
                    _plan.GenerateStarterTasks();
                    AssetDatabase.CreateAsset(_plan,
                        AssetDatabase.GenerateUniqueAssetPath(BudeStudioSettings.FolderPath + "/GamePlan.asset"));
                    AssetDatabase.SaveAssets();
                    ProjectState.Data.gamePlanPath = AssetDatabase.GetAssetPath(_plan);
                    ProjectState.Data.gameTitle = _plan.gameTitle;
                    ProjectState.Save("Created game plan");
                }
                return;
            }

            Undo.RecordObject(_plan, "Bude Studio: edit plan");

            _plan.gameTitle = EditorGUILayout.TextField("Title", _plan.gameTitle);
            _plan.description = EditorGUILayout.TextArea(_plan.description, GUILayout.MinHeight(60));
            _plan.genre = EditorGUILayout.TextField("Genre", _plan.genre);
            _plan.artStyle = EditorGUILayout.TextField("Art style", _plan.artStyle);
            _plan.coreLoop = EditorGUILayout.TextArea(_plan.coreLoop, GUILayout.MinHeight(40));

            EditorGUILayout.Space(6);
            Rect r = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.ProgressBar(r, _plan.Progress01,
                "Tasks: " + _plan.CompletedTasks + " / " + _plan.tasks.Count + " done");
            EditorGUILayout.Space(6);

            for (int i = 0; i < _plan.tasks.Count; i++)
            {
                var t = _plan.tasks[i];
                EditorGUILayout.BeginHorizontal();
                bool was = t.done;
                t.done = EditorGUILayout.ToggleLeft(
                    string.IsNullOrEmpty(t.title) ? "(untitled)" : t.title, t.done, GUILayout.Width(260));
                if (t.done != was)
                {
                    EditorUtility.SetDirty(_plan);
                    ProjectState.Save((t.done ? "Completed: " : "Reopened: ") + t.title);
                }
                t.detail = EditorGUILayout.TextArea(t.detail, GUILayout.MinHeight(0));
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    _plan.tasks.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (BigButton("+ Add task", "Adds an empty task you can type into.", 28))
            {
                _plan.tasks.Add(new PlanTask("New task", ""));
                GUIUtility.ExitGUI();
            }
            if (BigButton("Reset to starter tasks", "Replaces the list with the standard starter checklist.", 28))
                _plan.GenerateStarterTasks();
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_plan);
                ProjectState.Data.gameTitle = _plan.gameTitle;
                ProjectState.Data.gamePlanPath = AssetDatabase.GetAssetPath(_plan);
            }
        }

        // ------------------------------------------------- BIBLE

        private void DrawBibleTab()
        {
            HelpBox("The Asset Bible is your library: everything the game needs, with a status each. Import files safely with the button at the bottom; duplicates are caught automatically.");

            _bible = (AssetBible)EditorGUILayout.ObjectField("Asset Bible", _bible, typeof(AssetBible), false);
            if (_bible == null)
            {
                if (BigButton("Create an Asset Bible", "Makes the bible asset in Assets/BudeStudio."))
                {
                    if (!AssetDatabase.IsValidFolder(BudeStudioSettings.FolderPath))
                        AssetDatabase.CreateFolder("Assets", "BudeStudio");
                    _bible = CreateInstance<AssetBible>();
                    AssetDatabase.CreateAsset(_bible,
                        AssetDatabase.GenerateUniqueAssetPath(BudeStudioSettings.FolderPath + "/AssetBible.asset"));
                    AssetDatabase.SaveAssets();
                    ProjectState.Data.assetBiblePath = AssetDatabase.GetAssetPath(_bible);
                    ProjectState.Save("Created asset bible");
                }
                return;
            }

            Undo.RecordObject(_bible, "Bude Studio: edit bible");

            _bible.gameTitle = EditorGUILayout.TextField("For game", _bible.gameTitle);
            _bible.artStyleNotes = EditorGUILayout.TextArea(_bible.artStyleNotes, GUILayout.MinHeight(36));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Entries: " + _bible.entries.Count +
                "  (Planned " + _bible.CountByStatus(AssetStatus.Planned) +
                ", In project " + _bible.CountByStatus(AssetStatus.InProject) + ")",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            for (int i = 0; i < _bible.entries.Count; i++)
            {
                var e = _bible.entries[i];
                EditorGUILayout.BeginHorizontal();
                e.status = (AssetStatus)EditorGUILayout.EnumPopup(e.status, GUILayout.Width(90));
                e.category = EditorGUILayout.TextField(e.category, GUILayout.Width(90));
                e.name = EditorGUILayout.TextField(e.name);
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    _bible.entries.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(e.sourcePath))
                    EditorGUILayout.LabelField("   file: " + e.sourcePath, EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Add an entry", EditorStyles.boldLabel);
            _newCategory = EditorGUILayout.TextField("Category", _newCategory);
            _newName = EditorGUILayout.TextField("Name", _newName);
            _newDesc = EditorGUILayout.TextField("Description", _newDesc);
            if (BigButton("+ Add to bible", "Adds the entry (no duplicates: same name + category is skipped).") &&
                !string.IsNullOrEmpty(_newName))
            {
                _bible.TryAdd(new BibleEntry(_newCategory, _newName, _newDesc));
                _newName = ""; _newDesc = "";
            }

            EditorGUILayout.Space(8);
            if (BigButton("Import an art file...", "Pick a file, pick a category, done: copied into Assets/Art, sprite settings applied, duplicates flagged."))
            {
                string imported = AssetImporterTool.ImportFileWithDialog();
                if (!string.IsNullOrEmpty(imported))
                {
                    string fileName = System.IO.Path.GetFileName(imported);
                    _bible.TryAdd(new BibleEntry("Imported", fileName, imported)
                    {
                        sourcePath = imported,
                        status = AssetStatus.InProject
                    });
                    ProjectState.Save("Imported " + fileName);
                }
            }

            if (GUI.changed) EditorUtility.SetDirty(_bible);
        }

        // ------------------------------------------------- FACTORY

        private void DrawFactoryTab()
        {
            HelpBox("One-click objects, placed at the scene origin so you can drag them anywhere. Everything here is undoable (Ctrl+Z).");

            EditorGUILayout.LabelField("3D shapes", EditorStyles.boldLabel);
            string[] prims = { "Cube", "Sphere", "Capsule", "Cylinder", "Quad", "Plane" };
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < prims.Length; i++)
            {
                if (GUILayout.Button(prims[i], GUILayout.Height(BigButtonHeight)))
                {
                    var type = (PrimitiveType)System.Enum.Parse(typeof(PrimitiveType), prims[i]);
                    AssetFactory.CreatePrimitive(type, "BS_" + prims[i], Vector3.zero);
                    ProjectState.Save("Created " + prims[i]);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Cameras", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (BigButton("2D camera", "Orthographic camera with follow script, ready for platformers.", 30))
            {
                var cam = AssetFactory.CreateCamera("BS_Camera_2D", true, 5f, new Vector3(0, 0, -10));
                var follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.is2D = true;
            }
            if (BigButton("3D camera", "Perspective camera with third-person follow.", 30))
            {
                var camGo = new GameObject("BS_Camera_3D");
                camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                var follow3D = camGo.AddComponent<CameraFollow>();
                follow3D.is2D = false;
                Undo.RegisterCreatedObjectUndo(camGo, "Bude Studio: create camera");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("2D helpers", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (BigButton("Colored square sprite", "Creates a tintable square sprite asset in Assets/BudeStudio/Generated.", 30))
            {
                AssetFactory.CreateSquareSprite("White", Color.white);
                ProjectState.Save("Created square sprite");
            }
            if (BigButton("Text label", "Plain text object (TextMesh) for HUD or signs.", 30))
                AssetFactory.CreateText("BS_Text", "Hello world", Vector3.zero, 48, Color.white);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Gameplay", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (BigButton("Collectible", "A trigger that adds to the score when the player touches it.", 30))
            {
                var go = AssetFactory.CreatePrimitive(PrimitiveType.Sphere, "BS_Collectible", Vector3.zero);
                var c = go.AddComponent<SphereCollider>();
                c.isTrigger = true;
                go.AddComponent<Collectible>();
            }
            if (BigButton("Score keeper", "Empty object with the ScoreTracker component. One per scene.", 30))
            {
                var go = new GameObject("BS_Score");
                go.AddComponent<ScoreTracker>();
                Undo.RegisterCreatedObjectUndo(go, "Bude Studio: create score");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            if (BigButton("Import an art file...", "Same safe importer as in the BIBLE tab."))
                AssetImporterTool.ImportFileWithDialog();
        }

        // ------------------------------------------------- CREATE

        private void DrawCreateTab()
        {
            HelpBox("Build a whole playable starter scene in one click. Placeholder art is generated for you; replace it later by importing your own. Scenes are saved to Assets/Scenes.");

            if (BigButton("Build 2D platformer starter map",
                "Floor, platforms, a player with controls, 5 collectibles, a goal flag, score HUD, following camera."))
                MapTemplate2D.BuildStarterMap();

            EditorGUILayout.Space(6);
            if (BigButton("Build 3D arena starter map",
                "Ground, blocks, ramp, a 3D player with controls, floating orbs to collect, a goal, follow camera."))
                MapTemplate3D.BuildStarterMap();

            EditorGUILayout.Space(8);
            if (!string.IsNullOrEmpty(ProjectState.Data.lastBuiltScene))
            {
                EditorGUILayout.LabelField("Last built scene: " + ProjectState.Data.lastBuiltScene);
                if (BigButton("Open last built scene", "Loads the scene you generated most recently.", 26))
                    OpenScene(ProjectState.Data.lastBuiltScene);
            }
        }

        private static void OpenScene(string path)
        {
            if (System.IO.File.Exists(path))
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);
            else
                EditorUtility.DisplayDialog("Not found", "That scene file no longer exists:\n" + path, "OK");
        }

        // ------------------------------------------------- CHAT

        private void DrawChatTab()
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            if (!settings.HasApiKey)
                HelpBox("No API key yet: messages open your Bigfoot chat page in the browser instead. Paste the agent API key in the SETTINGS tab to chat right here.");

            EditorGUILayout.LabelField("Message to Bigfoot", EditorStyles.boldLabel);
            _chatInput = EditorGUILayout.TextArea(_chatInput, GUILayout.MinHeight(70));

            EditorGUILayout.BeginHorizontal();
            if (BigButton("Send", "Sends the message to your Bigfoot agent and shows the reply here.", 30))
            {
                if (!string.IsNullOrEmpty(_chatInput))
                {
                    _chatReply = "(thinking...)";
                    Repaint();
                    BigfootChatTransport.Send(_chatInput, reply => { _chatReply = reply; Repaint(); });
                }
            }
            if (GUILayout.Button("Open chat in browser", GUILayout.Height(30)))
                Application.OpenURL(settings.browserChatUrl);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Reply", EditorStyles.boldLabel);
            _chatScroll = EditorGUILayout.BeginScrollView(_chatScroll, GUILayout.MinHeight(120));
            EditorGUILayout.TextArea(_chatReply ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------- SETTINGS

        private void DrawSettingsTab()
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            Undo.RecordObject(settings, "Bude Studio: settings");

            EditorGUILayout.LabelField("Bigfoot chat hookup", EditorStyles.boldLabel);
            settings.agentApiBaseUrl = EditorGUILayout.TextField("API base URL", settings.agentApiBaseUrl);
            settings.agentId = EditorGUILayout.TextField("Agent id", settings.agentId);
            settings.apiKey = EditorGUILayout.PasswordField("API key", settings.apiKey);
            settings.browserChatUrl = EditorGUILayout.TextField("Browser chat URL", settings.browserChatUrl);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Import defaults", EditorStyles.boldLabel);
            settings.defaultPixelsPerUnit = EditorGUILayout.FloatField("Sprite pixels per unit", settings.defaultPixelsPerUnit);
            settings.categories = EditorGUILayout.TextField("Categories", settings.categories);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Backups", EditorStyles.boldLabel);
            settings.backupFolderName = EditorGUILayout.TextField("Backup folder", settings.backupFolderName);
            if (BigButton("Back up project now", "Zips Assets + ProjectSettings into a timestamped file next to the project.", 30))
                ProjectBackup.Backup();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "The API key lives in " + BudeStudioSettings.AssetPath + " inside your project. " +
                "Add 'Assets/BudeStudio/' to your project's .gitignore if you use git, so the key never leaves your machine.",
                MessageType.Info);

            if (GUI.changed) EditorUtility.SetDirty(settings);
        }

        // ------------------------------------------------- helpers

        private static bool BigButton(string label, string tooltip, int height = BigButtonHeight)
        {
            return GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(height));
        }

        private static void HelpBox(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.None);
            EditorGUILayout.Space(2);
        }
    }
}
