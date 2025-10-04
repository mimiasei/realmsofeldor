using UnityEngine;
using UnityEditor;
using System.IO;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor tool to generate placeholder sprite tiles for terrain.
    /// Creates simple colored 128x128 PNG textures for each terrain type.
    /// Menu: Realms of Eldor/Generate/Placeholder Terrain Sprites
    /// </summary>
    public class PlaceholderSpriteGenerator
    {
        [MenuItem("Realms of Eldor/Generate/Placeholder Terrain Sprites")]
        public static void GeneratePlaceholderSprites()
        {
            var folderPath = "Assets/Sprites/Terrain";

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            {
                AssetDatabase.CreateFolder("Assets", "Sprites");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Sprites", "Terrain");
            }

            // Generate sprites for each terrain type
            CreatePlaceholderSprite("Grass", new Color(0.4f, 0.8f, 0.3f), folderPath);
            CreatePlaceholderSprite("Dirt", new Color(0.6f, 0.4f, 0.2f), folderPath);
            CreatePlaceholderSprite("Sand", new Color(0.9f, 0.8f, 0.5f), folderPath);
            CreatePlaceholderSprite("Snow", new Color(0.95f, 0.95f, 1.0f), folderPath);
            CreatePlaceholderSprite("Swamp", new Color(0.3f, 0.4f, 0.3f), folderPath);
            CreatePlaceholderSprite("Rough", new Color(0.5f, 0.5f, 0.5f), folderPath);
            CreatePlaceholderSprite("Water", new Color(0.2f, 0.4f, 0.8f), folderPath);
            CreatePlaceholderSprite("Rock", new Color(0.4f, 0.4f, 0.4f), folderPath);
            CreatePlaceholderSprite("Lava", new Color(1.0f, 0.3f, 0.0f), folderPath);
            CreatePlaceholderSprite("Subterranean", new Color(0.2f, 0.2f, 0.25f), folderPath);

            AssetDatabase.Refresh();

            Debug.Log($"<color=green>✓ Generated placeholder terrain sprites in {folderPath}</color>");
        }

        private static void CreatePlaceholderSprite(string name, Color color, string folderPath)
        {
            var filePath = $"{folderPath}/{name}Tile.png";

            // Check if file already exists
            if (File.Exists(filePath))
            {
                Debug.Log($"  → Skipped {name} (already exists)");
                return;
            }

            // Create a simple 128x128 texture
            const int size = 128;
            var texture = new Texture2D(size, size);

            // Fill with base color
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Add slight variation for visual interest
                    var variation = Random.Range(-0.05f, 0.05f);
                    var pixelColor = new Color(
                        Mathf.Clamp01(color.r + variation),
                        Mathf.Clamp01(color.g + variation),
                        Mathf.Clamp01(color.b + variation),
                        1f
                    );

                    // Add border for clarity
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                    {
                        pixelColor = Color.Lerp(pixelColor, Color.black, 0.3f);
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();

            // Encode to PNG
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            // Clean up temporary texture
            Object.DestroyImmediate(texture);

            Debug.Log($"  ✓ Created {name} tile sprite");

            // Import and configure as sprite
            AssetDatabase.ImportAsset(filePath);
            ConfigureSpriteImportSettings(filePath);
        }

        private static void ConfigureSpriteImportSettings(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 128; // 1:1 mapping (128px = 1 world unit)
                importer.filterMode = FilterMode.Point; // Pixel-perfect for tile-based games
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;

                // Save and reimport
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
    }
}
