using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Damage range (min-max).
    /// </summary>
    public struct DamageRange
    {
        public int Min;
        public int Max;

        public DamageRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public override string ToString() => $"{Min}-{Max}";
    }

    /// <summary>
    /// Damage estimation with kills.
    /// </summary>
    public struct DamageEstimation
    {
        public DamageRange Damage;
        public DamageRange Kills;

        public DamageEstimation(DamageRange damage, DamageRange kills)
        {
            Damage = damage;
            Kills = kills;
        }

        public override string ToString() => $"Damage: {Damage}, Kills: {Kills}";
    }

    /// <summary>
    /// Attack context for damage calculation.
    /// Based on VCMI's BattleAttackInfo.
    /// </summary>
    public class AttackInfo
    {
        public BattleUnit Attacker { get; set; }
        public BattleUnit Defender { get; set; }

        public BattleHex AttackerPos { get; set; }
        public BattleHex DefenderPos { get; set; }

        public int ChargeDistance { get; set; }
        public bool IsShooting { get; set; }
        public bool LuckyStrike { get; set; }
        public bool UnluckyStrike { get; set; }
        public bool DeathBlow { get; set; }
        public bool DoubleDamage { get; set; }

        public AttackInfo(BattleUnit attacker, BattleUnit defender, bool shooting = false, int chargeDistance = 0)
        {
            Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            Defender = defender ?? throw new ArgumentNullException(nameof(defender));
            IsShooting = shooting;
            ChargeDistance = chargeDistance;
            AttackerPos = attacker.Position;
            DefenderPos = defender.Position;
        }

        /// <summary>
        /// Create reverse attack info (for retaliation).
        /// </summary>
        public AttackInfo Reverse()
        {
            return new AttackInfo(Defender, Attacker, false, 0)
            {
                AttackerPos = DefenderPos,
                DefenderPos = AttackerPos
            };
        }
    }

    /// <summary>
    /// Damage calculator using VCMI's formula.
    /// Based on VCMI's DamageCalculator.cpp
    ///
    /// Formula:
    /// - Base damage = (creature min-max damage) × stack count
    /// - Attack factors (additive): attack skill, offense/archery, bless, luck, jousting, hate, etc.
    /// - Defense factors (multiplicative): defense skill, armorer, shield, range penalty, obstacles, etc.
    /// - Final damage = baseDamage × (1 + sum(attackFactors)) × product(1 - defenseFactors)
    /// </summary>
    public class DamageCalculator
    {
        private readonly AttackInfo info;

        // Game settings (VCMI defaults)
        private const double AttackMultiplier = 0.05;        // 5% per attack point
        private const double AttackMultiplierCap = 3.0;      // Max 300% (60 attack advantage)
        private const double DefenseMultiplier = 0.025;      // 2.5% per defense point
        private const double DefenseMultiplierCap = 0.7;     // Max 70% reduction (28 defense advantage)

        public DamageCalculator(AttackInfo attackInfo)
        {
            info = attackInfo ?? throw new ArgumentNullException(nameof(attackInfo));
        }

        // ===== Public API =====

        /// <summary>
        /// Calculate damage range and kills.
        /// Main entry point for damage calculation.
        /// </summary>
        public DamageEstimation CalculateDamageRange()
        {
            var damageBase = GetBaseDamageStack();

            var attackFactors = GetAttackFactors();
            var defenseFactors = GetDefenseFactors();

            // Attack factors are ADDITIVE (1.0 + sum of all factors)
            var attackFactorTotal = 1.0;
            foreach (var factor in attackFactors)
            {
                attackFactorTotal += factor;
            }

            // Defense factors are MULTIPLICATIVE (product of (1 - factor))
            var defenseFactorTotal = 1.0;
            foreach (var factor in defenseFactors)
            {
                defenseFactorTotal *= (1.0 - Math.Min(1.0, factor));
            }

            var resultingFactor = attackFactorTotal * defenseFactorTotal;

            var damageDealt = new DamageRange(
                Math.Max(1, (int)Math.Floor(damageBase.Min * resultingFactor)),
                Math.Max(1, (int)Math.Floor(damageBase.Max * resultingFactor))
            );

            var killsDealt = GetCasualties(damageDealt);

            return new DamageEstimation(damageDealt, killsDealt);
        }

        // ===== Base Damage =====

        /// <summary>
        /// Get base damage for a single creature.
        /// </summary>
        private DamageRange GetBaseDamageSingle()
        {
            var minDmg = info.Attacker.MinDamage;
            var maxDmg = info.Attacker.MaxDamage;

            if (minDmg > maxDmg)
            {
                // Swap if inverted (shouldn't happen, but VCMI does this)
                (minDmg, maxDmg) = (maxDmg, minDmg);
            }

            return new DamageRange(minDmg, maxDmg);
        }

        /// <summary>
        /// Get base damage with bless/curse applied.
        /// Bless: always max damage
        /// Curse: always min damage
        /// </summary>
        private DamageRange GetBaseDamageBlessCurse()
        {
            var baseDamage = GetBaseDamageSingle();

            // TODO: Check for bless/curse status effects
            // For now, return unmodified damage
            return baseDamage;
        }

        /// <summary>
        /// Get base damage for entire stack.
        /// </summary>
        private DamageRange GetBaseDamageStack()
        {
            var stackSize = info.Attacker.Count;
            var baseDamage = GetBaseDamageBlessCurse();

            return new DamageRange(
                baseDamage.Min * stackSize,
                baseDamage.Max * stackSize
            );
        }

        // ===== Attack & Defense Stats =====

        private int GetActorAttackBase()
        {
            return info.Attacker.Attack;
        }

        private int GetActorAttackEffective()
        {
            return GetActorAttackBase();
        }

        private int GetTargetDefenseBase()
        {
            return info.Defender.Defense;
        }

        private int GetTargetDefenseEffective()
        {
            return GetTargetDefenseBase();
        }

        // ===== Attack Factors (Additive Bonuses) =====

        /// <summary>
        /// Attack skill factor: 5% per attack point advantage, capped at 300%.
        /// </summary>
        private double GetAttackSkillFactor()
        {
            var attackAdvantage = GetActorAttackEffective() - GetTargetDefenseEffective();

            if (attackAdvantage > 0)
            {
                var attackFactor = AttackMultiplier * attackAdvantage;
                return Math.Min(attackFactor, AttackMultiplierCap);
            }
            return 0.0;
        }

        /// <summary>
        /// Offense/Archery skill factor.
        /// For melee: Offense skill bonus
        /// For ranged: Archery skill bonus
        /// </summary>
        private double GetAttackOffenseArcheryFactor()
        {
            // TODO: Implement when hero skills are added
            // For now, return 0
            return 0.0;
        }

        /// <summary>
        /// General damage bonuses (e.g., morale bonuses, artifacts).
        /// </summary>
        private double GetAttackBlessFactor()
        {
            // TODO: Implement when status effects/artifacts are added
            return 0.0;
        }

        /// <summary>
        /// Luck factor: +100% damage on lucky strike.
        /// </summary>
        private double GetAttackLuckFactor()
        {
            if (info.LuckyStrike)
                return 1.0;  // +100% damage
            return 0.0;
        }

        /// <summary>
        /// Jousting bonus (Champions): +5% per hex charged.
        /// </summary>
        private double GetAttackJoustingFactor()
        {
            // TODO: Implement when jousting creatures are added
            // if (info.ChargeDistance > 0 && info.Attacker has jousting ability)
            //     return info.ChargeDistance * 0.05;
            return 0.0;
        }

        /// <summary>
        /// Attack from back bonus (vulnerable from behind).
        /// </summary>
        private double GetAttackFromBackFactor()
        {
            // TODO: Implement when positioning and facing are added
            return 0.0;
        }

        /// <summary>
        /// Death blow bonus (undead vs living).
        /// </summary>
        private double GetAttackDeathBlowFactor()
        {
            if (info.DeathBlow)
                return 1.0;  // +100% damage
            return 0.0;
        }

        /// <summary>
        /// Double damage bonus (creature specialties).
        /// </summary>
        private double GetAttackDoubleDamageFactor()
        {
            if (info.DoubleDamage)
                return 1.0;  // +100% damage
            return 0.0;
        }

        /// <summary>
        /// Hate bonus (creature vs specific enemy type).
        /// </summary>
        private double GetAttackHateFactor()
        {
            // TODO: Implement when creature hate abilities are added
            return 0.0;
        }

        private List<double> GetAttackFactors()
        {
            return new List<double>
            {
                GetAttackSkillFactor(),
                GetAttackOffenseArcheryFactor(),
                GetAttackBlessFactor(),
                GetAttackLuckFactor(),
                GetAttackJoustingFactor(),
                GetAttackFromBackFactor(),
                GetAttackDeathBlowFactor(),
                GetAttackDoubleDamageFactor(),
                GetAttackHateFactor()
            };
        }

        // ===== Defense Factors (Multiplicative Reductions) =====

        /// <summary>
        /// Defense skill factor: 2.5% per defense point advantage, capped at 70%.
        /// </summary>
        private double GetDefenseSkillFactor()
        {
            var defenseAdvantage = GetTargetDefenseEffective() - GetActorAttackEffective();

            if (defenseAdvantage > 0)
            {
                var defenseFactor = DefenseMultiplier * defenseAdvantage;
                return Math.Min(defenseFactor, DefenseMultiplierCap);
            }
            return 0.0;
        }

        /// <summary>
        /// Armorer skill factor (general damage reduction).
        /// </summary>
        private double GetDefenseArmorerFactor()
        {
            // TODO: Implement when hero skills are added
            return 0.0;
        }

        /// <summary>
        /// Magic shield factor (Shield/Air Shield spells).
        /// </summary>
        private double GetDefenseMagicShieldFactor()
        {
            // TODO: Implement when spells are added
            return 0.0;
        }

        /// <summary>
        /// Range penalty: -50% damage at long range or for ranged units in melee.
        /// </summary>
        private double GetDefenseRangePenaltiesFactor()
        {
            if (info.IsShooting)
            {
                // TODO: Check for distance penalty (beyond shooting range)
                // For now, no penalty
                return 0.0;
            }
            else
            {
                // Ranged unit in melee combat (no melee penalty ability)
                if (info.Attacker.IsRanged && !info.Attacker.CanShootInMelee)
                    return 0.5;  // -50% damage
            }
            return 0.0;
        }

        /// <summary>
        /// Obstacle penalty: -50% damage when shooting through walls/obstacles.
        /// </summary>
        private double GetDefenseObstacleFactor()
        {
            // TODO: Implement when obstacles are added
            return 0.0;
        }

        /// <summary>
        /// Unlucky strike: -50% damage.
        /// </summary>
        private double GetDefenseUnluckyFactor()
        {
            if (info.UnluckyStrike)
                return 0.5;  // -50% damage
            return 0.0;
        }

        /// <summary>
        /// Defending bonus: Already applied in BattleUnit.Defense getter (+50% defense).
        /// No additional factor needed here.
        /// </summary>
        private double GetDefenseDefendingFactor()
        {
            // Defense bonus already applied in GetTargetDefenseEffective()
            return 0.0;
        }

        private List<double> GetDefenseFactors()
        {
            return new List<double>
            {
                GetDefenseSkillFactor(),
                GetDefenseArmorerFactor(),
                GetDefenseMagicShieldFactor(),
                GetDefenseRangePenaltiesFactor(),
                GetDefenseObstacleFactor(),
                GetDefenseUnluckyFactor()
            };
        }

        // ===== Casualties Calculation =====

        /// <summary>
        /// Calculate casualties (kills) from damage range.
        /// </summary>
        private DamageRange GetCasualties(DamageRange damageDealt)
        {
            return new DamageRange(
                GetCasualties(damageDealt.Min),
                GetCasualties(damageDealt.Max)
            );
        }

        /// <summary>
        /// Calculate casualties (kills) from specific damage amount.
        /// VCMI formula: First kill the partially damaged creature, then full health creatures.
        /// </summary>
        private int GetCasualties(int damageDealt)
        {
            if (damageDealt < info.Defender.FirstUnitHP)
                return 0;  // Not enough damage to kill even first creature

            var damageLeft = damageDealt - info.Defender.FirstUnitHP;
            var killsLeft = damageLeft / info.Defender.MaxHealth;

            return Math.Min(1 + killsLeft, info.Defender.Count);
        }
    }
}
