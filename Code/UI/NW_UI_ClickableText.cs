using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Network.UI
{
    [AddComponentMenu("Network/UI/UI Clickable Text"), DisallowMultipleComponent]
    public class NW_UI_ClickableText : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] TMP_Text m_Text = default;

        public event UnityAction OnClickEvent;

        public void OnPointerClick(PointerEventData eventData) => OnClickEvent?.Invoke();

        public void SetColor(Color color) => m_Text.color = color;
        public void SetColor(Color32 color) => m_Text.color = color;
    }
}