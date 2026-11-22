using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WeirdLand.Unity.EditorTools
{
    /// <summary>
    /// Generates the placeholder UI prefabs and the VisualCatalog asset from code (tools/unity.sh prefabs).
    /// Existing prefabs are opened and rebuilt in place, reusing child objects by name, so regenerating
    /// keeps their object ids stable. An existing catalog keeps its colours and timings; only prefab links are renewed.
    /// </summary>
    public static class PrefabBuilder
    {
        public const string PrefabDir = "Assets/WeirdLand/Generated/Prefabs";
        public const string CatalogPath = "Assets/WeirdLand/Resources/VisualCatalog.asset";

        private const int UiLayer = 5;
        private static readonly Color White = Color.white;
        private static readonly Color Ink = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
        private static readonly Color CardFace = new Color32(0x26, 0x26, 0x26, 0xFF);
        private static readonly Color CostDisc = new Color32(0xF2, 0xC2, 0x30, 0xFF);
        private static readonly Color PreviewInk = new Color32(0xFF, 0xE0, 0x8A, 0xFF);
        private static readonly Color RoleInk = new Color32(0xCF, 0xCF, 0xCF, 0xFF);
        private static readonly Color BarBack = new Color32(0x22, 0x22, 0x22, 0xE0);
        private static readonly Color BarFill = new Color32(0x4C, 0xAF, 0x50, 0xFF);
        private static readonly Color BlockBadge = new Color32(0x6C, 0x8E, 0xAD, 0xFF);
        private static readonly Color ChipBack = new Color32(0x00, 0x00, 0x00, 0xB0);
        private static readonly Color TooltipBack = new Color32(0x10, 0x10, 0x10, 0xEE);
        private static readonly Color ButtonFace = new Color32(0xF2, 0xE9, 0xD0, 0xFF);
        private static readonly Color BubbleFace = new Color32(0xFF, 0xFF, 0xFF, 0xF2);

        [MenuItem("WeirdLand/Build Prefabs")]
        public static void BuildAll()
        {
            EnsureFolder(PrefabDir);
            EnsureFolder("Assets/WeirdLand/Resources");

            var chip = Build<StatusChip>("StatusChip", BuildStatusChip);
            var fighter = Build<FighterView>("Fighter", n => BuildFighter(n, chip));
            var card = Build<CardView>("Card", BuildCard);
            var button = Build<Button>("Button", BuildButton);
            var bubble = Build<IntentBubble>("IntentBubble", BuildIntentBubble);

            var cat = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<VisualCatalog>();
                AssetDatabase.CreateAsset(cat, CatalogPath);
            }
            cat.Card = card;
            cat.Fighter = fighter;
            cat.StatusChip = chip;
            cat.Button = button;
            cat.IntentBubble = bubble;
            if (cat.Timings == null) cat.Timings = new AnimTimings();
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log("[PrefabBuilder] built prefabs in " + PrefabDir + " and " + CatalogPath);
        }

        // ---- prefabs ----

        private static void BuildStatusChip(Node root)
        {
            root.Size(120, 28);
            root.Image(ChipBack, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, true);
            var h = root.Get<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 2, 2);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.spacing = 0;

            var label = root.Child("Label");
            label.Size(104, 24);
            var labelText = label.Text("", 16, White, TextAlignmentOptions.Center, false, TextWrappingModes.NoWrap);

            var tip = root.Child("Tooltip");
            tip.Get<LayoutElement>().ignoreLayout = true;
            tip.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(260, 84));
            tip.Image(TooltipBack, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, false);
            var tipLabel = tip.Child("Text");
            tipLabel.Stretch(10, 10, 6, 6);
            var tipText = tipLabel.Text("", 16, White, TextAlignmentOptions.Center, true, TextWrappingModes.Normal);
            tip.Go.SetActive(false);

            Wire(root.Get<StatusChip>(), ("_label", labelText), ("_tooltip", tip.Go), ("_tooltipText", tipText));
        }

        private static void BuildFighter(Node root, StatusChip chipPrefab)
        {
            root.Size(220, 360);

            var name = root.Child("Name");
            name.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 30));
            var nameText = name.Text("", 24, White, TextAlignmentOptions.Center, true, TextWrappingModes.NoWrap);

            var bar = root.Child("HpBar");
            bar.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(200, 24));
            bar.Image(BarBack, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, false);
            var fill = bar.Child("Fill");
            fill.Place(Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            fill.Image(BarFill, null, Image.Type.Simple, false);
            var hp = bar.Child("Text");
            hp.Stretch(0, 0, 0, 0);
            var hpText = hp.Text("", 16, White, TextAlignmentOptions.Center, false, TextWrappingModes.NoWrap);

            var chips = root.Child("Statuses");
            chips.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -64), new Vector2(220, 28));
            var row = chips.Get<HorizontalLayoutGroup>();
            row.spacing = 4;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var body = root.Child("Body");
            body.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(120, 250));
            var knob = Sprite("UI/Skin/Knob.psd");
            var tinted = new List<Graphic>
            {
                Limb(body, "LegBack", new Vector2(0, 100), 105, -20),
                Limb(body, "LegFront", new Vector2(0, 100), 105, 20),
                Limb(body, "Torso", new Vector2(0, 190), 92, 0),
                Limb(body, "ArmBack", new Vector2(0, 180), 75, -25),
                Limb(body, "ArmFront", new Vector2(0, 180), 75, 60),
            };
            var head = body.Child("Head");
            head.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(56, 56));
            tinted.Add(head.Image(White, knob, Image.Type.Simple, false));
            var ball = body.Child("Ball");
            ball.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(56, 17), new Vector2(34, 34));
            ball.Image(White, knob, Image.Type.Simple, false);
            var outline = ball.Get<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(2, -2);
            ball.Go.SetActive(false);

            var block = root.Child("Block");
            block.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(-82, 170), new Vector2(48, 48));
            block.Image(BlockBadge, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, false);
            var blockLabel = block.Child("Text");
            blockLabel.Stretch(0, 0, 0, 0);
            var blockText = blockLabel.Text("0", 24, White, TextAlignmentOptions.Center, false, TextWrappingModes.NoWrap);
            blockText.fontStyle = FontStyles.Bold;
            block.Go.SetActive(false);

            var anchor = root.Child("NumberAnchor");
            anchor.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 270), Vector2.zero);

            Wire(root.Get<FighterView>(),
                ("_body", body.Rect), ("_numberAnchor", anchor.Rect), ("_name", nameText), ("_hp", hpText),
                ("_hpFill", fill.Rect), ("_block", block.Go), ("_blockText", blockText), ("_ball", ball.Go),
                ("_chipRow", chips.Rect), ("_chipPrefab", chipPrefab));
            WireArray(root.Get<FighterView>(), "_tinted", tinted);
        }

        private static Graphic Limb(Node body, string name, Vector2 joint, float length, float angle)
        {
            var n = body.Child(name);
            n.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), joint, new Vector2(8, length));
            n.Rect.localRotation = Quaternion.Euler(0, 0, angle);
            return n.Image(White, null, Image.Type.Simple, false);
        }

        private static void BuildCard(Node root)
        {
            root.Size(200, 280);
            var frame = root.Image(White, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, true);
            var group = root.Get<CanvasGroup>();
            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
            var button = root.Get<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.disabledColor = White; // dimming comes from the CanvasGroup alpha
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var face = root.Child("Face");
            face.Stretch(8, 8, 8, 8);
            face.Image(CardFace, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, false);

            var name = root.Child("Name");
            name.TopBand(48, 12, 14, 56);
            var nameText = name.Text("", 26, White, TextAlignmentOptions.Center, true, TextWrappingModes.Normal);

            var role = root.Child("Role");
            role.TopBand(16, 16, 72, 24);
            var roleText = role.Text("", 20, RoleInk, TextAlignmentOptions.Center, true, TextWrappingModes.NoWrap);

            var text = root.Child("Text");
            text.TopBand(16, 16, 100, 110);
            var bodyText = text.Text("", 20, White, TextAlignmentOptions.Center, true, TextWrappingModes.Normal);

            var preview = root.Child("Preview");
            preview.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            preview.Rect.offsetMin = new Vector2(16, 14);
            preview.Rect.offsetMax = new Vector2(-16, 64);
            var previewText = preview.Text("", 20, PreviewInk, TextAlignmentOptions.Center, true, TextWrappingModes.NoWrap);
            previewText.fontStyle = FontStyles.Bold;

            var cost = root.Child("Cost");
            cost.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(22, -22), new Vector2(48, 48));
            cost.Image(CostDisc, Sprite("UI/Skin/Knob.psd"), Image.Type.Simple, false);
            var costLabel = cost.Child("Text");
            costLabel.Stretch(0, 0, 0, 0);
            var costText = costLabel.Text("", 32, Ink, TextAlignmentOptions.Center, false, TextWrappingModes.NoWrap);
            costText.fontStyle = FontStyles.Bold;

            Wire(root.Get<CardView>(),
                ("_frame", frame), ("_name", nameText), ("_cost", costText), ("_body", bodyText), ("_role", roleText),
                ("_preview", previewText), ("_group", group), ("_button", button));
        }

        private static void BuildButton(Node root)
        {
            root.Size(260, 72);
            var face = root.Image(ButtonFace, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, true);
            var button = root.Get<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ColorBlock.defaultColorBlock;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = root.Child("Label");
            label.Stretch(12, 12, 6, 6);
            label.Text("", 28, Ink, TextAlignmentOptions.Center, true, TextWrappingModes.NoWrap);
        }

        private static void BuildIntentBubble(Node root)
        {
            root.Size(280, 130);
            root.Image(BubbleFace, Sprite("UI/Skin/UISprite.psd"), Image.Type.Sliced, false);
            var label = root.Child("Text");
            label.Stretch(12, 12, 10, 10);
            var text = label.Text("", 22, Ink, TextAlignmentOptions.Center, true, TextWrappingModes.Normal);
            Wire(root.Get<IntentBubble>(), ("_text", text));
        }

        // ---- plumbing ----

        private static T Build<T>(string name, Action<Node> build) where T : Component
        {
            var path = PrefabDir + "/" + name + ".prefab";
            bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name, typeof(RectTransform));
            try
            {
                root.name = name;
                var node = new Node(root);
                build(node);
                node.Prune();
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save prefab " + path);
            }
            finally
            {
                if (existing) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
            var component = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<T>();
            if (component == null) throw new InvalidOperationException(path + " has no " + typeof(T).Name);
            return component;
        }

        private static void Wire(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var p = so.FindProperty(field);
                if (p == null) throw new InvalidOperationException(target.GetType().Name + " has no field " + field);
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray<T>(Object target, string field, IList<T> values) where T : Object
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null || !p.isArray) throw new InvalidOperationException(target.GetType().Name + " has no array " + field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite Sprite(string builtinPath)
        {
            var s = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinPath);
            if (s == null) throw new InvalidOperationException("Built-in sprite not found: " + builtinPath);
            return s;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        /// <summary>A GameObject being (re)built. Children are found by name or created, in call order; untouched ones are removed.</summary>
        private sealed class Node
        {
            public readonly GameObject Go;
            private readonly List<Node> _children = new List<Node>();

            public Node(GameObject go)
            {
                Go = go;
                Go.layer = UiLayer;
                if (Go.GetComponent<RectTransform>() == null) Go.AddComponent<RectTransform>();
            }

            public RectTransform Rect => (RectTransform)Go.transform;

            public Node Child(string name)
            {
                Transform found = null;
                var t = Go.transform;
                for (int i = 0; i < t.childCount; i++)
                {
                    var c = t.GetChild(i);
                    if (c.name == name && !_children.Exists(n => n.Go.transform == c))
                    {
                        found = c;
                        break;
                    }
                }
                if (found == null)
                {
                    var go = new GameObject(name, typeof(RectTransform));
                    go.transform.SetParent(t, false);
                    found = go.transform;
                }
                found.SetSiblingIndex(_children.Count);
                found.gameObject.SetActive(true);
                var node = new Node(found.gameObject);
                node.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
                _children.Add(node);
                return node;
            }

            public T Get<T>() where T : Component
            {
                if (!Go.TryGetComponent<T>(out var c)) c = Go.AddComponent<T>();
                return c;
            }

            /// <summary>Removes children that this build did not ask for, recursively.</summary>
            public void Prune()
            {
                var t = Go.transform;
                for (int i = t.childCount - 1; i >= 0; i--)
                {
                    var c = t.GetChild(i);
                    if (!_children.Exists(n => n.Go.transform == c)) Object.DestroyImmediate(c.gameObject);
                }
                foreach (var n in _children) n.Prune();
            }

            public void Place(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
            {
                var r = Rect;
                r.anchorMin = anchorMin;
                r.anchorMax = anchorMax;
                r.pivot = pivot;
                r.anchoredPosition = position;
                r.sizeDelta = size;
                r.localRotation = Quaternion.identity;
                r.localScale = Vector3.one;
            }

            public void Size(float w, float h)
                => Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));

            /// <summary>Fills the parent minus the given insets.</summary>
            public void Stretch(float left, float right, float top, float bottom)
            {
                Place(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                Rect.offsetMin = new Vector2(left, bottom);
                Rect.offsetMax = new Vector2(-right, -top);
            }

            /// <summary>A full-width band <paramref name="top"/> below the parent's top edge.</summary>
            public void TopBand(float left, float right, float top, float height)
            {
                Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
                Rect.offsetMin = new Vector2(left, -top - height);
                Rect.offsetMax = new Vector2(-right, -top);
            }

            public Image Image(Color color, Sprite sprite, Image.Type type, bool raycast)
            {
                var img = Get<Image>();
                img.sprite = sprite;
                img.type = type;
                img.color = color;
                img.raycastTarget = raycast;
                img.preserveAspect = false;
                return img;
            }

            public TextMeshProUGUI Text(string text, float size, Color color, TextAlignmentOptions align, bool autoSize,
                TextWrappingModes wrap)
            {
                var font = TMP_Settings.defaultFontAsset;
                if (font == null) throw new InvalidOperationException("TMP_Settings.defaultFontAsset is missing; run tools/unity.sh setup");
                var tmp = Get<TextMeshProUGUI>();
                tmp.font = font;
                tmp.fontSharedMaterial = font.material;
                tmp.text = text;
                tmp.fontStyle = FontStyles.Normal;
                tmp.enableAutoSizing = autoSize;
                tmp.fontSizeMax = size;
                tmp.fontSizeMin = Mathf.Min(12, size);
                tmp.fontSize = size;
                tmp.color = color;
                tmp.alignment = align;
                tmp.textWrappingMode = wrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.richText = false;
                tmp.raycastTarget = false;
                tmp.margin = Vector4.zero;
                return tmp;
            }
        }
    }
}
