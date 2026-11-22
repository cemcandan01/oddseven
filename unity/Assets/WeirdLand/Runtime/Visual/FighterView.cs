using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeirdLand.Core.Data;

namespace WeirdLand.Unity
{
    /// <summary>Placeholder stick figure with name, HP bar, block badge, ball marker and status chips.</summary>
    public class FighterView : MonoBehaviour
    {
        [SerializeField] private RectTransform _body;
        [SerializeField] private RectTransform _numberAnchor;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _hp;
        [SerializeField] private RectTransform _hpFill;
        [SerializeField] private GameObject _block;
        [SerializeField] private TMP_Text _blockText;
        [SerializeField] private GameObject _ball;
        [SerializeField] private RectTransform _chipRow;
        [SerializeField] private StatusChip _chipPrefab;
        [SerializeField] private Graphic[] _tinted = new Graphic[0];

        private static readonly StatusId[] StatusOrder = (StatusId[])Enum.GetValues(typeof(StatusId));
        private readonly List<StatusChip> _chips = new List<StatusChip>();
        private string _hpLabel = "";

        public RectTransform Body => _body;
        public RectTransform NumberAnchor => _numberAnchor;
        public TMP_Text NameText => _name;
        public TMP_Text HpText => _hp;
        public TMP_Text BlockText => _blockText;
        public float HpFraction => _hpFill.anchorMax.x;
        public bool BlockVisible => _block.activeSelf;
        public bool BallVisible => _ball.activeSelf;
        public IReadOnlyList<StatusChip> Chips => _chips;

        public void Setup(string playerName, string hpLabel, Color color, bool facingLeft)
        {
            _name.text = playerName;
            _hpLabel = hpLabel ?? "";
            foreach (var g in _tinted) g.color = color;
            var s = _body.localScale;
            _body.localScale = new Vector3(facingLeft ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
        }

        public void SetHp(int hp, int max)
        {
            float f = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
            _hpFill.anchorMax = new Vector2(f, _hpFill.anchorMax.y);
            _hp.text = _hpLabel + " " + hp.ToString(CultureInfo.InvariantCulture) + "/" + max.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Shows the block badge with the amount; hidden when 0.</summary>
        public void SetBlock(int block)
        {
            _blockText.text = block.ToString(CultureInfo.InvariantCulture);
            _block.SetActive(block > 0);
        }

        public void SetBall(bool hasBall) => _ball.SetActive(hasBall);

        /// <summary>One chip per status with stacks above 0, in StatusId enum order (never dictionary order).</summary>
        public void SetStatuses(IReadOnlyDictionary<StatusId, int> s, UiText t)
        {
            int used = 0;
            foreach (var id in StatusOrder)
            {
                if (s == null || !s.TryGetValue(id, out var stacks) || stacks <= 0) continue;
                if (used == _chips.Count) _chips.Add(Instantiate(_chipPrefab, _chipRow, false));
                _chips[used].transform.SetSiblingIndex(used);
                _chips[used].Bind(t.Get(UiKeys.Status(id)) + " " + stacks.ToString(CultureInfo.InvariantCulture),
                    t.Get(UiKeys.StatusDesc(id)));
                used++;
            }
            for (int i = _chips.Count - 1; i >= used; i--)
            {
                var chip = _chips[i];
                _chips.RemoveAt(i);
                if (chip == null) continue;
                chip.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(chip.gameObject);
                else DestroyImmediate(chip.gameObject);
            }
        }
    }
}
