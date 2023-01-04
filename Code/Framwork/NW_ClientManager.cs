using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Netcode.Transports.Facepunch;

using static Network.Framework.NW_NetworkExtensions;
using System.Threading.Tasks;

namespace Network.Framework
{
    [AddComponentMenu("Network/Framework/Client Manager"), DisallowMultipleComponent]
    [RequireComponent(typeof(NW_NetworkManager))]
    public class NW_ClientManager : MonoBehaviour
    {
        public static NW_ClientManager Instance { get; private set; } = null;

        public DisconnectReason DisconnectReason { get; private set; } = new DisconnectReason();

        public static event UnityAction<ConnectStatus> OnConnectionFinished;
        public static event UnityAction OnNetworkTimedOut;

        private NW_NetworkManager portal = null;
        private FacepunchTransport transport = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            portal = GetComponent<NW_NetworkManager>();
            transport = GetComponent<FacepunchTransport>();

            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

            DisconnectReason.OnReasonChanged += OnReasonChanged;
            NW_NetworkManager.OnClientReadied += OnNetworkReadied;
            NW_NetworkManager.OnConnectionCompleted += OnClientConnectionFinished;
            NW_NetworkManager.OnDisconnectReceived += OnDisconnectReasonReceived;

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        private void OnDestroy()
        {
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

            DisconnectReason.OnReasonChanged -= OnReasonChanged;
            NW_NetworkManager.OnClientReadied -= OnNetworkReadied;
            NW_NetworkManager.OnConnectionCompleted -= OnClientConnectionFinished;
            NW_NetworkManager.OnDisconnectReceived -= OnDisconnectReasonReceived;

            if (NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        /// <summary>
        /// Start client [With Steam Transport]
        /// </summary>
        /// <param name="lobby"></param>
        /// <returns></returns>
        public bool StartSteamClient(SteamId targetId)
        {
            transport.targetSteamId = targetId;
            return StartNetworkClient();
        }

        /// <summary>
        /// Start client
        /// </summary>
        /// <returns></returns>
        public bool StartNetworkClient()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                clientGUID = System.Guid.NewGuid().ToString(),
                clientScene = SceneManager.GetActiveScene().buildIndex,
                displayName = PlayerPrefs.GetString("PlayerName", "Missing Name")
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
            return NetworkManager.Singleton.StartClient();
        }

        #region Steam Callbacks

        private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
        {
            portal.SetLobby(lobby);
            var result = await lobby.Join();

            if (result != RoomEnter.Success)
            {
                Debug.LogError($"Couldn't enter the lobby, {result}", this);
                return;
            }

            StartSteamClient(friendId);
        }

        #endregion

        #region Network Callbacks

        private void OnNetworkReadied()
        {
            if (!NetworkManager.Singleton.IsClient)
                return;

            if (!NetworkManager.Singleton.IsHost)
                NW_NetworkManager.OnClientDisconnectRequested += OnUserDisconnectRequested;
        }

        private void OnUserDisconnectRequested()
        {
            Debug.Log($"You have disconnected from the server", this);

            DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);
            NetworkManager.Singleton.Shutdown();

            OnClientDisconnect(NetworkManager.Singleton.LocalClientId);

            portal.MainScene.TryLoadRegularScene();
        }

        private void OnClientConnectionFinished(ConnectStatus status)
        {
            if (status != ConnectStatus.Success)
                DisconnectReason.SetDisconnectReason(status);

            OnConnectionFinished?.Invoke(status);
        }

        private void OnDisconnectReasonReceived(ConnectStatus status)
        {
            Debug.Log($"You have been disconnected by {status}", this);
            DisconnectReason.SetDisconnectReason(status);
        }

        private void OnReasonChanged(ConnectStatus status)
        {
            Debug.Log($"{nameof(OnReasonChanged)} -> {status}", this);

            switch (status)
            {
                case ConnectStatus.KickDisconnect: OnClientKicked(); break;
            }

            void OnClientKicked()
            {
                Debug.Log("You have been kicked", this);
                portal.CurrentLobby?.Leave();
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            portal.CurrentLobby?.Leave();

            if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
            {
                NW_NetworkManager.OnClientDisconnectRequested -= OnUserDisconnectRequested;

                if (Application.CanStreamedLevelBeLoaded(portal.MainScene.SceneName) && SceneManager.GetActiveScene().name != portal.MainScene.SceneName)
                {
                    if (!DisconnectReason.HasTransitionReason)
                        DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);

                    portal.MainScene.TryLoadRegularScene();
                }
                else
                    OnNetworkTimedOut?.Invoke();
            }
        } 

        #endregion
    }
}