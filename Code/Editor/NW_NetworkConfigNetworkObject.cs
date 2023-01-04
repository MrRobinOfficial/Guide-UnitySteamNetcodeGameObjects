using Network.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace Network.Editor
{
    public class NW_NetworkConfigNetworkObject : EditorWindow
    {
        [MenuItem("Tools/Network/Config NetworkObject")]
        private static void Init()
        {
            var window = EditorWindow.GetWindow<NW_NetworkConfigNetworkObject>(title: "Config NetworkObject");
            window.titleContent.text = "Config NetworkObject";
            window.Show();
        }

        private void OnEnable() => Selection.selectionChanged = Repaint;

        private void OnDisable() => Selection.selectionChanged = null;

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.button);
            style.wordWrap = true;

            var obj = Selection.activeGameObject;

            if (!obj)
                return;

            EditorGUILayout.LabelField($"Currnet Object: {obj.name}", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            HandleComponent<NW_NetworkCompatibility>();
            HandleComponent<NetworkObject>();
            HandleComponent<NetworkAnimator>();
            HandleComponent<NetworkTransform>();
            HandleComponent<NetworkRigidbody2D>();
            HandleComponent<NetworkRigidbody>();

            void HandleComponent<T>() where T : Component
            {
                var hasT = obj.TryGetComponent(out T value);
                var name = typeof(T).Name;

                var color = style.normal.textColor;

                if (typeof(T) == typeof(NetworkAnimator) && obj.GetComponent<Animator>())
                    style.normal.textColor = Color.green;

                if (typeof(T) == typeof(NetworkRigidbody) && obj.GetComponent<Rigidbody>())
                    style.normal.textColor = Color.green;

                if (typeof(T) == typeof(NetworkRigidbody2D) && obj.GetComponent<Rigidbody2D>())
                    style.normal.textColor = Color.green;

                if (!hasT && GUILayout.Button($"Add {name} Component", style))
                    value = obj.AddComponent<T>();
                else if (hasT && GUILayout.Button($"Remove {name} Component", style))
                    DestroyImmediate(value);

                style.normal.textColor = color;
            }
        }
    }
}