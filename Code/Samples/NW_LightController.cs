using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

using static Network.Framework.NW_NetworkExtensions;

namespace Network.Samples
{
    [AddComponentMenu("Network/Framework/Light Controller"), DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class NW_LightController : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] AnimationCurve m_LightIntensity = new
        (
            new Keyframe(time: 0f, value: 0f),
            new Keyframe(time: 0.25f, value: 28855),
            new Keyframe(time: 0.5f, value: 115422),
            new Keyframe(time: 0.75f, value: 28855),
            new Keyframe(time: 1f, value: 0f)
        );
        [SerializeField] float m_Duration = 1f;

        private float time;
        private new Light light = null;
        private HDAdditionalLightData lightData;

        private void Awake()
        {
            light = GetComponent<Light>();
            lightData = light.GetComponent<HDAdditionalLightData>();
        }

        private void Update()
        {
            if (!IsOwnedByServer)
                return;

            if (Application.isPlaying)
                time = (time + (NetworkManager.ServerTime.FixedDeltaTime / m_Duration)) % 24f;

            UpdateLighting(time / 24f);
        }

        private void UpdateLighting(float value)
        {
            if (light != null)
            {
                lightData.intensity = m_LightIntensity.Evaluate(value);
                light.transform.localRotation = Quaternion.Euler
                (
                    new Vector3((value * 360f) - 90f, -170f, 0f)
                );
            }
        }

        private void OnTimeChanged(float oldTime, float newTime)
        {
            /// Client can't write to NetworkVariables

            if (!IsClient)
                return;

            UpdateLighting(time / 24f);
        }
    }
}