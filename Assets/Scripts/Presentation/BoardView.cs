using System;
using UnityEngine;

namespace Othello
{
    /// <summary>Creates the board display and synchronizes its cells with the board state.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [SerializeField, Range(0f, 0.2f)] private float cellGap = 0.04f;
        [SerializeField] private Color cellColor = new Color(0.06f, 0.4f, 0.2f, 1f);

        private readonly CellView[,] cells = new CellView[BoardState.Size, BoardState.Size];
        private Sprite cellSprite;
        private Sprite stoneSprite;
        private Texture2D stoneTexture;
        private bool initialized;

        private float ActualCellSize => Mathf.Max(0.1f, cellSize);

        public void Render(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!initialized)
            {
                CreateCells();
            }

            for (int row = 0; row < BoardState.Size; row++)
            {
                for (int column = 0; column < BoardState.Size; column++)
                {
                    cells[row, column].SetStone(board.GetStone(new BoardPosition(row, column)));
                }
            }
        }

        public bool TryGetPosition(Vector3 worldPosition, out BoardPosition position)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float size = ActualCellSize;
            float halfWidth = BoardState.Size * size * 0.5f;
            int row = Mathf.FloorToInt((halfWidth - local.y) / size);
            int column = Mathf.FloorToInt((local.x + halfWidth) / size);
            position = new BoardPosition(row, column);
            return BoardState.IsInside(position);
        }

        private void CreateCells()
        {
            CreateSprites();
            float size = ActualCellSize;
            float halfWidth = BoardState.Size * size * 0.5f;

            var background = new GameObject("Board Background");
            background.layer = gameObject.layer;
            background.transform.SetParent(transform, false);
            background.transform.localScale = Vector3.one * (BoardState.Size * size);
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = cellSprite;
            backgroundRenderer.color = new Color(0.015f, 0.09f, 0.035f, 1f);
            backgroundRenderer.sortingOrder = -1;

            for (int row = 0; row < BoardState.Size; row++)
            {
                for (int column = 0; column < BoardState.Size; column++)
                {
                    var cellObject = new GameObject($"Cell ({row}, {column})");
                    cellObject.layer = gameObject.layer;
                    cellObject.transform.SetParent(transform, false);
                    cellObject.transform.localPosition = new Vector3(
                        -halfWidth + (column + 0.5f) * size,
                        halfWidth - (row + 0.5f) * size, 0f);

                    var cell = cellObject.AddComponent<CellView>();
                    cell.Initialize(new BoardPosition(row, column), cellSprite, stoneSprite,
                        size, Mathf.Clamp(cellGap, 0f, 0.2f), cellColor);
                    cells[row, column] = cell;
                }
            }

            initialized = true;
        }

        private void CreateSprites()
        {
            // Share two sprites across all cells; no separate image assets are needed.
            Texture2D white = Texture2D.whiteTexture;
            cellSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height),
                new Vector2(0.5f, 0.5f), white.width, 0, SpriteMeshType.FullRect);
            cellSprite.name = "Othello Cell";

            const int resolution = 64;
            stoneTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            stoneTexture.name = "Othello Stone Texture";
            stoneTexture.filterMode = FilterMode.Bilinear;
            stoneTexture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[resolution * resolution];
            float centre = resolution * 0.5f;
            float radius = centre - 1f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = x + 0.5f - centre;
                    float dy = y + 0.5f - centre;
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            stoneTexture.SetPixels32(pixels);
            stoneTexture.Apply(false, true);
            stoneSprite = Sprite.Create(stoneTexture, new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution, 0, SpriteMeshType.FullRect);
            stoneSprite.name = "Othello Stone";
        }

        private void OnDestroy()
        {
            Release(cellSprite);
            Release(stoneSprite);
            Release(stoneTexture);
        }

        private static void Release(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(asset);
            }
            else
            {
                DestroyImmediate(asset);
            }
        }
    }
}
