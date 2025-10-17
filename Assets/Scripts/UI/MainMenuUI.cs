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

            // Debug: Log all scenes in build settings
            Debug.Log($"=== Scenes in Build Settings ===");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                Debug.Log($"[{i}] {scenePath}");
            }
            Debug.Log($"================================");
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

            // Try loading by name first, if that fails try by build index
            try
            {
                SceneManager.LoadScene(mapSelectionSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load scene by name '{mapSelectionSceneName}', trying by build index 1. Error: {e.Message}");
                SceneManager.LoadScene(1); // MapSelection should be at index 1
            }
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
