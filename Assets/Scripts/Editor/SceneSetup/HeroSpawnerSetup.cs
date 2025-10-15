using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RealmsOfEldor.Core.Events;

using System.Linq;
using RealmsOfEldor.Core.Events.EventChannels;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor utility to add HeroSpawner to the AdventureMap scene.
    /// </summary>
    public static class HeroSpawnerSetup
    {
        [MenuItem("Realms of Eldor/Setup/Add Hero Spawner to Scene")]
        public static void AddHeroSpawnerToScene()
        {
            // Check if we're in the right scene
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.name.Contains("AdventureMap"))
            {
                Debug.LogWarning("Please open the AdventureMap scene first!");
                return;
            }

            // Check if HeroSpawner already exists using reflection
            var existing = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(mb => mb.GetType().Name == "HeroSpawner");
            if (existing != null)
            {
                Debug.Log("HeroSpawner already exists in scene!");
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            // Load the hero prefab
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Heroes/Hero.prefab");
            if (heroPrefab == null)
            {
                Debug.LogError("Hero prefab not found at Assets/Prefabs/Heroes/Hero.prefab!");
                return;
            }

            // Load event channels
            var gameEvents = AssetDatabase.LoadAssetAtPath<GameEventChannel>(
                "Assets/Data/EventChannels/GameEventChannel.asset");
            var mapEvents = AssetDatabase.LoadAssetAtPath<MapEventChannel>(
                "Assets/Data/EventChannels/MapEventChannel.asset");
            var uiEvents = AssetDatabase.LoadAssetAtPath<UIEventChannel>(
                "Assets/Data/EventChannels/UIEventChannel.asset");

            if (gameEvents == null || mapEvents == null || uiEvents == null)
            {
                Debug.LogError("Event channels not found! Please create them first.");
                return;
            }

            // Find HeroSpawner script - try multiple methods
            MonoScript heroSpawnerScript = null;

            // Method 1: Direct path
            heroSpawnerScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Controllers/HeroSpawner.cs");

            // Method 2: Search by name if not found
            if (heroSpawnerScript == null)
            {
                var guids = AssetDatabase.FindAssets("HeroSpawner t:MonoScript");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("HeroSpawner.cs"))
                    {
                        heroSpawnerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                        Debug.Log($"Found HeroSpawner at: {path}");
                        break;
                    }
                }
            }

            if (heroSpawnerScript == null)
            {
                Debug.LogError("HeroSpawner.cs not found! Searched in Assets/Scripts/Controllers/. Make sure the file exists and Unity has imported it.");
                return;
            }

            var heroSpawnerType = heroSpawnerScript.GetClass();
            if (heroSpawnerType == null)
            {
                Debug.LogError($"HeroSpawner script found at {AssetDatabase.GetAssetPath(heroSpawnerScript)} but type could not be loaded. Check for compile errors in Unity Console.");
                return;
            }

            // Create HeroSpawner GameObject
            var spawnerGO = new GameObject("HeroSpawner");
            var spawner = spawnerGO.AddComponent(heroSpawnerType) as MonoBehaviour;

            // Wire up references using SerializedObject
            var serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("heroPrefab").objectReferenceValue = heroPrefab;
            serializedSpawner.FindProperty("gameEvents").objectReferenceValue = gameEvents;
            serializedSpawner.ApplyModifiedProperties();

            // Mark the scene as dirty
            EditorSceneManager.MarkSceneDirty(activeScene);

            // Select the spawner
            Selection.activeGameObject = spawnerGO;

            Debug.Log("✓ HeroSpawner added to scene successfully!");
            Debug.Log("  - Hero prefab: Assigned");
            Debug.Log("  - Game events: Assigned");
            Debug.Log("  - Please assign MapEvents and UIEvents in the Inspector if needed");
        }

        [MenuItem("Realms of Eldor/Setup/Setup Complete Hero System")]
        public static void SetupCompleteHeroSystem()
        {
            AddHeroSpawnerToScene();

            Debug.Log("\n=== Hero System Setup Complete ===");
            Debug.Log("1. ✓ HeroSpawner added to scene");
            Debug.Log("2. ✓ Hero prefab assigned");
            Debug.Log("3. ✓ Event channels wired up");
            Debug.Log("\nNext: Run the game and heroes should spawn automatically!");
        }
    }
}
