// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using uLink;

namespace uLinkEditor
{
	public class UnityConverter : EditorWindow
	{
		private const int WINDOW_WIDTH = 500;
		private const int WINDOW_HEIGHT = 390;

		[SerializeField]
		private bool _detailedLogging = false;

		[SerializeField]
		private bool _movePrefabs = true;

		[MenuItem("uLink/Convert from Unity Network...", false, 200)]
		public static void OnMenu()
		{
			var window = GetWindow<UnityConverter>(true, "Convert from Unity Network to uLink", true);
			window.minSize = window.maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);

			window._detailedLogging = EditorPrefs.GetBool("uLinkUnityConverter.detailedLogging", false);
			window._movePrefabs = EditorPrefs.GetBool("uLinkUnityConverter.movePrefabs", true);
		}

		protected void OnGUI()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(12);
			GUILayout.Label("Convert any use of Unity's Network, in your project, to uLink in these following steps:", EditorStyles.wordWrappedLabel);
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			GUILayout.Space(25);

			GUILayout.BeginHorizontal();
			GUILayout.Space(12);
			GUILayout.Label("Step 1. Scripts", EditorStyles.boldLabel);
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(62);
			GUILayout.Label("Automatically search and replace all references, events and calls to Unity's Network API with uLink's API. When done, make sure you correct any compilation errors that might have occurred, before proceeding to the next step.", EditorStyles.wordWrappedLabel);
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Convert All Scripts", GUILayout.Width(200), GUILayout.Height(23)) && CanConvert())
			{
				Helper.ConvertAllScripts(_detailedLogging, _movePrefabs);
				GUIUtility.ExitGUI();
				return;
			}
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			GUILayout.Space(30);

			GUILayout.BeginHorizontal();
			GUILayout.Space(12);
			GUILayout.Label("Step 2. Scenes & Prefabs", EditorStyles.boldLabel);
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(62);
			GUILayout.Label("Automatically search and replace every NetworkView, in each scene and prefab, with uLink's NetworkView while maintaining their properties and whatever prefab connection they might have.", EditorStyles.wordWrappedLabel);
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Convert All Scenes & Prefabs", GUILayout.Width(200), GUILayout.Height(23)) && CanConvert())
			{
				Helper.ConvertAllComponents(_detailedLogging, _movePrefabs);
				GUIUtility.ExitGUI();
				return;
			}
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			GUILayout.Space(60);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			_detailedLogging = GUILayout.Toggle(_detailedLogging, "Detailed Logging");
			if (GUI.changed) EditorPrefs.SetBool("uLinkUnityConverter.detailedLogging", _detailedLogging);
			GUILayout.Space(20);
			_movePrefabs = GUILayout.Toggle(_movePrefabs, "Move Prefabs to Resources");
			if (GUI.changed) EditorPrefs.SetBool("uLinkUnityConverter.movePrefabs", _movePrefabs);
			GUILayout.Space(12);
			GUILayout.EndHorizontal();

