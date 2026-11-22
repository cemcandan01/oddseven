using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WeirdLand.Unity
{
    /// <summary>Small status label ("Dengesiz 2") whose description shows while the pointer is over it.</summary>
    public class StatusChip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _tooltip;
        [SerializeField] private TMP_Text _tooltipText;

        public string Label => _label.text;
        public string Tooltip => _tooltipText.text;

        public void Bind(string label, string tooltip)
        {
            _label.text = label;
            _tooltipText.text = tooltip;
            _tooltip.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData) => _tooltip.SetActive(true);

        public void OnPointerExit(PointerEventData eventData) => _tooltip.SetActive(false);
    }
}
