using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Generates splatmap textures from GameMap terrain data for use with InnoGames/Terrain shader.
    /// Converts TerrainType (BiomeType + MoistureLevel) data into RGBA channels for texture splatting.
    ///
    /// Splatmap channels:
    /// - R: Texture A (assigned biome type)
    /// - G: Texture B (assigned biome type)
    /// - B: Texture C (assigned biome type)
    /// - A: Texture D (assigned biome type)
    /// - Remaining: Ground texture (baseline)
    /// </summary>
    public class TerrainSplatmapGenerator : MonoBehaviour
    {
        [Header("Biome Mapping")]
        [Tooltip("Which biome type uses Red channel (Texture A)")]
        public BiomeType textureA = BiomeType.Grass;

        [Tooltip("Which biome type uses Green channel (Texture B)")]
        public BiomeType textureB = BiomeType.Sand;

        [Tooltip("Which biome type uses Blue channel (Texture C)")]
        public BiomeType textureC = BiomeType.Dirt;

        [Tooltip("Which biome type uses Alpha channel (Texture D)")]
        public BiomeType textureD = BiomeType.Rock;

        [Tooltip("Which biome type uses Ground texture (baseline)")]
        public BiomeType groundTexture = BiomeType.Swamp;

        [Header("Blending Settings")]
        [Tooltip("Enable smooth transitions between terrain types (Gaussian blur)")]
        [SerializeField] private bool enableBlending = true;

        [Tooltip("Blur radius for smooth transitions (0 = no blur, 1-3 recommended)")]
        [SerializeField] [Range(0, 5)] private int blurRadius = 1;

        [Header("Debug")]
        [SerializeField] private bool logGeneration = false;

        /// <summary>
        /// Generates a splatmap texture from GameMap terrain data.
        /// Returns a Texture2D with RGBA channels representing terrain distribution.
        /// </summary>
        public Texture2D GenerateSplatmap(GameMap gameMap)
        {
            if (gameMap == null)
            {
                Debug.LogError("TerrainSplatmapGenerator: Cannot generate splatmap, gameMap is null!");
                return null;
            }

            var width = gameMap.Width;
            var height = gameMap.Height;

            if (logGeneration)
                Debug.Log($"TerrainSplatmapGenerator: Generating {width}x{height} splatmap...");

            // Create texture with RGBA32 format and mipmaps enabled (critical for performance)
            var splatmap = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: true, linear: true);
            splatmap.wrapMode = TextureWrapMode.Clamp;
            splatmap.filterMode = FilterMode.Bilinear;

            // Generate base splatmap from terrain data
            var colors = new Color[width * height];
            var terrainCounts = new System.Collections.Generic.Dictionary<TerrainType, int>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pos = new Position(x, y);
                    var tile = gameMap.GetTile(pos);
                    var terrain = tile.Terrain;

                    // Count terrain types for logging
                    if (!terrainCounts.ContainsKey(terrain))
                        terrainCounts[terrain] = 0;
                    terrainCounts[terrain]++;

                    // Map TerrainType to splatmap channel
                    var color = TerrainTypeToSplatColor(terrain);
                    colors[y * width + x] = color;
                }
            }

            splatmap.SetPixels(colors);

            // Apply blending if enabled (smooth transitions)
            if (enableBlending && blurRadius > 0)
            {
                BlurSplatmap(splatmap, blurRadius);
            }

            splatmap.Apply(updateMipmaps: true);

            if (logGeneration)
            {
                Debug.Log($"✓ Splatmap generated: {width}x{height}");
                foreach (var kvp in terrainCounts)
                {
                    var percentage = (kvp.Value / (float)(width * height)) * 100f;
                    Debug.Log($"  - {kvp.Key}: {kvp.Value} tiles ({percentage:F1}%)");
                }
            }

            return splatmap;
        }

        /// <summary>
        /// Maps a TerrainType (BiomeType + MoistureLevel) to a splatmap color (RGBA channels).
        /// Returns Color with appropriate channel set to 1.0.
        /// Moisture level is currently ignored - future enhancement could modulate intensity.
        /// </summary>
        private Color TerrainTypeToSplatColor(TerrainType terrain)
        {
            var color = Color.black; // Default: ground texture (all channels 0)

            // Map based on biome type (ignoring moisture for now)
            if (terrain.Biome == textureA)
                color.r = 1.0f; // Red channel = Texture A
            else if (terrain.Biome == textureB)
                color.g = 1.0f; // Green channel = Texture B
            else if (terrain.Biome == textureC)
                color.b = 1.0f; // Blue channel = Texture C
            else if (terrain.Biome == textureD)
                color.a = 1.0f; // Alpha channel = Texture D
            // else: groundTexture (black = use ground texture)

            // Optional: Modulate intensity based on moisture (subtle variation)
            // Uncomment to add moisture-based intensity variation:
            // float moistureIntensity = terrain.Moisture switch
            // {
            //     MoistureLevel.Arid => 0.7f,
            //     MoistureLevel.Dry => 0.85f,
            //     MoistureLevel.Temperate => 1.0f,
            //     MoistureLevel.Wet => 1.15f,
            //     _ => 1.0f
            // };
            // color *= moistureIntensity;

            return color;
        }

        /// <summary>
        /// Applies Gaussian blur to splatmap for smooth terrain transitions.
        /// Blurs each channel independently to create natural blending.
        /// </summary>
        private void BlurSplatmap(Texture2D splatmap, int radius)
        {
            if (radius <= 0) return;

            var width = splatmap.width;
            var height = splatmap.height;
            var colors = splatmap.GetPixels();
            var blurred = new Color[colors.Length];

            // Simple box blur (efficient approximation of Gaussian)
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var sumR = 0f;
                    var sumG = 0f;
                    var sumB = 0f;
                    var sumA = 0f;
                    var count = 0;

                    // Sample neighbors within radius
                    for (var dy = -radius; dy <= radius; dy++)
                    {
                        for (var dx = -radius; dx <= radius; dx++)
                        {
                            var nx = x + dx;
                            var ny = y + dy;

                            // Clamp to bounds
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                                continue;

                            var neighborColor = colors[ny * width + nx];
                            sumR += neighborColor.r;
                            sumG += neighborColor.g;
                            sumB += neighborColor.b;
                            sumA += neighborColor.a;
                            count++;
                        }
                    }

                    // Average
                    blurred[y * width + x] = new Color(
                        sumR / count,
                        sumG / count,
                        sumB / count,
                        sumA / count
                    );
                }
            }

            splatmap.SetPixels(blurred);
        }

        /// <summary>
        /// Saves splatmap to PNG file (for debugging/inspection).
        /// </summary>
        public void SaveSplatmapToPNG(Texture2D splatmap, string filename)
        {
            if (splatmap == null) return;

            var bytes = splatmap.EncodeToPNG();
            var path = System.IO.Path.Combine(Application.dataPath, filename);
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log($"✓ Saved splatmap to: {path}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// Returns a summary of terrain-to-channel mappings.
        /// </summary>
        public string GetMappingSummary()
        {
            return $"Splatmap Channel Mapping:\n" +
                   $"  Red (A):   {textureA}\n" +
                   $"  Green (B): {textureB}\n" +
                   $"  Blue (C):  {textureC}\n" +
                   $"  Alpha (D): {textureD}\n" +
                   $"  Ground:    {groundTexture}";
        }
    }
}
