using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeirdLand.Core.Data;

namespace WeirdLand.Unity
{
    /// <summary>Semi-transparent layer listing the pass targets; cancelling costs nothing.</summary>
    public class PassPicker : MonoBehaviour
    {
        private const float ButtonWidth = 420, ButtonHeight = 84, Gap = 12;

        private readonly List<Button> _targets = new List<Button>();
        private VisualCatalog _cat;
        private RectTransform _box;
        private TMP_Text _title;
        private Button _cancel;
        private Action<string> _onPick;

        public bool IsOpen => gameObject.activeSelf;
        public IReadOnlyList<Button> TargetButtons => _targets;

        public static PassPicker Create(Transform parent, VisualCatalog cat, UiText text, Action onCancel)
        {
            var root = UiFactory.Stretch(UiFactory.Rect("PassPicker", parent));
            var picker = root.gameObject.AddComponent<PassPicker>();
            picker._cat = cat;
            UiFactory.Stretch(UiFactory.Panel("Shade", root, UiFactory.Shade, null, true).rectTransform);
            var sprite = cat.Button.image.sprite;
            picker._box = UiFactory.Center(UiFactory.Panel("Box", root, UiFactory.Modal, sprite, true).rectTransform,
                Vector2.zero, new Vector2(ButtonWidth + 80, 400));
            picker._title = UiFactory.Label("Title", picker._box, 36, Color.white);
            UiFactory.Place(picker._title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(ButtonWidth + 40, 56));
            picker._title.text = text.Get(UiKeys.DuelPassPickTitle);
            picker._cancel = UiFactory.Button(cat, picker._box, "Cancel", text.Get(UiKeys.DuelCancel), onCancel);
            root.gameObject.SetActive(false);
            return picker;
        }

        /// <summary>One button per target: player name on the first line, slot name on the second.</summary>
        public void Open(IReadOnlyList<PlayerDef> targets, UiText text, Action<string> onPick)
        {
            _onPick = onPick;
            foreach (var b in _targets)
                if (b != null) Destroy(b.gameObject);
            _targets.Clear();

            float height = 24 + 56 + Gap + targets.Count * (ButtonHeight + Gap) + 72 + 24;
            _box.sizeDelta = new Vector2(ButtonWidth + 80, height);
            float y = -(24 + 56 + Gap);
            foreach (var p in targets)
            {
                var id = p.Id;
                var b = UiFactory.Button(_cat, _box, "Target_" + id, p.Name + "\n" + text.Get(UiKeys.SlotName(p.Slot)),
                    () => _onPick?.Invoke(id));
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(ButtonWidth, ButtonHeight));
                _targets.Add(b);
                y -= ButtonHeight + Gap;
            }
            UiFactory.Place((RectTransform)_cancel.transform, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(260, 72));
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            _onPick = null;
            gameObject.SetActive(false);
        }
    }
}
