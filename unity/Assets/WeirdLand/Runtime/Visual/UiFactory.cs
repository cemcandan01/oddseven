using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WeirdLand.Unity
{
    /// <summary>Small helpers the screens use to lay out runtime UI (panels, labels, catalog buttons).</summary>
    public static class UiFactory
    {
        public const int UiLayer = 5;
        public static readonly Color Ink = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
        public static readonly Color Shade = new Color32(0x00, 0x00, 0x00, 0x99);
        public static readonly Color Strip = new Color32(0x0E, 0x2E, 0x17, 0xE6);
        /// <summary>Opaque box colour for modal layers, so nothing underneath bleeds through.</summary>
        public static readonly Color Modal = new Color32(0x0E, 0x2E, 0x17, 0xFF);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors and pivots at <paramref name="anchor"/> (0..1 of the parent), then positions and sizes.</summary>
        public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = anchor;
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }

        /// <summary>Centre-anchored placement, position relative to the parent's centre.</summary>
        public static RectTransform Center(RectTransform r, Vector2 position, Vector2 size)
        {
            Place(r, new Vector2(0.5f, 0.5f), position, size);
            return r;
        }

        public static RectTransform Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            return r;
        }

        public static Image Panel(string name, Transform parent, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = false;
            t.raycastTarget = false;
            return t;
        }

        public static CanvasGroup Group(Component c, float alpha)
        {
            if (!c.TryGetComponent<CanvasGroup>(out var g)) g = c.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alpha;
            return g;
        }

        /// <summary>An instance of the catalog's Button prefab with a label and a click handler.</summary>
        public static Button Button(VisualCatalog cat, Transform parent, string name, string label, Action onClick)
        {
            var b = UnityEngine.Object.Instantiate(cat.Button, parent, false);
            b.name = name;
            SetLabel(b, label);
            if (onClick != null) b.onClick.AddListener(new UnityAction(onClick));
            return b;
        }

        /// <summary>The Button prefab has no typed label; its single TMP child carries the text.</summary>
        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null) t.text = text;
        }

        public static string GetLabel(Button b)
        {
            var t = b.GetComponentInChildren<TMP_Text>(true);
            return t != null ? t.text : "";
        }
    }
}
