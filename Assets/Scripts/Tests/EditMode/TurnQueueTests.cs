using NUnit.Framework;
using RealmsOfEldor.Core.Battle;
using RealmsOfEldor.Data;
using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Core;
using UnityEngine;

namespace RealmsOfEldor.Tests.Battle
{
    [TestFixture]
    public class TurnQueueTests
    {
        private CreatureData fastCreature;
        private CreatureData mediumCreature;
        private CreatureData slowCreature;

        [SetUp]
        public void Setup()
        {
            // Create test creatures with different speeds
            fastCreature = CreateTestCreature("Fast", speed: 10);
            mediumCreature = CreateTestCreature("Medium", speed: 7);
            slowCreature = CreateTestCreature("Slow", speed: 4);
        }

        // ===== Basic Queue Construction =====

        [Test]
        public void BuildQueue_EmptyUnits_CreatesEmptyQueue()
        {
            var queue = new TurnQueue();
            var units = new List<BattleUnit>();

            queue.BuildQueue(units, turn: 1);

            Assert.IsTrue(queue.IsEmpty());
            Assert.AreEqual(0, queue.GetRemainingCount());
        }

        [Test]
        public void BuildQueue_SingleUnit_CreatesQueueWithOneEntry()
        {
            var queue = new TurnQueue();
            var unit = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);
            var units = new List<BattleUnit> { unit };

            queue.BuildQueue(units, turn: 1);

            Assert.IsFalse(queue.IsEmpty());
            Assert.AreEqual(1, queue.GetRemainingCount());
            Assert.AreEqual(unit.UnitId, queue.PeekNextUnit());
        }

        [Test]
        public void BuildQueue_MultipleUnits_SortsCorrectly()
        {
            var queue = new TurnQueue();
            var slow = CreateBattleUnit(1, slowCreature, BattleSide.Attacker, slotIndex: 0);
            var fast = CreateBattleUnit(2, fastCreature, BattleSide.Attacker, slotIndex: 1);
            var medium = CreateBattleUnit(3, mediumCreature, BattleSide.Attacker, slotIndex: 2);

            var units = new List<BattleUnit> { slow, fast, medium };
            queue.BuildQueue(units, turn: 1);

            // Should be sorted by initiative: fast (10) -> medium (7) -> slow (4)
            Assert.AreEqual(fast.UnitId, queue.PeekNextUnit());
            queue.GetNextUnit();
            Assert.AreEqual(medium.UnitId, queue.PeekNextUnit());
            queue.GetNextUnit();
            Assert.AreEqual(slow.UnitId, queue.PeekNextUnit());
        }

        // ===== Initiative Sorting =====

