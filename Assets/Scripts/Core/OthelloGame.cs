using System;

namespace Othello
{
    /// <summary>Applies placements and manages turns, forced passes and the end of a game.</summary>
    public sealed class OthelloGame
    {
        public BoardState Board { get; }

        /// <summary>The side allowed to play, or Empty when the game has ended.</summary>
        public StoneColor CurrentTurn { get; private set; }

        public bool IsGameOver => CurrentTurn == StoneColor.Empty;

        /// <summary>The side skipped by the latest turn transition, or Empty if no side was skipped.</summary>
        public StoneColor LastPassedColor { get; private set; }

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

            ResolveTurn(firstTurn);
        }

        /// <summary>Rejects an illegal or out-of-turn placement without changing the board or turn.</summary>
        public bool TryPlaceStone(BoardPosition position, StoneColor color)
        {
            if (IsGameOver || color != CurrentTurn)
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

            ResolveTurn(color == StoneColor.Black ? StoneColor.White : StoneColor.Black);
            return true;
        }

        private void ResolveTurn(StoneColor nextColor)
        {
            LastPassedColor = StoneColor.Empty;
            if (OthelloRules.GetLegalMoves(Board, nextColor).Count > 0)
            {
                CurrentTurn = nextColor;
                return;
            }

            StoneColor otherColor = nextColor == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            if (OthelloRules.GetLegalMoves(Board, otherColor).Count > 0)
            {
                CurrentTurn = otherColor;
                LastPassedColor = nextColor;
                return;
            }

            // Neither side can play, including positions with empty cells remaining.
            CurrentTurn = StoneColor.Empty;
        }
    }
}
