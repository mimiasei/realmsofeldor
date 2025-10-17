using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Manages execution of modificators in dependency order.
    /// Based on VCMI's fillZones() job queue pattern.
    /// </summary>
    public class ModificatorPipeline
    {
        private readonly List<MapModificator> modificators = new List<MapModificator>();
        private readonly GameMap map;
        private readonly MapGenConfig config;
        private readonly System.Random random;

        public ModificatorPipeline(GameMap map, MapGenConfig config, int? seed = null)
        {
            this.map = map;
            this.config = config;
            this.random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// Adds a modificator to the pipeline.
        /// </summary>
        public void AddModificator(MapModificator modificator)
        {
            modificators.Add(modificator);
        }

        /// <summary>
        /// Executes all modificators in dependency order.
        /// Modificators with no remaining dependencies run first (sorted by priority).
        /// </summary>
        public void Execute()
        {
            Debug.Log($"[MapGen] Starting modificator pipeline with {modificators.Count} modificators");

            var pending = new List<MapModificator>(modificators);
            var executedCount = 0;

            // Execute modificators in dependency order
            while (pending.Count > 0)
            {
                // Find all modificators that are ready to run
                var ready = pending
                    .Where(m => m.IsReady(modificators))
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => m.Name)
                    .ToList();

                if (ready.Count == 0)
                {
                    // Deadlock - no modificators can run
                    var remainingNames = string.Join(", ", pending.Select(m => m.Name));
                    Debug.LogError($"[MapGen] Dependency deadlock! Remaining modificators: {remainingNames}");
                    break;
                }

                // Execute all ready modificators (could be parallelized in the future)
                foreach (var modificator in ready)
                {
                    modificator.Execute(map, config, random);
                    pending.Remove(modificator);
                    executedCount++;
                }
            }

            Debug.Log($"[MapGen] Pipeline complete: {executedCount}/{modificators.Count} modificators executed");
        }

        /// <summary>
        /// Executes all modificators and performs final cleanup.
        /// </summary>
        public void ExecuteWithCleanup()
        {
            Execute();

            // Final cleanup: calculate coastal tiles (VCMI equivalent)
            map.CalculateCoastalTiles();
            Debug.Log("[MapGen] Calculated coastal tiles");
        }

        /// <summary>
        /// Returns a summary of the pipeline configuration.
        /// </summary>
        public string GetSummary()
        {
            var summary = $"Modificator Pipeline ({modificators.Count} modificators):\n";
            foreach (var mod in modificators.OrderBy(m => m.Priority).ThenBy(m => m.Name))
            {
                var deps = mod.Dependencies.Count > 0
                    ? string.Join(", ", mod.Dependencies.Select(t => t.Name))
                    : "None";
                summary += $"  [{mod.Priority}] {mod.Name} (deps: {deps})\n";
            }
            return summary.TrimEnd('\n');
        }
    }
}
