using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Tests
{
    public class MapGenBudgetTests
    {
        private MapGenConfig _testConfig;

        [SetUp]
        public void SetUp()
        {
            // Create a test config with known values
            _testConfig = ScriptableObject.CreateInstance<MapGenConfig>();
            _testConfig.treasureBudget = 10000;
            _testConfig.mineCount = 5;
            _testConfig.dwellingCount = 3;
            _testConfig.resourcePileCount = 10;
            _testConfig.goldValueMultiplier = 1;
            _testConfig.basicResourceValue = 125;
            _testConfig.rareResourceValue = 500;
        }

        [Test]
        public void MapGenBudget_Constructor_InitializesWithZero()
        {
            var budget = new MapGenBudget(_testConfig);

            Assert.AreEqual(10000, budget.RemainingTreasureBudget);
            Assert.AreEqual(5, budget.RemainingMineSlots);
            Assert.AreEqual(3, budget.RemainingDwellingSlots);
            Assert.AreEqual(10, budget.RemainingResourcePileSlots);
            Assert.AreEqual(0, budget.TotalObjectsPlaced);
        }

        [Test]
        public void MapGenBudget_RecordResourcePile_DeductsBudget()
        {
            var budget = new MapGenBudget(_testConfig);
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 1000);

            budget.RecordResourcePile(resource);

            Assert.AreEqual(9000, budget.RemainingTreasureBudget);
            Assert.AreEqual(9, budget.RemainingResourcePileSlots);
            Assert.AreEqual(1, budget.TotalObjectsPlaced);
        }

        [Test]
        public void MapGenBudget_RecordMine_DecrementsMineCount()
        {
            var budget = new MapGenBudget(_testConfig);
            var mine = new MineObject(new Position(10, 10), ResourceType.Ore, 2);

            budget.RecordMine(mine);

            Assert.AreEqual(4, budget.RemainingMineSlots);
            Assert.AreEqual(10000, budget.RemainingTreasureBudget); // Mines don't use treasure budget
            Assert.AreEqual(1, budget.TotalObjectsPlaced);
        }

        [Test]
        public void MapGenBudget_RecordDwelling_DecrementsDwellingCount()
        {
            var budget = new MapGenBudget(_testConfig);
            var dwelling = new DwellingObject(new Position(15, 15), 1, 10);

            budget.RecordDwelling(dwelling);

            Assert.AreEqual(2, budget.RemainingDwellingSlots);
            Assert.AreEqual(10000, budget.RemainingTreasureBudget); // Dwellings don't use treasure budget
            Assert.AreEqual(1, budget.TotalObjectsPlaced);
        }

        [Test]
        public void MapGenBudget_CanPlaceResourcePile_ReturnsFalseWhenBudgetExceeded()
        {
            var budget = new MapGenBudget(_testConfig);

            // Spend most of the budget
            var largeResource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 9500);
            budget.RecordResourcePile(largeResource);

            // Try to place another large resource
            Assert.IsFalse(budget.CanPlaceResourcePile(1000)); // Would exceed budget
            Assert.IsTrue(budget.CanPlaceResourcePile(500)); // Within budget
        }

        [Test]
        public void MapGenBudget_CanPlaceResourcePile_ReturnsFalseWhenCountExceeded()
        {
            var budget = new MapGenBudget(_testConfig);

            // Place max resource piles
            for (int i = 0; i < 10; i++)
            {
                var resource = new ResourceObject(new Position(i, i), ResourceType.Gold, 100);
                budget.RecordResourcePile(resource);
            }

            Assert.IsFalse(budget.CanPlaceResourcePile(100)); // Count exceeded
        }

        [Test]
        public void MapGenBudget_CanPlaceMine_ReturnsFalseWhenCountExceeded()
        {
            var budget = new MapGenBudget(_testConfig);

            // Place max mines
            for (int i = 0; i < 5; i++)
            {
                var mine = new MineObject(new Position(i, i), ResourceType.Ore, 1);
                budget.RecordMine(mine);
            }

            Assert.IsFalse(budget.CanPlaceMine()); // Count exceeded
        }

        [Test]
        public void MapGenBudget_CanPlaceDwelling_ReturnsFalseWhenCountExceeded()
        {
            var budget = new MapGenBudget(_testConfig);

            // Place max dwellings
            for (int i = 0; i < 3; i++)
            {
                var dwelling = new DwellingObject(new Position(i, i), 1, 10);
                budget.RecordDwelling(dwelling);
            }

            Assert.IsFalse(budget.CanPlaceDwelling()); // Count exceeded
        }

        [Test]
        public void MapGenBudget_BudgetUtilization_CalculatesCorrectly()
        {
            var budget = new MapGenBudget(_testConfig);

            Assert.AreEqual(0f, budget.BudgetUtilization);

            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 5000);
            budget.RecordResourcePile(resource);

            Assert.AreEqual(0.5f, budget.BudgetUtilization, 0.01f); // 50%
        }

        [Test]
        public void MapGenBudget_RecordObject_HandlesAllTypes()
        {
            var budget = new MapGenBudget(_testConfig);

            var resource = new ResourceObject(new Position(1, 1), ResourceType.Gold, 500);
            var mine = new MineObject(new Position(2, 2), ResourceType.Ore, 1);
            var dwelling = new DwellingObject(new Position(3, 3), 1, 10);
            var decorative = new MapObject(MapObjectType.Decorative, new Position(4, 4));

            budget.RecordObject(resource);
            budget.RecordObject(mine);
            budget.RecordObject(dwelling);
            budget.RecordObject(decorative);

            Assert.AreEqual(4, budget.TotalObjectsPlaced);
            Assert.AreEqual(9500, budget.RemainingTreasureBudget);
            Assert.AreEqual(4, budget.RemainingMineSlots);
            Assert.AreEqual(2, budget.RemainingDwellingSlots);
        }

        [Test]
        public void MapGenConfig_CalculateResourceValue_Gold()
        {
            var value = _testConfig.CalculateResourceValue(ResourceType.Gold, 1000);
            Assert.AreEqual(1000, value); // 1000 * 1
        }

        [Test]
        public void MapGenConfig_CalculateResourceValue_BasicResource()
        {
            var value = _testConfig.CalculateResourceValue(ResourceType.Wood, 10);
            Assert.AreEqual(1250, value); // 10 * 125
        }

        [Test]
        public void MapGenConfig_CalculateResourceValue_RareResource()
        {
            var value = _testConfig.CalculateResourceValue(ResourceType.Crystal, 5);
            Assert.AreEqual(2500, value); // 5 * 500
        }

        [Test]
        public void MapObject_Value_CachesCalculation()
        {
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 1000);

            var value1 = resource.Value;
            var value2 = resource.Value;

            Assert.AreEqual(value1, value2);
            Assert.AreEqual(1000, value1); // Gold is 1:1
        }

        [Test]
        public void ResourceObject_Value_CalculatesCorrectly()
        {
            var goldPile = new ResourceObject(new Position(1, 1), ResourceType.Gold, 500);
            var woodPile = new ResourceObject(new Position(2, 2), ResourceType.Wood, 10);
            var crystalPile = new ResourceObject(new Position(3, 3), ResourceType.Crystal, 3);

            Assert.AreEqual(500, goldPile.Value);
            Assert.AreEqual(1250, woodPile.Value); // 10 * 125
            Assert.AreEqual(1500, crystalPile.Value); // 3 * 500
        }

        [Test]
        public void MineObject_Value_CalculatesStrategicValue()
        {
            var goldMine = new MineObject(new Position(1, 1), ResourceType.Gold, 1000);
            var oreMine = new MineObject(new Position(2, 2), ResourceType.Ore, 2);

            // Mines: daily value * 30 days
            Assert.AreEqual(30000, goldMine.Value); // 1000 * 1 * 30
            Assert.AreEqual(7500, oreMine.Value); // 2 * 125 * 30
        }

        [Test]
        public void MapObject_Value_DecorativeHasZeroValue()
        {
            var decorative = new MapObject(MapObjectType.Decorative, new Position(5, 5));
            Assert.AreEqual(0, decorative.Value);
        }
    }
}
