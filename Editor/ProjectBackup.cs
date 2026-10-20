using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// One-click project backup. Zips Assets + ProjectSettings into a timestamped
    /// archive next to the project (so Unity never imports it). Restore by
    /// unzipping over the project after closing Unity, or copy individual
    /// files back manually.
    /// </summary>
    public static class ProjectBackup
    {
        public static string Backup(bool openFolderAfter = true)
        {
            var settings = BudeStudioSettings.LoadOrCreate();
            string projectRoot = Path.GetDirectoryName(Application.dataPath); // one above Assets
            string backupsRoot = Path.Combine(projectRoot, settings.backupFolderName);
            if (!Directory.Exists(backupsRoot)) Directory.CreateDirectory(backupsRoot);

            string stamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string projectName = new DirectoryInfo(projectRoot).Name;
            string zipPath = Path.Combine(backupsRoot, $"{projectName}_{stamp}.zip");

            try
            {
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    AddFolder(zip, Application.dataPath, "Assets");
                    string ps = Path.Combine(projectRoot, "ProjectSettings");
                    if (Directory.Exists(ps)) AddFolder(zip, ps, "ProjectSettings");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Bude Studio] Backup failed: " + e.Message);
                EditorUtility.DisplayDialog("Backup failed", e.Message, "OK");
                return "";
            }

            Debug.Log("[Bude Studio] Backup created: " + zipPath);
            if (openFolderAfter) EditorUtility.RevealInFinder(zipPath);
            ProjectState.Load();
            ProjectState.Save("Backup created: " + Path.GetFileName(zipPath));
            return zipPath;
        }

        private static void AddFolder(ZipArchive zip, string folder, string entryPrefix)
        {
            string root = Path.GetFullPath(folder);
            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string rel = Path.GetFullPath(file).Substring(root.Length + 1).Replace('\\', '/');
                zip.CreateEntryFromFile(file, entryPrefix + "/" + rel);
            }
        }
    }
}
