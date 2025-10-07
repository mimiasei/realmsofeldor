using System;
using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Main battle state container.
    /// Based on VCMI's BattleInfo class.
    /// Holds all battle data: units, obstacles, round counter, etc.
    /// </summary>
    [Serializable]
    public class BattleState
    {
        // ===== Battle Identity =====
        public int BattleId { get; set; }
        public string BattleName { get; set; }

        // ===== Battle Sides =====
        public BattleSideInfo Attacker { get; private set; }
        public BattleSideInfo Defender { get; private set; }

        // ===== Units =====
        private readonly Dictionary<int, BattleUnit> units = new();
        private int nextUnitId = 0;

        // ===== Battle Flow =====
        public int CurrentRound { get; private set; } = 0;
        public int ActiveUnitId { get; set; } = -1;
        public BattlePhase CurrentPhase { get; set; } = BattlePhase.NOT_STARTED;

        // ===== Turn Queue =====
        private readonly TurnQueue turnQueue = new();

        // ===== Battlefield =====
        public BattleFieldType FieldType { get; set; } = BattleFieldType.GRASS;
        private readonly Dictionary<BattleHex, BattleObstacle> obstacles = new();

        // ===== Battle Results =====
        public bool IsFinished { get; private set; } = false;
        public BattleSide? WinningSide { get; private set; } = null;

        // ===== Constructor =====
        public BattleState(Hero attackingHero, Hero defendingHero)
        {
            Attacker = new BattleSideInfo(BattleSide.Attacker, attackingHero);
            Defender = new BattleSideInfo(BattleSide.Defender, defendingHero);
        }

        // ===== Unit Management =====

        /// <summary>
        /// Add a unit to battle from an army slot.
        /// </summary>
        public BattleUnit AddUnit(CreatureData creatureType, int count, BattleSide side, int slotIndex, BattleHex position)
        {
            if (count <= 0) return null;

            var unitId = nextUnitId++;
            var unit = new BattleUnit(
                unitId,
                creatureType,
                count,
                side,
                slotIndex,
                position
            );

            units[unitId] = unit;
            return unit;
        }

        /// <summary>
        /// Get unit by ID.
        /// </summary>
        public BattleUnit GetUnit(int unitId)
        {
            return units.TryGetValue(unitId, out var unit) ? unit : null;
        }

        /// <summary>
        /// Get all units.
        /// </summary>
        public IEnumerable<BattleUnit> GetAllUnits()
        {
            return units.Values;
        }

        /// <summary>
        /// Get alive units for a specific side.
        /// </summary>
        public IEnumerable<BattleUnit> GetUnitsForSide(BattleSide side)
        {
            return units.Values.Where(u => u.Side == side && u.IsAlive);
        }

        /// <summary>
        /// Get unit at specific hex position.
        /// </summary>
        public BattleUnit GetUnitAtPosition(BattleHex hex)
        {
            return units.Values.FirstOrDefault(u => u.IsAlive && u.Position == hex);
        }

        /// <summary>
        /// Remove dead units from battle.
        /// </summary>
        public void RemoveDeadUnits()
        {
            var deadUnits = units.Where(kv => !kv.Value.IsAlive).Select(kv => kv.Key).ToList();
            foreach (var unitId in deadUnits)
            {
                units.Remove(unitId);
            }
        }

        // ===== Round & Turn Management =====

        /// <summary>
        /// Start a new battle round.
        /// Builds turn queue based on initiative.
        /// </summary>
        public void StartNewRound()
        {
            CurrentRound++;

            // Reset all units for new round
            foreach (var unit in units.Values.Where(u => u.IsAlive))
            {
                unit.StartTurn();
                unit.UpdateStatusEffects();
            }

            // Build turn queue
            turnQueue.BuildQueue(units.Values, CurrentRound);
        }

        /// <summary>
        /// Get next unit to act.
        /// Automatically advances to next unit in turn queue.
        /// Returns null if no units left (round end).
        /// </summary>
        public BattleUnit GetNextUnit()
        {
            var nextUnitId = turnQueue.GetNextUnit();
            if (nextUnitId == -1)
                return null;

            SetActiveUnit(nextUnitId);
            return GetActiveUnit();
        }

        /// <summary>
        /// Peek at next unit without advancing queue.
        /// </summary>
        public BattleUnit PeekNextUnit()
        {
            var nextUnitId = turnQueue.PeekNextUnit();
            return nextUnitId != -1 ? GetUnit(nextUnitId) : null;
        }

        /// <summary>
        /// Set the currently active unit.
        /// </summary>
        public void SetActiveUnit(int unitId)
        {
            ActiveUnitId = unitId;
            var unit = GetUnit(unitId);
            unit?.StartTurn();
        }

        /// <summary>
        /// Get currently active unit.
        /// </summary>
        public BattleUnit GetActiveUnit()
        {
            return GetUnit(ActiveUnitId);
        }

        /// <summary>
        /// Check if turn queue has remaining units.
        /// </summary>
        public bool HasRemainingTurns()
        {
            return !turnQueue.IsEmpty();
        }

        /// <summary>
        /// Get turn order for UI display.
        /// </summary>
        public List<int> GetTurnOrder()
        {
            return turnQueue.GetTurnOrder();
        }

        /// <summary>
        /// Move current unit to wait phase.
        /// Unit will act later in round.
        /// </summary>
        public void WaitCurrentUnit()
        {
            var unit = GetActiveUnit();
            if (unit != null)
            {
                turnQueue.MoveToWaitPhase(unit.UnitId, unit);
            }
        }

        // ===== Obstacle Management =====

        /// <summary>
        /// Add an obstacle to the battlefield.
        /// </summary>
        public void AddObstacle(BattleHex hex, BattleObstacle obstacle)
        {
            if (hex.IsValid && obstacle != null)
            {
                obstacles[hex] = obstacle;
            }
        }

        /// <summary>
        /// Get obstacle at hex.
        /// </summary>
        public BattleObstacle GetObstacle(BattleHex hex)
        {
            return obstacles.TryGetValue(hex, out var obstacle) ? obstacle : null;
        }

        /// <summary>
        /// Check if hex is blocked by obstacle.
        /// </summary>
        public bool IsHexBlocked(BattleHex hex)
        {
            return obstacles.ContainsKey(hex);
        }

        // ===== Battle End Conditions =====

        /// <summary>
        /// Check if battle is over (one side has no units).
        /// </summary>
        public bool CheckBattleEnd()
        {
            var attackerHasUnits = units.Values.Any(u => u.Side == BattleSide.Attacker && u.IsAlive);
            var defenderHasUnits = units.Values.Any(u => u.Side == BattleSide.Defender && u.IsAlive);

            if (!attackerHasUnits && !defenderHasUnits)
            {
                // Draw (both sides eliminated simultaneously)
                EndBattle(null);
                return true;
            }

            if (!attackerHasUnits)
            {
                EndBattle(BattleSide.Defender);
                return true;
            }

            if (!defenderHasUnits)
            {
                EndBattle(BattleSide.Attacker);
                return true;
            }

            return false;
        }

        /// <summary>
        /// End the battle with a winner.
        /// </summary>
        public void EndBattle(BattleSide? winningSide)
        {
            IsFinished = true;
            WinningSide = winningSide;
            CurrentPhase = BattlePhase.ENDED;
        }

        // ===== Helper Methods =====

        /// <summary>
        /// Check if hex is occupied by a unit.
        /// </summary>
        public bool IsHexOccupied(BattleHex hex)
        {
            return GetUnitAtPosition(hex) != null;
        }

        /// <summary>
        /// Check if hex is accessible (not blocked by unit/obstacle).
        /// </summary>
        public bool IsHexAccessible(BattleHex hex)
        {
            return hex.IsAvailable && !IsHexOccupied(hex) && !IsHexBlocked(hex);
        }

        /// <summary>
        /// Get summary of battle state.
        /// </summary>
        public string GetBattleSummary()
        {
            var attackerUnits = GetUnitsForSide(BattleSide.Attacker).Count();
            var defenderUnits = GetUnitsForSide(BattleSide.Defender).Count();

            return $"Battle Round {CurrentRound} - Attacker: {attackerUnits} units, Defender: {defenderUnits} units";
        }

        // ===== Combat Actions =====

        /// <summary>
        /// Execute a melee attack action.
        /// Based on VCMI's attack flow.
        /// </summary>
        public AttackResult ExecuteAttack(BattleUnit attacker, BattleUnit defender, int chargeDistance = 0)
        {
            if (attacker == null || defender == null || !attacker.IsAlive || !defender.IsAlive)
                return null;

            // Create attack info
            var attackInfo = new AttackInfo(attacker, defender, shooting: false, chargeDistance);

            // Calculate damage
            var calculator = new DamageCalculator(attackInfo);
            var damageEstimation = calculator.CalculateDamageRange();

            // Roll actual damage (random between min and max)
            var random = new Random();
            var actualDamage = random.Next(damageEstimation.Damage.Min, damageEstimation.Damage.Max + 1);

            // Apply damage
            defender.TakeDamage(actualDamage);

            // Create result
            var result = new AttackResult
            {
                Attacker = attacker,
                Defender = defender,
                DamageDealt = actualDamage,
                KillsDealt = GetKillsFromDamage(defender, actualDamage),
                IsKilled = !defender.IsAlive,
                IsShooting = false
            };

            // Handle retaliation (if defender survives and can retaliate)
            if (defender.IsAlive && defender.CanRetaliate && !attacker.NoMeleeRetaliation)
            {
                result.Retaliation = ExecuteRetaliation(defender, attacker);
            }

            // Mark attacker as acted
            attacker.EndTurn();

            return result;
        }

        /// <summary>
        /// Execute a ranged attack action.
        /// </summary>
        public AttackResult ExecuteShoot(BattleUnit attacker, BattleUnit defender)
        {
            if (attacker == null || defender == null || !attacker.IsAlive || !defender.IsAlive)
                return null;

            if (!attacker.CanShoot)
                return null;

            // Create attack info
            var attackInfo = new AttackInfo(attacker, defender, shooting: true);

            // Calculate damage
            var calculator = new DamageCalculator(attackInfo);
            var damageEstimation = calculator.CalculateDamageRange();

            // Roll actual damage
            var random = new Random();
            var actualDamage = random.Next(damageEstimation.Damage.Min, damageEstimation.Damage.Max + 1);

            // Apply damage
            defender.TakeDamage(actualDamage);

            // Use one shot
            attacker.UseShot();

            // Create result (no retaliation for ranged attacks)
            var result = new AttackResult
            {
                Attacker = attacker,
                Defender = defender,
                DamageDealt = actualDamage,
                KillsDealt = GetKillsFromDamage(defender, actualDamage),
                IsKilled = !defender.IsAlive,
                IsShooting = true,
                Retaliation = null
            };

            // Mark attacker as acted
            attacker.EndTurn();

            return result;
        }

        /// <summary>
        /// Execute retaliation attack.
        /// Based on VCMI's retaliation flow.
        /// </summary>
        private AttackResult ExecuteRetaliation(BattleUnit retaliator, BattleUnit target)
        {
            if (retaliator == null || target == null || !retaliator.IsAlive || !target.IsAlive)
                return null;

            if (!retaliator.CanRetaliate)
                return null;

            // Create attack info (retaliation is always melee, no charge)
            var attackInfo = new AttackInfo(retaliator, target, shooting: false, chargeDistance: 0);

            // Calculate damage
            var calculator = new DamageCalculator(attackInfo);
            var damageEstimation = calculator.CalculateDamageRange();

            // Roll actual damage
            var random = new Random();
            var actualDamage = random.Next(damageEstimation.Damage.Min, damageEstimation.Damage.Max + 1);

            // Apply damage
            target.TakeDamage(actualDamage);

            // Use retaliation
            retaliator.UseRetaliation();

            // Create result (no counter-retaliation)
            return new AttackResult
            {
                Attacker = retaliator,
                Defender = target,
                DamageDealt = actualDamage,
                KillsDealt = GetKillsFromDamage(target, actualDamage),
                IsKilled = !target.IsAlive,
                IsShooting = false,
                Retaliation = null
            };
        }

        /// <summary>
        /// Calculate kills from damage (for result tracking).
        /// </summary>
        private int GetKillsFromDamage(BattleUnit unit, int damage)
        {
            var countBefore = unit.Count;

            // Simulate damage calculation (without actually applying it)
            var remaining = damage;
            var kills = 0;

            if (unit.FirstUnitHP <= remaining)
            {
                remaining -= unit.FirstUnitHP;
                kills++;
            }
            else
            {
                return 0;
            }

            // Kill complete creatures
            while (remaining >= unit.MaxHealth && kills < countBefore)
            {
                remaining -= unit.MaxHealth;
                kills++;
            }

            // Check if remaining damage would kill another creature
            if (kills < countBefore && remaining > 0 && remaining >= unit.MaxHealth)
            {
                kills++;
            }

            return Math.Min(kills, countBefore);
        }
    }

    /// <summary>
    /// Result of an attack action.
    /// </summary>
    public class AttackResult
    {
        public BattleUnit Attacker { get; set; }
        public BattleUnit Defender { get; set; }
        public int DamageDealt { get; set; }
        public int KillsDealt { get; set; }
        public bool IsKilled { get; set; }
        public bool IsShooting { get; set; }
        public AttackResult Retaliation { get; set; }

        public override string ToString()
        {
            var type = IsShooting ? "Shot" : "Attacked";
            var result = $"{Attacker.CreatureType.creatureName} {type} {Defender.CreatureType.creatureName} for {DamageDealt} damage ({KillsDealt} killed)";

            if (Retaliation != null)
            {
                result += $"\n  → Retaliation: {Retaliation.DamageDealt} damage ({Retaliation.KillsDealt} killed)";
            }

            return result;
        }
    }

    /// <summary>
    /// Information about one side in battle (attacker or defender).
    /// Based on VCMI's SideInBattle.
    /// </summary>
    [Serializable]
    public class BattleSideInfo
    {
        public BattleSide Side { get; private set; }
        public Hero Hero { get; private set; }
        public int SpellsCastThisRound { get; set; } = 0;
        public bool HasRetreated { get; set; } = false;

        public BattleSideInfo(BattleSide side, Hero hero)
        {
            Side = side;
            Hero = hero;
        }

        /// <summary>
        /// Reset counters at start of new round.
        /// </summary>
        public void StartNewRound()
        {
            SpellsCastThisRound = 0;
        }
    }

    /// <summary>
    /// Battle phases (VCMI pattern).
    /// </summary>
    public enum BattlePhase
    {
        NOT_STARTED,
        TACTICS,      // Pre-battle unit positioning
        NORMAL,       // Regular combat
        ENDED         // Battle finished
    }

    /// <summary>
    /// Battlefield terrain type.
    /// </summary>
    public enum BattleFieldType
    {
        GRASS,
        DIRT,
        SAND,
        SNOW,
        SWAMP,
        ROUGH,
        CAVE,
        LAVA
    }

    /// <summary>
    /// Battlefield obstacle.
    /// Placeholder for future obstacle system.
    /// </summary>
    [Serializable]
    public class BattleObstacle
    {
        public string ObstacleName { get; set; }
        public bool BlocksMovement { get; set; } = true;
        public bool BlocksRanged { get; set; } = false;

        public BattleObstacle(string name)
        {
            ObstacleName = name;
        }
    }
}
