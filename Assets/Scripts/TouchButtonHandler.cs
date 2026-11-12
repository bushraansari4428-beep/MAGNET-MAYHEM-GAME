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
        private static float _lastGlobalTriggerTime = 0f;

        public static bool CanTriggerGlobal()
        {
            if (Time.unscaledTime - _lastGlobalTriggerTime < 0.35f)
                return false;
            _lastGlobalTriggerTime = Time.unscaledTime;
            return true;
        }

        public void Initialize(UnityAction action, Color normalCol, Color pressedCol)
        {
            OnAction = action;
            _normalColor = normalCol;
            _pressedColor = pressedCol;
            _targetImage = GetComponent<Image>();
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
            // Already handled on PointerDown for instant zero-latency touch response
        }

        public void Trigger()
        {
            if (!CanTriggerGlobal())
                return;

            OnAction?.Invoke();
        }
    }
}
