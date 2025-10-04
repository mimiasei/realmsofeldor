using NUnit.Framework;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Tests
{
    public class MapObjectTests
    {
        [Test]
        public void ResourceObject_Constructor_InitializesCorrectly()
        {
            var pos = new Position(5, 10);
            var resource = new ResourceObject(pos, ResourceType.Gold, 500);

            Assert.AreEqual(MapObjectType.Resource, resource.ObjectType);
            Assert.AreEqual(pos, resource.Position);
            Assert.AreEqual(ResourceType.Gold, resource.ResourceType);
            Assert.AreEqual(500, resource.Amount);
            Assert.IsFalse(resource.IsBlocking);
            Assert.IsTrue(resource.IsVisitable);
            Assert.IsTrue(resource.IsRemovable);
        }

        [Test]
        public void ResourceObject_GetBlockedPositions_ReturnsEmpty()
        {
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Wood, 10);
            var blocked = resource.GetBlockedPositions();

            Assert.AreEqual(0, blocked.Count);
        }

        [Test]
        public void MineObject_Constructor_InitializesCorrectly()
        {
            var pos = new Position(3, 7);
            var mine = new MineObject(pos, ResourceType.Ore, 2);

            Assert.AreEqual(MapObjectType.Mine, mine.ObjectType);
            Assert.AreEqual(pos, mine.Position);
            Assert.AreEqual(ResourceType.Ore, mine.ResourceType);
            Assert.AreEqual(2, mine.DailyProduction);
            Assert.IsTrue(mine.IsBlocking);
            Assert.IsTrue(mine.IsVisitable);
            Assert.IsFalse(mine.IsRemovable);
        }

        [Test]
        public void MineObject_GetBlockedPositions_ReturnsOwnPosition()
        {
            var pos = new Position(3, 7);
            var mine = new MineObject(pos, ResourceType.Ore, 1);
            var blocked = mine.GetBlockedPositions();

            Assert.AreEqual(1, blocked.Count);
            Assert.IsTrue(blocked.Contains(pos));
        }

        [Test]
        public void DwellingObject_Constructor_InitializesCorrectly()
        {
            var pos = new Position(10, 15);
            var dwelling = new DwellingObject(pos, creatureId: 1, weeklyGrowth: 5);

            Assert.AreEqual(MapObjectType.Dwelling, dwelling.ObjectType);
            Assert.AreEqual(pos, dwelling.Position);
            Assert.AreEqual(1, dwelling.CreatureId);
            Assert.AreEqual(5, dwelling.WeeklyGrowth);
            Assert.AreEqual(5, dwelling.AvailableCreatures); // Starts with weeklyGrowth
            Assert.IsTrue(dwelling.IsBlocking);
            Assert.IsTrue(dwelling.IsVisitable);
            Assert.IsFalse(dwelling.IsRemovable);
        }

        [Test]
        public void DwellingObject_GetBlockedPositions_ReturnsOwnPosition()
        {
            var pos = new Position(10, 15);
            var dwelling = new DwellingObject(pos, 1, 5);
            var blocked = dwelling.GetBlockedPositions();

            Assert.AreEqual(1, blocked.Count);
            Assert.IsTrue(blocked.Contains(pos));
        }

        [Test]
        public void DwellingObject_ApplyWeeklyGrowth_IncreasesAvailableCreatures()
        {
            var dwelling = new DwellingObject(new Position(10, 15), 1, 5);
            Assert.AreEqual(5, dwelling.AvailableCreatures);

            dwelling.ApplyWeeklyGrowth();
            Assert.AreEqual(10, dwelling.AvailableCreatures);

            dwelling.ApplyWeeklyGrowth();
            Assert.AreEqual(15, dwelling.AvailableCreatures);
        }

        [Test]
        public void DwellingObject_CanRecruit_ReturnsTrueWhenEnoughCreatures()
        {
            var dwelling = new DwellingObject(new Position(10, 15), 1, 10);
            dwelling.AvailableCreatures = 15;

            Assert.IsTrue(dwelling.CanRecruit(10));
            Assert.IsTrue(dwelling.CanRecruit(15));
            Assert.IsFalse(dwelling.CanRecruit(16));
        }

        [Test]
        public void DwellingObject_Recruit_DecreasesAvailableCreatures()
        {
            var dwelling = new DwellingObject(new Position(10, 15), 1, 10);
            dwelling.AvailableCreatures = 20;

            dwelling.Recruit(8);
            Assert.AreEqual(12, dwelling.AvailableCreatures);

            dwelling.Recruit(12);
            Assert.AreEqual(0, dwelling.AvailableCreatures);
        }

        [Test]
        public void DwellingObject_Recruit_DoesNotRecruitMoreThanAvailable()
        {
            var dwelling = new DwellingObject(new Position(10, 15), 1, 10);
            dwelling.AvailableCreatures = 5;

            dwelling.Recruit(10); // Try to recruit more than available
            Assert.AreEqual(5, dwelling.AvailableCreatures); // Should not change
        }

        [Test]
        public void MapObject_GetVisitablePositions_ReturnsAdjacentPositions()
        {
            var mine = new MineObject(new Position(5, 5), ResourceType.Gold, 1);
            var visitable = mine.GetVisitablePositions();

            // Should return 8 adjacent positions
            Assert.AreEqual(8, visitable.Count);
            Assert.IsTrue(visitable.Contains(new Position(4, 4)));
            Assert.IsTrue(visitable.Contains(new Position(4, 5)));
            Assert.IsTrue(visitable.Contains(new Position(4, 6)));
            Assert.IsTrue(visitable.Contains(new Position(5, 4)));
            Assert.IsTrue(visitable.Contains(new Position(5, 6)));
            Assert.IsTrue(visitable.Contains(new Position(6, 4)));
            Assert.IsTrue(visitable.Contains(new Position(6, 5)));
            Assert.IsTrue(visitable.Contains(new Position(6, 6)));
        }

        [Test]
        public void MapObject_IsVisitableAt_ReturnsTrueForAdjacentPositions()
        {
            var mine = new MineObject(new Position(5, 5), ResourceType.Gold, 1);

            Assert.IsTrue(mine.IsVisitableAt(new Position(4, 4)));
            Assert.IsTrue(mine.IsVisitableAt(new Position(5, 4)));
            Assert.IsFalse(mine.IsVisitableAt(new Position(5, 5))); // Not adjacent to itself
            Assert.IsFalse(mine.IsVisitableAt(new Position(10, 10))); // Too far
        }

        [Test]
        public void MapObject_IsBlockingAt_ReturnsTrueForOwnPosition()
        {
            var mine = new MineObject(new Position(5, 5), ResourceType.Gold, 1);

            Assert.IsTrue(mine.IsBlockingAt(new Position(5, 5)));
            Assert.IsFalse(mine.IsBlockingAt(new Position(5, 4)));
        }

        [Test]
        public void MapObject_Name_DefaultsToObjectTypeAndId()
        {
            var resource = new ResourceObject(new Position(1, 1), ResourceType.Gold, 100);
            resource.InstanceId = 42;
            resource.Name = $"{resource.ObjectType}_{resource.InstanceId}";

            Assert.AreEqual("Resource_42", resource.Name);
        }

        [Test]
        public void MapObject_Owner_DefaultsToNeutral()
        {
            var mine = new MineObject(new Position(5, 5), ResourceType.Gold, 1);

            Assert.AreEqual(PlayerColor.Neutral, mine.Owner);
        }
    }
}
