using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MagnetMayhem
{
    public class TouchButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public UnityAction OnAction;
        private Image _targetImage;
        private Color _normalColor;
        private Color _pressedColor;
        private RectTransform _rect;

        public void Initialize(UnityAction action, Color normalCol, Color pressedCol)
        {
            OnAction = action;
            _normalColor = normalCol;
            _pressedColor = pressedCol;
            _targetImage = GetComponent<Image>();
            _rect = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_targetImage != null) _targetImage.color = _pressedColor;
            Trigger();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_targetImage != null) _targetImage.color = _normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Already triggered on PointerDown for instant zero-latency touch response
        }

        public void Trigger()
        {
            OnAction?.Invoke();
        }
    }
}
