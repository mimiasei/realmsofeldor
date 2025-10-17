using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Places resource piles on the map using budget-based system.
    /// Based on VCMI's TreasurePlacer modificator (resource pile portion).
    /// </summary>
    public class ResourcePlacerModificator : MapModificator
    {
        private MapGenBudget budget;

        public override string Name => "Resource Placer";
        public override int Priority => 30;
        public override List<System.Type> Dependencies => new List<System.Type>
        {
            typeof(TerrainPainterModificator)
        };

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            budget = new MapGenBudget(config);

            var resourceAttempts = 0;
            var placedCount = 0;

            while (budget.CanPlaceResourcePile(1) && resourceAttempts < 100)
            {
                resourceAttempts++;
                var pos = FindClearPosition(map, random);
                if (pos == null)
                    continue;

                // Generate resource with value consideration
                var resourceType = (ResourceType)random.Next(0, 7);
                var amount = CalculateResourceAmount(resourceType, budget.RemainingTreasureBudget, config, random);

                var resource = new ResourceObject(pos.Value, resourceType, amount);

                // Check if this resource fits within budget
                if (budget.CanPlaceResourcePile(resource.Value))
                {
                    map.AddObject(resource);
                    budget.RecordResourcePile(resource);
                    placedCount++;
                }
            }

            Debug.Log($"✓ {Name}: Placed {placedCount} resource piles (budget: {budget.TotalTreasureValue}/{config.treasureBudget})");
        }

        private int CalculateResourceAmount(ResourceType resourceType, int remainingBudget, MapGenConfig config, System.Random random)
        {
            int minAmount, maxAmount;

            switch (resourceType)
            {
                case ResourceType.Gold:
                    minAmount = 500;
                    maxAmount = System.Math.Min(2000, remainingBudget / config.goldValueMultiplier);
                    break;

                case ResourceType.Wood:
                case ResourceType.Ore:
                    minAmount = 3;
                    maxAmount = System.Math.Min(10, remainingBudget / config.basicResourceValue);
                    break;

                case ResourceType.Mercury:
                case ResourceType.Sulfur:
                case ResourceType.Crystal:
                case ResourceType.Gems:
                    minAmount = 2;
                    maxAmount = System.Math.Min(6, remainingBudget / config.rareResourceValue);
                    break;

                default:
                    return 1;
            }

            maxAmount = System.Math.Max(minAmount, maxAmount);
            return random.Next(minAmount, maxAmount + 1);
        }

        private Position? FindClearPosition(GameMap map, System.Random random)
        {
            for (var attempts = 0; attempts < 50; attempts++)
            {
                var x = random.Next(1, map.Width - 1);
                var y = random.Next(1, map.Height - 1);
                var pos = new Position(x, y);

                if (map.GetTile(pos).IsClear())
                {
                    return pos;
                }
            }
            return null;
        }
    }
}
