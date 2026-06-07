using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Managers;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// Pause menu. Listens for the Pause input event and toggles a panel,
    /// while routing the global pause state through <see cref="GameManager"/>.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private PlayerInput input;
        [SerializeField] private GameObject  panel;
        [SerializeField] private string      mainMenuScene = "MainMenu";

        private void Awake()
        {
            if (input == null) input = FindAnyObjectByType<PlayerInput>();
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (input != null) input.OnPausePerformed += Toggle;
        }

        private void OnDisable()
        {
            if (input != null) input.OnPausePerformed -= Toggle;
        }

        public void Toggle()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.TogglePause();
            if (panel != null) panel.SetActive(GameManager.Instance.IsPaused);
            Cursor.visible = GameManager.Instance.IsPaused;
            Cursor.lockState = GameManager.Instance.IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void Resume()
        {
            if (GameManager.Instance != null) GameManager.Instance.Resume();
            if (panel != null) panel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void GoToMainMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.Resume();
            SceneManager.LoadScene(mainMenuScene);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
