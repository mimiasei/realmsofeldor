using NUnit.Framework;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Tests
{
    [TestFixture]
    public class ResourceTests
    {
        [Test]
        public void ResourceSet_Get_ReturnsCorrectValue()
        {
            // Arrange
            var resources = new ResourceSet(wood: 100, gold: 500);

            // Act & Assert
            Assert.AreEqual(100, resources.Get(ResourceType.Wood));
            Assert.AreEqual(500, resources.Get(ResourceType.Gold));
        }

        [Test]
        public void ResourceSet_Set_SetsCorrectValue()
        {
            // Arrange
            var resources = new ResourceSet();

            // Act
            resources.Set(ResourceType.Wood, 50);

            // Assert
            Assert.AreEqual(50, resources.Get(ResourceType.Wood));
        }

        [Test]
        public void ResourceSet_Add_AddsResources()
        {
            // Arrange
            var resources = new ResourceSet(wood: 100, gold: 500);
            var toAdd = new ResourceSet(wood: 50, gold: 100);

            // Act
            resources.Add(toAdd);

            // Assert
            Assert.AreEqual(150, resources.Wood);
            Assert.AreEqual(600, resources.Gold);
        }

        [Test]
        public void ResourceSet_Subtract_SubtractsResources()
        {
            // Arrange
            var resources = new ResourceSet(wood: 100, gold: 500);
            var toSubtract = new ResourceSet(wood: 30, gold: 200);

            // Act
            resources.Subtract(toSubtract);

            // Assert
            Assert.AreEqual(70, resources.Wood);
            Assert.AreEqual(300, resources.Gold);
        }

        [Test]
        public void ResourceSet_CanAfford_ReturnsTrueWhenEnough()
        {
            // Arrange
            var resources = new ResourceSet(wood: 100, gold: 500);
            var cost = new ResourceSet(wood: 50, gold: 300);

            // Act & Assert
            Assert.IsTrue(resources.CanAfford(cost));
        }

        [Test]
        public void ResourceSet_CanAfford_ReturnsFalseWhenNotEnough()
        {
            // Arrange
            var resources = new ResourceSet(wood: 100, gold: 500);
            var cost = new ResourceSet(wood: 150, gold: 300);

            // Act & Assert
            Assert.IsFalse(resources.CanAfford(cost));
        }

        [Test]
        public void ResourceSet_OperatorAdd_WorksCorrectly()
        {
            // Arrange
            var a = new ResourceSet(wood: 100, gold: 500);
            var b = new ResourceSet(wood: 50, gold: 100);

            // Act
            var result = a + b;

            // Assert
            Assert.AreEqual(150, result.Wood);
            Assert.AreEqual(600, result.Gold);
        }

        [Test]
        public void ResourceSet_OperatorSubtract_WorksCorrectly()
        {
            // Arrange
            var a = new ResourceSet(wood: 100, gold: 500);
            var b = new ResourceSet(wood: 30, gold: 200);

            // Act
            var result = a - b;

            // Assert
            Assert.AreEqual(70, result.Wood);
            Assert.AreEqual(300, result.Gold);
        }

        [Test]
        public void Player_TryPayCost_SucceedsWhenCanAfford()
        {
            // Arrange
            var player = new Player();
            player.Resources = new ResourceSet(wood: 100, gold: 500);
            var cost = new ResourceSet(wood: 50, gold: 200);

            // Act
            bool result = player.TryPayCost(cost);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(50, player.Resources.Wood);
            Assert.AreEqual(300, player.Resources.Gold);
        }

        [Test]
        public void Player_TryPayCost_FailsWhenCannotAfford()
        {
            // Arrange
            var player = new Player();
            player.Resources = new ResourceSet(wood: 30, gold: 500);
            var cost = new ResourceSet(wood: 50, gold: 200);

            // Act
            bool result = player.TryPayCost(cost);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(30, player.Resources.Wood); // Unchanged
            Assert.AreEqual(500, player.Resources.Gold); // Unchanged
        }
    }
}
