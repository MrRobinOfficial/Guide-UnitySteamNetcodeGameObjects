using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

using static Network.Framework.NW_NetworkExtensions;

namespace Network.Samples
{
    [AddComponentMenu("Network/Samples/Car Controller"), DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class NW_CarController : NetworkBehaviour
    {
        [System.Serializable]
        private class Tire
        {
            public Transform wheel;
            public WheelCollider collider;
        }

        [Header("References")]
        [SerializeField] AudioSource m_EngineSource = default;
        [SerializeField] AudioClip m_HonkClip = default;

        [Header("Engine Audio")]
        [SerializeField, Range(0.01f, 1f)] float m_MinEnginePitch = 0.1f;
        [SerializeField] float m_PowEnginePitch = 0.1f;

        [Header("Config")]
        [SerializeField] uint m_MaxSteerAngle = 45;
        [SerializeField] uint m_MotorTorque = 1200;
        [SerializeField] uint m_BrakeTorque = 1200;
        [SerializeField] Tire[] m_Tires = default;

        private Rigidbody body;
        private AudioSource source;
        private Quaternion offset;
        private float autoBrakeInput = 1f;

        private const float MAX_SPEED = 300f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            source = GetComponent<AudioSource>();
        }

        private void Start() => offset = transform.localRotation;

        private void Update()
        {
            if (!this.IsOwner())
                return;

            float steeringInput = Input.GetAxis("Horizontal");
            float throttleInput = Mathf.Clamp01(Input.GetAxis("Vertical"));
            float brakeInput = Mathf.Clamp01(Mathf.Abs(Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 0f)) + autoBrakeInput);
            float handbrakeInput = Mathf.Clamp01(Keyboard.current[Key.Space].ReadValue() + autoBrakeInput);

            if (Input.GetKeyDown(KeyCode.H))
            {
                source.PlayOneShot(m_HonkClip, volumeScale: 1f);
                HonkServerRpc();
            }

            if (Keyboard.current[Key.Space].wasPressedThisFrame || throttleInput > Mathf.Epsilon || brakeInput > Mathf.Epsilon)
                autoBrakeInput = 0f;

            var forwardVelocity = Vector3.Dot(transform.forward, body.velocity);

            const float REVERSE_THRESHOLD = 1f;

            float speed = body.velocity.magnitude * 3.6f;

            var enginePitch = Mathf.Pow(speed, m_PowEnginePitch) / MAX_SPEED;

            if (enginePitch < m_MinEnginePitch)
                enginePitch = m_MinEnginePitch;

            m_EngineSource.pitch = Mathf.Clamp(enginePitch, 0f, 2.5f);
            m_EngineSource.volume = Mathf.Clamp(throttleInput, 0.5f, 1f);


            if (forwardVelocity < REVERSE_THRESHOLD && brakeInput > Mathf.Epsilon)
            {
                throttleInput = -brakeInput;
                brakeInput = 0f;
            }

            float steerAngle = m_MaxSteerAngle * steeringInput;
            float motorTorque = m_MotorTorque * throttleInput;
            float brakeTorque = m_BrakeTorque * brakeInput;

            m_Tires[0].collider.steerAngle = steerAngle;
            m_Tires[1].collider.steerAngle = steerAngle;

            for (int i = 0; i < m_Tires.Length; i++)
                HandleTire(m_Tires[i]);

            void HandleTire(Tire tire)
            {
                tire.collider.motorTorque = motorTorque;
                tire.collider.brakeTorque = brakeTorque + (handbrakeInput * m_BrakeTorque);

                tire.collider.GetWorldPose(out var pos, out var rot);
                tire.wheel.position = pos;
                tire.wheel.rotation = rot;
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)] // Unreliable means if delivery missed, it should skip and not retry again
        private void HonkServerRpc() => HonkClientRpc(); // Calls to all connected clients!

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void HonkClientRpc()
        {
            if (this.IsOwner())
                return; // We already honk on this client! RUNS ON OTHER CLIENTS INSTEAD!

            source.PlayOneShot(m_HonkClip, volumeScale: 1f);
        }
    }
}