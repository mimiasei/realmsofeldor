using UnityEngine;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Utilities
{
    /// <summary>
    /// Extension methods for converting between Position and Unity types
    /// Separated from core Position struct to keep Core assembly Unity-independent
    /// </summary>
    public static class PositionExtensions
    {
        /// <summary>
        /// Convert Position to Unity Vector2Int
        /// </summary>
        public static Vector2Int ToVector2Int(this Position pos)
        {
            return new Vector2Int(pos.X, pos.Y);
        }

        /// <summary>
        /// Convert Position to Unity Vector3 (Z = 0)
        /// </summary>
        public static Vector3 ToVector3(this Position pos)
        {
            return new Vector3(pos.X, pos.Y, 0);
        }

        /// <summary>
        /// Convert Vector2Int to Position
        /// </summary>
        public static Position ToPosition(this Vector2Int v)
        {
            return new Position(v.x, v.y);
        }

        /// <summary>
        /// Convert Vector3 to Position (ignores Z)
        /// </summary>
        public static Position ToPosition(this Vector3 v)
        {
            return new Position(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }
    }
}
