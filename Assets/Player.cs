using UnityEngine;
using System.Collections.Generic;

public class Player : uLink.MonoBehaviour
{
	#region public Both
	public MeshRenderer BodyRenderer;
	public Material BodyMaterials_Local;
	public Material BodyMaterials_Remote;
	#endregion

	#region Both
	private uLink.NetworkView m_NetworkView;
	private uLink.NetworkPlayer m_NetworkPlayer;

	/// <summary>
	/// UNDONE List效率肯定不好，DEMO就无所谓了，要在项目中用的时候再自己实现一个dequeue
	/// </summary>
	private List<UCMD> m_UCMDs;
	private uLink.BitStream m_StreamCache;
	/// <summary>
	/// 最后一次Simulate的UCMD的ID
	/// Note：只有Server上Simulate的结果是Authoritative
	/// 
	/// 这个值也相当于，Client最后一次Report给Server的UCMDID
	/// 
	/// 这个值肯定小于<see cref="m_LastUCMDID"/>
	/// </summary>
	private int m_LastSimulateUCMDID = -1;
	private int m_PlayerID = -1;
	#endregion

	#region Server
	/// <summary>
	/// Server收到的最后一个UCMD的ID
	/// </summary>
	private int m_LastReceviedUCMDID = -1;
	/// <summary>
	/// UNDONE List效率肯定不好，DEMO就无所谓了，要在项目中用的时候再自己实现一个dequeue
	/// </summary>
	private List<Snapshot> m_Snapshots;
	/// <summary>
	/// 这个变量 = 上一帧Simulate UCMD的TotalTime - <see cref="Time.fixedDeltaTime"/>
	/// </summary>
	/// <remarks>
	/// Server的<see cref="Time.fixedDeltaTime"/>不一定是Client上报的UCMD的<see cref="UCMD.FixedDeltaTime"/>的整数倍
	/// 例：Server每帧是3秒，Client每帧是2秒
	///		那Server每帧应该应该Simulate 1个UCMD还是2个？
	///	解决方案是，让Server Simulate的UCMD的总时间和Server的FixedTime尽量接近，即：
	///		Server的第1帧Simulate 2个UCMD
	///				第2帧Simulate 1个UCMD
	///				第3帧Simulate 2个UCMD
	///				第4帧Simulate 1个UCMD
	///				……
	/// 
	///	这个变量就是用来实现这个的功能的
	/// </remarks>
	private float m_LastSimulateExtraTime = 0;
	private int m_LastFrameInputDuplicateCount_Debug = 0;
	private int m_TotalInputDuplicateCount_Debug = 0;
	/// <summary>
	/// Client刚连接时，Server上InputBuffer是空的，为了让玩家操作流畅，Server会等InputBuffer到一定程度后开始Simulate
	/// </summary>
	private bool m_BeginSimulate = false;
	#endregion

	#region Owner Client
	private Camera m_Camera;
	private InputManager m_Input;
	/// <summary>
	/// 收到Server发来的最后一个Authoritative的UCMD的ID
	/// </summary>
	private int m_LastAckUCMDID = -1;
	/// <summary>
	/// 最后分配UCMD的ID
	/// </summary>
	private int m_LastUCMDID = -1;
	/// <summary>
	/// 是否需要回滚到<see cref="m_LastReceviedAckMessage"/>
	/// 当前帧收到Server发的<see cref="RpcNotifyAckMessage_S2C"/>时值为 true，否则 false
	/// </summary>
	private bool m_NeedRollbackToLastReceviedAckMessage = false;
	private AckMessage m_LastReceviedAckMessage;
	private bool m_LossReportUCMD_Debug = false;
	private int m_LastFrameCorrectionUCMDCount_Debug = 0;
	private int m_TotalCorrectionUCMDCount_Debug = 0;
	#endregion

	#region Other Client
	private List<Snapshot> m_SnapshotsInOtherClient;
	private int m_LeftSnapshotIdx = -1;
	private float m_LastLerpFixedTime = -1;
	#endregion

