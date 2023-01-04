using Network.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

namespace Network.UI
{
    [AddComponentMenu("Network/UI/UI Main Menu"), DisallowMultipleComponent]
    public class NW_UI_MainMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Button m_RefreshLobbies = default;
        [SerializeField] Transform m_LobbyRect = default;
        [SerializeField] Button m_LobbyPrefab = default;
        [SerializeField] Toggle m_JoinableToggle = default;
        [SerializeField] TMP_Dropdown m_VisibilityDropdown = default;
        [SerializeField] TMP_InputField m_NameField = default;
        [SerializeField] TMP_InputField m_MaxMembersField = default;
        [SerializeField] TMP_InputField m_JoinServerField = default;
        [SerializeField] Button m_CreateServerBtn = default;
        [SerializeField] Button m_CreateDedicatedServerBtn = default;

        private void Start()
        {
            var list = new List<string>
            {
                nameof(NW_SteamExtensions.LobbyVisibility.Public),
                nameof(NW_SteamExtensions.LobbyVisibility.Private),
                nameof(NW_SteamExtensions.LobbyVisibility.FriendsOnly),
                nameof(NW_SteamExtensions.LobbyVisibility.Invisible)
            };

            if (m_VisibilityDropdown != null)
                m_VisibilityDropdown.AddOptions(list);

            m_RefreshLobbies.onClick.AddListener(async() => await RefreshLobbies());
            m_CreateServerBtn.onClick.AddListener(CreateServer);
            m_CreateDedicatedServerBtn.onClick.AddListener(CreateDedicatedServer);
            m_JoinServerField.onEndEdit.AddListener(JoinServer);
        }

        private void OnDestroy()
        {
            m_RefreshLobbies.onClick.RemoveAllListeners();
            m_CreateServerBtn.onClick.RemoveAllListeners();
            m_CreateDedicatedServerBtn.onClick.RemoveAllListeners();
            m_JoinServerField.onEndEdit.RemoveAllListeners();
        }

        private async void OnEnable() => await RefreshLobbies();

        private async Task RefreshLobbies()
        {
            for (int i = 0; i < m_LobbyRect.childCount; i++)
                Destroy(m_LobbyRect.GetChild(i).gameObject);

            var lobbies = await NW_SteamExtensions.GetLobbiesAsync();

            if (lobbies == null)
                lobbies = new Steamworks.Data.Lobby[0];

            foreach (var lobby in lobbies)
            {
                var obj = Instantiate(m_LobbyPrefab, m_LobbyRect);
                var tmpText = obj.GetComponentInChildren<TMP_Text>();

                tmpText.SetText($"Lobby [{lobby.MemberCount:00}]: {lobby.Owner}");
                obj.onClick.AddListener(() => NW_ClientManager.Instance.StartSteamClient(lobby.Owner.Id));
            }
        }

        private void JoinServer(string input)
        {
            input = input.Trim();

            if (string.IsNullOrEmpty(input))
                return;

            var id = NW_SteamExtensions.GetLobbyIdFromCode(input);
            NW_ClientManager.Instance.StartSteamClient(id);
        }

        private async void CreateServer()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
                return;

            byte maxMembers = 4;

            if (m_MaxMembersField != null)
            {
                if (!byte.TryParse(m_MaxMembersField.text, out maxMembers))
                    maxMembers = 1; // Need a least one member!
            }

            string lobbyName = $"Lobby {System.Guid.NewGuid()}";

            if (m_NameField != null)
                lobbyName = m_NameField.text;

            bool joinable = true;

            if (m_JoinableToggle != null)
                joinable = m_JoinableToggle.isOn;

            var visibility = NW_SteamExtensions.LobbyVisibility.Public;

            if (m_VisibilityDropdown != null)
                visibility = (NW_SteamExtensions.LobbyVisibility)m_VisibilityDropdown.value;

            if (m_CreateServerBtn != null)
                m_CreateServerBtn.gameObject.SetActive(false);

            await NW_NetworkManager.Instance.StartSteamHost(new NW_SteamExtensions.LobbyConfig
            {
                name = lobbyName,
                joinable = joinable,
                maxMembers = maxMembers,
                visibility = visibility,
            });
        }

        private async void CreateDedicatedServer()
        {
            if (NetworkManager.Singleton.IsServer)
                return;

            byte maxMembers = 4;

            if (m_MaxMembersField != null)
            {
                if (!byte.TryParse(m_MaxMembersField.text, out maxMembers))
                    maxMembers = 1; // Need a least one member!
            }

            string lobbyName = $"Lobby {System.Guid.NewGuid()}";

            if (m_NameField != null)
                lobbyName = m_NameField.text;

            bool joinable = true;

            if (m_JoinableToggle != null)
                joinable = m_JoinableToggle.isOn;

            var visibility = NW_SteamExtensions.LobbyVisibility.Public;

            if (m_VisibilityDropdown != null)
                visibility = (NW_SteamExtensions.LobbyVisibility)m_VisibilityDropdown.value;

            if (m_CreateServerBtn != null)
                m_CreateServerBtn.gameObject.SetActive(false);

            await NW_NetworkManager.Instance.StartSteamServer(new NW_SteamExtensions.LobbyConfig
            {
                name = lobbyName,
                joinable = joinable,
                maxMembers = maxMembers,
                visibility = visibility,
            });
        }
    }
}