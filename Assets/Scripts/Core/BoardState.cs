using System;

namespace Othello
{
    /// <summary>Stores the 8x8 board and provides read access to its stones.</summary>
    public sealed class BoardState
    {
        public const int Size = 8;

        private readonly StoneColor[,] cells;

        /// <summary>Creates the standard opening with two black and two white stones.</summary>
        public BoardState()
        {
            cells = new StoneColor[Size, Size];
            cells[3, 3] = StoneColor.White;
            cells[3, 4] = StoneColor.Black;
            cells[4, 3] = StoneColor.Black;
            cells[4, 4] = StoneColor.White;
        }

        /// <summary>Copies a supplied position so later array edits cannot change this board.</summary>
        public BoardState(StoneColor[,] initialCells)
        {
            if (initialCells == null)
            {
                throw new ArgumentNullException(nameof(initialCells));
            }

            if (initialCells.GetLength(0) != Size || initialCells.GetLength(1) != Size)
            {
                throw new ArgumentException("The board must be 8x8.", nameof(initialCells));
            }

            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    if (!IsValidColor(initialCells[row, column]))
                    {
                        throw new ArgumentException("The board contains an invalid stone color.", nameof(initialCells));
                    }
                }
            }

            cells = (StoneColor[,])initialCells.Clone();
        }

        public static bool IsInside(BoardPosition position)
        {
            return position.Row >= 0 && position.Row < Size
                && position.Column >= 0 && position.Column < Size;
        }

        public StoneColor GetStone(BoardPosition position)
        {
            ValidatePosition(position);
            return cells[position.Row, position.Column];
        }

        /// <summary>Counts stones of a color, or unoccupied cells when given Empty.</summary>
        public int CountStones(StoneColor color)
        {
            if (!IsValidColor(color))
            {
                throw new ArgumentOutOfRangeException(nameof(color));
            }

            int count = 0;
            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    if (cells[row, column] == color)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        internal void SetStone(BoardPosition position, StoneColor color)
        {
            ValidatePosition(position);
            if (!IsValidColor(color))
            {
                throw new ArgumentOutOfRangeException(nameof(color));
            }

            cells[position.Row, position.Column] = color;
        }

        private static bool IsValidColor(StoneColor color)
        {
            return color == StoneColor.Empty || color == StoneColor.Black || color == StoneColor.White;
        }

        private static void ValidatePosition(BoardPosition position)
        {
            if (!IsInside(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position), "The position must be inside the board.");
            }
        }
    }
}
