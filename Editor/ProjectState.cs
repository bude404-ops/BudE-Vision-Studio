using System.IO;
using UnityEditor;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// Project state, persisted as JSON inside the project (Assets/BudeStudio).
    /// Tracks what was last done so the window can tell you where you left off
    /// in plain English.
    /// </summary>
    [System.Serializable]
    public class ProjectStateData
    {
        public string gameTitle = "";
        public string gamePlanPath = "";
        public string assetBiblePath = "";
        public string lastBuiltScene = "";
        public int completedTaskCount = 0;
        public string lastAction = "Nothing yet. Pick the PLAN tab and describe your game!";
    }

    public static class ProjectState
    {
        public const string Folder = BudeStudioSettings.FolderPath;
        public const string FilePath = Folder + "/ProjectState.json";

        public static ProjectStateData Data = new ProjectStateData();

        /// <summary>Loads state from disk (tolerant of a missing file).</summary>
        public static ProjectStateData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var loaded = JsonUtility.FromJson<ProjectStateData>(json);
                    if (loaded != null) Data = loaded;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Bude Studio] Could not read project state: " + e.Message);
            }
            return Data;
        }

        public static void Save(string lastAction = null)
        {
            try
            {
                if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);
                if (!string.IsNullOrEmpty(lastAction)) Data.lastAction = lastAction;
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Bude Studio] Could not save project state: " + e.Message);
            }
        }
    }
}
