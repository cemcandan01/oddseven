using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>One card in the hand. References are wired by PrefabBuilder.</summary>
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _cost;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private TMP_Text _role;
        [SerializeField] private TMP_Text _preview;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _button;

        private bool _listening;

        public CardInstance Card { get; private set; }
        public event Action<CardView> Clicked;

        public Image Frame => _frame;
        public TMP_Text NameText => _name;
        public TMP_Text CostText => _cost;
        public TMP_Text BodyText => _body;
        public TMP_Text RoleText => _role;
        public TMP_Text PreviewText => _preview;
        public CanvasGroup Group => _group;
        public Button Button => _button;

        public void Bind(CardInstance c, CardPreview p, bool playable, UiText t, VisualCatalog cat)
        {
            if (!_listening)
            {
                _button.onClick.AddListener(OnClick);
                _listening = true;
            }
            Card = c;
            _name.text = c.Def.Name;
            _cost.text = c.Cost.ToString(CultureInfo.InvariantCulture);
            _body.text = c.Def.Text ?? "";
            _role.text = t.Get(UiKeys.Role(c.Def.Role));
            _preview.text = PreviewLine(p, t);
            _frame.color = RoleColor(c.Def.Role, cat);
            _group.alpha = playable ? 1f : 0.5f;
            _button.interactable = playable;
        }

        private static string PreviewLine(CardPreview p, UiText t)
        {
            var lines = new List<string>(2);
            if (p.Damage > 0) lines.Add(t.Format(UiKeys.CardDamage, p.Damage));
            if (p.Block > 0) lines.Add(t.Format(UiKeys.CardBlock, p.Block));
            return string.Join("\n", lines);
        }

        private static Color RoleColor(CardRole role, VisualCatalog cat)
        {
            switch (role)
            {
                case CardRole.Attack: return cat.AttackCard;
                case CardRole.Defense: return cat.DefenseCard;
                default: return cat.BothCard;
            }
        }

        private void OnClick() => Clicked?.Invoke(this);

        private void OnDestroy()
        {
            if (_listening && _button != null) _button.onClick.RemoveListener(OnClick);
        }
    }
}
