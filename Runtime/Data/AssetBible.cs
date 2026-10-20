using System;
using System.Collections.Generic;
using UnityEngine;

namespace BudeStudio
{
    public enum AssetStatus { Planned, Generating, Reviewing, Approved, InProject }

    /// <summary>
    /// One entry in the Asset Bible: a thing the game needs, where it stands,
    /// and where its file lives once imported.
    /// </summary>
    [Serializable]
    public class BibleEntry
    {
        public string category = "Props";       // Characters / Props / Environment / UI / Audio
        public string name = "New Asset";
        [TextArea(2, 6)] public string description = "";
        public AssetStatus status = AssetStatus.Planned;
        public string sourcePath = "";          // imported file path, when it exists
        public string hash = "";                // MD5 of source bytes, used for duplicate detection

        public BibleEntry() { }
        public BibleEntry(string category, string name, string description)
        {
            this.category = category;
            this.name = name;
            this.description = description;
        }
    }

    /// <summary>
    /// The persistent Asset Bible asset. Every piece of art the game needs,
    /// tracked from idea to in-project. Edited from the BIBLE tab.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAssetBible", menuName = "Bude Studio/Asset Bible")]
    public class AssetBible : ScriptableObject
    {
        public string gameTitle = "Untitled Game";
        [TextArea(2, 6)] public string artStyleNotes = "One brush, one palette, shared light sources.";

        public List<BibleEntry> entries = new List<BibleEntry>();

        public int CountByStatus(AssetStatus status)
        {
            int n = 0;
            if (entries == null) return 0;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].status == status) n++;
            return n;
        }

        /// <summary>Adds an entry if no entry with the same name+category exists yet.</summary>
        public bool TryAdd(BibleEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.name)) return false;
            if (entries == null) entries = new List<BibleEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null && e.name == entry.name && e.category == entry.category) return false;
            }
            entries.Add(entry);
            return true;
        }
    }
}
