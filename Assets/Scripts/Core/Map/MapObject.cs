using System.Collections.Generic;
using System.Linq;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Base class for objects on the game map.
    /// Based on VCMI's CGObjectInstance.
    /// Can be instantiated directly for decorative objects (following VCMI pattern).
    /// </summary>
    public class MapObject
    {
        // Basic properties
        public int InstanceId { get; set; }
        public MapObjectType ObjectType { get; protected set; }
        public Position Position { get; set; }
        public PlayerColor Owner { get; set; }
        public string Name { get; set; }

        // Blocking and visitable configuration
        public bool IsBlocking { get; set; }
        public bool IsVisitable { get; set; }
        public bool IsRemovable { get; set; }

        // Value tracking for budget system
        private int? _cachedValue;
        public int Value
        {
            get
            {
                if (!_cachedValue.HasValue)
                    _cachedValue = CalculateValue();
                return _cachedValue.Value;
            }
        }

        // Guard information
        public GuardInfo Guard { get; set; }

        public MapObject(MapObjectType objectType, Position position)
        {
            ObjectType = objectType;
            Position = position;
            Owner = PlayerColor.Neutral;
            Name = $"{objectType}_{InstanceId}";
            IsBlocking = false;
            IsVisitable = false;
            IsRemovable = false;
            Guard = null; // No guard by default
        }

        // Virtual methods with default implementations (can be overridden by subclasses)
        public virtual HashSet<Position> GetBlockedPositions()
        {
            // Default: single tile blocking at object position if IsBlocking is true
            return IsBlocking ? new HashSet<Position> { Position } : new HashSet<Position>();
        }

        public virtual void OnVisit(Hero hero)
        {
            // Default: no action (decorative objects don't have visit logic)
        }

        /// <summary>
        /// Calculates the value of this object for budget tracking.
        /// Override in subclasses for specific value calculations.
        /// </summary>
        protected virtual int CalculateValue()
        {
            return 0; // Decorative objects have no value
        }

        // Get positions that can be visited from (adjacent to blocked positions)
        public virtual HashSet<Position> GetVisitablePositions()
        {
            var blockedPositions = GetBlockedPositions();
            var visitablePositions = new HashSet<Position>();

            foreach (var blockedPos in blockedPositions)
            {
                // Add all 8 adjacent positions
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var adjacentPos = new Position(blockedPos.X + dx, blockedPos.Y + dy);
                        if (!blockedPositions.Contains(adjacentPos))
                            visitablePositions.Add(adjacentPos);
                    }
                }
            }

            return visitablePositions;
        }

        public virtual bool IsVisitableAt(Position pos)
        {
            return GetVisitablePositions().Contains(pos);
        }

        public virtual bool IsBlockingAt(Position pos)
        {
            return GetBlockedPositions().Contains(pos);
        }

        /// <summary>
        /// Checks if this object has guards.
        /// </summary>
        public bool IsGuarded()
        {
            return Guard != null && Guard.CreatureId != 0 && Guard.Count > 0;
        }

        /// <summary>
        /// Gets the position where the guard is placed (blocking access to the object).
        /// Usually the tile directly in front of the object's main tile.
        /// </summary>
        public Position GetGuardPosition()
        {
            // Default: place guard directly south of object (below)
            // This matches VCMI's preference for placing guards below objects
            return new Position(Position.X, Position.Y + 1);
        }
    }

    /// <summary>
    /// Represents guard information for a map object.
    /// Based on VCMI's CGCreature pattern for object guards.
    /// </summary>
    public class GuardInfo
    {
        public int CreatureId { get; set; }
        public int Count { get; set; }
        public int CalculatedStrength { get; set; } // AI value used for calculation

        public GuardInfo(int creatureId, int count, int calculatedStrength = 0)
        {
            CreatureId = creatureId;
            Count = count;
            CalculatedStrength = calculatedStrength;
        }
    }

    /// <summary>
    /// Resource pile that can be picked up (wood, ore, gold, etc.)
    /// Non-blocking, one-time pickup.
    /// </summary>
    public class ResourceObject : MapObject
    {
        public ResourceType ResourceType { get; private set; }
        public int Amount { get; private set; }

        public ResourceObject(Position position, ResourceType resourceType, int amount)
            : base(MapObjectType.Resource, position)
        {
            ResourceType = resourceType;
            Amount = amount;
            IsBlocking = false;
            IsVisitable = true;
            IsRemovable = true;
            Name = $"{resourceType} Pile";
        }

        public override HashSet<Position> GetBlockedPositions()
        {
            return new HashSet<Position>(); // Non-blocking
        }

        public override void OnVisit(Hero hero)
        {
            // Resource pickup logic handled by GameState
            // This object will be removed after visit
        }

        protected override int CalculateValue()
        {
            // Calculate value based on resource type
            // Uses standard VCMI values: Gold 1:1, Basic 125:1, Rare 500:1
            switch (ResourceType)
            {
                case ResourceType.Gold:
                    return Amount;

                case ResourceType.Wood:
                case ResourceType.Ore:
                    return Amount * 125;

                case ResourceType.Mercury:
                case ResourceType.Sulfur:
                case ResourceType.Crystal:
                case ResourceType.Gems:
                    return Amount * 500;

                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// Mine that generates resources each day.
    /// Blocking, capturable, generates resources for owner.
    /// </summary>
    public class MineObject : MapObject
    {
        public ResourceType ResourceType { get; private set; }
        public int DailyProduction { get; private set; }

        public MineObject(Position position, ResourceType resourceType, int dailyProduction = 1)
            : base(MapObjectType.Mine, position)
        {
            ResourceType = resourceType;
            DailyProduction = dailyProduction;
            IsBlocking = true;
            IsVisitable = true;
            IsRemovable = false;
            Name = $"{resourceType} Mine";
        }

        public override HashSet<Position> GetBlockedPositions()
        {
            return new HashSet<Position> { Position };
        }

        public override void OnVisit(Hero hero)
        {
            // Flag mine for hero's owner
            // Logic handled by GameState
        }

        protected override int CalculateValue()
        {
            // Mines have strategic value: daily production * 30 days
            // Uses standard VCMI values: Gold 1:1, Basic 125:1, Rare 500:1
            int dailyValue;
            switch (ResourceType)
            {
                case ResourceType.Gold:
                    dailyValue = DailyProduction;
                    break;

                case ResourceType.Wood:
                case ResourceType.Ore:
                    dailyValue = DailyProduction * 125;
                    break;

                case ResourceType.Mercury:
                case ResourceType.Sulfur:
                case ResourceType.Crystal:
                case ResourceType.Gems:
                    dailyValue = DailyProduction * 500;
                    break;

                default:
                    dailyValue = 0;
                    break;
            }

            return dailyValue * 30; // Strategic value over time (30 days)
        }
    }

    /// <summary>
    /// Dwelling where creatures can be recruited.
    /// Blocking, has weekly creature growth.
    /// </summary>
    public class DwellingObject : MapObject
    {
        public int CreatureId { get; private set; }
        public int AvailableCreatures { get; set; }
        public int WeeklyGrowth { get; private set; }

        public DwellingObject(Position position, int creatureId, int weeklyGrowth)
            : base(MapObjectType.Dwelling, position)
        {
            CreatureId = creatureId;
            WeeklyGrowth = weeklyGrowth;
            AvailableCreatures = weeklyGrowth;
            IsBlocking = true;
            IsVisitable = true;
            IsRemovable = false;
            Name = $"Creature Dwelling";
        }

        public override HashSet<Position> GetBlockedPositions()
        {
            return new HashSet<Position> { Position };
        }

        public override void OnVisit(Hero hero)
        {
            // Recruitment UI logic
            // Handled by GameState/UI
        }

        public void ApplyWeeklyGrowth()
        {
            AvailableCreatures += WeeklyGrowth;
        }

        public bool CanRecruit(int count)
        {
            return count <= AvailableCreatures;
        }

        public void Recruit(int count)
        {
            if (CanRecruit(count))
                AvailableCreatures -= count;
        }
    }
}
