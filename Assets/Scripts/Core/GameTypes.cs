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
        Event = 20,
        Decorative = 21,
        Visitable = 22
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
