using NUnit.Framework;
using RealmsOfEldor.Core;
using System;
using System.Linq;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests
{
    public class GameMapTests
    {
        [Test]
        public void Constructor_InitializesMapCorrectly()
        {
            var map = new GameMap(10, 15);

            Assert.AreEqual(10, map.Width);
            Assert.AreEqual(15, map.Height);
        }

        [Test]
        public void Constructor_ThrowsExceptionForInvalidDimensions()
        {
            Assert.Throws<ArgumentException>(() => new GameMap(0, 10));
            Assert.Throws<ArgumentException>(() => new GameMap(10, 0));
            Assert.Throws<ArgumentException>(() => new GameMap(-5, 10));
        }

        [Test]
        public void Constructor_InitializesAllTilesWithGrass()
        {
            var map = new GameMap(5, 5);

            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    var tile = map.GetTile(new Position(x, y));
                    Assert.AreEqual(TerrainType.GrassTemperate, tile.Terrain);
                }
            }
        }

        [Test]
        public void IsInBounds_ReturnsTrueForValidPositions()
        {
            var map = new GameMap(10, 10);

            Assert.IsTrue(map.IsInBounds(new Position(0, 0)));
            Assert.IsTrue(map.IsInBounds(new Position(5, 5)));
            Assert.IsTrue(map.IsInBounds(new Position(9, 9)));
        }

        [Test]
        public void IsInBounds_ReturnsFalseForInvalidPositions()
        {
            var map = new GameMap(10, 10);

            Assert.IsFalse(map.IsInBounds(new Position(-1, 0)));
            Assert.IsFalse(map.IsInBounds(new Position(0, -1)));
            Assert.IsFalse(map.IsInBounds(new Position(10, 0)));
            Assert.IsFalse(map.IsInBounds(new Position(0, 10)));
            Assert.IsFalse(map.IsInBounds(new Position(15, 15)));
        }

        [Test]
        public void GetTile_ReturnsCorrectTile()
        {
            var map = new GameMap(10, 10);
            var tile = map.GetTile(new Position(3, 5));

            Assert.AreEqual(TerrainType.GrassTemperate, tile.Terrain);
        }

        [Test]
        public void GetTile_ThrowsExceptionForOutOfBounds()
        {
            var map = new GameMap(10, 10);

            Assert.Throws<ArgumentOutOfRangeException>(() => map.GetTile(new Position(-1, 5)));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.GetTile(new Position(10, 5)));
        }

        [Test]
        public void SetTile_UpdatesTile()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(3, 5);
            var newTile = new MapTile(TerrainType.SandTemperate, 1, 150);

            map.SetTile(pos, newTile);
            var retrievedTile = map.GetTile(pos);

            Assert.AreEqual(TerrainType.SandTemperate, retrievedTile.Terrain);
            Assert.AreEqual(1, retrievedTile.VisualVariant);
            Assert.AreEqual(150, retrievedTile.MovementCost);
        }

        [Test]
        public void SetTerrain_ChangesTerrain()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(4, 6);

            map.SetTerrain(pos, TerrainType.Water, 2, 200);
            var tile = map.GetTile(pos);

            Assert.AreEqual(TerrainType.Water, tile.Terrain);
            Assert.AreEqual(2, tile.VisualVariant);
            Assert.AreEqual(200, tile.MovementCost);
        }

        [Test]
        public void AddObject_AssignsIdAndAddsToMap()
        {
            var map = new GameMap(10, 10);
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 100);

            var id = map.AddObject(resource);

            Assert.AreEqual(1, id);
            Assert.AreEqual(1, resource.InstanceId);
            Assert.IsNotNull(map.GetObject(id));
        }

        [Test]
        public void AddObject_ThrowsExceptionForNull()
        {
            var map = new GameMap(10, 10);

            Assert.Throws<ArgumentNullException>(() => map.AddObject(null));
        }

        [Test]
        public void AddObject_UpdatesTileWithObjectReferences()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);
            var mine = new MineObject(pos, ResourceType.Ore, 1);

            map.AddObject(mine);
            var tile = map.GetTile(pos);

            Assert.IsTrue(tile.HasBlockingObject(mine.InstanceId));
            Assert.IsTrue(tile.HasVisitableObject(mine.InstanceId));
        }

        [Test]
        public void AddObject_IncrementalIdAssignment()
        {
            var map = new GameMap(10, 10);
            var obj1 = new ResourceObject(new Position(1, 1), ResourceType.Gold, 100);
            var obj2 = new ResourceObject(new Position(2, 2), ResourceType.Wood, 10);
            var obj3 = new MineObject(new Position(3, 3), ResourceType.Ore, 1);

            var id1 = map.AddObject(obj1);
            var id2 = map.AddObject(obj2);
            var id3 = map.AddObject(obj3);

            Assert.AreEqual(1, id1);
            Assert.AreEqual(2, id2);
            Assert.AreEqual(3, id3);
        }

        [Test]
        public void RemoveObject_RemovesObjectFromMap()
        {
            var map = new GameMap(10, 10);
            var resource = new ResourceObject(new Position(5, 5), ResourceType.Gold, 100);
            var id = map.AddObject(resource);

            map.RemoveObject(id);

            Assert.IsNull(map.GetObject(id));
        }

        [Test]
        public void RemoveObject_UpdatesTileReferences()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);
            var mine = new MineObject(pos, ResourceType.Ore, 1);
            var id = map.AddObject(mine);

            map.RemoveObject(id);
            var tile = map.GetTile(pos);

            Assert.IsFalse(tile.HasBlockingObject(id));
            Assert.IsFalse(tile.HasVisitableObject(id));
        }

        [Test]
        public void GetObject_ReturnsNullForInvalidId()
        {
            var map = new GameMap(10, 10);

            Assert.IsNull(map.GetObject(999));
        }

        [Test]
        public void GetObjectsAt_ReturnsObjectsAtPosition()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);
            var mine = new MineObject(pos, ResourceType.Gold, 1);
            map.AddObject(mine);

            var objects = map.GetObjectsAt(pos);

            Assert.AreEqual(1, objects.Count);
            Assert.AreEqual(mine.InstanceId, objects[0].InstanceId);
        }

        [Test]
        public void GetObjectsAt_ReturnsEmptyListForOutOfBounds()
        {
            var map = new GameMap(10, 10);
            var objects = map.GetObjectsAt(new Position(-1, -1));

            Assert.AreEqual(0, objects.Count);
        }

        [Test]
        public void GetObjectsByType_ReturnsCorrectObjects()
        {
            var map = new GameMap(10, 10);
            map.AddObject(new ResourceObject(new Position(1, 1), ResourceType.Gold, 100));
            map.AddObject(new MineObject(new Position(2, 2), ResourceType.Ore, 1));
            map.AddObject(new ResourceObject(new Position(3, 3), ResourceType.Wood, 50));

            var resources = map.GetObjectsByType(MapObjectType.Resource);
            var mines = map.GetObjectsByType(MapObjectType.Mine);

            Assert.AreEqual(2, resources.Count);
            Assert.AreEqual(1, mines.Count);
        }

        [Test]
        public void GetObjectsOfClass_ReturnsCorrectType()
        {
            var map = new GameMap(10, 10);
            map.AddObject(new ResourceObject(new Position(1, 1), ResourceType.Gold, 100));
            map.AddObject(new MineObject(new Position(2, 2), ResourceType.Ore, 1));
            map.AddObject(new DwellingObject(new Position(3, 3), 1, 5));

            var mines = map.GetObjectsOfClass<MineObject>();
            var dwellings = map.GetObjectsOfClass<DwellingObject>();

            Assert.AreEqual(1, mines.Count);
            Assert.AreEqual(1, dwellings.Count);
            Assert.IsInstanceOf<MineObject>(mines[0]);
            Assert.IsInstanceOf<DwellingObject>(dwellings[0]);
        }

        [Test]
        public void CanMoveBetween_ReturnsTrueForPassableTile()
        {
            var map = new GameMap(10, 10);

            Assert.IsTrue(map.CanMoveBetween(new Position(0, 0), new Position(1, 1)));
        }

        [Test]
        public void CanMoveBetween_ReturnsFalseForImpassableTile()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);
            map.SetTerrain(pos, TerrainType.Water); // Water is impassable

            Assert.IsFalse(map.CanMoveBetween(new Position(4, 4), pos));
        }

        [Test]
        public void CanMoveBetween_ReturnsFalseForBlockedTile()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);
            var mine = new MineObject(pos, ResourceType.Gold, 1);
            map.AddObject(mine);

            Assert.IsFalse(map.CanMoveBetween(new Position(4, 4), pos));
        }

        [Test]
        public void CanMoveBetween_ReturnsFalseForOutOfBounds()
        {
            var map = new GameMap(10, 10);

            Assert.IsFalse(map.CanMoveBetween(new Position(0, 0), new Position(-1, 0)));
            Assert.IsFalse(map.CanMoveBetween(new Position(0, 0), new Position(10, 10)));
        }

        [Test]
        public void GetMovementCost_ReturnsCorrectCost()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);
            map.SetTerrain(pos, TerrainType.SandTemperate, 0, 150);

            var cost = map.GetMovementCost(new Position(4, 4), pos);

            Assert.AreEqual(150, cost);
        }

        [Test]
        public void GetMovementCost_ReturnsMaxValueForImpassable()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);
            map.SetTerrain(pos, TerrainType.Water); // Water is impassable

            var cost = map.GetMovementCost(new Position(4, 4), pos);

            Assert.AreEqual(int.MaxValue, cost);
        }

        [Test]
        public void GetAdjacentPositions_Returns8Positions()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(5, 5);

            var adjacent = map.GetAdjacentPositions(pos);

            Assert.AreEqual(8, adjacent.Count);
        }

        [Test]
        public void GetAdjacentPositions_ExcludesOutOfBounds()
        {
            var map = new GameMap(10, 10);
            var pos = new Position(0, 0);

            var adjacent = map.GetAdjacentPositions(pos);

            Assert.AreEqual(3, adjacent.Count); // Only 3 valid adjacent positions at corner
        }

        [Test]
        public void CalculateCoastalTiles_MarksLandAdjacentToWater()
        {
            var map = new GameMap(10, 10);
            map.SetTerrain(new Position(5, 5), TerrainType.Water);

            map.CalculateCoastalTiles();

            // Adjacent land tiles should be coastal
            var tile1 = map.GetTile(new Position(4, 5));
            var tile2 = map.GetTile(new Position(6, 5));
            Assert.IsTrue(tile1.IsCoastal);
            Assert.IsTrue(tile2.IsCoastal);

            // Distant tiles should not be coastal
            var tile3 = map.GetTile(new Position(0, 0));
            Assert.IsFalse(tile3.IsCoastal);
        }

        [Test]
        public void ApplyWeeklyGrowth_IncreasesCreaturesInDwellings()
        {
            var map = new GameMap(10, 10);
            var dwelling1 = new DwellingObject(new Position(1, 1), 1, 5);
            var dwelling2 = new DwellingObject(new Position(2, 2), 2, 3);
            map.AddObject(dwelling1);
            map.AddObject(dwelling2);

            Assert.AreEqual(5, dwelling1.AvailableCreatures);
            Assert.AreEqual(3, dwelling2.AvailableCreatures);

            map.ApplyWeeklyGrowth();

            Assert.AreEqual(10, dwelling1.AvailableCreatures);
            Assert.AreEqual(6, dwelling2.AvailableCreatures);
        }

        [Test]
        public void GetAllObjects_ReturnsAllObjects()
        {
            var map = new GameMap(10, 10);
            map.AddObject(new ResourceObject(new Position(1, 1), ResourceType.Gold, 100));
            map.AddObject(new MineObject(new Position(2, 2), ResourceType.Ore, 1));
            map.AddObject(new DwellingObject(new Position(3, 3), 1, 5));

            var allObjects = map.GetAllObjects().ToList();

            Assert.AreEqual(3, allObjects.Count);
        }
    }
}
