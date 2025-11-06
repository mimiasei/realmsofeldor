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
        [Tooltip("Array of sprite variants for billboard rendering (3D mode)")]
        public Sprite[] spriteVariants;

        [Tooltip("Array of tile variants for tilemap rendering (legacy 2D mode) - deprecated")]
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

        // Get a random sprite variant (for 3D billboard rendering)
        public Sprite GetRandomSpriteVariant()
        {
            if (spriteVariants == null || spriteVariants.Length == 0)
                return null;

            return spriteVariants[Random.Range(0, spriteVariants.Length)];
        }

        // Get specific sprite variant by index (for 3D billboard rendering)
        public Sprite GetSpriteVariant(int index)
        {
            if (spriteVariants == null || spriteVariants.Length == 0)
                return null;

            if (index < 0 || index >= spriteVariants.Length)
                return spriteVariants[0];

            return spriteVariants[index];
        }

        // Get a random tile variant (for legacy 2D tilemap rendering)
        public TileBase GetRandomTileVariant()
        {
            if (tileVariants == null || tileVariants.Length == 0)
                return null;

            return tileVariants[Random.Range(0, tileVariants.Length)];
        }

        // Get specific tile variant by index (for legacy 2D tilemap rendering)
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
            // Auto-set passable based on biome type
            // All biomes except Water are passable
            isPassable = terrainType.Biome switch
            {
                BiomeType.Grass => true,
                BiomeType.Dirt => true,
                BiomeType.Rock => true,
                BiomeType.Sand => true,
                BiomeType.Snow => true,
                BiomeType.Swamp => true,
                BiomeType.Water => false,
                _ => true
            };

            // Auto-set water flag
            isWater = (terrainType.Biome == BiomeType.Water);

            // Auto-set movement costs for pathfinding testing
            // Different costs encourage pathfinder to find optimal routes
            movementCost = terrainType.Biome switch
            {
                BiomeType.Grass => 100,       // Fast (baseline)
                BiomeType.Dirt => 100,        // Fast (baseline)
                BiomeType.Rock => 110,        // Slightly slower
                BiomeType.Sand => 150,        // Medium slow
                BiomeType.Snow => 150,        // Medium slow
                BiomeType.Swamp => 175,       // Very slow (strong avoidance)
                BiomeType.Water => 999999,    // Impassable (high cost)
                _ => 100
            };
        }
    }
}
