using UnityEngine;
using UnityEngine.Events;

namespace Game.Managers
{
    /// <summary>
    /// Lightweight global game state. Holds a singleton reference, the canonical
    /// pause flag (driven by Time.timeScale), and high-level events that UI and
    /// audio subscribe to.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public UnityEvent OnPaused;
        public UnityEvent OnResumed;
        public UnityEvent OnPlayerDied;
        public UnityEvent OnGameOver;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            OnPaused?.Invoke();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            OnResumed?.Invoke();
        }

        public void TogglePause()
        {
            if (IsPaused) Resume(); else Pause();
        }

        public void NotifyPlayerDied() => OnPlayerDied?.Invoke();
        public void NotifyGameOver()   => OnGameOver?.Invoke();
    }
}
