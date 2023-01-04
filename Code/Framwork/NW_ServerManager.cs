using Steamworks;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using static Network.Framework.NW_NetworkExtensions;
using static Network.Framework.NW_SteamExtensions;

namespace Network.Framework
{
    [AddComponentMenu("Network/Framework/Server Manager"), DisallowMultipleComponent]
    public class NW_ServerManager : MonoBehaviour
    {
        private const int MAX_CONNECTION_PAYLOAD = 1024;

        public static NW_ServerManager Instance { get; private set; } = null;

        public static event UnityAction OnGameInProgressChanged;
        public static event UnityAction OnServerStarted;
        public static event UnityAction OnServerShutdown;

        [Header("Confg")]
        [SerializeField] byte m_MaxPlayers = 4;

        public bool GameInProgress
        {
            get => gameInProgress;
            set
            {
                OnGameInProgressChanged?.Invoke();
                gameInProgress = value;
            }
        }

        public IReadOnlyDictionary<FixedString64Bytes, MemberData> MemberLookup => members;
        public IReadOnlyDictionary<ulong, FixedString64Bytes> ClientLookup => clientIdToGuid;
        public IReadOnlyDictionary<SteamId, FixedString64Bytes> SteamLookup => steamIdToGuid;

        private Dictionary<FixedString64Bytes, MemberData> members = new(capacity: MAX_CONNECTION_PAYLOAD);
        private Dictionary<ulong, FixedString64Bytes> clientIdToGuid = new(capacity: MAX_CONNECTION_PAYLOAD);
        private Dictionary<SteamId, FixedString64Bytes> steamIdToGuid = new(capacity: MAX_CONNECTION_PAYLOAD);
        private Dictionary<ulong, int> clientSceneMap = new(capacity: MAX_CONNECTION_PAYLOAD);
        private bool gameInProgress = false;

        private NW_NetworkManager portal = null;

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

            NW_NetworkManager.OnClientReadied += HandleNetworkReadied;

            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;

            members = new Dictionary<FixedString64Bytes, MemberData>();
            clientIdToGuid = new Dictionary<ulong, FixedString64Bytes>();
            steamIdToGuid = new Dictionary<SteamId, FixedString64Bytes>();
            clientSceneMap = new Dictionary<ulong, int>();
        }

