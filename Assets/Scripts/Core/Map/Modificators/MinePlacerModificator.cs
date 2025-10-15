using System.Collections.Generic;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Places resource mines on the map using budget-based system.
    /// Based on VCMI's MinePlacer modificator.
    /// </summary>
    public class MinePlacerModificator : MapModificator
    {
        private MapGenBudget budget;

        public override string Name => "Mine Placer";
        public override int Priority => 40;
        public override List<System.Type> Dependencies => new List<System.Type>
        {
            typeof(TerrainPainterModificator),
            typeof(ResourcePlacerModificator)
        };

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            budget = new MapGenBudget(config);

            var mineAttempts = 0;
            var placedCount = 0;

            while (budget.CanPlaceMine() && mineAttempts < 50)
            {
                mineAttempts++;
                var pos = FindClearPosition(map, random);
                if (pos == null)
                    continue;

                var resourceType = (ResourceType)random.Next(1, 7); // Not gold mines
                var production = random.Next(1, 3);
                var mine = new MineObject(pos.Value, resourceType, production);

                map.AddObject(mine);
                budget.RecordMine(mine);
                placedCount++;
            }

            Debug.Log($"✓ {Name}: Placed {placedCount} mines (limit: {config.mineCount})");
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
