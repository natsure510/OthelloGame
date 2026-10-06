using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Othello.Tests
{
    public sealed class RandomCpuPlayerTests
    {
        [Test]
        public void Constructor_RejectsANullRandomSource()
        {
            Assert.Throws<ArgumentNullException>(() => new RandomCpuPlayer(null));
        }

        [Test]
        public void NullMoveList_IsRejected()
        {
            var cpu = new RandomCpuPlayer();

            Assert.Throws<ArgumentNullException>(() => cpu.TryChooseMove(null, out _));
        }

        [Test]
        public void NoLegalMoves_ReturnsFalseWithoutRequestingARandomIndex()
        {
            var random = new FixedIndexRandom(0);
            var cpu = new RandomCpuPlayer(random);

            Assert.That(cpu.TryChooseMove(Array.Empty<BoardPosition>(), out BoardPosition position), Is.False);

            Assert.That(position, Is.EqualTo(default(BoardPosition)));
            Assert.That(random.Calls, Is.Zero);
        }

        [Test]
        public void SingleLegalMove_IsAlwaysSelected()
        {
            var cpu = new RandomCpuPlayer();
            var onlyMove = new BoardPosition(2, 3);

            Assert.That(cpu.TryChooseMove(new[] { onlyMove }, out BoardPosition position), Is.True);

            Assert.That(position, Is.EqualTo(onlyMove));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EveryLegalMoveIncludingTheLastOne_CanBeSelected(int index)
        {
            var legalMoves = OthelloRules.GetLegalMoves(new BoardState(), StoneColor.White);
            var random = new FixedIndexRandom(index);
            var cpu = new RandomCpuPlayer(random);

            Assert.That(cpu.TryChooseMove(legalMoves, out BoardPosition position), Is.True);

            Assert.That(position, Is.EqualTo(legalMoves[index]));
            Assert.That(random.RequestedUpperBound, Is.EqualTo(legalMoves.Count));
            Assert.That(random.Calls, Is.EqualTo(1));
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void RepeatedSelection_OnlyReturnsLegalMovesAndPreservesAllInputState(StoneColor color)
        {
            var game = new OthelloGame(new BoardState(), color);
            var legalMoves = OthelloRules.GetLegalMoves(game.Board, color);
            var originalMoves = new List<BoardPosition>(legalMoves);
            var originalBoard = Snapshot(game.Board);
            var cpu = new RandomCpuPlayer(new Random(12345));

            for (int attempt = 0; attempt < 32; attempt++)
            {
                Assert.That(cpu.TryChooseMove(legalMoves, out BoardPosition position), Is.True);
                CollectionAssert.Contains(originalMoves, position);
                Assert.That(OthelloRules.IsLegalMove(game.Board, position, color), Is.True);
            }

            CollectionAssert.AreEqual(originalMoves, legalMoves);
            CollectionAssert.AreEqual(originalBoard, Snapshot(game.Board));
            Assert.That(game.CurrentTurn, Is.EqualTo(color));
            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
        }

        [Test]
        public void WhiteSelectionAfterPlayerMove_CanBeAppliedSeparatelyByTheGame()
        {
            var game = new OthelloGame();
            Assert.That(game.TryPlaceStone(new BoardPosition(2, 3), StoneColor.Black), Is.True);
            var originalBoard = Snapshot(game.Board);
            var legalMoves = OthelloRules.GetLegalMoves(game.Board, StoneColor.White);
            var cpu = new RandomCpuPlayer(new Random(42));

            Assert.That(cpu.TryChooseMove(legalMoves, out BoardPosition position), Is.True);

            CollectionAssert.AreEqual(originalBoard, Snapshot(game.Board));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            Assert.That(game.TryPlaceStone(position, StoneColor.White), Is.True);
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

        private sealed class FixedIndexRandom : Random
        {
            private readonly int index;

            public int RequestedUpperBound { get; private set; }
            public int Calls { get; private set; }

            public FixedIndexRandom(int index)
            {
                this.index = index;
            }

            public override int Next(int maxValue)
            {
                RequestedUpperBound = maxValue;
                Calls++;
                return index;
            }
        }
    }
}
