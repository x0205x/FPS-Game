using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Main menu button handlers. Start Prologue loads the gameplay scene;
    /// Credits opens the project author's GitHub profile.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string prologueScene = "TestPlayground";
        [SerializeField] private string creditsUrl = "https://github.com/xBrophyx";

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

        public void Options() { }

        public void Credits()
        {
            if (!string.IsNullOrEmpty(creditsUrl))
                Application.OpenURL(creditsUrl);
        }
    }
}
