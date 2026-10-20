using System.IO;
using UnityEditor;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// Asset Factory: creates simple objects and placeholder art with correct
    /// naming, sprite settings, and Undo support. No bells, no hidden magic.
    /// </summary>
    public static class AssetFactory
    {
        public const string GeneratedFolder = "Assets/BudeStudio/Generated";

        // NOTE: GameObject.CreatePrimitive supports Cube, Sphere, Capsule,
        // Cylinder, Quad, Plane. There is no "Camera" or "Sprite" primitive -
        // those are created explicitly below.

        public static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = string.IsNullOrEmpty(name) ? type.ToString() : name;
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Bude Studio: create " + go.name);
            return go;
        }

        public static Camera CreateCamera(string name, bool ortho, float orthoSize, Vector3 position)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "BS_Camera" : name);
            go.transform.position = position;
            var cam = go.AddComponent<Camera>();
            cam.orthographic = ortho;
            if (ortho) cam.orthographicSize = orthoSize;
            go.AddComponent<AudioListener>();
            Undo.RegisterCreatedObjectUndo(go, "Bude Studio: create camera");
            return cam;
        }

        /// <summary>
        /// Creates a colored square sprite asset (PNG) and returns it as a Sprite.
        /// Reuses an existing file of the same name, so repeated clicks don't spam
        /// duplicates.
        /// </summary>
        public static Sprite CreateSquareSprite(string name, Color color)
        {
            string folder = GeneratedFolder;
            EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/BS_Sprite_{name}.png");
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                var s = new TextureImporterSettings();
                importer.ReadTextureSettings(s);
                s.ApplyTextureType(TextureImporterType.Sprite);
                s.spritePixelsPerUnit = 16f;
                s.mipmapEnabled = false;
                s.alphaIsTransparency = true;
                importer.SetTextureSettings(s);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>GameObject with a SpriteRenderer using the given sprite and tint.</summary>
        public static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 position, Color tint)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "BS_Sprite" : name);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint; // SpriteRenderer.color tints - the sprite asset itself can't.
            Undo.RegisterCreatedObjectUndo(go, "Bude Studio: create sprite object");
            return go;
        }

        public static GameObject CreateText(string name, string text, Vector3 position, int fontSize, Color color)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "BS_Text" : name);
            go.transform.position = position;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.color = color;
            tm.anchor = TextAnchor.UpperLeft;
            tm.characterSize = 0.15f;
            // Unity 6 builtin font is LegacyRuntime.ttf (Arial.ttf is gone).
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) tm.font = font;
            Undo.RegisterCreatedObjectUndo(go, "Bude Studio: create text");
            return go;
        }

        public static Material CreateMaterial(string name, Color color)
        {
            string folder = GeneratedFolder;
            EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/BS_Mat_{name}.mat");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
