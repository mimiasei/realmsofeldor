using System;
using UnityEngine;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Hexagonal battlefield coordinate system.
    /// Based on VCMI's BattleHex.h - 17x11 hex grid (187 hexes total).
    /// </summary>
    [Serializable]
    public struct BattleHex : IEquatable<BattleHex>
    {
        // Grid dimensions (VCMI standard)
        public const int FIELD_WIDTH = 17;
        public const int FIELD_HEIGHT = 11;
        public const int FIELD_SIZE = 187;  // 17 * 11

        // Special hex constants
        public const int INVALID = -1;
        public const int HERO_ATTACKER = 0;
        public const int HERO_DEFENDER = 16;

        /// <summary>
        /// Hex navigation directions (6 neighbors)
        /// </summary>
        public enum Direction
        {
            None = -1,
            TopLeft = 0,
            TopRight = 1,
            Right = 2,
            BottomRight = 3,
            BottomLeft = 4,
            Left = 5
        }

        private int hex;

        // Constructors
        public BattleHex(int hexValue) => hex = hexValue;
        public BattleHex(int x, int y) => SetXY(x, y, out hex);

        // Properties
        public int X => hex % FIELD_WIDTH;
        public int Y => hex / FIELD_WIDTH;
        public int Value => hex;

        public bool IsValid => hex >= 0 && hex < FIELD_SIZE;
        public bool IsAvailable => IsValid && X > 0 && X < FIELD_WIDTH - 1;  // Exclude edge columns

        // Coordinate manipulation
        public void SetXY(int x, int y)
        {
            SetXY(x, y, out hex);
        }

        private static void SetXY(int x, int y, out int result)
        {
            if (x < 0 || x >= FIELD_WIDTH || y < 0 || y >= FIELD_HEIGHT)
            {
                Debug.LogWarning($"Invalid hex coords: ({x}, {y}), clamping to valid range");
                x = Mathf.Clamp(x, 0, FIELD_WIDTH - 1);
                y = Mathf.Clamp(y, 0, FIELD_HEIGHT - 1);
            }
            result = (x + y * FIELD_WIDTH);
        }

        /// <summary>
        /// Get neighboring hex in specified direction.
        /// Handles odd/even row offsets automatically.
        /// </summary>
        public BattleHex GetNeighbor(Direction dir)
        {
            if (dir == Direction.None)
                return this;

            var x = X;
            var y = Y;
            var oddRow = (y % 2) == 1;

            switch (dir)
            {
                case Direction.TopLeft:
                    x = oddRow ? x - 1 : x;
                    y = y - 1;
                    break;
                case Direction.TopRight:
                    x = oddRow ? x : x + 1;
                    y = y - 1;
                    break;
                case Direction.Right:
                    x = x + 1;
                    break;
                case Direction.BottomRight:
                    x = oddRow ? x : x + 1;
                    y = y + 1;
                    break;
                case Direction.BottomLeft:
                    x = oddRow ? x - 1 : x;
                    y = y + 1;
                    break;
                case Direction.Left:
                    x = x - 1;
                    break;
            }

            // Return invalid hex if out of bounds
            if (x < 0 || x >= FIELD_WIDTH || y < 0 || y >= FIELD_HEIGHT)
                return new BattleHex(INVALID);

            return new BattleHex(x, y);
        }

        /// <summary>
        /// Get all 6 neighbors (some may be invalid if on edge).
        /// </summary>
        public BattleHex[] GetAllNeighbors()
        {
            var neighbors = new BattleHex[6];
            for (var i = 0; i < 6; i++)
            {
                neighbors[i] = GetNeighbor((Direction)i);
            }
            return neighbors;
        }

        /// <summary>
        /// Calculate hexagonal distance between two hexes.
        /// Uses axial coordinate conversion for accurate hex distance.
        /// Based on VCMI's BattleHex::getDistance().
        /// </summary>
        public static int GetDistance(BattleHex a, BattleHex b)
        {
            if (!a.IsValid || !b.IsValid)
                return int.MaxValue;

            // Convert to axial coordinates (offset for odd rows)
            var y1 = a.Y;
            var y2 = b.Y;
            var x1 = a.X + y1 / 2;
            var x2 = b.X + y2 / 2;

            var xDst = x2 - x1;
            var yDst = y2 - y1;

            // Hexagonal distance formula
            if ((xDst >= 0 && yDst >= 0) || (xDst < 0 && yDst < 0))
                return Mathf.Max(Mathf.Abs(xDst), Mathf.Abs(yDst));

            return Mathf.Abs(xDst) + Mathf.Abs(yDst);
        }

        /// <summary>
        /// Check if two hexes are adjacent (distance = 1).
        /// </summary>
        public bool IsAdjacentTo(BattleHex other)
        {
            return GetDistance(this, other) == 1;
        }

        /// <summary>
        /// All valid movement directions.
        /// </summary>
        public static readonly Direction[] AllDirections = new[]
        {
            Direction.TopLeft, Direction.TopRight, Direction.Right,
            Direction.BottomRight, Direction.BottomLeft, Direction.Left
        };

        // IEquatable implementation
        public bool Equals(BattleHex other) => hex == other.hex;
        public override bool Equals(object obj) => obj is BattleHex h && Equals(h);
        public override int GetHashCode() => hex.GetHashCode();
        public override string ToString() => $"Hex({X}, {Y}) [{hex}]";

        // Operators
        public static bool operator ==(BattleHex a, BattleHex b) => a.hex == b.hex;
        public static bool operator !=(BattleHex a, BattleHex b) => a.hex != b.hex;

        public static implicit operator int(BattleHex h) => h.hex;
        public static implicit operator BattleHex(int value) => new BattleHex(value);
    }
}
