using UnityEngine;
using UnityEditor;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor tool to generate TerrainData ScriptableObject assets for all terrain types.
    /// Menu: Realms of Eldor/Generate/Terrain Data Assets
    /// </summary>
    public class TerrainDataGenerator
    {
        [MenuItem("Realms of Eldor/Generate/Terrain Data Assets")]
        public static void GenerateTerrainData()
        {
            var folderPath = "Assets/Data/Terrain";

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Terrain");
            }

            // Generate data for each terrain type
            CreateTerrainData(TerrainType.Grass, "Grass", folderPath, new Color(0.4f, 0.8f, 0.3f), 100);
            CreateTerrainData(TerrainType.Dirt, "Dirt", folderPath, new Color(0.6f, 0.4f, 0.2f), 125);
            CreateTerrainData(TerrainType.Sand, "Sand", folderPath, new Color(0.9f, 0.8f, 0.5f), 150);
            CreateTerrainData(TerrainType.Snow, "Snow", folderPath, new Color(0.95f, 0.95f, 1.0f), 150);
            CreateTerrainData(TerrainType.Swamp, "Swamp", folderPath, new Color(0.3f, 0.4f, 0.3f), 175);
            CreateTerrainData(TerrainType.Rough, "Rough", folderPath, new Color(0.5f, 0.5f, 0.5f), 125);
            CreateTerrainData(TerrainType.Water, "Water", folderPath, new Color(0.2f, 0.4f, 0.8f), 0, false, true);
            CreateTerrainData(TerrainType.Rock, "Rock", folderPath, new Color(0.4f, 0.4f, 0.4f), 0, false, false);
            CreateTerrainData(TerrainType.Lava, "Lava", folderPath, new Color(1.0f, 0.3f, 0.0f), 0, false, false);
            CreateTerrainData(TerrainType.Subterranean, "Subterranean", folderPath, new Color(0.2f, 0.2f, 0.25f), 100);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>✓ Generated TerrainData assets for all {System.Enum.GetValues(typeof(TerrainType)).Length} terrain types in {folderPath}</color>");
        }

        private static void CreateTerrainData(
            TerrainType type,
            string name,
            string folderPath,
            Color minimapColor,
            int movementCost,
            bool passable = true,
            bool water = false)
        {
            var assetPath = $"{folderPath}/{name}TerrainData.asset";

            // Check if asset already exists
            var existingAsset = AssetDatabase.LoadAssetAtPath<Data.TerrainData>(assetPath);
            if (existingAsset != null)
            {
                Debug.Log($"  → Skipped {name} (already exists)");
                return;
            }

            // Create new asset
            var terrainData = ScriptableObject.CreateInstance<Data.TerrainData>();

            // Use reflection to set private fields
            var dataType = typeof(Data.TerrainData);

            dataType.GetField("terrainType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(terrainData, type);

            dataType.GetField("minimapColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(terrainData, minimapColor);

            dataType.GetField("movementCost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(terrainData, movementCost);

            dataType.GetField("isPassable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(terrainData, passable);

            dataType.GetField("isWater", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(terrainData, water);

            // Leave tileVariants empty for now (user can add later)

            // Create and save asset
            AssetDatabase.CreateAsset(terrainData, assetPath);
            Debug.Log($"  ✓ Created {name} terrain data");
        }
    }
}
