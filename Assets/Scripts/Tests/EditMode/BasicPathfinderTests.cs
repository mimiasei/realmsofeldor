using NUnit.Framework;
using System.Collections.Generic;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests
{
    [TestFixture]
    public class BasicPathfinderTests
    {
        private GameMap map;
        private const int MapWidth = 10;
        private const int MapHeight = 10;

        [SetUp]
        public void SetUp()
        {
            map = new GameMap(MapWidth, MapHeight);
        }

        // ===== IsAdjacent Tests =====

        [Test]
        public void IsAdjacent_SamePosition_ReturnsFalse()
        {
            var pos = new Position(5, 5);
            Assert.IsFalse(BasicPathfinder.IsAdjacent(pos, pos));
        }

        [Test]
        public void IsAdjacent_CardinalDirections_ReturnsTrue()
        {
            var center = new Position(5, 5);
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(5, 4))); // N
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(5, 6))); // S
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(4, 5))); // W
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(6, 5))); // E
        }

        [Test]
        public void IsAdjacent_DiagonalDirections_ReturnsTrue()
        {
            var center = new Position(5, 5);
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(4, 4))); // NW
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(6, 4))); // NE
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(4, 6))); // SW
            Assert.IsTrue(BasicPathfinder.IsAdjacent(center, new Position(6, 6))); // SE
        }

        [Test]
        public void IsAdjacent_TwoTilesAway_ReturnsFalse()
        {
            var center = new Position(5, 5);
            Assert.IsFalse(BasicPathfinder.IsAdjacent(center, new Position(5, 3))); // 2 tiles N
            Assert.IsFalse(BasicPathfinder.IsAdjacent(center, new Position(7, 5))); // 2 tiles E
            Assert.IsFalse(BasicPathfinder.IsAdjacent(center, new Position(3, 3))); // 2 tiles diagonally
        }

        // ===== GetAdjacentPositions Tests =====

        [Test]
        public void GetAdjacentPositions_ReturnsEightPositions()
        {
            var center = new Position(5, 5);
            var adjacent = BasicPathfinder.GetAdjacentPositions(center);

            Assert.AreEqual(8, adjacent.Count);
        }

        [Test]
        public void GetAdjacentPositions_ContainsAllDirections()
        {
            var center = new Position(5, 5);
            var adjacent = BasicPathfinder.GetAdjacentPositions(center);

            // Check all 8 directions are present
            Assert.Contains(new Position(4, 4), adjacent); // NW
            Assert.Contains(new Position(5, 4), adjacent); // N
            Assert.Contains(new Position(6, 4), adjacent); // NE
            Assert.Contains(new Position(4, 5), adjacent); // W
            Assert.Contains(new Position(6, 5), adjacent); // E
            Assert.Contains(new Position(4, 6), adjacent); // SW
            Assert.Contains(new Position(5, 6), adjacent); // S
            Assert.Contains(new Position(6, 6), adjacent); // SE
        }

        [Test]
        public void GetAdjacentPositions_EdgePosition_ReturnsEight()
        {
            // Even at edge, still returns 8 positions (some may be out of bounds)
            var edge = new Position(0, 0);
            var adjacent = BasicPathfinder.GetAdjacentPositions(edge);

            Assert.AreEqual(8, adjacent.Count);
        }

        // ===== FindPath Tests =====

        [Test]
        public void FindPath_NullMap_ReturnsNull()
        {
            var path = BasicPathfinder.FindPath(null, new Position(0, 0), new Position(1, 1));
            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_OutOfBoundsStart_ReturnsNull()
        {
            var path = BasicPathfinder.FindPath(map, new Position(-1, 0), new Position(1, 1));
            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_OutOfBoundsEnd_ReturnsNull()
        {
            var path = BasicPathfinder.FindPath(map, new Position(0, 0), new Position(100, 100));
            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_SamePosition_ReturnsSingleStepPath()
        {
            var pos = new Position(5, 5);
            var path = BasicPathfinder.FindPath(map, pos, pos);

            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(pos, path[0]);
        }

        [Test]
        public void FindPath_AdjacentPassableTile_ReturnsTwoStepPath()
        {
            var start = new Position(5, 5);
            var end = new Position(5, 6);

            var path = BasicPathfinder.FindPath(map, start, end);

            Assert.IsNotNull(path);
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[1]);
        }

        [Test]
        public void FindPath_NonAdjacentTile_ReturnsNull()
        {
            // MVP only supports adjacent movement
            var start = new Position(5, 5);
            var end = new Position(5, 7); // 2 tiles away

            var path = BasicPathfinder.FindPath(map, start, end);

            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_ImpassableTile_ReturnsNull()
        {
            var start = new Position(5, 5);
            var end = new Position(5, 6);

            // Block the target tile
            var tile = map.GetTile(end);
            tile = new MapTile(TerrainType.Rock, 0, 0); // Rock is impassable
            map.SetTile(end, tile);

            var path = BasicPathfinder.FindPath(map, start, end);

            Assert.IsNull(path);
        }

        // ===== CalculatePathCost Tests =====

        [Test]
        public void CalculatePathCost_NullMap_ReturnsZero()
        {
            var path = new List<Position> { new Position(0, 0), new Position(1, 1) };
            Assert.AreEqual(0, BasicPathfinder.CalculatePathCost(null, path));
        }

        [Test]
        public void CalculatePathCost_NullPath_ReturnsZero()
        {
            Assert.AreEqual(0, BasicPathfinder.CalculatePathCost(map, null));
        }

        [Test]
        public void CalculatePathCost_SingleStepPath_ReturnsZero()
        {
            var path = new List<Position> { new Position(5, 5) };
            Assert.AreEqual(0, BasicPathfinder.CalculatePathCost(map, path));
        }

        [Test]
        public void CalculatePathCost_TwoStepPath_ReturnsMovementCost()
        {
            var path = new List<Position> { new Position(5, 5), new Position(5, 6) };
            var cost = BasicPathfinder.CalculatePathCost(map, path);

            // Default grass terrain has movement cost
            Assert.Greater(cost, 0);
        }

        // ===== GetReachablePositions Tests =====

        [Test]
        public void GetReachablePositions_NullMap_ReturnsEmpty()
        {
            var reachable = BasicPathfinder.GetReachablePositions(null, new Position(5, 5), 100);
            Assert.IsEmpty(reachable);
        }

        [Test]
        public void GetReachablePositions_ZeroMovementPoints_ReturnsEmpty()
        {
            var reachable = BasicPathfinder.GetReachablePositions(map, new Position(5, 5), 0);
            Assert.IsEmpty(reachable);
        }

        [Test]
        public void GetReachablePositions_OutOfBounds_ReturnsEmpty()
        {
            var reachable = BasicPathfinder.GetReachablePositions(map, new Position(-1, -1), 100);
            Assert.IsEmpty(reachable);
        }

        [Test]
        public void GetReachablePositions_SufficientMovement_ReturnsAdjacentTiles()
        {
            var start = new Position(5, 5);
            var reachable = BasicPathfinder.GetReachablePositions(map, start, 1000);

            // Should return some adjacent passable tiles
            Assert.Greater(reachable.Count, 0);
            Assert.LessOrEqual(reachable.Count, 8); // Max 8 adjacent tiles
        }

        [Test]
        public void GetReachablePositions_EdgePosition_FiltersOutOfBounds()
        {
            var corner = new Position(0, 0);
            var reachable = BasicPathfinder.GetReachablePositions(map, corner, 1000);

            // Should only return in-bounds adjacent tiles (3 tiles from corner)
            Assert.LessOrEqual(reachable.Count, 3);

            foreach (var pos in reachable)
            {
                Assert.IsTrue(map.IsInBounds(pos));
            }
        }

        // ===== CanReachPosition Tests =====

        [Test]
        public void CanReachPosition_NullMap_ReturnsFalse()
        {
            var hero = new Hero { Id = 1, TypeId = 1, CustomName = "Test" };
            hero.Movement = 1000;

            Assert.IsFalse(BasicPathfinder.CanReachPosition(null, hero, new Position(1, 1)));
        }

        [Test]
        public void CanReachPosition_NullHero_ReturnsFalse()
        {
            Assert.IsFalse(BasicPathfinder.CanReachPosition(map, null, new Position(1, 1)));
        }

        [Test]
        public void CanReachPosition_NoMovementPoints_ReturnsFalse()
        {
            var hero = new Hero { Id = 1, TypeId = 1, CustomName = "Test" };
            hero.Position = new Position(5, 5);
            hero.Movement = 0;

            Assert.IsFalse(BasicPathfinder.CanReachPosition(map, hero, new Position(5, 6)));
        }

        [Test]
        public void CanReachPosition_AdjacentWithMovement_ReturnsTrue()
        {
            var hero = new Hero { Id = 1, TypeId = 1, CustomName = "Test" };
            hero.Position = new Position(5, 5);
            hero.Movement = 1000;

            Assert.IsTrue(BasicPathfinder.CanReachPosition(map, hero, new Position(5, 6)));
        }

        [Test]
        public void CanReachPosition_OutOfBounds_ReturnsFalse()
        {
            var hero = new Hero { Id = 1, TypeId = 1, CustomName = "Test" };
            hero.Position = new Position(5, 5);
            hero.Movement = 1000;

            Assert.IsFalse(BasicPathfinder.CanReachPosition(map, hero, new Position(100, 100)));
        }

        // ===== Distance Tests =====

        [Test]
        public void ManhattanDistance_SamePosition_ReturnsZero()
        {
            var pos = new Position(5, 5);
            Assert.AreEqual(0, BasicPathfinder.ManhattanDistance(pos, pos));
        }

        [Test]
        public void ManhattanDistance_Cardinal_ReturnsCorrectDistance()
        {
            var pos1 = new Position(5, 5);
            var pos2 = new Position(8, 7);

            // |8-5| + |7-5| = 3 + 2 = 5
            Assert.AreEqual(5, BasicPathfinder.ManhattanDistance(pos1, pos2));
        }

        [Test]
        public void ChebyshevDistance_SamePosition_ReturnsZero()
        {
            var pos = new Position(5, 5);
            Assert.AreEqual(0, BasicPathfinder.ChebyshevDistance(pos, pos));
        }

        [Test]
        public void ChebyshevDistance_Cardinal_ReturnsCorrectDistance()
        {
            var pos1 = new Position(5, 5);
            var pos2 = new Position(8, 7);

            // max(|8-5|, |7-5|) = max(3, 2) = 3
            Assert.AreEqual(3, BasicPathfinder.ChebyshevDistance(pos1, pos2));
        }

        [Test]
        public void ChebyshevDistance_Diagonal_ReturnsCorrectDistance()
        {
            var pos1 = new Position(0, 0);
            var pos2 = new Position(3, 3);

            // max(|3-0|, |3-0|) = max(3, 3) = 3
            Assert.AreEqual(3, BasicPathfinder.ChebyshevDistance(pos1, pos2));
        }

        // ===== GetNextStep Tests =====

        [Test]
        public void GetNextStep_NullMap_ReturnsNull()
        {
            var next = BasicPathfinder.GetNextStep(null, new Position(0, 0), new Position(1, 1));
            Assert.IsNull(next);
        }

        [Test]
        public void GetNextStep_SamePosition_ReturnsNull()
        {
            var pos = new Position(5, 5);
            var next = BasicPathfinder.GetNextStep(map, pos, pos);
            Assert.IsNull(next);
        }

        [Test]
        public void GetNextStep_AdjacentPassable_ReturnsTarget()
        {
            var start = new Position(5, 5);
            var target = new Position(5, 6);

            var next = BasicPathfinder.GetNextStep(map, start, target);

            Assert.IsNotNull(next);
            Assert.AreEqual(target, next.Value);
        }

        [Test]
        public void GetNextStep_AdjacentImpassable_ReturnsNull()
        {
            var start = new Position(5, 5);
            var target = new Position(5, 6);

            // Block target
            map.SetTile(target, new MapTile(TerrainType.Rock, 0, 0));

            var next = BasicPathfinder.GetNextStep(map, start, target);

            Assert.IsNull(next);
        }

        [Test]
        public void GetNextStep_NonAdjacent_ReturnsNull()
        {
            // MVP doesn't support multi-step pathfinding
            var start = new Position(5, 5);
            var target = new Position(5, 7);

            var next = BasicPathfinder.GetNextStep(map, start, target);

            Assert.IsNull(next);
        }
    }
}
