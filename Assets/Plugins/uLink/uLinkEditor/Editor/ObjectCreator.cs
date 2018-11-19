// (c)2011 Unity Park. All Rights Reserved.

using UnityEngine;
using UnityEditor;

namespace uLinkEditor
{
	/* TODO: use this when removed the basic script files outside of the dll
	
	public static class ObjectCreator
	{
		public static bool CanCreate()
		{
			return true;
		}

		public static void Create<T>() where T : Component
		{
			var type = typeof(T);
			var go = new GameObject(type.Name, type);
			Selection.activeGameObject = go;
		}

		[MenuItem("uLink/Create Object/Register Prefabs", false, 600)]
		public static void OnMenu_RegisterPrefabs()
		{
			Create<uLink.RegisterPrefabs>();
		}

		[MenuItem("uLink/Create Object/Server Authentication", false, 601)]
		public static void OnMenu_ServerAuthentication()
		{
			Create<uLink.ServerAuthentication>();
		}

		[MenuItem("uLink/Create Object/Enter License Key", false, 602)]
		public static void OnMenu_EnterLicenseKey()
		{
			Create<uLink.EnterLicenseKey>();
		}
	}
	*/
}
