// (c)2012 MuchDifferent. All Rights Reserved.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace uLinkEditor
{
	/*
	public static class VersionMigrator_1_5_1
	{
		internal static readonly string _NAME = typeof(VersionMigrator_1_5_1).Name;

		static VersionMigrator_1_5_1()
		{
			if (EditorPrefs.GetBool(Application.dataPath + ":" + _NAME + ".hasMigrated", false)
				|| !Utility.IsOnlyOnceWhenProjectOpened(_NAME))
			{
				return;
			}

			EditorApplication.update += _Migrate;
		}

		private static void _Migrate()
		{
			EditorApplication.update -= _Migrate;

			if (Application.unityVersion.StartsWith("4."))
			{
				var files = _FindAllPrefabs();
				foreach (var file in files)
				{
					_ActivatePrefab(file);
				}
			}

			EditorPrefs.SetBool(Application.dataPath + ":" + _NAME + ".hasMigrated", true);
		}

		private static void _ActivatePrefab(string filename)
		{
			var prefab = AssetDatabase.LoadAssetAtPath(filename, typeof(GameObject)) as GameObject;
			if (prefab == null) return;

			if (PrefabUtility.GetPrefabType(prefab) != PrefabType.Prefab) return;

			var views = uLink.NetworkInstantiatorUtility.GetComponentsInChildren<uLink.NetworkView>(prefab.transform);
			if (views.Count == 0) return;

			Debug.Log("Migrating uLink 1.5.0 network prefab (" + filename + ") to 1.5.1, by making sure it's active");

			uLink.NetworkInstantiatorUtility.SetActiveRecursively(prefab.transform, true);
		}

		private static List<string> _FindAllPrefabs()
		{
			var files = new List<string>();
			_FindAllPrefabs(files, new DirectoryInfo("Assets/"));
			return files;
		}

		private static void _FindAllPrefabs(List<string> result, DirectoryInfo dirinfo)
		{
			foreach (FileInfo fileinfo in dirinfo.GetFiles("*.prefab"))
			{
				result.Add(Utility.GetRelativePath(fileinfo));
			}

			foreach (DirectoryInfo subdirinfo in dirinfo.GetDirectories())
			{
				_FindAllPrefabs(result, subdirinfo);
			}
		}
	}
	*/
}