			if (Utility.HelpButton(new Rect(8, WINDOW_HEIGHT - 7 - 22, 200, 22), "What does it do?"))
			{
				Application.OpenURL("http://developer.muchdifferent.com/unitypark/uLink/MigrateTouLink");
			}
		}

		private bool CanConvert()
		{
			if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				string message = EditorApplication.isCompiling ?
					"All scripts must be compiled before you can convert!" :
					"Can't convert while the editor is in play mode!";

				ShowNotification(new GUIContent(message));
				return false;
			}

			return true;
		}

		private static class Helper
		{
			private static bool detailedLogging = false;
			private static bool movePrefabs = true;

			private static Scripts scripts = new Scripts();
			private static Scenes scenes = new Scenes();
			private static Prefabs prefabs = new Prefabs();

			public static void ConvertAllScripts(bool _detailedLogging, bool _movePrefabs)
			{
				detailedLogging = _detailedLogging;
				movePrefabs = _movePrefabs;
				Clear();

				EditorUtility.DisplayProgressBar("Search Script(s)", "", 0);
				SearchDirectory(Application.dataPath, ".cs|.js|.boo");
				ConvertScripts();
			}

			public static void ConvertAllComponents(bool _detailedLogging, bool _movePrefabs)
			{
				detailedLogging = _detailedLogging;
				movePrefabs = _movePrefabs;
				Clear();

				EditorUtility.DisplayProgressBar("Search Scene(s) and Prefab(s)", "", 0);
				SearchDirectory(Application.dataPath, ".prefab|.unity");
				ConvertComponents();
			}

			private static void Clear()
			{
				scripts.Clear();
				prefabs.Clear();
				scenes.Clear();
			}

			private static void SearchDirectory(string path, string extensions)
			{
				SearchDirectory(new DirectoryInfo(path), extensions);
			}

			private static void SearchFile(string filename, string extensions)
			{
				SearchFile(new FileInfo(filename), extensions);
			}

			private static void SearchDirectory(DirectoryInfo dirinfo, string extensions)
			{
				if (dirinfo.Name.StartsWith("uLink", StringComparison.InvariantCulture) || (dirinfo.Attributes & FileAttributes.Hidden) != 0) return;

				foreach (var fileinfo in dirinfo.GetFiles())
					SearchFile(fileinfo, extensions);

				foreach (var subdirinfo in dirinfo.GetDirectories())
					SearchDirectory(subdirinfo, extensions);
			}

			private static void SearchFile(FileInfo fileinfo, string extensions)
			{
				if (fileinfo.Name.StartsWith("uLink", StringComparison.InvariantCulture) || (fileinfo.Attributes & FileAttributes.Hidden) != 0) return;

				string extension = fileinfo.Extension.ToUpperInvariant();
				if (extensions.IndexOf(extension, StringComparison.InvariantCulture) == -1) return;

				switch (extension)
				{
					case ".CS": scripts.Add(fileinfo, new CSScript()); return;
					case ".JS": scripts.Add(fileinfo, new JSScript()); return;
					case ".BOO": scripts.Add(fileinfo, new BooScript()); return;
					case ".PREFAB": prefabs.Add(fileinfo, new Prefab()); return;
					case ".UNITY": scenes.Add(fileinfo, new Scene()); return;
					default: return;
				}
			}

			private static void ConvertScripts()
			{
				scripts.ConvertAll();

				AssetDatabase.Refresh();

				scripts.PrintAll();

				uLink.Network.sendRate = UnityEngine.Network.sendRate;
				Utility.SavePrefs();
			}

			private static void ConvertComponents()
			{
				Scene.lastManualViewID = 0;

				string oldscene = EditorApplication.currentScene;

				scenes.ConvertAll();
				prefabs.ConvertAll();

				EditorApplication.OpenScene(oldscene);

				AssetDatabase.SaveAssets();

				if (movePrefabs)
				{
					prefabs.MoveAll();
				}

				scenes.PrintAll();
				prefabs.PrintAll();
			}

			private class Scripts : Assets<Script>
			{
				public void ConvertAll()
				{
					ConvertAll("Script(s)");
				}

				public void PrintAll()
				{
					PrintAll("change(s)", "Script(s)");
				}
			}

			private class Scenes : Assets<Scene>
			{
				public void ConvertAll()
				{
					ConvertAll("Scene(s)");
				}

				public void PrintAll()
				{
					PrintAll("NetworkView(s) converted", "Scene(s)");
				}
			}

			private class Prefabs : Assets<Prefab>
			{
				public void ConvertAll()
				{
					ConvertAll("Prefab(s)");
				}

				public void PrintAll()
				{
					PrintAll("NetworkView(s) converted", "Prefab(s)");
				}

				public void MoveAll()
				{
					float progress = 0;
					float delta = 1.0f / files.Count;

					Directory.CreateDirectory(Application.dataPath + Path.DirectorySeparatorChar + "Resources");
					AssetDatabase.ImportAsset("Assets/Resources", ImportAssetOptions.ForceSynchronousImport);

					foreach (Prefab asset in files.Values)
					{
						EditorUtility.DisplayProgressBar("Moving Prefab(s)", asset.filename, progress);

						asset.Move();

						progress += delta;
					}

					EditorUtility.ClearProgressBar();
				}
			}

			private class Assets<T> where T : Asset
			{
				protected System.Collections.Generic.Dictionary<string, T> files = new System.Collections.Generic.Dictionary<string, T>();

				public void Add(FileInfo fileinfo, T asset)
				{
					Add(Utility.GetRelativePath(fileinfo), asset);
				}

				public void Add(string filename, T asset)
				{
					asset.filename = filename;
					files.Add(filename, asset);
				}

				public void Clear()
				{
					files.Clear();
				}

				public T Find(string filename)
				{
					T asset;
					return files.TryGetValue(filename, out asset) ? asset : null;
				}

				protected void ConvertAll(string types)
				{
					float progress = 0;
					float delta = 1.0f / files.Count;

					foreach (var asset in files.Values)
					{
						EditorUtility.DisplayProgressBar("Converting " + types, asset.filename, progress);

						asset.Convert();

						Resources.UnloadUnusedAssets(); // unload any unused assets
						GC.Collect(); // clear any unreferenced memory allocations

						progress += delta;
					}

					EditorUtility.ClearProgressBar();
				}

				protected void PrintAll(string actions, string types)
				{
					int totalconversions = 0;
					int filecount = 0;
					string filenames = "";

					foreach (var asset in files.Values)
					{
						if (asset.conversions != 0)
						{
							totalconversions += asset.conversions;
							filenames += "\n" + asset.filename + " (" + asset.conversions + ")";
							filecount++;
						}
					}

					Debug.Log(totalconversions + " " + actions + " in " + filecount + " " + types + ":" + filenames);
				}
			}

			private class Scene : Asset
			{
				public static int lastManualViewID = 0;

				public override void Convert()
				{
					if (detailedLogging) Debug.Log("Opening Scene: " + filename);

					EditorApplication.OpenScene(filename);

					var allViews = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.NetworkView)) as UnityEngine.NetworkView[];
					var sceneViews = new List<UnityEngine.NetworkView>(allViews.Length);

					foreach (var view in allViews)
					{
						var type = PrefabUtility.GetPrefabType(view);
						if (type != PrefabType.Prefab) sceneViews.Add(view);
					}

					var prefabInstances = new System.Collections.Generic.HashSet<GameObject>();
					var prefabs = new System.Collections.Generic.HashSet<Prefab>();

					foreach (var view in sceneViews)
					{
						var type = PrefabUtility.GetPrefabType(view);
						if (type == PrefabType.PrefabInstance)
						{
							var pair = ReplacePrefabInstance(view);
							if (pair.Key != null) prefabInstances.Add(pair.Key);
							if (pair.Value != null) prefabs.Add(pair.Value);
						}
						else
						{
							Replace(view);
						}

						conversions++;
					}

					if (conversions == 0) return;

					foreach (var prefab in prefabs)
					{
						prefab.Convert();
					}

					if (detailedLogging) Debug.Log("Saving " + conversions + " conversion(s) in Scene: " + filename);

					EditorApplication.SaveAssets();

					foreach (var instance in prefabInstances)
					{
						PrefabUtility.ReconnectToLastPrefab(instance);
					}

					EditorApplication.SaveScene(filename);
				}

				private void Replace(UnityEngine.NetworkView oldView)
				{
					if (detailedLogging) Debug.Log("Replacing a Scene's NetworkView (" + oldView.viewID + ") at GameObject '" + Utility.GetHierarchyName(oldView.transform) + "' in Scene: " + filename, oldView);

					var newView = oldView.gameObject.AddComponent<uLink.NetworkView>();

					newView.observed = oldView.observed;
					newView.stateSynchronization = Utility.ReplaceStateSync(oldView.stateSynchronization);
					newView._manualViewID = ++lastManualViewID;
					EditorUtility.SetDirty(newView);

					Object.DestroyImmediate(oldView, true);
				}

				private KeyValuePair<GameObject, Prefab> ReplacePrefabInstance(UnityEngine.NetworkView oldView)
				{
					var prefabView = PrefabUtility.GetPrefabParent(oldView) as UnityEngine.NetworkView;
					Prefab prefab = null;

					if (prefabView != null)
					{
						string prefabFile = AssetDatabase.GetAssetPath(prefabView);
						prefab = prefabs.Find(prefabFile);

						if (prefab == null)
						{
							prefab = new Prefab { filename = prefabFile };
							if (!String.IsNullOrEmpty(prefabFile)) prefabs.Add(prefabFile, prefab);
						}
					}

					var prefabInstance = PrefabUtility.FindPrefabRoot(oldView.gameObject);

					Replace(oldView);

					return new KeyValuePair<GameObject, Prefab>(prefabInstance, prefab);
				}
			}

			private class Prefab : Asset
			{
				public override void Convert()
				{
					var prefab = AssetDatabase.LoadAssetAtPath(filename, typeof(GameObject)) as GameObject;
					if (prefab == null) return;

					if (PrefabUtility.GetPrefabType(prefab) != PrefabType.Prefab) return;

					// NOTE: GetComponentInChildren returns 0 in a prefab asset so we have to loop through all children ourselves
					var oldViews = Utility.GetComponentsInChildren<UnityEngine.NetworkView>(prefab.transform);
					if (oldViews.Length == 0) return;

					foreach (UnityEngine.NetworkView oldView in oldViews)
					{
						Replace(oldView);
					}

					if (detailedLogging) Debug.Log("Saving " + conversions + " conversion(s) in Prefab: " + filename);

					EditorApplication.SaveAssets();
				}

				public void Replace(UnityEngine.NetworkView oldView)
				{
					if (detailedLogging) Debug.Log("Replacing a Prefab's NetworkView (" + oldView.viewID + ") at GameObject '" + Utility.GetHierarchyName(oldView.transform) + "' in Prefab: " + filename, oldView);

					var newView = oldView.gameObject.AddComponent<uLink.NetworkView>();

					newView.observed = oldView.observed;
					newView.stateSynchronization = Utility.ReplaceStateSync(oldView.stateSynchronization);
					EditorUtility.SetDirty(newView);

					Object.DestroyImmediate(oldView, true);

					conversions++;
				}

				public void Move()
				{
					if (conversions == 0) return;

					string parentDir = Path.GetDirectoryName(filename);
					if (parentDir == null || String.Compare(Path.GetFileName(parentDir), "Resources", StringComparison.InvariantCultureIgnoreCase) != 0)
					{
						if (detailedLogging) Debug.Log("Moving Prefab to Resources: " + filename);

						string oldfilename = filename;
						filename = "Assets/Resources/" + Path.GetFileName(filename);

						string error = AssetDatabase.MoveAsset(oldfilename, filename);
						if (!String.IsNullOrEmpty(error)) Debug.LogError(error);
					}
				}
			}

			private class CSScript : Script
			{
				private static readonly Regex disconnectCallback = new Regex(@"
					\s*\b(?:private|public|protected|)				# access modifier
					\s*\b(?:virtual|override|)
					\s*\b[a-zA-Z_$][\.a-zA-Z0-9_$]*					# return type
					\s+(?<name>OnDisconnectedFromServer)\s*			# method name (group name)
					\((?>[^)]*)\)									# params
					\s*(\s*//.*?(\r\n?|\n)|\s*/\*.*?\*/)*			# comments
					\{
					(?<code>(?>										# code block (group code)
						[^{}]+
					|
						\{ (?<DEPTH>)								# opening bracket
					|
						\} (?<-DEPTH>)								# closing bracket
					)*)
					(?(DEPTH)(?!))									# all brackets must be closed
					\}", RegexOptions.IgnorePatternWhitespace);

				private static readonly Regex privateRPC = new Regex(@"
					\[RPC\]
					\s*(\s*//.*?(\r\n?|\n)|\s*/\*.*?\*/)*
					\s*\b(?<modifier>private|public|protected|)
					", RegexOptions.IgnorePatternWhitespace);

				public CSScript() : base("&&", "GetComponent<uLink.NetworkView>()", disconnectCallback, privateRPC, false) { }
			}

			private class JSScript : Script
			{
				private static readonly Regex disconnectCallback = new Regex(@"
					\s*\b(?:private|public|protected|)				# access modifier
					\s*\bfunction									# return type
					\s+(?<name>OnDisconnectedFromServer)\s*			# method name (group name)
					\((?>[^)]*)\)									# params
					\s*(\s*//.*?(\r\n?|\n)|\s*/\*.*?\*/)*			# comments
					\{
					(?<code>(?>										# code block (group code)
						[^{}]+
					|
						\{ (?<DEPTH>)								# opening bracket
					|
						\} (?<-DEPTH>)								# closing bracket
					)*)
					(?(DEPTH)(?!))									# all brackets must be closed
					\}", RegexOptions.IgnorePatternWhitespace);

				private static readonly Regex privateRPC = new Regex(@"
					\@RPC\
					\s*(\s*//.*?(\r\n?|\n)|\s*/\*.*?\*/)*
					\s*\b(?<modifier>private|public|protected|)
					\s*\bfunction
					", RegexOptions.IgnorePatternWhitespace);

				public JSScript() : base("&&", "GetComponent(uLink.NetworkView)", disconnectCallback, privateRPC, true) { }

				protected override string ReplaceLanguageSpecific(string data)
				{
					var matches = Regex.Matches(data, @"\#pragma\s+strict\b");

					for (int i = matches.Count - 1; i >= 0; i--)
					{
						var match = matches[i];

						data = data.Remove(match.Index, match.Length);
						conversions++;
					}

					return data;
				}
			}

			private class BooScript : Script
			{
				private static readonly Regex disconnectCallback = null; //TODO: match the disconnectCallback for Boo

				private static readonly Regex privateRPC = new Regex(@"
					\[RPC\]
					\s*(\s*//.*?(\r\n?|\n)|\s*/\*.*?\*/)*
					\s*\b(?<modifier>private|public|protected|)
					\s*\bdef
					", RegexOptions.IgnorePatternWhitespace);

				public BooScript() : base("and", "GetComponent[of uLink.NetworkView]()", disconnectCallback, privateRPC, true) { }
			}

			private class Script : Asset
			{
				private static readonly string[] types = new[]
				{
					"BitStream",
					"HostData",
					"MasterServer",
					"Network",
					"NetworkView",
					"NetworkMessageInfo",
					"NetworkPlayer",
					"NetworkViewID",
					"ConnectionTesterStatus",
					"MasterServerEvent",
					"NetworkConnectionError",
					"NetworkDisconnection",
					"NetworkLogLevel",
					"NetworkPeerType",
					"NetworkStateSynchronization",
					"RPCMode"
				};

				private static readonly string[] callbacks = new[]
				{
					"OnPlayerConnected",
					"OnServerInitialized",
					"OnConnectedToServer",
					"OnPlayerDisconnected",
					"OnDisconnectedFromServer",
					"OnFailedToConnect",
					"OnFailedToConnectToMasterServer",
					"OnMasterServerEvent",
					"OnNetworkInstantiate",
					"OnSerializeNetworkView"
				};

				private readonly string combineCondition;
				private readonly string getComponent;
				private readonly Regex disconnectCallback;
				private readonly Regex privateRPC;
				private readonly bool publicByDefault;

				protected Script(string combineCondition, string getComponent, Regex disconnectCallback, Regex privateRPC, bool publicByDefault)
				{
					this.combineCondition = combineCondition;
					this.getComponent = getComponent;
					this.disconnectCallback = disconnectCallback;
					this.privateRPC = privateRPC;
					this.publicByDefault = publicByDefault;
				}

				public override void Convert()
				{
					if (detailedLogging) Debug.Log("Reading script: " + filename);

					string data = File.ReadAllText(filename);
					string oldata = data;

					data = ReplaceLanguageSpecific(data);
					data = ReplaceConnecting(data);
					data = ReplaceIsClient(data);
					data = ReplaceOnDisconnectedFromServer(data);
					data = ReplaceRPCModifier(data);
					data = ReplaceTypes(data);
					data = ReplaceCallbacks(data);
					data = ReplaceProperties(data);

					if (data == oldata) return;

					if (detailedLogging) Debug.Log("Writing script: " + filename);
					File.WriteAllText(filename, data);
				}

				protected virtual string ReplaceLanguageSpecific(string data)
				{
					return data;
				}

				private string ReplaceTypes(string data)
				{
					foreach (string type in types)
					{
						var matches = Regex.Matches(data, @"(?<!uLink\.\s*)\b" + type + @"\b");

						for (int i = matches.Count - 1; i >= 0; i--)
						{
							var match = matches[i];

							data = data.Insert(match.Index, "uLink.");
							conversions++;
						}
					}

					return data;
				}

				private string ReplaceCallbacks(string data)
				{
					foreach (string callback in callbacks)
					{
						var matches = Regex.Matches(data, @"\b" + callback + @"\b");

						for (int i = matches.Count - 1; i >= 0; i--)
						{
							var match = matches[i];
							if (detailedLogging) Debug.Log("Replacing '" + match.Value + "' with 'uLink_" + match.Value + "' in script: " + filename);

							data = data.Insert(match.Index, "uLink_");
							conversions++;
						}
					}

					return data;
				}

				private string ReplaceProperties(string data)
				{
					var matches = Regex.Matches(data, @"\bnetworkView\b");

					for (int i = matches.Count - 1; i >= 0; i--)
					{
						var match = matches[i];
						if (detailedLogging) Debug.Log("Replacing '" + match.Value + "' with '" + getComponent + "' in script: " + filename);

						data = data.Remove(match.Index, match.Length);
						data = data.Insert(match.Index, getComponent);
						conversions++;
					}

					return data;
				}

				private string ReplaceConnecting(string data)
				{
					var matches = Regex.Matches(data, @"(?<!uLink\.\s*)\bNetwork\s*\.\s*peerType\s*\=\=\s*(?<!uLink\.\s*)NetworkPeerType\s*\.\s*Connecting\b");

					for (int i = matches.Count - 1; i >= 0; i--)
					{
						var match = matches[i];
						if (detailedLogging) Debug.Log("Replacing '" + match.Value + "' with 'uLink.Network.status == uLink.NetworkStatus.Connecting' in script: " + filename);

						data = data.Remove(match.Index, match.Length);
						data = data.Insert(match.Index, "uLink.Network.status == uLink.NetworkStatus.Connecting");
						conversions++;
					}

					return data;
				}

				private string ReplaceIsClient(string data)
				{
					var matches = Regex.Matches(data, @"(?<!uLink\.\s*)\bNetwork\s*\.\s*isClient\b");

					for (int i = matches.Count - 1; i >= 0; i--)
					{
						var match = matches[i];
						if (detailedLogging) Debug.Log("Replacing '" + match.Value + "' with '(uLink.Network.isClient " + combineCondition + " uLink.Network.status == uLink.NetworkStatus.Connected)' in script: " + filename);

						data = data.Remove(match.Index, match.Length);
						data = data.Insert(match.Index, "(uLink.Network.isClient " + combineCondition + " uLink.Network.status == uLink.NetworkStatus.Connected)");
						conversions++;
					}

					return data;
				}

				private string ReplaceOnDisconnectedFromServer(string data)
				{
					if (disconnectCallback != null)
					{
						var match = disconnectCallback.Match(data);
						if (match.Success)
						{
							if (detailedLogging) Debug.Log("Converting 'OnDisconnectedFromServer' to 'uLink_OnDisconnectedFromServer' and 'uLink_OnServerUninitialized' in script: " + filename);

							var name = match.Groups["name"];
							var code = match.Groups["code"];

							string serverCallback = match.Value;

							if (code.Length > 0)
							{
								string serverCode = Regex.Replace(code.Value, @"\bNetwork\s*\.\s*isServer\b", "true");
								if (serverCode != code.Value)
								{
									serverCallback = serverCallback.Remove(code.Index - match.Index, code.Length);
									serverCallback = serverCallback.Insert(code.Index - match.Index, serverCode);

									conversions++;
								}
							}

							serverCallback = serverCallback.Remove(name.Index - match.Index, name.Length);
							serverCallback = serverCallback.Insert(name.Index - match.Index, "uLink_OnServerUninitialized");

							data = data.Insert(match.Index + match.Length, serverCallback);

							conversions++;

							if (code.Length > 0)
							{
								string clientCode = Regex.Replace(code.Value, @"\bNetwork\s*\.\s*isServer\b", "false");
								if (clientCode != code.Value)
								{
									data = data.Remove(code.Index, code.Length);
									data = data.Insert(code.Index, clientCode);

									conversions++;
								}
							}

							data = data.Remove(name.Index, name.Length);
							data = data.Insert(name.Index, "uLink_OnDisconnectedFromServer");

							conversions++;
						}
					}
					else if (Regex.IsMatch(data, @"\bOnDisconnectedFromServer\b"))
					{
						Debug.LogWarning("Couldn't convert OnDisconnectedFromServer to uLink_OnServerUninitialized in script: " + filename);
						Debug.LogWarning("You might need to do this manually. Please read http://developer.unitypark3d.com/manual-ch12.html");
					}

					return data;
				}

				private string ReplaceRPCModifier(string data)
				{
					if (privateRPC != null)
					{
						var matches = privateRPC.Matches(data);

						for (int i = matches.Count - 1; i >= 0; i--)
						{
							var match = matches[i];

							var modifier = match.Groups["modifier"];
							if (modifier.Value == "public" || modifier.Value == "protected" || (publicByDefault && modifier.Length == 0)) continue;

							if (detailedLogging) Debug.Log("Converting private RPC to protected in script: " + filename);

							data = data.Remove(modifier.Index, modifier.Length);
							data = data.Insert(modifier.Index, modifier.Length == 0 ? "protected " : "protected");

							conversions++;
						}
					}

					return data;
				}

				private string NormalizeNewlines(string data)
				{
					return Regex.Replace(data, @"\r\n|\n\r|\n|\r", Environment.NewLine);
				}
			}

			private abstract class Asset
			{
				public string filename;

				public int conversions = 0;

				public abstract void Convert();
			}
		}
	}
}
