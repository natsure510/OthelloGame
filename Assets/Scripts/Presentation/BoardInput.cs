using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Othello
{
    /// <summary>Converts left mouse clicks to board coordinates and notifies listeners.</summary>
    public sealed class BoardInput : MonoBehaviour
    {
        [SerializeField] private Camera boardCamera;
        [SerializeField] private BoardView boardView;

        public event Action<BoardPosition> CellClicked;

        public bool InputEnabled { get; private set; }

        private void Awake()
        {
            if (boardCamera == null || boardView == null)
            {
                Debug.LogError("BoardInput requires a camera and board view.", this);
                enabled = false;
            }
        }

        public void SetInputEnabled(bool inputEnabled)
        {
            InputEnabled = inputEnabled;
        }

        public bool TryGetBoardPosition(Vector2 screenPosition, out BoardPosition position)
        {
            position = default;
            if (boardCamera == null || boardView == null || !boardCamera.pixelRect.Contains(screenPosition))
            {
                return false;
            }

            Ray ray = boardCamera.ScreenPointToRay(screenPosition);
            var boardPlane = new Plane(boardView.transform.forward, boardView.transform.position);
            if (!boardPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            return boardView.TryGetPosition(ray.GetPoint(distance), out position);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (!InputEnabled || mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (TryGetBoardPosition(mouse.position.ReadValue(), out BoardPosition position))
            {
                CellClicked?.Invoke(position);
            }
        }
    }
}
