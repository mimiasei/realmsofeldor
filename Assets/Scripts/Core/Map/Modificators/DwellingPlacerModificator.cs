using System.Collections.Generic;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Places creature dwellings on the map using budget-based system.
    /// Based on VCMI's ObjectDistributor modificator (dwelling portion).
    /// </summary>
    public class DwellingPlacerModificator : MapModificator
    {
        private MapGenBudget budget;

        public override string Name => "Dwelling Placer";
        public override int Priority => 50;
        public override List<System.Type> Dependencies => new List<System.Type>
        {
            typeof(TerrainPainterModificator),
            typeof(ResourcePlacerModificator),
            typeof(MinePlacerModificator)
        };

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            budget = new MapGenBudget(config);

            var dwellingAttempts = 0;
            var placedCount = 0;

            while (budget.CanPlaceDwelling() && dwellingAttempts < 50)
            {
                dwellingAttempts++;
                var pos = FindClearPosition(map, random);
                if (pos == null)
                    continue;

                var creatureId = random.Next(1, 10);
                var weeklyGrowth = random.Next(5, 15);
                var dwelling = new DwellingObject(pos.Value, creatureId, weeklyGrowth);
                dwelling.ApplyWeeklyGrowth(); // Start with some creatures

                map.AddObject(dwelling);
                budget.RecordDwelling(dwelling);
                placedCount++;
            }

            Debug.Log($"✓ {Name}: Placed {placedCount} dwellings (limit: {config.dwellingCount})");
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
