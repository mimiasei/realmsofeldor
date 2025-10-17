using System;
using System.Collections.Generic;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Represents a battle action (move, attack, wait, etc.).
    /// Based on VCMI's BattleAction.h
    /// </summary>
    [Serializable]
    public class BattleAction
    {
        // ===== Action Identity =====
        public int ActionId { get; set; }
        public BattleSide Side { get; set; }
        public int UnitId { get; set; }
        public ActionType Type { get; set; }

        // ===== Action Targets =====
        public BattleHex DestinationHex { get; set; }      // Where to move/attack
        public int TargetUnitId { get; set; } = -1;         // Target unit (for attacks/spells)
        public List<int> AdditionalTargets { get; set; }    // For multi-target actions

        // ===== Spell Actions =====
        public int SpellId { get; set; } = -1;              // If casting spell

        // ===== Movement Details =====
        public BattleHex AttackFromHex { get; set; }        // For melee attacks
        public bool ReturnAfterAttack { get; set; } = true; // Return to original position

        // ===== Constructor =====
        public BattleAction()
        {
            AdditionalTargets = new List<int>();
            DestinationHex = new BattleHex(BattleHex.INVALID);
            AttackFromHex = new BattleHex(BattleHex.INVALID);
        }

        // ===== Factory Methods (VCMI pattern) =====

        /// <summary>
        /// Create a DEFEND action (unit defends, gets defense bonus).
        /// </summary>
        public static BattleAction MakeDefend(BattleUnit unit)
        {
            return new BattleAction
            {
                Side = unit.Side,
                UnitId = unit.UnitId,
                Type = ActionType.DEFEND
            };
        }

        /// <summary>
        /// Create a WAIT action (unit delays action to later in round).
        /// </summary>
        public static BattleAction MakeWait(BattleUnit unit)
        {
            return new BattleAction
            {
                Side = unit.Side,
                UnitId = unit.UnitId,
                Type = ActionType.WAIT
            };
        }

        /// <summary>
        /// Create a WALK action (simple movement without attacking).
        /// </summary>
        public static BattleAction MakeWalk(BattleUnit unit, BattleHex destination)
        {
            return new BattleAction
            {
                Side = unit.Side,
                UnitId = unit.UnitId,
                Type = ActionType.WALK,
                DestinationHex = destination
            };
        }

        /// <summary>
        /// Create a WALK_AND_ATTACK action (move + melee attack).
        /// </summary>
        public static BattleAction MakeMeleeAttack(BattleUnit attacker, BattleUnit target,
            BattleHex attackFrom, bool returnAfterAttack = true)
        {
            return new BattleAction
            {
                Side = attacker.Side,
                UnitId = attacker.UnitId,
                Type = ActionType.WALK_AND_ATTACK,
                TargetUnitId = target.UnitId,
                DestinationHex = target.Position,
                AttackFromHex = attackFrom,
                ReturnAfterAttack = returnAfterAttack
            };
        }

        /// <summary>
        /// Create a SHOOT action (ranged attack).
        /// </summary>
        public static BattleAction MakeShoot(BattleUnit shooter, BattleUnit target)
        {
            return new BattleAction
            {
                Side = shooter.Side,
                UnitId = shooter.UnitId,
                Type = ActionType.SHOOT,
                TargetUnitId = target.UnitId,
                DestinationHex = target.Position
            };
        }

        /// <summary>
        /// Create a HERO_SPELL action (hero casts spell).
        /// </summary>
        public static BattleAction MakeSpellCast(BattleSide side, int spellId, BattleHex targetHex, int targetUnitId = -1)
        {
            return new BattleAction
            {
                Side = side,
                UnitId = -1,  // Hero, not a unit
                Type = ActionType.HERO_SPELL,
                SpellId = spellId,
                DestinationHex = targetHex,
                TargetUnitId = targetUnitId
            };
        }

        /// <summary>
        /// Create a RETREAT action (flee from battle).
        /// </summary>
        public static BattleAction MakeRetreat(BattleSide side)
        {
            return new BattleAction
            {
                Side = side,
                UnitId = -1,
                Type = ActionType.RETREAT
            };
        }

        /// <summary>
        /// Create a BAD_MORALE action (unit skips turn due to bad morale).
        /// </summary>
        public static BattleAction MakeBadMorale(BattleUnit unit)
        {
            return new BattleAction
            {
                Side = unit.Side,
                UnitId = unit.UnitId,
                Type = ActionType.BAD_MORALE
            };
        }

        // ===== Validation =====

        /// <summary>
        /// Check if this action is valid.
        /// </summary>
        public bool IsValid()
        {
            // All actions need a valid type
            if (Type == ActionType.NO_ACTION)
                return false;

            // Movement actions need a valid destination
            if (Type == ActionType.WALK || Type == ActionType.WALK_AND_ATTACK)
            {
                if (!DestinationHex.IsValid)
                    return false;
            }

            // Attack actions need a valid target
            if (Type == ActionType.WALK_AND_ATTACK || Type == ActionType.SHOOT)
            {
                if (TargetUnitId < 0)
                    return false;
            }

            // Spell actions need a spell ID
            if (Type == ActionType.HERO_SPELL || Type == ActionType.MONSTER_SPELL)
            {
                if (SpellId < 0)
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            return Type switch
            {
                ActionType.WALK => $"Walk to {DestinationHex}",
                ActionType.WALK_AND_ATTACK => $"Attack unit {TargetUnitId} from {AttackFromHex}",
                ActionType.SHOOT => $"Shoot at unit {TargetUnitId}",
                ActionType.WAIT => "Wait",
                ActionType.DEFEND => "Defend",
                ActionType.HERO_SPELL => $"Cast spell {SpellId} at {DestinationHex}",
                ActionType.RETREAT => "Retreat",
                ActionType.BAD_MORALE => "Bad Morale (skip turn)",
                _ => $"Action: {Type}"
            };
        }
    }

    /// <summary>
    /// Types of battle actions.
    /// Based on VCMI's EActionType enum.
    /// </summary>
    public enum ActionType
    {
        NO_ACTION = 0,

        // Battle Management
        END_TACTIC_PHASE,  // End tactics deployment phase
        RETREAT,           // Flee from battle
        SURRENDER,         // Surrender to opponent

        // Spell Casting
        HERO_SPELL,        // Hero casts spell
        MONSTER_SPELL,     // Creature casts spell (dragon breath, etc.)

        // Unit Actions
        WALK,              // Move without attacking
        WAIT,              // Delay action to later in round
        DEFEND,            // Defend (+defense bonus, skip turn)
        WALK_AND_ATTACK,   // Move and perform melee attack
        SHOOT,             // Ranged attack (uses ammo)

        // Automatic Actions
        BAD_MORALE,        // Unit skips turn due to bad morale
        STACK_HEAL,        // First aid tent healing

        // Siege (future)
        CATAPULT,          // Catapult attack on walls
    }
}
