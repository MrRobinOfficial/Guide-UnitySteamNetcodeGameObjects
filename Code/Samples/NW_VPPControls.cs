using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VehiclePhysics;

// THIS SCRIPT REQUIRES VPP (https://vehiclephysics.com/)

namespace Network.Samples
{
    public class NW_VPPControls : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] AudioClip m_HonkSFX = default;
        [SerializeField] AudioSource m_SFXSource = default;
        [SerializeField] VPVehicleToolkit m_Toolkit = default;

        [Header("Axis")]
        [SerializeField] InputAction m_Steering = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick/x");
        [SerializeField] InputAction m_Throttle = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/rightTrigger");
        [SerializeField] InputAction m_Brake = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftTrigger");
        [SerializeField] InputAction m_Clutch = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftShoulder");
        [SerializeField] InputAction m_Handbrake = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/buttonSouth");

        [Header("Buttons")]
        [SerializeField] InputAction m_Honk = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/h");
        [SerializeField] InputAction m_ShiftUp = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/e");
        [SerializeField] InputAction m_ShiftDown = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/q");
        [SerializeField] InputAction m_Reset = new InputAction(type: InputActionType.PassThrough, binding: "<Keyboard>/r");

        private Rigidbody body;

        private void Awake() => body = GetComponent<Rigidbody>();

        private void OnEnable()
        {
            if (!IsOwner)
                return;

            m_Steering.Enable();
            m_Throttle.Enable();
            m_Brake.Enable();
            m_Clutch.Enable();
            m_Handbrake.Enable();

            m_Reset.Enable();
            m_Honk.Enable();
            m_ShiftUp.Enable();
            m_ShiftDown.Enable();

            m_Reset.performed += Reset_performed;
            m_Honk.performed += Honk_performed;
            m_ShiftUp.performed += ShiftUp_performed;
            m_ShiftDown.performed += ShiftDown_performed;
        }

        private void OnDisable()
        {
            if (!IsOwner)
                return;

            m_Reset.performed -= Reset_performed;
            m_Honk.performed -= Honk_performed;
            m_ShiftUp.performed -= ShiftUp_performed;
            m_ShiftDown.performed -= ShiftDown_performed;

            m_Steering.Disable();
            m_Throttle.Disable();
            m_Brake.Disable();
            m_Clutch.Disable();
            m_Handbrake.Disable();

            m_Reset.Disable();
            m_Honk.Disable();
            m_ShiftUp.Disable();
            m_ShiftDown.Disable();
        }

        private void Update()
        {
            VPVehicleToolkit.SetSteering(m_Toolkit.vehicle, m_Steering.ReadValue<float>());
            VPVehicleToolkit.SetThrottle(m_Toolkit.vehicle, m_Throttle.ReadValue<float>());
            VPVehicleToolkit.SetBrake(m_Toolkit.vehicle, m_Brake.ReadValue<float>());
            VPVehicleToolkit.SetClutch(m_Toolkit.vehicle, m_Clutch.ReadValue<float>());
            VPVehicleToolkit.SetHandbrake(m_Toolkit.vehicle, m_Handbrake.ReadValue<float>());
        }

        private void ShiftUp_performed(InputAction.CallbackContext ctx) => m_Toolkit.ShiftGearUp();

        private void ShiftDown_performed(InputAction.CallbackContext ctx) => m_Toolkit.ShiftGearDown();

        private void Honk_performed(InputAction.CallbackContext ctx) => m_SFXSource.PlayOneShot(m_HonkSFX, volumeScale: 1f);


        private void Reset_performed(InputAction.CallbackContext ctx)
        {
            body.Sleep();

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            transform.position += Vector3.up * 2f;
            transform.localRotation = Quaternion.identity;

            body.WakeUp();
        }
    }
}
