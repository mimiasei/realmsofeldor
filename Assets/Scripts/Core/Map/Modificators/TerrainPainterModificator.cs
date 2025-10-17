using System.Collections.Generic;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Paints terrain on the map (grass, dirt, sand, water, etc.).
    /// This is the first modificator to run - no dependencies.
    /// Uses Perlin noise for coherent, zone-like terrain generation (VCMI-style).
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

            // Generate random offsets for Perlin noise layers to ensure uniqueness
            var offsetX = (float)random.NextDouble() * 1000f;
            var offsetY = (float)random.NextDouble() * 1000f;
            var scale = 0.1f; // Controls terrain feature size (lower = larger zones)

            // Use multiple Perlin noise octaves for natural-looking terrain
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pos = new Position(x, y);

                    // Sample Perlin noise at this position
                    var noiseValue = SampleMultiOctaveNoise(x + offsetX, y + offsetY, scale, 3);

                    // Map noise value (0-1) to terrain type
                    // Creates large coherent areas of similar terrain
                    if (noiseValue < 0.25f)
                        map.SetTerrain(pos, TerrainType.Grass);   // 25% grass
                    else if (noiseValue < 0.50f)
                        map.SetTerrain(pos, TerrainType.Dirt);    // 25% dirt
                    else if (noiseValue < 0.70f)
                        map.SetTerrain(pos, TerrainType.Sand);    // 20% sand
                    else if (noiseValue < 0.85f)
                        map.SetTerrain(pos, TerrainType.Rough);   // 15% rough
                    else
                        map.SetTerrain(pos, TerrainType.Swamp);   // 15% swamp
                }
            }

            Debug.Log($"✓ {Name}: Generated zone-based terrain using Perlin noise");

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

            Debug.Log($"✓ {Name}: Added {lakeCount} water lakes");
        }

        /// <summary>
        /// Samples Perlin noise with multiple octaves for natural-looking terrain.
        /// </summary>
        private float SampleMultiOctaveNoise(float x, float y, float scale, int octaves)
        {
            float total = 0f;
            float frequency = scale;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;

                // Each octave has double frequency and half amplitude
                frequency *= 2f;
                amplitude *= 0.5f;
            }

            // Normalize to 0-1 range
            return total / maxValue;
        }
    }
}
