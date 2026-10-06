using System;

namespace Othello
{
    /// <summary>Applies legal placements and their captures, and tracks the current turn.</summary>
    public sealed class OthelloGame
    {
        public BoardState Board { get; }
        public StoneColor CurrentTurn { get; private set; }

        public OthelloGame() : this(new BoardState())
        {
        }

        public OthelloGame(BoardState board) : this(board, StoneColor.Black)
        {
        }

        public OthelloGame(BoardState board, StoneColor firstTurn)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (firstTurn != StoneColor.Black && firstTurn != StoneColor.White)
            {
                throw new ArgumentOutOfRangeException(nameof(firstTurn), "The first turn must be black or white.");
            }

            CurrentTurn = firstTurn;
        }

        /// <summary>Rejects an illegal or out-of-turn placement without changing the board or turn.</summary>
        public bool TryPlaceStone(BoardPosition position, StoneColor color)
        {
            if (color != CurrentTurn)
            {
                return false;
            }

            var flippedStones = OthelloRules.GetFlippedStones(Board, position, color);
            if (flippedStones.Count == 0)
            {
                return false;
            }

            Board.SetStone(position, color);
            foreach (BoardPosition flippedPosition in flippedStones)
            {
                Board.SetStone(flippedPosition, color);
            }

            CurrentTurn = color == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            return true;
        }
    }
}
