using System.Collections.Generic;
using UnityEngine;

namespace RealmsOfEldor.Data
{
    /// <summary>
    /// Starting creature stack for hero
    /// </summary>
    [System.Serializable]
    public struct StartingStack
    {
        public CreatureData creature;
        public int minCount;
        public int maxCount;
    }

    /// <summary>
    /// Starting secondary skill for hero
    /// </summary>
    [System.Serializable]
    public struct StartingSkill
    {
        public SecondarySkillType skillType;
        public SkillLevel level;
    }

    /// <summary>
    /// Primary stat growth rates for hero class
    /// </summary>
    [System.Serializable]
    public struct PrimaryStatGrowth
    {
        [Tooltip("Chance to gain Attack on level up (0-100)")]
        [Range(0, 100)]
        public int attackChance;

        [Tooltip("Chance to gain Defense on level up (0-100)")]
        [Range(0, 100)]
        public int defenseChance;

        [Tooltip("Chance to gain Spell Power on level up (0-100)")]
        [Range(0, 100)]
        public int spellPowerChance;

        [Tooltip("Chance to gain Knowledge on level up (0-100)")]
        [Range(0, 100)]
        public int knowledgeChance;
    }

    /// <summary>
    /// Hero type definition ScriptableObject
    /// Based on VCMI's CHero static data
    /// </summary>
    [CreateAssetMenu(fileName = "HeroType", menuName = "Realms of Eldor/Hero Type")]
    public class HeroTypeData : ScriptableObject
    {
        [Header("Identity")]
        public int heroTypeId;
        public string heroName;
        public HeroClass heroClass;
        public Faction faction;

        [Header("Starting Primary Stats")]
        public int startAttack = 1;
        public int startDefense = 1;
        public int startSpellPower = 1;
        public int startKnowledge = 1;

        [Header("Primary Stat Growth")]
        [Tooltip("Probabilities for gaining each stat on level up (should sum to 100)")]
        public PrimaryStatGrowth statGrowth = new PrimaryStatGrowth
        {
            attackChance = 30,
            defenseChance = 30,
            spellPowerChance = 20,
            knowledgeChance = 20
        };

        [Header("Starting Army")]
        [Tooltip("Creatures the hero starts with")]
        public List<StartingStack> startingArmy = new List<StartingStack>();

        [Header("Starting Skills")]
        public List<StartingSkill> startingSkills = new List<StartingSkill>();

        [Header("Starting Spells")]
        public List<SpellData> startingSpells = new List<SpellData>();
        public bool startsWithSpellbook = false;

        [Header("Specialty")]
        public string specialtyName;
        [TextArea(3, 6)]
        public string specialtyDescription;

        [Header("Visuals")]
        public Sprite portrait;
        public Sprite smallPortrait;
        public Sprite mapSprite;
        public RuntimeAnimatorController mapAnimator;

        [Header("Audio")]
        public AudioClip selectionSound;

        /// <summary>
        /// Roll for primary stat increase on level up
        /// </summary>
        public PrimarySkill RollPrimaryStatIncrease()
        {
            int roll = Random.Range(0, 100);
            int cumulative = 0;

            cumulative += statGrowth.attackChance;
            if (roll < cumulative) return PrimarySkill.Attack;

            cumulative += statGrowth.defenseChance;
            if (roll < cumulative) return PrimarySkill.Defense;

            cumulative += statGrowth.spellPowerChance;
            if (roll < cumulative) return PrimarySkill.SpellPower;

            return PrimarySkill.Knowledge;
        }

        public override string ToString()
        {
            return $"{heroName} ({heroClass}, {faction})";
        }
    }
}
