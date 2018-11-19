// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace uLinkEditor
{
	public static class uLinkToolExecutor
	{
		[MenuItem("uLink/External Tools/Policy Server", false, 400)]
		public static void OnMenu_PolicyServer()
		{
			StartProcess("PolicyServer", "");
		}

		[MenuItem("uLink/External Tools/Master Server", false, 401)]
		public static void OnMenu_MasterServer()
		{
			StartProcess("MasterServer", "");
		}

		private static void StartProcess(string toolname, string args)
		{
			// TODO: remove this when fixed problem on mac
			if (Application.platform != RuntimePlatform.WindowsEditor)
			{
				EditorUtility.DisplayDialog("External Tool Run Issue", "Can't run external tools from inside the Unity Editor on Mac yet. We are working on it. In the meantime you can still run the tools from Finder or the terminal at /Users/Shared/uLink", "OK");
				return;
			}

			string filename = GetFilename(toolname);
			if (!File.Exists(filename))
			{
				EditorUtility.DisplayDialog("External Tool Not Found", "Can't find it under 'Plugins/uLink/Tools' nor '" + filename + "'. You might have deselected it when installing uLink. You can reinstall and select it at any time.", "OK");
				return;
			}

			Process proc = new Process();

			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				proc.StartInfo.FileName = filename;
				proc.StartInfo.Arguments = args;
				proc.StartInfo.UseShellExecute = true;
			}
			else
			{
				proc.StartInfo.FileName = "mono";
				proc.StartInfo.Arguments = filename + (String.IsNullOrEmpty(args) ? "" : " " + args);
				proc.StartInfo.UseShellExecute = false;
			}

			proc.StartInfo.RedirectStandardOutput = false;
			proc.Start();
		}

		// Returns the the filename for the tool included in this local Unity project if it exists.
		// Otherwise returns the filenam for the tool in the OS "shared folder".
		// The local alternative is needed for the asset store version of uLink since that version deos NOT install the tools
		// in the OS "shared folder". The asset store version demands a relative path inside the Unity project.
		private static string GetFilename(string toolname)
		{
			string projectLocalToolDir = Application.dataPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "uLink" + Path.DirectorySeparatorChar + "Tools" + Path.DirectorySeparatorChar;
			string projectLocalToolFilename = projectLocalToolDir + toolname + Path.DirectorySeparatorChar + "uLink" + toolname + ".exe";
			if (File.Exists(projectLocalToolFilename))
			 return projectLocalToolFilename;
			else
			 return GetSharedFolder() + toolname + Path.DirectorySeparatorChar + "uLink" + toolname + ".exe";
		}

		private static string GetSharedFolder()
		{
			return Application.platform == RuntimePlatform.WindowsEditor ?
				Environment.GetFolderPath((Environment.SpecialFolder)46) + Path.DirectorySeparatorChar + "uLink Tools" + Path.DirectorySeparatorChar :
				"/Users/Shared/uLink/";
		}
	}
}
