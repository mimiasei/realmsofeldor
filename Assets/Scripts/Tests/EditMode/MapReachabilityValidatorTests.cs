using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Tests
{
    public class MapReachabilityValidatorTests
    {
        [Test]
        public void FindReachableTiles_SimpleMap_FindsAllReachable()
        {
            // Create a 10x10 map with all grass (passable)
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(5, 5) };

            var reachable = validator.FindReachableTiles(startPositions);

            // All 100 tiles should be reachable
            Assert.AreEqual(100, reachable.Count);
        }

        [Test]
        public void FindReachableTiles_WithWaterBarrier_OnlyFindsAccessibleArea()
        {
            // Create a 10x10 map with a vertical water barrier down the middle
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (x == 5)
                        map.SetTerrain(new Position(x, y), TerrainType.Water);
                    else
                        map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(2, 5) };

            var reachable = validator.FindReachableTiles(startPositions);

            // Only left half should be reachable (5 columns * 10 rows = 50 tiles)
            Assert.AreEqual(50, reachable.Count);
            Assert.IsFalse(reachable.Contains(new Position(6, 5))); // Right side unreachable
            Assert.IsTrue(reachable.Contains(new Position(4, 5))); // Left side reachable
        }

        [Test]
        public void FindReachableTiles_WithMultipleStartPositions_FindsAllConnectedAreas()
        {
            // Create a 10x10 map with water barrier, but start positions on both sides
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (x == 5)
                        map.SetTerrain(new Position(x, y), TerrainType.Water);
                    else
                        map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(2, 5), new Position(7, 5) };

            var reachable = validator.FindReachableTiles(startPositions);

            // Both halves should be reachable (90 grass tiles)
            Assert.AreEqual(90, reachable.Count);
            Assert.IsTrue(reachable.Contains(new Position(2, 5))); // Left side
            Assert.IsTrue(reachable.Contains(new Position(7, 5))); // Right side
        }

        [Test]
        public void FindUnreachableObjects_AllReachable_ReturnsEmpty()
        {
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            // Add some objects
            map.AddObject(new ResourceObject(new Position(2, 2), ResourceType.Gold, 100));
            map.AddObject(new MineObject(new Position(7, 7), ResourceType.Ore, 1));

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(5, 5) };

            var unreachable = validator.FindUnreachableObjects(startPositions);

            Assert.AreEqual(0, unreachable.Count);
        }

        [Test]
        public void FindUnreachableObjects_ObjectBehindWater_ReturnsUnreachable()
        {
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (x == 5)
                        map.SetTerrain(new Position(x, y), TerrainType.Water);
                    else
                        map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            // Add object on left side (reachable) and right side (unreachable)
            map.AddObject(new ResourceObject(new Position(2, 2), ResourceType.Gold, 100));
            map.AddObject(new ResourceObject(new Position(7, 7), ResourceType.Gold, 100));

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(2, 5) };

            var unreachable = validator.FindUnreachableObjects(startPositions);

            Assert.AreEqual(1, unreachable.Count);
            Assert.AreEqual(new Position(7, 7), unreachable[0].Position);
        }

        [Test]
        public void RemoveUnreachableObjects_RemovesOrphanedObjects()
        {
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (x == 5)
                        map.SetTerrain(new Position(x, y), TerrainType.Water);
                    else
                        map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            // Add objects on both sides
            map.AddObject(new ResourceObject(new Position(2, 2), ResourceType.Gold, 100));
            map.AddObject(new ResourceObject(new Position(7, 7), ResourceType.Gold, 100));

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(2, 5) };

            var removed = validator.RemoveUnreachableObjects(startPositions);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(1, map.GetAllObjects().Count()); // Only reachable object remains
        }

        [Test]
        public void FixUnreachableObjects_RelocatesWhenPossible()
        {
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    // Create a small water lake with object inside
                    if ((x >= 4 && x <= 6) && (y >= 4 && y <= 6))
                        map.SetTerrain(new Position(x, y), TerrainType.Water);
                    else
                        map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            // Place object in center of water (unreachable)
            map.AddObject(new ResourceObject(new Position(5, 5), ResourceType.Gold, 100));

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(0, 0) };

            var result = validator.FixUnreachableObjects(startPositions, maxSearchRadius: 5);

            Assert.AreEqual(1, result.TotalUnreachableObjects);
            // Object should be relocated (not removed) since there's reachable space nearby
            Assert.Greater(result.ObjectsRelocated + result.ObjectsRemoved, 0);
        }

        [Test]
        public void CalculateStats_ReturnsCorrectStatistics()
        {
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (x == 5)
                        map.SetTerrain(new Position(x, y), TerrainType.Water); // 10 water tiles
                    else
                        map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate); // 90 grass tiles
                }
            }

            // Add objects on both sides
            map.AddObject(new ResourceObject(new Position(2, 2), ResourceType.Gold, 100));
            map.AddObject(new ResourceObject(new Position(7, 7), ResourceType.Gold, 100));

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(2, 5) };

            var stats = validator.CalculateStats(startPositions);

            Assert.AreEqual(100, stats.TotalTiles);
            Assert.AreEqual(90, stats.PassableTiles);
            Assert.AreEqual(50, stats.ReachableTiles); // Only left half
            Assert.AreEqual(40, stats.UnreachableTiles);
            Assert.AreEqual(2, stats.TotalObjects);
            Assert.AreEqual(1, stats.UnreachableObjects);
            Assert.AreEqual(0.5f / 0.9f, stats.ReachabilityPercentage, 0.01f); // 50/90 ≈ 55.5%
        }

        [Test]
        public void FindReachableTiles_DiagonalMovement_Works()
        {
            var map = new GameMap(5, 5);
            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(0, 0) };

            var reachable = validator.FindReachableTiles(startPositions);

            // Should reach diagonal corner
            Assert.IsTrue(reachable.Contains(new Position(4, 4)));
        }

        [Test]
        public void FindUnreachableObjects_VisitableObjectWithNoVisitPositions_IsUnreachable()
        {
            var map = new GameMap(10, 10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            // Surround mine with water (blocking all visitable positions)
            var minePos = new Position(5, 5);
            map.AddObject(new MineObject(minePos, ResourceType.Ore, 1));

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    map.SetTerrain(new Position(minePos.X + dx, minePos.Y + dy), TerrainType.Water);
                }
            }

            var validator = new MapReachabilityValidator(map);
            var startPositions = new List<Position> { new Position(0, 0) };

            var unreachable = validator.FindUnreachableObjects(startPositions);

            // Mine should be unreachable (no visitable positions)
            Assert.AreEqual(1, unreachable.Count);
        }
    }
}
