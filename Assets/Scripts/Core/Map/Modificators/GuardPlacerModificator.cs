using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Adds monster guards to high-value objects.
    /// Based on VCMI's TreasurePlacer::addGuards() logic.
    /// </summary>
    public class GuardPlacerModificator : MapModificator
    {
        public override string Name => "Guard Placer";
        public override int Priority => 60;
        public override List<System.Type> Dependencies => new List<System.Type>
        {
            typeof(TerrainPainterModificator),
            typeof(ResourcePlacerModificator),
            typeof(MinePlacerModificator),
            typeof(DwellingPlacerModificator)
        };

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            if (!config.enableGuards)
            {
                Debug.Log($"✓ {Name}: Disabled in config");
                return;
            }

            var guardsPlaced = 0;
            foreach (var obj in map.GetAllObjects())
            {
                if (config.RequiresGuards(obj.Value))
                {
                    var guard = ChooseGuard(obj.Value, config, random);
                    if (guard != null)
                    {
                        obj.Guard = guard;
                        guardsPlaced++;
                    }
                }
            }

            Debug.Log($"✓ {Name}: Placed {guardsPlaced} guards for high-value objects");
        }

        private GuardInfo ChooseGuard(int treasureValue, MapGenConfig config, System.Random random)
        {
            if (!config.RequiresGuards(treasureValue))
                return null;

            var guardStrength = config.CalculateGuardStrength(treasureValue);
            if (guardStrength <= 0)
                return null;

            // Simplified creature list (creatureId, AIValue per creature)
            var availableCreatures = new[]
            {
                (id: 1, aiValue: 30),     // Weak creature
                (id: 2, aiValue: 80),     // Low-tier creature
                (id: 3, aiValue: 150),    // Medium-tier creature
                (id: 4, aiValue: 300),    // Strong creature
                (id: 5, aiValue: 600),    // Very strong creature
                (id: 6, aiValue: 1200),   // Elite creature
                (id: 7, aiValue: 2500)    // Champion creature
            };

            // Find suitable creatures
            var suitableCreatures = availableCreatures
                .Where(c => c.aiValue * 50 >= guardStrength && c.aiValue <= guardStrength * 100)
                .ToList();

            if (suitableCreatures.Count == 0)
            {
                // Fallback: use strongest available creature
                var strongest = availableCreatures[availableCreatures.Length - 1];
                var count = Mathf.Max(1, guardStrength / strongest.aiValue);
                return new GuardInfo(strongest.id, count, guardStrength);
            }

            // Pick a random suitable creature
            var chosen = suitableCreatures[random.Next(0, suitableCreatures.Count)];
            var guardCount = Mathf.Max(1, guardStrength / chosen.aiValue);

            // Add randomization for stacks of 4+
            if (guardCount >= 4)
            {
                guardCount = Mathf.RoundToInt(guardCount * (float)(random.NextDouble() * 0.5 + 0.75)); // 0.75-1.25x
            }

            return new GuardInfo(chosen.id, guardCount, guardStrength);
        }
    }
}
