using System;
using UnityEngine;

namespace Game.WaveSystem
{
    [Serializable]
    public class WaveSpawnEntry
    {
        public GameObject enemyPrefab;
        [Min(1)] public int count = 1;
        [Min(0f)] public float spawnDelay = 0.5f;
    }

    /// <summary>
    /// Authored data for a single wave: who spawns, how many, and per-wave
    /// difficulty scalars that <see cref="WaveManager"/> can read.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/WaveSystem/Wave Data", fileName = "WaveData")]
    public class WaveData : ScriptableObject
    {
        [Header("Spawning")]
        public WaveSpawnEntry[] entries;
        [Min(0f)] public float warmupSeconds = 1f;

        [Header("Difficulty Scalars (multipliers)")]
        [Min(0f)] public float healthMultiplier   = 1f;
        [Min(0f)] public float damageMultiplier   = 1f;
        [Min(0f)] public float accuracyMultiplier = 1f;
    }
}
