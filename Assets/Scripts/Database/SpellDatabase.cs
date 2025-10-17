using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Database
{
    /// <summary>
    /// Spell database singleton
    /// Provides lookup access to all spell definitions
    /// Based on VCMI's CSpellHandler pattern
    /// </summary>
    public class SpellDatabase : MonoBehaviour
    {
        public static SpellDatabase Instance { get; private set; }

        [Header("Spell Definitions")]
        [Tooltip("Assign all spell ScriptableObjects here")]
        [SerializeField] private List<SpellData> spells = new List<SpellData>();

        // Fast lookup dictionaries
        private Dictionary<int, SpellData> spellLookup;
        private Dictionary<SpellSchool, List<SpellData>> spellsBySchool;
        private Dictionary<int, List<SpellData>> spellsByLevel;

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
            spellLookup = new Dictionary<int, SpellData>();
            spellsBySchool = new Dictionary<SpellSchool, List<SpellData>>();
            spellsByLevel = new Dictionary<int, List<SpellData>>();

            foreach (var spell in spells)
            {
                if (spell == null)
                {
                    Debug.LogWarning("Null spell in database!");
                    continue;
                }

                // Add to main lookup
                if (spellLookup.ContainsKey(spell.spellId))
                {
                    Debug.LogWarning($"Duplicate spell ID: {spell.spellId} ({spell.spellName})");
                }
                else
                {
                    spellLookup[spell.spellId] = spell;
                }

                // Add to school lookup
                if (!spellsBySchool.ContainsKey(spell.school))
                {
                    spellsBySchool[spell.school] = new List<SpellData>();
                }
                spellsBySchool[spell.school].Add(spell);

                // Add to level lookup
                if (!spellsByLevel.ContainsKey(spell.level))
                {
                    spellsByLevel[spell.level] = new List<SpellData>();
                }
                spellsByLevel[spell.level].Add(spell);
            }

            Debug.Log($"SpellDatabase initialized with {spellLookup.Count} spells");
        }

        /// <summary>
        /// Get spell by ID
        /// </summary>
        public SpellData GetSpell(int spellId)
        {
            if (spellLookup.TryGetValue(spellId, out var spell))
            {
                return spell;
            }

            Debug.LogWarning($"Spell not found: {spellId}");
            return null;
        }

        /// <summary>
        /// Get all spells for a school
        /// </summary>
        public IEnumerable<SpellData> GetSpellsBySchool(SpellSchool school)
        {
            if (spellsBySchool.TryGetValue(school, out var list))
            {
                return list;
            }
            return Enumerable.Empty<SpellData>();
        }

        /// <summary>
        /// Get spells by level
        /// </summary>
        public IEnumerable<SpellData> GetSpellsByLevel(int level)
        {
            if (spellsByLevel.TryGetValue(level, out var list))
            {
                return list;
            }
            return Enumerable.Empty<SpellData>();
        }

        /// <summary>
        /// Get spells by school and level
        /// </summary>
        public IEnumerable<SpellData> GetSpells(SpellSchool school, int level)
        {
            return GetSpellsBySchool(school).Where(s => s.level == level);
        }

        /// <summary>
        /// Get all battle spells
        /// </summary>
        public IEnumerable<SpellData> GetBattleSpells()
        {
            return spells.Where(s => s != null && s.canCastInBattle);
        }

        /// <summary>
        /// Get all adventure map spells
        /// </summary>
        public IEnumerable<SpellData> GetAdventureSpells()
        {
            return spells.Where(s => s != null && s.canCastOnAdventureMap);
        }

        /// <summary>
        /// Get all spells
        /// </summary>
        public IEnumerable<SpellData> GetAllSpells()
        {
            return spells.Where(s => s != null);
        }

        /// <summary>
        /// Get random spell by level
        /// </summary>
        public SpellData GetRandomSpell(int level)
        {
            var levelSpells = GetSpellsByLevel(level).ToList();
            if (levelSpells.Count == 0)
                return null;

            return levelSpells[Random.Range(0, levelSpells.Count)];
        }

        /// <summary>
        /// Check if spell exists
        /// </summary>
        public bool HasSpell(int spellId)
        {
            return spellLookup.ContainsKey(spellId);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Add spell to database
        /// </summary>
        public void AddSpell(SpellData spell)
        {
            if (!spells.Contains(spell))
            {
                spells.Add(spell);
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
