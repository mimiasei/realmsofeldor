using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace RealmsOfEldor.UI
{
    /// <summary>
    /// Main menu / splash screen UI controller.
    /// Provides navigation to map selection and quit functionality.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Scene Settings")]
        [SerializeField] private string mapSelectionSceneName = "MapSelection";

        void Start()
        {
            // Wire up button events
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            // Set title if provided
            if (titleText != null)
            {
                titleText.text = "Realms of Eldor";
            }
        }

        void OnDestroy()
        {
            // Clean up listeners
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        /// <summary>
        /// Called when Play button is clicked. Loads map selection scene.
        /// </summary>
        private void OnPlayClicked()
        {
            Debug.Log($"Loading map selection scene: {mapSelectionSceneName}");
            SceneManager.LoadScene(mapSelectionSceneName);
        }

        /// <summary>
        /// Called when Quit button is clicked. Exits the application.
        /// </summary>
        private void OnQuitClicked()
        {
            Debug.Log("Quitting application");

#if UNITY_EDITOR
            // In editor, stop play mode
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // In build, quit application
            Application.Quit();
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Test Play Button")]
        private void TestPlayButton()
        {
            OnPlayClicked();
        }

        [ContextMenu("Test Quit Button")]
        private void TestQuitButton()
        {
            OnQuitClicked();
        }
#endif
    }
}
