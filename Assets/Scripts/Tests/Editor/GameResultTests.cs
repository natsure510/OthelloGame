using System;
using NUnit.Framework;

namespace Othello.Tests
{
    public sealed class GameResultTests
    {
        [Test]
        public void MissingOrUnfinishedGameHasNoResult()
        {
            Assert.Throws<ArgumentNullException>(() => new GameResult(null));
            Assert.Throws<InvalidOperationException>(() => new GameResult(new OthelloGame()));
        }

        [TestCase(35, StoneColor.Black)]
        [TestCase(29, StoneColor.White)]
        [TestCase(32, StoneColor.Empty)]
        [TestCase(64, StoneColor.Black)]
        [TestCase(0, StoneColor.White)]
        public void FullBoardCountsOnlyActualStonesAndDeterminesWinner(int blackCount, StoneColor winner)
        {
            var cells = new StoneColor[8, 8];
            for (int index = 0; index < 64; index++)
            {
                cells[index / 8, index % 8] = index < blackCount ? StoneColor.Black : StoneColor.White;
            }

            var game = new OthelloGame(new BoardState(cells));
            var result = new GameResult(game);
            Assert.That(result.BlackCount, Is.EqualTo(blackCount));
            Assert.That(result.WhiteCount, Is.EqualTo(64 - blackCount));
            Assert.That(result.Winner, Is.EqualTo(winner));
        }

        [TestCase(3, 1, StoneColor.Black)]
        [TestCase(1, 3, StoneColor.White)]
        [TestCase(1, 1, StoneColor.Empty)]
        [TestCase(0, 0, StoneColor.Empty)]
        public void NeitherSideCanMoveWithEmptyCellsUsesActualCounts(int blackCount, int whiteCount,
            StoneColor winner)
        {
            var cells = new StoneColor[8, 8];
            for (int column = 0; column < blackCount; column++) cells[0, column] = StoneColor.Black;
            for (int column = 0; column < whiteCount; column++) cells[7, column] = StoneColor.White;
            var game = new OthelloGame(new BoardState(cells));
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.Board.CountStones(StoneColor.Empty), Is.GreaterThan(0));

            var result = new GameResult(game);
            Assert.That(result.BlackCount, Is.EqualTo(blackCount));
            Assert.That(result.WhiteCount, Is.EqualTo(whiteCount));
            Assert.That(result.Winner, Is.EqualTo(winner));
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void LastPlacementAndFlipsAreIncludedInFinalCounts(StoneColor playingColor)
        {
            var cells = new StoneColor[8, 8];
            cells[0, 1] = playingColor == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            cells[0, 2] = playingColor;
            var game = new OthelloGame(new BoardState(cells), playingColor);
            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), playingColor), Is.True);
            Assert.That(game.IsGameOver, Is.True);

            var result = new GameResult(game);
            Assert.That(result.BlackCount, Is.EqualTo(playingColor == StoneColor.Black ? 3 : 0));
            Assert.That(result.WhiteCount, Is.EqualTo(playingColor == StoneColor.White ? 3 : 0));
            Assert.That(result.Winner, Is.EqualTo(playingColor));
        }
    }
}
