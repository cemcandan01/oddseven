using System;
using TMPro;
using UnityEngine;
using WeirdLand.Core.Data;

namespace WeirdLand.Unity
{
    /// <summary>End-of-duel layer: result line, optional timeout line, and again / new seed / back buttons.</summary>
    public class ResultOverlay : MonoBehaviour
    {
        private TMP_Text _title;
        private TMP_Text _detail;

        public bool IsShown => gameObject.activeSelf;
        public CanvasGroup Group { get; private set; }

        /// <summary>Everything the overlay says, lines joined with '\n'.</summary>
        public string Text => string.IsNullOrEmpty(_detail.text) ? _title.text : _title.text + "\n" + _detail.text;

        public static ResultOverlay Create(Transform parent, VisualCatalog cat, UiText text,
            Action again, Action newSeed, Action back)
        {
            var root = UiFactory.Stretch(UiFactory.Rect("ResultOverlay", parent));
            var overlay = root.gameObject.AddComponent<ResultOverlay>();
            overlay.Group = UiFactory.Group(root, 1);
            UiFactory.Stretch(UiFactory.Panel("Shade", root, UiFactory.Shade, null, true).rectTransform);
            var box = UiFactory.Center(UiFactory.Panel("Box", root, UiFactory.Modal, cat.Button.image.sprite, true).rectTransform,
                new Vector2(0, 40), new Vector2(860, 400));

            overlay._title = UiFactory.Label("Title", box, 72, Color.white);
            overlay._title.fontStyle = FontStyles.Bold;
            UiFactory.Place(overlay._title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(800, 110));
            overlay._detail = UiFactory.Label("Detail", box, 32, new Color32(0xFF, 0xE0, 0x8A, 0xFF));
            UiFactory.Place(overlay._detail.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(800, 90));

            var labels = new[] { UiKeys.ResultAgain, UiKeys.ResultNewSeed, UiKeys.ResultBack };
            var actions = new[] { again, newSeed, back };
            for (int i = 0; i < 3; i++)
            {
                var b = UiFactory.Button(cat, box, "Button_" + labels[i], text.Get(labels[i]), actions[i]);
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2((i - 1) * 275, 40), new Vector2(260, 72));
            }
            root.gameObject.SetActive(false);
            return overlay;
        }

        public void Show(string title, string detail)
        {
            _title.text = title ?? "";
            _detail.text = detail ?? "";
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
