using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Othello.Tests
{
    public class OthelloLogicTests
    {
        [Test]
        public void InitialBoard_HasTheStandardFourStones()
        {
            var board = new BoardState();

            Assert.That(board.GetStone(new BoardPosition(3, 3)), Is.EqualTo(StoneColor.White));
            Assert.That(board.GetStone(new BoardPosition(4, 4)), Is.EqualTo(StoneColor.White));
            Assert.That(board.GetStone(new BoardPosition(3, 4)), Is.EqualTo(StoneColor.Black));
            Assert.That(board.GetStone(new BoardPosition(4, 3)), Is.EqualTo(StoneColor.Black));
            Assert.That(board.CountStones(StoneColor.Black), Is.EqualTo(2));
            Assert.That(board.CountStones(StoneColor.White), Is.EqualTo(2));
            Assert.That(board.CountStones(StoneColor.Empty), Is.EqualTo(60));
        }

        [Test]
        public void InitialBoard_HasExactlyFourLegalMovesForEachColor()
        {
            var board = new BoardState();

            CollectionAssert.AreEquivalent(new[]
            {
                new BoardPosition(2, 3), new BoardPosition(3, 2),
                new BoardPosition(4, 5), new BoardPosition(5, 4)
            }, OthelloRules.GetLegalMoves(board, StoneColor.Black));
            CollectionAssert.AreEquivalent(new[]
            {
                new BoardPosition(2, 4), new BoardPosition(3, 5),
                new BoardPosition(4, 2), new BoardPosition(5, 3)
            }, OthelloRules.GetLegalMoves(board, StoneColor.White));
        }

        [Test]
        public void CustomBoard_CopiesTheSuppliedArray()
        {
            var cells = new StoneColor[BoardState.Size, BoardState.Size];
            cells[0, 0] = StoneColor.Black;
            var board = new BoardState(cells);

            cells[0, 0] = StoneColor.White;
            cells[7, 7] = StoneColor.Black;

            Assert.That(board.GetStone(new BoardPosition(0, 0)), Is.EqualTo(StoneColor.Black));
            Assert.That(board.GetStone(new BoardPosition(7, 7)), Is.EqualTo(StoneColor.Empty));
        }

        [Test]
        public void CustomBoard_RejectsNullWrongDimensionsAndInvalidStoneColors()
        {
            Assert.Throws<ArgumentNullException>(() => new BoardState(null));
            Assert.Throws<ArgumentException>(() => new BoardState(new StoneColor[7, 8]));
            Assert.Throws<ArgumentException>(() => new BoardState(new StoneColor[8, 9]));

            var cells = new StoneColor[8, 8];
            cells[0, 0] = (StoneColor)99;
            Assert.Throws<ArgumentException>(() => new BoardState(cells));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(8, 0)]
        [TestCase(0, 8)]
        public void BoardAccess_RejectsCoordinatesOutsideTheBoard(int row, int column)
        {
            var position = new BoardPosition(row, column);
            var board = new BoardState();

            Assert.That(BoardState.IsInside(position), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetStone(position));
            Assert.That(BoardState.IsInside(new BoardPosition(0, 0)), Is.True);
            Assert.That(BoardState.IsInside(new BoardPosition(7, 7)), Is.True);
        }

        [TestCase(-1, -1)]
        [TestCase(-1, 0)]
        [TestCase(-1, 1)]
        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(1, -1)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        public void LegalMove_FlipsACompleteChainInEachDirection(int rowStep, int columnStep)
        {
            var cells = new StoneColor[8, 8];
            var first = new BoardPosition(3 + rowStep, 3 + columnStep);
            var second = new BoardPosition(3 + 2 * rowStep, 3 + 2 * columnStep);
            cells[first.Row, first.Column] = StoneColor.White;
            cells[second.Row, second.Column] = StoneColor.White;
            cells[3 + 3 * rowStep, 3 + 3 * columnStep] = StoneColor.Black;
            var board = new BoardState(cells);
            var origin = new BoardPosition(3, 3);

            Assert.That(OthelloRules.IsLegalMove(board, origin, StoneColor.Black), Is.True);
            CollectionAssert.AreEquivalent(new[] { first, second },
                OthelloRules.GetFlippedStones(board, origin, StoneColor.Black));

            var game = new OthelloGame(board);
            Assert.That(game.TryPlaceStone(origin, StoneColor.Black), Is.True);
            Assert.That(game.Board.CountStones(StoneColor.Black), Is.EqualTo(4));
            Assert.That(game.Board.CountStones(StoneColor.White), Is.Zero);
        }

        [Test]
        public void Placement_FlipsAllEightRaysAndPreservesUnrelatedStones()
        {
            var cells = new StoneColor[8, 8];
            var expectedFlips = new List<BoardPosition>();
            for (int rowStep = -1; rowStep <= 1; rowStep++)
            {
                for (int columnStep = -1; columnStep <= 1; columnStep++)
                {
                    if (rowStep == 0 && columnStep == 0)
                    {
                        continue;
                    }

                    var position = new BoardPosition(3 + rowStep, 3 + columnStep);
                    expectedFlips.Add(position);
                    cells[position.Row, position.Column] = StoneColor.White;
                    cells[3 + 2 * rowStep, 3 + 2 * columnStep] = StoneColor.Black;
                }
            }

            cells[7, 7] = StoneColor.White;
            cells[7, 6] = StoneColor.Black;
            var game = new OthelloGame(new BoardState(cells));
            var origin = new BoardPosition(3, 3);

            CollectionAssert.AreEquivalent(expectedFlips,
                OthelloRules.GetFlippedStones(game.Board, origin, StoneColor.Black));
            Assert.That(game.TryPlaceStone(origin, StoneColor.Black), Is.True);

            cells[origin.Row, origin.Column] = StoneColor.Black;
            foreach (var position in expectedFlips)
            {
                cells[position.Row, position.Column] = StoneColor.Black;
            }

            CollectionAssert.AreEqual(cells, Snapshot(game.Board));
            Assert.That(game.Board.CountStones(StoneColor.Black), Is.EqualTo(18));
            Assert.That(game.Board.CountStones(StoneColor.White), Is.EqualTo(1));
        }

        [TestCase(0, 0, 1, 1)]
        [TestCase(0, 7, 1, -1)]
        [TestCase(7, 0, -1, 1)]
        [TestCase(7, 7, -1, -1)]
        public void CornerPlacement_CanCaptureAlongBothEdgesAndTheDiagonal(
            int row, int column, int rowStep, int columnStep)
        {
            var cells = new StoneColor[8, 8];
            var expectedFlips = new[]
            {
                new BoardPosition(row + rowStep, column),
                new BoardPosition(row, column + columnStep),
                new BoardPosition(row + rowStep, column + columnStep)
            };
            foreach (var position in expectedFlips)
            {
                cells[position.Row, position.Column] = StoneColor.Black;
            }

            cells[row + 2 * rowStep, column] = StoneColor.White;
            cells[row, column + 2 * columnStep] = StoneColor.White;
            cells[row + 2 * rowStep, column + 2 * columnStep] = StoneColor.White;
            var game = new OthelloGame(new BoardState(cells), StoneColor.White);
            var origin = new BoardPosition(row, column);

            CollectionAssert.AreEquivalent(expectedFlips,
                OthelloRules.GetFlippedStones(game.Board, origin, StoneColor.White));
            Assert.That(game.TryPlaceStone(origin, StoneColor.White), Is.True);
            Assert.That(game.Board.CountStones(StoneColor.White), Is.EqualTo(7));
            Assert.That(game.Board.CountStones(StoneColor.Black), Is.Zero);
        }

        [TestCase("W.B")]
        [TestCase("WW.B")]
        [TestCase("WWWW")]
        [TestCase("BWB")]
        public void UnbracketedRays_DoNotMakeALegalMove(string ray)
        {
            var cells = new StoneColor[8, 8];
            for (int index = 0; index < ray.Length; index++)
            {
                cells[3, 4 + index] = ray[index] == 'W' ? StoneColor.White
                    : ray[index] == 'B' ? StoneColor.Black : StoneColor.Empty;
            }

            var board = new BoardState(cells);
            var origin = new BoardPosition(3, 3);

            Assert.That(OthelloRules.IsLegalMove(board, origin, StoneColor.Black), Is.False);
            Assert.That(OthelloRules.GetFlippedStones(board, origin, StoneColor.Black), Is.Empty);
        }

        [Test]
        public void RayAtAnEdge_DoesNotWrapIntoTheNextRow()
        {
            var cells = new StoneColor[8, 8];
            cells[3, 7] = StoneColor.White;
            cells[4, 0] = StoneColor.Black;
            var board = new BoardState(cells);
            var origin = new BoardPosition(3, 6);

            Assert.That(OthelloRules.IsLegalMove(board, origin, StoneColor.Black), Is.False);
            Assert.That(OthelloRules.GetFlippedStones(board, origin, StoneColor.Black), Is.Empty);
        }

        [Test]
        public void OccupiedOrigin_IsRejectedEvenWhenItWouldBracketOpponents()
        {
            var cells = new StoneColor[8, 8];
            cells[3, 3] = StoneColor.White;
            cells[3, 4] = StoneColor.White;
            cells[3, 5] = StoneColor.Black;
            var game = new OthelloGame(new BoardState(cells));
            var origin = new BoardPosition(3, 3);

            Assert.That(OthelloRules.IsLegalMove(game.Board, origin, StoneColor.Black), Is.False);
            Assert.That(OthelloRules.GetFlippedStones(game.Board, origin, StoneColor.Black), Is.Empty);
            Assert.That(game.TryPlaceStone(origin, StoneColor.Black), Is.False);
            CollectionAssert.AreEqual(cells, Snapshot(game.Board));
        }

        [Test]
        public void LegalMoveQueries_DoNotModifyTheBoard()
        {
            var board = new BoardState();
            var before = Snapshot(board);

            OthelloRules.IsLegalMove(board, new BoardPosition(2, 3), StoneColor.Black);
            OthelloRules.GetFlippedStones(board, new BoardPosition(2, 3), StoneColor.Black);
            OthelloRules.GetLegalMoves(board, StoneColor.Black);
            OthelloRules.GetLegalMoves(board, StoneColor.White);

            CollectionAssert.AreEqual(before, Snapshot(board));
        }

        [TestCase(3, 3, StoneColor.Black)]
        [TestCase(3, 4, StoneColor.Black)]
        [TestCase(0, 0, StoneColor.Black)]
        [TestCase(-1, 3, StoneColor.Black)]
        [TestCase(8, 3, StoneColor.Black)]
        [TestCase(3, -1, StoneColor.Black)]
        [TestCase(3, 8, StoneColor.Black)]
        [TestCase(2, 3, StoneColor.Empty)]
        [TestCase(2, 3, (StoneColor)99)]
        public void InvalidMove_IsRejectedAndLeavesTheWholeBoardUnchanged(
            int row, int column, StoneColor color)
        {
            var game = new OthelloGame();
            var before = Snapshot(game.Board);
            var position = new BoardPosition(row, column);

            Assert.That(OthelloRules.IsLegalMove(game.Board, position, color), Is.False);
            Assert.That(OthelloRules.GetFlippedStones(game.Board, position, color), Is.Empty);
            Assert.That(game.TryPlaceStone(position, color), Is.False);

            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [TestCase(StoneColor.Empty)]
        [TestCase((StoneColor)99)]
        public void LegalMoves_ForAnInvalidPlayerColorAreEmpty(StoneColor color)
        {
            var board = new BoardState();
            var before = Snapshot(board);

            Assert.That(OthelloRules.GetLegalMoves(board, color), Is.Empty);
            CollectionAssert.AreEqual(before, Snapshot(board));
        }

        [Test]
        public void Rules_RejectANullBoard()
        {
            var position = new BoardPosition(2, 3);

            Assert.Throws<ArgumentNullException>(
                () => OthelloRules.IsLegalMove(null, position, StoneColor.Black));
            Assert.Throws<ArgumentNullException>(
                () => OthelloRules.GetFlippedStones(null, position, StoneColor.Black));
            Assert.Throws<ArgumentNullException>(
                () => OthelloRules.GetLegalMoves(null, StoneColor.Black));
        }

        [Test]
        public void OpeningPlacement_ChangesOnlyTheOriginAndCapturedStone()
        {
            var game = new OthelloGame();
            var expected = Snapshot(game.Board);
            expected[2, 3] = StoneColor.Black;
            expected[3, 3] = StoneColor.Black;

            Assert.That(game.TryPlaceStone(new BoardPosition(2, 3), StoneColor.Black), Is.True);

            CollectionAssert.AreEqual(expected, Snapshot(game.Board));
            Assert.That(game.Board.CountStones(StoneColor.Black), Is.EqualTo(4));
            Assert.That(game.Board.CountStones(StoneColor.White), Is.EqualTo(1));
            Assert.That(game.Board.CountStones(StoneColor.Empty), Is.EqualTo(59));
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
