using System;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Evaluates a potential attack option.
    /// Based on VCMI's AttackPossibility class.
    /// Phase 5E: Battle AI implementation.
    /// </summary>
    public class AttackPossibility
    {
        // Attack details
        public BattleUnit Attacker { get; set; }
        public BattleUnit Defender { get; set; }
        public BattleHex FromHex { get; set; }
        public bool IsShooting { get; set; }

        // Damage estimates
        public int DamageToDefender { get; set; }
        public int RetaliationDamage { get; set; }
        public bool DefenderKilled { get; set; }
        public bool AttackerKilled { get; set; }  // Killed by retaliation

        // Scoring
        public float Score { get; set; }

        /// <summary>
        /// Calculate attack value (damage dealt - damage taken).
        /// Based on VCMI's attackValue() formula.
        /// </summary>
        public float CalculateValue()
        {
            var value = DamageToDefender - RetaliationDamage;

            // Bonus for killing defender
            if (DefenderKilled)
                value += 100;

            // Penalty for getting killed by retaliation
            if (AttackerKilled)
                value -= 1000;

            return value;
        }

        /// <summary>
        /// Evaluate an attack possibility.
        /// Simulates the attack and calculates expected outcomes.
        /// </summary>
        public static AttackPossibility Evaluate(
            BattleUnit attacker,
            BattleUnit defender,
            BattleHex fromHex,
            bool isShooting,
            BattleState battleState)
        {
            if (attacker == null || defender == null)
                return null;

            var possibility = new AttackPossibility
            {
                Attacker = attacker,
                Defender = defender,
                FromHex = fromHex,
                IsShooting = isShooting
            };

            // Calculate distance for charge bonus
            var chargeDistance = fromHex.IsValid
                ? BattleHex.GetDistance(fromHex, defender.Position)
                : 0;

            // Create attack info
            var attackInfo = new AttackInfo(attacker, defender, isShooting, chargeDistance);

            // Calculate damage to defender
            var calculator = new DamageCalculator(attackInfo);
            var damageRange = calculator.CalculateDamageRange();

            // Use average damage for AI evaluation
            possibility.DamageToDefender = (damageRange.Damage.Min + damageRange.Damage.Max) / 2;

            // Check if defender would be killed
            possibility.DefenderKilled = possibility.DamageToDefender >= defender.TotalHealth;

            // Calculate retaliation damage (if melee and defender survives)
            if (!isShooting && !possibility.DefenderKilled && defender.CanRetaliate && !attacker.NoMeleeRetaliation)
            {
                var retalInfo = new AttackInfo(defender, attacker, shooting: false, chargeDistance: 0);
                var retalCalculator = new DamageCalculator(retalInfo);
                var retalDamage = retalCalculator.CalculateDamageRange();
                possibility.RetaliationDamage = (retalDamage.Damage.Min + retalDamage.Damage.Max) / 2;

                // Check if attacker would be killed by retaliation
                possibility.AttackerKilled = possibility.RetaliationDamage >= attacker.TotalHealth;
            }
            else
            {
                possibility.RetaliationDamage = 0;
                possibility.AttackerKilled = false;
            }

            // Calculate final score
            possibility.Score = possibility.CalculateValue();

            return possibility;
        }

        public override string ToString()
        {
            var action = IsShooting ? "Shoot" : "Attack";
            return $"{action} {Defender.CreatureType.creatureName}: DMG={DamageToDefender}, RETAL={RetaliationDamage}, Score={Score:F0}";
        }
    }
}
