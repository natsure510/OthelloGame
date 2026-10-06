using NUnit.Framework;

namespace Othello.Tests
{
    public sealed class OthelloPassTests
    {
        [Test]
        public void StandardOpening_IsInProgressWithoutAPass()
        {
            var game = new OthelloGame();

            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void StartingSideWithoutMoves_IsPassedWithoutChangingTheBoard(StoneColor playingColor)
        {
            var board = CreateTwoMoveBoard(playingColor);
            var before = Snapshot(board);
            StoneColor passedColor = OtherColor(playingColor);
            Assert.That(OthelloRules.GetLegalMoves(board, passedColor), Is.Empty);
            Assert.That(OthelloRules.GetLegalMoves(board, playingColor).Count, Is.EqualTo(2));

            var game = new OthelloGame(board, passedColor);

            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.CurrentTurn, Is.EqualTo(playingColor));
            Assert.That(game.LastPassedColor, Is.EqualTo(passedColor));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void OpponentWithoutMoves_IsPassedAndTheSameSideCanPlayAgain(StoneColor playingColor)
        {
            var game = new OthelloGame(CreateTwoMoveBoard(playingColor), playingColor);
            StoneColor opponent = OtherColor(playingColor);

            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), playingColor), Is.True);

            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.CurrentTurn, Is.EqualTo(playingColor));
            Assert.That(game.LastPassedColor, Is.EqualTo(opponent));
            Assert.That(OthelloRules.GetLegalMoves(game.Board, opponent), Is.Empty);
            CollectionAssert.AreEquivalent(new[] { new BoardPosition(7, 0) },
                OthelloRules.GetLegalMoves(game.Board, playingColor));
            Assert.That(game.Board.GetStone(new BoardPosition(0, 0)), Is.EqualTo(playingColor));
            Assert.That(game.Board.GetStone(new BoardPosition(0, 1)), Is.EqualTo(playingColor));

            // Invalid attempts must not erase the last pass or advance the turn.
            var before = Snapshot(game.Board);
            Assert.That(game.TryPlaceStone(new BoardPosition(7, 0), opponent), Is.False);
            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), playingColor), Is.False);
            Assert.That(game.TryPlaceStone(new BoardPosition(-1, 0), playingColor), Is.False);
            Assert.That(game.CurrentTurn, Is.EqualTo(playingColor));
            Assert.That(game.LastPassedColor, Is.EqualTo(opponent));
            CollectionAssert.AreEqual(before, Snapshot(game.Board));

            Assert.That(game.TryPlaceStone(new BoardPosition(7, 0), playingColor), Is.True);
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.Board.CountStones(playingColor), Is.EqualTo(64));
            Assert.That(game.Board.CountStones(StoneColor.Empty), Is.Zero);
        }

        [Test]
        public void NormalTurnFollowingAPass_ClearsThePreviousPass()
        {
            var cells = FilledBoard(StoneColor.Black);
            cells[0, 0] = StoneColor.Empty;
            cells[0, 1] = StoneColor.White;
            cells[1, 0] = StoneColor.Empty;
            cells[1, 1] = StoneColor.White;
            cells[1, 2] = StoneColor.White;
            cells[7, 0] = StoneColor.Empty;
            cells[7, 1] = StoneColor.White;
            var game = new OthelloGame(new BoardState(cells));

            Assert.That(game.TryPlaceStone(new BoardPosition(7, 0), StoneColor.Black), Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.White));

            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), StoneColor.Black), Is.True);

            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
            Assert.That(OthelloRules.IsLegalMove(game.Board, new BoardPosition(1, 0), StoneColor.White), Is.True);
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void NoMovesForEitherSide_EndsAtConstructionEvenWithEmptyCells(StoneColor firstTurn)
        {
            var cells = new StoneColor[8, 8];
            cells[0, 0] = StoneColor.Black;
            cells[7, 7] = StoneColor.White;
            var game = new OthelloGame(new BoardState(cells), firstTurn);

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.Board.CountStones(StoneColor.Empty), Is.EqualTo(62));
            CollectionAssert.AreEqual(cells, Snapshot(game.Board));
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void FullBoard_EndsWithoutChangingAnyStones(StoneColor firstTurn)
        {
            var cells = FilledBoard(StoneColor.Black);
            cells[7, 7] = StoneColor.White;
            var game = new OthelloGame(new BoardState(cells), firstTurn);

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
            CollectionAssert.AreEqual(cells, Snapshot(game.Board));
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void PlacementCanEndTheGameWithBothColorsAndEmptyCellsRemaining(StoneColor playingColor)
        {
            StoneColor opponent = OtherColor(playingColor);
            var cells = new StoneColor[8, 8];
            cells[0, 1] = opponent;
            cells[0, 2] = playingColor;
            cells[7, 7] = opponent;
            var game = new OthelloGame(new BoardState(cells), playingColor);
            Assert.That(game.IsGameOver, Is.False);

            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), playingColor), Is.True);

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.Board.CountStones(playingColor), Is.EqualTo(3));
            Assert.That(game.Board.CountStones(opponent), Is.EqualTo(1));
            Assert.That(game.Board.CountStones(StoneColor.Empty), Is.EqualTo(60));
            Assert.That(OthelloRules.GetLegalMoves(game.Board, playingColor), Is.Empty);
            Assert.That(OthelloRules.GetLegalMoves(game.Board, opponent), Is.Empty);
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        [TestCase(StoneColor.Empty)]
        [TestCase((StoneColor)99)]
        public void EndedGame_RejectsAllFurtherPlacements(StoneColor color)
        {
            var cells = new StoneColor[8, 8];
            cells[0, 0] = StoneColor.Black;
            cells[7, 7] = StoneColor.White;
            var game = new OthelloGame(new BoardState(cells));

            Assert.That(game.TryPlaceStone(new BoardPosition(3, 3), color), Is.False);

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
            CollectionAssert.AreEqual(cells, Snapshot(game.Board));
        }

        [Test]
        public void EmptyBoard_HasNoMovesAndIsAlreadyOver()
        {
            var game = new OthelloGame(new BoardState(new StoneColor[8, 8]));

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.Board.CountStones(StoneColor.Empty), Is.EqualTo(64));
        }

        [Test]
        public void StandardGameCanAdvanceToTheEndWithoutGettingStuckOnAnUnplayableTurn()
        {
            var game = new OthelloGame();
            int moves = 0;
            while (!game.IsGameOver)
            {
                var legalMoves = OthelloRules.GetLegalMoves(game.Board, game.CurrentTurn);
                Assert.That(legalMoves, Is.Not.Empty, "An active turn must always have a legal move.");
                Assert.That(moves, Is.LessThan(60), "There are only 60 initially empty cells.");
                Assert.That(game.TryPlaceStone(legalMoves[0], game.CurrentTurn), Is.True);
                moves++;
                Assert.That(game.Board.CountStones(StoneColor.Empty), Is.EqualTo(60 - moves));
            }

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(OthelloRules.GetLegalMoves(game.Board, StoneColor.Black), Is.Empty);
            Assert.That(OthelloRules.GetLegalMoves(game.Board, StoneColor.White), Is.Empty);
        }

        private static BoardState CreateTwoMoveBoard(StoneColor playingColor)
        {
            var cells = FilledBoard(playingColor);
            cells[0, 0] = StoneColor.Empty;
            cells[0, 1] = OtherColor(playingColor);
            cells[7, 0] = StoneColor.Empty;
            cells[7, 1] = OtherColor(playingColor);
            return new BoardState(cells);
        }

        private static StoneColor[,] FilledBoard(StoneColor color)
        {
            var cells = new StoneColor[8, 8];
            for (int row = 0; row < 8; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    cells[row, column] = color;
                }
            }

            return cells;
        }

        private static StoneColor OtherColor(StoneColor color)
        {
            return color == StoneColor.Black ? StoneColor.White : StoneColor.Black;
        }

        private static StoneColor[,] Snapshot(BoardState board)
        {
            var cells = new StoneColor[8, 8];
            for (int row = 0; row < 8; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    cells[row, column] = board.GetStone(new BoardPosition(row, column));
                }
            }

            return cells;
        }
    }
}
