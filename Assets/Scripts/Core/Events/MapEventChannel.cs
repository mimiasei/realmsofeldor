using System;
using System.Collections.Generic;
using UnityEngine;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Core.Events
{
    /// <summary>
    /// ScriptableObject event channel for map-related events.
    /// Decouples map logic from visual/UI systems.
    /// </summary>
    [CreateAssetMenu(fileName = "MapEventChannel", menuName = "Realms of Eldor/Events/Map Event Channel")]
    public class MapEventChannel : ScriptableObject
    {
        // Map lifecycle events
        public event Action<GameMap> OnMapLoaded;
        public event Action OnMapUnloaded;

        // Terrain events
        public event Action<Position, TerrainType> OnTerrainChanged;
        public event Action<Position> OnTileUpdated;

        // Object events
        public event Action<MapObject> OnObjectAdded;
        public event Action<int> OnObjectRemoved;
        public event Action<MapObject, Position, Position> OnObjectMoved;
        public event Action<MapObject, PlayerColor> OnObjectOwnerChanged;
        public event Action<Hero, MapObject> OnObjectVisited;
        public event Action<MapObject> OnObjectClicked; // Raised when object is clicked

        // Hero movement on map
        public event Action<Hero, Position, Position> OnHeroMovedOnMap;
        public event Action<Hero, Position> OnHeroTeleported;

        // Selection and highlighting
        public event Action<Position> OnTileSelected;
        public event Action<List<Position>> OnTilesHighlighted;
        public event Action OnSelectionCleared;

        // Raise map lifecycle events
        public void RaiseMapLoaded(GameMap map)
        {
            OnMapLoaded?.Invoke(map);
        }

        public void RaiseMapUnloaded()
        {
            OnMapUnloaded?.Invoke();
        }

        // Raise terrain events
        public void RaiseTerrainChanged(Position position, TerrainType newTerrain)
        {
            OnTerrainChanged?.Invoke(position, newTerrain);
        }

        public void RaiseTileUpdated(Position position)
        {
            OnTileUpdated?.Invoke(position);
        }

        // Raise object events
        public void RaiseObjectAdded(MapObject obj)
        {
            OnObjectAdded?.Invoke(obj);
        }

        public void RaiseObjectRemoved(int objectId)
        {
            OnObjectRemoved?.Invoke(objectId);
        }

        public void RaiseObjectMoved(MapObject obj, Position from, Position to)
        {
            OnObjectMoved?.Invoke(obj, from, to);
        }

        public void RaiseObjectOwnerChanged(MapObject obj, PlayerColor newOwner)
        {
            OnObjectOwnerChanged?.Invoke(obj, newOwner);
        }

        public void RaiseObjectVisited(Hero hero, MapObject obj)
        {
            OnObjectVisited?.Invoke(hero, obj);
        }

        public void RaiseObjectClicked(MapObject obj)
        {
            OnObjectClicked?.Invoke(obj);
        }

        // Raise hero movement events
        public void RaiseHeroMovedOnMap(Hero hero, Position from, Position to)
        {
            OnHeroMovedOnMap?.Invoke(hero, from, to);
        }

        public void RaiseHeroTeleported(Hero hero, Position destination)
        {
            OnHeroTeleported?.Invoke(hero, destination);
        }

        // Raise selection events
        public void RaiseTileSelected(Position position)
        {
            OnTileSelected?.Invoke(position);
        }

        public void RaiseTilesHighlighted(List<Position> positions)
        {
            OnTilesHighlighted?.Invoke(positions);
        }

        public void RaiseSelectionCleared()
        {
            OnSelectionCleared?.Invoke();
        }

        // Clear all subscriptions (useful for scene transitions)
        public void ClearAllSubscriptions()
        {
            OnMapLoaded = null;
            OnMapUnloaded = null;
            OnTerrainChanged = null;
            OnTileUpdated = null;
            OnObjectAdded = null;
            OnObjectRemoved = null;
            OnObjectMoved = null;
            OnObjectOwnerChanged = null;
            OnObjectVisited = null;
            OnObjectClicked = null;
            OnHeroMovedOnMap = null;
            OnHeroTeleported = null;
            OnTileSelected = null;
            OnTilesHighlighted = null;
            OnSelectionCleared = null;
        }
    }
}
