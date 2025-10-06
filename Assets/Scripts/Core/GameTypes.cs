using System;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Resource types matching HOMM3 resources
    /// </summary>
    public enum ResourceType
    {
        Wood = 0,
        Mercury = 1,
        Ore = 2,
        Sulfur = 3,
        Crystal = 4,
        Gems = 5,
        Gold = 6
    }

    /// <summary>
    /// Player colors
    /// </summary>
    public enum PlayerColor
    {
        Red = 0,
        Blue = 1,
        Tan = 2,
        Green = 3,
        Orange = 4,
        Purple = 5,
        Teal = 6,
        Pink = 7,
        Neutral = 255
    }

    /// <summary>
    /// Terrain types for map tiles
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

    // Faction moved to RealmsOfEldor.Data.GameDataTypes

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
    /// Artifact equipment slots
    /// </summary>
    public enum ArtifactSlot
    {
        Head = 0,
        Shoulders = 1,
        Neck = 2,
        RightHand = 3,
        LeftHand = 4,
        Torso = 5,
        RightRing = 6,
        LeftRing = 7,
        Feet = 8,
        Misc1 = 9,
        Misc2 = 10,
        Misc3 = 11,
        Misc4 = 12,
        Ballista = 13,
        AmmoCart = 14,
        FirstAidTent = 15,
        Catapult = 16,
        Spellbook = 17,
        Misc5 = 18
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
    /// Map object types
    /// </summary>
    public enum MapObjectType
    {
        Hero = 0,
        Town = 1,
        Monster = 2,
        Resource = 3,
        Mine = 4,
        Artifact = 5,
        TreasureChest = 6,
        RandomResource = 7,
        RandomTown = 8,
        RandomHero = 9,
        RandomDwelling = 10,
        RandomMonster = 11,
        Shrine = 12,
        Pandora = 13,
        Dwelling = 14,
        Garrison = 15,
        Lighthouse = 16,
        University = 17,
        Shipyard = 18,
        Obelisk = 19,
        Event = 20
    }

    /// <summary>
    /// Battle side
    /// </summary>
    public enum BattleSide
    {
        Attacker = 0,
        Defender = 1
    }

    // CreatureTier moved to RealmsOfEldor.Data.GameDataTypes
}
