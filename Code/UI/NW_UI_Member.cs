using Steamworks;
using TMPro;
using UnityEngine;

namespace Network.UI
{
    public class NW_UI_Member : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TMP_Text m_DisplayName = default;
        [SerializeField] TMP_Text m_TeamName = default;

        public void Init(Friend friend)
        {
            m_DisplayName.SetText(friend.Name);
            m_TeamName.SetText(friend.GetRichPresence("clan"));
        }
    }
}