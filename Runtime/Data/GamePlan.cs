using System;
using System.Collections.Generic;
using UnityEngine;

namespace BudeStudio
{
    /// <summary>
    /// Serializable unit of work inside the game plan. One task = one clear step
    /// a beginner can understand and check off.
    /// </summary>
    [Serializable]
    public class PlanTask
    {
        public string id;
        public string title;
        [TextArea(2, 6)] public string detail;
        public bool done;

        public PlanTask() { id = Guid.NewGuid().ToString("N").Substring(0, 8); }

        public PlanTask(string title, string detail)
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 8);
            this.title = title;
            this.detail = detail;
            done = false;
        }
    }

    /// <summary>
    /// The persistent Game Plan asset. Created and edited from the Bude Studio
    /// PLAN tab. This is the single source of truth for what the game is and
    /// what step it is on.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGamePlan", menuName = "Bude Studio/Game Plan")]
    public class GamePlan : ScriptableObject
    {
        [Header("Identity")]
        public string gameTitle = "Untitled Game";
        [TextArea(3, 10)] public string description = "";
        public string genre = "2D Platformer";
        public string artStyle = "Painterly storybook";

        [Header("Core Loop")]
        [TextArea(2, 8)] public string coreLoop =
            "Move through the world, collect things, avoid hazards, reach the goal.";

        [Header("Tasks")]
        public List<PlanTask> tasks = new List<PlanTask>();

        /// <summary>Number of completed tasks.</summary>
        public int CompletedTasks
        {
            get
            {
                int n = 0;
                if (tasks == null) return 0;
                for (int i = 0; i < tasks.Count; i++)
                    if (tasks[i] != null && tasks[i].done) n++;
                return n;
            }
        }

        public float Progress01
        {
            get { return tasks == null || tasks.Count == 0 ? 0f : (float)CompletedTasks / tasks.Count; }
        }

        /// <summary>Seed a brand-new plan with a sane starter task list.</summary>
        public void GenerateStarterTasks()
        {
            tasks = new List<PlanTask>
            {
                new PlanTask("Approve the game direction", "Read the description and core loop, tweak them until they feel right."),
                new PlanTask("Build the asset bible", "List every character, prop, and environment piece the game needs."),
                new PlanTask("Import or generate art", "Bring placeholder or final art into the project via the FACTORY tab."),
                new PlanTask("Build a starter map", "Use the CREATE tab to lay down a playable 2D or 3D level."),
                new PlanTask("Playtest and note issues", "Play the map. Write down everything that feels wrong."),
                new PlanTask("Ask Bigfoot for the next step", "Use the CHAT tab to get a concrete next action.")
            };
        }
    }
}
