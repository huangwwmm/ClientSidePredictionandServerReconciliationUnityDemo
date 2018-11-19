// (c)2011 Unity Park. All Rights Reserved.

using System;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	public class SettingsEditor : EditorWindow
	{
		[SerializeField]
		private bool isInitialized = false;

		//[SerializeField]
		//private string lastReadPrefs = String.Empty;

		[SerializeField]
		private bool foldoutLogging = false;
		[SerializeField]
		protected bool foldoutClient = false;
		[SerializeField]
		private bool foldoutServer = false;
		[SerializeField]
		private bool foldoutCellServer = false;
		[SerializeField]
		private bool foldoutMasterServer = false;
		[SerializeField]
		private bool foldoutEmulation = false;
		[SerializeField]
		private bool foldoutConfig = false;

		[SerializeField]
		private Vector2 scrollPosition;

		[SerializeField]
		private bool wasPlaying = false;

		[SerializeField]
		private double lastRepaintTime;
		private const double REPAINT_INTERVAL = 0.5;

		[SerializeField]
		private bool useAutoSave = true;

		[SerializeField]
		private double lastAutoSaveTime;
		private const double AUTOSAVE_INTERVAL = 1.5;

		[SerializeField]
		private int emulationPreset = 0;

		private static readonly string[] LOGLEVEL_NAMES =
		{
			"Off",
			"Error",
			"Warning",
			"Info",
			"Debug",
		};

		private static readonly string[] RPCTYPESAFE_NAMES =
		{
			"Off",
			"Only In Editor",
			"Always",
		};

		private const int INDENT_BASE_LEVEL = 0;

		[MenuItem("uLink/Edit Settings...", false, 301)]
		public static void OnMenu()
		{
			GetWindow<SettingsEditor>(false, "uLink Settings", true);
		}

		private void Init()
		{
			isInitialized = true;

			emulationPreset = EmulationPreset.Find();
			useAutoSave = EditorPrefs.GetBool("uLinkSettingsEditor.useAutoSave", useAutoSave);
			lastAutoSaveTime = EditorApplication.timeSinceStartup;

			Show(true);
		}

		protected void OnGUI()
		{
			if (!isInitialized) Init();

			DrawSettingsGUI();

			lastRepaintTime = EditorApplication.timeSinceStartup;
		}

		protected void Update()
		{
			if (!isInitialized) Init();

			bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
			bool isCompiling = EditorApplication.isCompiling;
			double time = EditorApplication.timeSinceStartup;

			if (wasPlaying && !isPlaying)
			{
				Utility.ReloadPrefs();
			}

			wasPlaying = isPlaying;

			if (isPlaying && time - lastRepaintTime >= REPAINT_INTERVAL)
			{
				Repaint();
			}

			if (!isPlaying && !isCompiling && useAutoSave && time - lastAutoSaveTime >= AUTOSAVE_INTERVAL)
			{
				Utility.SavePrefs();
				Repaint();
			}
		}

		private void DrawSettingsGUI()
		{
			EditorGUIUtility.LookLikeInspector();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			GUILayout.Space(12);

			EditorGUI.indentLevel = INDENT_BASE_LEVEL + 1;
			uLink.Network.sendRate = Mathf.Max(0, EditorGUILayout.FloatField("State Sync Send Rate", uLink.Network.sendRate));
			uLink.Network.isAuthoritativeServer = EditorGUILayout.Toggle("Authoritative Server", uLink.Network.isAuthoritativeServer);
			uLink.Network.useDifferentStateForOwner = EditorGUILayout.Toggle("Different State For Owner", uLink.Network.useDifferentStateForOwner);
			uLink.Network.rpcTypeSafe = (RPCTypeSafe)EditorGUILayout.Popup("RPC Type Safe", (int)uLink.Network.rpcTypeSafe, RPCTYPESAFE_NAMES);
			EditorGUI.indentLevel = INDENT_BASE_LEVEL;

			if (BeginFoldout(ref foldoutClient, "Client"))
			{
				uLink.Network.requireSecurityForConnecting = EditorGUILayout.Toggle("Require Security For Connecting", uLink.Network.requireSecurityForConnecting);
				uLink.Network.symmetricKeySize = Mathf.Max(0, EditorGUILayout.IntField("AES Symmetric Key Size", uLink.Network.symmetricKeySize));

				EndFoldout();
			}

			if (BeginFoldout(ref foldoutServer, "Server"))
			{
				uLink.Network.incomingPassword = EditorGUILayout.TextField("Incoming Password", uLink.Network.incomingPassword);
				uLink.Network.useProxy = EditorGUILayout.Toggle("Use Proxy", uLink.Network.useProxy);
				uLink.Network.useRedirect = EditorGUILayout.Toggle("Use Redirect", uLink.Network.useRedirect);
				uLink.Network.redirectIP = EditorGUILayout.TextField("Redirect IP/Host", uLink.Network.redirectIP);
				uLink.Network.redirectPort = Mathf.Max(0, EditorGUILayout.IntField("Redirect Port", uLink.Network.redirectPort));

				EndFoldout();
			}

			if (BeginFoldout(ref foldoutCellServer, "Cell Server"))
			{
				uLink.Network.trackRate = Mathf.Max(0, EditorGUILayout.FloatField("Track Rate", uLink.Network.trackRate));
				uLink.Network.trackMaxDelta = Mathf.Max(0, EditorGUILayout.FloatField("Track Max Delta", uLink.Network.trackMaxDelta));

				EndFoldout();
			}

			if (BeginFoldout(ref foldoutMasterServer, "Master Server"))
			{
				uLink.MasterServer.gameType = EditorGUILayout.TextField("Game Type", uLink.MasterServer.gameType);
				uLink.MasterServer.gameName = EditorGUILayout.TextField("Game Name", uLink.MasterServer.gameName);
				uLink.MasterServer.gameMode = EditorGUILayout.TextField("Game Mode", uLink.MasterServer.gameMode);
				uLink.MasterServer.gameLevel = EditorGUILayout.TextField("Game Level", uLink.MasterServer.gameLevel);
				uLink.MasterServer.comment = EditorGUILayout.TextField("Comment", uLink.MasterServer.comment);
				uLink.MasterServer.dedicatedServer = EditorGUILayout.Toggle("Dedicated Server", uLink.MasterServer.dedicatedServer);
				uLink.MasterServer.updateRate = Mathf.Max(0, EditorGUILayout.FloatField("Update Rate", uLink.MasterServer.updateRate));
				uLink.MasterServer.ipAddress = EditorGUILayout.TextField("Master Server IP/Host", uLink.MasterServer.ipAddress);
				uLink.MasterServer.port = Mathf.Max(0, EditorGUILayout.IntField("Master Server Port", uLink.MasterServer.port));
				uLink.MasterServer.password = EditorGUILayout.TextField("Master Server Password", uLink.MasterServer.password);

				EndFoldout();
			}

			GUILayout.Space(12);

			EditorGUI.indentLevel = INDENT_BASE_LEVEL + 1;
			uLink.NetworkLog.minLevel = (uLink.NetworkLogLevel)EditorGUILayout.Popup("Minimum Log Level", (int)uLink.NetworkLog.minLevel, LOGLEVEL_NAMES);
			EditorGUI.indentLevel = INDENT_BASE_LEVEL;

			if (BeginFoldout(ref foldoutLogging, "Detailed Log Levels"))
			{
				foreach (uint flagValue in Enum.GetValues(typeof(uLink.NetworkLogFlags)))
				{
					uLink.NetworkLogFlags flag = (uLink.NetworkLogFlags)flagValue;
					if (flag == uLink.NetworkLogFlags.None || flag == uLink.NetworkLogFlags.All) continue;

					int curLevel = (int)uLink.NetworkLog.GetMaxLevel(flag);
					int newLevel = EditorGUILayout.Popup(flag.ToString(), curLevel, LOGLEVEL_NAMES);
					if (curLevel != newLevel) uLink.NetworkLog.SetLevel(flag, (uLink.NetworkLogLevel)newLevel);
				}

				EndFoldout();
			}

			GUILayout.Space(12);

			{
				bool oldGUIChanged = GUI.changed;
				GUI.changed = false;

				EditorGUI.indentLevel = INDENT_BASE_LEVEL + 1;
				emulationPreset = EditorGUILayout.Popup("Network Emulation", emulationPreset, EmulationPreset.NAMES);
				EditorGUI.indentLevel = INDENT_BASE_LEVEL;

				if (GUI.changed)
				{
					EmulationPreset.Apply(emulationPreset);
				}
				else
				{
					GUI.changed = oldGUIChanged;
				}
			}

			if (BeginFoldout(ref foldoutEmulation, "Detailed Emulation"))
			{
				bool oldGUIChanged = GUI.changed;
				GUI.changed = false;

				uLink.Network.emulation.maxBandwidth = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Max Bandwidth", "The amount of kB allowed to be sent per second. Set to 0 for no limit."), uLink.Network.emulation.maxBandwidth));
				uLink.Network.emulation.chanceOfLoss = Mathf.Clamp01(EditorGUILayout.FloatField(new GUIContent("Chance Of Loss", "The chance for a packet to become lost in transit. 0 means no loss. 1 means all packets are lost."), uLink.Network.emulation.chanceOfLoss));
				uLink.Network.emulation.chanceOfDuplicates = Mathf.Clamp01(EditorGUILayout.FloatField(new GUIContent("Chance Of Duplicates", "The chance for a packet to become duplicated in transit. 0 means no duplicates. 1 means all packets are duplicated."), uLink.Network.emulation.chanceOfDuplicates));
				uLink.Network.emulation.minLatency = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Minimum Latency", "The minimum two-way latency in seconds of outgoing packets."), uLink.Network.emulation.minLatency));
				uLink.Network.emulation.maxLatency = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Maximum Latency", "The maximum two-way latency in seconds of outgoing packets."), uLink.Network.emulation.maxLatency));

				if (GUI.changed)
				{
					emulationPreset = EmulationPreset.Find();
				}
				else
				{
					GUI.changed = oldGUIChanged;
				}

				EndFoldout();
			}

			GUILayout.Space(12);

			if (BeginFoldout(ref foldoutConfig, "Advanced Configuration"))
			{
				uLink.Network.config.localIP = EditorGUILayout.TextField("Local IP Address", uLink.Network.config.localIP);
				uLink.Network.config.sendBufferSize = Mathf.Max(0, EditorGUILayout.IntField("Send Buffer Size", uLink.Network.config.sendBufferSize));
				uLink.Network.config.receiveBufferSize = Mathf.Max(0, EditorGUILayout.IntField("Receive Buffer Size", uLink.Network.config.receiveBufferSize));
				uLink.Network.config.maximumTransmissionUnit = Mathf.Max(0, EditorGUILayout.IntField("Maximum Transmission Unit", uLink.Network.config.maximumTransmissionUnit));
				uLink.Network.config.timeBetweenPings = Mathf.Max(0, EditorGUILayout.FloatField("Time Between Pings", uLink.Network.config.timeBetweenPings));
				uLink.Network.config.timeoutDelay = Mathf.Max(0, EditorGUILayout.FloatField("Timeout Delay", uLink.Network.config.timeoutDelay));
				uLink.Network.config.handshakeRetriesMaxCount = Mathf.Max(0, EditorGUILayout.IntField("Maximum Handshake Retries", uLink.Network.config.handshakeRetriesMaxCount));
				uLink.Network.config.handshakeRetryDelay = Mathf.Max(0, EditorGUILayout.FloatField("Handshake Retry Delay", uLink.Network.config.handshakeRetryDelay));
				uLink.Network.config.batchSendAtEndOfFrame = EditorGUILayout.Toggle("Batch Send At End Of Frame", uLink.Network.config.batchSendAtEndOfFrame);

				EndFoldout();
			}

			EditorGUILayout.EndScrollView();

			GUILayout.FlexibleSpace();
			Utility.HorizontalBar();
			GUILayout.Space(6);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(12);
			if (GUILayout.Button("Reset", GUILayout.Width(75), GUILayout.Height(21)))
			{
				Utility.ResetPrefs();
			}
			GUILayout.FlexibleSpace();

			EditorGUILayout.BeginVertical();
			GUILayout.Space(7);
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				GUI.enabled = false;
				GUILayout.Toggle(useAutoSave, "Auto Save");
				GUI.enabled = true;
			}
			else
			{
				bool oldGUIChanged = GUI.changed;
				GUI.changed = false;

				useAutoSave = GUILayout.Toggle(useAutoSave, "Auto Save");

				if (GUI.changed)
				{
					EditorPrefs.SetBool("uLinkSettingsEditor.useAutoSave", useAutoSave);
				}
				else
				{
					GUI.changed = oldGUIChanged;
				}
			}
			EditorGUILayout.EndVertical();

			GUILayout.Space(8);
			if (Utility.IsPrefsDirty())
			{
				if (GUILayout.Button("Save now", GUILayout.Width(75), GUILayout.Height(21)))
				{
					Utility.SavePrefs();
				}
			}
			else
			{
				GUI.enabled = false;
				GUILayout.Button("Saved", GUILayout.Width(75), GUILayout.Height(21));
				GUI.enabled = true;
			}
			GUILayout.Space(8);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(12);

			if (GUI.changed)
			{
				lastAutoSaveTime = EditorApplication.timeSinceStartup;
			}
		}

		private static bool BeginFoldout(ref bool foldout, string title)
		{
			foldout = EditorGUILayout.Foldout(foldout, title);

			if (foldout)
			{
				EditorGUI.indentLevel = INDENT_BASE_LEVEL + 2;
			}

			return foldout;
		}

		private static void EndFoldout()
		{
			GUILayout.Space(2);
			EditorGUI.indentLevel = INDENT_BASE_LEVEL;
		}

		private class EmulationPreset
		{
			public static readonly string[] NAMES =
			{
				"Off",
				"Broadband",
				"DSL",
				"ISDN",
				"Dial-Up",
				"Local WiFi",
				"3G",
				"GPRS",
				"Custom",
			};

			private static readonly EmulationPreset[] PRESETS =
			{
				new EmulationPreset(0, 0, 0, 0, 0), 
				new EmulationPreset(1000, 0.0002f, 0.0002f, 0.005f, 0.01f), 
				new EmulationPreset(256, 0.01f, 0.01f, 0.01f, 0.07f), 
				new EmulationPreset(64, 0.01f, 0.01f, 0.05f, 0.1f), 
				new EmulationPreset(56, 0.01f, 0.01f, 0.2f, 0.4f), 
				new EmulationPreset(6750, 0.1f, 0.1f, 0.002f, 0.007f), 
				new EmulationPreset(2000, 0.2f, 0.2f, 0.09f, 0.2f), 
				new EmulationPreset(110, 0.2f, 0.2f, 0.5f, 1f), 
			};

			private readonly float maxBandwidth;
			private readonly float chanceOfLoss;
			private readonly float chanceOfDuplicates;
			private readonly float minLatency;
			private readonly float maxLatency;

			private EmulationPreset(float maxBandwidth, float chanceOfLoss, float chanceOfDuplicates, float minLatency, float maxLatency)
			{
				this.maxBandwidth = maxBandwidth;
				this.chanceOfLoss = chanceOfLoss;
				this.chanceOfDuplicates = chanceOfDuplicates;
				this.minLatency = minLatency;
				this.maxLatency = maxLatency;
			}

			private bool Equals(NetworkEmulation emulation)
			{
				return
					emulation.maxBandwidth == maxBandwidth &&
					emulation.chanceOfLoss == chanceOfLoss &&
					emulation.chanceOfDuplicates == chanceOfDuplicates &&
					emulation.minLatency == minLatency &&
					emulation.maxLatency == maxLatency;
			}

			private void Apply()
			{
				uLink.Network.emulation.maxBandwidth = maxBandwidth;
				uLink.Network.emulation.chanceOfLoss = chanceOfLoss;
				uLink.Network.emulation.chanceOfDuplicates = chanceOfDuplicates;
				uLink.Network.emulation.minLatency = minLatency;
				uLink.Network.emulation.maxLatency = maxLatency;
			}

			public static int Find()
			{
				var emulation = uLink.Network.emulation;

				for (int i = 0; i < PRESETS.Length; i++)
				{
					if (PRESETS[i].Equals(emulation)) return i;
				}

				return PRESETS.Length;
			}

			public static void Apply(int preset)
			{
				if (preset < PRESETS.Length)
				{
					PRESETS[preset].Apply();
				}
			}
		}
	}
}
