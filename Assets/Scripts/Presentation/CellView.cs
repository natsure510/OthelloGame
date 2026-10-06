using System;
using UnityEngine;

namespace Othello
{
    /// <summary>Displays the background and stone for one board cell.</summary>
    public sealed class CellView : MonoBehaviour
    {
        private SpriteRenderer cellRenderer;
        private SpriteRenderer stoneRenderer;

        public BoardPosition Position { get; private set; }

        public void Initialize(
            BoardPosition position, Sprite cellSprite, Sprite stoneSprite,
            float cellSize, float cellGap, Color cellColor)
        {
            Position = position;
            cellRenderer = CreateRenderer("Background", cellSprite, 0);
            cellRenderer.color = cellColor;
            cellRenderer.transform.localScale = Vector3.one * (cellSize * (1f - cellGap));

            stoneRenderer = CreateRenderer("Stone", stoneSprite, 1);
            stoneRenderer.transform.localScale = Vector3.one * (cellSize * 0.78f);
            SetStone(StoneColor.Empty);
        }

        public void SetStone(StoneColor color)
        {
            if (color != StoneColor.Empty && color != StoneColor.Black && color != StoneColor.White)
            {
                throw new ArgumentOutOfRangeException(nameof(color));
            }

            stoneRenderer.enabled = color != StoneColor.Empty;
            stoneRenderer.color = color == StoneColor.Black ? Color.black : Color.white;
        }

        private SpriteRenderer CreateRenderer(string objectName, Sprite sprite, int sortingOrder)
        {
            var child = new GameObject(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