	public void DoGUI()
	{
		if (uLink.Network.isServer)
		{
			GUI.color = Color.red;
			GUILayout.Box("Remote: " + gameObject.name);
			GUI.color = Color.white;

			GUILayout.Box("Recevied UCMD: " + m_LastReceviedUCMDID);
			GUILayout.Box("Simulate UCMD: " + m_LastSimulateUCMDID);
			GUILayout.Box("UCMD Cahche: " + (m_LastReceviedUCMDID - m_LastSimulateUCMDID));
			GUILayout.Box("Last Input Duplicate: " + m_LastFrameInputDuplicateCount_Debug);
			GUILayout.Box("Total Input Duplicate: " + m_TotalInputDuplicateCount_Debug);
			GUILayout.Box(string.Format("Ping: {0}", uLink.Network.GetLastPing(m_NetworkPlayer)));
		}
		else if (uLink.Network.isClient)
		{
			if (m_NetworkView.isOwner)
			{
				GUI.color = Color.green;
				GUILayout.Box("Local: " + gameObject.name);
				GUI.color = Color.white;

				GUILayout.Box("LastAckUCMDID: " + m_LastAckUCMDID);
				GUILayout.Box("LastSimulateUCMDID: " + m_LastSimulateUCMDID);
				GUILayout.Box("Prediction UCMD: " + (m_LastSimulateUCMDID - m_LastAckUCMDID));
				GUILayout.Box(string.Format("Ping: {0}", uLink.Network.GetLastPing(uLink.NetworkPlayer.server)));
				GUILayout.Box("Loss ReportUCMD(Q): " + m_LossReportUCMD_Debug);
				GUILayout.Box("Last Correction UCMD: " + m_LastFrameCorrectionUCMDCount_Debug);
				GUILayout.Box("Total Correction UCMD: " + m_TotalCorrectionUCMDCount_Debug);
			}
			else
			{
				GUI.color = Color.red;
				GUILayout.Box("Remote: " + gameObject.name);
				GUI.color = Color.white;

				GUILayout.Box("Left Snapshot: " + m_LeftSnapshotIdx);
			}
		}
	}

	protected void OnDestroy()
	{
		if (Main.Instance) // Stop Application时，Instance可能是null
		{
			Main.Instance.UnRegisterPlayer(this);
		}

		m_StreamCache = new uLink.BitStream(true);
		m_UCMDs = null;

		if (uLink.Network.isServer)
		{
			m_Snapshots = null;
		}
		else if (uLink.Network.isClient)
		{
			if (m_NetworkView.isOwner)
			{
				m_Input = null;
				m_Camera = null;
			}
			else
			{
				m_SnapshotsInOtherClient = null;
			}
		}

		m_NetworkView = null;
	}

	protected void FixedUpdate()
	{
		if (uLink.Network.isClient)
		{
			if (m_NetworkView.isOwner)
			{
				FixedUpdate_OwnerClient();
			}
			else
			{
				FixedUpdate_OtherClient();
			}
		}
		else if (uLink.Network.isServer)
		{
			FixedUpdate_Server();
		}
	}

	protected void LateUpdate()
	{
		#region Debug
		if (uLink.Network.isClient)
		{
			if (m_NetworkView.isOwner)
			{
				m_LossReportUCMD_Debug = m_Input.GetQKeyDown()
					? !m_LossReportUCMD_Debug
					: m_LossReportUCMD_Debug;
			}
		}
		#endregion

		if (m_Camera != null)
		{
			Vector3 playerPosition = transform.position;
			m_Camera.transform.position = new Vector3(playerPosition.x, 10, playerPosition.z);
		}
	}

	#region Owner Client
	private void uLink_OnDisconnectedFromServer(uLink.NetworkDisconnection mode)
	{
		Debug.Log("uLink_OnDisconnectedFromServer " + gameObject.name);
	}

