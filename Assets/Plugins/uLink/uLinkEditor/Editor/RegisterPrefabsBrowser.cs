// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	public class RegisterPrefabsBrowser : EditorWindow
	{
		[Serializable]
		private class Item
		{
			[SerializeField]
			private string _path;

			[SerializeField]
			private int _indent;

			[SerializeField]
			private string _name;

			[SerializeField]
			private GameObject _gameObject;

			[SerializeField]
			public bool selected;

			public Item()
			{ }

			public Item(string path, GameObject gameObject, uLink.RegisterPrefabs target)
			{
				_path = path;

				_indent = Utility.CountOccurencesOfChar(path, Path.DirectorySeparatorChar);

				_name = path + (gameObject != null ? " (" + gameObject.name + ")" : "");

				_gameObject = gameObject;
				selected = target.prefabs.Contains(gameObject);
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

			public string name
			{
				get
				{
					return _name;
				}
			}

			public GameObject gameObject
			{
				get
				{
					return _gameObject;
				}
			}

			public void UpdateSelection(uLink.RegisterPrefabs target)
			{
				if (!selected)
				{
					target.prefabs.RemoveAll(_gameObject);
					EditorUtility.SetDirty(target);
				}
				else if (_gameObject != null && !target.prefabs.Contains(_gameObject))
				{
					target.prefabs.Add(_gameObject);
					EditorUtility.SetDirty(target);
				}
			}
		}

		[SerializeField]
		private Item[] _items;

		[SerializeField]
		private uLink.RegisterPrefabs _target;

		[SerializeField]
		private int _focusRow = -1;

		[SerializeField]
		private Vector2 _scrollPosition;

		private const int LIST_ID = 42;

		internal static void Open(uLink.RegisterPrefabs target)
		{
			var window = GetWindow<RegisterPrefabsBrowser>(true, "Select uLink prefabs to be registered");
			window.Init(target);
			window.Repaint();
		}

		public RegisterPrefabsBrowser()
		{
			position = new Rect(100f, 100f, 400f, 300f);
			minSize = new Vector2(400f, 200f);
		}

		protected void OnGUI()
		{
			if (_target == null)
			{
				Close();
				return;
			}

			GUILayout.Label("Prefabs to Register", Utility.GetStyles().title);
			GUILayout.Space(1);
			GUILayout.BeginVertical(Utility.GetStyles().Box, GUILayout.ExpandHeight(true));

			var duplicates = Utility.FindDuplicatePrefabs(_target.prefabs);

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
				GUILayout.Label("No prefabs containing uLink NetworkView where found");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
			}
			else
			{
				DrawTree(duplicates);
			}

			EditorGUILayout.EndScrollView();

			GUILayout.EndVertical();

			if (_items == null) GUI.enabled = false;

			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(10f);

			if (GUILayout.Button("All", GUILayout.Width(50f)))
			{
				_target.prefabs.Clear();

				foreach (var item in _items)
				{
					item.selected = true;
					if (item.gameObject != null) _target.prefabs.Add(item.gameObject);
				}

				EditorUtility.SetDirty(_target);
			}

			if (GUILayout.Button("None", GUILayout.Width(50f)))
			{
				_target.prefabs.Clear();

				foreach (var item in _items)
				{
					item.selected = false;
				}

				EditorUtility.SetDirty(_target);
			}

			if (duplicates.Count > 0)
			{
				GUILayout.Space(20);
				EditorGUILayout.HelpBox("Tow or more prefabs have identical names. Rename or deselect to avoid conflict.", MessageType.Error);
				GUILayout.Space(18);
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Close"))
			{
				Close();
			}

			GUILayout.Space(10f);
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);

			if (_items == null) GUI.enabled = true;
		}

		private void DrawTree(System.Collections.Generic.HashSet<string> duplicates)
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

				GUI.color = selected && item.gameObject != null && duplicates.Contains(item.gameObject.name) ? Utility.ERROR_COLOR : Utility.NORMAL_COLOR;

				if (wasSelected != selected)
				{
					item.selected = selected;
					item.UpdateSelection(_target);

					_focusRow = row;
					GUIUtility.keyboardControl = LIST_ID;
				}

				if (isRepaint)
				{
					var position2 = new Rect(position.x + indent, y, 16, 16);
					GUI.DrawTexture(position2, AssetDatabase.GetCachedIcon(item.path));
				}

				position = new Rect(position.x + 20 + indent, rect.y + 3, position.width - 20 - indent, position.height);
				GUI.Label(position, item.name);

				GUI.color = Utility.NORMAL_COLOR;
			}

			if (_focusRow != -1 && GUIUtility.keyboardControl == LIST_ID && Event.current.type == EventType.KeyDown)
			{
				bool useEvent = true;

				switch (Event.current.keyCode)
				{
					case KeyCode.Space:
						var item = _items[_focusRow];
						item.selected = !item.selected;
						item.UpdateSelection(_target);
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

		private void Init(uLink.RegisterPrefabs target)
		{
			_target = target;

			var files = Utility.GetAllAssetFiles("*.prefab");
			if (files == null) return;

			var items = new List<Item>(files.Length);

			foreach (var file in files)
			{
				var gameObject = AssetDatabase.LoadAssetAtPath(file, typeof(GameObject)) as GameObject;

				if (gameObject != null &&
					PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab &&
					Utility.HasComponentInChildren<uLink.NetworkView>(gameObject.transform))
				{
					var item = new Item(file, gameObject, _target);
					items.Add(item);
				}
			}

			_items = items.ToArray();
		}
	}
}
