using UnityEngine;

namespace RealmsOfEldor.Data
{
    /// <summary>
    /// Configuration for Random Map Generation (RMG).
    /// Based on VCMI's randomMap.json config system.
    /// Controls budgets, density, and balance parameters.
    ///
    /// IMPORTANT: A default instance must exist at Assets/Resources/MapGenConfig.asset
    /// for the Singleton pattern to work. Access via MapGenConfig.Instance.
    /// </summary>
    [CreateAssetMenu(fileName = "MapGenConfig", menuName = "Game/Map Generation Config")]
    public class MapGenConfig : ScriptableObject
    {
        [Header("Treasure Budget")]
        [Tooltip("Total value budget for treasure objects (gold piles, resources)")]
        public int treasureBudget = 10000;

        [Tooltip("Maximum value for a single treasure object")]
        public int treasureValueLimit = 5000;

        [Header("Object Counts")]
        [Tooltip("Number of resource mines to place")]
        public int mineCount = 7;

        [Tooltip("Number of creature dwellings to place")]
        public int dwellingCount = 4;

        [Tooltip("Number of resource piles to place (uses treasure budget)")]
        public int resourcePileCount = 15;

        [Header("Resource Values")]
        [Tooltip("Gold value: 1 gold = 1 value")]
        public int goldValueMultiplier = 1;

        [Tooltip("Wood/Ore value per unit (VCMI uses 125)")]
        public int basicResourceValue = 125;

        [Tooltip("Rare resource value per unit (Mercury/Sulfur/Crystal/Gems, VCMI uses 500)")]
        public int rareResourceValue = 500;

        [Header("Mine Production")]
        [Tooltip("Extra resources limit for mines (VCMI: 10)")]
        public int mineExtraResourcesLimit = 10;

        [Header("Guard Placement")]
        [Tooltip("Minimum treasure value to spawn guards (VCMI: 2000)")]
        public int minGuardedValue = 2000;

        [Tooltip("Enable guard placement for high-value objects")]
        public bool enableGuards = true;

        [Header("Guard Strength Calculation (VCMI Formula)")]
        [Tooltip("Value threshold for first guard formula (VCMI uses 2500, 1500, 1000, 500, 0 based on difficulty)")]
        public int guardStrengthValue1 = 2500;

        [Tooltip("Value threshold for second guard formula (VCMI uses 7500)")]
        public int guardStrengthValue2 = 7500;

        [Tooltip("Multiplier for first guard formula (VCMI uses 0.5-1.5 based on difficulty)")]
        public float guardStrengthMultiplier1 = 1.0f;

        [Tooltip("Multiplier for second guard formula (VCMI uses 0.5-1.5 based on difficulty)")]
        public float guardStrengthMultiplier2 = 1.0f;

        [Header("Obstacle Placement")]
        [Tooltip("Enable obstacle placement (trees, rocks, decorations)")]
        public bool enableObstacles = true;

        [Tooltip("Target number of obstacles to place")]
        public int obstacleCount = 30;

        [Tooltip("Percentage of obstacles that should be blocking (0.0 = none, 1.0 = all)")]
        [Range(0f, 1f)]
        public float blockingObstacleRatio = 0.3f;

        [Header("Obstacle Type Distribution")]
        [Tooltip("Percentage chance for mountains/rocks (blocking)")]
        [Range(0f, 1f)]
        public float mountainRockChance = 0.3f;

        [Tooltip("Percentage chance for trees (mostly decorative)")]
        [Range(0f, 1f)]
        public float treeChance = 0.4f;

        [Tooltip("Percentage chance for bushes/flowers (decorative)")]
        [Range(0f, 1f)]
        public float bushFlowerChance = 0.3f;

        [Header("Reachability Validation")]
        [Tooltip("Enable reachability validation (ensures all objects are accessible)")]
        public bool validateReachability = true;

        [Header("Density Control (Future)")]
        [Tooltip("Target object density: objects per 100 tiles")]
        public float objectDensity = 5f;

        /// <summary>
        /// Determines if a treasure value qualifies for guards.
        /// Based on VCMI's isGuardNeededForTreasure() logic.
        /// </summary>
        public bool RequiresGuards(int treasureValue)
        {
            if (!enableGuards)
                return false;

            return treasureValue >= minGuardedValue;
        }

        /// <summary>
        /// Calculates guard strength using VCMI's formula.
        /// Based on ObjectManager::chooseGuard() in VCMI.
        /// Formula: strength = max(0, (value - threshold1) * multiplier1) + max(0, (value - threshold2) * multiplier2)
        /// </summary>
        /// <param name="treasureValue">Value of the treasure to guard</param>
        /// <returns>Total AI value of guards needed</returns>
        public int CalculateGuardStrength(int treasureValue)
        {
            if (!RequiresGuards(treasureValue))
                return 0;

            // VCMI formula (simplified for normal difficulty)
            // strength1 = max(0, (value - 2500) * 1.0)
            // strength2 = max(0, (value - 7500) * 1.0)
            // total = strength1 + strength2

            var strength1 = Mathf.Max(0f, (treasureValue - guardStrengthValue1) * guardStrengthMultiplier1);
            var strength2 = Mathf.Max(0f, (treasureValue - guardStrengthValue2) * guardStrengthMultiplier2);

            return Mathf.RoundToInt(strength1 + strength2);
        }

        /// <summary>
        /// Singleton accessor for default config.
        /// Loads from Resources/MapGenConfig.asset
        /// </summary>
        private static MapGenConfig _instance;
        public static MapGenConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MapGenConfig>("MapGenConfig");
                    if (_instance == null)
                    {
                        Debug.LogWarning("MapGenConfig not found in Resources folder. Using default values.");
                        _instance = CreateInstance<MapGenConfig>();
                    }
                }
                return _instance;
            }
        }
    }
}