	private void FixedUpdate_OwnerClient()
	{
		#region Replicate
		m_LastFrameCorrectionUCMDCount_Debug = 0;
		if (m_NeedRollbackToLastReceviedAckMessage)
		{
			m_NeedRollbackToLastReceviedAckMessage = false;

			// UNDONE Client的UCMDID可能和Server下发的AckUCMDID对应不上，例如以下情况：
			// 玩家切到后台再切回游戏时 Client的UCMDID会小于 Server下发的AckMessage的UCMDID
			// 玩家用加速齿轮或其他作弊方式，加速游戏后，Client的UCMDID远远大于AckMessageUCMDID
			//
			// 解决方案：
			// 方案A：修正Client的UCMDID —— 根据AckUCMDID和RTT时间算出正确的UCMDID
			// 方案B：缩放FixedUpdate的频率
			//		Q：修改固定的频率(硬编码写死)？还是根据AckUCMDID - Client的UCMDID计算频率

			#region 方案A
			// Server下发的AckUCMDID大于Client最后一次上报的UCMDID，玩家可能是切到后台后切回游戏，也可能是某种作弊方式
			if (m_LastReceviedAckMessage.ACKUCMD.UCMDID > m_LastSimulateUCMDID)
			{
				m_LastFrameCorrectionUCMDCount_Debug++;

				// 根据AckUCMDID和RTT时间算出正确的UCMDID
				int expectUCMDID = m_LastAckUCMDID
					+ Mathf.CeilToInt(2.0f // UNDONE 为什么乘2？原因待查，猜测ulink的Ping是单向的不是往返的
						* (uLink.Network.GetLastPing(uLink.NetworkPlayer.server) * 0.001f + 1.0f / Main.Instance.Global.ServerUpdateRate)
						/ m_LastReceviedAckMessage.ACKUCMD.FixedDeltaTime);

				while (m_LastUCMDID < expectUCMDID) // UNDONE 优化性能
				{
					UCMD iterUCMD = m_LastReceviedAckMessage.ACKUCMD;
					iterUCMD.UCMDID = ++m_LastUCMDID;
					m_UCMDs.Add(iterUCMD);
				}
			}
			#endregion

			DoReplicate(m_LastReceviedAckMessage.MySnapshot);
			m_LastAckUCMDID = m_LastReceviedAckMessage.ACKUCMD.UCMDID;
			m_LastSimulateUCMDID = m_LastAckUCMDID;
		}
		m_TotalCorrectionUCMDCount_Debug += m_LastFrameCorrectionUCMDCount_Debug;
		#endregion

		#region Simulate(Prediction)
		UCMD currentFrameUCMD = new UCMD();
		currentFrameUCMD.Axis = m_Input.GetAsix();
		currentFrameUCMD.UCMDID = ++m_LastUCMDID;
		currentFrameUCMD.FixedDeltaTime = Time.fixedDeltaTime;
		m_UCMDs.Add(currentFrameUCMD);

		// send all unacknowledge ucmd to server
		int unackUCMDCount = m_UCMDs.Count - (m_LastAckUCMDID + 1);
		if (unackUCMDCount > 0)
		{
			m_StreamCache.ResetBuffer();
			m_StreamCache.WriteByte((byte)unackUCMDCount); // UNDONE 限制发送UCMD的数量，因为Server在FixedUpdate时如果没有Cache的UCMD，则会Input Duplicate，CLient的UCMD就无效了
			for (int iUCMD = m_LastAckUCMDID + 1; iUCMD < m_UCMDs.Count; iUCMD++)
			{
				UCMD iterUCMD = m_UCMDs[iUCMD];
				iterUCMD.WriteTo(m_StreamCache);
				if (iUCMD > m_LastSimulateUCMDID)
				{
					DoSimulate(iterUCMD);
					m_LastSimulateUCMDID = iterUCMD.UCMDID;
				}
			}
			if (!m_LossReportUCMD_Debug)
			{
				m_NetworkView.UnreliableRPC("RpcReportUCMD_C2S", uLink.RPCMode.Server, m_StreamCache);
			}
		}
		#endregion
	}

