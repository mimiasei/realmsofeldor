using System;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// 2D position on the game map
    /// Pure C# implementation (no Unity dependencies)
    /// Use PositionExtensions for Unity type conversions
    /// </summary>
    [Serializable]
    public struct Position : IEquatable<Position>
    {
        public int X;
        public int Y;

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Calculate Manhattan distance to another position
        /// </summary>
        public int ManhattanDistance(Position other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        /// <summary>
        /// Calculate Euclidean distance to another position
        /// </summary>
        public float Distance(Position other)
        {
            int dx = X - other.X;
            int dy = Y - other.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Check if positions are adjacent (including diagonals)
        /// </summary>
        public bool IsAdjacentTo(Position other)
        {
            var dx = Math.Abs(X - other.X);
            var dy = Math.Abs(Y - other.Y);
            return dx <= 1 && dy <= 1 && (dx + dy) > 0;
        }

        public bool Equals(Position other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Position other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Position a, Position b) => a.Equals(b);
        public static bool operator !=(Position a, Position b) => !a.Equals(b);

        public override string ToString() => $"({X}, {Y})";
    }
}
