using UnityEngine;
using UnityEngine.Tilemaps;

namespace RealmsOfEldor.Data
{
    /// <summary>
    /// ScriptableObject defining terrain type properties.
    /// Used by MapRenderer to visualize terrain.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainData", menuName = "Realms of Eldor/Terrain Data")]
    public class TerrainData : ScriptableObject
    {
        [Header("Terrain Identity")]
        public TerrainType terrainType;
        public string terrainName;

        [Header("Visual Properties")]
        [Tooltip("Array of tile variants for visual variety")]
        public TileBase[] tileVariants;

        [Tooltip("Minimap color for this terrain")]
        public Color minimapColor = Color.white;

        [Header("Gameplay Properties")]
        [Tooltip("Movement cost multiplier (100 = normal)")]
        public int movementCost = 100;

        [Tooltip("Can units walk on this terrain?")]
        public bool isPassable = true;

        [Tooltip("Is this a water terrain?")]
        public bool isWater = false;

        [Header("Audio")]
        [Tooltip("Sound when moving onto this terrain")]
        public AudioClip movementSound;

        // Get a random tile variant (for visual variety)
        public TileBase GetRandomTileVariant()
        {
            if (tileVariants == null || tileVariants.Length == 0)
                return null;

            return tileVariants[Random.Range(0, tileVariants.Length)];
        }

        // Get specific tile variant by index
        public TileBase GetTileVariant(int index)
        {
            if (tileVariants == null || tileVariants.Length == 0)
                return null;

            if (index < 0 || index >= tileVariants.Length)
                return tileVariants[0];

            return tileVariants[index];
        }

        private void OnValidate()
        {
            // Auto-set passable based on terrain type
            // Only Grass, Dirt, Sand, Rough, and Swamp are passable for pathfinding testing
            isPassable = terrainType switch
            {
                TerrainType.Grass => true,
                TerrainType.Subterranean => true,
                TerrainType.Dirt => true,
                TerrainType.Rock => true,
                TerrainType.Sand => true,
                TerrainType.Rough => true,
                TerrainType.Snow => true,
                TerrainType.Swamp => true,
                _ => false // Water, Lava are impassable
            };

            // Auto-set water flag
            isWater = (terrainType == TerrainType.Water);

            // Auto-set movement costs for pathfinding testing
            // Different costs encourage pathfinder to find optimal routes
            movementCost = terrainType switch
            {
                TerrainType.Grass => 100,       // Fast (baseline)
                TerrainType.Subterranean => 100,// Fast (baseline)
                TerrainType.Dirt => 100,        // Fast (baseline)
                TerrainType.Rock => 110,        // Slightly slower
                TerrainType.Rough => 125,       // Slow (encourages avoidance)
                TerrainType.Sand => 150,        // Medium slow
                TerrainType.Snow => 150,        // Medium slow
                TerrainType.Swamp => 175,       // Very slow (strong avoidance)
                TerrainType.Water => 999999,    // Impassable (high cost)
                TerrainType.Lava => 999999,     // Impassable
                _ => 100
            };
        }
    }
}
