// (c)2011 Unity Park. All Rights Reserved.

using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	[CustomEditor(typeof(uLink.EnterLicenseKey))]
	public class EnterLicenseKeyEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var target = base.target as EnterLicenseKey;
			if (target == null) return;

			var textFieldStyle = new GUIStyle(EditorStyles.miniTextField);
			textFieldStyle.fixedHeight = 0;
			textFieldStyle.fixedWidth = 0;
			textFieldStyle.stretchWidth = true;

			GUILayout.Label("To activate a full license for uLink, enter your license key:", EditorStyles.wordWrappedLabel);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(15);
			target.licenseKey = EditorGUILayout.TextField(target.licenseKey, textFieldStyle);
			GUILayout.Space(15);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);

			EditorGUILayout.HelpBox("Your key must never be included in the client build; otherwise it's compromised.", MessageType.Info);

			GUILayout.Space(2);
		}
	}
}
