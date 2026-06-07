using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.AI;
using Game.Common;

namespace Game.WaveSystem
{
    /// <summary>
    /// Drives the wave loop. Spawns enemies from <see cref="waves"/>, applies
    /// difficulty scalars, watches for all of them dying, then advances.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private WaveData[] waves;
        [SerializeField, Min(0f)] private float secondsBetweenWaves = 4f;
        [SerializeField] private bool loopWaves = true;

        public UnityEvent<int>          OnWaveStarted;     // (waveIndex, 0-based)
        public UnityEvent<int>          OnWaveCompleted;   // (waveIndex)
        public UnityEvent<int, int>     OnEnemyCountChanged; // (alive, total)

        public int CurrentWaveIndex { get; private set; } = -1;
        public int AliveEnemies     { get; private set; }
        public int TotalEnemiesInWave { get; private set; }

        private readonly List<GameObject> _alive = new();

        private void Start() => StartCoroutine(RunWaves());

        private IEnumerator RunWaves()
        {
            if (waves == null || waves.Length == 0 || spawner == null) yield break;

            int idx = 0;
            do
            {
                CurrentWaveIndex = idx;
                yield return StartCoroutine(RunSingleWave(waves[idx]));
                OnWaveCompleted?.Invoke(idx);
                yield return new WaitForSeconds(secondsBetweenWaves);
                idx = loopWaves ? (idx + 1) % waves.Length : idx + 1;
            } while (loopWaves || idx < waves.Length);
        }

        private IEnumerator RunSingleWave(WaveData wave)
        {
            OnWaveStarted?.Invoke(CurrentWaveIndex);
            yield return new WaitForSeconds(wave.warmupSeconds);

            TotalEnemiesInWave = 0;
            foreach (var e in wave.entries) TotalEnemiesInWave += Mathf.Max(1, e.count);
            AliveEnemies = 0;
            _alive.Clear();
            OnEnemyCountChanged?.Invoke(AliveEnemies, TotalEnemiesInWave);

            foreach (WaveSpawnEntry entry in wave.entries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    GameObject go = spawner.Spawn(entry.enemyPrefab);
                    if (go != null)
                    {
                        ApplyDifficulty(go, wave);
                        TrackEnemy(go);
                    }
                    yield return new WaitForSeconds(entry.spawnDelay);
                }
            }

            while (AliveEnemies > 0) yield return null;
        }

        private void ApplyDifficulty(GameObject go, WaveData wave)
        {
            if (go.TryGetComponent<Health>(out var hp))
                hp.Revive(hp.MaxHealth * wave.healthMultiplier);
        }

        private void TrackEnemy(GameObject go)
        {
            _alive.Add(go);
            AliveEnemies++;
            OnEnemyCountChanged?.Invoke(AliveEnemies, TotalEnemiesInWave);

            if (go.TryGetComponent<Health>(out var hp))
            {
                UnityAction handler = null;
                handler = () =>
                {
                    hp.OnDied.RemoveListener(handler);
                    _alive.Remove(go);
                    AliveEnemies = Mathf.Max(0, AliveEnemies - 1);
                    OnEnemyCountChanged?.Invoke(AliveEnemies, TotalEnemiesInWave);
                };
                hp.OnDied.AddListener(handler);
            }
        }
    }
}
