using System;

namespace RealmsOfEldor.Data
{
    /// <summary>
    /// Faction types (town alignments)
    /// </summary>
    public enum Faction
    {
        Castle = 0,
        Rampart = 1,
        Tower = 2,
        Inferno = 3,
        Necropolis = 4,
        Dungeon = 5,
        Stronghold = 6,
        Fortress = 7,
        Conflux = 8,
        Neutral = 9
    }

    /// <summary>
    /// Creature tiers (1-7 like HOMM3)
    /// </summary>
    public enum CreatureTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5,
        Tier6 = 6,
        Tier7 = 7
    }

    /// <summary>
    /// Resource cost structure for buildings, creatures, etc.
    /// </summary>
    [Serializable]
    public struct ResourceCost
    {
        public int Wood;
        public int Mercury;
        public int Ore;
        public int Sulfur;
        public int Crystal;
        public int Gems;
        public int Gold;

        public ResourceCost(int wood = 0, int mercury = 0, int ore = 0, int sulfur = 0,
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

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (Wood > 0) parts.Add($"{Wood}W");
            if (Mercury > 0) parts.Add($"{Mercury}Mer");
            if (Ore > 0) parts.Add($"{Ore}O");
            if (Sulfur > 0) parts.Add($"{Sulfur}S");
            if (Crystal > 0) parts.Add($"{Crystal}C");
            if (Gems > 0) parts.Add($"{Gems}G");
            if (Gold > 0) parts.Add($"{Gold}g");
            return string.Join(", ", parts);
        }
    }
}
