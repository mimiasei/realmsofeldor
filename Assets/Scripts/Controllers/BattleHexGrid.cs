using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Utility class for battle hex grid coordinate conversions.
    /// Based on VCMI's BattleFieldController hex positioning algorithm.
    /// Uses offset row layout: even rows are shifted right by half hex width.
    /// </summary>
    public static class BattleHexGrid
    {
        // Constants from VCMI and HoMM3
        public const int BATTLE_WIDTH = 17;  // Number of hex columns
        public const int BATTLE_HEIGHT = 11; // Number of hex rows

        // Visual dimensions (can be adjusted for Unity)
        public const float HEX_WIDTH = 44f;   // Pixel width of hex
        public const float HEX_HEIGHT = 42f;  // Pixel height of hex

        // Derived constants
        private const float HALF_HEX_WIDTH = HEX_WIDTH * 0.5f;
        private const float ROW_OFFSET = HEX_HEIGHT; // Vertical distance between rows

        /// <summary>
        /// Converts battlefield hex coordinates to Unity world position.
        /// </summary>
        /// <param name="hexX">Hex column (0-16)</param>
        /// <param name="hexY">Hex row (0-10)</param>
        /// <returns>World position in Unity space</returns>
        public static Vector3 HexToWorld(int hexX, int hexY)
        {
            // Calculate base X position
            var worldX = hexX * HEX_WIDTH;

            // Even rows (0, 2, 4...) are shifted right by half hex width
            if (hexY % 2 == 0)
            {
                worldX += HALF_HEX_WIDTH;
            }

            // Calculate Y position (rows go top-to-bottom)
            var worldY = hexY * ROW_OFFSET;

            return new Vector3(worldX, worldY, 0f);
        }

        /// <summary>
        /// Converts Unity world position to battlefield hex coordinates.
        /// Returns the closest hex to the given world position.
        /// </summary>
        /// <param name="worldPos">World position</param>
        /// <returns>Hex coordinates (x, y) or (-1, -1) if out of bounds</returns>
        public static Vector2Int WorldToHex(Vector3 worldPos)
        {
            // Approximate row from Y coordinate
            var hexY = Mathf.RoundToInt(worldPos.y / ROW_OFFSET);

            // Adjust X based on row offset
            var adjustedX = worldPos.x;
            if (hexY % 2 == 0)
            {
                adjustedX -= HALF_HEX_WIDTH;
            }

            // Calculate column
            var hexX = Mathf.RoundToInt(adjustedX / HEX_WIDTH);

            // Validate bounds
            if (hexX < 0 || hexX >= BATTLE_WIDTH || hexY < 0 || hexY >= BATTLE_HEIGHT)
            {
                return new Vector2Int(-1, -1);
            }

            return new Vector2Int(hexX, hexY);
        }

        /// <summary>
        /// Checks if hex coordinates are within battlefield bounds.
        /// </summary>
        public static bool IsValidHex(int hexX, int hexY)
        {
            return hexX >= 0 && hexX < BATTLE_WIDTH && hexY >= 0 && hexY < BATTLE_HEIGHT;
        }

        /// <summary>
        /// Checks if hex coordinates are within battlefield bounds.
        /// </summary>
        public static bool IsValidHex(Vector2Int hex)
        {
            return IsValidHex(hex.x, hex.y);
        }

        /// <summary>
        /// Gets the center position of the entire battlefield.
        /// Useful for centering the camera.
        /// </summary>
        public static Vector3 GetBattlefieldCenter()
        {
            var centerX = (BATTLE_WIDTH - 1) * HEX_WIDTH * 0.5f;
            var centerY = (BATTLE_HEIGHT - 1) * ROW_OFFSET * 0.5f;
            return new Vector3(centerX, centerY, 0f);
        }

        /// <summary>
        /// Calculates Manhattan distance between two hexes.
        /// Used for movement range calculations.
        /// </summary>
        public static int GetHexDistance(int x1, int y1, int x2, int y2)
        {
            // Convert to axial coordinates for proper hex distance
            var q1 = x1 - (y1 - (y1 & 1)) / 2;
            var r1 = y1;

            var q2 = x2 - (y2 - (y2 & 1)) / 2;
            var r2 = y2;

            var dq = Mathf.Abs(q1 - q2);
            var dr = Mathf.Abs(r1 - r2);
            var ds = Mathf.Abs((q1 + r1) - (q2 + r2));

            return Mathf.Max(dq, Mathf.Max(dr, ds));
        }

        /// <summary>
        /// Calculates Manhattan distance between two hexes.
        /// </summary>
        public static int GetHexDistance(Vector2Int hex1, Vector2Int hex2)
        {
            return GetHexDistance(hex1.x, hex1.y, hex2.x, hex2.y);
        }

        /// <summary>
        /// Gets all 6 neighboring hex coordinates.
        /// Returns only valid hexes within battlefield bounds.
        /// </summary>
        public static Vector2Int[] GetNeighbors(int hexX, int hexY)
        {
            var neighbors = new System.Collections.Generic.List<Vector2Int>();

            // Offset coordinates have different neighbor patterns for even/odd rows
            var isEvenRow = hexY % 2 == 0;

            // Define neighbor offsets based on row parity
            int[,] offsets;
            if (isEvenRow)
            {
                // Even rows (shifted right)
                offsets = new int[,] {
                    { 0, -1 },  // Top-left
                    { 1, -1 },  // Top-right
                    { -1, 0 },  // Left
                    { 1, 0 },   // Right
                    { 0, 1 },   // Bottom-left
                    { 1, 1 }    // Bottom-right
                };
            }
            else
            {
                // Odd rows
                offsets = new int[,] {
                    { -1, -1 }, // Top-left
                    { 0, -1 },  // Top-right
                    { -1, 0 },  // Left
                    { 1, 0 },   // Right
                    { -1, 1 },  // Bottom-left
                    { 0, 1 }    // Bottom-right
                };
            }

            // Add valid neighbors
            for (var i = 0; i < 6; i++)
            {
                var nx = hexX + offsets[i, 0];
                var ny = hexY + offsets[i, 1];

                if (IsValidHex(nx, ny))
                {
                    neighbors.Add(new Vector2Int(nx, ny));
                }
            }

            return neighbors.ToArray();
        }

        /// <summary>
        /// Gets all 6 neighboring hex coordinates.
        /// </summary>
        public static Vector2Int[] GetNeighbors(Vector2Int hex)
        {
            return GetNeighbors(hex.x, hex.y);
        }
    }
}