        private void OnDestroy()
        {
            NW_NetworkManager.OnClientReadied -= HandleNetworkReadied;

            if (NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        }

        /// <summary>
        /// Get member data via <seealso cref="ulong"/>
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        public MemberData? GetMemberData(ulong clientId)
        {
            if (clientIdToGuid.TryGetValue(clientId, out var clientGuid))
            {
                if (members.TryGetValue(clientGuid, out MemberData playerData))
                    return playerData;
                else
                    Debug.LogWarning($"No member data found for client id: {clientId}");
            }
            else
                Debug.LogWarning($"No client guid found for client id: {clientId}");

            return null;
        }

        /// <summary>
        /// Get member data via <seealso cref="SteamId"/>
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        public MemberData? GetMemberData(SteamId steamId)
        {
            if (steamIdToGuid.TryGetValue(steamId, out var clientGuid))
            {
                if (members.TryGetValue(clientGuid, out MemberData playerData))
                    return playerData;
                else
                    Debug.LogWarning($"No member data found for client id: {steamId}");
            }
            else
                Debug.LogWarning($"No client guid found for client id: {steamId}");

            return null;
        }

        /// <summary>
        /// Kick specific client from the server
        /// </summary>
        /// <param name="clientId"></param>
        public void KickClient(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            if (NetworkManager.Singleton.SpawnManager != null)
            {
                var networkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

                if (networkObject != null)
                    networkObject.Despawn(true);
            }

            NetworkManager.Singleton.DisconnectClient(clientId);
        }

        /// <summary>
        /// Server will start the game
        /// </summary>
        public bool StartGame()
        {
            if (!NetworkManager.Singleton.IsServer)
                return false;

            gameInProgress = true;
            portal.GameScene.TryLoadNetworkScene();
            return true;
        }

        /// <summary>
        /// Server will end the game
        /// </summary>
        public bool EndGame()
        {
            if (!NetworkManager.Singleton.IsServer)
                return false;

            gameInProgress = false;
            portal.LobbyScene.TryLoadNetworkScene();
            return true;
        }

        private void ClearData()
        {
            members.Clear();
            clientIdToGuid.Clear();
            clientSceneMap.Clear();

            gameInProgress = false;
        }

        private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
        {
            if (connectionData.Length > MAX_CONNECTION_PAYLOAD)
            {
                callback(createPlayerObject: false, playerPrefabHash: uint.MinValue, approved: false, position: null, rotation: null);
                return;
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                callback(createPlayerObject: false, playerPrefabHash: null, approved: true, position: null, rotation: null);
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

            var status = ConnectStatus.Success;

            if (gameInProgress)
                status = ConnectStatus.GameInProgress;
            else if (members.Count >= m_MaxPlayers)
                status = ConnectStatus.ServerFull;

            if (status == ConnectStatus.Success)
            {
                clientSceneMap[clientId] = connectionPayload.clientScene;
                clientIdToGuid[clientId] = connectionPayload.clientGUID;
                members[connectionPayload.clientGUID] = new MemberData(SteamClient.SteamId, connectionPayload.displayName, clientId);
            }

            callback(createPlayerObject: false, playerPrefabHash: 0, approved: true, position: null, rotation: null);

            portal.ServerToClientConnectResult(clientId, status);

            if (status != ConnectStatus.Success)
                StartCoroutine(WaitToDisconnectClient(clientId, status));
        }

        private IEnumerator WaitToDisconnectClient(ulong clientId, ConnectStatus reason, float seconds = 0f)
        {
            portal.ServerToClientSetDisconnectReason(clientId, reason);
            yield return new WaitForSeconds(seconds);
            KickClient(clientId);
        }

        #region Network Callbacks

        private void HandleNetworkReadied()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            NW_NetworkManager.OnClientDisconnectRequested += HandleUserDisconnectRequested;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            NW_NetworkManager.OnClientSceneChanged += HandleClientSceneChanged;

            portal.LobbyScene.TryLoadNetworkScene();

            if (NetworkManager.Singleton.IsHost)
            {
                clientSceneMap[NetworkManager.Singleton.LocalClientId] = SceneManager.GetActiveScene().buildIndex;
            }
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            clientSceneMap.Remove(clientId);

            if (clientIdToGuid.TryGetValue(clientId, out var guid))
            {
                clientIdToGuid.Remove(clientId);

                if (members[guid].ClientId == clientId)
                    members.Remove(guid);
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                NW_NetworkManager.OnClientDisconnectRequested -= HandleUserDisconnectRequested;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
                NW_NetworkManager.OnClientSceneChanged -= HandleClientSceneChanged;
            }
        }

        private void HandleClientSceneChanged(ulong clientId, int sceneIndex) => clientSceneMap[clientId] = sceneIndex;

        private void HandleUserDisconnectRequested()
        {
            HandleClientDisconnect(NetworkManager.Singleton.LocalClientId);

            NetworkManager.Singleton.Shutdown();

            ClearData();

            portal.MainScene.TryLoadRegularScene();
        }

        private void HandleServerStarted()
        {
            if (!NetworkManager.Singleton.IsHost)
                return;

            var clientGuid = System.Guid.NewGuid().ToString();
            var playerName = PlayerPrefs.GetString("PlayerName", IsSteamRunning ? SteamClient.Name : "Missing Name");

            members.TryAdd(clientGuid, new MemberData
            (
                IsSteamRunning ? SteamClient.SteamId : default, 
                playerName, 
                NetworkManager.Singleton.LocalClientId
            ));

            clientIdToGuid.TryAdd(NetworkManager.Singleton.LocalClientId, clientGuid);

            OnServerStarted?.Invoke();
        }

        #endregion
    }
}