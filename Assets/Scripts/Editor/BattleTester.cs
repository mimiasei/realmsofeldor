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

            // Initialize test battle
            EditorApplication.delayCall += () =>
            {
                SetupTestBattle();
                EditorApplication.isPlaying = true;
            };
        }

        /// <summary>
        /// Sets up a test battle with random creatures.
        /// </summary>
        private static void SetupTestBattle()
        {
            // Ensure GameStateManager exists
            var gameStateManager = GameObject.FindFirstObjectByType<GameStateManager>();
            if (gameStateManager == null)
            {
                var go = new GameObject("GameStateManager");
                gameStateManager = go.AddComponent<GameStateManager>();
                Debug.Log("Created GameStateManager for battle test");
            }

            // Ensure CreatureDatabase exists
            var creatureDatabase = GameObject.FindFirstObjectByType<CreatureDatabase>();
            if (creatureDatabase == null)
            {
                // Try to load from Resources
                var databasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CreatureDatabase.prefab");
                if (databasePrefab != null)
                {
                    PrefabUtility.InstantiatePrefab(databasePrefab);
                    Debug.Log("Loaded CreatureDatabase prefab");
                }
                else
                {
                    Debug.LogError("CreatureDatabase prefab not found at Assets/Prefabs/CreatureDatabase.prefab");
                    return;
                }
            }

            // Find BattleController
            var battleController = GameObject.FindFirstObjectByType<Controllers.Battle.BattleController>();
            if (battleController == null)
            {
                Debug.LogError("BattleController not found in scene! Please add it to the Battle scene.");
                return;
            }

            // Set up test battle through editor script callback
            // This will run when play mode starts
            var testSetup = battleController.gameObject.AddComponent<BattleTestSetup>();
            testSetup.hideFlags = HideFlags.DontSave; // Don't save this test component

            Debug.Log("Battle test setup complete. Press Play to start the test battle.");
        }

        /// <summary>
        /// Runtime component that creates test heroes with random armies.
        /// Destroyed after setup.
        /// </summary>
        private class BattleTestSetup : MonoBehaviour
        {
            private void Start()
            {
                // Create test battle with random creatures
                CreateRandomTestBattle();

                // Destroy this component after setup
                Destroy(this);
            }

            private void CreateRandomTestBattle()
            {
                if (GameStateManager.Instance == null)
                {
                    Debug.LogError("GameStateManager not found!");
                    return;
                }

                if (CreatureDatabase.Instance == null)
                {
                    Debug.LogError("CreatureDatabase not found!");
                    return;
                }

                // Get all available creatures
                var allCreatures = CreatureDatabase.Instance.GetAllCreatures().ToList();
                if (allCreatures.Count == 0)
                {
                    Debug.LogError("No creatures in database!");
                    return;
                }

                Debug.Log($"Creating test battle with {allCreatures.Count} creatures available");

                // Create attacker hero (player)
                var attackerHero = GameStateManager.Instance.State.AddHero(
                    typeId: 0,
                    owner: 0,
                    position: new Position(0, 0)
                );
                attackerHero.CustomName = "Test Hero";

                // Add 1 small creature stack to attacker (e.g., 5-10 creatures of tier 1-2)
                var playerCreature = GetRandomCreature(allCreatures, CreatureTier.Tier1, CreatureTier.Tier2);
                if (playerCreature != null)
                {
                    var count = Random.Range(5, 11);
                    attackerHero.Army.AddCreatures(0, playerCreature.creatureId, count);
                    Debug.Log($"Player army: {count}x {playerCreature.creatureName}");
                }

                // Create defender hero (AI)
                var defenderHero = GameStateManager.Instance.State.AddHero(
                    typeId: 1,
                    owner: 1,
                    position: new Position(1, 1)
                );
                defenderHero.CustomName = "Enemy Forces";

                // Add 2-4 random enemy creature stacks
                var enemyStackCount = Random.Range(2, 5);
                for (var i = 0; i < enemyStackCount; i++)
                {
                    var enemyCreature = GetRandomCreature(allCreatures);
                    if (enemyCreature != null)
                    {
                        // Scale count based on tier (higher tier = fewer creatures)
                        var count = enemyCreature.tier switch
                        {
                            CreatureTier.Tier1 => Random.Range(8, 15),
                            CreatureTier.Tier2 => Random.Range(5, 10),
                            CreatureTier.Tier3 => Random.Range(3, 7),
                            CreatureTier.Tier4 => Random.Range(2, 5),
                            CreatureTier.Tier5 => Random.Range(1, 4),
                            CreatureTier.Tier6 => Random.Range(1, 3),
                            CreatureTier.Tier7 => 1,
                            _ => Random.Range(3, 8)
                        };

                        defenderHero.Army.AddCreatures(i, enemyCreature.creatureId, count);
                        Debug.Log($"Enemy stack {i + 1}: {count}x {enemyCreature.creatureName} (Tier {enemyCreature.tier})");
                    }
                }

                // Tell BattleController to use these heroes
                var battleController = GetComponent<Controllers.Battle.BattleController>();
                if (battleController != null)
                {
                    // Use reflection to set private test fields
                    var type = battleController.GetType();
                    var attackerField = type.GetField("testAttacker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var defenderField = type.GetField("testDefender", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    attackerField?.SetValue(battleController, attackerHero);
                    defenderField?.SetValue(battleController, defenderHero);

                    Debug.Log("Test battle heroes assigned to BattleController");
                }
            }

            /// <summary>
            /// Get a random creature from the database, optionally filtered by tier.
            /// </summary>
            private Data.CreatureData GetRandomCreature(
                System.Collections.Generic.List<Data.CreatureData> creatures,
                CreatureTier? minTier = null,
                CreatureTier? maxTier = null)
            {
                var filtered = creatures;

                if (minTier.HasValue || maxTier.HasValue)
                {
                    filtered = creatures.Where(c =>
                    {
                        if (minTier.HasValue && c.tier < minTier.Value)
                            return false;
                        if (maxTier.HasValue && c.tier > maxTier.Value)
                            return false;
                        return true;
                    }).ToList();
                }

                if (filtered.Count == 0)
                    return creatures[Random.Range(0, creatures.Count)];

                return filtered[Random.Range(0, filtered.Count)];
            }
        }
    }
}
