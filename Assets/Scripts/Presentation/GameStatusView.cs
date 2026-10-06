using System;
using UnityEngine;
using UnityEngine.UI;

namespace Othello
{
    /// <summary>Displays turn, stone counts, forced passes and the final result.</summary>
    public sealed class GameStatusView : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField, Min(0.1f)] private float passDisplaySeconds = 3f;

        private Canvas canvas;
        private Text turnLabel;
        private Text countLabel;
        private Text passLabel;
        private Text resultLabel;
        private Font generatedFont;
        private float passExpiresAt;

        public string TurnText => turnLabel == null ? string.Empty : turnLabel.text;
        public string CountText => countLabel == null ? string.Empty : countLabel.text;
        public string PassText => passLabel == null ? string.Empty : passLabel.text;
        public string ResultText => resultLabel == null ? string.Empty : resultLabel.text;

        public void Render(OthelloGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (canvas == null)
            {
                CreateDisplay();
            }

            turnLabel.text = game.IsGameOver ? "対局終了" :
                game.CurrentTurn == StoneColor.Black ? "あなたの手番（黒）" : "CPUの手番（白）";
            countLabel.text = $"黒（あなた）：{game.Board.CountStones(StoneColor.Black)}    "
                + $"白（CPU）：{game.Board.CountStones(StoneColor.White)}";

            if (game.IsGameOver)
            {
                var result = new GameResult(game);
                countLabel.text = $"最終石数　黒（あなた）：{result.BlackCount}    白（CPU）：{result.WhiteCount}";
                resultLabel.text = result.Winner == StoneColor.Black ? "あなたの勝ち！"
                    : result.Winner == StoneColor.White ? "CPUの勝ち！" : "引き分けです";
                passLabel.text = string.Empty;
            }
            else
            {
                resultLabel.text = string.Empty;
                if (game.LastPassedColor != StoneColor.Empty)
                {
                    passLabel.text = game.LastPassedColor == StoneColor.Black
                        ? "黒（あなた）は置ける場所がないためパスしました"
                        : "白（CPU）は置ける場所がないためパスしました";
                    // Keep the notice readable even if the next CPU move arrives immediately.
                    passExpiresAt = Time.unscaledTime + Mathf.Max(0.1f, passDisplaySeconds);
                }
            }

            // Share the third row so the result never overlaps a pass notification.
            passLabel.gameObject.SetActive(!game.IsGameOver);
            resultLabel.gameObject.SetActive(game.IsGameOver);
        }

        private void Update()
        {
            if (passLabel != null && Time.unscaledTime >= passExpiresAt)
            {
                passLabel.text = string.Empty;
            }
        }

        private void OnEnable()
        {
            if (canvas != null) canvas.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void CreateDisplay()
        {
            var canvasObject = new GameObject("Game Status Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            // Scale with height to keep the header within the space above the board.
            scaler.matchWidthOrHeight = 1f;

            Font displayFont = font;
            if (displayFont == null)
            {
                generatedFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Yu Gothic", "Meiryo", "Hiragino Sans", "Noto Sans CJK JP", "Arial" }, 24);
                displayFont = generatedFont;
            }

            turnLabel = CreateLabel("Turn", displayFont, 28, 10f, 36f, Color.white);
            countLabel = CreateLabel("Stone Counts", displayFont, 24, 46f, 32f, Color.white);
            passLabel = CreateLabel("Pass Notice", displayFont, 22, 78f, 30f,
                new Color(1f, 0.9f, 0.45f));
            resultLabel = CreateLabel("Final Result", displayFont, 24, 78f, 30f,
                new Color(1f, 0.9f, 0.45f));
        }

        private Text CreateLabel(string objectName, Font displayFont, int size,
            float top, float height, Color color)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(canvas.transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(12f, -top - height);
            rect.offsetMax = new Vector2(-12f, -top);
            var label = labelObject.GetComponent<Text>();
            label.font = displayFont;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private void OnDestroy()
        {
            if (generatedFont != null) Destroy(generatedFont);
        }
    }
}
