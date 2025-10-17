using System;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Map;

namespace RealmsOfEldor.Data.EventChannels
{
    /// <summary>
    /// Event channel for map-related events.
    /// Decouples map logic from UI/rendering systems using ScriptableObject-based events.
    /// </summary>
    [CreateAssetMenu(fileName = "MapEventChannel", menuName = "Realms of Eldor/Event Channels/Map Events")]
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
        public event Action<MapObject, Position> OnObjectMoved;
        public event Action<MapObject, PlayerColor> OnObjectOwnerChanged;
        public event Action<MapObject, Hero> OnObjectVisited;

        // Hero movement events (map-specific)
        public event Action<Hero, Position> OnHeroMovedOnMap;
        public event Action<Hero, Position> OnHeroTeleported;

        // Map selection events
        public event Action<Position> OnTileSelected;
        public event Action<Position[]> OnTilesHighlighted;
        public event Action OnSelectionCleared;

        #region Map Lifecycle

        public void RaiseMapLoaded(GameMap map)
        {
            OnMapLoaded?.Invoke(map);
        }

        public void RaiseMapUnloaded()
        {
            OnMapUnloaded?.Invoke();
        }

        #endregion

        #region Terrain Events

        public void RaiseTerrainChanged(Position position, TerrainType newTerrain)
        {
            OnTerrainChanged?.Invoke(position, newTerrain);
        }

        public void RaiseTileUpdated(Position position)
        {
            OnTileUpdated?.Invoke(position);
        }

        #endregion

        #region Object Events

        public void RaiseObjectAdded(MapObject obj)
        {
            OnObjectAdded?.Invoke(obj);
        }

        public void RaiseObjectRemoved(int objectId)
        {
            OnObjectRemoved?.Invoke(objectId);
        }

        public void RaiseObjectMoved(MapObject obj, Position newPosition)
        {
            OnObjectMoved?.Invoke(obj, newPosition);
        }

        public void RaiseObjectOwnerChanged(MapObject obj, PlayerColor newOwner)
        {
            OnObjectOwnerChanged?.Invoke(obj, newOwner);
        }

        public void RaiseObjectVisited(MapObject obj, Hero hero)
        {
            OnObjectVisited?.Invoke(obj, hero);
        }

        #endregion

        #region Hero Movement Events

        public void RaiseHeroMovedOnMap(Hero hero, Position newPosition)
        {
            OnHeroMovedOnMap?.Invoke(hero, newPosition);
        }

        public void RaiseHeroTeleported(Hero hero, Position newPosition)
        {
            OnHeroTeleported?.Invoke(hero, newPosition);
        }

        #endregion

        #region Selection Events

        public void RaiseTileSelected(Position position)
        {
            OnTileSelected?.Invoke(position);
        }

        public void RaiseTilesHighlighted(Position[] positions)
        {
            OnTilesHighlighted?.Invoke(positions);
        }

        public void RaiseSelectionCleared()
        {
            OnSelectionCleared?.Invoke();
        }

        #endregion

        /// <summary>
        /// Clears all event subscriptions. Call when changing scenes.
        /// </summary>
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
            OnHeroMovedOnMap = null;
            OnHeroTeleported = null;
            OnTileSelected = null;
            OnTilesHighlighted = null;
            OnSelectionCleared = null;
        }
    }
}
