// (c)2012 MuchDifferent. All Rights Reserved.

using System;
using UnityEditor;
using UnityEngine;

namespace uLinkEditor
{
	/*
	[InitializeOnLoad]
	public static class VersionMigrator_1_5
	{
		internal static readonly string _NAME = typeof(VersionMigrator_1_5).Name;

		static VersionMigrator_1_5()
		{
			if (EditorPrefs.GetBool(Application.dataPath + ":" + _NAME + ".hasMigrated", false)
				|| !Utility.IsOnlyOnceWhenProjectOpened(_NAME))
			{
				return;
			}

			EditorApplication.update += Migrate;
		}

		private static void Migrate()
		{
			EditorApplication.update -= Migrate;

			if (LoadOldPrefs())
			{
				Debug.Log("Migrating uLink 1.2 settings file (uLinkNetworkPrefs.cs) to 1.5.0 (uLinkPrefs.txt)");

				Utility.SavePrefs();
				DeleteOldPrefs();
			}

			EditorPrefs.SetBool(Application.dataPath + ":" + _NAME + ".hasMigrated", true);
		}

		private static bool LoadOldPrefs()
		{
			bool found = false;

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.FullName.StartsWith("Assembly-"))
				{
					try
					{
						foreach (var type in asm.GetExportedTypes())
						{
							if (type.IsClass && type.Name == "uLinkNetworkPrefs")
							{
								var initializer = type.TypeInitializer;
								if (initializer != null)
								{
									initializer.Invoke(null, null);
									found = true;
								}
							}
						}
					}
					catch
					{
					}
				}
			}

			return found;
		}

		private static void DeleteOldPrefs()
		{
			AssetDatabase.DeleteAsset("Assets/Resources/uLinkNetworkPrefs.cs");
		}
	}
	*/
}