	[RPC]
	private void RpcNotifyAckMessage_S2C(uLink.BitStream stream)
	{
		AckMessage ackMessage = new AckMessage();
		ackMessage.ReadFrom(stream);
		if (ackMessage.ACKUCMD.UCMDID > m_LastReceviedAckMessage.ACKUCMD.UCMDID)
		{
			m_LastReceviedAckMessage = ackMessage;
			m_NeedRollbackToLastReceviedAckMessage = true;
		}
	}
	#endregion

	#region Other Client
	private void FixedUpdate_OtherClient()
	{
		LerpType lerpType;
		// not received snapshot
		if (m_SnapshotsInOtherClient.Count == 0)
		{
			lerpType = LerpType.None;
		}
		// first sync
		else if (m_LeftSnapshotIdx < 0)
		{
			m_LeftSnapshotIdx = 0;
			lerpType = LerpType.Replicate;
		}
		// wait first interpolate delay 
		else if (m_LeftSnapshotIdx == 0)
		{
			// UNDONE 这种情况应该特殊处理，参考osNetworkTransformReplicator.SyncState.SimulateTimeTooEarlyToInterpolate
			// 但毕竟是Demo，就简单处理了
			float rightTime = m_SnapshotsInOtherClient[m_SnapshotsInOtherClient.Count - 1].FixedTime;
			lerpType = rightTime - m_LastLerpFixedTime >= Main.Instance.Global.LerpDelay
				? LerpType.Interpolate
				: LerpType.None;
		}
		else
		{
			float rightTime = m_SnapshotsInOtherClient[m_SnapshotsInOtherClient.Count - 1].FixedTime;
			lerpType = rightTime - m_LastLerpFixedTime >= Time.fixedDeltaTime
				? LerpType.Interpolate
				: LerpType.Extrapolate;
		}

		switch (lerpType)
		{
			case LerpType.None:
				break;
			case LerpType.Replicate:
				Snapshot replicateSnapshot = m_SnapshotsInOtherClient[m_LeftSnapshotIdx];
				DoReplicate(replicateSnapshot);
				m_LastLerpFixedTime = replicateSnapshot.FixedTime;
				break;
			case LerpType.Interpolate:
				{
					int rightSnapshotIdx = m_LeftSnapshotIdx + 1;
					float currentSyncFixedTime = m_LastLerpFixedTime + Time.fixedDeltaTime;
					while (m_SnapshotsInOtherClient[rightSnapshotIdx].FixedTime < currentSyncFixedTime)
					{
						rightSnapshotIdx++;
						if (rightSnapshotIdx >= m_SnapshotsInOtherClient.Count)
						{
							//#if UNITY_EDITOR
							Debug.LogError("不应该进这个分支，应该程序逻辑写错了");
							//#endif
							break;
						}
					}
					m_LeftSnapshotIdx = rightSnapshotIdx - 1;
					Snapshot leftSnapshot = m_SnapshotsInOtherClient[m_LeftSnapshotIdx];
					Snapshot rightSnapshot = m_SnapshotsInOtherClient[rightSnapshotIdx];
					Snapshot targetSnapshot = Snapshot.LerpWithFixedTime(leftSnapshot, rightSnapshot, currentSyncFixedTime);
					DoReplicate(targetSnapshot);

					m_LastLerpFixedTime = currentSyncFixedTime;
				}
				break;
			case LerpType.Extrapolate:
				{
					float currentSyncFixedTime = m_LastLerpFixedTime + Time.fixedDeltaTime;
					Snapshot leftSnapshot = m_SnapshotsInOtherClient[m_SnapshotsInOtherClient.Count - 2];
					Snapshot rightSnapshot = m_SnapshotsInOtherClient[m_SnapshotsInOtherClient.Count - 1];
					Snapshot targetSnapshot = Snapshot.LerpWithFixedTime(leftSnapshot, rightSnapshot, currentSyncFixedTime);
					DoReplicate(targetSnapshot);

					m_LeftSnapshotIdx = m_SnapshotsInOtherClient.Count - 1;
					m_LastLerpFixedTime = currentSyncFixedTime;
				}
				break;
			default:
#if UNITY_EDITOR
				Debug.LogError("意外的SyncType: " + lerpType.ToString());
#endif
				break;
		}
	}

