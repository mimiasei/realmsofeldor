using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Database
{
    /// <summary>
    /// Hero type database singleton
    /// Provides lookup access to all hero type definitions
    /// Based on VCMI's CHeroHandler pattern
    /// </summary>
    public class HeroDatabase : MonoBehaviour
    {
        public static HeroDatabase Instance { get; private set; }

        [Header("Hero Type Definitions")]
        [Tooltip("Assign all hero type ScriptableObjects here")]
        [SerializeField] private List<HeroTypeData> heroTypes = new List<HeroTypeData>();

        // Fast lookup dictionaries
        private Dictionary<int, HeroTypeData> heroLookup;
        private Dictionary<HeroClass, List<HeroTypeData>> heroesByClass;
        private Dictionary<Faction, List<HeroTypeData>> heroesByFaction;

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDatabase();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Initialize lookup dictionaries
        /// </summary>
        private void InitializeDatabase()
        {
            heroLookup = new Dictionary<int, HeroTypeData>();
            heroesByClass = new Dictionary<HeroClass, List<HeroTypeData>>();
            heroesByFaction = new Dictionary<Faction, List<HeroTypeData>>();

            foreach (var heroType in heroTypes)
            {
                if (heroType == null)
                {
                    Debug.LogWarning("Null hero type in database!");
                    continue;
                }

                // Add to main lookup
                if (heroLookup.ContainsKey(heroType.heroTypeId))
                {
                    Debug.LogWarning($"Duplicate hero type ID: {heroType.heroTypeId} ({heroType.heroName})");
                }
                else
                {
                    heroLookup[heroType.heroTypeId] = heroType;
                }

                // Add to class lookup
                if (!heroesByClass.ContainsKey(heroType.heroClass))
                {
                    heroesByClass[heroType.heroClass] = new List<HeroTypeData>();
                }
                heroesByClass[heroType.heroClass].Add(heroType);

                // Add to faction lookup
                if (!heroesByFaction.ContainsKey(heroType.faction))
                {
                    heroesByFaction[heroType.faction] = new List<HeroTypeData>();
                }
                heroesByFaction[heroType.faction].Add(heroType);
            }

            Debug.Log($"HeroDatabase initialized with {heroLookup.Count} hero types");
        }

        /// <summary>
        /// Get hero type by ID
        /// </summary>
        public HeroTypeData GetHeroType(int heroTypeId)
        {
            if (heroLookup.TryGetValue(heroTypeId, out var heroType))
            {
                return heroType;
            }

            Debug.LogWarning($"Hero type not found: {heroTypeId}");
            return null;
        }

        /// <summary>
        /// Get all hero types for a class
        /// </summary>
        public IEnumerable<HeroTypeData> GetHeroesByClass(HeroClass heroClass)
        {
            if (heroesByClass.TryGetValue(heroClass, out var list))
            {
                return list;
            }
            return Enumerable.Empty<HeroTypeData>();
        }

        /// <summary>
        /// Get all hero types for a faction
        /// </summary>
        public IEnumerable<HeroTypeData> GetHeroesByFaction(Faction faction)
        {
            if (heroesByFaction.TryGetValue(faction, out var list))
            {
                return list;
            }
            return Enumerable.Empty<HeroTypeData>();
        }

        /// <summary>
        /// Get all hero types
        /// </summary>
        public IEnumerable<HeroTypeData> GetAllHeroTypes()
        {
            return heroTypes.Where(h => h != null);
        }

        /// <summary>
        /// Get random hero type for faction
        /// </summary>
        public HeroTypeData GetRandomHeroType(Faction faction)
        {
            var heroes = GetHeroesByFaction(faction).ToList();
            if (heroes.Count == 0)
                return null;

            return heroes[Random.Range(0, heroes.Count)];
        }

        /// <summary>
        /// Check if hero type exists
        /// </summary>
        public bool HasHeroType(int heroTypeId)
        {
            return heroLookup.ContainsKey(heroTypeId);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Add hero type to database
        /// </summary>
        public void AddHeroType(HeroTypeData heroType)
        {
            if (!heroTypes.Contains(heroType))
            {
                heroTypes.Add(heroType);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Editor helper: Refresh database
        /// </summary>
        [ContextMenu("Refresh Database")]
        public void RefreshDatabase()
        {
            InitializeDatabase();
        }
#endif
    }
}
