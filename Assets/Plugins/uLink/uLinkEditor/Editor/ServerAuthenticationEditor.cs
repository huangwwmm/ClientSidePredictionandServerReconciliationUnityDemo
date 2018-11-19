// (c)2011 Unity Park. All Rights Reserved.

using System;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	[CustomEditor(typeof(ServerAuthentication))]
	public class ServerAuthenticationEditor : Editor
	{
		internal const string NOKEYS_MESSAGE = "Set key to guarantee that the client is communicaing with a trusted server.";
		internal const string BOTHKEYS_MESSAGE = "Private and public key must be separated into server and client, respectively.";

		public override void OnInspectorGUI()
		{
			var target = base.target as ServerAuthentication;
			if (target == null) return;

			var textFieldStyle = new GUIStyle(EditorStyles.miniTextField);
			textFieldStyle.fixedHeight = 0;
			textFieldStyle.fixedWidth = 0;
			textFieldStyle.stretchWidth = true;

			GUILayout.Label("To verify server, set key to assign on Awake:", EditorStyles.wordWrappedLabel);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(15);
			GUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Server (Private Key)", GUILayout.Width(125));
			target.privateKey = EditorGUILayout.TextField(target.privateKey, textFieldStyle);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Client (Public Key)", GUILayout.Width(125));
			target.publicKey = EditorGUILayout.TextField(target.publicKey, textFieldStyle);
			EditorGUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.Space(15);
			EditorGUILayout.EndHorizontal();

			if (String.IsNullOrEmpty(target.privateKey) && String.IsNullOrEmpty(target.publicKey))
			{
				EditorGUILayout.HelpBox(NOKEYS_MESSAGE, MessageType.Info);
			}
			else if (!String.IsNullOrEmpty(target.privateKey) && !String.IsNullOrEmpty(target.publicKey))
			{
				EditorGUILayout.HelpBox(BOTHKEYS_MESSAGE, MessageType.Warning);
			}

			GUILayout.Space(2);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(10);

			target.initializeSecurity = GUILayout.Toggle(target.initializeSecurity, "Initialize Security");
			if (GUI.changed) EditorUtility.SetDirty(target);

			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Set Key", GUILayout.Width(100)))
			{
				ServerAuthenticationGenerator.Open(target);
			}

			GUILayout.Space(18);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);
		}
	}
}