	[RPC]
	private void RpcNotifySnapshot_S2C(uLink.BitStream stream)
	{
		Snapshot snapshot = new Snapshot();
		snapshot.ReadFrom(stream);

		if (m_SnapshotsInOtherClient.Count == 0 // 第一个收到的Snapshot
			|| snapshot.FixedTime > m_SnapshotsInOtherClient[m_SnapshotsInOtherClient.Count - 1].FixedTime) // 是上一个收到的Snapshot的后续)
		{
			m_SnapshotsInOtherClient.Add(snapshot);
		}
	}
	#endregion

	#region Server
	private void uLink_OnPlayerDisconnected(uLink.NetworkPlayer player)
	{
		Debug.Log("uLink_OnPlayerDisconnected " + gameObject.name);
		uLink.Network.DestroyPlayerObjects(player);
		uLink.Network.RemoveRPCs(player);
		uLink.Network.RemoveInstantiates(player);
	}

	private void FixedUpdate_Server()
	{
		// 还未收到客户端发来的UCMD，当玩家还未Connected处理
		// 此时玩家单位会静止不动，不过持续时间不会超过半个RTT
		if (!m_BeginSimulate)
		{
			float inputBufferTime = 0;
			for (int iUCMD = 0; iUCMD < m_UCMDs.Count; iUCMD++)
			{
				inputBufferTime += m_UCMDs[iUCMD].FixedDeltaTime;
			}
			m_BeginSimulate = inputBufferTime >= Main.Instance.Global.InputBufferTime;
		}

		if (!m_BeginSimulate)
		{
			return;
		}

		#region Simulate
		// UNDONE 客户端作弊加速游戏时（FixedUpdate的deltaTime不变，但是FixedUpdate频率增加），导致上报的UCMD增加
		// 当Server发现玩家的UCMD Buffer full的时候，把玩家当作弊处理

		float fixedDeltaTime = Time.fixedDeltaTime - m_LastSimulateExtraTime; // 当前帧需要Simulate的时间
		float currentFrameTotalSimulateTime = 0; // 当前帧已经Simulate的时间
		for (int iUCMD = m_LastSimulateUCMDID + 1; iUCMD < m_UCMDs.Count; iUCMD++)
		{
			if (currentFrameTotalSimulateTime >= fixedDeltaTime)
			{
				break;
			}

			UCMD iterUCMD = m_UCMDs[iUCMD];
			DoSimulate(iterUCMD);
			currentFrameTotalSimulateTime += iterUCMD.FixedDeltaTime;
			m_LastSimulateUCMDID = iterUCMD.UCMDID;
		}

		#region Input Duplicate
		UCMD lastUCMD = m_UCMDs[m_UCMDs.Count - 1];
		m_LastFrameInputDuplicateCount_Debug = 0;
		while (currentFrameTotalSimulateTime < fixedDeltaTime)
		{
			m_LastFrameInputDuplicateCount_Debug++;

			lastUCMD.UCMDID++;
			DoSimulate(lastUCMD);
			m_LastSimulateUCMDID = lastUCMD.UCMDID;
			currentFrameTotalSimulateTime += lastUCMD.FixedDeltaTime;

			UCMD iterUCMD = lastUCMD;
			m_UCMDs.Add(iterUCMD);
		}
		m_TotalInputDuplicateCount_Debug += m_LastFrameInputDuplicateCount_Debug;
		#endregion End Input Duplicate

		m_LastSimulateExtraTime = currentFrameTotalSimulateTime - fixedDeltaTime;
		#endregion End Simulate

		Snapshot snapshot = TakeSnapshot();
		m_Snapshots.Add(snapshot);

		// Notify to Owner
		m_StreamCache.ResetBuffer();
		AckMessage ackMessage = new AckMessage();
		ackMessage.ACKUCMD = m_UCMDs[m_LastSimulateUCMDID];
		ackMessage.MySnapshot = snapshot;
		ackMessage.WriteTo(m_StreamCache);
		m_NetworkView.UnreliableRPC("RpcNotifyAckMessage_S2C", uLink.RPCMode.Owner, m_StreamCache);

		// Notify to Other
		m_StreamCache.ResetBuffer();
		snapshot.WriteTo(m_StreamCache);
		m_NetworkView.UnreliableRPC("RpcNotifySnapshot_S2C", uLink.RPCMode.OthersExceptOwner, m_StreamCache);
	}

