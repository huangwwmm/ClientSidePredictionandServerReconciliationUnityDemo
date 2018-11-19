// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using uLink;
using Object = UnityEngine.Object;

namespace uLinkEditor
{
	internal static partial class Utility
	{
		public static readonly Color NORMAL_COLOR = Color.white;
		public static readonly Color WARNING_COLOR = new Color(1, 1, 0.5f);
		public static readonly Color ERROR_COLOR = new Color(1, 0.5f, 0.5f);

		private const string PREFS_DIRECTORY = "Assets/Resources";
		private const string PREFS_FILENAME = PREFS_DIRECTORY + "/" + uLink.NetworkPrefs.resourcePath + ".txt";
		
		public static readonly bool isWindows;
		private static readonly StringComparison _pathComparison;

		static Utility()
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					isWindows = true;
					break;
				case PlatformID.Unix:
				case PlatformID.MacOSX:
				case PlatformID.Xbox:
				default:
					isWindows = false;
					break;
			}

			_pathComparison = isWindows ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
		}

		public static bool ArePathsEqual(string pathA, string pathB)
		{
			return String.Compare(
				Path.GetFullPath(pathA).TrimEnd('\\').TrimEnd('/'),
				Path.GetFullPath(pathB).TrimEnd('\\').TrimEnd('/'),
				_pathComparison) == 0;
		}

		private static void SetPrefs()
		{
			ProjectIdentifier.SetPrefs();
			uLink.Network.SetPrefs();
			uLink.MasterServer.SetPrefs();
			uLink.NetworkLog.SetPrefs();
		}

		private static void GetPrefs()
		{
			ProjectIdentifier.GetPrefs();
			uLink.Network.GetPrefs();
			uLink.MasterServer.GetPrefs();
			uLink.NetworkLog.GetPrefs();
		}

		public static void ResetPrefs()
		{
			uLink.NetworkPrefs.DeleteAll();
			GetPrefs();
		}

		public static void ReloadPrefs()
		{
			uLink.NetworkPrefs.Reload();
			GetPrefs();
		}

		public static void SavePrefs()
		{
			SetPrefs();

			string prefs = uLink.NetworkPrefs.ToConfigString();

			if (!Directory.Exists(PREFS_DIRECTORY))
			{
				Directory.CreateDirectory(PREFS_DIRECTORY);
				AssetDatabase.ImportAsset(PREFS_DIRECTORY, ImportAssetOptions.ForceUpdate);
			}
			else if (File.Exists(PREFS_FILENAME) && prefs == File.ReadAllText(PREFS_FILENAME))
			{
				return;
			}

			File.WriteAllText(PREFS_FILENAME, prefs);
			AssetDatabase.ImportAsset(PREFS_FILENAME, ImportAssetOptions.ForceUpdate);
		}

		public static bool HasPrefs()
		{
			return File.Exists(PREFS_FILENAME);
		}

		public static bool IsPrefsDirty()
		{
			SetPrefs();
			string prefs = uLink.NetworkPrefs.ToConfigString();

			return !File.Exists(PREFS_FILENAME) || prefs != File.ReadAllText(PREFS_FILENAME);
		}

		public static bool HelpButton(Rect position, string text)
		{
			EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);
			return GUI.Button(position, new GUIContent(text, _GetHelpIcon()), EditorStyles.miniLabel);
		}

		private static Texture _helpIcon = null;

		private static Texture _GetHelpIcon()
		{
			if (_helpIcon == null)
			{
				var content = IconContent("_Help");
				if (content != null) _helpIcon = content.image;
			}

			return _helpIcon;
		}

		private static GUIContent[] _waitSpin = null;

		public static GUIContent GetWaitSpinIcon()
		{
			if (_waitSpin == null)
			{
				_waitSpin = new GUIContent[12];
				for (int i = 0; i < 12; i++)
				{
					_waitSpin[i] = IconContent("WaitSpin" + i.ToString("00", CultureInfo.InvariantCulture));
				}
			}

			int frame = (int)(EditorApplication.timeSinceStartup * 10) % 12;
			return _waitSpin[frame];
		}

		private static GUIStyle _whiteTexture;

		public static GUIStyle _GetWhiteTexture()
		{
			if (_whiteTexture == null)
			{
				_whiteTexture = new GUIStyle();
				_whiteTexture.normal.background = EditorGUIUtility.whiteTexture;
			}

			return _whiteTexture;
		}

		public static void HorizontalBar()
		{
			var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));

			if (Event.current.type == EventType.Repaint)
			{
				var oldcolor = GUI.color;
				GUI.color = new Color(0.35f, 0.35f, 0.35f, 1);

				var whiteTextureStyle = _GetWhiteTexture();
				whiteTextureStyle.Draw(rect, false, false, false, false);

				GUI.color = oldcolor;
			}
		}

		private static GUIContent _dummyLabel = new GUIContent(" ");

		public static T[] ArrayField<T>(bool hasPrefix, T[] curList, bool allowSceneObjects) where T : UnityEngine.Object
		{
			var label = hasPrefix ? _dummyLabel : GUIContent.none;
			var newList = new List<T>(curList.Length);

			foreach (var oldObj in curList)
			{
				if (oldObj != null)
				{
					var newObj = EditorGUILayout.ObjectField(label, oldObj, typeof(T), allowSceneObjects) as T;
					if (newObj != null) newList.Add(newObj);
				}
			}

			var addObj = EditorGUILayout.ObjectField(label, null, typeof(T), allowSceneObjects) as T;
			if (addObj != null) newList.Add(addObj);

			return newList.ToArray();
		}

		public static uLink.NetworkStateSynchronization ReplaceStateSync(UnityEngine.NetworkStateSynchronization statesync)
		{
			switch (statesync)
			{
				case UnityEngine.NetworkStateSynchronization.Unreliable: return uLink.NetworkStateSynchronization.Unreliable;
				case UnityEngine.NetworkStateSynchronization.ReliableDeltaCompressed: return uLink.NetworkStateSynchronization.ReliableDeltaCompressed;
				default: return uLink.NetworkStateSynchronization.Off;
			}
		}

		public static T[] GetComponentsInChildren<T>(Transform transform) where T : Component
		{
			var result = new List<T>();

			_GetComponentsInChildren(result, transform);

			return result.ToArray();
		}

		private static void _GetComponentsInChildren<T>(List<T> result, Transform transform) where T : Component
		{
			result.AddRange(transform.GetComponents<T>());

			foreach (Transform child in transform)
			{
				_GetComponentsInChildren(result, child);
			}
		}

		public static bool HasComponentInChildren<T>(Transform transform) where T : Component
		{
			if (transform.GetComponent<T>() != null) return true;

			foreach (Transform child in transform)
			{
				if (HasComponentInChildren<T>(child)) return true;
			}

			return false;
		}

		public static int GetNetworkViewID(UnityEngine.NetworkViewID viewID)
		{
			string str = viewID.ToString();
			int id;

			var sceneID = Regex.Match(str, @"SceneID\: ([0-9]+)");
			if (sceneID.Success && Int32.TryParse(sceneID.Groups[1].Value, out id)) return id;

			var allocatedID = Regex.Match(str, @"AllocatedID\: ([0-9]+)");
			if (allocatedID.Success && Int32.TryParse(allocatedID.Groups[1].Value, out id)) return id;
			
			return 0;
		}

		public static string GetRelativePath(FileInfo fileinfo)
		{
			var fileuri = new Uri(fileinfo.FullName);
			var assetsuri = new Uri(Application.dataPath);
			return assetsuri.MakeRelativeUri(fileuri).ToString();
		}

		public static string GetHierarchyName(Transform transform)
		{
			string hierarchy = transform.name;

			while (transform.parent != null)
			{
				transform = transform.parent;
				hierarchy = transform.name + "/" + hierarchy;
			}

			return hierarchy;
		}

		public static int GetComponentIndex<T>(T component) where T : Component
		{
			var components = component.GetComponents<T>();
			int index = 0;

			foreach (var c in components)
			{
				if (c == component) return index;
				index++;
			}

			return index;
		}

		public static System.Collections.Generic.HashSet<string> FindDuplicatePrefabs(IEnumerable<GameObject> prefabs)
		{
			var duplicates = new System.Collections.Generic.HashSet<string>();
			var previous = new System.Collections.Generic.Dictionary<string, GameObject>();

			foreach (GameObject go in prefabs)
			{
				if (go == null || PrefabUtility.GetPrefabType(go) != PrefabType.Prefab) continue;

				string name = go.name;
				GameObject previousGo;

				if (previous.TryGetValue(name, out previousGo))
				{
					if (previousGo != go) duplicates.Add(name);
				}
				else
				{
					previous.Add(name, go);
				}
			}

			return duplicates;
		}

		public static int CountOccurencesOfChar(string instance, char c)
		{
			int num = 0;
			for (int i = 0; i < instance.Length; i++)
			{
				char c2 = instance[i];
				if (c == c2)
				{
					num++;
				}
			}
			return num;
		}

		public static string[] GetAllAssetFiles(string pattern)
		{
			string assetsDir = Application.dataPath;
			var files = Directory.GetFiles(assetsDir, pattern, SearchOption.AllDirectories);

			int relativePathIndex = assetsDir.Length - "Assets".Length;
			for (int i = 0; i < files.Length; i++)
			{
				files[i] = files[i].Substring(relativePathIndex);
			}

			return files;
		}

		public class Styles
		{
			public GUIStyle Box = "OL Box";
			public GUIStyle ConsoleEntryBackEven = "CN EntryBackEven";
			public GUIStyle ConsoleEntryBackOdd = "CN EntryBackOdd";
			public GUIStyle title = "OL Title";
		}

		private static Styles _styles;

		public static Styles GetStyles()
		{
			if (_styles == null) _styles = new Styles();
			return _styles;
		}

		public static bool IsOnlyOnceWhenProjectOpened(string name)
		{
			// NOTE: hack to make sure this doesn't return true when scripts are recompiled
			string key = name + ":" + Application.dataPath;
			int curId = Process.GetCurrentProcess().Id;
			int lastId = EditorPrefs.GetInt(key, 0);
			if (curId == lastId) return false;
			EditorPrefs.SetInt(key, curId);
			return true;
		}

		public static void PropertyField(Object obj, string propertyPath, params GUILayoutOption[] options)
		{
			var serializedObj = new SerializedObject(obj);
			var serializedProperty = serializedObj.FindProperty(propertyPath);

			EditorGUILayout.PropertyField(serializedProperty, null, true, options);
			serializedObj.ApplyModifiedProperties();
		}

		private static GUIContent _logoGUIContent;

		public static GUIContent GetLogoAsGUIContent()
		{
			return _logoGUIContent ?? (_logoGUIContent = new GUIContent(GetLogoTexture()));
		}

		private static Texture2D _logoTexture;

		public static Texture2D GetLogoTexture()
		{
			return _logoTexture ?? (_logoTexture = AssetDatabase.LoadAssetAtPath(GetLogoPath(), typeof(Texture2D)) as Texture2D);
		}

		private static string GetLogoPath()
		{
			try
			{
				return AssetDatabase.GUIDToAssetPath("3e69dca290c77d24ebc2d003067026fe");
			}
			catch
			{
				try
				{
					return Path.GetDirectoryName(Path.GetDirectoryName(GetuLinkAssemblyPath())) + "/Icons/uLinkIcon.dds";
				}
				catch
				{
					return "Assets/Plugins/uLink/Icons/uLinkIcon.dds";
				}
			}
		}

		public static string GetuLinkAssemblyPath()
		{
			return GetAssemblyPath<NetworkVersion>();
		}

		public static string GetAssemblyPath<T>()
		{
			string absolutePath = typeof(T).Assembly.Location.Replace('\\', '/');
			string relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);

			return relativePath;
		}

		/*
		public static void SetBoolsToTexture(bool[] boolMap, Texture2D tex)
		{
			const int Width = 256;
			const int Height = 256;

			for (var i = 0; i < Width * Height; i++)
			{
				var color = (i < boolMap.Length) ? (boolMap[i] ? Color.white : Color.black) : Color.red;
				tex.SetPixel(i % Width, i / Width, color);
			}

			tex.Apply();
		}

		public static void DrawTexture(Texture2D tex)
		{
			Rect rect = GUILayoutUtility.GetRect(tex.width, tex.height, GUILayout.ExpandWidth(false));

			if (Event.current.type == EventType.Repaint)
			{
				GUIStyle style = new GUIStyle();
				style.normal.background = tex;
				style.Draw(rect, false, false, false, false);
			}
		}
		*/
	}
}
