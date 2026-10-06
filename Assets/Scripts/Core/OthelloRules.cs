using System;
using System.Collections.Generic;

namespace Othello
{
    /// <summary>Queries legal moves and captured stones without changing the board.</summary>
    public static class OthelloRules
    {
        public static bool IsLegalMove(BoardState board, BoardPosition position, StoneColor color)
        {
            return GetFlippedStones(board, position, color).Count > 0;
        }

        public static IReadOnlyList<BoardPosition> GetLegalMoves(BoardState board, StoneColor color)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var legalMoves = new List<BoardPosition>();
            for (int row = 0; row < BoardState.Size; row++)
            {
                for (int column = 0; column < BoardState.Size; column++)
                {
                    var position = new BoardPosition(row, column);
                    if (IsLegalMove(board, position, color))
                    {
                        legalMoves.Add(position);
                    }
                }
            }

            return legalMoves;
        }

        /// <summary>Returns only opponent stones bracketed by this move and an existing friendly stone.</summary>
        public static IReadOnlyList<BoardPosition> GetFlippedStones(
            BoardState board, BoardPosition position, StoneColor color)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var flippedStones = new List<BoardPosition>();
            if ((color != StoneColor.Black && color != StoneColor.White)
                || !BoardState.IsInside(position)
                || board.GetStone(position) != StoneColor.Empty)
            {
                return flippedStones;
            }

            StoneColor opponent = color == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            for (int rowStep = -1; rowStep <= 1; rowStep++)
            {
                for (int columnStep = -1; columnStep <= 1; columnStep++)
                {
                    if (rowStep == 0 && columnStep == 0)
                    {
                        continue;
                    }

                    var scan = new BoardPosition(position.Row + rowStep, position.Column + columnStep);
                    int capturedCount = 0;
                    while (BoardState.IsInside(scan) && board.GetStone(scan) == opponent)
                    {
                        capturedCount++;
                        scan = new BoardPosition(scan.Row + rowStep, scan.Column + columnStep);
                    }

                    // A ray only captures when a friendly stone closes a nonempty opponent chain.
                    if (capturedCount == 0 || !BoardState.IsInside(scan) || board.GetStone(scan) != color)
                    {
                        continue;
                    }

                    for (int distance = 1; distance <= capturedCount; distance++)
                    {
                        flippedStones.Add(new BoardPosition(
                            position.Row + rowStep * distance,
                            position.Column + columnStep * distance));
                    }
                }
            }

            return flippedStones;
        }
    }
}
