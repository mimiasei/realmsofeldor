using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Unity 6 Build Profiles workaround - manually adds scenes to build settings.
    /// This fixes the issue where scenes are checked in Build Profiles but not actually included.
    /// </summary>
    public static class SceneBuildSettingsFixer
    {
        [MenuItem("Realms of Eldor/Fix Build Settings/Add All Scenes to Build", priority = 0)]
        public static void AddAllScenesToBuild()
        {
            // Define scene paths
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/MapSelection.unity",
                "Assets/Scenes/AdventureMap.unity"
            };

            // Create EditorBuildSettingsScene array
            var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            foreach (var scenePath in scenePaths)
            {
                // Check if scene exists
                if (System.IO.File.Exists(scenePath))
                {
                    var scene = new EditorBuildSettingsScene(scenePath, true);
                    sceneList.Add(scene);
                    Debug.Log($"✓ Added to build settings: {scenePath}");
                }
                else
                {
                    Debug.LogWarning($"⚠ Scene not found: {scenePath}");
                }
            }

            // Set the scenes in build settings
            EditorBuildSettings.scenes = sceneList.ToArray();

            Debug.Log($"✅ Build settings updated! {sceneList.Count} scenes added.");
            Debug.Log("Now try running the game again - scene loading should work.");
        }

        [MenuItem("Realms of Eldor/Fix Build Settings/Show Current Build Settings", priority = 1)]
        public static void ShowCurrentBuildSettings()
        {
            Debug.Log("=== Current Build Settings ===");
            var scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0)
            {
                Debug.LogWarning("⚠ NO SCENES in build settings!");
            }
            else
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    var scene = scenes[i];
                    string status = scene.enabled ? "✓" : "✗";
                    Debug.Log($"[{i}] {status} {scene.path}");
                }
            }

            Debug.Log("==============================");
        }

        [MenuItem("Realms of Eldor/Fix Build Settings/Clear All Build Settings", priority = 2)]
        public static void ClearBuildSettings()
        {
            if (EditorUtility.DisplayDialog(
                "Clear Build Settings?",
                "This will remove all scenes from build settings. You'll need to re-add them.",
                "Clear",
                "Cancel"))
            {
                EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
                Debug.Log("✓ Build settings cleared.");
            }
        }
    }
}
