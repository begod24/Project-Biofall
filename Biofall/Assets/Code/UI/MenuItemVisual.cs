using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Biofall.UI
{
    // One row of the main menu: label, the bracket frame that appears around the active row,
    // and the small marker to its left. Highlights on hover and on keyboard selection so the
    // list reads the same either way.
    [RequireComponent(typeof(Button))]
    public sealed class MenuItemVisual : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Graphic frame;
        [SerializeField] private TMP_Text marker;

        [SerializeField] private Color idleColor = new(0.72f, 0.72f, 0.74f, 1f);
        [SerializeField] private Color activeColor = new(1f, 0.16f, 0.16f, 1f);

        private bool _hovered;
        private bool _selected;

        private void Awake() => Apply();

        private void OnEnable() { _hovered = false; _selected = false; Apply(); }

        public void OnPointerEnter(PointerEventData _) { _hovered = true; Apply(); }
        public void OnPointerExit(PointerEventData _) { _hovered = false; Apply(); }
        public void OnSelect(BaseEventData _) { _selected = true; Apply(); }
        public void OnDeselect(BaseEventData _) { _selected = false; Apply(); }

        private void Apply()
        {
            bool active = _hovered || _selected;

            if (label != null) label.color = active ? activeColor : idleColor;
            if (frame != null) frame.enabled = active;
            if (marker != null) marker.enabled = active;
        }
    }
}
