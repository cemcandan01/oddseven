using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeirdLand.Core.Data;

namespace WeirdLand.Unity
{
    /// <summary>
    /// The duel screen's widgets, created once under the screen's RectTransform (1920x1080 reference layout):
    /// top strip, the two fighters with the intent bubble, centre messages, the hand, the energy / pile panel,
    /// the turn and pass buttons, an effects layer, and the pass picker and result overlays on top.
    /// </summary>
    public sealed class DuelLayout
    {
        public const float FighterScale = 1.2f;
        public static readonly Vector2 UserPos = new Vector2(-560, 40), EnemyPos = new Vector2(560, 40);
        public static readonly Vector2 HandSize = new Vector2(HandView.MaxWidth, 280);
        private static readonly Color White = Color.white;
        private static readonly Color Muted = new Color32(0xCF, 0xD8, 0xCF, 0xFF);
        private static readonly Color EnergyDisc = new Color32(0xF2, 0xC2, 0x30, 0xFF);

        public RectTransform Root, Hand, Fx, DeckPoint;
        public Vector2 BodyPos;
        public FighterView User, Enemy;
        public IntentBubble Intent;
        public TMP_Text Turn, Kind, Seed, EnergyLabel, Energy, Draw, Discard, NoCards, Banner, EnemyCard, PassLine;
        public CanvasGroup BannerGroup, EnemyCardGroup, PassGroup;
        public Button EndTurn, Pass, Back;
        public PassPicker Picker;
        public ResultOverlay Result;

        public FighterView Fighter(bool user) => user ? User : Enemy;

        public static DuelLayout Build(RectTransform root, VisualCatalog cat, UiText text, DuelScreen screen)
        {
            var ui = new DuelLayout { Root = UiFactory.Stretch(root) };
            root.gameObject.layer = UiFactory.UiLayer;
            var panel = cat.Button.image.sprite;

            ui.User = UnityEngine.Object.Instantiate(cat.Fighter, root, false);
            ui.Enemy = UnityEngine.Object.Instantiate(cat.Fighter, root, false);
            ui.BodyPos = ui.User.Body.anchoredPosition;
            ui.Intent = UnityEngine.Object.Instantiate(cat.IntentBubble, root, false);
            var bubble = (RectTransform)ui.Intent.transform;
            UiFactory.Center(bubble, EnemyPos + new Vector2(0, 360 * FighterScale * 0.5f + 85), bubble.sizeDelta);

            ui.Banner = ui.CenterLabel("Banner", 84, new Vector2(0, 200), new Vector2(800, 130));
            ui.Banner.fontStyle = FontStyles.Bold;
            ui.BannerGroup = UiFactory.Group(ui.Banner, 0);
            ui.EnemyCard = ui.CenterLabel("EnemyCard", 46, new Vector2(0, 110), new Vector2(1000, 80));
            ui.EnemyCardGroup = UiFactory.Group(ui.EnemyCard, 0);
            ui.PassLine = ui.CenterLabel("PassLine", 46, new Vector2(0, 160), new Vector2(1000, 150));
            ui.PassGroup = UiFactory.Group(ui.PassLine, 0);

            ui.Hand = UiFactory.Rect("Hand", root);
            UiFactory.Place(ui.Hand, new Vector2(0.5f, 0), new Vector2(0, 170), HandSize);
            ui.Hand.pivot = new Vector2(0.5f, 0.5f); // a card's anchoredPosition equals its local position
            ui.NoCards = UiFactory.Label("NoCards", ui.Hand, 32, White);
            UiFactory.Center(ui.NoCards.rectTransform, Vector2.zero, new Vector2(900, 160));

            var info = UiFactory.Panel("Info", root, UiFactory.Strip, panel).rectTransform;
            UiFactory.Place(info, Vector2.zero, new Vector2(30, 30), new Vector2(300, 290));
            ui.EnergyLabel = UiFactory.Label("EnergyLabel", info, 28, White);
            UiFactory.Place(ui.EnergyLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(260, 40));
            var disc = UiFactory.Panel("EnergyDisc", info, EnergyDisc);
            disc.sprite = cat.Fighter.Ball.GetComponent<Image>().sprite;
            disc.type = Image.Type.Simple;
            UiFactory.Place(disc.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(140, 140));
            ui.Energy = UiFactory.Label("Energy", disc.rectTransform, 50, UiFactory.Ink);
            ui.Energy.fontStyle = FontStyles.Bold;
            UiFactory.Stretch(ui.Energy.rectTransform);
            ui.Draw = UiFactory.Label("DrawPile", info, 28, Muted);
            UiFactory.Place(ui.Draw.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(260, 36));
            ui.Discard = UiFactory.Label("DiscardPile", info, 28, Muted);
            UiFactory.Place(ui.Discard.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(260, 36));
            ui.DeckPoint = ui.Draw.rectTransform;

            ui.EndTurn = UiFactory.Button(cat, root, "EndTurn", "", screen.ClickEndTurn);
            UiFactory.Place((RectTransform)ui.EndTurn.transform, new Vector2(1, 0), new Vector2(-40, 40), new Vector2(300, 84));
            ui.Pass = UiFactory.Button(cat, root, "Pass", "", screen.ClickPass);
            UiFactory.Place((RectTransform)ui.Pass.transform, new Vector2(1, 0), new Vector2(-40, 144), new Vector2(300, 104));

            var top = UiFactory.Panel("Top", root, UiFactory.Strip).rectTransform;
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(0, 84);
            ui.Turn = UiFactory.Label("Turn", top, 36, White, TextAlignmentOptions.Left);
            UiFactory.Place(ui.Turn.rectTransform, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(320, 60));
            ui.Kind = UiFactory.Label("Kind", top, 34, White);
            UiFactory.Place(ui.Kind.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 60));
            ui.Seed = UiFactory.Label("Seed", top, 28, Muted, TextAlignmentOptions.Right);
            UiFactory.Place(ui.Seed.rectTransform, new Vector2(1, 0.5f), new Vector2(-310, 0), new Vector2(300, 60));
            ui.Back = UiFactory.Button(cat, top, "Back", "", screen.ClickBack);
            UiFactory.Place((RectTransform)ui.Back.transform, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(260, 64));

            ui.Fx = UiFactory.Stretch(UiFactory.Rect("Fx", root));
            ui.Picker = PassPicker.Create(root, cat, text, screen.ClickCancelPass);
            ui.Result = ResultOverlay.Create(root, cat, text, screen.ClickAgain, screen.ClickNewSeed, screen.ClickBack);
            return ui;
        }

        /// <summary>Names, colours, facing and position of one fighter; clears bars, block, chips and ball.</summary>
        public void SetupFighter(bool user, string playerName, string hpLabel, Color color, int hp, int maxHp)
        {
            var f = Fighter(user);
            f.Setup(playerName, hpLabel, color, !user);
            var r = (RectTransform)f.transform;
            UiFactory.Center(r, user ? UserPos : EnemyPos, r.sizeDelta);
            r.localScale = Vector3.one * FighterScale;
            f.Body.anchoredPosition = BodyPos;
            f.SetHp(hp, maxHp);
            f.SetBlock(0);
            f.SetStatuses(null, null);
            f.SetBall(false);
        }

        private TMP_Text CenterLabel(string name, float size, Vector2 pos, Vector2 box)
        {
            var l = UiFactory.Label(name, Root, size, White);
            UiFactory.Center(l.rectTransform, pos, box);
            return l;
        }
    }
}
