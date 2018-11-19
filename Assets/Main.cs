using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
	public static Main Instance;

	public string IP;
	public int Port;
	public GameObject PlayerPrefab;

	[Space]
	/// <summary>
	/// 在Server上配置，Client连接时同步给Client
	/// </summary>
	public GlobalParameter Global = new GlobalParameter();

	private RoleType m_RoleType;
	private GameProgress m_GameProgress;

	private List<Player> m_Players;

	public void RegisterPlayer(Player player)
	{
		m_Players.Add(player);
	}

	public void UnRegisterPlayer(Player player)
	{
		m_Players.Remove(player);
	}

	protected void Awake()
	{
		Instance = this;
		Application.runInBackground = true;
	}

	protected void OnDestroy()
	{
		StopGame();
		Instance = null;
	}

	protected void OnGUI()
	{
		switch (m_GameProgress)
		{
			case GameProgress.None:
				if (GUILayout.Button("Host Server"))
				{
					m_RoleType = RoleType.Server;
					StartGame();
				}
				if (GUILayout.Button("Join Game"))
				{
					m_RoleType = RoleType.Client;
					StartGame();
				}
				break;
			case GameProgress.Gaming:
				GUILayout.BeginHorizontal();
				GUILayout.Box(m_RoleType.ToString() + ": ");
				if (GUILayout.Button("Stop Game"))
				{
					StopGame();
					return;
				}
				GUILayout.EndHorizontal();

				GUILayout.Box("Min Latency: " + (int)(uLink.Network.emulation.minLatency * 1000.0f));
				uLink.Network.emulation.minLatency = GUILayout.HorizontalSlider(uLink.Network.emulation.minLatency * 1000.0f
					, 0.0f, 800.0f
					, GUILayout.Width(Screen.width - 10.0f)) * 0.001f;
				if (uLink.Network.emulation.maxLatency < uLink.Network.emulation.minLatency)
				{
					uLink.Network.emulation.maxLatency = uLink.Network.emulation.minLatency;
				}
				GUILayout.Box("Min Latency: " + (int)(uLink.Network.emulation.maxLatency * 1000.0f));
				uLink.Network.emulation.maxLatency = GUILayout.HorizontalSlider(uLink.Network.emulation.maxLatency* 1000.0f
					, 0.0f, 800.0f
					, GUILayout.Width(Screen.width - 10.0f)) * 0.001f;
				if (uLink.Network.emulation.minLatency > uLink.Network.emulation.maxLatency)
				{
					uLink.Network.emulation.minLatency = uLink.Network.emulation.maxLatency;
				}

				GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
				GUILayout.BeginHorizontal();
				GUILayout.BeginVertical();
				GUILayout.FlexibleSpace();

				#region LeftDown
				for (int iPlayer = 0; iPlayer < m_Players.Count; iPlayer++)
				{
					m_Players[iPlayer].DoGUI();
				}
				#endregion

				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
				break;
		}
	}

	private void StartGame()
	{
		gameObject.name = "Main_" + m_RoleType.ToString();
		m_Players = new List<Player>();
		switch (m_RoleType)
		{
			case RoleType.Server:
				InitializeServer();
				break;
			case RoleType.Client:
				InitializeClient();
				break;
		}
		m_GameProgress = GameProgress.Gaming;
	}

	private void StopGame()
	{
		m_GameProgress = GameProgress.None;
		uLink.Network.Disconnect();
		m_Players = null;
	}

	#region Server
	private void InitializeServer()
	{
		uLink.Network.InitializeServer(Global.MaximumConnectionCount, Port);

		Time.fixedDeltaTime = 1.0f / Global.ServerUpdateRate;
		Time.maximumDeltaTime = Time.fixedDeltaTime;
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = Global.ServerUpdateRate;
		uLink.Network.sendRate = Global.ServerUpdateRate;
	}

	private void uLink_OnPlayerApproval(uLink.NetworkPlayerApproval approval)
	{
		Debug.Log("uLink_OnPlayerApproval");
		uLink.BitStream approvalData = new uLink.BitStream(true);
		approvalData.WriteObject<GlobalParameter>(Global);
		approval.Approve(approvalData);
	}

	protected void uLink_OnPlayerConnected(uLink.NetworkPlayer player)
	{
		Debug.Log("uLink_OnPlayerConnected");

		uLink.BitStream stream = new uLink.BitStream(true);
		stream.WriteInt32(player.id);
		uLink.Network.Instantiate(player, PlayerPrefab, Vector3.zero, Quaternion.identity, 0, stream);
	}
	#endregion

	#region Client
	private void InitializeClient()
	{
		uLink.Network.Connect(IP, Port);
	}

	protected void uLink_OnConnectedToServer()
	{
		Debug.Log("uLink_OnConnectedToServer");
		uLink.BitStream approvalData = uLink.Network.approvalData;
		Global = approvalData.ReadObject<GlobalParameter>();

		Time.fixedDeltaTime = 1.0f / Global.ClientUpdateRate;
		Time.maximumDeltaTime = Time.fixedDeltaTime;
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = Global.ClientUpdateRate;
		uLink.Network.sendRate = Global.ClientUpdateRate;
	}
	#endregion

	public enum RoleType
	{
		Client,
		Server,
	}

	public enum GameProgress
	{
		None,
		Gaming,
	}

	[System.Serializable]
	public class GlobalParameter
	{
		[Header("Server")]
		public int ServerUpdateRate = 7;
		public int MaximumConnectionCount = 16;
		public float InputBufferTime = 0.3f;

		[Header("Client")]
		public int ClientUpdateRate = 30;
		public float LerpDelay = 0.5f;

		[Header("Both")]
		public float MoveSpeed = 10.0f;
		public float AirWallWidth = 15.0f;
		public float AirWallHeight = 15.0f;
	}
}