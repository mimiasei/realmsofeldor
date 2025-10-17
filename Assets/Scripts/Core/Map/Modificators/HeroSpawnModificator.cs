using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Finds a valid spawn position for a hero on the map after terrain generation.
    /// This ensures the hero doesn't spawn on water or impassable terrain.
    /// Based on VCMI's player start position placement.
    /// Note: This modificator only finds the position - hero creation happens externally.
    /// </summary>
    public class HeroSpawnModificator : MapModificator
    {
        public override string Name => "Hero Spawn Position";
        public override int Priority => 90; // Run after ReachabilityValidator (80)
        public override List<System.Type> Dependencies => new List<System.Type>
        {
            typeof(TerrainPainterModificator),
            typeof(ReachabilityValidatorModificator)
        };

        /// <summary>
        /// Gets the spawn position found by this modificator.
        /// Null if no valid position was found.
        /// </summary>
        public Position? SpawnPosition { get; private set; }

        protected override void Run(GameMap map, MapGenConfig config, System.Random random)
        {
            // Find a passable position for hero spawn
            SpawnPosition = FindPassableSpawnPosition(map, random);

            if (SpawnPosition == null)
            {
                Debug.LogError("❌ Failed to find passable spawn position for hero!");
            }
            else
            {
                Debug.Log($"✓ {Name}: Found valid hero spawn position at {SpawnPosition.Value}");
            }
        }

        /// <summary>
        /// Finds a random passable position on the map for hero spawn.
        /// Prioritizes positions near the center and away from map edges.
        /// </summary>
        private Position? FindPassableSpawnPosition(GameMap map, System.Random random)
        {
            var width = map.Width;
            var height = map.Height;

            // Try to find a position in the central area first
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var x = random.Next(width / 4, width * 3 / 4);
                var y = random.Next(height / 4, height * 3 / 4);
                var pos = new Position(x, y);

                if (IsValidSpawnPosition(map, pos))
                    return pos;
            }

            // Fallback: try anywhere on the map
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var x = random.Next(2, width - 2);
                var y = random.Next(2, height - 2);
                var pos = new Position(x, y);

                if (IsValidSpawnPosition(map, pos))
                    return pos;
            }

            return null;
        }

        /// <summary>
        /// Checks if a position is valid for hero spawning.
        /// Must be passable, not have objects, and not be water.
        /// </summary>
        private bool IsValidSpawnPosition(GameMap map, Position pos)
        {
            if (!map.IsInBounds(pos))
                return false;

            var tile = map.GetTile(pos);

            // Check if tile is passable (not water)
            if (!tile.IsPassable())
                return false;

            // Check if tile has objects (both blocking and visitable)
            if (tile.GetBlockingObjects().Count > 0 || tile.GetVisitableObjects().Count > 0)
                return false;

            // Check if any adjacent tiles are impassable (avoid spawning next to water)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var adjacentPos = new Position(pos.X + dx, pos.Y + dy);
                    if (map.IsInBounds(adjacentPos))
                    {
                        var adjacentTile = map.GetTile(adjacentPos);
                        if (!adjacentTile.IsPassable())
                            return false; // Don't spawn next to water
                    }
                }
            }

            return true;
        }
    }
}