	[RPC]
	private void RpcReportUCMD_C2S(uLink.BitStream stream)
	{
		int receiveCount = stream.ReadByte();
		for (int iUCMD = 0; iUCMD < receiveCount; iUCMD++)
		{
			UCMD iterUCMD = new UCMD();
			iterUCMD.ReadFrom(stream);

			if (iterUCMD.UCMDID < m_UCMDs.Count)
			{
				// Client为了防止丢包会冗余发UCMD
				m_UCMDs[iterUCMD.UCMDID] = iterUCMD;
			}
			else if (iterUCMD.UCMDID == m_UCMDs.Count)
			{
				m_UCMDs.Add(iterUCMD);
			}
			else
			{
				UCMD lastUCMD = m_UCMDs[m_UCMDs.Count - 1];
				while (iterUCMD.UCMDID >= m_UCMDs.Count)
				{
					lastUCMD.UCMDID++;
					m_UCMDs.Add(lastUCMD);
				}
				m_UCMDs.Add(iterUCMD);
			}
		}

		m_LastReceviedUCMDID = m_UCMDs.Count - 1;
	}
	#endregion

	#region Both
	private void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
	{
		uLink.BitStream initialData = info.networkView.initialData;
		m_PlayerID = initialData.ReadInt32();
		Debug.Log("uLink_OnNetworkInstantiate " + gameObject.name);

		m_NetworkView = info.networkView;
		uLink.NetworkPlayer[] players = uLink.Network.connections;
		for (int iPlayer = 0; iPlayer < players.Length; iPlayer++)
		{
			uLink.NetworkPlayer iterPlayer = players[iPlayer];
			if (iterPlayer != null && iterPlayer.id == m_PlayerID)
			{
				m_NetworkPlayer = iterPlayer;
				break;
			}
		}

		if (uLink.Network.isServer)
		{
			gameObject.name = string.Format("Player_Remote_{0}", m_PlayerID);
			BodyRenderer.material = BodyMaterials_Remote;

			m_Snapshots = new List<Snapshot>();
		}
		else if (uLink.Network.isClient)
		{
			if (m_NetworkView.isOwner)
			{
				gameObject.name = string.Format("Player_Local_{0}", m_PlayerID);
				BodyRenderer.material = BodyMaterials_Local;

				m_Camera = FindObjectOfType<Camera>();
				m_Camera.transform.localEulerAngles = new Vector3(90, 0, 0);

				m_Input = gameObject.AddComponent<InputManager>();

				m_LastReceviedAckMessage.ACKUCMD.UCMDID = int.MinValue;
			}
			else
			{
				gameObject.name = string.Format("Player_Remote_{0}", m_PlayerID);
				BodyRenderer.material = BodyMaterials_Remote;

				m_SnapshotsInOtherClient = new List<Snapshot>();
			}
		}

		m_StreamCache = new uLink.BitStream(true);
		m_UCMDs = new List<UCMD>();

		Main.Instance.RegisterPlayer(this);
	}

	private void DoReplicate(Snapshot snapshot)
	{
		transform.position = snapshot.Position;
		transform.rotation = snapshot.Rotation;
	}

