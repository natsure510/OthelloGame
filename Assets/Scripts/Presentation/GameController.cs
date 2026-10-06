using System.Collections;
using UnityEngine;

namespace Othello
{
    /// <summary>Connects player input, game logic and board display.</summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardInput boardInput;
        [SerializeField, Min(0f)] private float cpuMoveDelay = 0.3f;

        private OthelloGame game;
        private CpuTurnRunner cpuTurnRunner;
        private Coroutine cpuRoutine;

        public BoardState Board => game.Board;
        public StoneColor CurrentTurn => game.CurrentTurn;
        public bool IsGameOver => game.IsGameOver;
        public StoneColor LastPassedColor => game.LastPassedColor;
        public bool IsCpuThinking => cpuRoutine != null;

        private bool CanAcceptPlayerInput => game != null && !game.IsGameOver
            && game.CurrentTurn == StoneColor.Black && !IsCpuThinking;

        private void Awake()
        {
            if (boardView == null || boardInput == null)
            {
                Debug.LogError("GameController requires a board view and board input.", this);
                enabled = false;
                return;
            }

            game = new OthelloGame();
            cpuTurnRunner = new CpuTurnRunner(new RandomCpuPlayer(), Mathf.Max(0f, cpuMoveDelay));
            RefreshBoard();
        }

        private void OnEnable()
        {
            if (game == null || boardInput == null)
            {
                return;
            }

            boardInput.CellClicked += HandleCellClicked;
            ContinueGame();
        }

        private void OnDisable()
        {
            if (cpuRoutine != null)
            {
                StopCoroutine(cpuRoutine);
                cpuRoutine = null;
            }

            if (boardInput != null)
            {
                boardInput.CellClicked -= HandleCellClicked;
                boardInput.SetInputEnabled(false);
            }
        }

        public bool TryPlayPlayerMove(BoardPosition position)
        {
            if (!isActiveAndEnabled || !CanAcceptPlayerInput)
            {
                return false;
            }

            if (!game.TryPlaceStone(position, StoneColor.Black))
            {
                return false;
            }

            RefreshBoard();
            ContinueGame();
            return true;
        }

        private void ContinueGame()
        {
            boardInput.SetInputEnabled(CanAcceptPlayerInput);
            if (!game.IsGameOver && game.CurrentTurn == StoneColor.White && cpuRoutine == null)
            {
                cpuRoutine = StartCoroutine(PlayCpuTurns());
            }
        }

        private IEnumerator PlayCpuTurns()
        {
            yield return cpuTurnRunner.Run(game, RefreshBoard);
            cpuRoutine = null;
            boardInput.SetInputEnabled(CanAcceptPlayerInput);
        }

        private void RefreshBoard()
        {
            boardView.Render(game.Board);
        }

        private void HandleCellClicked(BoardPosition position)
        {
            TryPlayPlayerMove(position);
        }
    }
}
