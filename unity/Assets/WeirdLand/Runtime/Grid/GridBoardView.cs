using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Unity
{
    /// <summary>
    /// The 12 x 8 pitch seen from above (cells squashed vertically for a slight tilt), placeholder circles for the
    /// players, the ball, reachable-cell highlights and pass lines. Holds no game state of its own beyond what it shows.
    /// </summary>
    public sealed class GridBoardView
    {
        public const float CellW = 120, CellH = 86, TokenSize = 70, BallSize = 26;
        private const float LineWidth = 6, DashLength = 22, DashGap = 14;

        private static readonly Color CellA = new Color32(0x2A, 0x7A, 0x3E, 0xFF);
        private static readonly Color CellB = new Color32(0x25, 0x6E, 0x38, 0xFF);
        private static readonly Color ShotZone = new Color32(0x3A, 0x8E, 0x4A, 0xFF);
        private static readonly Color HighlightInk = new Color32(0xF2, 0xE0, 0x6A, 0xFF);
        private static readonly Color LineInk = new Color32(0xFF, 0xFF, 0xFF, 0xE6);
        private static Sprite _circle;

        private sealed class Token
        {
            public RectTransform Rect;
            public Image Body;
            public CanvasGroup Group;
            public bool Stunned;
        }

        private readonly Dictionary<string, Token> _tokens = new Dictionary<string, Token>();
        private readonly Image[,] _cells = new Image[GridGeometry.Width, GridGeometry.Height];
        private readonly Color[,] _cellBase = new Color[GridGeometry.Width, GridGeometry.Height];
        private readonly bool[,] _lit = new bool[GridGeometry.Width, GridGeometry.Height];
        private readonly TweenSet _tw;
        private RectTransform _tokenLayer, _lineLayer, _ball;

        public RectTransform Root { get; private set; }
        public int HighlightCount { get; private set; }
        public int PassLineCount { get; private set; }
        public int DashedLineCount { get; private set; }
        public string BallHolder { get; private set; }

        public GridBoardView(TweenSet tw) { _tw = tw; }

        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n * n];
                float r = n * 0.5f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                        byte a = (byte)(Mathf.Clamp01(r - d) * 255);
                        px[y * n + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
                return _circle;
            }
        }

        public void Build(RectTransform parent, Action<Cell> onCell, Action<string> onPlayer, GridMatch match,
            VisualCatalog cat, UiText text)
        {
            Root = UiFactory.Center(UiFactory.Rect("Board", parent), new Vector2(0, -10),
                new Vector2(CellW * GridGeometry.Width, CellH * GridGeometry.Height));
            var cellLayer = UiFactory.Stretch(UiFactory.Rect("Cells", Root));
            for (int x = 0; x < GridGeometry.Width; x++)
                for (int y = 0; y < GridGeometry.Height; y++)
                {
                    var c = new Cell(x, y);
                    var img = UiFactory.Panel("Cell_" + x + "_" + y, cellLayer, Color.white, null, true);
                    UiFactory.Center(img.rectTransform, CellPosition(c), new Vector2(CellW - 2, CellH - 2));
                    bool zone = GridGeometry.InShotZone(c, true) || GridGeometry.InShotZone(c, false);
                    _cellBase[x, y] = zone ? ShotZone : ((x + y) % 2 == 0 ? CellA : CellB);
                    img.color = _cellBase[x, y];
                    var b = img.gameObject.AddComponent<Button>();
                    b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(() => onCell(c));
                    _cells[x, y] = img;
                }
            var mid = UiFactory.Panel("HalfLine", Root, LineInk);
            UiFactory.Center(mid.rectTransform, Vector2.zero, new Vector2(4, CellH * GridGeometry.Height));
            _lineLayer = UiFactory.Stretch(UiFactory.Rect("PassLines", Root));
            _tokenLayer = UiFactory.Stretch(UiFactory.Rect("Players", Root));

            foreach (var p in match.Players)
            {
                var id = p.Def.Id;
                var body = UiFactory.Panel("Player_" + id, _tokenLayer,
                    p.Def.Slot == Slot.GK ? cat.Keeper : p.IsUser ? cat.UserTeam : cat.EnemyTeam, null, true);
                body.sprite = Circle;
                body.type = Image.Type.Simple;
                var rect = UiFactory.Center(body.rectTransform, CellPosition(p.Pos), new Vector2(TokenSize, TokenSize));
                var label = UiFactory.Label("Name", rect, 18, Color.white);
                UiFactory.Center(label.rectTransform, new Vector2(0, -TokenSize * 0.5f - 12), new Vector2(CellW, 26));
                label.text = text.Get(UiKeys.SlotName(p.Def.Slot));
                var btn = body.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => onPlayer(id));
                _tokens[id] = new Token { Rect = rect, Body = body, Group = UiFactory.Group(body, 1) };
            }

            var ball = UiFactory.Panel("Ball", _tokenLayer, Color.white);
            ball.sprite = Circle;
            _ball = UiFactory.Center(ball.rectTransform, Vector2.zero, new Vector2(BallSize, BallSize));
        }

        public Vector2 CellPosition(Cell c)
            => new Vector2((c.X - (GridGeometry.Width - 1) * 0.5f) * CellW, (c.Y - (GridGeometry.Height - 1) * 0.5f) * CellH);

        public Vector2 TokenPosition(string id) => _tokens[id].Rect.anchoredPosition;
        public bool IsShownStunned(string id) => _tokens[id].Stunned;

        private static Vector2 BallOffset => new Vector2(TokenSize * 0.4f, -TokenSize * 0.3f);

        /// <summary>Snaps every token, the stun look and the ball to the match state.</summary>
        public void Sync(GridMatch match)
        {
            foreach (var p in match.Players)
            {
                _tokens[p.Def.Id].Rect.anchoredPosition = CellPosition(p.Pos);
                SetStunned(p.Def.Id, p.Stun > 0);
            }
            if (match.Carrier != null) PlaceBall(match.Carrier.Def.Id);
        }

        public void SetStunned(string id, bool stunned)
        {
            var t = _tokens[id];
            t.Stunned = stunned;
            t.Group.alpha = stunned ? 0.35f : 1;
        }

        public void PlaceBall(string holder)
        {
            BallHolder = holder;
            _ball.anchoredPosition = _tokens[holder].Rect.anchoredPosition + BallOffset;
            _ball.SetAsLastSibling();
        }

        /// <summary>Moves a token (and the ball when it holds it).</summary>
        public Tween MoveToken(string id, Cell to, float duration)
        {
            var target = CellPosition(to);
            var t = _tw.Move(_tokens[id].Rect, target, duration);
            if (id == BallHolder) _tw.Move(_ball, target + BallOffset, duration);
            return t;
        }

        public Tween FlyBall(string to, float duration)
        {
            BallHolder = to;
            _ball.SetAsLastSibling();
            return _tw.Move(_ball, _tokens[to].Rect.anchoredPosition + BallOffset, duration, 0, Ease.OutQuad);
        }

        public Tween Punch(string id, float duration) => _tw.Punch(_tokens[id].Rect, 0.3f, duration);

        public void Highlight(IReadOnlyList<Cell> cells)
        {
            ClearHighlights();
            foreach (var c in cells)
            {
                _lit[c.X, c.Y] = true;
                _cells[c.X, c.Y].color = Color.Lerp(_cellBase[c.X, c.Y], HighlightInk, 0.6f);
            }
            HighlightCount = cells.Count;
        }

        public bool IsLit(Cell c) => GridGeometry.InBounds(c) && _lit[c.X, c.Y];

        public void ClearHighlights()
        {
            for (int x = 0; x < GridGeometry.Width; x++)
                for (int y = 0; y < GridGeometry.Height; y++)
                {
                    _lit[x, y] = false;
                    _cells[x, y].color = _cellBase[x, y];
                }
            HighlightCount = 0;
        }

        /// <summary>A line from the carrier to every pass target: solid when clear, dashed when contested. No percentages.</summary>
        public void ShowPassLines(GridMatch match)
        {
            ClearLines();
            var from = CellPosition(match.Carrier.Pos);
            foreach (var t in match.PassTargets())
            {
                bool dashed = match.PassLineContested(t.Def.Id);
                DrawLine(from, CellPosition(t.Pos), dashed);
                PassLineCount++;
                if (dashed) DashedLineCount++;
            }
        }

        public void ClearLines()
        {
            for (int i = _lineLayer.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_lineLayer.GetChild(i).gameObject);
            PassLineCount = 0;
            DashedLineCount = 0;
        }

        private void DrawLine(Vector2 a, Vector2 b, bool dashed)
        {
            var line = UiFactory.Rect("Line" + (dashed ? "_Dashed" : "_Solid"), _lineLayer);
            var dir = b - a;
            float len = dir.magnitude;
            line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0, 0.5f);
            line.anchoredPosition = a;
            line.sizeDelta = new Vector2(len, LineWidth);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            if (!dashed)
            {
                line.gameObject.AddComponent<Image>().color = LineInk;
                line.GetComponent<Image>().raycastTarget = false;
                return;
            }
            for (float s = 0; s < len; s += DashLength + DashGap)
            {
                var dash = UiFactory.Panel("Dash", line, LineInk).rectTransform;
                dash.anchorMin = dash.anchorMax = dash.pivot = new Vector2(0, 0.5f);
                dash.anchoredPosition = new Vector2(s, 0);
                dash.sizeDelta = new Vector2(Mathf.Min(DashLength, len - s), LineWidth);
            }
        }
    }
}
