using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Tests
{
    /// <summary>
    /// Unit tests for guard placement system.
    /// Tests MapGenConfig guard calculations and MapObject guard functionality.
    /// Based on VCMI's guard placement algorithm.
    /// </summary>
    public class GuardPlacementTests
    {
        private MapGenConfig config;

        [SetUp]
        public void SetUp()
        {
            // Create a test config with known values
            config = ScriptableObject.CreateInstance<MapGenConfig>();
            config.minGuardedValue = 2000;
            config.enableGuards = true;
            config.guardStrengthValue1 = 2500;
            config.guardStrengthValue2 = 7500;
            config.guardStrengthMultiplier1 = 1.0f;
            config.guardStrengthMultiplier2 = 1.0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        #region Guard Requirement Tests

        [Test]
        public void RequiresGuards_LowValue_ReturnsFalse()
        {
            // Value below minGuardedValue (2000)
            Assert.IsFalse(config.RequiresGuards(1000));
            Assert.IsFalse(config.RequiresGuards(1999));
        }

        [Test]
        public void RequiresGuards_AtThreshold_ReturnsTrue()
        {
            // Value exactly at minGuardedValue (2000)
            Assert.IsTrue(config.RequiresGuards(2000));
        }

        [Test]
        public void RequiresGuards_HighValue_ReturnsTrue()
        {
            // Values above minGuardedValue
            Assert.IsTrue(config.RequiresGuards(3000));
            Assert.IsTrue(config.RequiresGuards(10000));
        }

        [Test]
        public void RequiresGuards_GuardsDisabled_ReturnsFalse()
        {
            config.enableGuards = false;
            Assert.IsFalse(config.RequiresGuards(5000));
        }

        #endregion

        #region Guard Strength Calculation Tests

        [Test]
        public void CalculateGuardStrength_BelowThreshold_ReturnsZero()
        {
            // Value below minGuardedValue should return 0
            var strength = config.CalculateGuardStrength(1000);
            Assert.AreEqual(0, strength);
        }

        [Test]
        public void CalculateGuardStrength_AtMinGuardedValue_ReturnsZero()
        {
            // At exactly minGuardedValue (2000), below first threshold (2500)
            var strength = config.CalculateGuardStrength(2000);
            Assert.AreEqual(0, strength);
        }

        [Test]
        public void CalculateGuardStrength_AboveFirstThreshold_ReturnsCorrectValue()
        {
            // Value 3000: (3000 - 2500) * 1.0 + 0 = 500
            var strength = config.CalculateGuardStrength(3000);
            Assert.AreEqual(500, strength);
        }

        [Test]
        public void CalculateGuardStrength_AboveSecondThreshold_UsesDoubleFormula()
        {
            // Value 10000:
            // strength1 = (10000 - 2500) * 1.0 = 7500
            // strength2 = (10000 - 7500) * 1.0 = 2500
            // total = 10000
            var strength = config.CalculateGuardStrength(10000);
            Assert.AreEqual(10000, strength);
        }

        [Test]
        public void CalculateGuardStrength_BetweenThresholds_UsesOnlyFirstFormula()
        {
            // Value 5000 (between 2500 and 7500):
            // strength1 = (5000 - 2500) * 1.0 = 2500
            // strength2 = max(0, 5000 - 7500) = 0
            // total = 2500
            var strength = config.CalculateGuardStrength(5000);
            Assert.AreEqual(2500, strength);
        }

        [Test]
        public void CalculateGuardStrength_WithDifferentMultipliers_ScalesCorrectly()
        {
            config.guardStrengthMultiplier1 = 0.5f;
            config.guardStrengthMultiplier2 = 1.5f;

            // Value 10000:
            // strength1 = (10000 - 2500) * 0.5 = 3750
            // strength2 = (10000 - 7500) * 1.5 = 3750
            // total = 7500
            var strength = config.CalculateGuardStrength(10000);
            Assert.AreEqual(7500, strength);
        }

        #endregion

        #region GuardInfo Class Tests

        [Test]
        public void GuardInfo_Constructor_SetsProperties()
        {
            var guard = new GuardInfo(5, 10, 500);
            Assert.AreEqual(5, guard.CreatureId);
            Assert.AreEqual(10, guard.Count);
            Assert.AreEqual(500, guard.CalculatedStrength);
        }

        [Test]
        public void GuardInfo_Constructor_DefaultStrength()
        {
            var guard = new GuardInfo(3, 7);
            Assert.AreEqual(3, guard.CreatureId);
            Assert.AreEqual(7, guard.Count);
            Assert.AreEqual(0, guard.CalculatedStrength);
        }

        #endregion

        #region MapObject Guard Tests

        [Test]
        public void MapObject_IsGuarded_NoGuard_ReturnsFalse()
        {
            var obj = new MapObject(MapObjectType.Resource, new Position(5, 5));
            Assert.IsFalse(obj.IsGuarded());
        }

        [Test]
        public void MapObject_IsGuarded_WithGuard_ReturnsTrue()
        {
            var obj = new MapObject(MapObjectType.Resource, new Position(5, 5));
            obj.Guard = new GuardInfo(3, 5, 300);
            Assert.IsTrue(obj.IsGuarded());
        }

        [Test]
        public void MapObject_IsGuarded_GuardWithZeroCount_ReturnsFalse()
        {
            var obj = new MapObject(MapObjectType.Resource, new Position(5, 5));
            obj.Guard = new GuardInfo(3, 0, 300);
            Assert.IsFalse(obj.IsGuarded());
        }

        [Test]
        public void MapObject_IsGuarded_GuardWithZeroCreatureId_ReturnsFalse()
        {
            var obj = new MapObject(MapObjectType.Resource, new Position(5, 5));
            obj.Guard = new GuardInfo(0, 5, 300);
            Assert.IsFalse(obj.IsGuarded());
        }

        [Test]
        public void MapObject_GetGuardPosition_ReturnsBelowObject()
        {
            var obj = new MapObject(MapObjectType.Resource, new Position(10, 10));
            var guardPos = obj.GetGuardPosition();

            // Guard should be placed directly below (south of) the object
            Assert.AreEqual(10, guardPos.X);
            Assert.AreEqual(11, guardPos.Y); // Y+1 (below)
        }

        #endregion

        #region Resource Object Value Tests

        [Test]
        public void ResourceObject_Value_CalculatesCorrectly()
        {
            config.goldValueMultiplier = 1;
            config.basicResourceValue = 125;
            config.rareResourceValue = 500;

            // Gold: 2000 * 1 = 2000
            var goldPile = new ResourceObject(new Position(5, 5), ResourceType.Gold, 2000);
            Assert.AreEqual(2000, goldPile.Value);

            // Wood: 10 * 125 = 1250
            var woodPile = new ResourceObject(new Position(5, 5), ResourceType.Wood, 10);
            Assert.AreEqual(1250, woodPile.Value);

            // Gems (rare): 5 * 500 = 2500
            var gemPile = new ResourceObject(new Position(5, 5), ResourceType.Gems, 5);
            Assert.AreEqual(2500, gemPile.Value);
        }

        [Test]
        public void ResourceObject_HighValue_RequiresGuards()
        {
            // 2500 gold = 2500 value > 2000 threshold
            var highValueResource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 2500);
            Assert.IsTrue(config.RequiresGuards(highValueResource.Value));
        }

        #endregion

        #region Mine Object Value Tests

        [Test]
        public void MineObject_Value_CalculatesStrategicValue()
        {
            config.basicResourceValue = 125;

            // Wood mine producing 2/day: 2 * 125 * 30 days = 7500
            var mine = new MineObject(new Position(5, 5), ResourceType.Wood, 2);
            Assert.AreEqual(7500, mine.Value);
        }

        [Test]
        public void MineObject_HighProduction_RequiresGuards()
        {
            config.basicResourceValue = 125;

            // Wood mine producing 3/day: 3 * 125 * 30 = 11250 > 2000
            var mine = new MineObject(new Position(5, 5), ResourceType.Wood, 3);
            Assert.IsTrue(config.RequiresGuards(mine.Value));

            // Should also trigger strong guards (above both thresholds)
            var strength = config.CalculateGuardStrength(mine.Value);
            Assert.Greater(strength, 0);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void GuardPlacement_FullWorkflow_WorksCorrectly()
        {
            // Create high-value resource
            var resource = new ResourceObject(new Position(10, 10), ResourceType.Gold, 3000);

            // Check if it requires guards
            Assert.IsTrue(config.RequiresGuards(resource.Value));

            // Calculate guard strength
            var strength = config.CalculateGuardStrength(resource.Value);
            Assert.AreEqual(500, strength); // (3000 - 2500) * 1.0 = 500

            // Assign guards
            resource.Guard = new GuardInfo(3, 10, strength);

            // Verify guard was assigned
            Assert.IsTrue(resource.IsGuarded());
            Assert.AreEqual(3, resource.Guard.CreatureId);
            Assert.AreEqual(10, resource.Guard.Count);
            Assert.AreEqual(500, resource.Guard.CalculatedStrength);

            // Verify guard position
            var guardPos = resource.GetGuardPosition();
            Assert.AreEqual(new Position(10, 11), guardPos);
        }

        [Test]
        public void GuardPlacement_MultipleObjectTypes_AssignsCorrectly()
        {
            // Low-value resource (no guards)
            var lowResource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 1000);
            Assert.IsFalse(config.RequiresGuards(lowResource.Value));

            // High-value resource (guards)
            var highResource = new ResourceObject(new Position(10, 10), ResourceType.Gold, 3000);
            Assert.IsTrue(config.RequiresGuards(highResource.Value));

            // High-value mine (guards)
            var mine = new MineObject(new Position(15, 15), ResourceType.Wood, 2);
            Assert.IsTrue(config.RequiresGuards(mine.Value));

            // Verify different strength calculations
            var resourceStrength = config.CalculateGuardStrength(highResource.Value);
            var mineStrength = config.CalculateGuardStrength(mine.Value);
            Assert.AreNotEqual(resourceStrength, mineStrength);
        }

        #endregion
    }
}
