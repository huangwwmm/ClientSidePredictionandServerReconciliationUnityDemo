// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace uLinkEditor
{
	// NOTE: this class is a workaround for a issue in Unity 4.x. If adb.exe isn't running, then Unity will start it during playmode which for some reason makes the uLink socket co-owned by adb.exe. We solve this by starting the adb.exe at Editor startup instead.

	[InitializeOnLoad]
	public static class WinAndroidHack
	{
		static WinAndroidHack()
		{
			if (Application.platform != RuntimePlatform.WindowsEditor) return;
			if (Application.unityVersion.StartsWith("3")) return;

			if (!Utility.IsOnlyOnceWhenProjectOpened("uLinkWinHack")) return;

			try
			{
				_StartADB();
			}
			catch
			{
			}
		}

		private static void _StartADB()
		{
			string androidSDKRoot = EditorPrefs.GetString("AndroidSdkRoot", null);
			if (String.IsNullOrEmpty(androidSDKRoot)) return;

			string toolsDir = Path.Combine(androidSDKRoot, "platform-tools");
			if (!Directory.Exists(toolsDir)) return;

			string adbExe = Path.Combine(toolsDir, "adb.exe");
			if (!File.Exists(adbExe)) return;

			foreach (var p in Process.GetProcesses())
			{
				try
				{
					if (p.ProcessName == "adb") return;
				}
				catch
				{
				}
			}

			new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = adbExe,
					Arguments = "start-server",
					UseShellExecute = false,
					CreateNoWindow = true,
					//RedirectStandardOutput = true,
					//RedirectStandardError = true,
				},
			}.Start();
		}
	}
}
