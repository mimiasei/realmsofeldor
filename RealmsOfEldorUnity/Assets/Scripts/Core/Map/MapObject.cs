using System;
using System.Collections.Generic;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Abstract base class for all objects that can appear on the adventure map.
    /// Based on VCMI's CGObjectInstance (CGObjectInstance.h).
    /// Pure C# class with no Unity dependencies.
    /// </summary>
    [Serializable]
    public abstract class MapObject
    {
        /// <summary>
        /// Unique instance identifier for this object.
        /// Corresponds to position in GameMap's object list.
        /// </summary>
        public int InstanceId { get; set; }

        /// <summary>
        /// Type of map object (mine, resource, dwelling, etc.)
        /// </summary>
        public MapObjectType ObjectType { get; protected set; }

        /// <summary>
        /// Position of the object's anchor point (bottom-right corner in VCMI).
        /// For simplicity in Unity, this is the object's primary tile.
        /// </summary>
        public Position Position { get; set; }

        /// <summary>
        /// Current owner of this object (if applicable).
        /// PlayerColor.Neutral for unowned objects.
        /// </summary>
        public PlayerColor Owner { get; set; }

        /// <summary>
        /// If true, object blocks movement through its tiles.
        /// </summary>
        public bool BlocksMovement { get; protected set; }

        /// <summary>
        /// If true, object can be visited/interacted with by heroes.
        /// Based on VCMI's isVisitable.
        /// </summary>
        public bool IsVisitable { get; protected set; }

        /// <summary>
        /// If true, object can only be visited from adjacent tiles (hero cannot stand on it).
        /// Based on VCMI's blockVisit.
        /// </summary>
        public bool BlockedVisitable { get; protected set; }

        /// <summary>
        /// If true, object can be removed from the map.
        /// Based on VCMI's removable.
        /// </summary>
        public bool IsRemovable { get; protected set; }

        /// <summary>
        /// Custom name for this object instance (optional).
        /// </summary>
        public string InstanceName { get; set; }

        protected MapObject(MapObjectType objectType, Position position)
        {
            ObjectType = objectType;
            Position = position;
            Owner = PlayerColor.Neutral;
            InstanceName = string.Empty;

            // Default values (override in derived classes as needed)
            BlocksMovement = true;
            IsVisitable = false;
            BlockedVisitable = false;
            IsRemovable = false;
        }

        /// <summary>
        /// Gets the set of all positions blocked by this object.
        /// For multi-tile objects, returns all occupied tiles.
        /// Based on VCMI's getBlockedPos.
        /// </summary>
        public abstract HashSet<Position> GetBlockedPositions();

        /// <summary>
        /// Gets the set of all positions from which this object can be visited.
        /// For most objects, this is adjacent tiles. For some (like towns), hero can stand on them.
        /// </summary>
        public virtual HashSet<Position> GetVisitablePositions()
        {
            if (!IsVisitable)
                return new HashSet<Position>();

            // Default: object occupies one tile and can be visited from adjacent tiles
            var visitablePositions = new HashSet<Position>();

            if (BlockedVisitable)
            {
                // Can only visit from adjacent tiles
                var adjacentOffsets = new[]
                {
                    new Position(0, 1),   // North
                    new Position(1, 0),   // East
                    new Position(0, -1),  // South
                    new Position(-1, 0),  // West
                    new Position(1, 1),   // NE
                    new Position(1, -1),  // SE
                    new Position(-1, -1), // SW
                    new Position(-1, 1)   // NW
                };

                foreach (var offset in adjacentOffsets)
                {
                    visitablePositions.Add(new Position(Position.X + offset.X, Position.Y + offset.Y));
                }
            }
            else
            {
                // Hero can stand on the object itself
                visitablePositions.Add(Position);
            }

            return visitablePositions;
        }

        /// <summary>
        /// Called when a hero visits this object.
        /// Override in derived classes to implement specific behavior.
        /// </summary>
        public abstract void OnVisit(Hero hero);

        /// <summary>
        /// Checks if the object is visitable at the specified position.
        /// Based on VCMI's visitableAt.
        /// </summary>
        public virtual bool IsVisitableAt(Position pos)
        {
            if (!IsVisitable)
                return false;

            return GetVisitablePositions().Contains(pos);
        }

        /// <summary>
        /// Checks if the object blocks movement at the specified position.
        /// Based on VCMI's blockingAt.
        /// </summary>
        public virtual bool IsBlockingAt(Position pos)
        {
            if (!BlocksMovement)
                return false;

            return GetBlockedPositions().Contains(pos);
        }

        /// <summary>
        /// Changes the owner of this object.
        /// </summary>
        public virtual void SetOwner(PlayerColor newOwner)
        {
            Owner = newOwner;
        }

        public override string ToString()
        {
            var name = !string.IsNullOrEmpty(InstanceName) ? InstanceName : ObjectType.ToString();
            return $"{name} at {Position} (Owner: {Owner})";
        }
    }

    /// <summary>
    /// Simple resource pile object (gold, wood, ore, etc.).
    /// Gives resources to hero when picked up and is removed.
    /// </summary>
    [Serializable]
    public class ResourceObject : MapObject
    {
        public ResourceType ResourceType { get; private set; }
        public int Amount { get; private set; }

        public ResourceObject(Position position, ResourceType resourceType, int amount)
            : base(MapObjectType.Resource, position)
        {
            ResourceType = resourceType;
            Amount = amount;
            BlocksMovement = false;
            IsVisitable = true;
            BlockedVisitable = false; // Hero can stand on resource
            IsRemovable = true;
        }

        public override HashSet<Position> GetBlockedPositions()
        {
            // Resources don't block movement
            return new HashSet<Position>();
        }

        public override void OnVisit(Hero hero)
        {
            // Give resource to hero's player
            // This will be handled by GameState in full implementation
        }
    }

    /// <summary>
    /// Mine object that generates daily resources for its owner.
    /// </summary>
    [Serializable]
    public class MineObject : MapObject
    {
        public ResourceType ResourceType { get; private set; }
        public int DailyProduction { get; private set; }

        public MineObject(Position position, ResourceType resourceType, int dailyProduction)
            : base(MapObjectType.Mine, position)
        {
            ResourceType = resourceType;
            DailyProduction = dailyProduction;
            BlocksMovement = true;
            IsVisitable = true;
            BlockedVisitable = true; // Must visit from adjacent tile
            IsRemovable = false;
        }

        public override HashSet<Position> GetBlockedPositions()
        {
            // Mine occupies single tile
            return new HashSet<Position> { Position };
        }

        public override void OnVisit(Hero hero)
        {
            // Flag mine for hero's player
            // This will be handled by GameState in full implementation
        }
    }

    /// <summary>
    /// Monster dwelling that allows recruitment of creatures.
    /// </summary>
    [Serializable]
    public class DwellingObject : MapObject
    {
        public int CreatureId { get; private set; }
        public int AvailableCount { get; set; }
        public int WeeklyGrowth { get; private set; }

        public DwellingObject(Position position, int creatureId, int initialCount, int weeklyGrowth)
            : base(MapObjectType.Dwelling, position)
        {
            CreatureId = creatureId;
            AvailableCount = initialCount;
            WeeklyGrowth = weeklyGrowth;
            BlocksMovement = true;
            IsVisitable = true;
            BlockedVisitable = true;
            IsRemovable = false;
        }

        public override HashSet<Position> GetBlockedPositions()
        {
            return new HashSet<Position> { Position };
        }

        public override void OnVisit(Hero hero)
        {
            // Open recruitment dialog
            // This will be handled by UI layer in full implementation
        }

        /// <summary>
        /// Called at the start of each week to add new creatures.
        /// </summary>
        public void AddWeeklyGrowth()
        {
            AvailableCount += WeeklyGrowth;
        }
    }
}
