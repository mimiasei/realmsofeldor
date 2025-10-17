using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Turn queue system for battle.
    /// Based on VCMI's CMP_stack comparator and turn order logic.
    /// Manages initiative-based turn order with multiple phases (NORMAL, WAIT_MORALE, WAIT).
    /// </summary>
    public class TurnQueue
    {
        // ===== Turn Queue State =====
        private readonly List<TurnEntry> queue = new();
        private int currentTurn = 0;
        private BattleSide lastActiveSide = BattleSide.Attacker;

        // ===== Turn Phases =====
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.NORMAL;

        // ===== Constructor =====
        public TurnQueue()
        {
        }

        // ===== Queue Management =====

        /// <summary>
        /// Build turn queue from all alive units.
        /// Call this at start of each round.
        /// </summary>
        public void BuildQueue(IEnumerable<BattleUnit> units, int turn)
        {
            queue.Clear();
            currentTurn = turn;
            CurrentPhase = TurnPhase.NORMAL;

            // Add all alive units to NORMAL phase
            foreach (var unit in units.Where(u => u.IsAlive && !u.HasWaited))
            {
                queue.Add(new TurnEntry
                {
                    UnitId = unit.UnitId,
                    Phase = TurnPhase.NORMAL,
                    Initiative = unit.Initiative,
                    Side = unit.Side,
                    SlotIndex = unit.SlotIndex
                });
            }

            // Sort by initiative (NORMAL phase)
            SortQueue();
        }

        /// <summary>
        /// Get next unit ID to act.
        /// Returns -1 if no units left in queue.
        /// </summary>
        public int GetNextUnit()
        {
            if (queue.Count == 0)
                return -1;

            var entry = queue[0];
            queue.RemoveAt(0);

            // Track which side went last (for tiebreaker)
            lastActiveSide = entry.Side;

            return entry.UnitId;
        }

        /// <summary>
        /// Peek at next unit without removing from queue.
        /// </summary>
        public int PeekNextUnit()
        {
            return queue.Count > 0 ? queue[0].UnitId : -1;
        }

        /// <summary>
        /// Move unit to WAIT phase.
        /// Unit will act later in the round with potential morale bonus.
        /// </summary>
        public void MoveToWaitPhase(int unitId, BattleUnit unit)
        {
            // Remove from current position
            queue.RemoveAll(e => e.UnitId == unitId);

            // Add to WAIT_MORALE phase first (morale check happens when unit acts)
            queue.Add(new TurnEntry
            {
                UnitId = unitId,
                Phase = TurnPhase.WAIT_MORALE,
                Initiative = unit.Initiative,
                Side = unit.Side,
                SlotIndex = unit.SlotIndex
            });

            unit.HasWaited = true;

            // Re-sort queue
            SortQueue();
        }

        /// <summary>
        /// Move unit from WAIT_MORALE to WAIT phase (after morale check failed).
        /// </summary>
        public void MoveToWaitPhaseNoMorale(int unitId, BattleUnit unit)
        {
            // Find and update entry
            var entry = queue.FirstOrDefault(e => e.UnitId == unitId);
            if (entry != null)
            {
                entry.Phase = TurnPhase.WAIT;
                SortQueue();
            }
        }

        /// <summary>
        /// Insert unit for bonus turn (good morale).
        /// Unit acts immediately after current unit.
        /// </summary>
        public void InsertBonusTurn(int unitId, BattleUnit unit)
        {
            // Insert at front of queue (acts next)
            queue.Insert(0, new TurnEntry
            {
                UnitId = unitId,
                Phase = CurrentPhase,
                Initiative = unit.Initiative,
                Side = unit.Side,
                SlotIndex = unit.SlotIndex
            });
        }

        /// <summary>
        /// Check if queue is empty.
        /// </summary>
        public bool IsEmpty()
        {
            return queue.Count == 0;
        }

        /// <summary>
        /// Get count of remaining units in queue.
        /// </summary>
        public int GetRemainingCount()
        {
            return queue.Count;
        }

        /// <summary>
        /// Get all unit IDs in turn order (for UI display).
        /// </summary>
        public List<int> GetTurnOrder()
        {
            return queue.Select(e => e.UnitId).ToList();
        }

        // ===== Sorting Logic =====

        /// <summary>
        /// Sort queue based on VCMI's CMP_stack comparator.
        /// Initiative-based with tiebreaker rules.
        /// </summary>
        private void SortQueue()
        {
            queue.Sort((a, b) => CompareEntries(a, b));

            // Update current phase based on first entry
            if (queue.Count > 0)
            {
                CurrentPhase = queue[0].Phase;
            }
        }

        /// <summary>
        /// Compare two turn entries.
        /// Based on VCMI's CMP_stack::operator().
        /// </summary>
        private int CompareEntries(TurnEntry a, TurnEntry b)
        {
            // Sort by phase first (NORMAL -> WAIT_MORALE -> WAIT)
            if (a.Phase != b.Phase)
                return a.Phase.CompareTo(b.Phase);

            // Within same phase:
            // 1. Primary sort: Initiative (higher goes first)
            if (a.Initiative != b.Initiative)
                return b.Initiative.CompareTo(a.Initiative); // Descending order

            // 2. Tie-breaker: Same side → slot order (ascending)
            if (a.Side == b.Side)
                return a.SlotIndex.CompareTo(b.SlotIndex);

            // 3. Tie-breaker: Different sides → prioritize side that didn't go last
            // This prevents one side from monopolizing turns when initiatives are equal
            if (a.Side == lastActiveSide)
                return 1;  // a goes later
            if (b.Side == lastActiveSide)
                return -1; // b goes later

            // 4. Final tie-breaker: attacker goes before defender
            return a.Side.CompareTo(b.Side);
        }
    }

    /// <summary>
    /// Turn queue entry.
    /// Stores unit data for sorting.
    /// </summary>
    internal class TurnEntry
    {
        public int UnitId { get; set; }
        public TurnPhase Phase { get; set; }
        public int Initiative { get; set; }
        public BattleSide Side { get; set; }
        public int SlotIndex { get; set; }
    }

    /// <summary>
    /// Turn phases (based on VCMI's BattlePhases).
    /// Units in earlier phases act before units in later phases.
    /// </summary>
    public enum TurnPhase
    {
        NORMAL = 0,       // Regular turns
        WAIT_MORALE = 1,  // Units that waited (eligible for morale bonus)
        WAIT = 2          // Units that waited (no morale eligibility)
    }
}
