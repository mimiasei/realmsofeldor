using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Resource storage structure matching HOMM3 resource system
    /// </summary>
    [Serializable]
    public struct ResourceSet
    {
        public int Wood;
        public int Mercury;
        public int Ore;
        public int Sulfur;
        public int Crystal;
        public int Gems;
        public int Gold;

        public ResourceSet(int wood = 0, int mercury = 0, int ore = 0, int sulfur = 0,
                          int crystal = 0, int gems = 0, int gold = 0)
        {
            Wood = wood;
            Mercury = mercury;
            Ore = ore;
            Sulfur = sulfur;
            Crystal = crystal;
            Gems = gems;
            Gold = gold;
        }

        /// <summary>
        /// Get resource amount by type
        /// </summary>
        public int Get(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => Wood,
                ResourceType.Mercury => Mercury,
                ResourceType.Ore => Ore,
                ResourceType.Sulfur => Sulfur,
                ResourceType.Crystal => Crystal,
                ResourceType.Gems => Gems,
                ResourceType.Gold => Gold,
                _ => 0
            };
        }

        /// <summary>
        /// Set resource amount by type
        /// </summary>
        public void Set(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Wood: Wood = amount; break;
                case ResourceType.Mercury: Mercury = amount; break;
                case ResourceType.Ore: Ore = amount; break;
                case ResourceType.Sulfur: Sulfur = amount; break;
                case ResourceType.Crystal: Crystal = amount; break;
                case ResourceType.Gems: Gems = amount; break;
                case ResourceType.Gold: Gold = amount; break;
            }
        }

        /// <summary>
        /// Add resources
        /// </summary>
        public void Add(ResourceSet other)
        {
            Wood += other.Wood;
            Mercury += other.Mercury;
            Ore += other.Ore;
            Sulfur += other.Sulfur;
            Crystal += other.Crystal;
            Gems += other.Gems;
            Gold += other.Gold;
        }

        /// <summary>
        /// Subtract resources
        /// </summary>
        public void Subtract(ResourceSet other)
        {
            Wood -= other.Wood;
            Mercury -= other.Mercury;
            Ore -= other.Ore;
            Sulfur -= other.Sulfur;
            Crystal -= other.Crystal;
            Gems -= other.Gems;
            Gold -= other.Gold;
        }

        /// <summary>
        /// Check if we have enough resources to pay a cost
        /// </summary>
        public bool CanAfford(ResourceSet cost)
        {
            return Wood >= cost.Wood &&
                   Mercury >= cost.Mercury &&
                   Ore >= cost.Ore &&
                   Sulfur >= cost.Sulfur &&
                   Crystal >= cost.Crystal &&
                   Gems >= cost.Gems &&
                   Gold >= cost.Gold;
        }

        /// <summary>
        /// Operator overloads for convenience
        /// </summary>
        public static ResourceSet operator +(ResourceSet a, ResourceSet b)
        {
            var result = a;
            result.Add(b);
            return result;
        }

        public static ResourceSet operator -(ResourceSet a, ResourceSet b)
        {
            var result = a;
            result.Subtract(b);
            return result;
        }

        public override string ToString()
        {
            var resources = new List<string>();
            if (Wood > 0) resources.Add($"Wood: {Wood}");
            if (Mercury > 0) resources.Add($"Mercury: {Mercury}");
            if (Ore > 0) resources.Add($"Ore: {Ore}");
            if (Sulfur > 0) resources.Add($"Sulfur: {Sulfur}");
            if (Crystal > 0) resources.Add($"Crystal: {Crystal}");
            if (Gems > 0) resources.Add($"Gems: {Gems}");
            if (Gold > 0) resources.Add($"Gold: {Gold}");

            return resources.Any() ? string.Join(", ", resources) : "No resources";
        }
    }

    /// <summary>
    /// Resource cost structure for buildings, creatures, etc.
    /// </summary>
    [Serializable]
    public struct ResourceCost
    {
        public ResourceSet Cost;

        public ResourceCost(ResourceSet cost)
        {
            Cost = cost;
        }

        public bool CanPay(ResourceSet available)
        {
            return available.CanAfford(Cost);
        }

        public static implicit operator ResourceSet(ResourceCost cost) => cost.Cost;
        public static implicit operator ResourceCost(ResourceSet resources) => new ResourceCost(resources);
    }
}
