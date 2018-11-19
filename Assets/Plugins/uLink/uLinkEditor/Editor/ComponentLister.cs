// (c)2011 Unity Park. All Rights Reserved.

using UnityEngine;
using UnityEditor;

namespace uLinkEditor
{
	/* TODO: use this when removed the basic script files outside of the dll

	public static class ComponentLister
	{
		public static Object[] GetSceneSelectedGameObjects()
		{
			return Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.ExcludePrefab);
		}

		public static bool CanAdd()
		{
			return GetSceneSelectedGameObjects().Length != 0;
		}

		public static void Add<T>() where T : Component
		{
			var type = typeof(T);
			var gos = GetSceneSelectedGameObjects();

			foreach (GameObject go in gos)
			{
				go.AddComponent(type);
			}
		}

		[MenuItem("uLink/Basic Components/Network View", true, 610)]
		public static bool OnMenu_NetworkView_Validate()
		{
			return CanAdd();
		}

		[MenuItem("uLink/Basic Components/Network View", false, 610)]
		public static void OnMenu_NetworkView()
		{
			Add<uLink.NetworkView>();
		}

		[MenuItem("uLink/Basic Components/Network P2P", true, 611)]
		public static bool OnMenu_NetworkP2P_Validate()
		{
			return CanAdd();
		}

		[MenuItem("uLink/Basic Components/Network P2P", false, 611)]
		public static void OnMenu_NetworkP2P()
		{
			Add<uLink.NetworkP2P>();
		}

		[MenuItem("uLink/Basic Components/Register Prefabs", true, 650)]
		public static bool OnMenu_RegisterPrefabs_Validate()
		{
			return CanAdd();
		}

		[MenuItem("uLink/Basic Components/Register Prefabs", false, 650)]
		public static void OnMenu_RegisterPrefabs()
		{
			Add<uLink.RegisterPrefabs>();
		}

		[MenuItem("uLink/Basic Components/Server Authentication", true, 651)]
		public static bool OnMenu_ServerAuthentication_Validate()
		{
			return CanAdd();
		}

		[MenuItem("uLink/Basic Components/Server Authentication", false, 651)]
		public static void OnMenu_ServerAuthentication()
		{
			Add<uLink.ServerAuthentication>();
		}

		[MenuItem("uLink/Basic Components/Enter License Key", true, 652)]
		public static bool OnMenu_EnterLicenseKey_Validate()
		{
			return CanAdd();
		}

		[MenuItem("uLink/Basic Components/Enter License Key", false, 652)]
		public static void OnMenu_EnterLicenseKey()
		{
			Add<uLink.EnterLicenseKey>();
		}
	}
	*/
}
