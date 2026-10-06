using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Othello.Tests
{
    public sealed class CpuTurnRunnerTests
    {
        private BoardView boardView;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            boardView = new GameObject("CPU Test Board").AddComponent<BoardView>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(boardView.gameObject);
            yield return null;
        }

        [Test]
        public void ConstructorRejectsAMissingCpu()
        {
            Assert.Throws<ArgumentNullException>(() => new CpuTurnRunner(null, 0f));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void ConstructorRejectsInvalidDelays(float delay)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CpuTurnRunner(new RandomCpuPlayer(), delay));
        }

        [Test]
        public void RunRequiresAGameAndDisplayCallback()
        {
            var runner = CreateRunner();
            Assert.Throws<ArgumentNullException>(() => runner.Run(null, () => { }));
            Assert.Throws<ArgumentNullException>(() => runner.Run(new OthelloGame(), null));
        }

        [UnityTest]
        public IEnumerator WhiteMoveRefreshesTheDisplayAndHandsBackToBlack()
        {
            var game = new OthelloGame(new BoardState(), StoneColor.White);
            boardView.Render(game.Board);
            int updates = 0;

            yield return CreateRunner().Run(game, () =>
            {
                updates++;
                boardView.Render(game.Board);
            });

            Assert.That(updates, Is.EqualTo(1));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.Board.CountStones(StoneColor.White), Is.EqualTo(4));
            Assert.That(game.Board.CountStones(StoneColor.Black), Is.EqualTo(1));
            AssertDisplayMatches(game.Board);
        }

        [UnityTest]
        public IEnumerator BlackPassLetsCpuPlayAgainAndStopAtGameOver()
        {
            var cells = FilledBoard(StoneColor.White);
            cells[0, 0] = StoneColor.Empty;
            cells[0, 1] = StoneColor.Black;
            cells[7, 0] = StoneColor.Empty;
            cells[7, 1] = StoneColor.Black;
            var game = new OthelloGame(new BoardState(cells), StoneColor.White);
            boardView.Render(game.Board);
            int updates = 0;
            bool observedBlackPass = false;

            yield return CreateRunner().Run(game, () =>
            {
                updates++;
                observedBlackPass |= game.LastPassedColor == StoneColor.Black;
                boardView.Render(game.Board);
                AssertDisplayMatches(game.Board);
            });

            Assert.That(updates, Is.EqualTo(2));
            Assert.That(observedBlackPass, Is.True);
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(game.Board.CountStones(StoneColor.White), Is.EqualTo(64));
            Assert.That(game.Board.CountStones(StoneColor.Black), Is.Zero);
        }

        [UnityTest]
        public IEnumerator WhitePassLeavesTheBoardForThePlayerToContinue()
        {
            var cells = FilledBoard(StoneColor.Black);
            cells[0, 0] = StoneColor.Empty;
            cells[0, 1] = StoneColor.White;
            cells[7, 0] = StoneColor.Empty;
            cells[7, 1] = StoneColor.White;
            var game = new OthelloGame(new BoardState(cells));
            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), StoneColor.Black), Is.True);
            boardView.Render(game.Board);
            var before = Snapshot(game.Board);
            int updates = 0;

            yield return CreateRunner().Run(game, () => updates++);

            Assert.That(updates, Is.Zero);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.White));
            Assert.That(game.IsGameOver, Is.False);
            CollectionAssert.AreEqual(before, Snapshot(game.Board));
            AssertDisplayMatches(game.Board);
            Assert.That(game.TryPlaceStone(new BoardPosition(7, 0), StoneColor.Black), Is.True);
        }

        [UnityTest]
        public IEnumerator EndedGameDoesNotStartACpuMove()
        {
            var game = new OthelloGame(new BoardState(new StoneColor[8, 8]));
            boardView.Render(game.Board);
            int updates = 0;

            yield return CreateRunner().Run(game, () => updates++);

            Assert.That(updates, Is.Zero);
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.Board.CountStones(StoneColor.Empty), Is.EqualTo(64));
            AssertDisplayMatches(game.Board);
        }

        private static CpuTurnRunner CreateRunner()
        {
            return new CpuTurnRunner(new RandomCpuPlayer(new System.Random(42)), 0f);
        }

        private void AssertDisplayMatches(BoardState board)
        {
            var cells = boardView.GetComponentsInChildren<CellView>();
            Assert.That(cells.Length, Is.EqualTo(64));
            foreach (CellView cell in cells)
            {
                var stone = cell.transform.Find("Stone").GetComponent<SpriteRenderer>();
                StoneColor color = board.GetStone(cell.Position);
                Assert.That(stone.enabled, Is.EqualTo(color != StoneColor.Empty));
                if (color != StoneColor.Empty)
                {
                    Assert.That(stone.color, Is.EqualTo(color == StoneColor.Black ? Color.black : Color.white));
                }
            }
        }

        private static StoneColor[,] FilledBoard(StoneColor color)
        {
            var cells = new StoneColor[BoardState.Size, BoardState.Size];
            for (int row = 0; row < BoardState.Size; row++)
            {
                for (int column = 0; column < BoardState.Size; column++)
                {
                    cells[row, column] = color;
                }
            }

            return cells;
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
