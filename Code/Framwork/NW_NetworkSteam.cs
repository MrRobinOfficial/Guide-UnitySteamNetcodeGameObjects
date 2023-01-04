using Steamworks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static Network.Framework.NW_NetworkExtensions;

namespace Network.Framework
{
    [AddComponentMenu("Network/Framework/NetworkSteam"), DisallowMultipleComponent]
    public class NW_NetworkSteam : NetworkBehaviour
    {
        private NW_NetworkCompatibility compatibility;

        [Header("References")]
        [SerializeField] TMP_Text m_NameTag = default;

        public FixedString64Bytes GUID { get; private set; }
        public MemberData Data { get; private set; }

        private void Awake() => compatibility = GetComponent<NW_NetworkCompatibility>();

        private void OnEnable() => Init();

        public void Init()
        {
            if (NW_ServerManager.Instance.ClientLookup.TryGetValue(compatibility.ClientId, out var guid))
                GUID = guid;

            if (NW_ServerManager.Instance.MemberLookup.TryGetValue(GUID, out var data))
                Data = data;

            if (m_NameTag == null || !SteamClient.IsValid)
            {
                m_NameTag.gameObject.SetActive(false);
                return;
            }

            m_NameTag.SetText(Data.DisplayName.ToString());
            m_NameTag.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            var rot = Camera.main.transform.rotation;
            m_NameTag.transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
        }
    }
}