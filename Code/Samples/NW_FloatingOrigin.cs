using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network.Samples
{
    [AddComponentMenu("Network/Samples/Floating Origin"), DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class NW_FloatingOrigin : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("When camera position achieve threshold, then reset origins position")] uint m_Threshold = 5000;

        private void LateUpdate()
        {
            float3 cameraPos = gameObject.transform.position;
            cameraPos.y = 0f;

            if (math.length(cameraPos) > m_Threshold)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var objects = SceneManager.GetSceneAt(i).GetRootGameObjects();

                    for (int j = 0; j < objects.Length; j++)
                        objects[j].transform.position -= (Vector3)cameraPos;
                }

                //float3 originDelta = float3.zero - cameraPos;
            }
        }
    } 
}
