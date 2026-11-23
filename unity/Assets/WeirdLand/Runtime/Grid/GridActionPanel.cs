using System;
using UnityEngine;
using UnityEngine.UI;
using WeirdLand.Core.Data;

namespace WeirdLand.Unity
{
    /// <summary>
    /// Bottom bar: İlerle and Pas at live in the action panel; Saha sits beside it and hides or shows the panel
    /// (and its highlights) without spending a move.
    /// </summary>
    public sealed class GridActionPanel
    {
        public Button Advance { get; private set; }
        public Button Pass { get; private set; }
        public Button Field { get; private set; }
        public RectTransform Actions { get; private set; }
        public bool Visible => Actions.gameObject.activeSelf;

        public void Build(RectTransform parent, VisualCatalog cat, UiText text, Action advance, Action pass, Action field)
        {
            var bar = UiFactory.Place(UiFactory.Rect("ActionBar", parent), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(1200, 90));
            Actions = UiFactory.Place(UiFactory.Rect("Actions", bar), new Vector2(0.5f, 0.5f), new Vector2(-140, 0), new Vector2(560, 80));
            Advance = UiFactory.Button(cat, Actions, "Advance", text.Get(GridUiKeys.Advance), advance);
            UiFactory.Place((RectTransform)Advance.transform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(270, 76));
            Pass = UiFactory.Button(cat, Actions, "Pass", text.Get(GridUiKeys.Pass), pass);
            UiFactory.Place((RectTransform)Pass.transform, new Vector2(1, 0.5f), Vector2.zero, new Vector2(270, 76));
            Field = UiFactory.Button(cat, bar, "Field", text.Get(GridUiKeys.Field), field);
            UiFactory.Place((RectTransform)Field.transform, new Vector2(0.5f, 0.5f), new Vector2(300, 0), new Vector2(220, 76));
        }

        public void SetVisible(bool visible) => Actions.gameObject.SetActive(visible);

        public void SetInteractable(bool on)
        {
            Advance.interactable = on;
            Pass.interactable = on;
        }
    }
}
