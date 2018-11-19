// (c)2011 Unity Park. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	[CustomEditor(typeof(uLink.RegisterPrefabs))]
	public class RegisterPrefabsEditor : Editor
	{
		internal const string DUPLICATENAME_MESSAGE = "Name is identical to a already selected prefab. You can rename it to avoid conflicts.";
		internal const string ALREADYSELECTED_MESSAGE = "Has already been selected.";
		internal const string NOTPREFABTYPE_MESSAGE = "Game object is not a prefab asset.";
		internal const string NONETWORKVIEW_MESSAGE = "Missing a uLink NetworkView component.";

		public override void OnInspectorGUI()
		{
			var target = base.target as RegisterPrefabs;
			if (target == null) return;

			GUILayout.Label("Select uLink prefabs to register on Awake:");

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(20);
			EditorGUILayout.BeginVertical();

			var prevoiusNames = new System.Collections.Generic.HashSet<string>();

			var newPrefabs = new List<GameObject>();
			foreach (GameObject oldObj in target.prefabs)
			{
				if (oldObj != null)
				{
					bool isAlreadySelected = newPrefabs.Contains(oldObj);
					bool isNotPrefabType = PrefabUtility.GetPrefabType(oldObj) != PrefabType.Prefab;
					bool hasNoNetworkView = !Utility.HasComponentInChildren<uLink.NetworkView>(oldObj.transform);
					bool hasDuplicateName = prevoiusNames.Contains(oldObj.name);

					GUI.backgroundColor = (isAlreadySelected || isNotPrefabType || hasNoNetworkView || hasDuplicateName) ? Utility.ERROR_COLOR : Utility.NORMAL_COLOR;

					var newObj = EditorGUILayout.ObjectField(oldObj, typeof(GameObject), false) as GameObject;
					if (newObj != null)
					{
						newPrefabs.Add(newObj);
						prevoiusNames.Add(newObj.name);
						if (GUI.changed) EditorUtility.SetDirty(target);
					}
					else
					{
						EditorUtility.SetDirty(target);
					}

					GUI.backgroundColor = Utility.NORMAL_COLOR;

					if (isNotPrefabType)
					{
						EditorGUILayout.HelpBox(NOTPREFABTYPE_MESSAGE, MessageType.Error);
					}
					else if (hasNoNetworkView)
					{
						EditorGUILayout.HelpBox(NONETWORKVIEW_MESSAGE, MessageType.Error);
					}
					else if (hasDuplicateName)
					{
						EditorGUILayout.HelpBox(DUPLICATENAME_MESSAGE, MessageType.Error);
					}
					else if (isAlreadySelected)
					{
						EditorGUILayout.HelpBox(ALREADYSELECTED_MESSAGE, MessageType.Error);
					}
					else
					{
						continue;
					}

					EditorGUILayout.Space();
				}
			}

			target.prefabs = newPrefabs;

			GameObject addToList = EditorGUILayout.ObjectField(null, typeof(GameObject), false) as GameObject;
			if (addToList != null)
			{
				target.prefabs.Add(addToList);
				EditorUtility.SetDirty(target);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(10);

			target.replaceIfExists = GUILayout.Toggle(target.replaceIfExists, "Replace if exists");
			if (GUI.changed) EditorUtility.SetDirty(target);

			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Browse", GUILayout.Width(100)))
			{
				RegisterPrefabsBrowser.Open(target);
			}

			GUILayout.Space(18);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);
		}
	}
}
