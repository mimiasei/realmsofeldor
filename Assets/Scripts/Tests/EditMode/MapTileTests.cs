using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests
{
    public class MapTileTests
    {
        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            var tile = new MapTile(TerrainType.Grass, 0, 100);

            Assert.AreEqual(TerrainType.Grass, tile.Terrain);
            Assert.AreEqual(0, tile.VisualVariant);
            Assert.AreEqual(100, tile.MovementCost);
            Assert.IsFalse(tile.IsCoastal);
            Assert.IsFalse(tile.HasFavorableWinds);
        }

        [Test]
        public void IsPassable_ReturnsTrueForGrass()
        {
            var tile = new MapTile(TerrainType.Grass);
            Assert.IsTrue(tile.IsPassable());
        }

        [Test]
        public void IsPassable_ReturnsFalseForRock()
        {
            var tile = new MapTile(TerrainType.Rock);
            Assert.IsFalse(tile.IsPassable());
        }

        [Test]
        public void IsPassable_ReturnsFalseForSubterranean()
        {
            var tile = new MapTile(TerrainType.Subterranean);
            Assert.IsTrue(tile.IsPassable()); // Subterranean is passable
        }

        [Test]
        public void IsWater_ReturnsTrueForWaterTerrain()
        {
            var tile = new MapTile(TerrainType.Water);
            Assert.IsTrue(tile.IsWater());
        }

        [Test]
        public void IsWater_ReturnsFalseForGrass()
        {
            var tile = new MapTile(TerrainType.Grass);
            Assert.IsFalse(tile.IsWater());
        }

        [Test]
        public void IsClear_ReturnsTrueForPassableTileWithNoObjects()
        {
            var tile = new MapTile(TerrainType.Grass);
            Assert.IsTrue(tile.IsClear());
        }

        [Test]
        public void IsClear_ReturnsFalseForBlockedTile()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddBlockingObject(1);
            Assert.IsFalse(tile.IsClear());
        }

        [Test]
        public void IsBlocked_ReturnsTrueWhenBlockingObjectAdded()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddBlockingObject(1);
            Assert.IsTrue(tile.IsBlocked());
        }

        [Test]
        public void AddVisitableObject_AddsObjectToList()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddVisitableObject(1);

            var visitableObjects = tile.GetVisitableObjects();
            Assert.AreEqual(1, visitableObjects.Count);
            Assert.IsTrue(tile.HasVisitableObject(1));
        }

        [Test]
        public void AddBlockingObject_AddsObjectToList()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddBlockingObject(2);

            var blockingObjects = tile.GetBlockingObjects();
            Assert.AreEqual(1, blockingObjects.Count);
            Assert.IsTrue(tile.HasBlockingObject(2));
        }

        [Test]
        public void AddVisitableObject_DoesNotAddDuplicates()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddVisitableObject(1);
            tile.AddVisitableObject(1);

            var visitableObjects = tile.GetVisitableObjects();
            Assert.AreEqual(1, visitableObjects.Count);
        }

        [Test]
        public void RemoveVisitableObject_RemovesObject()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddVisitableObject(1);
            tile.RemoveVisitableObject(1);

            Assert.IsFalse(tile.HasVisitableObject(1));
            Assert.AreEqual(0, tile.GetVisitableObjects().Count);
        }

        [Test]
        public void RemoveBlockingObject_RemovesObject()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.AddBlockingObject(2);
            tile.RemoveBlockingObject(2);

            Assert.IsFalse(tile.HasBlockingObject(2));
            Assert.AreEqual(0, tile.GetBlockingObjects().Count);
        }

        [Test]
        public void SetCoastal_SetsCoastalFlag()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.SetCoastal(true);

            Assert.IsTrue(tile.IsCoastal);
        }

        [Test]
        public void SetFavorableWinds_SetsFavorableWindsFlag()
        {
            var tile = new MapTile(TerrainType.Water);
            tile.SetFavorableWinds(true);

            Assert.IsTrue(tile.HasFavorableWinds);
        }

        [Test]
        public void SetTerrain_ChangesTerrain()
        {
            var tile = new MapTile(TerrainType.Grass);
            tile.SetTerrain(TerrainType.Sand, 1, 150);

            Assert.AreEqual(TerrainType.Sand, tile.Terrain);
            Assert.AreEqual(1, tile.VisualVariant);
            Assert.AreEqual(150, tile.MovementCost);
        }

        [Test]
        public void SetTerrain_KeepsExistingVariantWhenNegative()
        {
            var tile = new MapTile(TerrainType.Grass, 2, 100);
            tile.SetTerrain(TerrainType.Dirt, -1, -1);

            Assert.AreEqual(TerrainType.Dirt, tile.Terrain);
            Assert.AreEqual(2, tile.VisualVariant); // Kept original
            Assert.AreEqual(100, tile.MovementCost); // Kept original
        }
    }
}
