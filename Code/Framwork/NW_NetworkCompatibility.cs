using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Network.Framework
{
    [AddComponentMenu("Network/Framework/Network Compatibility"), DisallowMultipleComponent]
    public class NW_NetworkCompatibility : NetworkBehaviour
    {
        [SerializeField] ulong m_ClientId = ulong.MinValue;

        public ulong ClientId => m_ClientId;

        public int ClientIndex { get; private set; } = 0;

        private void Start()
        {
            NetworkRigidbody body;
            NetworkRigidbody2D body2D;
            NetworkAnimator animator;
            NetworkTransform tran;
            NetworkObject obj;

            if (NetworkManager.Singleton == null)
            {
                if (TryGetComponent(out body))
                    body.enabled = false;

                if (TryGetComponent(out body2D))
                    body2D.enabled = false;

                if (TryGetComponent(out animator))
                    animator.enabled = false;

                if (TryGetComponent(out tran))
                    tran.enabled = false;

                if (TryGetComponent(out obj))
                    obj.enabled = false;

                return;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.LocalClientId == NetworkObject.OwnerClientId && NetworkManager.IsServer)
                NetworkObject.ChangeOwnership(m_ClientId);

            if (IsLocalPlayer)
            {
                Debug.Log($"IsLocalPlayer: {IsLocalPlayer}", this);
                //ClientConnectionServerRpc();
            }

            // UNITY BUG: ChangeOwnership doesn't refresh it's owner objects list
        }

        [ServerRpc]
        private void ClientConnectionServerRpc() => ClientIndex = NetworkManager.Singleton.ConnectedClientsList.Count;
    }
}
