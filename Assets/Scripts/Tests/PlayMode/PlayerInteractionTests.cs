using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Othello.Tests
{
    public sealed class PlayerInteractionTests
    {
        private Scene scene;
        private Scene previousActiveScene;
        private Mouse mouse;
        private GameController controller;
        private BoardView boardView;
        private BoardInput boardInput;
        private GameStatusView statusView;
        private Camera boardCamera;
        private InputSettings.UpdateMode previousUpdateMode;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
        private bool previousRunInBackground;
        private bool observedMousePress;
        private readonly List<BoardPosition> clickedPositions = new List<BoardPosition>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousActiveScene = SceneManager.GetActiveScene();
            previousUpdateMode = InputSystem.settings.updateMode;
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousRunInBackground = Application.runInBackground;

            // Batch runs have no focused Game View. Route the virtual mouse to the
            // player loop so these tests exercise BoardInput.Update normally.
            Application.runInBackground = true;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            Assert.That(mouse.enabled, Is.True);
            clickedPositions.Clear();
            InputSystem.onAfterUpdate += ObserveMousePress;
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);

            scene = SceneManager.GetSceneByName("SampleScene");
            SceneManager.SetActiveScene(scene);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out GameController foundController))
                {
                    controller = foundController;
                    boardView = root.GetComponent<BoardView>();
                    boardInput = root.GetComponent<BoardInput>();
                    statusView = root.GetComponent<GameStatusView>();
                }

                if (root.TryGetComponent(out Camera foundCamera))
                {
                    boardCamera = foundCamera;
                }
            }

            Assert.That(controller, Is.Not.Null, "The sample scene must contain the game controller.");
            Assert.That(boardView, Is.Not.Null);
            Assert.That(boardInput, Is.Not.Null);
            Assert.That(statusView, Is.Not.Null);
            Assert.That(boardCamera, Is.Not.Null);
            boardInput.CellClicked += RecordCellClicked;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            InputSystem.onAfterUpdate -= ObserveMousePress;
            if (boardInput != null)
            {
                boardInput.CellClicked -= RecordCellClicked;
            }

            if (mouse != null && mouse.added)
            {
                InputSystem.RemoveDevice(mouse);
            }

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }

            InputSystem.settings.updateMode = previousUpdateMode;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
            Application.runInBackground = previousRunInBackground;
        }

        [UnityTest]
        public IEnumerator SampleSceneDisplaysInitialBoardAndAcceptsPlayerInput()
        {
            Assert.That(boardView.GetComponentsInChildren<CellView>().Length, Is.EqualTo(64));
            AssertInitialState();
            yield return null;
        }

        [UnityTest]
        public IEnumerator StatusHeaderIsVisibleAboveTheBoardWithoutBlockingClicks()
        {
            Canvas.ForceUpdateCanvases();
            var labels = statusView.GetComponentsInChildren<Text>();
            Assert.That(labels.Length, Is.EqualTo(3));
            float boardTop = boardCamera.WorldToScreenPoint(
                boardView.transform.TransformPoint(new Vector3(0f, 4f, 0f))).y;
            foreach (Text label in labels)
            {
                var corners = new Vector3[4];
                label.rectTransform.GetWorldCorners(corners);
                Assert.That(corners[0].y, Is.GreaterThan(boardTop),
                    "All status rows must stay above the board.");
                Assert.That(corners[1].y, Is.LessThanOrEqualTo(Screen.height));
                Assert.That(label.raycastTarget, Is.False);
                if (!string.IsNullOrEmpty(label.text))
                {
                    Assert.That(label.cachedTextGenerator.vertexCount, Is.GreaterThan(0),
                        "The status text must generate visible geometry.");
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator LegalMouseClickUpdatesBoardAndBlocksInputUntilCpuReturnsTheTurn()
        {
            StoneColor[,] boardAfterPlayer = null;
            IReadOnlyList<BoardPosition> whiteMoves = null;
            float previousTimeScale = Time.timeScale;
            // Pause the CPU's timed wait so the blocked-click check cannot race its move.
            Time.timeScale = 0f;
            try
            {
                yield return ClickCell(new BoardPosition(2, 3));
                Assert.That(controller.Board.CountStones(StoneColor.Black), Is.EqualTo(4));
                Assert.That(controller.Board.CountStones(StoneColor.White), Is.EqualTo(1));
                Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.White));
                Assert.That(controller.IsCpuThinking, Is.True);
                Assert.That(boardInput.InputEnabled, Is.False);
                AssertDisplayMatchesBoard();
                boardAfterPlayer = Snapshot(controller.Board);
                whiteMoves = OthelloRules.GetLegalMoves(controller.Board, StoneColor.White);

                var secondBlackMove = new BoardPosition(4, 5);
                Assert.That(OthelloRules.IsLegalMove(controller.Board, secondBlackMove, StoneColor.Black), Is.True);
                Assert.That(controller.TryPlayPlayerMove(secondBlackMove), Is.False);
                yield return ClickCell(secondBlackMove);
                CollectionAssert.AreEqual(boardAfterPlayer, Snapshot(controller.Board));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }

            yield return WaitForCpu();
            AssertCpuMoveMatchesLegalWhiteMove(boardAfterPlayer, whiteMoves);
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(boardInput.InputEnabled, Is.True);
            AssertDisplayMatchesBoard();
        }

        [UnityTest]
        public IEnumerator DisablingControllerCancelsThePendingCpuMoveAndEnablingResumesOnce()
        {
            Assert.That(controller.TryPlayPlayerMove(new BoardPosition(2, 3)), Is.True);
            Assert.That(controller.IsCpuThinking, Is.True);
            var beforeCpu = Snapshot(controller.Board);
            var whiteMoves = OthelloRules.GetLegalMoves(controller.Board, StoneColor.White);

            controller.enabled = false;
            yield return new WaitForSeconds(0.4f);

            Assert.That(controller.IsCpuThinking, Is.False);
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.White));
            Assert.That(boardInput.InputEnabled, Is.False);
            CollectionAssert.AreEqual(beforeCpu, Snapshot(controller.Board));

            controller.enabled = true;
            Assert.That(controller.IsCpuThinking, Is.True);
            yield return WaitForCpu();

            AssertCpuMoveMatchesLegalWhiteMove(beforeCpu, whiteMoves);
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(boardInput.InputEnabled, Is.True);
            AssertDisplayMatchesBoard();
        }

        [UnityTest]
        public IEnumerator PlayerAndCpuCanCompleteAGameAndRejectClicksAfterTheEnd()
        {
            int playerMoves = 0;
            while (!controller.IsGameOver)
            {
                Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.Black));
                Assert.That(boardInput.InputEnabled, Is.True);
                var legalMoves = OthelloRules.GetLegalMoves(controller.Board, StoneColor.Black);
                Assert.That(legalMoves, Is.Not.Empty);
                int emptyBefore = controller.Board.CountStones(StoneColor.Empty);

                yield return ClickCell(legalMoves[0]);
                yield return WaitForCpu();

                Assert.That(controller.Board.CountStones(StoneColor.Empty), Is.LessThan(emptyBefore));
                AssertDisplayMatchesBoard();
                playerMoves++;
                Assert.That(playerMoves, Is.LessThanOrEqualTo(60));
            }

            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.Empty));
            Assert.That(controller.IsCpuThinking, Is.False);
            Assert.That(boardInput.InputEnabled, Is.False);
            Assert.That(OthelloRules.GetLegalMoves(controller.Board, StoneColor.Black), Is.Empty);
            Assert.That(OthelloRules.GetLegalMoves(controller.Board, StoneColor.White), Is.Empty);
            var endedBoard = Snapshot(controller.Board);
            string finalResult = statusView.ResultText;
            string finalCounts = statusView.CountText;

            yield return ClickCell(new BoardPosition(0, 0));
            Assert.That(controller.TryPlayPlayerMove(new BoardPosition(0, 0)), Is.False);
            CollectionAssert.AreEqual(endedBoard, Snapshot(controller.Board));
            Assert.That(statusView.ResultText, Is.EqualTo(finalResult));
            Assert.That(statusView.CountText, Is.EqualTo(finalCounts));
            Canvas.ForceUpdateCanvases();
            Text resultLabel = statusView.transform.Find("Game Status Canvas/Final Result").GetComponent<Text>();
            Assert.That(resultLabel.gameObject.activeInHierarchy, Is.True);
            Assert.That(resultLabel.cachedTextGenerator.vertexCount, Is.GreaterThan(0));
            var corners = new Vector3[4];
            resultLabel.rectTransform.GetWorldCorners(corners);
            float boardTop = boardCamera.WorldToScreenPoint(
                boardView.transform.TransformPoint(new Vector3(0f, 4f, 0f))).y;
            Assert.That(corners[0].y, Is.GreaterThan(boardTop));
        }

        [UnityTest]
        public IEnumerator IllegalEmptyAndOccupiedClicksLeaveBoardAndTurnUnchanged()
        {
            yield return ClickCell(new BoardPosition(0, 0));
            AssertInitialState();

            yield return ClickCell(new BoardPosition(3, 3));
            AssertInitialState();

            yield return ClickAtWorldPosition(boardView.transform.TransformPoint(new Vector3(4.25f, 0f, 0f)));
            AssertInitialState();
        }

        [UnityTest]
        public IEnumerator ScreenCoordinatesMatchCellsAndRejectPositionOutsideBoard()
        {
            foreach (CellView cell in boardView.GetComponentsInChildren<CellView>())
            {
                Vector2 screenPosition = boardCamera.WorldToScreenPoint(cell.transform.position);
                Assert.That(boardInput.TryGetBoardPosition(screenPosition, out BoardPosition position), Is.True);
                Assert.That(position, Is.EqualTo(cell.Position));
            }

            Vector2 outside = boardCamera.WorldToScreenPoint(
                boardView.transform.TransformPoint(new Vector3(4.25f, 0f, 0f)));
            Assert.That(boardInput.TryGetBoardPosition(outside, out _), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovingRotatingAndScalingBoardKeepsMouseCoordinatesAligned()
        {
            boardView.transform.position = new Vector3(0.25f, -0.15f, 0.5f);
            boardView.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
            boardView.transform.localScale = new Vector3(0.8f, 0.9f, 1f);

            foreach (CellView cell in boardView.GetComponentsInChildren<CellView>())
            {
                Vector2 screenPosition = boardCamera.WorldToScreenPoint(cell.transform.position);
                Assert.That(boardInput.TryGetBoardPosition(screenPosition, out BoardPosition position), Is.True);
                Assert.That(position, Is.EqualTo(cell.Position));
            }

            yield return ClickCell(new BoardPosition(2, 3));
            yield return WaitForCpu();
            Assert.That(controller.Board.GetStone(new BoardPosition(2, 3)), Is.EqualTo(StoneColor.Black));
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(controller.Board.CountStones(StoneColor.Empty), Is.EqualTo(58));
            AssertDisplayMatchesBoard();
        }

        private IEnumerator WaitForCpu()
        {
            float deadline = Time.realtimeSinceStartup + 25f;
            while (controller.IsCpuThinking)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "CPU turns must complete.");
                yield return null;
            }

            Assert.That(controller.CurrentTurn, Is.Not.EqualTo(StoneColor.White));
        }

        private void AssertCpuMoveMatchesLegalWhiteMove(
            StoneColor[,] beforeCpu, IReadOnlyList<BoardPosition> legalMoves)
        {
            var placedPositions = new List<BoardPosition>();
            for (int row = 0; row < BoardState.Size; row++)
            {
                for (int column = 0; column < BoardState.Size; column++)
                {
                    var position = new BoardPosition(row, column);
                    if (beforeCpu[row, column] == StoneColor.Empty
                        && controller.Board.GetStone(position) == StoneColor.White)
                    {
                        placedPositions.Add(position);
                    }
                }
            }

            Assert.That(placedPositions.Count, Is.EqualTo(1), "The CPU must place exactly one stone.");
            CollectionAssert.Contains(legalMoves, placedPositions[0]);
            var expectedGame = new OthelloGame(new BoardState(beforeCpu), StoneColor.White);
            Assert.That(expectedGame.TryPlaceStone(placedPositions[0], StoneColor.White), Is.True);
            CollectionAssert.AreEqual(Snapshot(expectedGame.Board), Snapshot(controller.Board));
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

        private IEnumerator ClickCell(BoardPosition position)
        {
            CellView selected = null;
            foreach (CellView cell in boardView.GetComponentsInChildren<CellView>())
            {
                if (cell.Position == position)
                {
                    selected = cell;
                    break;
                }
            }

            Assert.That(selected, Is.Not.Null);
            yield return ClickAtWorldPosition(selected.transform.position);
        }

        private IEnumerator ClickAtWorldPosition(Vector3 worldPosition)
        {
            Vector2 screenPosition = boardCamera.WorldToScreenPoint(worldPosition);
            bool isOnBoard = boardInput.TryGetBoardPosition(screenPosition, out BoardPosition expectedPosition);
            bool shouldDispatchClick = boardInput.InputEnabled && isOnBoard;
            int previousClickCount = clickedPositions.Count;
            observedMousePress = false;
            InputSystem.QueueStateEvent(mouse,
                new MouseState { position = screenPosition }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPosition });
            yield return null;

            Assert.That(observedMousePress, Is.True,
                "The virtual mouse press must reach a dynamic input update.");
            Assert.That(clickedPositions.Count,
                Is.EqualTo(previousClickCount + (shouldDispatchClick ? 1 : 0)),
                "BoardInput.Update must dispatch exactly one enabled board click and ignore other clicks.");
            if (shouldDispatchClick)
            {
                Assert.That(clickedPositions[clickedPositions.Count - 1], Is.EqualTo(expectedPosition));
            }
        }

        private void ObserveMousePress()
        {
            if (InputState.currentUpdateType == InputUpdateType.Dynamic
                && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                observedMousePress = true;
            }
        }

        private void RecordCellClicked(BoardPosition position)
        {
            clickedPositions.Add(position);
        }

        private void AssertInitialState()
        {
            Assert.That(controller.Board.CountStones(StoneColor.Black), Is.EqualTo(2));
            Assert.That(controller.Board.CountStones(StoneColor.White), Is.EqualTo(2));
            Assert.That(controller.Board.GetStone(new BoardPosition(3, 3)), Is.EqualTo(StoneColor.White));
            Assert.That(controller.Board.GetStone(new BoardPosition(0, 0)), Is.EqualTo(StoneColor.Empty));
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(boardInput.InputEnabled, Is.True);
            AssertDisplayMatchesBoard();
        }

        private void AssertDisplayMatchesBoard()
        {
            Assert.That(statusView.TurnText, Is.EqualTo(controller.IsGameOver ? "対局終了" :
                controller.CurrentTurn == StoneColor.Black ? "あなたの手番（黒）" : "CPUの手番（白）"));
            Assert.That(statusView.CountText, Is.EqualTo(
                (controller.IsGameOver ? "最終石数　" : string.Empty)
                + $"黒（あなた）：{controller.Board.CountStones(StoneColor.Black)}    "
                + $"白（CPU）：{controller.Board.CountStones(StoneColor.White)}"));
            int blackCount = controller.Board.CountStones(StoneColor.Black);
            int whiteCount = controller.Board.CountStones(StoneColor.White);
            string expectedResult = !controller.IsGameOver ? string.Empty
                : blackCount > whiteCount ? "あなたの勝ち！"
                : whiteCount > blackCount ? "CPUの勝ち！" : "引き分けです";
            Assert.That(statusView.ResultText, Is.EqualTo(expectedResult));
            int visibleStones = 0;
            foreach (CellView cell in boardView.GetComponentsInChildren<CellView>())
            {
                Transform stoneTransform = cell.transform.Find("Stone");
                Assert.That(stoneTransform, Is.Not.Null);
                SpriteRenderer stone = stoneTransform.GetComponent<SpriteRenderer>();
                Assert.That(stone, Is.Not.Null);
                StoneColor color = controller.Board.GetStone(cell.Position);
                Assert.That(stone.enabled, Is.EqualTo(color != StoneColor.Empty));
                if (color != StoneColor.Empty)
                {
                    visibleStones++;
                    Assert.That(stone.color, Is.EqualTo(color == StoneColor.Black ? Color.black : Color.white));
                }
            }

            Assert.That(visibleStones, Is.EqualTo(
                controller.Board.CountStones(StoneColor.Black) + controller.Board.CountStones(StoneColor.White)));
        }
    }
}
