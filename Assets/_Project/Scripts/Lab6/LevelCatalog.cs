using System;
using UnityEngine;

namespace ArenaSurvival.Lab6
{
    [Serializable]
    public sealed class LevelEntry
    {
        public string title;
        public string scenePath;
    }

    /// <summary>Lists gameplay scenes without assuming their build indices.</summary>
    [CreateAssetMenu(menuName = "Arena Survival/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelEntry[] levels;
    }
}
