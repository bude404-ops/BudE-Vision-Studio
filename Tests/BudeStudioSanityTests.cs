using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BudeStudio.Tests
{
    /// <summary>
    /// Sanity tests. They intentionally avoid touching the AssetDatabase so they
    /// never pollute a real project: pure in-memory roundtrips and creation tests.
    /// Run via Window > General > Test Runner (EditMode).
    /// </summary>
    public class BudeStudioSanityTests
    {
        // ---------- GamePlan ----------

        [Test]
        public void GamePlan_JsonRoundtrip_PreservesFields()
        {
            var plan = ScriptableObject.CreateInstance<GamePlan>();
            plan.gameTitle = "Lil Foots";
            plan.description = "Bigfoot lore platformer";
            plan.tasks = new List<PlanTask> { new PlanTask("step one", "do it") };

            string json = JsonUtility.ToJson(plan);
            var copy = ScriptableObject.CreateInstance<GamePlan>();
            JsonUtility.FromJsonOverwrite(json, copy);

            Assert.AreEqual("Lil Foots", copy.gameTitle);
            Assert.AreEqual("Bigfoot lore platformer", copy.description);
            Assert.AreEqual(1, copy.tasks.Count);
            Assert.AreEqual("step one", copy.tasks[0].title);
            Assert.AreEqual("do it", copy.tasks[0].detail);
        }

        [Test]
        public void GamePlan_StarterTasks_AreSeededAndCounted()
        {
            var plan = ScriptableObject.CreateInstance<GamePlan>();
            plan.GenerateStarterTasks();
            Assert.GreaterOrEqual(plan.tasks.Count, 5, "starter task list should be usable");
            Assert.AreEqual(0, plan.CompletedTasks, "fresh plan has nothing done");
            Assert.AreEqual(0f, plan.Progress01);

            plan.tasks[0].done = true;
            Assert.AreEqual(1, plan.CompletedTasks);
            Assert.Greater(plan.Progress01, 0f);
        }

        // ---------- AssetBible ----------

        [Test]
        public void AssetBible_Creation_AndDuplicateGuard()
        {
            var bible = ScriptableObject.CreateInstance<AssetBible>();
            Assert.IsNotNull(bible);
            Assert.AreEqual(0, bible.entries.Count);

            Assert.IsTrue(bible.TryAdd(new BibleEntry("Characters", "Lily", "the hero")));
            Assert.IsFalse(bible.TryAdd(new BibleEntry("Characters", "Lily", "duplicate!")),
                "same name+category must not be added twice");
            Assert.IsTrue(bible.TryAdd(new BibleEntry("Props", "Lantern", "different category is fine")));
            Assert.AreEqual(2, bible.entries.Count);
        }

        // ---------- ProjectState ----------

        [Test]
        public void ProjectState_JsonRoundtrip_PreservesFields()
        {
            var data = new ProjectStateData
            {
                gameTitle = "Test Game",
                lastBuiltScene = "Assets/Scenes/X.unity",
                completedTaskCount = 3
            };
            string json = JsonUtility.ToJson(data);
            var copy = JsonUtility.FromJson<ProjectStateData>(json);

            Assert.AreEqual("Test Game", copy.gameTitle);
            Assert.AreEqual("Assets/Scenes/X.unity", copy.lastBuiltScene);
            Assert.AreEqual(3, copy.completedTaskCount);
        }

        // ---------- Chat transport (pure logic, no network) ----------

        [Test]
        public void BigfootChatTransport_ExtractReply_FindsKnownFields()
        {
            Assert.AreEqual("hello there",
                BigfootChatTransport.TryExtractReply("{\"content\":\"hello there\"}"));
            Assert.AreEqual("plain text",
                BigfootChatTransport.TryExtractReply("{\"result\":{\"text\":\"plain text\"}}"));
        }

        [Test]
        public void BigfootChatTransport_Escaping_Roundtrips()
        {
            string msg = "line1\nline2 \"quoted\"";
            string json = BigfootChatTransport.JsonEscape(msg);
            Assert.IsFalse(json.Contains("\n"), "newlines must be escaped for JSON");
            string back = BigfootChatTransport.ExtractStringField("{\"content\":" + json + "}", "content");
            Assert.AreEqual(msg, back);
        }

        [Test]
        public void BigfootChatTransport_ConversationId_Extracts()
        {
            string id = BigfootChatTransport.ExtractFirstConversationId(
                "[{\"id\":\"69f56989777455158d4472f4\",\"title\":null}]");
            Assert.AreEqual("69f56989777455158d4472f4", id);
        }

        // ---------- Settings (pure logic) ----------

        [Test]
        public void BudeStudioSettings_CategoryList_SplitsAndTrims()
        {
            var settings = ScriptableObject.CreateInstance<BudeStudioSettings>();
            settings.categories = "Characters, Props ,UI,,";
            var cats = settings.CategoryList();
            CollectionAssert.AreEqual(new[] { "Characters", "Props", "UI" }, cats);
        }
    }
}
