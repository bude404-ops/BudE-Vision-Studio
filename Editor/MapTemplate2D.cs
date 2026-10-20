using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// Generates a playable 2D platformer starter scene with one click:
    /// floor + platforms + player + collectibles + goal + camera + score HUD.
    /// All art uses generated placeholder squares; replace them by importing
    /// your own art.
    /// </summary>
    public static class MapTemplate2D
    {
        public static void BuildStarterMap(string sceneName = "BS_StarterMap2D")
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite square = AssetFactory.CreateSquareSprite("White", Color.white);

            // Ground: several floor slabs.
            for (int i = 0; i < 12; i++)
            {
                var slab = AssetFactory.CreateSpriteObject("BS_Floor_" + i, square,
                    new Vector3(i * 2f - 11f, -2f, 0f), new Color(0.36f, 0.55f, 0.38f));
                slab.transform.localScale = new Vector3(2f, 1f, 1f);
                slab.AddComponent<BoxCollider2D>();
            }

            // A few floating platforms.
            float[][] platforms =
            {
                new[] { -6f, 0.5f, 3f },
                new[] { -1f, 1.5f, 3f },
                new[] { 4f, 0.8f, 3f },
                new[] { 8f, 1.8f, 3f },
                new[] { 11f, 0.3f, 2f }
            };
            foreach (var p in platforms)
            {
                var plat = AssetFactory.CreateSpriteObject("BS_Platform", square,
                    new Vector3(p[0], p[1], 0f), new Color(0.55f, 0.44f, 0.30f));
                plat.transform.localScale = new Vector3(p[2], 0.5f, 1f);
                plat.AddComponent<BoxCollider2D>();
            }

            // Player.
            var player = AssetFactory.CreateSpriteObject("BS_Player", square,
                new Vector3(-9f, 1f, 0f), new Color(0.85f, 0.30f, 0.25f));
            player.transform.localScale = new Vector3(0.9f, 1.2f, 1f);
            player.AddComponent<BoxCollider2D>();
            var rb = player.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 2.5f;
            player.AddComponent<PlayerController2D>();

            // Collectibles.
            for (int i = 0; i < 5; i++)
            {
                var coin = AssetFactory.CreateSpriteObject("BS_Collectible_" + i, square,
                    new Vector3(-6f + i * 4f, 2.2f + (i % 2) * 1.2f, 0f), new Color(1f, 0.8f, 0.2f));
                coin.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                var cc = coin.AddComponent<CircleCollider2D>();
                cc.isTrigger = true;
                coin.AddComponent<Collectible>();
            }

            // Goal flag.
            var flag = AssetFactory.CreateSpriteObject("BS_GoalFlag", square,
                new Vector3(11f, 1.5f, 0f), new Color(0.2f, 0.7f, 0.9f));
            flag.transform.localScale = new Vector3(0.8f, 2f, 1f);
            var fcol = flag.AddComponent<BoxCollider2D>();
            fcol.isTrigger = true;
            flag.AddComponent<GoalFlag>();

            // Score HUD.
            var scoreGo = new GameObject("BS_Score");
            var score = scoreGo.AddComponent<ScoreTracker>();
            var hud = AssetFactory.CreateText("BS_ScoreHud", "SCORE 0",
                new Vector3(-9.5f, 3.6f, 0f), 48, Color.white);
            score.hud = hud;
            score.scoreToWin = 5;

            // Camera.
            var cam = AssetFactory.CreateCamera("BS_Camera", true, 5f, new Vector3(-9f, 1f, -10f));
            cam.backgroundColor = new Color(0.62f, 0.78f, 0.88f);
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.is2D = true;

            // Save.
            SaveScene(scene, sceneName, "2D starter map");
        }

        internal static void SaveScene(UnityEngine.SceneManagement.Scene scene, string sceneName, string label)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            string path = $"Assets/Scenes/{sceneName}.unity";
            bool saved = EditorSceneManager.SaveScene(scene, path);
            Debug.Log(saved
                ? "[Bude Studio] " + label + " saved to " + path + " - press Play to test it."
                : "[Bude Studio] " + label + " created but NOT saved. Save the scene yourself now.");

            ProjectState.Load();
            ProjectState.Data.lastBuiltScene = path;
            if (string.IsNullOrEmpty(ProjectState.Data.gameTitle))
                ProjectState.Data.gameTitle = "My First Game";
            ProjectState.Save("Built " + label + ": " + path);
        }
    }
}
