// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	public class StatisticsViewer : EditorWindow
	{
		[SerializeField]
		private Vector2 scrollPosition;

		[SerializeField]
		private double lastRepaintTime;

		private const double REPAINT_INTERVAL = 0.1;

		private const float WINDOW_MARGIN_X = 10;
		private const float WINDOW_MARGIN_Y = 10;

		private const float COLUMN_WIDTH = 150;

		[SerializeField]
		private bool showDetails = false;

		[SerializeField]
		private bool isInitialized = false;

		[MenuItem("uLink/View Statistics...", false, 300)]
		public static void OnMenu()
		{
			GetWindow<StatisticsViewer>(false, "uLink Statistics", true).Show(true);
		}

		private void Init()
		{
			isInitialized = true;

			showDetails = EditorPrefs.GetBool("uLinkStatisticsViewer.showDetails", showDetails);

			Show(true);
		}

		protected void Update()
		{
			if (EditorApplication.timeSinceStartup - lastRepaintTime >= REPAINT_INTERVAL)
			{
				Repaint();
			}
		}

		protected void OnGUI()
		{
			if (!isInitialized) Init();

			EditorGUIUtility.LookLikeInspector();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			GUILayout.BeginHorizontal();
			GUILayout.Space(WINDOW_MARGIN_X);
			GUILayout.BeginVertical();
			GUILayout.Space(WINDOW_MARGIN_Y);

			GUILayout.BeginVertical();
			StatisticsGUI();
			GUILayout.EndVertical();

			GUILayout.Space(10);
			GUILayout.EndVertical();
			GUILayout.Space(6);
			GUILayout.EndHorizontal();

			EditorGUILayout.EndScrollView();

			GUILayout.FlexibleSpace();
			Utility.HorizontalBar();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUILayout.BeginVertical();
			GUILayout.Space(6);
			showDetails = GUILayout.Toggle(showDetails, "Show Details");
			if (GUI.changed)
			{
				EditorPrefs.SetBool("uLinkStatisticsViewer.showDetails", showDetails);
			}
			GUILayout.EndVertical();

			GUILayout.Space(8);
			GUILayout.EndHorizontal();

			GUILayout.Space(8);

			lastRepaintTime = EditorApplication.timeSinceStartup;
		}

		private void StatisticsGUI()
		{
			uLink.NetworkPlayer[] connections = uLink.Network.connections;
			uLink.NetworkView[] networkViews = uLink.Network.networkViews;

			GUILayout.BeginVertical("Box");

			GUILayout.BeginHorizontal();
			GUILayout.Label("Frame Rate:", GUILayout.Width(COLUMN_WIDTH));
			if (Application.isPlaying)
			{
				GUILayout.Label(Mathf.RoundToInt(1.0f / Time.smoothDeltaTime).ToString(CultureInfo.InvariantCulture) + " FPS");
			}
			else
			{
				GUILayout.Label("N/A");
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Status:", GUILayout.Width(COLUMN_WIDTH));
			GUILayout.Label(uLink.NetworkUtility.GetStatusString(uLink.Network.peerType, uLink.Network.status));
			GUILayout.EndHorizontal();

			var listenEndPoint = uLink.Network.listenEndPoint;
			if (!listenEndPoint.isUnassigned)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Listen EndPoint:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label(listenEndPoint.ToRawString());
				GUILayout.EndHorizontal();
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label("Last Error:", GUILayout.Width(COLUMN_WIDTH));
			GUILayout.Label(uLink.NetworkUtility.GetErrorString(uLink.Network.lastError));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Network Time:", GUILayout.Width(COLUMN_WIDTH));
			GUILayout.Label(uLink.Network.time.ToString(CultureInfo.InvariantCulture) + " s");
			GUILayout.EndHorizontal();

			if (showDetails)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Server Time Offset:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label((uLink.Network.config.serverTimeOffset).ToString(CultureInfo.InvariantCulture) + " s");
				GUILayout.EndHorizontal();
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label("Network Objects:", GUILayout.Width(COLUMN_WIDTH));
			GUILayout.Label(networkViews.Length.ToString(CultureInfo.InvariantCulture));
			GUILayout.EndHorizontal();

			if (uLink.Network.isServer)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Connections:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label(connections.Length.ToString(CultureInfo.InvariantCulture));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Name in Master Server:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label(uLink.MasterServer.isRegistered ? uLink.MasterServer.gameName : "Not Registered");
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();

			foreach (var player in connections)
			{
				uLink.NetworkStatistics stats = player.statistics;
				if (stats == null) continue;

				GUILayout.BeginVertical("Box");

				GUILayout.BeginHorizontal();
				GUILayout.Label("Player:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label(player.ToString());
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Ping (average):", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label(player.lastPing + " (" + player.averagePing + ") ms");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Sent:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label((int)stats.bytesSentPerSecond + " B/s");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Received:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label((int)stats.bytesReceivedPerSecond + " B/s");
				GUILayout.EndHorizontal();

				if (showDetails)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("Packets sent:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.packetsSent.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Packets received:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.packetsReceived.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Messages sent:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messagesSent.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Messages received:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messagesReceived.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Messages resent:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messagesResent.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Messages unsent:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messagesUnsent.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Messages stored:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messagesStored.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Messages withheld:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messagesWithheld.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Msg duplicates rejected:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messageDuplicatesRejected.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Msg sequences rejected:", GUILayout.Width(COLUMN_WIDTH));
					GUILayout.Label(stats.messageSequencesRejected.ToString(CultureInfo.InvariantCulture));
					GUILayout.EndHorizontal();
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("Encryption:", GUILayout.Width(COLUMN_WIDTH));
				GUILayout.Label(player.securityStatus.ToString());
				GUILayout.EndHorizontal();

				GUILayout.EndVertical();
			}
		}
	}
}
