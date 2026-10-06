using System;

namespace Othello
{
    /// <summary>Zero-based board coordinates, with row 0 at the top and column 0 on the left.</summary>
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public int Row { get; }
        public int Column { get; }

        public BoardPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public bool Equals(BoardPosition other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Column;
            }
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({Row}, {Column})";
        }
    }
}