        [Test]
        public void BuildQueue_HigherInitiativeGoesFirst()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, slowCreature, BattleSide.Attacker, slotIndex: 0);    // Speed 4
            var unit2 = CreateBattleUnit(2, fastCreature, BattleSide.Attacker, slotIndex: 1);    // Speed 10

            queue.BuildQueue(new[] { unit1, unit2 }, turn: 1);

            Assert.AreEqual(unit2.UnitId, queue.GetNextUnit()); // Fast goes first
            Assert.AreEqual(unit1.UnitId, queue.GetNextUnit()); // Slow goes second
        }

        // ===== Tiebreaker Rules =====

        [Test]
        public void BuildQueue_SameSide_SameInitiative_UsesSlotOrder()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, mediumCreature, BattleSide.Attacker, slotIndex: 2);  // Slot 2
            var unit2 = CreateBattleUnit(2, mediumCreature, BattleSide.Attacker, slotIndex: 0);  // Slot 0
            var unit3 = CreateBattleUnit(3, mediumCreature, BattleSide.Attacker, slotIndex: 1);  // Slot 1

            queue.BuildQueue(new[] { unit1, unit2, unit3 }, turn: 1);

            // Same initiative (7), same side → sort by slot order (0, 1, 2)
            Assert.AreEqual(unit2.UnitId, queue.GetNextUnit()); // Slot 0
            Assert.AreEqual(unit3.UnitId, queue.GetNextUnit()); // Slot 1
            Assert.AreEqual(unit1.UnitId, queue.GetNextUnit()); // Slot 2
        }

        [Test]
        public void BuildQueue_DifferentSides_SameInitiative_UsesSidePriority()
        {
            var queue = new TurnQueue();
            var attacker1 = CreateBattleUnit(1, mediumCreature, BattleSide.Attacker, slotIndex: 0);
            var defender1 = CreateBattleUnit(2, mediumCreature, BattleSide.Defender, slotIndex: 0);
            var attacker2 = CreateBattleUnit(3, mediumCreature, BattleSide.Attacker, slotIndex: 1);

            queue.BuildQueue(new[] { attacker1, defender1, attacker2 }, turn: 1);

            var turnOrder = queue.GetTurnOrder();

            // With same initiative, sides should alternate to prevent monopoly
            // First should be attacker (default first side)
            Assert.AreEqual(3, turnOrder.Count);
        }

        // ===== Queue Operations =====

        [Test]
        public void GetNextUnit_EmptyQueue_ReturnsNegativeOne()
        {
            var queue = new TurnQueue();

            var nextUnitId = queue.GetNextUnit();

            Assert.AreEqual(-1, nextUnitId);
        }

        [Test]
        public void GetNextUnit_RemovesUnitFromQueue()
        {
            var queue = new TurnQueue();
            var unit = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);

            queue.BuildQueue(new[] { unit }, turn: 1);
            Assert.AreEqual(1, queue.GetRemainingCount());

            queue.GetNextUnit();

            Assert.AreEqual(0, queue.GetRemainingCount());
            Assert.IsTrue(queue.IsEmpty());
        }

        [Test]
        public void PeekNextUnit_DoesNotRemoveFromQueue()
        {
            var queue = new TurnQueue();
            var unit = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);

            queue.BuildQueue(new[] { unit }, turn: 1);

            var peeked1 = queue.PeekNextUnit();
            var peeked2 = queue.PeekNextUnit();

            Assert.AreEqual(unit.UnitId, peeked1);
            Assert.AreEqual(unit.UnitId, peeked2);
            Assert.AreEqual(1, queue.GetRemainingCount());
        }

        // ===== Wait Mechanics =====

        [Test]
        public void MoveToWaitPhase_MovesUnitToWaitMoralePhase()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);
            var unit2 = CreateBattleUnit(2, slowCreature, BattleSide.Attacker, slotIndex: 1);

            queue.BuildQueue(new[] { unit1, unit2 }, turn: 1);

            // Fast unit waits
            queue.MoveToWaitPhase(unit1.UnitId, unit1);

            // Slow unit should now be next
            Assert.AreEqual(unit2.UnitId, queue.PeekNextUnit());
        }

        [Test]
        public void MoveToWaitPhase_UnitActsLaterInRound()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);   // Speed 10
            var unit2 = CreateBattleUnit(2, mediumCreature, BattleSide.Attacker, slotIndex: 1); // Speed 7
            var unit3 = CreateBattleUnit(3, slowCreature, BattleSide.Attacker, slotIndex: 2);   // Speed 4

            queue.BuildQueue(new[] { unit1, unit2, unit3 }, turn: 1);

            // Fast unit waits
            queue.MoveToWaitPhase(unit1.UnitId, unit1);

            // Order should now be: medium (7) -> slow (4) -> fast (10, waited)
            Assert.AreEqual(unit2.UnitId, queue.GetNextUnit());
            Assert.AreEqual(unit3.UnitId, queue.GetNextUnit());
            Assert.AreEqual(unit1.UnitId, queue.GetNextUnit()); // Fast unit acts last after waiting
        }

        [Test]
        public void MoveToWaitPhase_SetsHasWaitedFlag()
        {
            var queue = new TurnQueue();
            var unit = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);

            queue.BuildQueue(new[] { unit }, turn: 1);
            Assert.IsFalse(unit.HasWaited);

            queue.MoveToWaitPhase(unit.UnitId, unit);

            Assert.IsTrue(unit.HasWaited);
        }

        [Test]
        public void BuildQueue_SkipsUnitsWithHasWaitedFlag()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);
            var unit2 = CreateBattleUnit(2, slowCreature, BattleSide.Attacker, slotIndex: 1);

            unit1.HasWaited = true; // Already waited

            queue.BuildQueue(new[] { unit1, unit2 }, turn: 1);

            // Only unit2 should be in queue
            Assert.AreEqual(1, queue.GetRemainingCount());
            Assert.AreEqual(unit2.UnitId, queue.PeekNextUnit());
        }

        // ===== Bonus Turn (Good Morale) =====

        [Test]
        public void InsertBonusTurn_InsertsUnitAtFront()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);
            var unit2 = CreateBattleUnit(2, slowCreature, BattleSide.Attacker, slotIndex: 1);

            queue.BuildQueue(new[] { unit1, unit2 }, turn: 1);
            queue.GetNextUnit(); // Remove unit1

            // Unit1 gets good morale → bonus turn
            queue.InsertBonusTurn(unit1.UnitId, unit1);

            // Unit1 should act next (before unit2)
            Assert.AreEqual(unit1.UnitId, queue.PeekNextUnit());
        }

        // ===== Turn Order Display =====

        [Test]
        public void GetTurnOrder_ReturnsCorrectOrder()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, slowCreature, BattleSide.Attacker, slotIndex: 0);
            var unit2 = CreateBattleUnit(2, fastCreature, BattleSide.Attacker, slotIndex: 1);
            var unit3 = CreateBattleUnit(3, mediumCreature, BattleSide.Attacker, slotIndex: 2);

            queue.BuildQueue(new[] { unit1, unit2, unit3 }, turn: 1);

            var turnOrder = queue.GetTurnOrder();

            Assert.AreEqual(3, turnOrder.Count);
            Assert.AreEqual(unit2.UnitId, turnOrder[0]); // Fast
            Assert.AreEqual(unit3.UnitId, turnOrder[1]); // Medium
            Assert.AreEqual(unit1.UnitId, turnOrder[2]); // Slow
        }

        [Test]
        public void GetTurnOrder_UpdatesAfterGetNextUnit()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);
            var unit2 = CreateBattleUnit(2, slowCreature, BattleSide.Attacker, slotIndex: 1);

            queue.BuildQueue(new[] { unit1, unit2 }, turn: 1);
            queue.GetNextUnit();

            var turnOrder = queue.GetTurnOrder();

            Assert.AreEqual(1, turnOrder.Count);
            Assert.AreEqual(unit2.UnitId, turnOrder[0]);
        }

        // ===== Edge Cases =====

        [Test]
        public void BuildQueue_SkipsDeadUnits()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);
            var unit2 = CreateBattleUnit(2, slowCreature, BattleSide.Attacker, slotIndex: 1);

            unit1.TakeDamage(1000); // Kill unit1
            Assert.IsFalse(unit1.IsAlive);

            queue.BuildQueue(new[] { unit1, unit2 }, turn: 1);

            // Only unit2 should be in queue
            Assert.AreEqual(1, queue.GetRemainingCount());
            Assert.AreEqual(unit2.UnitId, queue.PeekNextUnit());
        }

        [Test]
        public void HasRemainingCount_AccuratelyReportsCount()
        {
            var queue = new TurnQueue();
            var unit1 = CreateBattleUnit(1, fastCreature, BattleSide.Attacker, slotIndex: 0);
            var unit2 = CreateBattleUnit(2, slowCreature, BattleSide.Attacker, slotIndex: 1);

            queue.BuildQueue(new[] { unit1, unit2 }, turn: 1);

            Assert.AreEqual(2, queue.GetRemainingCount());

            queue.GetNextUnit();
            Assert.AreEqual(1, queue.GetRemainingCount());

            queue.GetNextUnit();
            Assert.AreEqual(0, queue.GetRemainingCount());
        }

        // ===== Helper Methods =====

        private CreatureData CreateTestCreature(string name, int speed)
        {
            return ScriptableObject.CreateInstance<CreatureData>();
        }

        private BattleUnit CreateBattleUnit(int unitId, CreatureData creature, BattleSide side, int slotIndex)
        {
            var hex = new BattleHex(slotIndex); // Simple position
            return new BattleUnit(unitId, creature, count: 10, side, slotIndex, hex);
        }
    }
}
