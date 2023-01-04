using Network.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] SceneReference m_SceneReference = default;
    [SerializeField] bool m_LoadOnAwake = true;
    [SerializeField] bool m_LoadWithNetwork = false;
    [SerializeField] LoadSceneMode m_Mode = LoadSceneMode.Single;

    private void Awake()
    {
        if (!m_LoadOnAwake)
            return;

        if (m_LoadWithNetwork)
            m_SceneReference.TryLoadNetworkScene(m_Mode);
        else
            m_SceneReference.TryLoadRegularScene(m_Mode);
    }

    public void LoadScene()
    {
        if (m_LoadWithNetwork)
            m_SceneReference.TryLoadNetworkScene(m_Mode);
        else
            m_SceneReference.TryLoadRegularScene(m_Mode);
    }
}
