using UnityEngine;
using UnityEngine.Tilemaps;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Data
{
    /// <summary>
    /// ScriptableObject defining visual and gameplay properties of terrain types.
    /// Used by MapRenderer to assign correct tiles and calculate movement costs.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainData", menuName = "Realms of Eldor/Terrain Data")]
    public class TerrainData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The terrain type this data represents")]
        public TerrainType terrainType;

        [Tooltip("Display name for the terrain")]
        public string displayName;

        [Tooltip("Description of the terrain")]
        [TextArea(2, 4)]
        public string description;

        [Header("Gameplay Properties")]
        [Tooltip("Base movement cost to traverse this terrain (10 = easy, 20 = difficult)")]
        [Range(1, 50)]
        public int baseMovementCost = 10;

        [Tooltip("Can units move through this terrain?")]
        public bool isPassable = true;

        [Tooltip("Is this a water terrain type?")]
        public bool isWater = false;

        [Header("Visual Properties")]
        [Tooltip("Tile assets for this terrain (variants for visual variety)")]
        public TileBase[] tileVariants;

        [Tooltip("Minimap color for this terrain")]
        public Color minimapColor = Color.white;

        [Header("Audio")]
        [Tooltip("Sound played when moving onto this terrain")]
        public AudioClip movementSound;

        /// <summary>
        /// Gets a random tile variant for visual variety.
        /// </summary>
        public TileBase GetRandomTileVariant()
        {
            if (tileVariants == null || tileVariants.Length == 0)
                return null;

            if (tileVariants.Length == 1)
                return tileVariants[0];

            var index = Random.Range(0, tileVariants.Length);
            return tileVariants[index];
        }

        /// <summary>
        /// Gets a tile variant by index.
        /// </summary>
        public TileBase GetTileVariant(int index)
        {
            if (tileVariants == null || tileVariants.Length == 0)
                return null;

            index = Mathf.Clamp(index, 0, tileVariants.Length - 1);
            return tileVariants[index];
        }

        private void OnValidate()
        {
            // Ensure movement cost is max for impassable terrain
            if (!isPassable)
            {
                baseMovementCost = int.MaxValue;
            }

            // Auto-set display name from enum if empty
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = terrainType.ToString();
            }
        }
    }
}
