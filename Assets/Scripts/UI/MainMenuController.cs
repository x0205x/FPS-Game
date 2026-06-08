using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Main menu button handlers. Start Prologue loads the gameplay scene;
    /// Options opens the game site; Credits opens the author's GitHub profile.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string prologueScene = "TestPlayground";
        [SerializeField] private string optionsUrl = "https://x0205x.github.io/FPS-Game/";
        private const string CreditsProfileUrl = "https://github.com/x0205x";

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
            Application.OpenURL(CreditsProfileUrl);
        }
    }
}
