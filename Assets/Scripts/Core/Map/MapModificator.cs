using System.Collections.Generic;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Base class for map generation modificators (VCMI pattern).
    /// Each modificator is a job that modifies the map in some way (terrain, objects, obstacles, etc.).
    /// Modificators have dependencies and run when their dependencies are satisfied.
    /// </summary>
    public abstract class MapModificator
    {
        /// <summary>
        /// Human-readable name for debugging and logging
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Execution priority (lower values run first). Default: 100
        /// </summary>
        public virtual int Priority => 100;

        /// <summary>
        /// List of modificator types this modificator depends on.
        /// This modificator will only run after all dependencies have completed.
        /// </summary>
        public virtual List<System.Type> Dependencies => new List<System.Type>();

        /// <summary>
        /// Whether this modificator has completed execution
        /// </summary>
        public bool IsFinished { get; protected set; }

        /// <summary>
        /// Whether this modificator is ready to run.
        /// Default implementation checks if all dependencies are finished.
        /// </summary>
        public virtual bool IsReady(List<MapModificator> allModificators)
        {
            // Check if all dependencies have finished
            foreach (var dependencyType in Dependencies)
            {
                var found = false;
                foreach (var mod in allModificators)
                {
                    if (mod.GetType() == dependencyType && mod.IsFinished)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    // Dependency not finished yet
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Execute this modificator's map generation logic.
        /// Should set IsFinished = true when complete.
        /// </summary>
        public void Execute(GameMap map, MapGenConfig config, System.Random random)
        {
            Debug.Log($"[MapGen] Running modificator: {Name}");
            Run(map, config, random);
            IsFinished = true;
        }

        /// <summary>
        /// Override this method to implement the modificator's logic.
        /// </summary>
        protected abstract void Run(GameMap map, MapGenConfig config, System.Random random);
    }
}
