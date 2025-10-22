using UnityEngine;
using System.Linq;
using RealmsOfEldor.Core;
using RealmsOfEldor.Database;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Runtime component that creates test battles with random creatures.
    /// Add this to BattleController in the Battle scene for testing.
    /// </summary>
    public class BattleTestSetup : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool autoSetup = true;
        [SerializeField] private int playerStackCount = 1;
        [SerializeField] private int enemyStackCountMin = 2;
        [SerializeField] private int enemyStackCountMax = 4;

        private void Start()
        {
            if (autoSetup)
            {
                StartCoroutine(SetupAfterDelay());
            }
        }

        private System.Collections.IEnumerator SetupAfterDelay()
        {
            // Wait for GameStateManager and CreatureDatabase to initialize
            yield return new WaitForSeconds(0.5f);

            CreateRandomTestBattle();
        }

        [ContextMenu("Create Random Test Battle")]
        public void CreateRandomTestBattle()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogError("BattleTestSetup: GameStateManager not found!");
                return;
            }

            if (CreatureDatabase.Instance == null)
            {
                Debug.LogError("BattleTestSetup: CreatureDatabase not found!");
                return;
            }

            // Get all available creatures
            var allCreatures = CreatureDatabase.Instance.GetAllCreatures().ToList();
            if (allCreatures.Count == 0)
            {
                Debug.LogError("BattleTestSetup: No creatures in database!");
                return;
            }

            Debug.Log($"BattleTestSetup: Creating test battle with {allCreatures.Count} creatures available");

            // Create attacker hero (player)
            var attackerHero = GameStateManager.Instance.State.AddHero(
                typeId: 0,
                owner: 0,
                position: new Position(0, 0)
            );
            attackerHero.CustomName = "Test Hero";

            // Add player creature stacks
            for (var i = 0; i < playerStackCount; i++)
            {
                var playerCreature = GetRandomCreature(allCreatures, CreatureTier.Tier1, CreatureTier.Tier2);
                if (playerCreature != null)
                {
                    var count = Random.Range(5, 11);
                    attackerHero.Army.AddCreatures(playerCreature.creatureId, count, i);
                    Debug.Log($"BattleTestSetup: Player army slot {i}: {count}x {playerCreature.creatureName}");
                }
            }

            // Create defender hero (AI)
            var defenderHero = GameStateManager.Instance.State.AddHero(
                typeId: 1,
                owner: 1,
                position: new Position(1, 1)
            );
            defenderHero.CustomName = "Enemy Forces";

            // Add random enemy creature stacks
            var enemyStackCount = Random.Range(enemyStackCountMin, enemyStackCountMax + 1);
            for (var i = 0; i < enemyStackCount; i++)
            {
                var enemyCreature = GetRandomCreature(allCreatures);
                if (enemyCreature != null)
                {
                    // Scale count based on tier
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

                    defenderHero.Army.AddCreatures(enemyCreature.creatureId, count, i);
                    Debug.Log($"BattleTestSetup: Enemy stack {i + 1}: {count}x {enemyCreature.creatureName} (Tier {enemyCreature.tier})");
                }
            }

            // Start the battle
            var battleController = GetComponent<BattleController>();
            if (battleController != null)
            {
                battleController.StartBattle(attackerHero, defenderHero);
                Debug.Log($"BattleTestSetup: Battle started - {attackerHero.CustomName} vs {defenderHero.CustomName}");
            }
            else
            {
                Debug.LogError("BattleTestSetup: BattleController not found on this GameObject!");
            }
        }

        private CreatureData GetRandomCreature(
            System.Collections.Generic.List<CreatureData> creatures,
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
