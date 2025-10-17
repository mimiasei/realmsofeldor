using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Database
{
    /// <summary>
    /// Creature database singleton
    /// Provides lookup access to all creature definitions
    /// Based on VCMI's CCreatureHandler pattern
    /// </summary>
    public class CreatureDatabase : MonoBehaviour
    {
        public static CreatureDatabase Instance { get; private set; }

        [Header("Creature Definitions")]
        [Tooltip("Assign all creature ScriptableObjects here")]
        [SerializeField] private List<CreatureData> creatures = new List<CreatureData>();

        // Fast lookup dictionary
        private Dictionary<int, CreatureData> creatureLookup;
        private Dictionary<Faction, List<CreatureData>> creaturesByFaction;

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
            creatureLookup = new Dictionary<int, CreatureData>();
            creaturesByFaction = new Dictionary<Faction, List<CreatureData>>();

            foreach (var creature in creatures)
            {
                if (creature == null)
                {
                    Debug.LogWarning("Null creature in database!");
                    continue;
                }

                // Add to main lookup
                if (creatureLookup.ContainsKey(creature.creatureId))
                {
                    Debug.LogWarning($"Duplicate creature ID: {creature.creatureId} ({creature.creatureName})");
                }
                else
                {
                    creatureLookup[creature.creatureId] = creature;
                }

                // Add to faction lookup
                if (!creaturesByFaction.ContainsKey(creature.faction))
                {
                    creaturesByFaction[creature.faction] = new List<CreatureData>();
                }
                creaturesByFaction[creature.faction].Add(creature);
            }

            Debug.Log($"CreatureDatabase initialized with {creatureLookup.Count} creatures");
        }

        /// <summary>
        /// Get creature by ID
        /// </summary>
        public CreatureData GetCreature(int creatureId)
        {
            if (creatureLookup.TryGetValue(creatureId, out var creature))
            {
                return creature;
            }

            Debug.LogWarning($"Creature not found: {creatureId}");
            return null;
        }

        /// <summary>
        /// Get all creatures for a faction
        /// </summary>
        public IEnumerable<CreatureData> GetCreaturesByFaction(Faction faction)
        {
            if (creaturesByFaction.TryGetValue(faction, out var list))
            {
                return list;
            }
            return Enumerable.Empty<CreatureData>();
        }

        /// <summary>
        /// Get creatures by tier
        /// </summary>
        public IEnumerable<CreatureData> GetCreaturesByTier(CreatureTier tier)
        {
            return creatures.Where(c => c != null && c.tier == tier);
        }

        /// <summary>
        /// Get creatures by faction and tier
        /// </summary>
        public IEnumerable<CreatureData> GetCreatures(Faction faction, CreatureTier tier)
        {
            return GetCreaturesByFaction(faction).Where(c => c.tier == tier);
        }

        /// <summary>
        /// Get all creatures
        /// </summary>
        public IEnumerable<CreatureData> GetAllCreatures()
        {
            return creatures.Where(c => c != null);
        }

        /// <summary>
        /// Check if creature exists
        /// </summary>
        public bool HasCreature(int creatureId)
        {
            return creatureLookup.ContainsKey(creatureId);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Add creature to database
        /// </summary>
        public void AddCreature(CreatureData creature)
        {
            if (!creatures.Contains(creature))
            {
                creatures.Add(creature);
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
