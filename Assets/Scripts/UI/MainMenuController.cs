using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Main menu button handlers. Start Prologue loads the gameplay scene;
    /// Options opens the game site; Credits opens the Development.Bio release page.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string prologueScene = "TestPlayground";
        [SerializeField] private string optionsUrl = "https://x0205x.github.io/FPS-Game/";
        [SerializeField] private string creditsUrl = "https://github.com/x0205x/FPS-Game/releases/tag/Development.Bio";

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void StartPrologue()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SceneManager.LoadScene(prologueScene);
        }

        public void Options()
        {
            if (!string.IsNullOrEmpty(optionsUrl))
                Application.OpenURL(optionsUrl);
        }

        public void Credits()
        {
            if (!string.IsNullOrEmpty(creditsUrl))
                Application.OpenURL(creditsUrl);
        }
    }
}
