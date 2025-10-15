using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Map;

namespace RealmsOfEldor.Tests
{
    public class MapTileTests
    {
        [Test]
        public void MapTile_Constructor_InitializesCorrectly()
        {
            var tile = new MapTile(TerrainType.Grass);

            Assert.AreEqual(TerrainType.Grass, tile.Terrain);
            Assert.AreEqual(100, tile.MovementCost);
            Assert.IsTrue(tile.IsPassable);
            Assert.IsFalse(tile.IsBlocked);
            Assert.IsFalse(tile.IsVisitable);
        }

        [Test]
        public void MapTile_WaterTerrain_IsWaterReturnsTrue()
        {
            var tile = new MapTile(TerrainType.Water);

            Assert.IsTrue(tile.IsWater);
            Assert.IsFalse(tile.IsLand);
        }

        [Test]
        public void MapTile_LandTerrain_IsLandReturnsTrue()
        {
            var tile = new MapTile(TerrainType.Grass);

            Assert.IsTrue(tile.IsLand);
            Assert.IsFalse(tile.IsWater);
        }

        [Test]
        public void MapTile_RockTerrain_IsNotPassable()
        {
            var tile = new MapTile(TerrainType.Rock);

            Assert.IsFalse(tile.IsPassable);
            Assert.AreEqual(int.MaxValue, tile.MovementCost);
        }

        [Test]
        public void MapTile_SwampTerrain_HasHigherMovementCost()
        {
            var swampTile = new MapTile(TerrainType.Swamp);
            var grassTile = new MapTile(TerrainType.Grass);

            Assert.Greater(swampTile.MovementCost, grassTile.MovementCost);
            Assert.AreEqual(175, swampTile.MovementCost);
        }

        [Test]
        public void MapTile_AddVisitableObject_AddsObjectToList()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddVisitableObject(1);

            Assert.IsTrue(tile.IsVisitable);
            Assert.AreEqual(1, tile.TopVisitableObjectId);
        }

        [Test]
        public void MapTile_AddVisitableObject_NoDuplicates()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddVisitableObject(1);
            tile.AddVisitableObject(1);

            Assert.AreEqual(1, tile.VisitableObjectIds.Count);
        }

        [Test]
        public void MapTile_AddBlockingObject_BlocksTile()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddBlockingObject(1);

            Assert.IsTrue(tile.IsBlocked);
            Assert.IsFalse(tile.IsClear);
        }

        [Test]
        public void MapTile_RemoveVisitableObject_RemovesFromList()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddVisitableObject(1);
            tile.RemoveVisitableObject(1);

            Assert.IsFalse(tile.IsVisitable);
            Assert.AreEqual(-1, tile.TopVisitableObjectId);
        }

        [Test]
        public void MapTile_RemoveBlockingObject_UnblocksTile()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddBlockingObject(1);
            tile.RemoveBlockingObject(1);

            Assert.IsFalse(tile.IsBlocked);
            Assert.IsTrue(tile.IsClear);
        }

        [Test]
        public void MapTile_TopVisitableObjectId_ReturnsLastAdded()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddVisitableObject(1);
            tile.AddVisitableObject(2);
            tile.AddVisitableObject(3);

            Assert.AreEqual(3, tile.TopVisitableObjectId);
        }

        [Test]
        public void MapTile_TopVisitableObjectId_NoObjects_ReturnsMinusOne()
        {
            var tile = new MapTile(TerrainType.Grass);

            Assert.AreEqual(-1, tile.TopVisitableObjectId);
        }

        [Test]
        public void MapTile_CoastalFlag_CanBeSetAndRead()
        {
            var tile = new MapTile(TerrainType.Grass);
            Assert.IsFalse(tile.IsCoastal);

            tile.IsCoastal = true;
            Assert.IsTrue(tile.IsCoastal);

            tile.IsCoastal = false;
            Assert.IsFalse(tile.IsCoastal);
        }

        [Test]
        public void MapTile_IsClear_PassableAndNotBlocked()
        {
            var tile = new MapTile(TerrainType.Grass);
            Assert.IsTrue(tile.IsClear);

            tile.AddBlockingObject(1);
            Assert.IsFalse(tile.IsClear);
        }

        [Test]
        public void MapTile_IsClear_ImpassableTerrain_ReturnsFalse()
        {
            var tile = new MapTile(TerrainType.Rock);
            Assert.IsFalse(tile.IsClear);
        }
    }
}
