using System.Collections.Generic;
using UnityEngine;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Data
{
    /// <summary>
    /// Creature definition ScriptableObject
    /// Based on VCMI's CCreature static data
    /// </summary>
    [CreateAssetMenu(fileName = "Creature", menuName = "Realms of Eldor/Creature")]
    public class CreatureData : ScriptableObject
    {
        [Header("Identity")]
        public int creatureId;
        public string creatureName;
        public Faction faction;
        public CreatureTier tier;

        [Header("Combat Stats")]
        public int attack;
        public int defense;
        public int minDamage;
        public int maxDamage;
        public int hitPoints;
        public int speed;
        public int shots = 0; // 0 = melee, >0 = ranged

        [Header("Combat Properties")]
        public bool isFlying = false;
        public bool isDoubleAttack = false;
        public bool canShootInMelee = false;
        public bool noMeleeRetal = false; // No melee retaliation

        [Header("Special Abilities")]
        [Tooltip("Text descriptions of special abilities")]
        public List<string> abilities = new List<string>();

        [Header("Growth & Economy")]
        public int weeklyGrowth = 1;
        public ResourceCost cost;
        public int aiValue = 100; // AI strategic value

        [Header("Visuals")]
        public Sprite icon;
        public Sprite battleSprite;
        public RuntimeAnimatorController battleAnimator;

        [Header("Audio")]
        public AudioClip attackSound;
        public AudioClip hitSound;
        public AudioClip deathSound;

        /// <summary>
        /// Get average damage
        /// </summary>
        public float GetAverageDamage()
        {
            return (minDamage + maxDamage) / 2f;
        }

        /// <summary>
        /// Get total health pool for a stack
        /// </summary>
        public int GetTotalHealth(int count)
        {
            return hitPoints * count;
        }

        /// <summary>
        /// Calculate damage dealt by this creature to target
        /// Basic HOMM3 formula: base_damage * count * attack_defense_modifier
        /// </summary>
        public int CalculateDamage(int attackerCount, CreatureData target)
        {
            // Random damage in range
            float baseDamage = Random.Range(minDamage, maxDamage + 1);

            // Attack/Defense modifier
            int attackDiff = attack - target.defense;
            float modifier = 1.0f;

            if (attackDiff > 0)
            {
                // Each attack point over defense adds 5% damage
                modifier = 1.0f + (attackDiff * 0.05f);
            }
            else if (attackDiff < 0)
            {
                // Each defense point over attack reduces damage
                modifier = 1.0f / (1.0f + Mathf.Abs(attackDiff) * 0.025f);
            }

            int totalDamage = Mathf.RoundToInt(baseDamage * attackerCount * modifier);
            return Mathf.Max(1, totalDamage); // Minimum 1 damage
        }

        /// <summary>
        /// Check if creature is ranged
        /// </summary>
        public bool IsRanged()
        {
            return shots > 0;
        }

        public override string ToString()
        {
            return $"{creatureName} (Tier {(int)tier}, {faction})";
        }
    }
}
