using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Map;
using System.Linq;

namespace RealmsOfEldor.Tests
{
    public class MapObjectTests
    {
        [Test]
        public void ResourceObject_Constructor_InitializesCorrectly()
        {
            var pos = new Position(5, 5);
            var resource = new ResourceObject(pos, ResourceType.Gold, 500);

            Assert.AreEqual(MapObjectType.Resource, resource.ObjectType);
            Assert.AreEqual(pos, resource.Position);
            Assert.AreEqual(ResourceType.Gold, resource.ResourceType);
            Assert.AreEqual(500, resource.Amount);
            Assert.IsFalse(resource.BlocksMovement);
            Assert.IsTrue(resource.IsVisitable);
            Assert.IsFalse(resource.BlockedVisitable);
            Assert.IsTrue(resource.IsRemovable);
        }

        [Test]
        public void ResourceObject_GetBlockedPositions_ReturnsEmptySet()
        {
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 500);
            var blocked = resource.GetBlockedPositions();

            Assert.AreEqual(0, blocked.Count);
        }

        [Test]
        public void MineObject_Constructor_InitializesCorrectly()
        {
            var pos = new Position(10, 10);
            var mine = new MineObject(pos, ResourceType.Ore, 2);

            Assert.AreEqual(MapObjectType.Mine, mine.ObjectType);
            Assert.AreEqual(pos, mine.Position);
            Assert.AreEqual(ResourceType.Ore, mine.ResourceType);
            Assert.AreEqual(2, mine.DailyProduction);
            Assert.IsTrue(mine.BlocksMovement);
            Assert.IsTrue(mine.IsVisitable);
            Assert.IsTrue(mine.BlockedVisitable);
            Assert.IsFalse(mine.IsRemovable);
        }

        [Test]
        public void MineObject_GetBlockedPositions_ReturnsSingleTile()
        {
            var pos = new Position(10, 10);
            var mine = new MineObject(pos, ResourceType.Ore, 2);
            var blocked = mine.GetBlockedPositions();

            Assert.AreEqual(1, blocked.Count);
            Assert.IsTrue(blocked.Contains(pos));
        }

        [Test]
        public void MineObject_GetVisitablePositions_ReturnsAdjacentTiles()
        {
            var pos = new Position(10, 10);
            var mine = new MineObject(pos, ResourceType.Ore, 2);
            var visitable = mine.GetVisitablePositions();

            // Should have 8 adjacent tiles
            Assert.AreEqual(8, visitable.Count);

            // Check all cardinal and diagonal directions
            Assert.IsTrue(visitable.Contains(new Position(10, 11))); // N
            Assert.IsTrue(visitable.Contains(new Position(11, 10))); // E
            Assert.IsTrue(visitable.Contains(new Position(10, 9)));  // S
            Assert.IsTrue(visitable.Contains(new Position(9, 10)));  // W
            Assert.IsTrue(visitable.Contains(new Position(11, 11))); // NE
            Assert.IsTrue(visitable.Contains(new Position(11, 9)));  // SE
            Assert.IsTrue(visitable.Contains(new Position(9, 9)));   // SW
            Assert.IsTrue(visitable.Contains(new Position(9, 11)));  // NW
        }

        [Test]
        public void DwellingObject_Constructor_InitializesCorrectly()
        {
            var pos = new Position(15, 15);
            var dwelling = new DwellingObject(pos, creatureId: 5, initialCount: 10, weeklyGrowth: 3);

            Assert.AreEqual(MapObjectType.Dwelling, dwelling.ObjectType);
            Assert.AreEqual(pos, dwelling.Position);
            Assert.AreEqual(5, dwelling.CreatureId);
            Assert.AreEqual(10, dwelling.AvailableCount);
            Assert.AreEqual(3, dwelling.WeeklyGrowth);
            Assert.IsTrue(dwelling.BlocksMovement);
            Assert.IsTrue(dwelling.IsVisitable);
            Assert.IsTrue(dwelling.BlockedVisitable);
        }

        [Test]
        public void DwellingObject_AddWeeklyGrowth_IncreasesAvailableCount()
        {
            var dwelling = new DwellingObject(new Position(15, 15), creatureId: 5, initialCount: 10, weeklyGrowth: 3);

            dwelling.AddWeeklyGrowth();
            Assert.AreEqual(13, dwelling.AvailableCount);

            dwelling.AddWeeklyGrowth();
            Assert.AreEqual(16, dwelling.AvailableCount);
        }

        [Test]
        public void MapObject_SetOwner_ChangesOwnership()
        {
            var mine = new MineObject(new Position(10, 10), ResourceType.Ore, 2);
            Assert.AreEqual(PlayerColor.Neutral, mine.Owner);

            mine.SetOwner(PlayerColor.Red);
            Assert.AreEqual(PlayerColor.Red, mine.Owner);
        }

        [Test]
        public void MapObject_IsVisitableAt_ReturnsTrueForVisitablePosition()
        {
            var mine = new MineObject(new Position(10, 10), ResourceType.Ore, 2);

            // Adjacent tile should be visitable
            Assert.IsTrue(mine.IsVisitableAt(new Position(10, 11)));
            Assert.IsTrue(mine.IsVisitableAt(new Position(11, 10)));

            // Mine's own position should not be visitable (blocked visitable)
            Assert.IsFalse(mine.IsVisitableAt(new Position(10, 10)));
        }

        [Test]
        public void MapObject_IsBlockingAt_ReturnsTrueForBlockedPosition()
        {
            var mine = new MineObject(new Position(10, 10), ResourceType.Ore, 2);

            Assert.IsTrue(mine.IsBlockingAt(new Position(10, 10)));
            Assert.IsFalse(mine.IsBlockingAt(new Position(10, 11)));
        }

        [Test]
        public void ResourceObject_IsVisitableAt_TrueForOwnPosition()
        {
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 500);

            // Resource allows standing on it (not blocked visitable)
            Assert.IsTrue(resource.IsVisitableAt(new Position(5, 5)));
        }

        [Test]
        public void MapObject_InstanceName_CanBeSet()
        {
            var mine = new MineObject(new Position(10, 10), ResourceType.Ore, 2);
            Assert.AreEqual(string.Empty, mine.InstanceName);

            mine.InstanceName = "Abandoned Mine";
            Assert.AreEqual("Abandoned Mine", mine.InstanceName);
        }
    }
}
