using System.Collections.Generic;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Tracks budget spending during map generation.
    /// Based on VCMI's treasure budget and object count limits.
    /// Prevents over-saturation of resources and ensures balanced maps.
    /// </summary>
    public class MapGenBudget
    {
        private readonly MapGenConfig _config;

        // Budget tracking
        private int _treasureBudgetSpent;
        private int _minesPlaced;
        private int _dwellingsPlaced;
        private int _resourcePilesPlaced;

        // Placement tracking
        private readonly List<MapObject> _placedObjects = new List<MapObject>();

        public MapGenBudget(MapGenConfig config)
        {
            _config = config;
            _treasureBudgetSpent = 0;
            _minesPlaced = 0;
            _dwellingsPlaced = 0;
            _resourcePilesPlaced = 0;
        }

        #region Budget Queries

        /// <summary>
        /// Checks if we can place a resource pile within treasure budget.
        /// </summary>
        public bool CanPlaceResourcePile(int value)
        {
            if (_resourcePilesPlaced >= _config.resourcePileCount)
                return false;

            if (_treasureBudgetSpent + value > _config.treasureBudget)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if we can place a mine within count limit.
        /// </summary>
        public bool CanPlaceMine()
        {
            return _minesPlaced < _config.mineCount;
        }

        /// <summary>
        /// Checks if we can place a dwelling within count limit.
        /// </summary>
        public bool CanPlaceDwelling()
        {
            return _dwellingsPlaced < _config.dwellingCount;
        }

        /// <summary>
        /// Gets remaining treasure budget.
        /// </summary>
        public int RemainingTreasureBudget => _config.treasureBudget - _treasureBudgetSpent;

        /// <summary>
        /// Gets remaining mine slots.
        /// </summary>
        public int RemainingMineSlots => _config.mineCount - _minesPlaced;

        /// <summary>
        /// Gets remaining dwelling slots.
        /// </summary>
        public int RemainingDwellingSlots => _config.dwellingCount - _dwellingsPlaced;

        /// <summary>
        /// Gets remaining resource pile slots.
        /// </summary>
        public int RemainingResourcePileSlots => _config.resourcePileCount - _resourcePilesPlaced;

        #endregion

        #region Budget Recording

        /// <summary>
        /// Records a placed resource pile and deducts from treasure budget.
        /// </summary>
        public void RecordResourcePile(ResourceObject resource)
        {
            _resourcePilesPlaced++;
            _treasureBudgetSpent += resource.Value;
            _placedObjects.Add(resource);
        }

        /// <summary>
        /// Records a placed mine.
        /// </summary>
        public void RecordMine(MineObject mine)
        {
            _minesPlaced++;
            _placedObjects.Add(mine);
        }

        /// <summary>
        /// Records a placed dwelling.
        /// </summary>
        public void RecordDwelling(DwellingObject dwelling)
        {
            _dwellingsPlaced++;
            _placedObjects.Add(dwelling);
        }

        /// <summary>
        /// Records any map object (generic).
        /// </summary>
        public void RecordObject(MapObject obj)
        {
            if (obj is ResourceObject resource)
            {
                RecordResourcePile(resource);
            }
            else if (obj is MineObject mine)
            {
                RecordMine(mine);
            }
            else if (obj is DwellingObject dwelling)
            {
                RecordDwelling(dwelling);
            }
            else
            {
                // Decorative or other objects don't count against budget
                _placedObjects.Add(obj);
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets total number of objects placed.
        /// </summary>
        public int TotalObjectsPlaced => _placedObjects.Count;

        /// <summary>
        /// Gets total treasure value placed.
        /// </summary>
        public int TotalTreasureValue => _treasureBudgetSpent;

        /// <summary>
        /// Gets all placed objects (read-only).
        /// </summary>
        public IReadOnlyList<MapObject> PlacedObjects => _placedObjects.AsReadOnly();

        /// <summary>
        /// Gets budget utilization percentage (0.0 - 1.0).
        /// </summary>
        public float BudgetUtilization
        {
            get
            {
                if (_config.treasureBudget == 0)
                    return 0f;
                return (float)_treasureBudgetSpent / _config.treasureBudget;
            }
        }

        /// <summary>
        /// Returns a summary of budget status for logging.
        /// </summary>
        public string GetSummary()
        {
            return $"Budget Status:\n" +
                   $"  Treasure: {_treasureBudgetSpent}/{_config.treasureBudget} ({BudgetUtilization:P0})\n" +
                   $"  Resource Piles: {_resourcePilesPlaced}/{_config.resourcePileCount}\n" +
                   $"  Mines: {_minesPlaced}/{_config.mineCount}\n" +
                   $"  Dwellings: {_dwellingsPlaced}/{_config.dwellingCount}\n" +
                   $"  Total Objects: {TotalObjectsPlaced}";
        }

        #endregion
    }
}
