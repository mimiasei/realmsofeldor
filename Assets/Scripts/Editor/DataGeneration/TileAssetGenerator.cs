using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor tool to convert sprite assets into Tile assets for use with Unity Tilemap.
    /// Also automatically links the tiles to TerrainData assets.
    /// Menu: Realms of Eldor/Generate/Create Tile Assets from Sprites
    /// </summary>
    public class TileAssetGenerator
    {
        [MenuItem("Realms of Eldor/Generate/Create Tile Assets from Sprites")]
        public static void GenerateTileAssets()
        {
            var spritesPath = "Assets/Sprites/Terrain";
            var tilesPath = "Assets/Data/Tiles";
            var terrainDataPath = "Assets/Data/Terrain";

            // Ensure tiles folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Data/Tiles"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Tiles");
            }

            // Array of terrain type names
            string[] terrainNames = new string[]
            {
                "Grass", "Dirt", "Sand", "Snow", "Swamp",
                "Rough", "Water", "Rock", "Lava", "Subterranean"
            };

            int created = 0;
            int linked = 0;

            foreach (var name in terrainNames)
            {
                // Load sprite
                var spritePath = $"{spritesPath}/{name}Tile.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                if (sprite == null)
                {
                    Debug.LogWarning($"Sprite not found: {spritePath}");
                    continue;
                }

                // Create tile asset path
                var tilePath = $"{tilesPath}/{name}Tile.asset";

                // Check if tile already exists
                var existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                Tile tile;

                if (existingTile != null)
                {
                    tile = existingTile;
                    Debug.Log($"  → Using existing tile: {name}Tile");
                }
                else
                {
                    // Create new tile
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.color = Color.white;

                    // Save tile asset
                    AssetDatabase.CreateAsset(tile, tilePath);
                    created++;
                    Debug.Log($"  ✓ Created tile: {name}Tile");
                }

                // Now link the tile to the TerrainData
                var terrainDataAssetPath = $"{terrainDataPath}/{name}TerrainData.asset";
                var terrainData = AssetDatabase.LoadAssetAtPath<Data.TerrainData>(terrainDataAssetPath);

                if (terrainData != null)
                {
                    // Use SerializedObject to modify the TerrainData
                    var serializedObject = new SerializedObject(terrainData);
                    var tileVariantsProperty = serializedObject.FindProperty("tileVariants");

                    // Set array size to 1 if empty
                    if (tileVariantsProperty.arraySize == 0)
                    {
                        tileVariantsProperty.arraySize = 1;
                    }

                    // Assign tile to first element
                    var element0 = tileVariantsProperty.GetArrayElementAtIndex(0);
                    element0.objectReferenceValue = tile;

                    // Apply changes
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(terrainData);

                    linked++;
                    Debug.Log($"  ✓ Linked {name}Tile to {name}TerrainData");
                }
                else
                {
                    Debug.LogWarning($"TerrainData not found: {terrainDataAssetPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>✓ Tile generation complete!</color>");
            Debug.Log($"  - Created {created} new tile assets");
            Debug.Log($"  - Linked {linked} tiles to TerrainData");
            Debug.Log($"  - Tiles saved to: {tilesPath}");
        }
    }
}
