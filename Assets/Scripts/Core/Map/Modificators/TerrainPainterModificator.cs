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
            var moistureOffsetX = (float)random.NextDouble() * 1000f;
            var moistureOffsetY = (float)random.NextDouble() * 1000f;

            var biomeScale = 0.1f; // Controls biome feature size (lower = larger zones)
            var moistureScale = 0.05f; // Controls moisture variation (lower = larger moisture zones)

            // Track moisture distribution for logging
            var moistureCounts = new Dictionary<MoistureLevel, int>
            {
                { MoistureLevel.Arid, 0 },
                { MoistureLevel.Dry, 0 },
                { MoistureLevel.Temperate, 0 },
                { MoistureLevel.Wet, 0 }
            };

            // Use multiple Perlin noise octaves for natural-looking terrain
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pos = new Position(x, y);

                    // Sample Perlin noise for biome type
                    var biomeNoise = SampleMultiOctaveNoise(x + offsetX, y + offsetY, biomeScale, 3);

                    // Sample Perlin noise for moisture level (separate layer)
                    var moistureNoise = SampleMultiOctaveNoise(x + moistureOffsetX, y + moistureOffsetY, moistureScale, 3);

                    // Determine biome type from noise value
                    BiomeType biome;
                    if (biomeNoise < 0.25f)
                        biome = BiomeType.Grass;   // 25% grass
                    else if (biomeNoise < 0.50f)
                        biome = BiomeType.Dirt;    // 25% dirt
                    else if (biomeNoise < 0.70f)
                        biome = BiomeType.Sand;    // 20% sand
                    else if (biomeNoise < 0.85f)
                        biome = BiomeType.Rock;    // 15% rock (was Rough)
                    else
                        biome = BiomeType.Swamp;   // 15% swamp

                    // Determine moisture level from noise value
                    // TODO: Phase 2 - Enable all moisture levels once TerrainData assets are created
                    // For now, use Temperate for all terrain (we only have Temperate TerrainData assets)
                    MoistureLevel moisture = MoistureLevel.Temperate;

                    // DISABLED until Phase 2:
                    // if (moistureNoise < 0.25f)
                    //     moisture = MoistureLevel.Arid;
                    // else if (moistureNoise < 0.50f)
                    //     moisture = MoistureLevel.Dry;
                    // else if (moistureNoise < 0.75f)
                    //     moisture = MoistureLevel.Temperate;
                    // else
                    //     moisture = MoistureLevel.Wet;

                    moistureCounts[moisture]++;

                    // Set terrain with both biome and moisture
                    map.SetTerrain(pos, new TerrainType(biome, moisture));
                }
            }

            Debug.Log($"✓ {Name}: Generated zone-based terrain using Perlin noise with moisture variants");
            Debug.Log($"  Moisture distribution: Arid={moistureCounts[MoistureLevel.Arid]} ({moistureCounts[MoistureLevel.Arid]*100f/(width*height):F1}%), " +
                      $"Dry={moistureCounts[MoistureLevel.Dry]} ({moistureCounts[MoistureLevel.Dry]*100f/(width*height):F1}%), " +
                      $"Temperate={moistureCounts[MoistureLevel.Temperate]} ({moistureCounts[MoistureLevel.Temperate]*100f/(width*height):F1}%), " +
                      $"Wet={moistureCounts[MoistureLevel.Wet]} ({moistureCounts[MoistureLevel.Wet]*100f/(width*height):F1}%)");

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
