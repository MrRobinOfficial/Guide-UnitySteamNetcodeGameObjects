using Network.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using static Network.Framework.NW_NetworkExtensions;

namespace Network.Editor
{
    [CustomEditor(typeof(NW_ServerManager))]
    public class NW_ServerManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (NetworkManager.Singleton == null)
            {
                base.OnInspectorGUI();
                return;
            }

            var data = (NW_ServerManager)target;

            EditorGUI.BeginDisabledGroup(disabled: true);

            EditorGUILayout.LabelField($"Current Members [{data.MemberLookup.Count}]");

            EditorGUI.indentLevel++;

            foreach (var member in data.MemberLookup)
                DrawMember(member);

            EditorGUI.indentLevel--;

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        private void DrawMember(KeyValuePair<FixedString64Bytes, MemberData> member)
        {
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.wordWrap = true;
            var data = member.Value;

            EditorGUILayout.LabelField($"Key/Id: {member.Key} | {data.ClientId}", labelStyle);
            EditorGUILayout.LabelField($"Display Name: {data.DisplayName}", labelStyle);

            EditorGUILayout.Space();
        }
    }
}