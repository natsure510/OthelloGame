using System;

namespace Othello
{
    /// <summary>An immutable snapshot of the stone counts and winner of an ended game.</summary>
    public sealed class GameResult
    {
        public int BlackCount { get; }
        public int WhiteCount { get; }

        /// <summary>The winning color, or Empty for a draw.</summary>
        public StoneColor Winner { get; }

        public GameResult(OthelloGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (!game.IsGameOver)
            {
                throw new InvalidOperationException("A result is available only after the game has ended.");
            }

            BlackCount = game.Board.CountStones(StoneColor.Black);
            WhiteCount = game.Board.CountStones(StoneColor.White);
            Winner = BlackCount > WhiteCount ? StoneColor.Black
                : WhiteCount > BlackCount ? StoneColor.White : StoneColor.Empty;
        }
    }
}
