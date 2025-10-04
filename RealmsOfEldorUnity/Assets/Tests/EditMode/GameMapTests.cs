using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Map;
using System;

namespace RealmsOfEldor.Tests
{
    public class GameMapTests
    {
        [Test]
        public void GameMap_Constructor_InitializesCorrectly()
        {
            var map = new GameMap(50, 50, "Test Map");

            Assert.AreEqual(50, map.Width);
            Assert.AreEqual(50, map.Height);
            Assert.AreEqual("Test Map", map.Name);
            Assert.IsEmpty(map.Description);
        }

        [Test]
        public void GameMap_Constructor_InvalidDimensions_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new GameMap(0, 50));
            Assert.Throws<ArgumentException>(() => new GameMap(50, 0));
            Assert.Throws<ArgumentException>(() => new GameMap(-10, -10));
        }

        [Test]
        public void GameMap_InitializeTerrain_AllTilesAreGrass()
        {
            var map = new GameMap(10, 10);

            for (var x = 0; x < 10; x++)
            {
                for (var y = 0; y < 10; y++)
                {
                    var tile = map.GetTile(new Position(x, y));
                    Assert.AreEqual(TerrainType.Grass, tile.Terrain);
                }
            }
        }

        [Test]
        public void GameMap_IsInBounds_ValidatesCorrectly()
        {
            var map = new GameMap(50, 50);

            Assert.IsTrue(map.IsInBounds(new Position(0, 0)));
            Assert.IsTrue(map.IsInBounds(new Position(49, 49)));
            Assert.IsTrue(map.IsInBounds(new Position(25, 25)));

            Assert.IsFalse(map.IsInBounds(new Position(-1, 0)));
            Assert.IsFalse(map.IsInBounds(new Position(0, -1)));
            Assert.IsFalse(map.IsInBounds(new Position(50, 0)));
            Assert.IsFalse(map.IsInBounds(new Position(0, 50)));
            Assert.IsFalse(map.IsInBounds(new Position(100, 100)));
        }

        [Test]
        public void GameMap_GetTile_OutOfBounds_ThrowsException()
        {
            var map = new GameMap(50, 50);

            Assert.Throws<ArgumentOutOfRangeException>(() => map.GetTile(new Position(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.GetTile(new Position(50, 50)));
        }

        [Test]
        public void GameMap_SetTerrain_ChangesTerrainType()
        {
            var map = new GameMap(50, 50);
            var pos = new Position(10, 10);

            map.SetTerrain(pos, TerrainType.Water);
            var tile = map.GetTile(pos);

            Assert.AreEqual(TerrainType.Water, tile.Terrain);
        }

        [Test]
        public void GameMap_AddObject_AssignsUniqueId()
        {
            var map = new GameMap(50, 50);
            var obj1 = new MineObject(new Position(10, 10), ResourceType.Ore, 2);
            var obj2 = new MineObject(new Position(20, 20), ResourceType.Gold, 1000);

            map.AddObject(obj1);
            map.AddObject(obj2);

            Assert.AreEqual(0, obj1.InstanceId);
            Assert.AreEqual(1, obj2.InstanceId);
        }

        [Test]
        public void GameMap_AddObject_UpdatesTileReferences()
        {
            var map = new GameMap(50, 50);
            var pos = new Position(10, 10);
            var mine = new MineObject(pos, ResourceType.Ore, 2);

            map.AddObject(mine);

            var tile = map.GetTile(pos);
            Assert.IsTrue(tile.IsBlocked);
            Assert.AreEqual(1, tile.BlockingObjectIds.Count);
            Assert.AreEqual(mine.InstanceId, tile.BlockingObjectIds[0]);
        }

        [Test]
        public void GameMap_AddObject_OutOfBounds_ThrowsException()
        {
            var map = new GameMap(50, 50);
            var obj = new MineObject(new Position(100, 100), ResourceType.Ore, 2);

            Assert.Throws<ArgumentException>(() => map.AddObject(obj));
        }

        [Test]
        public void GameMap_RemoveObject_RemovesFromMap()
        {
            var map = new GameMap(50, 50);
            var pos = new Position(10, 10);
            var mine = new MineObject(pos, ResourceType.Ore, 2);

            map.AddObject(mine);
            var removed = map.RemoveObject(mine.InstanceId);

            Assert.IsTrue(removed);
            Assert.IsNull(map.GetObject(mine.InstanceId));

            var tile = map.GetTile(pos);
            Assert.IsFalse(tile.IsBlocked);
        }

        [Test]
        public void GameMap_RemoveObject_InvalidId_ReturnsFalse()
        {
            var map = new GameMap(50, 50);
            var removed = map.RemoveObject(999);

            Assert.IsFalse(removed);
        }

        [Test]
        public void GameMap_GetObject_ReturnsCorrectObject()
        {
            var map = new GameMap(50, 50);
            var mine = new MineObject(new Position(10, 10), ResourceType.Ore, 2);

            map.AddObject(mine);
            var retrieved = map.GetObject(mine.InstanceId);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(mine, retrieved);
        }

        [Test]
        public void GameMap_GetObjectsAt_ReturnsObjectsOnTile()
        {
            var map = new GameMap(50, 50);
            var pos = new Position(10, 10);
            var mine = new MineObject(pos, ResourceType.Ore, 2);

            map.AddObject(mine);
            var objects = map.GetObjectsAt(pos);

            Assert.AreEqual(1, objects.Count);
            Assert.AreEqual(mine, objects[0]);
        }

        [Test]
        public void GameMap_GetObjectsByType_FiltersCorrectly()
        {
            var map = new GameMap(50, 50);
            var mine1 = new MineObject(new Position(10, 10), ResourceType.Ore, 2);
            var mine2 = new MineObject(new Position(20, 20), ResourceType.Gold, 1000);
            var resource = new ResourceObject(new Position(30, 30), ResourceType.Wood, 10);

            map.AddObject(mine1);
            map.AddObject(mine2);
            map.AddObject(resource);

            var mines = map.GetObjectsByType(MapObjectType.Mine);
            var resources = map.GetObjectsByType(MapObjectType.Resource);

            Assert.AreEqual(2, mines.Count);
            Assert.AreEqual(1, resources.Count);
        }

        [Test]
        public void GameMap_GetObjectsOfClass_FiltersCorrectly()
        {
            var map = new GameMap(50, 50);
            var mine = new MineObject(new Position(10, 10), ResourceType.Ore, 2);
            var resource = new ResourceObject(new Position(20, 20), ResourceType.Gold, 500);

            map.AddObject(mine);
            map.AddObject(resource);

            var mines = map.GetObjectsOfClass<MineObject>();
            var resources = map.GetObjectsOfClass<ResourceObject>();

            Assert.AreEqual(1, mines.Count);
            Assert.AreEqual(1, resources.Count);
            Assert.IsInstanceOf<MineObject>(mines[0]);
            Assert.IsInstanceOf<ResourceObject>(resources[0]);
        }

        [Test]
        public void GameMap_CanMoveBetween_ValidatesMovement()
        {
            var map = new GameMap(50, 50);
            var from = new Position(10, 10);
            var adjacent = new Position(10, 11);
            var farAway = new Position(20, 20);

            Assert.IsTrue(map.CanMoveBetween(from, adjacent));
            Assert.IsFalse(map.CanMoveBetween(from, farAway));
        }

        [Test]
        public void GameMap_CanMoveBetween_BlockedTile_ReturnsFalse()
        {
            var map = new GameMap(50, 50);
            var from = new Position(10, 10);
            var to = new Position(10, 11);
            var mine = new MineObject(to, ResourceType.Ore, 2);

            map.AddObject(mine);

            Assert.IsFalse(map.CanMoveBetween(from, to));
        }

        [Test]
        public void GameMap_CanMoveBetween_ImpassableTerrain_ReturnsFalse()
        {
            var map = new GameMap(50, 50);
            var from = new Position(10, 10);
            var to = new Position(10, 11);

            map.SetTerrain(to, TerrainType.Rock);

            Assert.IsFalse(map.CanMoveBetween(from, to));
        }

        [Test]
        public void GameMap_GetMovementCost_ReturnsCorrectCost()
        {
            var map = new GameMap(50, 50);
            var from = new Position(10, 10);
            var to = new Position(10, 11);

            map.SetTerrain(to, TerrainType.Swamp);

            var cost = map.GetMovementCost(from, to);
            Assert.AreEqual(175, cost);
        }

        [Test]
        public void GameMap_GetMovementCost_InvalidMovement_ReturnsMaxValue()
        {
            var map = new GameMap(50, 50);
            var from = new Position(10, 10);
            var to = new Position(10, 11);

            map.SetTerrain(to, TerrainType.Rock);

            var cost = map.GetMovementCost(from, to);
            Assert.AreEqual(int.MaxValue, cost);
        }

        [Test]
        public void GameMap_CalculateCoastalTiles_MarksAdjacentToWater()
        {
            var map = new GameMap(10, 10);

            // Create a water tile
            map.SetTerrain(new Position(5, 5), TerrainType.Water);
            map.CalculateCoastalTiles();

            // Check adjacent tiles are coastal
            Assert.IsTrue(map.GetTile(new Position(4, 5)).IsCoastal);
            Assert.IsTrue(map.GetTile(new Position(6, 5)).IsCoastal);
            Assert.IsTrue(map.GetTile(new Position(5, 4)).IsCoastal);
            Assert.IsTrue(map.GetTile(new Position(5, 6)).IsCoastal);

            // Check non-adjacent tiles are not coastal
            Assert.IsFalse(map.GetTile(new Position(0, 0)).IsCoastal);
        }

        [Test]
        public void GameMap_GetAdjacentPositions_ReturnsEightNeighbors()
        {
            var map = new GameMap(50, 50);
            var pos = new Position(25, 25);

            var adjacent = map.GetAdjacentPositions(pos);

            Assert.AreEqual(8, adjacent.Count);
            Assert.Contains(new Position(24, 24), adjacent);
            Assert.Contains(new Position(25, 24), adjacent);
            Assert.Contains(new Position(26, 24), adjacent);
            Assert.Contains(new Position(24, 25), adjacent);
            Assert.Contains(new Position(26, 25), adjacent);
            Assert.Contains(new Position(24, 26), adjacent);
            Assert.Contains(new Position(25, 26), adjacent);
            Assert.Contains(new Position(26, 26), adjacent);
        }

        [Test]
        public void GameMap_GetAdjacentPositions_EdgeTile_ReturnsOnlyValidPositions()
        {
            var map = new GameMap(50, 50);
            var pos = new Position(0, 0);

            var adjacent = map.GetAdjacentPositions(pos);

            Assert.AreEqual(3, adjacent.Count);
            Assert.Contains(new Position(1, 0), adjacent);
            Assert.Contains(new Position(0, 1), adjacent);
            Assert.Contains(new Position(1, 1), adjacent);
        }
    }
}
