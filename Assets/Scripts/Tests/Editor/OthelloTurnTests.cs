using System;
using NUnit.Framework;

namespace Othello.Tests
{
    public class OthelloTurnTests
    {
        [Test]
        public void NewGame_StartsWithBlack()
        {
            Assert.That(new OthelloGame().CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(new OthelloGame(new BoardState()).CurrentTurn, Is.EqualTo(StoneColor.Black));
        }

        [Test]
        public void CustomFirstTurn_CanStartWithWhiteAndPreservesTheBoard()
        {
            var board = new BoardState();
            var before = Snapshot(board);

            var game = new OthelloGame(board, StoneColor.White);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            Assert.That(game.Board, Is.SameAs(board));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [TestCase(StoneColor.Empty)]
        [TestCase((StoneColor)99)]
        public void CustomFirstTurn_RejectsInvalidColors(StoneColor firstTurn)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new OthelloGame(new BoardState(), firstTurn));
        }

        [Test]
        public void Constructors_RejectANullBoard()
        {
            Assert.Throws<ArgumentNullException>(() => new OthelloGame(null));
            Assert.Throws<ArgumentNullException>(() => new OthelloGame(null, StoneColor.White));
        }

        [TestCase(0, 0)]
        [TestCase(3, 3)]
        [TestCase(3, 4)]
        [TestCase(-1, 3)]
        [TestCase(8, 3)]
        [TestCase(3, -1)]
        [TestCase(3, 8)]
        public void IllegalBlackMove_KeepsTheBoardAndBlackTurn(int row, int column)
        {
            var game = new OthelloGame();
            var before = Snapshot(game.Board);

            Assert.That(game.TryPlaceStone(new BoardPosition(row, column), StoneColor.Black), Is.False);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [TestCase(StoneColor.Empty)]
        [TestCase((StoneColor)99)]
        public void InvalidPlayerColor_KeepsTheBoardAndTurn(StoneColor color)
        {
            var game = new OthelloGame();
            var before = Snapshot(game.Board);

            Assert.That(game.TryPlaceStone(new BoardPosition(2, 3), color), Is.False);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [Test]
        public void IllegalWhiteMove_KeepsTheBoardAndWhiteTurn()
        {
            var game = new OthelloGame(new BoardState(), StoneColor.White);
            var before = Snapshot(game.Board);

            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), StoneColor.White), Is.False);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [Test]
        public void WhiteMoveOnBlackTurn_IsRejectedEvenWhenLegalForWhite()
        {
            var game = new OthelloGame();
            var position = new BoardPosition(2, 4);
            var before = Snapshot(game.Board);
            Assert.That(OthelloRules.IsLegalMove(game.Board, position, StoneColor.White), Is.True);

            Assert.That(game.TryPlaceStone(position, StoneColor.White), Is.False);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [Test]
        public void LegalBlackMove_UpdatesTheBoardAndTransfersTheTurnToWhite()
        {
            var game = new OthelloGame();
            var expected = Snapshot(game.Board);
            expected[2, 3] = StoneColor.Black;
            expected[3, 3] = StoneColor.Black;

            Assert.That(game.TryPlaceStone(new BoardPosition(2, 3), StoneColor.Black), Is.True);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            CollectionAssert.AreEqual(expected, Snapshot(game.Board));
        }

        [Test]
        public void BlackCannotPlaceAgainWhileWhiteHasTheTurn()
        {
            var game = new OthelloGame();
            Assert.That(game.TryPlaceStone(new BoardPosition(2, 3), StoneColor.Black), Is.True);
            var before = Snapshot(game.Board);
            var blackMoves = OthelloRules.GetLegalMoves(game.Board, StoneColor.Black);
            Assert.That(blackMoves, Is.Not.Empty);

            Assert.That(game.TryPlaceStone(blackMoves[0], StoneColor.Black), Is.False);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [Test]
        public void LegalWhiteMove_UpdatesTheBoardAndTransfersTheTurnToBlack()
        {
            var game = new OthelloGame(new BoardState(), StoneColor.White);
            var expected = Snapshot(game.Board);
            expected[2, 4] = StoneColor.White;
            expected[3, 4] = StoneColor.White;

            Assert.That(game.TryPlaceStone(new BoardPosition(2, 4), StoneColor.White), Is.True);

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            CollectionAssert.AreEqual(expected, Snapshot(game.Board));
        }

        private static StoneColor[,] Snapshot(BoardState board)
        {
            var cells = new StoneColor[BoardState.Size, BoardState.Size];
            for (int row = 0; row < BoardState.Size; row++)
            {
                for (int column = 0; column < BoardState.Size; column++)
                {
                    cells[row, column] = board.GetStone(new BoardPosition(row, column));
                }
            }

            return cells;
        }
    }
}
