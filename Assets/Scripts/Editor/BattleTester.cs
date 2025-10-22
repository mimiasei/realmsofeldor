using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealmsOfEldor.Core;
using RealmsOfEldor.Database;
using System.Linq;
using RealmsOfEldor.Controllers;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Battle system tester with random creature generation.
    /// Creates test battles with one hero vs random enemy creatures.
    /// </summary>
    public static class BattleTester
    {
        private const string BATTLE_SCENE_PATH = "Assets/Scenes/Battle.unity";

        [MenuItem("Realms of Eldor/Test Battle System")]
        public static void LaunchTestBattle()
        {
            // Check if battle scene exists
            if (!System.IO.File.Exists(BATTLE_SCENE_PATH))
            {
                EditorUtility.DisplayDialog(
                    "Battle Scene Missing",
                    $"Battle scene not found at: {BATTLE_SCENE_PATH}\n\nPlease create the battle scene first.",
                    "OK"
                );
                return;
            }

            // Save current scene
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "Save current scene before launching battle test?",
                    "Save",
                    "Don't Save"))
                {
                    return;
                }
                EditorSceneManager.SaveOpenScenes();
            }

            // Load battle scene
            var scene = EditorSceneManager.OpenScene(BATTLE_SCENE_PATH, OpenSceneMode.Single);

            // Find or create BattleController with test setup
            var battleController = GameObject.FindFirstObjectByType<Controllers.Battle.BattleController>();
            if (battleController == null)
            {
                EditorUtility.DisplayDialog(
                    "BattleController Missing",
                    "BattleController not found in scene! Please add it to the Battle scene.",
                    "OK"
                );
                return;
            }

            // Add BattleTestSetup component if not present
            var testSetup = battleController.GetComponent<Controllers.Battle.BattleTestSetup>();
            if (testSetup == null)
            {
                testSetup = battleController.gameObject.AddComponent<Controllers.Battle.BattleTestSetup>();
                Debug.Log("BattleTester: Added BattleTestSetup component to BattleController");
            }

            // Save the scene to persist the component
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // Enter play mode - BattleTestSetup component will handle the rest
            EditorApplication.isPlaying = true;
        }
    }
}