	private void DoSimulate(UCMD ucmd)
	{
		Vector3 position = transform.position;
		Vector3 moveToPosition = new Vector3(position.x + ucmd.Axis.x * ucmd.FixedDeltaTime * Main.Instance.Global.MoveSpeed
			, position.y
			, position.z + ucmd.Axis.y * ucmd.FixedDeltaTime * Main.Instance.Global.MoveSpeed);
		Vector3 canMoveToPosition = new Vector3(moveToPosition.x > Main.Instance.Global.AirWallWidth
				? Main.Instance.Global.AirWallWidth
				: moveToPosition.x < -Main.Instance.Global.AirWallWidth
					? -Main.Instance.Global.AirWallWidth
					: moveToPosition.x
			, moveToPosition.y
			, moveToPosition.z > Main.Instance.Global.AirWallHeight
				? Main.Instance.Global.AirWallHeight
				: moveToPosition.z < -Main.Instance.Global.AirWallHeight
					? -Main.Instance.Global.AirWallHeight
					: moveToPosition.z);
		transform.position = canMoveToPosition;

		float angle = Mathf.Atan2(ucmd.Axis.x, ucmd.Axis.y) * Mathf.Rad2Deg;
		angle = angle < 0.0f ? angle + 360.0f : angle;
		transform.localEulerAngles = new Vector3(0, angle, 0);
	}

	private Snapshot TakeSnapshot()
	{
		Snapshot snapshot = new Snapshot();
		snapshot.FixedTime = Time.fixedTime;
		snapshot.Position = transform.position;
		snapshot.Rotation = transform.rotation;
		return snapshot;
	}
	#endregion

	/// <summary>
	/// UserCommand
	/// </summary>
	public struct UCMD
	{
		public int UCMDID;
		public Vector2 Axis;
		public float FixedDeltaTime;

		public void WriteTo(uLink.BitStream stream)
		{
			stream.WriteInt32(UCMDID);
			stream.WriteVector2(Axis);
			stream.WriteSingle(FixedDeltaTime);
		}

		public void ReadFrom(uLink.BitStream stream)
		{
			UCMDID = stream.ReadInt32();
			Axis = stream.ReadVector2();
			FixedDeltaTime = stream.ReadSingle();
		}
	}

	public struct AckMessage
	{
		public Snapshot MySnapshot;
		public UCMD ACKUCMD;

		public void WriteTo(uLink.BitStream stream)
		{
			ACKUCMD.WriteTo(stream);
			MySnapshot.WriteTo(stream);
		}

		public void ReadFrom(uLink.BitStream stream)
		{
			ACKUCMD.ReadFrom(stream);
			MySnapshot.ReadFrom(stream);
		}
	}

	public struct Snapshot
	{
		/// <summary>
		/// Server Simulate出这个Snapshot的ServerFixedTime
		/// </summary>
		public float FixedTime;
		public Vector3 Position;
		public Quaternion Rotation;

		public static Snapshot LerpWithFixedTime(Snapshot left, Snapshot right, float fixedTime)
		{
			Snapshot result = new Snapshot();
			result.FixedTime = fixedTime;
			float t = (fixedTime - left.FixedTime) / (right.FixedTime - left.FixedTime);
			result.Position = left.Position + (right.Position - left.Position) * t;
			result.Rotation = Quaternion.Lerp(left.Rotation, right.Rotation, t);
			return result;
		}

		public void WriteTo(uLink.BitStream stream)
		{
			stream.WriteSingle(FixedTime);
			stream.WriteVector3(Position);
			stream.WriteQuaternion(Rotation);
		}

		public void ReadFrom(uLink.BitStream stream)
		{
			FixedTime = stream.ReadSingle();
			Position = stream.ReadVector3();
			Rotation = stream.ReadQuaternion();
		}
	}

	public enum LerpType
	{
		None,
		Replicate,
		Interpolate,
		Extrapolate,
	}
}