using UnityEditor;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// Local settings for Bude Studio, stored as an asset in Assets/BudeStudio.
    /// The API key field is intentionally kept out of source control: the
    /// Assets/BudeStudio folder is gitignored inside game projects.
    /// </summary>
    public class BudeStudioSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/BudeStudio/BudeStudioSettings.asset";
        public const string FolderPath = "Assets/BudeStudio";

        [Header("Bigfoot chat")]
        [Tooltip("Base URL of the Bigfoot agent API.")]
        public string agentApiBaseUrl = "https://app.base44.com";

        [Tooltip("The Bigfoot agent id.")]
        public string agentId = "69f56986c51e35a876adf38d";

        [Tooltip("API key for the agent. Stored locally only, never committed.")]
        public string apiKey = "";

        [Tooltip("Opened in the browser when no API key is set, or on failure.")]
        public string browserChatUrl = "https://app.base44.com/superagent/69f56986c51e35a876adf38d";

        [Header("Import defaults")]
        [Tooltip("Pixels per unit applied to freshly imported sprites.")]
        public float defaultPixelsPerUnit = 16f;

        [Tooltip("Categories offered by the importer. Comma separated.")]
        public string categories = "Characters,Props,Environment,UI,Audio";

        [Header("Studio Link (Bigfoot builds in Unity)")]
        [Tooltip("Editor polls the Bigfoot conversation over normal HTTPS - no relay, no tunnel.")]
        public bool studioLinkEnabled = false;
        [Tooltip("Off: each build command waits for your EXECUTE click. On: runs immediately.")]
        public bool studioLinkAutoRun = false;

        [Header("Backups")]
        public string backupFolderName = "BudeStudioBackups";

        /// <summary>Loads the settings asset, creating it with defaults if missing.</summary>
        public static BudeStudioSettings LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BudeStudioSettings>(AssetPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder(FolderPath))
                AssetDatabase.CreateFolder("Assets", "BudeStudio");

            var settings = CreateInstance<BudeStudioSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public string[] CategoryList()
        {
            if (string.IsNullOrEmpty(categories)) return new[] { "Props" };
            var parts = categories.Split(',');
            var list = new System.Collections.Generic.List<string>();
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (!string.IsNullOrEmpty(t)) list.Add(t);
            }
            return list.Count > 0 ? list.ToArray() : new[] { "Props" };
        }

        public bool HasApiKey
        {
            get { return !string.IsNullOrEmpty(apiKey); }
        }
    }
}
