// (c)2011 Unity Park. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	[CustomEditor(typeof(uLink.NetworkView))]
	public class NetworkViewEditor : Editor
	{
		private static readonly string[] STATESYNC_NAMES =
		{
			"Off",
			"Unreliable",
			"Reliable",
			"Reliable Delta Compressed",
		};

		private static readonly string[] SECURABLE_NAMES =
		{
			"None",
			"Only RPCs",
			"Only State Synchronization",
			"Both",
		};

		private static readonly string[] VIEWIDSCENE_NAMES =
		{
			"Unassigned",
			"Manual",
		};

		private static readonly string[] VIEWIDPREFAB_NAMES =
		{
			"Allocated",
			"Manual",
		};

		private static readonly string[] VIEWIDCHILD_NAMES =
		{
			"Inherited",
		};

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

		public bool showReceiverList;
		public bool showChildren;

		public NetworkViewEditor()
		{
			showReceiverList = EditorPrefs.GetBool("NetworkViewEditor.showReceiverList", true);
			showChildren = EditorPrefs.GetBool("NetworkViewEditor.showChildren", true);
		}

		public override void OnInspectorGUI()
		{
			var target = base.target as uLink.NetworkView;
			if (target == null) return;

			bool isPrefab = EditorUtility.IsPersistent(target);
			bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;

			if (!isPlaying && target._manualViewID == -1)
			{
				target._manualViewID = isPrefab ? 0 : GetUnassignedManualViewID(target);
				EditorUtility.SetDirty(target);
			}

			EditorGUIUtility.LookLikeInspector();
			GUILayout.Space(1);

			{
				int stateSyncIndex = (int)target.stateSynchronization;
				if (stateSyncIndex > 0)
				{
					stateSyncIndex--;
					if (stateSyncIndex > 2) stateSyncIndex--;
				}

				stateSyncIndex = EditorGUILayout.Popup("State Synchronization", stateSyncIndex, STATESYNC_NAMES);
				if (GUI.changed) EditorUtility.SetDirty(target);

				if (stateSyncIndex > 0)
				{
					stateSyncIndex++;
					if (stateSyncIndex > 2) stateSyncIndex++;
				}

				target.stateSynchronization = (uLink.NetworkStateSynchronization)stateSyncIndex;
			}

			target.securable = (NetworkSecurable)EditorGUILayout.Popup("Securable", (int)target.securable, SECURABLE_NAMES);
			if (GUI.changed) EditorUtility.SetDirty(target);

			target.rpcReceiver = (RPCReceiver)EditorGUILayout.Popup("RPC Receiver", (int)target.rpcReceiver, RPCRECEIVER_NAMES);
			if (GUI.changed) EditorUtility.SetDirty(target);

			switch (target.rpcReceiver)
			{
				case RPCReceiver.GameObjects:
					if (target.rpcReceiverGameObjects == null) target.rpcReceiverGameObjects = new GameObject[0];

					showReceiverList = EditorGUILayout.Foldout(showReceiverList, "GameObjects (" + target.rpcReceiverGameObjects.Length + ")");
					if (GUI.changed) EditorPrefs.SetBool("NetworkViewEditor.showReceiverList", showReceiverList);

					if (showReceiverList)
					{
						target.rpcReceiverGameObjects = Utility.ArrayField(true, target.rpcReceiverGameObjects, true);
						if (GUI.changed) EditorUtility.SetDirty(target);
					}

					break;
			}

			Utility.PropertyField(target, "observed");
			if (GUI.changed) EditorUtility.SetDirty(target); // might not be necessary but just in case

			if (isPlaying && !isPrefab)
			{
				GUI.enabled = false;
				EditorGUILayout.LabelField("View ID", target.viewID.id.ToString());
				GUI.enabled = true;
			}
			else if (target.parent != null)
			{
				GUI.enabled = false;
				EditorGUILayout.Popup("View ID", 0, VIEWIDCHILD_NAMES);
				GUI.enabled = true;
			}
			else
			{
				var viewIDNames = isPrefab ? VIEWIDPREFAB_NAMES : VIEWIDSCENE_NAMES;
				int viewIDType = (target._manualViewID == 0) ? 0 : 1;

				if (EditorGUILayout.Popup("View ID", viewIDType, viewIDNames) == 1)
				{
					int oldManualViewID = target._manualViewID;

					EditorGUI.indentLevel++;
					target._manualViewID = Mathf.Max(1, EditorGUILayout.IntField("Manual ID", target._manualViewID));
					if (GUI.changed) EditorUtility.SetDirty(target);

					EditorGUI.indentLevel--;

					if (!isPrefab)
					{
						var other = FindByManualViewID(target._manualViewID, target);
						if (other != null)
						{
							EditorGUILayout.HelpBox("ViewID already assigned to " + other.ToHierarchyString() + " in this scene!", MessageType.Error);
						}

						int unassignedID = GetUnassignedManualViewID(target);
						if (oldManualViewID == 0)
						{
							target._manualViewID = unassignedID;
							EditorUtility.SetDirty(target);
						}
						else if (unassignedID != target._manualViewID)
						{
							EditorGUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							if (GUILayout.Button(new GUIContent("Get Available ID", "Assign the lowset unassigned manual view ID available within this scene"), GUILayout.Width(110)))
							{
								target._manualViewID = unassignedID;
								EditorUtility.SetDirty(target);
							}
							GUILayout.Space(5);
							EditorGUILayout.EndHorizontal();
						}
					}
					else
					{
						EditorGUILayout.HelpBox("If prefab will have multiple instances, set ViewID to Allocated instead.", MessageType.Info);
					}
				}
				else
				{
					target._manualViewID = 0;
					EditorUtility.SetDirty(target);

					if (!isPrefab)
					{
						EditorGUILayout.HelpBox("Before the NetwokView can be used, ViewID must be assigned.", MessageType.Info);
					}
				}
			}

			GUI.enabled = false;
			if (target.childCount > 0)
			{
				showChildren = EditorGUILayout.Foldout(showChildren, "Children (" + target.childCount + ")");
				if (GUI.changed) EditorPrefs.SetBool("NetworkViewEditor.showChildren", showChildren);

				if (showChildren)
				{
					EditorGUI.indentLevel++;
					for (int i = 0; i < target.childCount; i++)
					{
						EditorGUILayout.ObjectField(" Index " + i, target.GetChild(i), typeof(uLink.NetworkView), true);
					}
					EditorGUI.indentLevel--;
				}
			}
			else if (target.parent != null)
			{
				EditorGUILayout.ObjectField("Parent", target.parent, typeof(uLink.NetworkView), true);
				EditorGUILayout.LabelField("Child Index", target.childIndex.ToString());
			}
			GUI.enabled = true;
		}

		private static int GetUnassignedManualViewID(uLink.NetworkView exclude)
		{
			var all = Resources.FindObjectsOfTypeAll(typeof(uLink.NetworkView)) as uLink.NetworkView[];
			var used = new System.Collections.Generic.HashSet<int>();

			foreach (var nv in all)
			{
				if (nv != exclude && nv._manualViewID != 0 && EditorUtility.GetPrefabType(nv) != PrefabType.Prefab)
				{
					used.Add(nv._manualViewID);
				}
			}

			int unusedID = 1;

			while (used.Contains(unusedID)) unusedID++;

			return unusedID;
		}

		private static uLink.NetworkView FindByManualViewID(int manualID, uLink.NetworkView exclude)
		{
			var all = Resources.FindObjectsOfTypeAll(typeof(uLink.NetworkView)) as uLink.NetworkView[];

			foreach (var nv in all)
			{
				if (nv != exclude && nv._manualViewID == manualID && EditorUtility.GetPrefabType(nv) != PrefabType.Prefab)
				{
					return nv;
				}
			}

			return null;
		}
	}
}
