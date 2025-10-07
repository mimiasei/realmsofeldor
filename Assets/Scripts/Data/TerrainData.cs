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
            if (terrainType == TerrainType.Rock)
                isPassable = false;

            // Auto-set water flag
            isWater = (terrainType == TerrainType.Water);

            movementCost = terrainType switch
            {
                // Auto-set movement cost for common terrain types
                TerrainType.Grass or TerrainType.Dirt => 100,
                TerrainType.Sand or TerrainType.Snow => 150,
                TerrainType.Swamp => 175,
                TerrainType.Lava => 200,
                _ => movementCost
            };
        }
    }
}
