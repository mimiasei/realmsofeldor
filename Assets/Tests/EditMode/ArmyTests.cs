using NUnit.Framework;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Tests
{
    [TestFixture]
    public class ArmyTests
    {
        [Test]
        public void Army_AddCreatures_AddsToEmptySlot()
        {
            // Arrange
            var army = new Army();

            // Act
            bool result = army.AddCreatures(creatureId: 1, count: 10);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10, army.GetSlot(0).Count);
            Assert.AreEqual(1, army.GetSlot(0).CreatureId);
        }

        [Test]
        public void Army_AddCreatures_MergesWithExistingStack()
        {
            // Arrange
            var army = new Army();
            army.AddCreatures(creatureId: 1, count: 10);

            // Act
            bool result = army.AddCreatures(creatureId: 1, count: 5);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(15, army.GetSlot(0).Count);
        }

        [Test]
        public void Army_AddCreatures_UsesPreferredSlot()
        {
            // Arrange
            var army = new Army();

            // Act
            bool result = army.AddCreatures(creatureId: 1, count: 10, preferredSlot: 3);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10, army.GetSlot(3).Count);
        }

        [Test]
        public void Army_AddCreatures_FailsWhenFull()
        {
            // Arrange
            var army = new Army();
            for (int i = 0; i < Army.MaxSlots; i++)
            {
                army.AddCreatures(creatureId: i, count: 10);
            }

            // Act
            bool result = army.AddCreatures(creatureId: 99, count: 10);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Army_RemoveCreatures_ReducesCount()
        {
            // Arrange
            var army = new Army();
            army.AddCreatures(creatureId: 1, count: 10);

            // Act
            bool result = army.RemoveCreatures(slotIndex: 0, count: 5);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5, army.GetSlot(0).Count);
        }

        [Test]
        public void Army_RemoveCreatures_ClearsSlotWhenEmpty()
        {
            // Arrange
            var army = new Army();
            army.AddCreatures(creatureId: 1, count: 10);

            // Act
            bool result = army.RemoveCreatures(slotIndex: 0, count: 10);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNull(army.GetSlot(0));
        }

        [Test]
        public void Army_RemoveCreatures_FailsWhenNotEnoughCreatures()
        {
            // Arrange
            var army = new Army();
            army.AddCreatures(creatureId: 1, count: 5);

            // Act
            bool result = army.RemoveCreatures(slotIndex: 0, count: 10);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(5, army.GetSlot(0).Count);
        }

        [Test]
        public void Army_IsEmpty_ReturnsTrueForNewArmy()
        {
            // Arrange
            var army = new Army();

            // Act & Assert
            Assert.IsTrue(army.IsEmpty());
        }

        [Test]
        public void Army_IsEmpty_ReturnsFalseWhenHasCreatures()
        {
            // Arrange
            var army = new Army();
            army.AddCreatures(creatureId: 1, count: 10);

            // Act & Assert
            Assert.IsFalse(army.IsEmpty());
        }

        [Test]
        public void Army_GetTotalCount_ReturnsCorrectSum()
        {
            // Arrange
            var army = new Army();
            army.AddCreatures(creatureId: 1, count: 10);
            army.AddCreatures(creatureId: 2, count: 5);
            army.AddCreatures(creatureId: 3, count: 7);

            // Act
            int total = army.GetTotalCount();

            // Assert
            Assert.AreEqual(22, total);
        }

        [Test]
        public void Army_Clear_RemovesAllStacks()
        {
            // Arrange
            var army = new Army();
            army.AddCreatures(creatureId: 1, count: 10);
            army.AddCreatures(creatureId: 2, count: 5);

            // Act
            army.Clear();

            // Assert
            Assert.IsTrue(army.IsEmpty());
        }

        [Test]
        public void Army_FindEmptySlot_ReturnsCorrectIndex()
        {
            // Arrange
            var army = new Army();
            army.SetSlot(0, new CreatureStack(1, 10));
            army.SetSlot(1, new CreatureStack(2, 5));

            // Act
            int emptySlot = army.FindEmptySlot();

            // Assert
            Assert.AreEqual(2, emptySlot);
        }

        [Test]
        public void Army_FindEmptySlot_ReturnsNegativeWhenFull()
        {
            // Arrange
            var army = new Army();
            for (int i = 0; i < Army.MaxSlots; i++)
            {
                army.AddCreatures(creatureId: i, count: 10);
            }

            // Act
            int emptySlot = army.FindEmptySlot();

            // Assert
            Assert.AreEqual(-1, emptySlot);
        }
    }
}
