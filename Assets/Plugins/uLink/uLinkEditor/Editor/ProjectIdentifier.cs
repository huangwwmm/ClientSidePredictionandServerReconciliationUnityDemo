// (c)2012 MuchDifferent. All Rights Reserved.

using System;
using UnityEditor;
using UnityEngine;
using uLink;

namespace uLinkEditor
{
	internal static class ProjectIdentifier
	{
		/*public static NetworkVersion version;
		public static string name;
		public static string company;
		public static string guid;*/

		public static void GetPrefs()
		{
			/*
			version = new NetworkVersion(NetworkPrefs.Get("Project.version", "1.5.1 Lina (2013-01-22)"));
			name = NetworkPrefs.Get("Project.name", PlayerSettings.productName);
			company = NetworkPrefs.Get("Project.company", PlayerSettings.companyName);
			guid = NetworkPrefs.Get("Project.guid", Guid.NewGuid().ToString());
			*/
		}

		public static void SetPrefs()
		{
			/*
			NetworkPrefs.Set("Project.version", version.ToString());
			NetworkPrefs.Set("Project.name", name);
			NetworkPrefs.Set("Project.company", company);
			NetworkPrefs.Set("Project.guid", guid);
			*/
		}
	}
}
