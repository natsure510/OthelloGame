using UnityEngine;

namespace Othello
{
    /// <summary>Connects player input, game logic and board display.</summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardInput boardInput;

        private OthelloGame game;

        public BoardState Board => game.Board;
        public StoneColor CurrentTurn => game.CurrentTurn;

        private void Awake()
        {
            if (boardView == null || boardInput == null)
            {
                Debug.LogError("GameController requires a board view and board input.", this);
                enabled = false;
                return;
            }

            game = new OthelloGame();
            boardView.Render(game.Board);
        }

        private void OnEnable()
        {
            if (game == null || boardInput == null)
            {
                return;
            }

            boardInput.CellClicked += HandleCellClicked;
            boardInput.SetInputEnabled(game.CurrentTurn == StoneColor.Black);
        }

        private void OnDisable()
        {
            if (boardInput != null)
            {
                boardInput.CellClicked -= HandleCellClicked;
                boardInput.SetInputEnabled(false);
            }
        }

        public bool TryPlayPlayerMove(BoardPosition position)
        {
            if (!isActiveAndEnabled || game == null || game.CurrentTurn != StoneColor.Black)
            {
                return false;
            }

            if (!game.TryPlaceStone(position, StoneColor.Black))
            {
                return false;
            }

            boardView.Render(game.Board);
            boardInput.SetInputEnabled(game.CurrentTurn == StoneColor.Black);
            return true;
        }

        private void HandleCellClicked(BoardPosition position)
        {
            TryPlayPlayerMove(position);
        }
    }
}
