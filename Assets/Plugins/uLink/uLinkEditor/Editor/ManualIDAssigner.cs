// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	public class ManualIDAssigner : EditorWindow
	{
		private enum UniqueMode
		{
			OnlyWithinEachSelectedScene,
			AcrossAllSelectedScenes,
		}

		private static readonly string[] UNIQUEMODE_NAMES = new string[]
		{
			"Make Unique Only Within Each Selected Scene",
			"Make Unique Across All Selected Scenes",
		};

		[Serializable]
		private class Item
		{
			[SerializeField]
			private string _path;

			[SerializeField]
			private int _indent;

			[SerializeField]
			private bool _selected;

			public Item()
			{ }

			public Item(string path)
			{
				_path = path;

				_indent = Utility.CountOccurencesOfChar(path, Path.DirectorySeparatorChar);

				string key = String.Concat("ManualIDAssigner.item:", Application.dataPath, ":", path);
				_selected = EditorPrefs.GetBool(key, false);
			}

			public string path
			{
				get
				{
					return _path;
				}
			}

			public int indent
			{
				get
				{
					return _indent;
				}
			}

			public bool selected
			{
				get
				{
					return _selected;
				}
				set
				{
					_selected = value;
					string key = String.Concat("ManualIDAssigner.item:", Application.dataPath, ":", path);
					EditorPrefs.SetBool(key, value);
				}
			}
		}

		[SerializeField]
		private int _uniqueMode;

		[SerializeField]
		private Item[] _items;

		[SerializeField]
		private int _focusRow = -1;

		[SerializeField]
		private Vector2 _scrollPosition;

		private const int LIST_ID = 42;

		[MenuItem("uLink/Assign Unique Manual View IDs...", false, 201)]
		public static void OnMenu()
		{
			var window = GetWindow<ManualIDAssigner>(true, "Assign Unique Manual View IDs", true);
			window.Init();
			window.Repaint();
		}

		public ManualIDAssigner()
		{
			position = new Rect(100f, 100f, 550f, 300f);
			minSize = new Vector2(550f, 200f);
		}

		protected void OnGUI()
		{
			GUILayout.Label("Scenes to Assign", Utility.GetStyles().title);
			GUILayout.Space(1);
			GUILayout.BeginVertical(Utility.GetStyles().Box, GUILayout.ExpandHeight(true));

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			if (_items == null)
			{
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Sorry, something went wrong");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
			}
			else if (_items.Length == 0)
			{
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("No scenes where found");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
			}
			else
			{
				DrawTree();
			}

			EditorGUILayout.EndScrollView();

			GUILayout.EndVertical();

			if (_items == null) GUI.enabled = false;

			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(10f);

			if (GUILayout.Button("All", GUILayout.Width(50f)))
			{
				foreach (var item in _items)
				{
					item.selected = true;
				}
			}

			if (GUILayout.Button("None", GUILayout.Width(50f)))
			{
				foreach (var item in _items)
				{
					item.selected = false;
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.Space(20);

			GUILayout.BeginVertical();
			GUILayout.Space(3);
			EditorGUIUtility.LookLikeInspector();
			_uniqueMode = EditorGUILayout.Popup(_uniqueMode, UNIQUEMODE_NAMES, GUILayout.Width(300));
			if (GUI.changed) EditorPrefs.SetInt("uLinkManualIDAssigner.uniqueMode", _uniqueMode);
			EditorGUIUtility.LookLikeControls();
			GUILayout.EndVertical();

			if (GUILayout.Button("Check"))
			{
				CheckSelectedScenes();
				GUIUtility.ExitGUI();
				return;
			}

			GUILayout.Space(3);

			if (GUILayout.Button("Assign"))
			{
				AssignSelectedScenes();
				GUIUtility.ExitGUI();
				return;
			}

			GUILayout.Space(10f);
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);

			if (_items == null) GUI.enabled = true;
		}

		private void DrawTree()
		{
			bool isRepaint = (Event.current.type == EventType.Repaint);

			for (int row = 0; row < _items.Length; row++)
			{
				var item = _items[row];

				var rect = GUILayoutUtility.GetRect(24, 24, GUILayout.ExpandWidth(true));
				var position = rect;
				position = new Rect(position.x + 1, position.y, position.width - 2, position.height);

				float indent = item.indent * 15 + 5;
				bool isEven = ((row % 2) == 0);

				if (isRepaint)
				{
					var style = isEven ? Utility.GetStyles().ConsoleEntryBackEven : Utility.GetStyles().ConsoleEntryBackOdd;
					style.Draw(position, false, false, _focusRow == row, false);
				}

				float height = rect.height * 0.5f - 8;
				float y = rect.y + height;
				bool wasSelected = item.selected;

				position = new Rect(position.x + 3, position.y, position.width - 3, position.height);
				bool selected = GUI.Toggle(position, wasSelected, string.Empty, GUIStyle.none);
				position.y = y;
				position.height -= height;

				GUI.Toggle(position, selected, string.Empty);

				position.y = rect.y;
				position.height += height;

				if (wasSelected != selected)
				{
					item.selected = selected;

					_focusRow = row;
					GUIUtility.keyboardControl = LIST_ID;
				}

				if (isRepaint)
				{
					var position2 = new Rect(position.x + indent, y, 16, 16);
					GUI.DrawTexture(position2, AssetDatabase.GetCachedIcon(item.path));
				}

				position = new Rect(position.x + 20 + indent, rect.y + 3, position.width - 20 - indent, position.height);
				GUI.Label(position, item.path);
			}

			if (_focusRow != -1 && GUIUtility.keyboardControl == LIST_ID && Event.current.type == EventType.KeyDown)
			{
				bool useEvent = true;

				switch (Event.current.keyCode)
				{
					case KeyCode.Space:
						var item = _items[_focusRow];
						item.selected = !item.selected;
						break;

					case KeyCode.DownArrow:
						if (_focusRow < _items.Length - 1) _focusRow++;
						break;

					case KeyCode.UpArrow:
						if (_focusRow > 0) _focusRow--;
						break;

					default:
						useEvent = false;
						break;
				}

				if (useEvent) Event.current.Use();
			}
		}

		private void Init()
		{
			_uniqueMode = EditorPrefs.GetInt("uLinkManualIDAssigner.uniqueMode", (int)UniqueMode.OnlyWithinEachSelectedScene);

			var files = Utility.GetAllAssetFiles("*.unity");
			if (files == null) return;

			_items = new Item[files.Length];

			for (int i = 0; i < files.Length; i++)
			{
				_items[i] = new Item(files[i]);
			}
		}

		private bool CheckIfAnySelection(string message)
		{
			foreach (var item in _items)
			{
				if (item.selected) return true;
			}

			ShowNotification(new GUIContent(message));
			return false;
		}

		private void AssignSelectedScenes()
		{
			if (!CheckIfAnySelection("Please select scene(s) to assign.")) return;

			EditorApplication.SaveCurrentSceneIfUserWantsTo();
			string oldScene = EditorApplication.currentScene;

			int lastUniqueID = 0;
			int maxID = 0;

			float progress = 0;
			float deltaProgress = 1f / _items.Length;

			foreach (var item in _items)
			{
				EditorUtility.DisplayProgressBar("Assigning Unique Manual View IDs...", item.path, progress);
				progress += deltaProgress;

				if (item.selected)
				{
					lastUniqueID = AssignScene(item.path, lastUniqueID);
					if (maxID < lastUniqueID) maxID = lastUniqueID;
				}
			}

			EditorUtility.ClearProgressBar();
			if (EditorApplication.currentScene != oldScene) EditorApplication.OpenScene(oldScene);
		}

		private int AssignScene(string filename, int lastUniqueID)
		{
			if (EditorApplication.currentScene != filename) EditorApplication.OpenScene(filename);
			if (_uniqueMode == (int)UniqueMode.OnlyWithinEachSelectedScene) lastUniqueID = 0;

			var all = Resources.FindObjectsOfTypeAll(typeof(uLink.NetworkView)) as uLink.NetworkView[];
			var sorted = new SortedDictionary<string, List<uLink.NetworkView>>();

			foreach (var nv in all)
			{
				var prefabType = EditorUtility.GetPrefabType(nv);
				if (prefabType == PrefabType.Prefab) continue;

				string name = Utility.GetHierarchyName(nv.transform) + Utility.GetComponentIndex(nv);

				List<uLink.NetworkView> list;
				if (!sorted.TryGetValue(name, out list))
				{
					list = new List<uLink.NetworkView>();
					sorted.Add(name, list);
				}

				list.Add(nv);
			}

			foreach (var pair in sorted)
			{
				foreach (var nv in pair.Value)
				{
					nv._manualViewID = ++lastUniqueID;
					EditorUtility.SetDirty(nv);
				}
			}

			EditorApplication.SaveScene(filename);

			return lastUniqueID;
		}

		private void CheckSelectedScenes()
		{
			if (!CheckIfAnySelection("Please select scene(s) to check.")) return;

			EditorApplication.SaveCurrentSceneIfUserWantsTo();
			string oldScene = EditorApplication.currentScene;

			var indexed = new System.Collections.Generic.Dictionary<int, string>();
			int maxID = 0;

			float progress = 0;
			float deltaProgress = 1f / _items.Length;

			foreach (var item in _items)
			{
				EditorUtility.DisplayProgressBar("Checking Manual View IDs...", item.path, progress);
				progress += deltaProgress;

				if (item.selected)
				{
					int sceneMaxID = CheckScene(item.path, indexed);
					if (maxID < sceneMaxID) maxID = sceneMaxID;
				}
			}

			EditorUtility.ClearProgressBar();
			if (EditorApplication.currentScene != oldScene) EditorApplication.OpenScene(oldScene);
		}

		private int CheckScene(string filename, System.Collections.Generic.Dictionary<int, string> indexed)
		{
			if (EditorApplication.currentScene != filename) EditorApplication.OpenScene(filename);
			if (_uniqueMode == (int)UniqueMode.OnlyWithinEachSelectedScene) indexed.Clear();

			var all = Resources.FindObjectsOfTypeAll(typeof(uLink.NetworkView)) as uLink.NetworkView[];

			int maxID = 0;

			foreach (var nv in all)
			{
				PrefabType prefabType = EditorUtility.GetPrefabType(nv);
				if (prefabType == PrefabType.Prefab) continue;

				if (maxID < nv._manualViewID) maxID = nv._manualViewID;

				string other;
				if (indexed.TryGetValue(nv._manualViewID, out other))
				{
					Debug.LogWarning("In scene " + filename + ": manual ViewID " + nv._manualViewID + " in " + nv + " is already assigned to " + other, nv);
				}
				else
				{
					string name = nv.ToHierarchyString() + "(Scene: " + filename + ")";
					indexed.Add(nv._manualViewID, name);
				}
			}

			return maxID;
		}
	}
}
