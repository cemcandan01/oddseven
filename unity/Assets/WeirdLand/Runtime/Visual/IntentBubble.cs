using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>Speech bubble over the opponent showing what it will do this turn.</summary>
    public class IntentBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        public string Text => _text.text;

        public void Show(IntentView v, UiText t)
        {
            _text.text = Compose(v, t);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _text.text = "";
            gameObject.SetActive(false);
        }

        private static string Compose(IntentView v, UiText t)
        {
            switch (v.Kind)
            {
                case IntentKind.Pass:
                    return t.Format(UiKeys.IntentPass, v.PassTargetName, UiText.Percent(v.PassChance));
                case IntentKind.Card:
                    var lines = new List<string> { v.CardName };
                    if (v.Damage > 0) lines.Add(t.Format(UiKeys.IntentDamage, v.Damage));
                    if (v.Block > 0) lines.Add(t.Format(UiKeys.IntentBlock, v.Block));
                    if (v.Statuses != null)
                        foreach (var s in v.Statuses)
                        {
                            var name = t.Get(UiKeys.Status(s.Status));
                            lines.Add(s.Stacks > 1 ? name + " " + s.Stacks.ToString(CultureInfo.InvariantCulture) : name);
                        }
                    return string.Join("\n", lines);
                default:
                    return t.Get(UiKeys.IntentWait);
            }
        }
    }
}
