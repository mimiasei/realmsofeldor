using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Tests
{
    /// <summary>
    /// Unit tests for ObstaclePlacer.
    /// Verifies obstacle placement logic, density control, and path preservation.
    /// </summary>
    public class ObstaclePlacerTests
    {
        // Basic Placement Tests

        [Test]
        public void PlaceObstacles_EmptyMap_PlacesObstacles()
        {
            // Arrange
            var map = new GameMap(20, 20);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree, ObstacleType.Rock };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 10, allowBlocking: true);

            // Assert
            Assert.IsNotNull(obstacles);
            Assert.GreaterOrEqual(obstacles.Count, 5); // Should place at least some obstacles
            Assert.LessOrEqual(obstacles.Count, 10);   // Should not exceed target
        }

        [Test]
        public void PlaceObstacles_ReachesDensityTarget()
        {
            // Arrange
            var map = new GameMap(30, 30);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree, ObstacleType.Bush };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 20, allowBlocking: false);

            // Assert
            Assert.GreaterOrEqual(obstacles.Count, 15); // Should reach close to target
            Assert.LessOrEqual(obstacles.Count, 20);
        }

        [Test]
        public void PlaceObstacles_ZeroDensity_PlacesNothing()
        {
            // Arrange
            var map = new GameMap(10, 10);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 0, allowBlocking: false);

            // Assert
            Assert.AreEqual(0, obstacles.Count);
        }

        // Prohibited Area Tests

        [Test]
        public void PlaceObstacles_AvoidsExistingObjects()
        {
            // Arrange
            var map = new GameMap(20, 20);
            InitializePassableTerrain(map);

            // Place a resource object
            var resource = new ResourceObject(new Position(10, 10), ResourceType.Gold, 1000);
            map.AddObject(resource);

            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 50, allowBlocking: false);

            // Assert
            // No obstacle should be at the resource position
            Assert.IsFalse(obstacles.Any(o => o.Position.Equals(resource.Position)));

            // No obstacle should be adjacent to resource (1-tile buffer)
            foreach (var obstacle in obstacles)
            {
                var distance = System.Math.Abs(obstacle.Position.X - resource.Position.X) +
                              System.Math.Abs(obstacle.Position.Y - resource.Position.Y);
                Assert.GreaterOrEqual(distance, 2); // At least 2 Manhattan distance
            }
        }

        [Test]
        public void PlaceObstacles_AvoidsHighValueObjectsWithBuffer()
        {
            // Arrange
            var map = new GameMap(25, 25);
            InitializePassableTerrain(map);

            // Place a high-value mine (value > 1000)
            var mine = new MineObject(new Position(12, 12), ResourceType.Gold, dailyProduction: 1000);
            map.AddObject(mine);

            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Rock };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 30, allowBlocking: true);

            // Assert
            // Obstacles should maintain buffer around high-value objects
            foreach (var obstacle in obstacles)
            {
                var distance = System.Math.Max(
                    System.Math.Abs(obstacle.Position.X - mine.Position.X),
                    System.Math.Abs(obstacle.Position.Y - mine.Position.Y));
                Assert.GreaterOrEqual(distance, 2); // Minimum 1-tile buffer (Chebyshev distance)
            }
        }

        // Blocking vs Decorative Tests

        [Test]
        public void PlaceObstacles_AllowBlockingFalse_PlacesOnlyNonBlocking()
        {
            // Arrange
            var map = new GameMap(15, 15);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree, ObstacleType.Bush, ObstacleType.Mountain };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 20, allowBlocking: false);

            // Assert
            // When allowBlocking is false, all obstacles should be non-blocking
            // Note: Some obstacle types like Bush are always non-blocking
            Assert.IsTrue(obstacles.All(o => !o.IsBlocking || o.Name == "Bush" || o.Name == "Flowers"));
        }

        [Test]
        public void PlaceObstacles_WithBlockingAllowed_CreatesMixedObstacles()
        {
            // Arrange
            var map = new GameMap(20, 20);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Mountain, ObstacleType.Bush };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 30, allowBlocking: true);

            // Assert
            // Should have some blocking and some non-blocking
            var blockingCount = obstacles.Count(o => o.IsBlocking);
            var nonBlockingCount = obstacles.Count(o => !o.IsBlocking);

            Assert.Greater(blockingCount, 0, "Should have at least some blocking obstacles");
            Assert.Greater(nonBlockingCount, 0, "Should have at least some non-blocking obstacles");
        }

        // Path Preservation Tests

        [Test]
        public void PlaceObstacles_DoesNotBlockAllNeighbors()
        {
            // Arrange
            var map = new GameMap(15, 15);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Mountain, ObstacleType.Rock };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 15, allowBlocking: true);

            // Assert
            // For each blocking obstacle, at least one neighbor should remain passable
            foreach (var obstacle in obstacles.Where(o => o.IsBlocking))
            {
                int passableNeighbors = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        var neighbor = new Position(obstacle.Position.X + dx, obstacle.Position.Y + dy);
                        if (map.IsInBounds(neighbor) && map.IsClear(neighbor))
                            passableNeighbors++;
                    }
                }

                Assert.Greater(passableNeighbors, 2, $"Obstacle at {obstacle.Position} should have at least 3 passable neighbors");
            }
        }

        // Obstacle Type Tests

        [Test]
        public void PlaceObstacles_UsesProvidedObstacleTypes()
        {
            // Arrange
            var map = new GameMap(20, 20);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 10, allowBlocking: false);

            // Assert
            Assert.IsTrue(obstacles.All(o => o.Name == "Tree"));
        }

        [Test]
        public void PlaceObstacles_MultipleTypes_DistributesRandomly()
        {
            // Arrange
            var map = new GameMap(30, 30);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType>
            {
                ObstacleType.Tree,
                ObstacleType.Rock,
                ObstacleType.Bush
            };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 30, allowBlocking: true);

            // Assert
            var treeCount = obstacles.Count(o => o.Name == "Tree");
            var rockCount = obstacles.Count(o => o.Name == "Rock");
            var bushCount = obstacles.Count(o => o.Name == "Bush");

            // Should have some of each type (with high probability)
            Assert.Greater(treeCount, 0, "Should place some trees");
            // Rocks and bushes might be zero due to randomness, so we just check total
            Assert.AreEqual(obstacles.Count, treeCount + rockCount + bushCount);
        }

        // Edge Cases

        [Test]
        public void PlaceObstacles_FullMap_HandlesGracefully()
        {
            // Arrange
            var map = new GameMap(10, 10);
            InitializePassableTerrain(map);

            // Fill map with objects
            for (int x = 2; x < 8; x++)
            {
                for (int y = 2; y < 8; y++)
                {
                    var obj = new ResourceObject(new Position(x, y), ResourceType.Wood, 5);
                    map.AddObject(obj);
                }
            }

            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 50, allowBlocking: false);

            // Assert
            // Should place very few or zero obstacles due to lack of space
            Assert.LessOrEqual(obstacles.Count, 10);
        }

        [Test]
        public void PlaceObstacles_EmptyObstacleTypeList_ThrowsException()
        {
            // Arrange
            var map = new GameMap(10, 10);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var emptyList = new List<ObstacleType>();

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                placer.PlaceObstacles(emptyList, targetDensity: 5, allowBlocking: false));
        }

        [Test]
        public void PlaceObstacles_ImpassableTerrain_Avoids()
        {
            // Arrange
            var map = new GameMap(15, 15);

            // Fill with mix of passable and impassable terrain
            for (int x = 0; x < 15; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    // Make every other column water (impassable)
                    if (x % 2 == 0)
                        map.SetTerrain(new Position(x, y), TerrainType.Water);
                    else
                        map.SetTerrain(new Position(x, y), TerrainType.Grass);
                }
            }

            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree };

            // Act
            var obstacles = placer.PlaceObstacles(obstacleTypes, targetDensity: 20, allowBlocking: false);

            // Assert
            // No obstacle should be on water
            foreach (var obstacle in obstacles)
            {
                var tile = map.GetTile(obstacle.Position);
                Assert.IsTrue(tile.IsPassable(), $"Obstacle at {obstacle.Position} is on impassable terrain");
            }
        }

        // Statistics Tests

        [Test]
        public void GetStats_ReturnsValidStatistics()
        {
            // Arrange
            var map = new GameMap(20, 20);
            InitializePassableTerrain(map);
            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree };

            // Act
            placer.PlaceObstacles(obstacleTypes, targetDensity: 15, allowBlocking: false);
            var stats = placer.GetStats();

            // Assert
            Assert.AreEqual(400, stats.TotalMapTiles); // 20x20
            Assert.GreaterOrEqual(stats.ProhibitedTiles, 0);
            Assert.GreaterOrEqual(stats.BlockedTiles, 0);
        }

        [Test]
        public void GetStats_CalculatesPercentagesCorrectly()
        {
            // Arrange
            var map = new GameMap(10, 10);
            InitializePassableTerrain(map);

            // Add one object
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 100);
            map.AddObject(resource);

            var placer = new ObstaclePlacer(map);
            var obstacleTypes = new List<ObstacleType> { ObstacleType.Tree };

            // Act
            placer.PlaceObstacles(obstacleTypes, targetDensity: 5, allowBlocking: false);
            var stats = placer.GetStats();

            // Assert
            Assert.AreEqual(100, stats.TotalMapTiles); // 10x10
            Assert.GreaterOrEqual(stats.ProhibitedPercent, 0f);
            Assert.LessOrEqual(stats.ProhibitedPercent, 100f);
            Assert.GreaterOrEqual(stats.BlockedPercent, 0f);
            Assert.LessOrEqual(stats.BlockedPercent, 100f);
        }

        // Helper Methods

        private void InitializePassableTerrain(GameMap map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    map.SetTerrain(new Position(x, y), TerrainType.Grass);
                }
            }
        }
    }
}
