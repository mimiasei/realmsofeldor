using System.Collections.Generic;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Paints terrain on the map (grass, dirt, sand, water, etc.).
    /// This is the first modificator to run - no dependencies.
    /// Based on VCMI's TerrainPainter modificator.
    /// </summary>
    public class TerrainPainterModificator : MapModificator
    {
        public override string Name => "Terrain Painter";
        public override int Priority => 10; // Run first
        public override List<System.Type> Dependencies => new List<System.Type>(); // No dependencies

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            var width = map.Width;
            var height = map.Height;

            // Fill with diverse terrain types
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pos = new Position(x, y);
                    var roll = random.NextDouble();

                    if (roll < 0.40)
                        map.SetTerrain(pos, TerrainType.Grass);
                    else if (roll < 0.65)
                        map.SetTerrain(pos, TerrainType.Dirt);
                    else if (roll < 0.85)
                        map.SetTerrain(pos, TerrainType.Sand);
                    else if (roll < 0.95)
                        map.SetTerrain(pos, TerrainType.Rough);
                    else
                        map.SetTerrain(pos, TerrainType.Swamp);
                }
            }

            // Add water lakes (impassable obstacles)
            var lakeCount = Mathf.Max(3, (width * height) / 200); // Scale with map size
            for (var i = 0; i < lakeCount; i++)
            {
                var centerX = random.Next(5, width - 5);
                var centerY = random.Next(5, height - 5);
                var radius = random.Next(2, 4);

                for (var y = -radius; y <= radius; y++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y <= radius * radius)
                        {
                            var pos = new Position(centerX + x, centerY + y);
                            if (map.IsInBounds(pos))
                            {
                                map.SetTerrain(pos, TerrainType.Water);
                            }
                        }
                    }
                }
            }

            Debug.Log($"✓ {Name}: Generated terrain with {lakeCount} water lakes");
        }
    }
}
