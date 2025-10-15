using UnityEngine;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Factory for creating map objects from ScriptableObject configurations.
    /// Bridges design-time MapObjectConfig assets to runtime MapObject instances.
    /// Based on VCMI's object instantiation pattern.
    /// </summary>
    public static class MapObjectFactory
    {
        /// <summary>
        /// Creates a runtime MapObject from a ScriptableObject configuration.
        /// Supports both designer-created assets and procedural generation.
        /// </summary>
        /// <param name="config">MapObjectConfig ScriptableObject asset</param>
        /// <param name="position">Position on the map</param>
        /// <returns>Runtime MapObject instance (may be base class or specialized subclass)</returns>
        public static MapObject CreateFromConfig(object config, Position position)
        {
            // Handle editor-only type (MapObjectConfig is defined in Editor assembly)
            // We use dynamic to avoid assembly reference issues
            if (config == null)
            {
                Debug.LogError("MapObjectFactory: config is null");
                return null;
            }

            // Use reflection to read ScriptableObject properties
            var configType = config.GetType();
            var objectTypeField = configType.GetField("objectType");
            var objectNameField = configType.GetField("objectName");
            var isBlockingField = configType.GetField("isBlocking");
            var isVisitableField = configType.GetField("isVisitable");
            var resourceTypeField = configType.GetField("resourceType");
            var resourceAmountField = configType.GetField("resourceAmount");
            var productionAmountField = configType.GetField("productionAmount");
            var creatureIdField = configType.GetField("creatureId");
            var weeklyGrowthField = configType.GetField("weeklyGrowth");

            if (objectTypeField == null)
            {
                Debug.LogError("MapObjectFactory: objectType field not found on config");
                return null;
            }

            var objectType = (MapObjectType)objectTypeField.GetValue(config);
            var objectName = objectNameField?.GetValue(config) as string ?? objectType.ToString();
            var isBlocking = isBlockingField != null && (bool)isBlockingField.GetValue(config);
            var isVisitable = isVisitableField != null && (bool)isVisitableField.GetValue(config);

            MapObject mapObject;

            switch (objectType)
            {
                case MapObjectType.Resource:
                    var resourceType = (ResourceType)(resourceTypeField?.GetValue(config) ?? ResourceType.Gold);
                    var resourceAmount = resourceAmountField != null ? (int)resourceAmountField.GetValue(config) : 500;
                    mapObject = new ResourceObject(position, resourceType, resourceAmount);
                    break;

                case MapObjectType.Mine:
                    var mineResourceType = (ResourceType)(resourceTypeField?.GetValue(config) ?? ResourceType.Gold);
                    var productionAmount = productionAmountField != null ? (int)productionAmountField.GetValue(config) : 1;
                    mapObject = new MineObject(position, mineResourceType, productionAmount);
                    break;

                case MapObjectType.Dwelling:
                    var creatureId = creatureIdField != null ? (int)creatureIdField.GetValue(config) : 1;
                    var weeklyGrowth = weeklyGrowthField != null ? (int)weeklyGrowthField.GetValue(config) : 10;
                    mapObject = new DwellingObject(position, creatureId, weeklyGrowth);
                    break;

                case MapObjectType.Decorative:
                case MapObjectType.Visitable:
                default:
                    // Use base MapObject class for decoratives and other simple objects
                    mapObject = new MapObject(objectType, position)
                    {
                        Name = objectName,
                        IsBlocking = isBlocking,
                        IsVisitable = isVisitable,
                        IsRemovable = false
                    };
                    break;
            }

            return mapObject;
        }

        /// <summary>
        /// Creates a resource object directly (for procedural generation).
        /// Use this when you don't have a MapObjectConfig asset.
        /// </summary>
        public static ResourceObject CreateResource(Position position, ResourceType resourceType, int amount)
        {
            return new ResourceObject(position, resourceType, amount);
        }

        /// <summary>
        /// Creates a mine object directly (for procedural generation).
        /// Use this when you don't have a MapObjectConfig asset.
        /// </summary>
        public static MineObject CreateMine(Position position, ResourceType resourceType, int dailyProduction = 1)
        {
            return new MineObject(position, resourceType, dailyProduction);
        }

        /// <summary>
        /// Creates a dwelling object directly (for procedural generation).
        /// Use this when you don't have a MapObjectConfig asset.
        /// </summary>
        public static DwellingObject CreateDwelling(Position position, int creatureId, int weeklyGrowth)
        {
            return new DwellingObject(position, creatureId, weeklyGrowth);
        }

        /// <summary>
        /// Creates a decorative object directly (for procedural generation).
        /// Use this when you don't have a MapObjectConfig asset.
        /// </summary>
        public static MapObject CreateDecorative(Position position, string name, bool isBlocking)
        {
            return new MapObject(MapObjectType.Decorative, position)
            {
                Name = name,
                IsBlocking = isBlocking,
                IsVisitable = false,
                IsRemovable = false
            };
        }
    }
}
