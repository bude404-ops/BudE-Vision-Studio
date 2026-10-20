using UnityEditor;
using UnityEngine;

namespace BudeStudio.EditorTools
{
    /// <summary>
    /// Generates a playable 3D arena starter scene: ground plane, blocks, a ramp,
    /// floating collectible orbs, a goal, and a third-person follow camera.
    /// </summary>
    public static class MapTemplate3D
    {
        public static void BuildStarterMap(string sceneName = "BS_StarterMap3D")
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Ground.
            var ground = AssetFactory.CreatePrimitive(PrimitiveType.Plane, "BS_Ground", Vector3.zero);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            var gr = ground.GetComponent<Renderer>();
            if (gr != null) gr.sharedMaterial = AssetFactory.CreateMaterial("Ground", new Color(0.42f, 0.58f, 0.45f));

            // Light.
            var lightGo = new GameObject("BS_DirectionalLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Undo.RegisterCreatedObjectUndo(lightGo, "Bude Studio: create light");

            // Blocks and a ramp.
            for (int i = 0; i < 5; i++)
            {
                var block = AssetFactory.CreatePrimitive(PrimitiveType.Cube, "BS_Block_" + i,
                    new Vector3(-6f + i * 3f, 0.5f + i * 0.4f, 4f));
                block.transform.localScale = new Vector3(2f, 1f + i * 0.6f, 2f);
            }
            var ramp = AssetFactory.CreatePrimitive(PrimitiveType.Cube, "BS_Ramp",
                new Vector3(4f, 0.35f, 2.5f));
            ramp.transform.localScale = new Vector3(2.5f, 0.5f, 3.5f);
            ramp.transform.rotation = Quaternion.Euler(-14f, 0f, 0f);

            // Player.
            var player = AssetFactory.CreatePrimitive(PrimitiveType.Capsule, "BS_Player",
                new Vector3(0f, 1.5f, -6f));
            var rbp = player.AddComponent<Rigidbody>();
            rbp.freezeRotation = true;
            player.AddComponent<PlayerController3D>();

            // Collectibles (floating orbs).
            for (int i = 0; i < 5; i++)
            {
                var orb = AssetFactory.CreatePrimitive(PrimitiveType.Sphere, "BS_Collectible_" + i,
                    new Vector3(-4f + i * 2f, 1.2f, -2f));
                orb.transform.localScale = Vector3.one * 0.6f;
                var oc = orb.AddComponent<SphereCollider>();
                oc.isTrigger = true;
                orb.AddComponent<Collectible>();
                var r = orb.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = AssetFactory.CreateMaterial("Orb", new Color(1f, 0.8f, 0.2f));
            }

            // Goal.
            var goal = AssetFactory.CreatePrimitive(PrimitiveType.Cylinder, "BS_GoalFlag",
                new Vector3(0f, 0.5f, 8f));
            goal.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
            var gc = goal.AddComponent<CapsuleCollider>();
            gc.isTrigger = true;
            goal.AddComponent<GoalFlag>();

            // Score.
            var scoreGo = new GameObject("BS_Score");
            var score = scoreGo.AddComponent<ScoreTracker>();
            score.scoreToWin = 5;

            // Camera (third-person follow).
            var camGo = new GameObject("BS_Camera");
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.is2D = false;
            follow.offset3D = new Vector3(0f, 6f, -8f);
            Undo.RegisterCreatedObjectUndo(camGo, "Bude Studio: create camera");

            // Save.
            MapTemplate2D.SaveScene(scene, sceneName, "3D arena map");
        }
    }
}
