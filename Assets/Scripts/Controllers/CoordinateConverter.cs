using UnityEngine;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Converts between different coordinate systems used in the game.
    /// Handles conversion between game logic positions and 3D world coordinates.
    ///
    /// Coordinate Systems:
    /// - Game Logic: Position(x, y) - abstract 2D grid coordinates
    /// - 3D World: Vector3(x, y, z) - 3D ground plane on X,Z (Y=height)
    /// </summary>
    public static class CoordinateConverter
    {
        /// <summary>
        /// Converts game logic Position to 3D world position on ground plane.
        /// This is the NEW coordinate system for Cartographer rendering.
        /// </summary>
        /// <param name="pos">Game logic position (X,Y grid)</param>
        /// <param name="heightOffset">Height above ground (Y axis)</param>
        /// <returns>World position on X,Z ground plane</returns>
        public static Vector3 PositionToWorld3D(Position pos, float heightOffset = 0f)
        {
            return new Vector3(pos.X + 0.5f, heightOffset, pos.Y + 0.5f);
        }

        /// <summary>
        /// Converts 3D world position to game logic Position.
        /// Expects position on X,Z ground plane.
        /// </summary>
        /// <param name="worldPos">World position (uses X and Z, ignores Y)</param>
        /// <returns>Game logic position (X,Y grid)</returns>
        public static Position WorldToPosition3D(Vector3 worldPos)
        {
            return new Position(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.z)
            );
        }

        /// <summary>
        /// Raycasts from screen point to 3D ground plane (Y=0).
        /// Used for mouse input in Cartographer system.
        /// </summary>
        /// <param name="camera">Camera to raycast from</param>
        /// <param name="screenPoint">Screen position (usually Input.mousePosition)</param>
        /// <param name="hitPoint">World position where ray hits ground plane</param>
        /// <returns>True if ray hit the ground plane</returns>
        public static bool ScreenToWorld3D(Camera camera, Vector3 screenPoint, out Vector3 hitPoint)
        {
            // Safety check: ensure screen point is within camera viewport bounds
            // This prevents Unity's "Screen position out of view frustum" errors
            if (screenPoint.x < 0 || screenPoint.x > Screen.width ||
                screenPoint.y < 0 || screenPoint.y > Screen.height)
            {
                Debug.LogWarning($"CoordinateConverter: Screen point {screenPoint} is outside screen bounds ({Screen.width}x{Screen.height})");
                hitPoint = Vector3.zero;
                return false;
            }

            // Verify camera is valid
            if (camera == null)
            {
                Debug.LogError("CoordinateConverter: Camera is null!");
                hitPoint = Vector3.zero;
                return false;
            }

            try
            {
                Ray ray = camera.ScreenPointToRay(screenPoint);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Y=0 ground plane

                Debug.Log($"CoordinateConverter: Camera '{camera.gameObject.name}' at position {camera.transform.position}, rotation {camera.transform.rotation.eulerAngles}");
                Debug.Log($"CoordinateConverter: Ray from screen {screenPoint}: origin={ray.origin}, direction={ray.direction}");

                // Check if ray direction has negative Y component (pointing down)
                if (ray.direction.y >= 0)
                {
                    Debug.LogWarning($"CoordinateConverter: Ray direction.y is {ray.direction.y:F3} (>= 0), meaning ray is pointing UP or HORIZONTAL, not down! Camera may be misconfigured.");
                }

                if (groundPlane.Raycast(ray, out float enter))
                {
                    hitPoint = ray.GetPoint(enter);
                    Debug.Log($"CoordinateConverter: Ground plane hit at distance={enter}, hitPoint={hitPoint}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"CoordinateConverter: Ray did not intersect ground plane. Camera GameObject='{camera.gameObject.name}', pos={camera.transform.position}, forward={camera.transform.forward}, rotation={camera.transform.rotation.eulerAngles}");
                    Debug.LogWarning($"CoordinateConverter: Ray.direction.y = {ray.direction.y:F3} (must be negative to hit ground below)");
                }
            }
            catch (System.Exception ex)
            {
                // Silently catch raycast exceptions (mouse outside frustum)
                Debug.LogError($"CoordinateConverter: Exception during raycast: {ex.Message}");
                hitPoint = Vector3.zero;
                return false;
            }

            hitPoint = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Gets tile center in 3D world space.
        /// Adds 0.5 offset to center position on tile.
        /// </summary>
        /// <param name="pos">Tile position</param>
        /// <param name="heightOffset">Height above ground</param>
        /// <returns>Center of tile in world space</returns>
        public static Vector3 GetTileCenter3D(Position pos, float heightOffset = 0f)
        {
            return new Vector3(pos.X + 0.5f, heightOffset, pos.Y + 0.5f);
        }

        /// <summary>
        /// Gets tile corner (bottom-left) in 3D world space.
        /// </summary>
        /// <param name="pos">Tile position</param>
        /// <param name="heightOffset">Height above ground</param>
        /// <returns>Corner of tile in world space</returns>
        public static Vector3 GetTileCorner3D(Position pos, float heightOffset = 0f)
        {
            return new Vector3(pos.X, heightOffset, pos.Y);
        }

        /// <summary>
        /// Checks if a world position is within map bounds.
        /// </summary>
        /// <param name="worldPos">World position to check</param>
        /// <param name="mapWidth">Map width in tiles</param>
        /// <param name="mapHeight">Map height in tiles</param>
        /// <returns>True if position is within bounds</returns>
        public static bool IsWorldPositionInBounds(Vector3 worldPos, int mapWidth, int mapHeight)
        {
            return worldPos.x >= 0 && worldPos.x < mapWidth &&
                   worldPos.z >= 0 && worldPos.z < mapHeight;
        }

        /// <summary>
        /// Calculates map bounds in 3D world space.
        /// Returns min and max corners of the map.
        /// </summary>
        /// <param name="mapWidth">Map width in tiles</param>
        /// <param name="mapHeight">Map height in tiles</param>
        /// <returns>(min corner, max corner)</returns>
        public static (Vector3 min, Vector3 max) GetMapBounds3D(int mapWidth, int mapHeight)
        {
            Vector3 min = new Vector3(0, 0, 0);
            Vector3 max = new Vector3(mapWidth, 0, mapHeight);
            return (min, max);
        }

        /// <summary>
        /// Gets the center of the map in 3D world space.
        /// </summary>
        /// <param name="mapWidth">Map width in tiles</param>
        /// <param name="mapHeight">Map height in tiles</param>
        /// <param name="heightOffset">Height above ground</param>
        /// <returns>Center position of map</returns>
        public static Vector3 GetMapCenter3D(int mapWidth, int mapHeight, float heightOffset = 0f)
        {
            return new Vector3(mapWidth * 0.5f, heightOffset, mapHeight * 0.5f);
        }
    }
}
