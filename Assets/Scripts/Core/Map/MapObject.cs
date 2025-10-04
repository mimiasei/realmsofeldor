using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Base class for objects on the game map.
    /// Based on VCMI's CGObjectInstance.
    /// </summary>
    public abstract class MapObject
    {
        // Basic properties
        public int InstanceId { get; set; }
        public MapObjectType ObjectType { get; protected set; }
        public Position Position { get; set; }
        public PlayerColor Owner { get; set; }
        public string Name { get; set; }

        // Blocking and visitable configuration
        public bool IsBlocking { get; protected set; }
        public bool IsVisitable { get; protected set; }
        public bool IsRemovable { get; protected set; }

        protected MapObject(MapObjectType objectType, Position position)
        {
            ObjectType = objectType;
            Position = position;
            Owner = PlayerColor.Neutral;
            Name = $"{objectType}_{InstanceId}";
            IsBlocking = false;
            IsVisitable = false;
            IsRemovable = false;
        }

        // Abstract methods for subclasses
        public abstract HashSet<Position> GetBlockedPositions();
        public abstract void OnVisit(Hero hero);

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
