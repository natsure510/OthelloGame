using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
                }

                if (root.TryGetComponent(out Camera foundCamera))
                {
                    boardCamera = foundCamera;
                }
            }

            Assert.That(controller, Is.Not.Null, "The sample scene must contain the game controller.");
            Assert.That(boardView, Is.Not.Null);
            Assert.That(boardInput, Is.Not.Null);
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
        public IEnumerator LegalMouseClickPlacesAndFlipsStonesThenEndsPlayerTurn()
        {
            yield return ClickCell(new BoardPosition(2, 3));

            Assert.That(controller.Board.GetStone(new BoardPosition(2, 3)), Is.EqualTo(StoneColor.Black));
            Assert.That(controller.Board.GetStone(new BoardPosition(3, 3)), Is.EqualTo(StoneColor.Black));
            Assert.That(controller.Board.CountStones(StoneColor.Black), Is.EqualTo(4));
            Assert.That(controller.Board.CountStones(StoneColor.White), Is.EqualTo(1));
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.White));
            Assert.That(boardInput.InputEnabled, Is.False);
            AssertDisplayMatchesBoard();

            // A legal white move must not be accepted as a second player click.
            yield return ClickCell(new BoardPosition(2, 2));

            Assert.That(controller.Board.GetStone(new BoardPosition(2, 2)), Is.EqualTo(StoneColor.Empty));
            Assert.That(controller.Board.CountStones(StoneColor.Black), Is.EqualTo(4));
            Assert.That(controller.Board.CountStones(StoneColor.White), Is.EqualTo(1));
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.White));
            AssertDisplayMatchesBoard();
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
            Assert.That(controller.Board.GetStone(new BoardPosition(2, 3)), Is.EqualTo(StoneColor.Black));
            Assert.That(controller.CurrentTurn, Is.EqualTo(StoneColor.White));
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
