using System;
using System.Collections.Generic;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Player state - represents a player in the game
    /// Based on VCMI's PlayerState
    /// </summary>
    [Serializable]
    public class Player
    {
        public int Id { get; set; }
        public PlayerColor Color { get; set; }
        public string Name { get; set; }
        public bool IsHuman { get; set; }
        public bool IsActive { get; set; } = true;

        // Resources
        public ResourceSet Resources { get; set; } = new ResourceSet(
            wood: 20,
            mercury: 10,
            ore: 20,
            sulfur: 10,
            crystal: 10,
            gems: 10,
            gold: 30000
        );

        // Owned entities
        public List<int> HeroIds { get; set; } = new List<int>();
        public List<int> TownIds { get; set; } = new List<int>();

        // Day tracking
        public int LastTurnDay { get; set; }

        public Player()
        {
        }

        public Player(int id, PlayerColor color, string name, bool isHuman)
        {
            Id = id;
            Color = color;
            Name = name;
            IsHuman = isHuman;
        }

        /// <summary>
        /// Add resources to player
        /// </summary>
        public void AddResources(ResourceSet resources)
        {
            Resources.Add(resources);
        }

        /// <summary>
        /// Try to pay a resource cost
        /// </summary>
        public bool TryPayCost(ResourceSet cost)
        {
            if (!Resources.CanAfford(cost))
                return false;

            Resources.Subtract(cost);
            return true;
        }

        /// <summary>
        /// Check if player can afford something
        /// </summary>
        public bool CanAfford(ResourceSet cost)
        {
            return Resources.CanAfford(cost);
        }

        /// <summary>
        /// Get daily income from towns and mines
        /// </summary>
        public ResourceSet CalculateDailyIncome()
        {
            // Base income from towns
            // This will be calculated based on owned towns and mines
            // For now, return minimal income
            return new ResourceSet(gold: 1000 * TownIds.Count);
        }
    }
}
