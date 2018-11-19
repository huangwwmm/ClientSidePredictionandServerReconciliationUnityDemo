// (c)2011 Unity Park. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	[CustomEditor(typeof(NetworkP2P))]
	public class NetworkP2PEditor : Editor
	{
		private static readonly string[] RPCRECEIVER_NAMES =
		{
			"Off",
			"Only Observed Component",
			"This GameObject",
			"This GameObject And Children",
			"Root GameObject And Children",
			"All Active GameObjects",
			"GameObjects",
		};

		private static readonly string[] NETWORKSTATUS_NAMES =
		{
			"Disconnected",
			"Connecting",
			"Connected",
			"Disconnecting",
		};

		public bool showPeerData;
		public bool showReceiverList;
		public bool showConnections;

		public NetworkP2PEditor()
		{
			showPeerData = EditorPrefs.GetBool("NetworkP2PEditor.showPeerData", true);
			showReceiverList = EditorPrefs.GetBool("NetworkP2PEditor.showReceiverList", true);
			showConnections = EditorPrefs.GetBool("NetworkP2PEditor.showConnections", true);
		}

		public override void OnInspectorGUI()
		{
			var target = base.target as NetworkP2P;
			if (target == null) return;

			bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode && !EditorUtility.IsPersistent(target);

			if (!isPlaying && target.listenPort == -1)
			{
				target.listenPort = 0;
				EditorUtility.SetDirty(target);
			}

			EditorGUIUtility.LookLikeInspector();
			GUILayout.Space(1);

			showPeerData = EditorGUILayout.Foldout(showPeerData, "Peer Data");
			if (GUI.changed) EditorPrefs.SetBool("NetworkP2PEditor.showPeerData", showPeerData);

			if (showPeerData)
			{
				EditorGUI.indentLevel++;
				target.peerType = EditorGUILayout.TextField("Type", target.peerType);
				target.peerName = EditorGUILayout.TextField("Name", target.peerName);
				target.comment = EditorGUILayout.TextField("Comment", target.comment);
				EditorGUI.indentLevel--;
			}

			target.incomingPassword = EditorGUILayout.TextField("Incoming Password", target.incomingPassword);

			if (isPlaying) GUI.enabled = false;
			target.listenPort = Mathf.Clamp(EditorGUILayout.IntField("Listen Port", target.listenPort), -1, ushort.MaxValue);
			target.maxConnections = Mathf.Max(EditorGUILayout.IntField("Max Connections", target.maxConnections), 1);
			if (isPlaying) GUI.enabled = true;

			target.rpcReceiver = (RPCReceiver)EditorGUILayout.Popup("RPC Receiver", (int)target.rpcReceiver, RPCRECEIVER_NAMES);
			if (GUI.changed) EditorUtility.SetDirty(target);

			switch (target.rpcReceiver)
			{
				case RPCReceiver.OnlyObservedComponent:
					EditorGUI.indentLevel++;
					Utility.PropertyField(target, "observed");
					if (GUI.changed) EditorUtility.SetDirty(target); // might not be necessary but just in case
					EditorGUI.indentLevel--;
					break;

				case RPCReceiver.GameObjects:
					if (target.rpcReceiverGameObjects == null) target.rpcReceiverGameObjects = new GameObject[0];

					showReceiverList = EditorGUILayout.Foldout(showReceiverList, "GameObjects (" + target.rpcReceiverGameObjects.Length + ")");
					if (GUI.changed) EditorPrefs.SetBool("NetworkP2PEditor.showReceiverList", showReceiverList);

					if (showReceiverList)
					{
						target.rpcReceiverGameObjects = Utility.ArrayField(true, target.rpcReceiverGameObjects, true);
						if (GUI.changed) EditorUtility.SetDirty(target);
					}

					break;
			}

			if (isPlaying)
			{
				GUI.enabled = false;

				var connections = target.allConnections;

				showConnections = EditorGUILayout.Foldout(showConnections, "All Connections (" + connections.Length + ")");
				if (GUI.changed) EditorPrefs.SetBool("NetworkP2PEditor.showConnections", showConnections);

				if (showConnections)
				{
					EditorGUI.indentLevel++;

					if (connections.Length > 0)
					{
						foreach (var connection in connections)
						{
							EditorGUILayout.TextField(connection.Key.endpoint.ToString(), NETWORKSTATUS_NAMES[(byte)connection.Value]);
						}
					}
					else
					{
						EditorGUILayout.TextField("None", "");
					}
					
					EditorGUI.indentLevel--;
				}

				GUI.enabled = true;
			}
		}
	}
}
