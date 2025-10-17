using System.Collections.Generic;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Validates that all objects are reachable and fixes unreachable ones.
    /// Based on VCMI's RockFiller modificator.
    /// </summary>
    public class ReachabilityValidatorModificator : MapModificator
    {
        private List<Position> startPositions;
        private int searchRadius;

        public override string Name => "Reachability Validator";
        public override int Priority => 80;
        public override List<System.Type> Dependencies => new List<System.Type>
        {
            typeof(TerrainPainterModificator),
            typeof(ResourcePlacerModificator),
            typeof(MinePlacerModificator),
            typeof(DwellingPlacerModificator),
            typeof(GuardPlacerModificator),
            typeof(ObstaclePlacerModificator)
        };

        public ReachabilityValidatorModificator(List<Position> startPositions, int searchRadius = 5)
        {
            this.startPositions = startPositions;
            this.searchRadius = searchRadius;
        }

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            if (!config.validateReachability)
            {
                Debug.Log($"✓ {Name}: Disabled in config");
                return;
            }

            // Use provided start positions or default to map center
            var positions = startPositions ?? new List<Position> { new Position(map.Width / 2, map.Height / 2) };

            // Run reachability validation
            var validator = new MapReachabilityValidator(map);
            var result = validator.FixUnreachableObjects(positions, searchRadius);

            // Log results
            if (result.TotalUnreachableObjects > 0)
            {
                Debug.Log($"✓ {Name}:\n{result}");
            }
            else
            {
                var stats = validator.CalculateStats(positions);
                Debug.Log($"✓ {Name}: All objects reachable!\n{stats}");
            }
        }
    }
}
