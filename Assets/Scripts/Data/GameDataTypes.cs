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
    /// Hero class types
    /// </summary>
    public enum HeroClass
    {
        Knight = 0,
        Cleric = 1,
        Ranger = 2,
        Druid = 3,
        Alchemist = 4,
        Wizard = 5,
        Demoniac = 6,
        Heretic = 7,
        DeathKnight = 8,
        Necromancer = 9,
        Overlord = 10,
        Warlock = 11,
        Barbarian = 12,
        BattleMage = 13,
        Beastmaster = 14,
        Witch = 15,
        Planeswalker = 16,
        Elementalist = 17
    }

    /// <summary>
    /// Primary stats for heroes
    /// </summary>
    public enum PrimarySkill
    {
        Attack = 0,
        Defense = 1,
        SpellPower = 2,
        Knowledge = 3
    }

    /// <summary>
    /// Secondary skill types
    /// </summary>
    public enum SecondarySkillType
    {
        Pathfinding = 0,
        Archery = 1,
        Logistics = 2,
        Scouting = 3,
        Diplomacy = 4,
        Navigation = 5,
        Leadership = 6,
        Wisdom = 7,
        Mysticism = 8,
        Luck = 9,
        Ballistics = 10,
        EagleEye = 11,
        Necromancy = 12,
        Estates = 13,
        FireMagic = 14,
        AirMagic = 15,
        WaterMagic = 16,
        EarthMagic = 17,
        Scholar = 18,
        Tactics = 19,
        Artillery = 20,
        Learning = 21,
        Offense = 22,
        Armorer = 23,
        Intelligence = 24,
        Sorcery = 25,
        Resistance = 26,
        FirstAid = 27
    }

    /// <summary>
    /// Secondary skill levels
    /// </summary>
    public enum SkillLevel
    {
        None = 0,
        Basic = 1,
        Advanced = 2,
        Expert = 3
    }

    /// <summary>
    /// Terrain types
    /// </summary>
    public enum TerrainType
    {
        Dirt = 0,
        Sand = 1,
        Grass = 2,
        Snow = 3,
        Swamp = 4,
        Rough = 5,
        Subterranean = 6,
        Lava = 7,
        Water = 8,
        Rock = 9
    }

    /// <summary>
    /// Spell schools
    /// </summary>
    public enum SpellSchool
    {
        Air = 0,
        Earth = 1,
        Fire = 2,
        Water = 3,
        All = 4
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
