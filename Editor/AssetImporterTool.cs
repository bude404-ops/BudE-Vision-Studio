using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// Safe art importer.
    /// Flow: pick file -> pick category -> copy into Assets/Art/<category> ->
    /// duplicate check (MD5 of file bytes) -> overwrite approval if duplicate ->
    /// apply sprite import settings when it is an image -> optionally log it
    /// into the Asset Bible.
    /// </summary>
    public static class AssetImporterTool
    {
        public const string ArtRoot = "Assets/Art";

        /// <summary>Opens a file picker and imports the chosen file. Returns the imported asset path (or empty).</summary>
        public static string ImportFileWithDialog()
        {
            string src = EditorUtility.OpenFilePanel(
                "Pick an art file (png, jpg, tga, psd)", "", "png,jpg,jpeg,tga,psd,bmp");
            if (string.IsNullOrEmpty(src)) return "";

            var settings = BudeStudioSettings.LoadOrCreate();
            int cat = EditorUtility.DisplayDialogComplex(
                "Which category?",
                "Where does this asset belong?",
                "Characters", "Environment", "Props");
            string category = cat == 0 ? "Characters" : cat == 1 ? "Environment" : "Props";

            return ImportFile(src, category, settings.defaultPixelsPerUnit);
        }

        /// <summary>Copies the file into the project with duplicate handling and import settings.</summary>
        public static string ImportFile(string sourcePath, string category, float pixelsPerUnit)
        {
            if (!File.Exists(sourcePath))
            {
                EditorUtility.DisplayDialog("Import failed", "File not found:\n" + sourcePath, "OK");
                return "";
            }

            if (!AssetDatabase.IsValidFolder(ArtRoot)) AssetDatabase.CreateFolder("Assets", "Art");
            string folder = $"{ArtRoot}/{category}";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(ArtRoot, category);

            string fileName = Path.GetFileName(sourcePath);
            string dest = $"{folder}/{fileName}";
            string hash = Md5Of(sourcePath);

            // Duplicate check: any existing asset in the project with the same content?
            string existing = FindDuplicateByHash(hash, fileName);
            if (!string.IsNullOrEmpty(existing) && existing != dest)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Duplicate detected",
                    "A file with the exact same content already exists:\n" + existing +
                    "\n\nImport again anyway (a copy will be created)?",
                    "Import a copy", "Cancel");
                if (!overwrite) return existing;
                dest = AssetDatabase.GenerateUniqueAssetPath(dest);
            }

            File.Copy(sourcePath, dest, true);
            AssetDatabase.ImportAsset(dest);

            // Sprite settings for images.
            var importer = AssetImporter.GetAtPath(dest) as TextureImporter;
            if (importer != null)
            {
                var s = new TextureImporterSettings();
                importer.ReadTextureSettings(s);
                s.ApplyTextureType(TextureImporterType.Sprite);
                s.spritePixelsPerUnit = pixelsPerUnit;
                s.mipmapEnabled = false;
                s.alphaIsTransparency = true;
                importer.SetTextureSettings(s);
                importer.SaveAndReimport();
                Debug.Log($"[Bude Studio] Imported sprite '{fileName}' -> {dest} (PPU {pixelsPerUnit})");
            }
            else
            {
                Debug.Log($"[Bude Studio] Imported '{fileName}' -> {dest}");
            }

            AssetDatabase.SaveAssets();
            return dest;
        }

        /// <summary>MD5 hex hash of a file's bytes. Used for duplicate detection.</summary>
        public static string Md5Of(string path)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(path))
            {
                byte[] hash = md5.ComputeHash(stream);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>Finds an existing imported image whose file hash matches.</summary>
        private static string FindDuplicateByHash(string hash, string fileName)
        {
            var guids = AssetDatabase.FindAssets(
                Path.GetFileNameWithoutExtension(fileName), new[] { ArtRoot });
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsImageFile(p)) continue;
                try { if (Md5Of(p) == hash) return p; }
                catch { /* unreadable, skip */ }
            }
            return "";
        }

        private static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga"
                || ext == ".psd" || ext == ".bmp";
        }
    }
}
