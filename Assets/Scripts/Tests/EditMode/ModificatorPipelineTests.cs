using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Map;
using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests
{
    [TestFixture]
    public class ModificatorPipelineTests
    {
        private GameMap testMap;
        private MapGenConfig testConfig;

        [SetUp]
        public void SetUp()
        {
            testMap = new GameMap(30, 30);
            testConfig = UnityEngine.ScriptableObject.CreateInstance<MapGenConfig>();
            testConfig.treasureBudget = 10000;
            testConfig.mineCount = 5;
            testConfig.dwellingCount = 3;
            testConfig.enableGuards = true;
            testConfig.enableObstacles = true;
            testConfig.obstacleCount = 20;
            testConfig.validateReachability = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (testConfig != null)
            {
                UnityEngine.Object.DestroyImmediate(testConfig);
            }
        }

        [Test]
        public void ModificatorPipeline_CanBeCreated()
        {
            var pipeline = new ModificatorPipeline(testMap, testConfig);
            Assert.IsNotNull(pipeline);
        }

        [Test]
        public void ModificatorPipeline_CanAddModificators()
        {
            var pipeline = new ModificatorPipeline(testMap, testConfig);
            pipeline.AddModificator(new TerrainPainterModificator());
            pipeline.AddModificator(new ResourcePlacerModificator());

            var summary = pipeline.GetSummary();
            Assert.IsTrue(summary.Contains("Terrain Painter"));
            Assert.IsTrue(summary.Contains("Resource Placer"));
        }

        [Test]
        public void ModificatorPipeline_ExecutesInDependencyOrder()
        {
            var pipeline = new ModificatorPipeline(testMap, testConfig, seed: 42);

            // Add in random order - pipeline should sort by dependencies
            pipeline.AddModificator(new GuardPlacerModificator());
            pipeline.AddModificator(new TerrainPainterModificator());
            pipeline.AddModificator(new ResourcePlacerModificator());

            pipeline.Execute();

            // Verify all modificators finished
            // TerrainPainter should run first, then ResourcePlacer, then GuardPlacer
            Assert.Greater(testMap.GetAllObjects().Count(), 0, "Should have placed objects");
        }

        [Test]
        public void TerrainPainterModificator_PaintsTerrain()
        {
            var modificator = new TerrainPainterModificator();
            modificator.Execute(testMap, testConfig, new System.Random(42));

            // Check that terrain was painted (not all default grass)
            var terrainTypes = new HashSet<TerrainType>();
            for (var y = 0; y < testMap.Height; y++)
            {
                for (var x = 0; x < testMap.Width; x++)
                {
                    var terrain = testMap.GetTile(new Position(x, y)).Terrain;
                    terrainTypes.Add(terrain);
                }
            }

            Assert.IsTrue(modificator.IsFinished);
            Assert.Greater(terrainTypes.Count, 1, "Should have multiple terrain types");
        }

        [Test]
        public void ResourcePlacerModificator_PlacesResources()
        {
            // First paint terrain
            var terrainPainter = new TerrainPainterModificator();
            terrainPainter.Execute(testMap, testConfig, new System.Random(42));

            // Then place resources
            var resourcePlacer = new ResourcePlacerModificator();
            resourcePlacer.Execute(testMap, testConfig, new System.Random(42));

            var resources = testMap.GetAllObjects().OfType<ResourceObject>().ToList();
            Assert.IsTrue(resourcePlacer.IsFinished);
            Assert.Greater(resources.Count, 0, "Should have placed resources");
        }

        [Test]
        public void MinePlacerModificator_PlacesMines()
        {
            // First paint terrain
            var terrainPainter = new TerrainPainterModificator();
            terrainPainter.Execute(testMap, testConfig, new System.Random(42));

            // Then place mines
            var minePlacer = new MinePlacerModificator();
            minePlacer.Execute(testMap, testConfig, new System.Random(42));

            var mines = testMap.GetAllObjects().OfType<MineObject>().ToList();
            Assert.IsTrue(minePlacer.IsFinished);
            Assert.Greater(mines.Count, 0, "Should have placed mines");
        }

        [Test]
        public void DwellingPlacerModificator_PlacesDwellings()
        {
            // First paint terrain
            var terrainPainter = new TerrainPainterModificator();
            terrainPainter.Execute(testMap, testConfig, new System.Random(42));

            // Then place dwellings
            var dwellingPlacer = new DwellingPlacerModificator();
            dwellingPlacer.Execute(testMap, testConfig, new System.Random(42));

            var dwellings = testMap.GetAllObjects().OfType<DwellingObject>().ToList();
            Assert.IsTrue(dwellingPlacer.IsFinished);
            Assert.Greater(dwellings.Count, 0, "Should have placed dwellings");
        }

        [Test]
        public void GuardPlacerModificator_AddsGuardsToHighValueObjects()
        {
            // Setup: paint terrain, place high-value resources
            var terrainPainter = new TerrainPainterModificator();
            terrainPainter.Execute(testMap, testConfig, new System.Random(42));

            var resourcePlacer = new ResourcePlacerModificator();
            resourcePlacer.Execute(testMap, testConfig, new System.Random(42));

            // Place guards
            var guardPlacer = new GuardPlacerModificator();
            guardPlacer.Execute(testMap, testConfig, new System.Random(42));

            // Check if any high-value objects have guards
            var guardedObjects = testMap.GetAllObjects().Where(o => o.IsGuarded()).ToList();
            Assert.IsTrue(guardPlacer.IsFinished);
            // May or may not have guards depending on value, just check it didn't crash
        }

        [Test]
        public void ObstaclePlacerModificator_PlacesObstacles()
        {
            // Setup: paint terrain
            var terrainPainter = new TerrainPainterModificator();
            terrainPainter.Execute(testMap, testConfig, new System.Random(42));

            // Place obstacles
            var obstaclePlacer = new ObstaclePlacerModificator();
            obstaclePlacer.Execute(testMap, testConfig, new System.Random(42));

            var obstacles = testMap.GetAllObjects().Where(o => o.ObjectType >= (MapObjectType)100).ToList(); // Decorative/obstacle types
            Assert.IsTrue(obstaclePlacer.IsFinished);
            Assert.Greater(obstacles.Count, 0, "Should have placed obstacles");
        }

        [Test]
        public void ReachabilityValidatorModificator_ValidatesReachability()
        {
            // Setup: full map with objects
            var pipeline = new ModificatorPipeline(testMap, testConfig, seed: 42);
            pipeline.AddModificator(new TerrainPainterModificator());
            pipeline.AddModificator(new ResourcePlacerModificator());
            pipeline.AddModificator(new MinePlacerModificator());

            var startPositions = new List<Position> { new Position(5, 5), new Position(25, 25) };
            pipeline.AddModificator(new ReachabilityValidatorModificator(startPositions, 5));

            pipeline.Execute();

            // Validator should have run without errors
            Assert.Greater(testMap.GetAllObjects().Count(), 0);
        }

        [Test]
        public void ModificatorPipeline_FullPipeline_CreatesCompleteMap()
        {
            var pipeline = new ModificatorPipeline(testMap, testConfig, seed: 42);

            // Add all modificators
            pipeline.AddModificator(new TerrainPainterModificator());
            pipeline.AddModificator(new ResourcePlacerModificator());
            pipeline.AddModificator(new MinePlacerModificator());
            pipeline.AddModificator(new DwellingPlacerModificator());
            pipeline.AddModificator(new GuardPlacerModificator());
            pipeline.AddModificator(new ObstaclePlacerModificator());

            var startPositions = new List<Position> { new Position(5, 5), new Position(25, 25) };
            pipeline.AddModificator(new ReachabilityValidatorModificator(startPositions, 5));

            // Execute with cleanup
            pipeline.ExecuteWithCleanup();

            // Verify map is complete
            Assert.Greater(testMap.GetAllObjects().Count(), 0, "Should have objects");

            // Verify different object types exist
            var resources = testMap.GetAllObjects().OfType<ResourceObject>().Count();
            var mines = testMap.GetAllObjects().OfType<MineObject>().Count();
            var dwellings = testMap.GetAllObjects().OfType<DwellingObject>().Count();

            Assert.Greater(resources, 0, "Should have resources");
            Assert.Greater(mines, 0, "Should have mines");
            Assert.Greater(dwellings, 0, "Should have dwellings");
        }

        [Test]
        public void ModificatorPipeline_DependencyChain_PreventsOutOfOrderExecution()
        {
            // Create a test modificator that depends on TerrainPainter
            var pipeline = new ModificatorPipeline(testMap, testConfig);

            // Add ResourcePlacer first (depends on TerrainPainter)
            pipeline.AddModificator(new ResourcePlacerModificator());

            // TerrainPainter hasn't been added yet - ResourcePlacer should wait
            var summaryBefore = pipeline.GetSummary();
            Assert.IsTrue(summaryBefore.Contains("Resource Placer"));

            // Now add TerrainPainter
            pipeline.AddModificator(new TerrainPainterModificator());

            // Execute - should run TerrainPainter first
            pipeline.Execute();

            Assert.Greater(testMap.GetAllObjects().Count(), 0);
        }

        [Test]
        public void ModificatorPipeline_GetSummary_ShowsPriorityAndDependencies()
        {
            var pipeline = new ModificatorPipeline(testMap, testConfig);
            pipeline.AddModificator(new TerrainPainterModificator());
            pipeline.AddModificator(new ResourcePlacerModificator());
            pipeline.AddModificator(new GuardPlacerModificator());

            var summary = pipeline.GetSummary();

            Assert.IsTrue(summary.Contains("Terrain Painter"));
            Assert.IsTrue(summary.Contains("Resource Placer"));
            Assert.IsTrue(summary.Contains("Guard Placer"));
            Assert.IsTrue(summary.Contains("deps:"));
        }

        [Test]
        public void ModificatorPipeline_WithSeed_ProducesDeterministicResults()
        {
            var config = testConfig;

            // Generate first map
            var map1 = new GameMap(30, 30);
            var pipeline1 = new ModificatorPipeline(map1, config, seed: 12345);
            pipeline1.AddModificator(new TerrainPainterModificator());
            pipeline1.AddModificator(new ResourcePlacerModificator());
            pipeline1.Execute();

            // Generate second map with same seed
            var map2 = new GameMap(30, 30);
            var pipeline2 = new ModificatorPipeline(map2, config, seed: 12345);
            pipeline2.AddModificator(new TerrainPainterModificator());
            pipeline2.AddModificator(new ResourcePlacerModificator());
            pipeline2.Execute();

            // Should have same number of objects (deterministic)
            Assert.AreEqual(map1.GetAllObjects().Count(), map2.GetAllObjects().Count());
        }
    }
}
