// (c)2011 Unity Park. All Rights Reserved.

using System;
using UnityEngine;
using UnityEditor;

namespace uLinkEditor
{
	public class LicenseHelper : EditorWindow
	{
		private const int WINDOW_WIDTH = 400;
		private const int WINDOW_HEIGHT = 220;

		[SerializeField]
		private string licenseKey = "";

		[MenuItem("uLink/Enter License...", false, 100)]
		public static void OnMenu()
		{
			LicenseHelper window = GetWindow<LicenseHelper>(true, "Enter License Key for uLink", true);
			window.minSize = window.maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
		}

		protected void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(12);
			EditorGUILayout.BeginVertical();

			GUILayout.Label("To activate a full license for uLink, follow these instructions:", EditorStyles.wordWrappedLabel);
			GUILayout.Space(12);

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Enter license key: ");
			licenseKey = EditorGUILayout.TextField(licenseKey, GUILayout.Width(200), GUILayout.Height(20));
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(20);
			GUILayout.Label("Open your server scene. The scene must not be included when building the client. Press create to add a game object with the component 'uLinkEnterLicenseKey' which will register your key.", EditorStyles.wordWrappedLabel);
			GUILayout.Space(16);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(12);
			GUILayout.FlexibleSpace();

			if (String.IsNullOrEmpty(licenseKey))
			{
				GUI.enabled = false;
				GUILayout.Button("Create 'Enter License Key'", GUILayout.Width(200), GUILayout.Height(23));
				GUI.enabled = true;
			}
			else
			{
				if (GUILayout.Button("Create 'Enter License Key'", GUILayout.Width(200), GUILayout.Height(23)))
				{
					var go = new GameObject("uLinkEnterLicenseKey");
					var component = go.AddComponent<uLink.EnterLicenseKey>();

					if (component != null) component.licenseKey = licenseKey;
					Selection.activeGameObject = go;

					Close();
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.Space(12);
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.EndVertical();
			GUILayout.Space(12);
			EditorGUILayout.EndHorizontal();

			if (Utility.HelpButton(new Rect(8, WINDOW_HEIGHT - 7 - 22, 200, 22), "Where can I get a license key?"))
			{
				ResourceLinker.OnMenu_Buy();
			}
		}
	}
}
