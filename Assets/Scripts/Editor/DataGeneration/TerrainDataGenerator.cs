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

            // Generate data for each biome type with Temperate moisture level (default)
            // Note: TerrainType is now a struct with BiomeType + MoistureLevel
            CreateTerrainData(TerrainType.GrassTemperate, "GrassTemperate", folderPath, new Color(0.4f, 0.8f, 0.3f), 100);
            CreateTerrainData(TerrainType.DirtTemperate, "DirtTemperate", folderPath, new Color(0.6f, 0.4f, 0.2f), 100);
            CreateTerrainData(TerrainType.SandTemperate, "SandTemperate", folderPath, new Color(0.9f, 0.8f, 0.5f), 150);
            CreateTerrainData(TerrainType.SnowTemperate, "SnowTemperate", folderPath, new Color(0.95f, 0.95f, 1.0f), 150);
            CreateTerrainData(TerrainType.SwampTemperate, "SwampTemperate", folderPath, new Color(0.3f, 0.4f, 0.3f), 175);
            CreateTerrainData(TerrainType.RockTemperate, "RockTemperate", folderPath, new Color(0.5f, 0.5f, 0.5f), 110);
            CreateTerrainData(TerrainType.Water, "Water", folderPath, new Color(0.2f, 0.4f, 0.8f), 999999, false, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var biomeCount = System.Enum.GetValues(typeof(BiomeType)).Length;
            Debug.Log($"<color=green>✓ Generated TerrainData assets for {biomeCount} biome types (Temperate moisture) in {folderPath}</color>");
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
