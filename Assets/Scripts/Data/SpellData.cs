using System.Collections.Generic;
using UnityEngine;


namespace RealmsOfEldor.Data
{
    /// <summary>
    /// Spell target type
    /// </summary>
    public enum SpellTarget
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        SingleAny,
        Battlefield,
        Self
    }

    /// <summary>
    /// Spell effect type
    /// </summary>
    public enum SpellEffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Summon,
        Resurrection,
        Teleport,
        Special
    }

    /// <summary>
    /// Individual spell effect component
    /// </summary>
    [System.Serializable]
    public struct SpellEffect
    {
        public SpellEffectType effectType;
        public int basePower;
        [Tooltip("How much spell power affects this effect")]
        public float spellPowerScaling;
        public int duration; // In rounds, 0 = instant
    }

    /// <summary>
    /// Spell definition ScriptableObject
    /// Based on VCMI's CSpell
    /// </summary>
    [CreateAssetMenu(fileName = "Spell", menuName = "Realms of Eldor/Spell")]
    public class SpellData : ScriptableObject
    {
        [Header("Identity")]
        public int spellId;
        public string spellName;
        public SpellSchool school;
        [Range(1, 5)]
        public int level = 1;

        [Header("Casting")]
        public int manaCost;
        public SpellTarget targetType;
        public bool canCastInBattle = true;
        public bool canCastOnAdventureMap = false;

        [Header("Effects")]
        public List<SpellEffect> effects = new List<SpellEffect>();

        [Header("Description")]
        [TextArea(3, 6)]
        public string description;

        [Header("Visuals")]
        public Sprite icon;
        public GameObject effectPrefab;
        public Color effectColor = Color.white;

        [Header("Audio")]
        public AudioClip castSound;

        /// <summary>
        /// Calculate damage for damage spell
        /// </summary>
        public int CalculateDamage(int casterSpellPower)
        {
            int totalDamage = 0;

            foreach (var effect in effects)
            {
                if (effect.effectType == SpellEffectType.Damage)
                {
                    totalDamage += effect.basePower +
                                  Mathf.RoundToInt(casterSpellPower * effect.spellPowerScaling);
                }
            }

            return totalDamage;
        }

        /// <summary>
        /// Calculate healing for heal spell
        /// </summary>
        public int CalculateHealing(int casterSpellPower)
        {
            int totalHealing = 0;

            foreach (var effect in effects)
            {
                if (effect.effectType == SpellEffectType.Heal ||
                    effect.effectType == SpellEffectType.Resurrection)
                {
                    totalHealing += effect.basePower +
                                   Mathf.RoundToInt(casterSpellPower * effect.spellPowerScaling);
                }
            }

            return totalHealing;
        }

        /// <summary>
        /// Get spell school bonus requirement
        /// </summary>
        public SecondarySkillType GetRequiredMagicSkill()
        {
            return school switch
            {
                SpellSchool.Air => SecondarySkillType.AirMagic,
                SpellSchool.Earth => SecondarySkillType.EarthMagic,
                SpellSchool.Fire => SecondarySkillType.FireMagic,
                SpellSchool.Water => SecondarySkillType.WaterMagic,
                _ => SecondarySkillType.Wisdom
            };
        }

        /// <summary>
        /// Check if hero can cast this spell
        /// </summary>
        public bool CanCast(Hero hero, bool inBattle)
        {
            // Check spellbook
            if (!hero.HasSpellbook)
                return false;

            // Check if known
            if (!hero.KnowsSpell(spellId))
                return false;

            // Check mana
            if (hero.Mana < manaCost)
                return false;

            // Check context
            if (inBattle && !canCastInBattle)
                return false;

            if (!inBattle && !canCastOnAdventureMap)
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"{spellName} (Lvl {level} {school})";
        }
    }
}
