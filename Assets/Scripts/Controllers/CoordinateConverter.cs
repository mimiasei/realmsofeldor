using UnityEngine;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Converts between different coordinate systems used in the game.
    /// Handles migration from old 2D system (X,Y plane, Z=0) to new 3D system (X,Z plane, Y=height).
    ///
    /// Coordinate Systems:
    /// - Game Logic: Position(x, y) - abstract 2D grid coordinates
    /// - Old 2D World: Vector3(x, y, 0) - flat 2D on X,Y plane
    /// - New 3D World: Vector3(x, 0, y) - 3D ground plane on X,Z, Y=height
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
        /// Converts game logic Position to old 2D world position.
        /// This is the OLD coordinate system, kept for backward compatibility during migration.
        /// </summary>
        /// <param name="pos">Game logic position</param>
        /// <returns>World position on X,Y plane (Z=0)</returns>
        [System.Obsolete("Use PositionToWorld3D instead for new Cartographer system")]
        public static Vector3 PositionToWorld2D(Position pos)
        {
            return new Vector3(pos.X + 0.5f, pos.Y + 0.5f, 0f);
        }

        /// <summary>
        /// Converts old 2D world position to game logic Position.
        /// Expects position on X,Y plane (Z=0).
        /// </summary>
        /// <param name="worldPos">World position (uses X and Y, ignores Z)</param>
        /// <returns>Game logic position</returns>
        [System.Obsolete("Use WorldToPosition3D instead for new Cartographer system")]
        public static Position WorldToPosition2D(Vector3 worldPos)
        {
            return new Position(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.y)
            );
        }

        /// <summary>
        /// Converts old 2D world position to new 3D world position.
        /// Useful during migration to convert existing positions.
        /// </summary>
        /// <param name="worldPos2D">Old world position (X,Y,0)</param>
        /// <param name="heightOffset">Height in new system</param>
        /// <returns>New world position (X,height,Z)</returns>
        public static Vector3 World2DToWorld3D(Vector3 worldPos2D, float heightOffset = 0f)
        {
            return new Vector3(worldPos2D.x, heightOffset, worldPos2D.y);
        }

        /// <summary>
        /// Converts new 3D world position to old 2D world position.
        /// Useful for backward compatibility during migration.
        /// </summary>
        /// <param name="worldPos3D">New world position (X,Y,Z)</param>
        /// <returns>Old world position (X,Y,0)</returns>
        public static Vector3 World3DToWorld2D(Vector3 worldPos3D)
        {
            return new Vector3(worldPos3D.x, worldPos3D.z, 0f);
        }

        /// <summary>
        /// Raycasts from screen point to 3D ground plane (Y=0).
        /// Used for mouse input in new Cartographer system.
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
                hitPoint = Vector3.zero;
                return false;
            }

            try
            {
                Ray ray = camera.ScreenPointToRay(screenPoint);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Y=0 ground plane

                if (groundPlane.Raycast(ray, out float enter))
                {
                    hitPoint = ray.GetPoint(enter);
                    return true;
                }
            }
            catch (System.Exception)
            {
                // Silently catch raycast exceptions (mouse outside frustum)
                hitPoint = Vector3.zero;
                return false;
            }

            hitPoint = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Raycasts from screen point to old 2D plane (Z=0).
        /// Used for mouse input in old 2D system.
        /// </summary>
        /// <param name="camera">Camera to raycast from</param>
        /// <param name="screenPoint">Screen position</param>
        /// <param name="hitPoint">World position where ray hits 2D plane</param>
        /// <returns>True if ray hit the 2D plane</returns>
        [System.Obsolete("Use ScreenToWorld3D instead for new Cartographer system")]
        public static bool ScreenToWorld2D(Camera camera, Vector3 screenPoint, out Vector3 hitPoint)
        {
            Ray ray = camera.ScreenPointToRay(screenPoint);
            Plane flatPlane = new Plane(Vector3.forward, Vector3.zero); // Z=0 plane

            if (flatPlane.Raycast(ray, out float enter))
            {
                hitPoint = ray.GetPoint(enter);
                return true;
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
