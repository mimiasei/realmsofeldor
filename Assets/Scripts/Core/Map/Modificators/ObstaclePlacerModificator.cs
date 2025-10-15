using System.Collections.Generic;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Places decorative and blocking obstacles on the map.
    /// Based on VCMI's ObstaclePlacer modificator.
    /// </summary>
    public class ObstaclePlacerModificator : MapModificator
    {
        public override string Name => "Obstacle Placer";
        public override int Priority => 70;
        public override List<System.Type> Dependencies => new List<System.Type>
        {
            typeof(TerrainPainterModificator),
            typeof(ResourcePlacerModificator),
            typeof(MinePlacerModificator),
            typeof(DwellingPlacerModificator),
            typeof(GuardPlacerModificator)
        };

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            if (!config.enableObstacles)
            {
                Debug.Log($"✓ {Name}: Disabled in config");
                return;
            }

            // Build obstacle type list based on configuration
            var obstacleTypes = BuildObstacleTypeList(config);

            // Create obstacle placer
            var placer = new ObstaclePlacer(map, random);

            // Place obstacles
            var obstacles = placer.PlaceObstacles(
                obstacleTypes,
                config.obstacleCount,
                allowBlocking: config.blockingObstacleRatio > 0f);

            // Log statistics
            var stats = placer.GetStats();
            Debug.Log($"✓ {Name}: Placed {obstacles.Count} obstacles\n{stats}");
        }

        private List<ObstacleType> BuildObstacleTypeList(MapGenConfig config)
        {
            var obstacleTypes = new List<ObstacleType>();

            // Add mountain/rock types (30% by default)
            if (config.mountainRockChance > 0f)
            {
                obstacleTypes.Add(ObstacleType.Mountain);
                obstacleTypes.Add(ObstacleType.Rock);
                obstacleTypes.Add(ObstacleType.Boulder);
            }

            // Add tree types (40% by default)
            if (config.treeChance > 0f)
            {
                obstacleTypes.Add(ObstacleType.Tree);
                obstacleTypes.Add(ObstacleType.Tree); // Add twice for higher frequency
            }

            // Add bush/flower types (30% by default)
            if (config.bushFlowerChance > 0f)
            {
                obstacleTypes.Add(ObstacleType.Bush);
                obstacleTypes.Add(ObstacleType.Flowers);
                obstacleTypes.Add(ObstacleType.Grass);
            }

            // If no types configured, provide defaults
            if (obstacleTypes.Count == 0)
            {
                obstacleTypes.Add(ObstacleType.Tree);
                obstacleTypes.Add(ObstacleType.Rock);
                obstacleTypes.Add(ObstacleType.Bush);
            }

            return obstacleTypes;
        }
    }
}
