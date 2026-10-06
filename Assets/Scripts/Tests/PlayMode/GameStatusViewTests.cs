using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Othello.Tests
{
    public sealed class GameStatusViewTests
    {
        private GameStatusView view;

        [SetUp]
        public void SetUp()
        {
            view = new GameObject("Status Test").AddComponent<GameStatusView>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(view.gameObject);
            yield return null;
        }

        [Test]
        public void OpeningAndBothPlayersMovesDisplayCurrentTurnAndFlippedCounts()
        {
            var game = new OthelloGame();
            view.Render(game);
            Assert.That(view.TurnText, Is.EqualTo("あなたの手番（黒）"));
            Assert.That(view.CountText, Is.EqualTo("黒（あなた）：2    白（CPU）：2"));
            Assert.That(view.PassText, Is.Empty);

            Assert.That(game.TryPlaceStone(new BoardPosition(2, 3), StoneColor.Black), Is.True);
            view.Render(game);
            Assert.That(view.TurnText, Is.EqualTo("CPUの手番（白）"));
            Assert.That(view.CountText, Is.EqualTo("黒（あなた）：4    白（CPU）：1"));

            Assert.That(game.TryPlaceStone(OthelloRules.GetLegalMoves(game.Board, StoneColor.White)[0],
                StoneColor.White), Is.True);
            view.Render(game);
            Assert.That(view.TurnText, Is.EqualTo("あなたの手番（黒）"));
            AssertCounts(game);

            foreach (Text label in view.GetComponentsInChildren<Text>())
            {
                Assert.That(label.raycastTarget, Is.False);
                foreach (char character in label.text)
                {
                    Assert.That(label.font.HasCharacter(character), Is.True,
                        $"The display font must contain '{character}'.");
                }
            }
        }

        [TestCase(StoneColor.Black)]
        [TestCase(StoneColor.White)]
        public void ForcedPassDisplaysSkippedSideAndContinuedTurn(StoneColor playingColor)
        {
            var game = CreatePassGame(playingColor);
            Assert.That(game.TryPlaceStone(new BoardPosition(7, 0), playingColor), Is.True);
            view.Render(game);
            string passedSide = playingColor == StoneColor.Black ? "白（CPU）" : "黒（あなた）";
            Assert.That(view.PassText, Is.EqualTo($"{passedSide}は置ける場所がないためパスしました"));
            Assert.That(view.TurnText, Is.EqualTo(playingColor == StoneColor.Black
                ? "あなたの手番（黒）" : "CPUの手番（白）"));
            AssertCounts(game);
        }

        [UnityTest]
        public IEnumerator PassNoticeSurvivesNextMoveThenExpiresWhileGameTimeIsPaused()
        {
            typeof(GameStatusView).GetField("passDisplaySeconds", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, 0.15f);
            var game = CreatePassGame(StoneColor.Black);
            Assert.That(game.TryPlaceStone(new BoardPosition(7, 0), StoneColor.Black), Is.True);
            view.Render(game);
            string notice = view.PassText;
            Assert.That(notice, Is.Not.Empty);

            Assert.That(game.TryPlaceStone(new BoardPosition(0, 0), StoneColor.Black), Is.True);
            Assert.That(game.LastPassedColor, Is.EqualTo(StoneColor.Empty));
            view.Render(game);
            Assert.That(view.PassText, Is.EqualTo(notice));
            Assert.That(view.TurnText, Is.EqualTo("CPUの手番（白）"));
            AssertCounts(game);

            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                yield return new WaitForSecondsRealtime(0.2f);
                yield return null;
                Assert.That(view.PassText, Is.Empty);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void EndedGameShowsFinalCountsAndClearsPreviousPass()
        {
            var passGame = CreatePassGame(StoneColor.Black);
            passGame.TryPlaceStone(new BoardPosition(7, 0), StoneColor.Black);
            view.Render(passGame);
            Assert.That(view.PassText, Is.Not.Empty);

            var cells = new StoneColor[8, 8];
            cells[0, 0] = StoneColor.Black;
            cells[7, 7] = StoneColor.White;
            var endedGame = new OthelloGame(new BoardState(cells));
            Assert.That(endedGame.IsGameOver, Is.True);
            view.Render(endedGame);
            Assert.That(view.TurnText, Is.EqualTo("対局終了"));
            Assert.That(view.PassText, Is.Empty);
            Assert.That(view.CountText, Is.EqualTo("黒（あなた）：1    白（CPU）：1"));
        }

        private void AssertCounts(OthelloGame game)
        {
            Assert.That(view.CountText, Is.EqualTo(
                $"黒（あなた）：{game.Board.CountStones(StoneColor.Black)}    "
                + $"白（CPU）：{game.Board.CountStones(StoneColor.White)}"));
        }

        private static OthelloGame CreatePassGame(StoneColor playingColor)
        {
            StoneColor opponent = playingColor == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            var cells = new StoneColor[8, 8];
            for (int row = 0; row < 8; row++)
                for (int column = 0; column < 8; column++)
                    cells[row, column] = playingColor;
            cells[0, 0] = StoneColor.Empty;
            cells[0, 1] = opponent;
            cells[1, 0] = StoneColor.Empty;
            cells[1, 1] = opponent;
            cells[1, 2] = opponent;
            cells[7, 0] = StoneColor.Empty;
            cells[7, 1] = opponent;
            return new OthelloGame(new BoardState(cells), playingColor);
        }
    }
}
