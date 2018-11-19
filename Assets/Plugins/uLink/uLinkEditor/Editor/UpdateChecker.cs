// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	[InitializeOnLoad]
	public class UpdateChecker : EditorWindow
	{
		private const string URL_VERSION = "http://download.muchdifferent.com/updatechecker/format-v5.php?product=ulink";
		private const string URL_DOWNLOAD = "http://developer.muchdifferent.com/unitypark/Downloads";

		//private static bool isAutorun = true;
		//private static bool hasWindow = false;
		
		[NonSerialized]
		private static WWW www;

		//[SerializeField]
		//private string comparisonText = String.Empty;
		//[SerializeField]
		//private string newVersion = String.Empty;
		//[SerializeField]
		//private string releaseNotes = String.Empty;
		[SerializeField]
		private Vector2 scrollPosition;

		static UpdateChecker()
		{
			//if (!Utility.IsOnlyOnceWhenProjectOpened("uLinkUpdateChecker"))
			//{
			//	return;
			//}

			//isAutorun = EditorPrefs.GetBool("uLinkUpdateChecker.isAutorun", isAutorun);
			//if (isAutorun)
			//{
			//	FetchVersionInfo();
			//}
		}

		//[MenuItem("uLink/Check for Updates...", false, 500)]
		//public static void OnMenu()
		//{
		//	//GetWindow();

		//	//FetchVersionInfo();
		//}

		protected void OnGUI()
		{
			//hasWindow = true;

			//if (www == null) FetchVersionInfo();

			//GUI.Box(new Rect(13, 8, 64, 64), Utility.GetLogoAsGUIContent(), GUIStyle.none);

			//GUILayout.BeginHorizontal();
			//GUILayout.Space(12);
			//GUILayout.BeginVertical();
			//GUILayout.Space(6);

			//GUILayout.BeginHorizontal();
			//GUILayout.Space(120);
			//GUILayout.Label("Imported version:", GUILayout.Width(130));
			//GUILayout.Label(uLink.NetworkVersion.current.ToString(), GUILayout.Width(270));
			//GUILayout.FlexibleSpace();
			//GUILayout.EndHorizontal();

			//if (www != null && www.isDone)
			//{
			//	if (!String.IsNullOrEmpty(newVersion))
			//	{
			//		GUILayout.BeginHorizontal();
			//		GUILayout.Space(120);
			//		GUILayout.Label("New version:", EditorStyles.boldLabel, GUILayout.Width(130));
			//		GUILayout.Label(newVersion, EditorStyles.boldLabel, GUILayout.Width(270));
			//		GUILayout.FlexibleSpace();
			//		GUILayout.EndHorizontal();
			//	}

			//	GUILayout.Space(18);
			//	GUILayout.BeginHorizontal();
			//	GUILayout.Space(120);
			//	GUILayout.Label(comparisonText, EditorStyles.wordWrappedLabel);
			//	GUILayout.EndHorizontal();

			//	if (!String.IsNullOrEmpty(releaseNotes))
			//	{
			//		GUILayout.Space(22);
			//		GUILayout.Label("Release notes:", EditorStyles.boldLabel, GUILayout.Width(200));
			//		GUILayout.Space(2);

			//		GUILayout.BeginHorizontal();
			//		GUILayout.Space(13);

			//		var style = EditorStyles.wordWrappedMiniLabel;

			//		// Calculate height for scoll size based so it doesn't cutoff a line in the middle:
			//		var rect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
			//		rect.height -= (rect.height - 2) % style.lineHeight;

			//		// Calculate height for view size based on word wrapping releaseNotes:
			//		var view = new Rect(0, 0, rect.width - 15, 0);
			//		view.height = style.CalcHeight(new GUIContent(releaseNotes), view.width);

			//		scrollPosition = GUI.BeginScrollView(rect, scrollPosition, view);
			//		GUI.Label(view, releaseNotes, style);
			//		GUI.EndScrollView();

			//		EditorGUILayout.EndVertical();

			//		GUILayout.Space(9);
			//		GUILayout.EndHorizontal();
			//	}
			//	else
			//	{
			//		GUILayout.FlexibleSpace();
			//	}

			//	GUILayout.Space(18);

			//	if (!String.IsNullOrEmpty(newVersion))
			//	{
			//		GUILayout.BeginHorizontal();
			//		GUILayout.FlexibleSpace();
			//		if (GUILayout.Button("Download new version", GUILayout.Width(200), GUILayout.Height(23)))
			//		{
			//			Application.OpenURL(URL_DOWNLOAD);
			//			Close();
			//		}
			//		if (GUILayout.Button("Skip new version", GUILayout.Width(200), GUILayout.Height(23)))
			//		{
			//			EditorPrefs.SetString("uLinkUpdateChecker.skipVersion", newVersion);
			//			Close();
			//		}
			//		GUILayout.FlexibleSpace();
			//		GUILayout.EndHorizontal();
			//		GUILayout.Space(12);
			//	}
			//}
			//else
			//{
			//	GUILayout.Space(40);

			//	GUILayout.BeginHorizontal();
			//	GUILayout.FlexibleSpace();
			//	GUILayout.BeginVertical();
			//	GUILayout.FlexibleSpace();
			//	GUILayout.Label(new GUIContent(" Checking for updates...", Utility.GetWaitSpinIcon().image));
			//	GUILayout.FlexibleSpace();
			//	GUILayout.EndVertical();
			//	GUILayout.FlexibleSpace();
			//	GUILayout.EndHorizontal();

			//	GUILayout.Space(23);
			//}

			//GUILayout.EndVertical();
			//GUILayout.Space(12);
			//GUILayout.EndHorizontal();

			//GUILayout.BeginHorizontal();
			//GUILayout.FlexibleSpace();
			//isAutorun = GUILayout.Toggle(isAutorun, "Check for Updates");
			//GUILayout.Space(8);
			//GUILayout.EndHorizontal();
			//GUILayout.Space(8);

			//if (GUI.changed)
			//{
			//	EditorPrefs.SetBool("uLinkUpdateChecker.isAutorun", isAutorun);
			//}
		}

		protected void OnInspectorUpdate()
		{
			//if (www != null && !www.isDone)
			//{
			//	Repaint();
			//}
		}

		private static UpdateChecker GetWindow()
		{
			//var window = GetWindow<UpdateChecker>(true, "Check for uLink Updates");
			//window.minSize = new Vector2(600, 412);

			//hasWindow = true;

			//return window;
			return null;
		}

		private static void FetchVersionInfo()
		{
			//www = new WWW(URL_VERSION);
			//EditorApplication.update += CheckVersionInfo;
		}

		private static void CheckVersionInfo()
		{
			//if (www == null) www = new WWW(URL_VERSION);
			//if (!www.isDone && www.error == null) return;

			//EditorApplication.update -= CheckVersionInfo;

			//CompareVersions();
		}

		private static void CompareVersions()
		{
			//string newVersion;
			//string releaseNotes;

			//if (String.IsNullOrEmpty(www.error))
			//{
			//	string printableText = www.text.Replace((char)65533, '\'');
			//	string[] split = printableText.Split(new char[] { '\n', '\r' }, 2, StringSplitOptions.RemoveEmptyEntries);

			//	newVersion = split.Length > 0 ? split[0] : String.Empty;
			//	releaseNotes = split.Length > 1 ? split[1] : String.Empty;

			//	string skipVersion = EditorPrefs.GetString("uLinkUpdateChecker.skipVersion");
			//	if (!hasWindow && newVersion == skipVersion)
			//	{
			//		return;
			//	}
			//}
			//else
			//{
			//	UnityEngine.Debug.LogWarning("Unable to check for uLink updates. Web request returned: " + www.error);

			//	if (!hasWindow)
			//	{
			//		return;
			//	}

			//	newVersion = String.Empty;
			//	releaseNotes = String.Empty;
			//}

			//var version = new uLink.NetworkVersion(newVersion, NetworkVersion.InputFormat.FileName);
			//bool isUnavailable = version.Equals(uLink.NetworkVersion.unavailable);
			//bool hasDownload = uLink.NetworkVersion.current < version;

			//if (hasWindow || hasDownload)
			//{
			//	var window = GetWindow();
			//	window.newVersion = hasDownload ? version.ToString() : String.Empty;
			//	window.comparisonText = GetComparisonText(isUnavailable, hasDownload);
			//	window.releaseNotes = releaseNotes;
			//	window.Repaint();
			//}
		}

		private static string GetComparisonText(bool isUnavailable, bool hasDownload)
		{
		//	if (isUnavailable) return "Sorry, couldn't check the latest version.";

		//	switch (uLink.NetworkVersion.current.build)
		//	{
		//		case uLink.NetworkVersionBuild.Alpha:
		//			return (hasDownload) ?
		//				"We highly recommend upgrading your\nalpha version to the new stable version of uLink." :
		//				"Your alpha version of uLink is the latest.";

		//		case uLink.NetworkVersionBuild.Beta:
		//			return (hasDownload) ?
		//				"We highly recommend upgrading your\nbeta version to the new stable version of uLink." :
		//				"Your beta version of uLink is the latest.";

		//		case uLink.NetworkVersionBuild.Custom:
		//			return (hasDownload) ?
		//				"There is a new version available,\nbut you might want to keep your custom build of uLink." :
		//				"Your custom build of uLink is the latest.";

		//		default:
		//			return (hasDownload) ?
		//				"There is a new version of uLink available for download." :
		//				"uLink is up to date.";
		//	}
			return "";
		}
	}
}
