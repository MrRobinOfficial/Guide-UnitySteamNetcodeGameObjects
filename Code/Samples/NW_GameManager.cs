using Network.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Network.Samples
{
    public class NW_GameManager : MonoBehaviour
    {
        public static NW_GameManager Instance { get; private set; } = null;

        [Header("References")]
        [SerializeField] BoxCollider m_WorldBounds = default;

        private bool hasShutdown = false;

        private InputAction pingAction;

        public Vector3 GetRandomLocation()
        {
            var bounds = m_WorldBounds.bounds;

            var point = new Vector3
            (
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (point != m_WorldBounds.ClosestPoint(point))
                point = GetRandomLocation(); // Out of the collider!

            return point;
        }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            if (pingAction == null)
                pingAction = new InputAction(binding: "<Keyboard>/tab");

            pingAction.Enable();

            pingAction.performed += PingAction_performed;
        }

        private void OnDisable()
        {
            pingAction.performed -= PingAction_performed;
            pingAction.Disable();
        }

        private void PingAction_performed(InputAction.CallbackContext ctx)
        {
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            print($"Ping: {transport.GetCurrentRtt(NetworkManager.Singleton.LocalClientId)} ms");
        }

        private void OnGUI()
        {
            if (NetworkManager.Singleton == null)
                return;

            if (hasShutdown && GUI.Button(new Rect(10, 50, 300, 40), "Restart Server"))
            {
                NW_NetworkManager.Instance.StartNetworkHost();
                hasShutdown = false;
            }

            if (!NetworkManager.Singleton.IsServer)
                return;

            if (GUI.Button(new Rect(10, 10, 300, 40), "End Current Game"))
                NW_ServerManager.Instance.EndGame();

            if (!hasShutdown && GUI.Button(new Rect(10, 90, 300, 40), "Stop Server"))
            {
                NW_NetworkManager.Instance.RequestDisconnect();
                hasShutdown = true;
            }
        }
    }
}