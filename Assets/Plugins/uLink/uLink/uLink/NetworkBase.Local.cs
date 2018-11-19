#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12061 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-14 21:25:28 +0200 (Mon, 14 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Net;
using System.Collections.Generic;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	internal abstract partial class NetworkBaseLocal : NetworkBaseApp
	{
		public bool isMessageQueueRunning = true;

		public bool isAuthoritativeServer = false;

		public bool useDifferentStateForOwner = true;

		internal NetworkFlags _rpcFlags = NetworkFlags.Normal;

		internal NetworkStatus _status = NetworkStatus.Disconnected;
		internal NetworkPeerType _peerType = NetworkPeerType.Disconnected;
		internal NetworkDisconnection _disconnectionType;

		internal NetworkPlayer _localPlayer = NetworkPlayer.unassigned;
		internal double _timeToDisconnect = 0;

		public PublicKey publicKey;
		public PrivateKey privateKey;

		private readonly NetworkEmulation _emulation;
		private readonly NetworkConfig _config;

		private double _nextSyncState = 0;
		private float _intervalSyncState = 1.0f / 15.0f;

		private double _timeAtLastUpdate = Single.MaxValue;

		internal double _recyclingDelayForViewID = 30;
		internal double _recyclingDelayForPlayerID = 30;

		private readonly Dictionary<NetworkPlayer, BitStream> _userLoginData = new Dictionary<NetworkPlayer, BitStream>();

		private readonly ParameterWriterCache _rpcWriter = new ParameterWriterCache(false);
		private readonly ParameterWriterCache _initialWriter = new ParameterWriterCache(false);

		public bool batchSendAtEndOfFrame = true;

		public NetworkConnectionError lastError = NetworkConnectionError.NoError;

		public NetworkBaseLocal()
		{
			_emulation = new NetworkEmulation(this);
			_config = new NetworkConfig(this);
		}

		public NetworkFlags defaultRPCFlags
		{
			get
			{
				return _rpcFlags;
			}
		}

		public int listenPort
		{
			get
			{
				var netBase = _GetNetBase();
				return (netBase != null && netBase.IsListening) ? netBase.ListenPort : NetworkEndPoint.unassignedPort;
			}
		}

		public NetworkEndPoint listenEndPoint
		{
			get
			{
				var netBase = _GetNetBase();
				return (netBase != null && netBase.IsListening) ? netBase.ListenEndPoint : NetworkEndPoint.unassigned;
			}
		}

		public NetworkStatus status { get { return _status; } }
		public NetworkPeerType peerType { get { return _peerType; } }

		public bool isClient { get { return _peerType == NetworkPeerType.Client; } }
		public bool isServer { get { return _peerType == NetworkPeerType.Server; } }
		public bool isCellServer { get { return _peerType == NetworkPeerType.CellServer; } }
		public bool isClientOrCellServer { get { return _peerType == NetworkPeerType.Client || _peerType == NetworkPeerType.CellServer; } }
		public bool isServerOrCellServer { get { return _peerType == NetworkPeerType.Server || _peerType == NetworkPeerType.CellServer; } }

		public float sendRate
		{
			set
			{
				_intervalSyncState = 1.0f / value;

				if (!Single.IsInfinity(_intervalSyncState))
				{
					_nextSyncState = NetworkTime.localTime + _intervalSyncState;
				}
			}

			get
			{
				return 1.0f / _intervalSyncState;
			}
		}

		public NetworkPlayer[] connections
		{
			get
			{
				if (isServer)
					return _ServerGetPlayers();

				if (isClientOrCellServer)
					return _ClientGetPlayers();

				return new NetworkPlayer[0];
			}
		}

		internal abstract NetworkPlayer[] _ClientGetPlayers();
		internal abstract NetworkPlayer[] _ServerGetPlayers();

		public int connectionCount
		{
			get
			{
				if (isServer)
					return _ServerGetPlayerCount();

				if (isClientOrCellServer)
					return 1;

				return 0;
			}
		}

		internal abstract int _ServerGetPlayerCount();

		internal void _SetLoginData(NetworkPlayer target, BitStream stream)
		{
			_userLoginData[target] = stream;
		}

		internal void _RemoveLoginData(NetworkPlayer target)
		{
			_userLoginData.Remove(target);
		}

		public BitStream GetLoginData(NetworkPlayer target)
		{
			BitStream stream;

			return _userLoginData.TryGetValue(target, out stream) ? stream : null;
		}

		public NetworkSecurityStatus GetSecurityStatus(NetworkPlayer target)
		{
			if (isServer)
				return _ServerGetSecurityStatus(target);

			if (isClient)
				return _ClientGetSecurityStatus(target);

			// TODO: what if _isCellServer?
			return NetworkSecurityStatus.Disabled;
		}

		internal abstract NetworkSecurityStatus _ClientGetSecurityStatus(NetworkPlayer target);
		internal abstract NetworkSecurityStatus _ServerGetSecurityStatus(NetworkPlayer target);

		public bool IsConnected(NetworkPlayer target)
		{
			if (isServer)
				return _ServerIsConnected(target);

			if (isClient)
				return _ClientIsConnected(target);

			// TODO: what if _isCellServer?
			return false;
		}

		internal abstract bool _ClientIsConnected(NetworkPlayer target);
		internal abstract bool _ServerIsConnected(NetworkPlayer target);

		internal NetConnection _GetConnection(NetworkPlayer target)
		{
			if (isServer)
				return _ServerGetConnection(target);

			if (isClient)
				return _ClientGetConnection(target);

			// TODO: what if _isCellServer?
			return null;
		}

		internal abstract NetConnection _ClientGetConnection(NetworkPlayer target);
		internal abstract NetConnection _ServerGetConnection(NetworkPlayer target);

		public NetworkEndPoint FindAvailableEndPoint(NetworkPlayer target)
		{
			var endpoint = FindExternalEndPoint(target);
			return (!endpoint.isUnassigned) ? endpoint : FindInternalEndPoint(target);
		}

		public NetworkEndPoint FindInternalEndPoint(NetworkPlayer target)
		{
			if (isServer)
				return _ServerFindInternalEndPoint(target);

			if (isClient)
				return _ClientFindInternalEndPoint(target);

			// TODO: what if _isCellServer?
			return NetworkEndPoint.unassigned;
		}

		public NetworkEndPoint FindExternalEndPoint(NetworkPlayer target)
		{
			if (isServer)
				return _ServerFindExternalEndPoint(target);

			if (isClient)
				return _ClientFindExternalEndPoint(target);

			// TODO: what if _isCellServer?
			return NetworkEndPoint.unassigned;
		}

		internal abstract NetworkEndPoint _ClientFindInternalEndPoint(NetworkPlayer target);
		internal abstract NetworkEndPoint _ServerFindInternalEndPoint(NetworkPlayer target);

		internal abstract NetworkEndPoint _ClientFindExternalEndPoint(NetworkPlayer target);
		internal abstract NetworkEndPoint _ServerFindExternalEndPoint(NetworkPlayer target);

		public int GetAveragePing(NetworkPlayer target)
		{
			if (isServer)
				return _ServerGetAveragePing(target);

			if (isClientOrCellServer)
				return _ClientGetAveragePing(target);

			return -1;
		}

		internal abstract int _ServerGetAveragePing(NetworkPlayer target);
		internal abstract int _ClientGetAveragePing(NetworkPlayer target);

		public int GetLastPing(NetworkPlayer target)
		{
			if (isServer)
				return _ServerGetLastPing(target);

			if (isClientOrCellServer)
				return _ClientGetLastPing(target);

			return -1;
		}

		internal abstract int _ServerGetLastPing(NetworkPlayer target);
		internal abstract int _ClientGetLastPing(NetworkPlayer target);

		public NetworkStatistics GetStatistics(NetworkPlayer target)
		{
			if (isServer)
			{
				return _ServerGetStatistics(target);
			}
			if (isClientOrCellServer)
			{
				return _ClientGetStatistics(target);
			}

			return null;
		}

		internal abstract NetworkStatistics _ServerGetStatistics(NetworkPlayer target);
		internal abstract NetworkStatistics _ClientGetStatistics(NetworkPlayer target);

		public void SetLevelPrefix(int prefix)
		{
			// TODO: implement
		}

		public void SetReceivingEnabled(NetworkPlayer player, NetworkGroup group, bool enabled)
		{
			// TODO: implement
		}

		public void SetSendingEnabled(NetworkGroup group, bool enabled)
		{
			// TODO: implement
		}

		public void SetSendingEnabled(NetworkPlayer player, NetworkGroup group, bool enabled)
		{
			// TODO: implement
		}

		private double _GetAndResetTimeSinceLastUpdate()
		{
			double now = NetworkTime.localTime;

			double deltaTime = now - _timeAtLastUpdate;
			if (deltaTime < 0) deltaTime = 0;

			_timeAtLastUpdate = now;

			return deltaTime;
		}

		public void Update()
		{
			// TODO: warn if we have incoming messages and time since last update has been more than 4 seconds

			double deltaTime = _GetAndResetTimeSinceLastUpdate();

			if (_status != NetworkStatus.Disconnected)
			{
				if (_status == NetworkStatus.Connected)
				{
					NetProfiler.BeginSample("_DoStateSyncs");
					_DoStateSyncs();
					NetProfiler.EndSample();
				}

				NetProfiler.BeginSample("_ClientUpdate");
				_ClientUpdate(deltaTime);
				NetProfiler.EndSample();

				NetProfiler.BeginSample("_ServerUpdate");
				_ServerUpdate(deltaTime);
				NetProfiler.EndSample();
			}

			if (_status == NetworkStatus.Disconnecting && _timeToDisconnect <= NetworkTime.localTime)
			{
				NetProfiler.BeginSample("_DisconnectImmediate");
				DisconnectImmediate();
				NetProfiler.EndSample();
			}

			NetProfiler.BeginSample("_MasterUpdate");
			_MasterUpdate(deltaTime);
			NetProfiler.EndSample();
			
#if !NO_POOLING
			NetProfiler.BeginSample("_ResetBitStreamPools");
			NetworkMessage.ResetBitStreamPools();
			NetProfiler.EndSample();
#endif
		}

		internal abstract void _ClientUpdate(double deltaTime);
		internal abstract void _ServerUpdate(double deltaTime);
		internal abstract void _MasterUpdate(double deltaTime);

		public void Disconnect(int timeoutInMillis)
		{
			if (_status == NetworkStatus.Disconnecting || _status == NetworkStatus.Disconnected)
			{
				return;
			}

			float timeout = timeoutInMillis * 0.001f;

			_ClientDisconnect(timeout);
			_ServerDisconnect(timeout);

			if (Math.Abs(timeout) < Single.Epsilon || _status == NetworkStatus.Connecting)
			{
				DisconnectImmediate();
			}
			else
			{
				_status = NetworkStatus.Disconnecting;
				_timeToDisconnect = NetworkTime.localTime + timeout;
			}
		}

		internal abstract void _ClientDisconnect(float timeout);
		internal abstract void _ServerDisconnect(float timeout);

		internal void _NetworkShutdown()
		{
			if (_status == NetworkStatus.Connecting)
			{
				_FailConnectionAttempt(NetworkConnectionError.NetworkShutdown);
			}
			else
			{
				DisconnectImmediate();
			}
		}

		internal void _FailConnectionAttempt(NetworkConnectionError error)
		{
			lastError = error;

			string eventName = (isCellServer) ? "OnFailedToConnectToPikkoServer" : "OnFailedToConnect";

			_Cleanup();

			_Notify(eventName, error);
		}

		public void DisconnectImmediate()
		{
			if (_status == NetworkStatus.Disconnected)
			{
				return;
			}

			var oldStatus = _status;
			var oldType = _peerType;

			_CleanupAllNetworkViews();
			_Cleanup();

			if (oldStatus == NetworkStatus.Connected | oldStatus == NetworkStatus.Disconnecting)
			{
				_NotifyDisconnected(oldType);
			}
		}

		private void _CleanupAllNetworkViews()
		{
			Log.Debug(NetworkLogFlags.NetworkView, "Cleanup all ", _enabledViews.Count, " NetworkView(s)");

			var buffer = Utility.ToArray(_enabledViews);

			foreach (var pair in buffer)
			{
				var viewID = pair.Key;
				var nv = pair.Value;

				if (nv.destroyOnFinalDisconnect) OnDestroy(nv);

				if (viewID.isAllocated) nv.SetUnassignedViewID();
			}
		}

		internal void _Cleanup()
		{
			_status = NetworkStatus.Disconnected;
			_peerType = NetworkPeerType.Disconnected;

			_localPlayer = NetworkPlayer.unassigned;
			_UnsynchronizeServerTime();
			_userViews.Clear();
			NetworkPlayer._userData.Clear();
			_userLoginData.Clear();

			_timeToDisconnect = 0;
			_nextSyncState = 0;

			_ClientCleanup();
			_ServerCleanup();
		}

		internal abstract void _ClientCleanup();
		internal abstract void _ServerCleanup();

		private void _NotifyDisconnected(NetworkPeerType peerType)
		{
			string eventName;

			switch (peerType)
			{
				case NetworkPeerType.Server:
					eventName = "OnServerUninitialized";
					_disconnectionType = NetworkDisconnection.Disconnected; //TODO: use _disconnectionType and for OnPlayerDisconnected
					break;

				case NetworkPeerType.Client:
					eventName = "OnDisconnectedFromServer";
					break;

				case NetworkPeerType.CellServer:
					eventName = "OnDisconnectedFromPikkoServer";
					break;

				default:
					return;
			}

			_Notify(eventName, _disconnectionType);
		}

		internal NetBase _GetNetBase()
		{
			NetBase netBase = _ServerGetNetBase();
			if (netBase != null)
			{
				return netBase;
			}

			netBase = _ClientGetNetBase();
			if (netBase != null)
			{
				return netBase;
			}

			return null;
		}

		internal NetBase _GetNetBaseMaster()
		{
			return _MasterGetNetBase();
		}

		internal abstract NetBase _ClientGetNetBase();
		internal abstract NetBase _ServerGetNetBase();
		internal abstract NetBase _MasterGetNetBase();

		internal void _ConfigureNetBase(NetBase net)
		{
			net.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, true);
			net.SetMessageTypeEnabled(NetMessageType.ConnectionRejected, true);

#if DEBUG // TODO: fix this
			net.SetMessageTypeEnabled(NetMessageType.BadMessageReceived, true);
			net.SetMessageTypeEnabled(NetMessageType.DebugMessage, true);
			net.SetMessageTypeEnabled(NetMessageType.VerboseDebugMessage, true);
#endif

			emulation._Apply(net);
			config._Apply(net);
		}

		public void RPC(NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args)
		{
			RPC(_rpcFlags, viewID, rpcName, target, args);
		}

		public void RPC(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args)
		{
			RPC(_rpcFlags, viewID, rpcName, targets, args);
		}

		public void RPC(NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args)
		{
			RPC(_rpcFlags, viewID, rpcName, mode, args);
		}

		public void RPC<T>(NetworkViewID viewID, string rpcName, NetworkPlayer target, T arg)
		{
			RPC(viewID, rpcName, target, (object)arg);
		}

		public void RPC<T>(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, T arg)
		{
			RPC(viewID, rpcName, targets, (object)arg);
		}

		public void RPC<T>(NetworkViewID viewID, string rpcName, RPCMode mode, T arg)
		{
			RPC(viewID, rpcName, mode, (object)arg);
		}

		public void UnreliableRPC(NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args)
		{
			RPC(_rpcFlags | NetworkFlags.Unreliable, viewID, rpcName, target, args);
		}

		public void UnreliableRPC(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args)
		{
			RPC(_rpcFlags | NetworkFlags.Unreliable, viewID, rpcName, targets, args);
		}

		public void UnreliableRPC(NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args)
		{
			RPC(_rpcFlags | NetworkFlags.Unreliable, viewID, rpcName, mode, args);
		}

		public void UnreliableRPC<T>(NetworkViewID viewID, string rpcName, NetworkPlayer target, T arg)
		{
			UnreliableRPC(viewID, rpcName, target, (object)arg);
		}

		public void UnreliableRPC<T>(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, T arg)
		{
			UnreliableRPC(viewID, rpcName, targets, (object)arg);
		}

		public void UnreliableRPC<T>(NetworkViewID viewID, string rpcName, RPCMode mode, T arg)
		{
			UnreliableRPC(viewID, rpcName, mode, (object)arg);
		}

		public void UnencryptedRPC(NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args)
		{
			RPC(_rpcFlags | NetworkFlags.Unencrypted, viewID, rpcName, target, args);
		}

		public void UnencryptedRPC(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args)
		{
			RPC(_rpcFlags | NetworkFlags.Unencrypted, viewID, rpcName, targets, args);
		}

		public void UnencryptedRPC(NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args)
		{
			RPC(_rpcFlags | NetworkFlags.Unencrypted, viewID, rpcName, mode, args);
		}

		public void UnencryptedRPC<T>(NetworkViewID viewID, string rpcName, NetworkPlayer target, T arg)
		{
			UnencryptedRPC(viewID, rpcName, target, (object)arg);
		}

		public void UnencryptedRPC<T>(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, T arg)
		{
			UnencryptedRPC(viewID, rpcName, targets, (object)arg);
		}

		public void UnencryptedRPC<T>(NetworkViewID viewID, string rpcName, RPCMode mode, T arg)
		{
			UnencryptedRPC(viewID, rpcName, mode, (object)arg);
		}

		public void RPC<T>(NetworkFlags flags, NetworkViewID viewID, string rpcName, NetworkPlayer target, T arg)
		{
			RPC(flags, viewID, rpcName, target, (object)arg);
		}

		public void RPC<T>(NetworkFlags flags, NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, T arg)
		{
			RPC(flags, viewID, rpcName, targets, (object)arg);
		}

		public void RPC<T>(NetworkFlags flags, NetworkViewID viewID, string rpcName, RPCMode mode, T arg)
		{
			RPC(flags, viewID, rpcName, mode, (object)arg);
		}

		public void RPC(NetworkFlags flags, NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args)
		{
			_AssertRPC(viewID, target);

			if (target == NetworkPlayer.cellProxies)
			{
				var nv = _FindNetworkView(viewID);
				if(!(nv != null && nv.isCellAuthority)){Utility.Exception( "CellProxy object ", nv, " can't send a RPC to other CellProxies, only the CellAuthority object can send to a CellProxy.");}
			}

			var msg = new NetworkMessage(this, flags, NetworkMessage.Channel.RPC, rpcName, NetworkMessage.InternalCode.None, target, NetworkPlayer.unassigned, viewID);
			_rpcWriter.Write(msg.stream, rpcName, args);
			_CreatePrivateRPC(msg);
		}

		public void RPC(NetworkFlags flags, NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args)
		{
			_AssertRPC(viewID, targets);

			foreach (var target in targets)
			{
				if (target == NetworkPlayer.cellProxies)
				{
					var nv = _FindNetworkView(viewID);
					if(!(nv != null && nv.isCellAuthority)){Utility.Exception( "CellProxy object ", nv, " can't send a RPC to other CellProxies, only the CellAuthority object can send to a CellProxy.");}

					break;
				}
			}

			var stream = new BitStream((flags & NetworkFlags.TypeUnsafe) == 0);
			_rpcWriter.Write(stream, rpcName, args);

			foreach (var target in targets)
			{
				var msg = new NetworkMessage(this, flags, NetworkMessage.Channel.RPC, rpcName, NetworkMessage.InternalCode.None, target, NetworkPlayer.unassigned, viewID);
				msg.stream.AppendBitStream(stream);
				_CreatePrivateRPC(msg);
			}
		}

		public void RPC(NetworkFlags flags, NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args)
		{
			_AssertRPC(viewID, mode);
			var msg = new NetworkMessage(this, flags, NetworkMessage.Channel.RPC, rpcName, mode, viewID);
			_rpcWriter.Write(msg.stream, rpcName, args);
			_CreateRPCMode(msg, mode);
		}

		public void _CreateRPCMode(NetworkMessage rpc, RPCMode mode)
		{
			if (mode == RPCMode.Server && (isServer || (isCellServer && rpc.hasViewID && rpc.viewID.isCellAuthority)))
			{
				rpc.stream._isWriting = false;
				_ExecuteRPC(rpc);
			}
			else
			{
				_SendRPC(rpc);

				if (mode == RPCMode.All || mode == RPCMode.AllBuffered)
				{
					rpc.stream._isWriting = false;
					_ExecuteRPC(rpc);
				}
			}
		}

		public void _CreatePrivateRPC(NetworkMessage rpc)
		{
			if (rpc.target == _localPlayer || (rpc.target.isServer && isCellServer && rpc.hasViewID && rpc.viewID.isCellAuthority))
			{
				rpc.stream._isWriting = false;
				_ExecuteRPC(rpc);
			}
			else
			{
				_SendRPC(rpc);
			}
		}

		internal void _ExecuteRPC(NetworkMessage rpc)
		{
			Log.Debug(NetworkLogFlags.RPC, "Executing ", rpc);

			if (rpc.isCustom)
			{
				_ExecuteCustomRPC(rpc);
			}
			else
			{
				rpc.ExecuteInternal(this as NetworkBase);
			}
		}

		private void _ExecuteCustomRPC(NetworkMessage rpc)
		{
			// this shouldn't happen, but may happen if the sender has already been disconnected and its messages are still incoming.
			if (rpc.sender == NetworkPlayer.unassigned)
			{
				Log.Debug(NetworkLogFlags.RPC, "Dropped, incoming RPC is from unknown sender (probably recently disconnected): ", rpc);

				return;

				/*
				// sanity check everything!

				var netBase = _GetNetBase();
				var connection = netBase != null ? netBase.GetConnection(rpc.connection.RemoteEndpoint) : null;
				var sender = connection != null ? _GetConnectionPlayerID(connection) : NetworkPlayer.unassigned;

				
				if (sender != NetworkPlayer.unassigned)
				{
					Log.Warning(NetworkLogFlags.RPC, "Found sender in tag: ", sender);
					rpc.sender = sender;
				}
				else
				{
					sender = _FindConnectionPlayerID(rpc.connection);

					if (sender != NetworkPlayer.unassigned)
					{
						Log.Warning(NetworkLogFlags.RPC, "Found sender in connections: ", sender);
						rpc.sender = sender;
					}
					else
					{
						Log.Error(NetworkLogFlags.RPC, "Unable to recognize sender, dropping RPC: ", rpc);
						return;
					}
				}
				*/
			}

			NetworkViewBase nv = _FindNetworkView(rpc.viewID);
			if (nv.IsNotNull())
			{
				Log.Debug(NetworkLogFlags.RPC, "Calling ", nv.viewID, " of message ", rpc.name);

				if (String.IsNullOrEmpty(rpc.name))
				{
					Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, "Dropped unnamed RPC ", rpc);
					return;
				}

				if (nv.isCellProxy && rpc.sender != NetworkPlayer.server)
				{
					if (rpc.isReliable)
					{
						Log.Debug(NetworkLogFlags.CellServer | NetworkLogFlags.RPC,
							"Dropping reliable RPC ", rpc.name, " to cell proxy due to sender not being cell auth; if this occurs during normal handovers (rather than cell crash repair), it is a bug");
					}
					//else no warning for unreliable drops since it the lack of order means handovers will cause this

					return;
				}
				if (nv.isCellAuthority && !rpc.target.isServer)
				{
					if (rpc.isReliable)
					{
						Log.Debug(NetworkLogFlags.CellServer | NetworkLogFlags.RPC,
							"Dropping reliable RPC ", rpc.name, " to cell auth due to target not being cell auth; possible consequence of sending RPCs to cell proxies promoted to cell auths");
					}
					//else no warning for unreliable drops since the lack of order makes it normal

					return;
				}
				var info = new NetworkMessageInfo(rpc, nv);
				nv._CallRPC(rpc.name, rpc.stream, info);
			}
			else
			{
				// TODO: this message has been hackisly changed from Error to Debug in order to be silent when doing P2P handover.
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, "RPC can't find NetworkView with ", rpc.viewID);
				//It is normal that unreliable RPCs sent to the client are dropped right here = BEFORE the 
				//complete (sometimes big and fragmented) RPC buffer has been received.
			}
		}

		public void _SendRPC(NetworkMessage rpc)
		{
			if (isServer)
				_ServerSendRPC(rpc);
			else
				_ClientSendRPC(rpc);
		}

		internal abstract void _ClientSendRPC(NetworkMessage rpc);
		internal abstract void _ServerSendRPC(NetworkMessage rpc);

		private void _DoStateSyncs()
		{
			var localNow = NetworkTime.localTime;

			if (Single.IsInfinity(_intervalSyncState) || _nextSyncState > localNow)
			{
				return;
			}

			_nextSyncState += _intervalSyncState;

			if (isServer && _ServerGetPlayerCount() == 0)
			{
				return;
			}

			if (!isAuthoritativeServer) // if NOT authoritative server
			{
				List<NetworkViewBase> views;
				if (_userViews.TryGetValue(_localPlayer, out views))
				{
					var buffer = views.ToArray();

					foreach (var nv in buffer)
					{
						if (nv.stateSynchronization == NetworkStateSynchronization.Off) continue;

						_SendStateSyncProxy(nv);
					}
				}
			}
			else if (isServer)
			{
				var buffer = new NetworkViewBase[_enabledViews.Count];
				_enabledViews.Values.CopyTo(buffer, 0);

				foreach (var nv in buffer)
				{
					if (nv.stateSynchronization == NetworkStateSynchronization.Off) continue;

					_SendStateSyncProxy(nv);
				}

				if (useDifferentStateForOwner)
				{
					foreach (var nv in buffer)
					{
						if (nv.stateSynchronization != NetworkStateSynchronization.Off)
						{
							_SendStateSyncOwner(nv);
						}
					}
				}
			}
			else if (isCellServer)
			{
				_SendMultiStateSync();
			}
		}

		private void _SendMultiStateSync()
		{
			var reliableEncryptedViews = new List<NetworkViewBase>(_enabledViews.Count);
			var unreliableEncryptedViews = new List<NetworkViewBase>(_enabledViews.Count);
			var reliableUnencryptedViews = new List<NetworkViewBase>(_enabledViews.Count);
			var unreliableUnencryptedViews = new List<NetworkViewBase>(_enabledViews.Count);

			foreach (var pair in _enabledViews)
			{
				var nv = pair.Value;

				if (nv.stateSynchronization != NetworkStateSynchronization.Off && (!nv.isInstantiatedRemotely /* optimization of nv.isAuthority */))
				{
					if ((nv.securable & NetworkSecurable.OnlyStateSynchronization) != 0)
					{
						if (nv.stateSynchronization == NetworkStateSynchronization.Reliable || nv.stateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed)
							reliableEncryptedViews.Add(nv);
						else // TODO: if nv.stateSynchronization == NetworkStateSynchronization.UnreliableDeltaCompressed
							unreliableEncryptedViews.Add(nv);
					}
					else
					{
						if (nv.stateSynchronization == NetworkStateSynchronization.Reliable || nv.stateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed)
							reliableUnencryptedViews.Add(nv);
						else // TODO: if nv.stateSynchronization == NetworkStateSynchronization.UnreliableDeltaCompressed
							unreliableUnencryptedViews.Add(nv);
					}
				}
			}

			if (reliableEncryptedViews.Count > 0) _SendMultiStateSync(reliableEncryptedViews, NetworkFlags.Normal);
			if (unreliableEncryptedViews.Count > 0) _SendMultiStateSync(unreliableEncryptedViews, NetworkFlags.Unreliable);
			if (reliableUnencryptedViews.Count > 0) _SendMultiStateSync(reliableUnencryptedViews, NetworkFlags.Unencrypted);
			if (unreliableUnencryptedViews.Count > 0) _SendMultiStateSync(unreliableUnencryptedViews, NetworkFlags.Unencrypted | NetworkFlags.Unreliable);
		}

		private void _SendMultiStateSync(List<NetworkViewBase> views, NetworkFlags flags)
		{
			_SendMultiStateSyncProxy(views, flags);

			_SendMultiStateSyncCellProxy(views, flags);

			/* TODO: add support for this without wasting bandwidth
			
			if (isAuthoritativeServer && useDifferentStateForOwner)
			{
				_SendMultiStateSyncOwner(views, flags);
			}
			*/
		}

		private void _SendMultiStateSyncProxy(List<NetworkViewBase> views, NetworkFlags flags)
		{
			var msg = new NetworkMessage(this, NetworkFlags.TypeUnsafe | NetworkFlags.Unbuffered | flags, NetworkMessage.Channel.StateSyncProxy, NetworkMessage.InternalCode.MultiStateSyncProxy, RPCMode.Others, NetworkPlayer.unassigned);

			var states = new List<StateSync>(views.Count);

			foreach (var nv in views)
			{
				if (nv._hasSerializeCellProxy)
				{
					var stream = new BitStream(false);
					if (nv._SerializeProxy(stream, new NetworkMessageInfo(msg, nv)))
					{
						Log.Debug(NetworkLogFlags.StateSync, "Successfully serialized proxy state for ", nv.viewID);
						states.Add(new StateSync(nv.viewID, new SerializedBuffer(stream)));
					}
				}
			}

			if (states.Count == 0) return;

			Log.Debug(NetworkLogFlags.StateSync, "Sending serialized proxy state for ", states.Count, " NetworkView(s)");

			msg.stream.WriteStateSyncs(states.ToArray());

			if (isServer)
				_ServerSendStateSync(msg);
			else if (isClientOrCellServer)
				_ClientSendStateSync(msg);
			else
				Log.Debug(NetworkLogFlags.StateSync, "Network has been shutdown while serializing multiple proxy states");
		}

		private void _SendMultiStateSyncOwner(List<NetworkViewBase> views, NetworkFlags flags)
		{
			var msg = new NetworkMessage(this, NetworkFlags.TypeUnsafe | NetworkFlags.Unbuffered | flags, NetworkMessage.Channel.StateSyncOwner, NetworkMessage.InternalCode.MultiStateSyncOwner, RPCMode.Others, NetworkPlayer.unassigned);

			var states = new List<StateSync>(views.Count);

			foreach (var nv in views)
			{
				if (nv._hasSerializeOwner)
				{
					var stream = new BitStream(false);
					if (nv._SerializeOwner(stream, new NetworkMessageInfo(msg, nv)))
					{
						Log.Debug(NetworkLogFlags.StateSync, "Successfully serialized owner state for ", nv.viewID);
						states.Add(new StateSync(nv.viewID, new SerializedBuffer(stream)));
					}
				}
			}

			if (states.Count == 0) return;

			Log.Debug(NetworkLogFlags.StateSync, "Sending serialized owner state for ", states.Count, " NetworkView(s)");

			msg.stream.WriteStateSyncs(states.ToArray());

			if (isServer)
				_ServerSendStateSync(msg);
			else if (isClientOrCellServer)
				_ClientSendStateSync(msg);
			else
				Log.Debug(NetworkLogFlags.StateSync, "Network has been shutdown while serializing multiple owner states");
		}

		private void _SendMultiStateSyncCellProxy(List<NetworkViewBase> views, NetworkFlags flags)
		{
			var msg = new NetworkMessage(this, NetworkFlags.TypeUnsafe | NetworkFlags.Unbuffered | flags, NetworkMessage.Channel.StateSyncCellProxy, NetworkMessage.InternalCode.MultiStateSyncCellProxy, RPCMode.Others, NetworkPlayer.unassigned);

			var states = new List<StateSync>(views.Count);

			foreach (var nv in views)
			{
				if (nv._hasSerializeCellProxy)
				{
					var stream = new BitStream(false);
					if (nv._SerializeCellProxy(stream, new NetworkMessageInfo(msg, nv)))
					{
						Log.Debug(NetworkLogFlags.StateSync, "Successfully serialized cell proxy state for ", nv.viewID);
						states.Add(new StateSync(nv.viewID, new SerializedBuffer(stream)));
					}
				}
			}

			if (states.Count == 0) return;

			Log.Debug(NetworkLogFlags.StateSync, "Sending serialized cell proxy state for ", states.Count, " NetworkView(s)");

			msg.stream.WriteStateSyncs(states.ToArray());

			if (isServer)
				_ServerSendStateSync(msg);
			else if (isClientOrCellServer)
				_ClientSendStateSync(msg);
			else
				Log.Debug(NetworkLogFlags.StateSync, "Network has been shutdown while serializing multiple cell proxy states");
		}

		private void _SendStateSyncProxy(NetworkViewBase nv)
		{
			if (!nv._hasSerializeProxy) return;

			var msgCode = nv.stateSynchronization != NetworkStateSynchronization.ReliableDeltaCompressed
				? NetworkMessage.InternalCode.StateSyncProxy
				: (nv._prevProxyStateSerialization != null
					? NetworkMessage.InternalCode.StateSyncProxyDeltaCompressed
					: NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit);

			var viewID = nv.viewID;

			var exclusion = useDifferentStateForOwner ? nv.owner : NetworkPlayer.unassigned;
			var msg = new NetworkMessage(this, nv._syncFlags, NetworkMessage.Channel.StateSyncProxy, String.Empty, msgCode, NetworkPlayer.unassigned, exclusion, viewID);

			if (nv._nextProxyStateSerialization == null) nv._nextProxyStateSerialization = new BitStream(true, false);
			var nextState = nv._nextProxyStateSerialization;
			nextState._buffer.Reset();

			if (!nv._SerializeProxy(nextState, new NetworkMessageInfo(msg, nv)))
			{
				return;
			}

			if (nv.stateSynchronization != NetworkStateSynchronization.ReliableDeltaCompressed)
			{
				msg.stream._buffer.Write(nextState._data, 0, nextState._buffer.LengthBytes);
			}
			else
			{
				var prevState = nv._prevProxyStateSerialization;
				nv._nextProxyStateSerialization = prevState ?? new BitStream(true, false);
				nv._prevProxyStateSerialization = nextState;

				byte sqNr = (byte)nv._expectedProxyStateDeltaCompressedSequenceNr;
				nv._expectedProxyStateDeltaCompressedSequenceNr = (byte)(sqNr + 1);

				msg.stream._buffer.Write(sqNr);

				if (msg.internCode == NetworkMessage.InternalCode.StateSyncProxyDeltaCompressed)
				{
					var uncompressedDiff = NetBuffer.CreateDiff(nextState._data, nextState._buffer.LengthBytes, prevState._data, prevState._buffer.LengthBytes);
					if (!Utility.AreAllElementsZero(uncompressedDiff))
					{
						var uncompressedLength = uncompressedDiff.Length;
						var compressedDiff = new byte[uncompressedLength];
						int compressedLength = 0;

						try
						{
							compressedLength = RunLengthEncoding.Encode(uncompressedDiff, 0, uncompressedLength, compressedDiff, 0, uncompressedLength);
						}
						catch (Exception ex)
						{
							Log.Warning(NetworkLogFlags.Compression, "Failed to delta-compress state sync proxy using the diff (", uncompressedDiff, ") between new state (", nextState, ") and previous state (", prevState, ") for object ", nv, ": ", ex);
						}

						if (compressedLength <= 0 || compressedLength >= uncompressedLength) // uncompressible data
						{
							Log.Info(NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Skipping delta-compression of state sync proxy from ", nv, " because the serialized data (", uncompressedLength, " bytes) could not be made any smaller (", compressedLength, " bytes)");

							compressedDiff = nextState._data;
							compressedLength = uncompressedLength;

							// TODO: avoid fulhacks
							msg.internCode = NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit;
							msg.stream._data[msg.stream._buffer.LengthBytes - 4] = (byte)NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit;
						}
						else
						{
							msg.stream._buffer.WriteVariableUInt32((uint)uncompressedLength);
						}

						msg.stream._buffer.Write(compressedDiff, 0, compressedLength);
					}
					else
					{
						// TODO: nothing has changed, should we drop the message?

						// TODO: avoid fulhacks
						msg.internCode = NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit;
						msg.stream._data[msg.stream._buffer.LengthBytes - 4] = (byte)NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit;
					}
				}
				else
				{
					msg.stream._buffer.Write(nextState._data, 0, nextState._buffer.LengthBytes);
				}
			}

			Log.Info(NetworkLogFlags.StateSync, "Sending serialized proxy state for ", viewID, " with state sync ", nv.stateSynchronization, " and raw server timestamp ", msg.rawServerTimeSent);

			if (isServer)
				_ServerSendStateSync(msg);
			else if (isClientOrCellServer)
				_ClientSendStateSync(msg);
			else
				Log.Info(NetworkLogFlags.StateSync, "Network has been shutdown while serializing proxy state for ", nv.viewID);
		}

		private void _SendStateSyncOwner(NetworkViewBase nv)
		{
			var owner = nv.owner;
			if (owner.isServer || !IsConnected(owner) || !nv._hasSerializeOwner) return;

			var msgCode = nv.stateSynchronization != NetworkStateSynchronization.ReliableDeltaCompressed
				? NetworkMessage.InternalCode.StateSyncOwner
				: (nv._prevOwnerStateSerialization != null
					? NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressed
					: NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit);

			var viewID = nv.viewID;

			var msg = new NetworkMessage(this, nv._syncFlags, NetworkMessage.Channel.StateSyncOwner, String.Empty, msgCode, owner, NetworkPlayer.unassigned, viewID);

			if (nv._nextOwnerStateSerialization == null) nv._nextOwnerStateSerialization = new BitStream(true, false);
			var nextState = nv._nextOwnerStateSerialization;
			nextState._buffer.Reset();

			if (!nv._SerializeOwner(nextState, new NetworkMessageInfo(msg, nv)))
			{
				return;
			}

			if (nv.stateSynchronization != NetworkStateSynchronization.ReliableDeltaCompressed)
			{
				msg.stream._buffer.Write(nextState._data, 0, nextState._buffer.LengthBytes);
			}
			else
			{
				var prevState = nv._prevOwnerStateSerialization;
				nv._nextOwnerStateSerialization = prevState ?? new BitStream(true, false);
				nv._prevOwnerStateSerialization = nextState;

				byte sqNr = (byte)nv._expectedOwnerStateDeltaCompressedSequenceNr;
				nv._expectedOwnerStateDeltaCompressedSequenceNr = (byte)(sqNr + 1);

				msg.stream._buffer.Write(sqNr);

				if (msg.internCode == NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressed)
				{
					var uncompressedDiff = NetBuffer.CreateDiff(nextState._data, nextState._buffer.LengthBytes, prevState._data, prevState._buffer.LengthBytes);
					if (!Utility.AreAllElementsZero(uncompressedDiff))
					{
						var uncompressedLength = uncompressedDiff.Length;
						var compressedDiff = new byte[uncompressedLength];
						int compressedLength = 0;

						try
						{
							compressedLength = RunLengthEncoding.Encode(uncompressedDiff, 0, uncompressedLength, compressedDiff, 0, uncompressedLength);
						}
						catch (Exception ex)
						{
							Log.Debug(NetworkLogFlags.Compression, "Failed to delta-compress state sync owner using the diff (", uncompressedDiff, ") between new state (", nextState, ") and previous state (", prevState, "): ", ex);
						}

						if (compressedLength <= 0 || compressedLength >= uncompressedLength) // uncompressible data
						{
							Log.Info(NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Skipping delta-compression of state sync owner from ", nv, " because the serialized data (", uncompressedLength, " bytes) could not be made any smaller (", compressedLength, " bytes)");

							compressedDiff = nextState._data;
							compressedLength = uncompressedLength;

							// TODO: avoid fulhacks
							msg.internCode = NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit;
							msg.stream._data[msg.stream._buffer.LengthBytes - 4] = (byte)NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit;
						}
						else
						{
							msg.stream._buffer.WriteVariableUInt32((uint)uncompressedLength);
						}

						msg.stream._buffer.Write(compressedDiff, 0, compressedLength);
					}
					else
					{
						// TODO: nothing has changed, should we drop the message?

						// TODO: avoid fulhacks
						msg.internCode = NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit;
						msg.stream._data[msg.stream._buffer.LengthBytes - 4] = (byte)NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit;
					}
				}
				else
				{
					msg.stream._buffer.Write(nextState._data, 0, nextState._buffer.LengthBytes);
				}
			}

			Log.Info(NetworkLogFlags.StateSync, "Sending serialized owner state for ", viewID, " with state sync ", nv.stateSynchronization, " and raw server timestamp ", msg.rawServerTimeSent);

			if (isServer)
				_ServerSendStateSync(msg);
			else if (isClientOrCellServer)
				_ClientSendStateSync(msg);
			else
				Log.Info(NetworkLogFlags.StateSync, "Network has been shutdown while serializing owner state for ", nv.viewID);
		}

		private void _SendStateSyncCellProxy(NetworkViewBase nv)
		{
			if (!nv._hasSerializeCellProxy) return;

			var viewID = nv.viewID;

			var exclusion = useDifferentStateForOwner ? nv.owner : NetworkPlayer.unassigned;
			var msg = new NetworkMessage(this, nv._syncFlags, NetworkMessage.Channel.StateSyncCellProxy, String.Empty, NetworkMessage.InternalCode.StateSyncCellProxy, NetworkPlayer.unassigned, exclusion, viewID);

			if (!nv._SerializeCellProxy(msg.stream, new NetworkMessageInfo(msg, nv)))
			{
				return;
			}

			Log.Info(NetworkLogFlags.StateSync, "Sending serialized cell proxy state for ", viewID, " with state sync ", nv.stateSynchronization, " and raw server timestamp ", msg.rawServerTimeSent);

			if (isServer)
				_ServerSendStateSync(msg);
			else if (isClientOrCellServer)
				_ClientSendStateSync(msg);
			else
				Log.Info(NetworkLogFlags.StateSync, "Network has been shutdown while serializing cell proxy state for ", nv.viewID);
		}

		internal void _RPCStateSyncProxyDeltaCompressed(BitStream stream, NetworkMessage msg)
		{
			_RPCStateSyncProxy(stream, msg);
		}

		internal void _RPCStateSyncOwnerDeltaCompressed(BitStream stream, NetworkMessage msg)
		{
			_RPCStateSyncOwner(stream, msg);
		}

		internal void _RPCStateSyncProxyDeltaCompressedInit(BitStream stream, NetworkMessage msg)
		{
			_RPCStateSyncProxy(stream, msg);
		}

		internal void _RPCStateSyncOwnerDeltaCompressedInit(BitStream stream, NetworkMessage msg)
		{
			_RPCStateSyncOwner(stream, msg);
		}

		internal void _RPCStateSyncProxy(BitStream stream, NetworkMessage msg)
		{
			if (_status != NetworkStatus.Connected)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing received proxy state before connection is complete.");
				return;
			}

			if (isAuthoritativeServer && (isServerOrCellServer || !msg.sender.isServerOrCellServer))
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing proxy state by non-authoritative ", msg.sender);
				return;
			}

			Log.Info(NetworkLogFlags.StateSync, "Proxy state synchronization received: ", msg);

			_UpdateProxyState(msg.viewID, stream, msg);
		}

		internal void _RPCStateSyncOwner(BitStream stream, NetworkMessage msg)
		{
			if (!isClient || _status != NetworkStatus.Connected)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing received owner state before client connection is complete.");
				return;
			}

			if (!msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing owner state by non-server ", msg.sender);
				return;
			}

			if (!isAuthoritativeServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing owner state by non-authoritative ", msg.sender);
				return;
			}

			Log.Info(NetworkLogFlags.StateSync, "Owner state synchronization received: ", msg);

			if (!useDifferentStateForOwner)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", msg.viewID, " was dropped because proxy state should be used instead");
				return;
			}

			_UpdateOwnerState(msg.viewID, stream, msg);
		}

		internal void _RPCStateSyncCellProxy(BitStream stream, NetworkMessage msg)
		{
			if (_status != NetworkStatus.Connected)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing received cell proxy state before connection is complete.");
				return;
			}

			if (!isCellServer || !msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing cell proxy state by non-authoritative ", msg.sender);
				return;
			}

			Log.Info(NetworkLogFlags.StateSync, "Cell proxy state synchronization received: ", msg);

			_UpdateCellProxyState(msg.viewID, stream, msg);
		}

		internal void _UpdateProxyState(NetworkViewID viewID, BitStream stream, NetworkMessage msg)
		{
			NetworkViewBase nv = _FindNetworkView(viewID);
			if (nv.IsNull())
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was not executed locally because it has no NetworkView");
				return;
			}

			if (nv.stateSynchronization == NetworkStateSynchronization.Off)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because state synchronization is off.");
				return;
			}

			if ((nv.stateSynchronization == NetworkStateSynchronization.Unreliable && msg.channel != NetChannel.Unreliable) ||
				(nv.stateSynchronization == NetworkStateSynchronization.Reliable && ((int)msg.channel & (int)NetChannel.ReliableUnordered) == 0))
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because the sender (", msg.channel, ") and receiver (", nv.stateSynchronization, ") seem to be using different modes of state synchronization.");
				return;
			}

			if (!nv._hasSerializeProxy)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because it has no serialization callback.");
				return;
			}

			if (useDifferentStateForOwner && nv.isMine)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because the owner doesn't use proxy state");
				return;
			}

			if (!isAuthoritativeServer && nv.isOwner)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because the owner is not the authority");
				return;
			}

			if (!isAuthoritativeServer && msg.sender != nv.owner)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because the sender is not the owner");
				return;
			}

			bool senderReliableDeltaCompressed = (msg.internCode == NetworkMessage.InternalCode.StateSyncProxyDeltaCompressed | msg.internCode == NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit);
			bool receiverReliableDeltaCompressed = (nv.stateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed);
			if (senderReliableDeltaCompressed != receiverReliableDeltaCompressed)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because the sender (ReliableDeltaCompressed = ", senderReliableDeltaCompressed, ") and receiver (ReliableDeltaCompressed = ", receiverReliableDeltaCompressed, ") are using different incompatible modes of state synchronization.");
				return;
			}

			// NOTE: we do this before checking timestamp, just in case it is dropped - which for *Reliable*DeltaCompressed can only happen because of time inaccuracy.
			if (senderReliableDeltaCompressed)
			{
				var sqNr = stream._buffer.ReadByte();

				if (msg.internCode == NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit)
				{
					nv._expectedProxyStateDeltaCompressedSequenceNr = (byte)(sqNr + 1);
					if (nv._nextProxyStateSerialization == null) nv._nextProxyStateSerialization = new BitStream(false, false);
				}
				else if (nv._nextProxyStateSerialization == null)
				{
					Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because delta compression has not been initialized properly, this can happen if previous state syncs were dropped explicitly by the application or automatically by uLink because the network object was missing.");
					return;
				}
				else if (sqNr != nv._expectedProxyStateDeltaCompressedSequenceNr)
				{
					Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because delta compression is out of sync, this can happen if previous state syncs were dropped explicitly by the application or automatically by uLink because the network object was missing.");
					return;
				}
				else
				{
					nv._expectedProxyStateDeltaCompressedSequenceNr = (byte)(sqNr + 1);
				}

				var prevState = nv._prevProxyStateSerialization;
				var nextState = nv._nextProxyStateSerialization;
				nextState._buffer.Reset();

				if (msg.internCode == NetworkMessage.InternalCode.StateSyncProxyDeltaCompressed)
				{
					var uncompressedLength = (int)stream._buffer.ReadVariableUInt32();
					if (uncompressedLength == 0)
					{
						Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Decompressed state sync proxy is of length zero, which it shouldn't be. This could be due to corruption, so the state sync will be dropped.");
						return;
					}

					var uncompressedDiff = new byte[uncompressedLength];

					try
					{
						uncompressedLength = RunLengthEncoding.Decode(stream._data, stream._buffer.PositionBytes, stream._buffer.BytesRemaining, uncompressedDiff, 0, uncompressedLength);
					}
					catch (Exception ex)
					{
						Log.Warning(NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Failed to delta-decompress state sync proxy for ", viewID, ": ", ex);
						return;
					}

					if (uncompressedLength != uncompressedDiff.Length)
					{
						Log.Warning(NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Decompressed state sync proxy should be of length (", uncompressedDiff.Length, " bytes) but is (", uncompressedLength, " bytes). This is likely due to corruption so it will be dropped.");
						return;
					}

					nextState._buffer.WriteDiff(uncompressedDiff, uncompressedLength, prevState._data, prevState._buffer.LengthBytes);
					prevState._buffer.Reset();
				}
				else
				{
					if (prevState == null) prevState = new BitStream(false, false);
					if (stream.isEOF) 
					{
						stream = prevState; // NOTE: nothing has changed since last state sync update
						prevState._bitIndex = 0;
					}

					nextState._buffer.Write(stream._data, stream._buffer.PositionBytes, stream._buffer.BytesRemaining);
				}

				stream = nextState;

				nv._prevProxyStateSerialization = nextState;
				nv._nextProxyStateSerialization = prevState;
			}

			double localTimestamp = msg.localTimeSent;

			if (localTimestamp <= nv._lastProxyTimestamp)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Proxy state with ", viewID, " was dropped because it's late (", localTimestamp, " <= ", nv._lastProxyTimestamp, ")");
				return;
			}

			nv._lastProxyTimestamp = localTimestamp;

			Log.Debug(NetworkLogFlags.StateSync, "Deserializing proxy state with viewID ", viewID);
			nv._SerializeProxy(stream, new NetworkMessageInfo(msg, nv));
		}

		internal void _UpdateOwnerState(NetworkViewID viewID, BitStream stream, NetworkMessage msg)
		{
			NetworkViewBase nv = _FindNetworkView(viewID);
			if (nv.IsNull())
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because it has no NetworkView");
				return;
			}

			if (nv.stateSynchronization == NetworkStateSynchronization.Off)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because state synchronization is off.");
				return;
			}

			if ((nv.stateSynchronization == NetworkStateSynchronization.Unreliable && msg.channel != NetChannel.Unreliable) ||
				(nv.stateSynchronization == NetworkStateSynchronization.Reliable && ((int)msg.channel & (int)NetChannel.ReliableUnordered) == 0))
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because the sender (", msg.channel, ") and receiver (", nv.stateSynchronization, ") seem to be using different modes of state synchronization.");
				return;
			}

			if (!nv._hasSerializeOwner)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because it has no serialization callback.");
				return;
			}

			if (!nv.isMine)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because the receiver is not the owner");
				return;
			}

			bool senderReliableDeltaCompressed = (msg.internCode == NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressed | msg.internCode == NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit);
			bool receiverReliableDeltaCompressed = (nv.stateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed);
			if (senderReliableDeltaCompressed != receiverReliableDeltaCompressed)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because the sender (ReliableDeltaCompressed = ", senderReliableDeltaCompressed, ") and receiver (ReliableDeltaCompressed = ", receiverReliableDeltaCompressed, ") are using different incompatible modes of state synchronization.");
				return;
			}

			// NOTE: we do this before checking timestamp, just in case it is dropped - which for *Reliable*DeltaCompressed can only happen because of time inaccuracy.
			if (senderReliableDeltaCompressed)
			{
				var sqNr = stream._buffer.ReadByte();

				if (msg.internCode == NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit)
				{
					nv._expectedOwnerStateDeltaCompressedSequenceNr = (byte)(sqNr + 1);
					if (nv._nextOwnerStateSerialization == null) nv._nextOwnerStateSerialization = new BitStream(false, false);
				}
				else if (nv._nextOwnerStateSerialization == null)
				{
					Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because delta compression has not been initialized properly, this can happen if previous state syncs were dropped explicitly by the application or automatically by uLink because the network object was missing.");
					return;
				}
				else if (sqNr != nv._expectedOwnerStateDeltaCompressedSequenceNr)
				{
					Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because delta compression is out of sync, this can happen if previous state syncs were dropped explicitly by the application or automatically by uLink because the network object was missing.");
					return;
				}
				else
				{
					nv._expectedOwnerStateDeltaCompressedSequenceNr = (byte)(sqNr + 1);
				}

				var prevState = nv._prevOwnerStateSerialization;
				var nextState = nv._nextOwnerStateSerialization;
				nextState._buffer.Reset();

				if (msg.internCode == NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressed)
				{
					var uncompressedLength = (int)stream._buffer.ReadVariableUInt32();
					if (uncompressedLength == 0)
					{
						Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Decompressed state sync owner is of length zero, which it shouldn't be. This could be due to corruption, so the state sync will be dropped.");
						return;
					}

					var uncompressedDiff = new byte[uncompressedLength];

					try
					{
						uncompressedLength = RunLengthEncoding.Decode(stream._data, stream._buffer.PositionBytes, stream._buffer.BytesRemaining, uncompressedDiff, 0, uncompressedLength);
					}
					catch (Exception ex)
					{
						Log.Warning(NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Failed to delta-decompress state sync owner for ", viewID, ": ", ex);
						return;
					}

					if (uncompressedLength != uncompressedDiff.Length)
					{
						Log.Warning(NetworkLogFlags.StateSync | NetworkLogFlags.Compression, "Decompressed state sync owner should be of length (", uncompressedDiff.Length, " bytes) but is (", uncompressedLength, " bytes). This is likely due to corruption so it will be dropped.");
						return;
					}

					nextState._buffer.WriteDiff(uncompressedDiff, uncompressedLength, prevState._data, prevState._buffer.LengthBytes);
					prevState._buffer.Reset();
				}
				else
				{
					if (prevState == null) prevState = new BitStream(false, false);
					if (stream.isEOF)
					{
						stream = prevState; // NOTE: nothing has changed since last state sync update
						prevState._bitIndex = 0;
					}

					nextState._buffer.Write(stream._data, stream._buffer.PositionBytes, stream._buffer.BytesRemaining);
				}

				stream = nextState;

				nv._prevOwnerStateSerialization = nextState;
				nv._nextOwnerStateSerialization = prevState;
			}

			double localTimestamp = msg.localTimeSent;

			if (localTimestamp <= nv._lastOwnerTimestamp)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Owner state with ", viewID, " was dropped because its late (", localTimestamp, " <= ", nv._lastOwnerTimestamp, ")");
				return;
			}

			nv._lastOwnerTimestamp = localTimestamp;

			Log.Debug(NetworkLogFlags.StateSync, "Deserializing owner state with viewID ", viewID);
			nv._SerializeOwner(stream, new NetworkMessageInfo(msg, nv));
		}

		internal void _UpdateCellProxyState(NetworkViewID viewID, BitStream stream, NetworkMessage msg)
		{
			NetworkViewBase nv = _FindNetworkView(viewID);
			if (nv.IsNull())
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Cell proxy state with ", viewID, " was not executed locally because it has no NetworkView");
				return;
			}

			if (nv.stateSynchronization == NetworkStateSynchronization.Off)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Cell proxy state with ", viewID, " was dropped because state synchronization is off.");
				return;
			}

			if ((nv.stateSynchronization == NetworkStateSynchronization.Unreliable && msg.channel != NetChannel.Unreliable) ||
				(nv.stateSynchronization == NetworkStateSynchronization.Reliable && ((int)msg.channel & (int)NetChannel.ReliableUnordered) == 0))
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Cell proxy state with ", viewID, " was dropped because the sender (", msg.channel, ") and receiver (", nv.stateSynchronization, ") seem to be using different modes of state synchronization.");
				return;
			}

			if (!nv._hasSerializeCellProxy)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Cell proxy state with ", viewID, " was dropped because it has no serialization callback.");
				return;
			}

			if (!nv.isCellProxy)
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Cell proxy state with ", viewID, " was dropped because the receiver isn't a CellProxy.");
				return;
			}

			double localTimestamp = msg.localTimeSent;

			if (localTimestamp <= nv._lastCellProxyTimestamp)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.StateSync, "Cell proxy state with ", viewID, " was dropped because it's late (", localTimestamp, " <= ", nv._lastCellProxyTimestamp, ")");
				return;
			}

			nv._lastCellProxyTimestamp = localTimestamp;

			Log.Debug(NetworkLogFlags.StateSync, "Deserializing cell proxy state with viewID ", viewID);
			nv._SerializeCellProxy(stream, new NetworkMessageInfo(msg, nv));
		}

		internal void _RPCMultiStateSyncProxy(NetworkMessage msg)
		{
			_AssertIsConnected();

			if (isServerOrCellServer)
			{
				if(!(!isAuthoritativeServer)){Utility.Exception( "Authoritative server received multiple proxy states when it shouldn't");}
			}
			else
			{
				_AssertSenderIsServerOrCellServer(msg);
			}

			Log.Info(NetworkLogFlags.StateSync, "Multiple proxy state synchronization received: ", msg);

			var count = msg.stream.ReadUInt32();
			for (var i = 0; i < count; ++i)
			{
				NetBuffer data;
				NetworkViewID viewID;
				StateSync._Read(msg.stream._buffer, out viewID, out data);

				BitStream stream = new BitStream(data, false);
				_UpdateProxyState(viewID, stream, msg);
			}
		}

		internal void _RPCMultiStateSyncOwner(StateSync[] states, NetworkMessage msg)
		{
			_AssertIsClientConnected();
			_AssertSenderIsServerOrCellServer(msg);

			if(!(isAuthoritativeServer)){Utility.Exception( "Received multiple owner states when it shouldn't");}

			Log.Info(NetworkLogFlags.StateSync, "Multiple owner state synchronization received: ", msg);

			if (!useDifferentStateForOwner)
			{
				Log.Warning(NetworkLogFlags.StateSync, "Owner states where dropped because they should not be used");
				return;
			}

			foreach (var state in states)
			{
				BitStream stream = new BitStream(state.data.buffer, false);
				_UpdateOwnerState(state.viewID, stream, msg);
			}
		}

		internal void _RPCMultiStateSyncCellProxy(NetworkMessage msg)
		{
			_AssertIsCellServerConnected();
			_AssertSenderIsServerOrCellServer(msg);

			Log.Info(NetworkLogFlags.StateSync, "Multiple cell proxy state synchronization received: ", msg);

			var count = msg.stream.ReadUInt32();
			for (var i = 0; i < count; ++i)
			{
				NetBuffer data;
				NetworkViewID viewID;
				StateSync._Read(msg.stream._buffer, out viewID, out data);

				BitStream stream = new BitStream(data, false);
				_UpdateCellProxyState(viewID, stream, msg);
			}
		}

		internal abstract void _ClientSendStateSync(NetworkMessage state);
		internal abstract void _ServerSendStateSync(NetworkMessage state);

		private void _CreateForOthers(NetworkViewID viewID, NetworkPlayer owner, NetworkAuthFlags authFlags, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			var msg = new NetworkMessage(this, NetworkFlags.TypeUnsafe, NetworkMessage.Channel.RPC, String.Empty, NetworkMessage.InternalCode.Create, NetworkPlayer.unassigned, NetworkPlayer.unassigned, viewID);
			msg.stream.WriteNetworkPlayer(owner);
			msg.stream.WriteNetworkGroup(group);
			msg.stream.WriteByte((byte)authFlags);
			msg.stream.WriteVector3(pos);
			msg.stream.WriteQuaternion(rot);
			msg.stream.WriteString(proxyPrefab);
			msg.stream.WriteString(ownerPrefab);
			msg.stream.WriteString(serverPrefab);
			msg.stream.WriteString(cellAuthPrefab);
			msg.stream.WriteString(cellProxyPrefab);
			_initialWriter.Write(msg.stream, proxyPrefab, initialData);

			_SendRPC(msg);
		}

		// TODO: create InstantiateForOthers overloads!

		public NetworkViewBase Instantiate(string prefab, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			return Instantiate(_localPlayer, prefab, pos, rot, group, initialData);
		}

		public NetworkViewBase Instantiate(string othersPrefab, string ownerPrefab, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			return Instantiate(_localPlayer, othersPrefab, ownerPrefab, isServer ? ownerPrefab : othersPrefab, pos, rot, group, initialData);
		}

		public NetworkViewBase Instantiate(NetworkPlayer owner, string prefab, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			return Instantiate(owner, prefab, prefab, prefab, pos, rot, group, initialData);
		}

		public NetworkViewBase Instantiate(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			var viewID = AllocateViewID(owner, group);
			return Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, pos, rot, group, initialData);
		}

		public NetworkViewBase Instantiate(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			var viewID = AllocateViewID(owner, group);
			return Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, pos, rot, group, initialData);
		}

		public NetworkViewBase Instantiate(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			return Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, null, null, NetworkAuthFlags.None, pos, rot, group, initialData);
		}

		public NetworkViewBase Instantiate(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 pos, Quaternion rot, NetworkGroup group, params object[] initialData)
		{
			_AssertIsAuthoritative();
			if(!(viewID != NetworkViewID.unassigned)){Utility.Exception( "viewID must be assigned");}
			if(!(owner != NetworkPlayer.unassigned)){Utility.Exception( "owner must be assigned");}
			if(!(isServerOrCellServer || owner == _localPlayer)){Utility.Exception( "Client can't instantiate for other players");}

			// TODO: fulhacks, this might not be problem free to do.
			if (owner.isCellServer || owner == NetworkPlayer.cellProxies) owner = NetworkPlayer.server;

			_CreateForOthers(viewID, owner, authFlags, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, pos, rot, group, initialData);

			var stream = new BitStream(true, false);
			_initialWriter.Write(stream, proxyPrefab, initialData);
			stream._isWriting = false; // the user needs to be able to read from it.
			return _Create(authFlags, false, pos, rot, viewID, owner, group, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, stream, new NetworkMessage(this));
		}

		internal NetworkViewBase _Create(NetworkAuthFlags authFlags, bool isInstantiatedRemotely, Vector3 pos, Quaternion rot, NetworkViewID viewID, NetworkPlayer owner, NetworkGroup group, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, BitStream initialData, NetworkMessage msg)
		{
			string localPrefab =
				(isServerOrCellServer) ?
				(isServer? serverPrefab : (isInstantiatedRemotely? cellProxyPrefab : cellAuthPrefab)) :
				(_localPlayer == owner ? ownerPrefab : proxyPrefab);

			var args = new NetworkInstantiateArgs(pos, rot, viewID, owner, group, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, isInstantiatedRemotely, initialData);

			return _Create(localPrefab, args, msg);
		}

		public NetworkViewID AllocateViewID()
		{
			return _AllocateViewID(_localPlayer, NetworkGroup.unassigned);
		}

		public NetworkViewID AllocateViewID(NetworkGroup group)
		{
			return _AllocateViewID(_localPlayer, group);
		}

		public NetworkViewID[] AllocateViewIDs(int count)
		{
			return _AllocateViewIDs(count, _localPlayer, NetworkGroup.unassigned);
		}

		public NetworkViewID[] AllocateViewIDs(int count, NetworkGroup group)
		{
			return _AllocateViewIDs(count, _localPlayer, group);
		}

		public NetworkViewID AllocateViewID(NetworkPlayer owner)
		{
			return _AllocateViewID(owner, NetworkGroup.unassigned);
		}

		public NetworkViewID AllocateViewID(NetworkPlayer owner, NetworkGroup group)
		{
			_AssertIsConnected();
			_AssertIsAuthoritative();
			if(!(owner != NetworkPlayer.unassigned)){Utility.Exception( "owner must be assigned");}

			// TODO: check if viewID is assigned else log error "out of viewIDs"
			return _AllocateViewID(owner, group);
		}

		public NetworkViewID[] AllocateViewIDs(int count, NetworkPlayer owner)
		{
			return _AllocateViewIDs(count, owner, NetworkGroup.unassigned);
		}

		public NetworkViewID[] AllocateViewIDs(int count, NetworkPlayer owner, NetworkGroup group)
		{
			_AssertIsConnected();
			_AssertIsAuthoritative();
			if(!(owner != NetworkPlayer.unassigned)){Utility.Exception( "owner must be assigned");}

			// TODO: check if viewIDs is assigned else log error "out of viewIDs"
			return _AllocateViewIDs(count, owner, group);
		}

		protected abstract NetworkViewID _AllocateViewID(NetworkPlayer owner, NetworkGroup group);
		protected abstract NetworkViewID[] _AllocateViewIDs(int count, NetworkPlayer owner, NetworkGroup group);

		internal static void _AssignConnectionPlayerID(NetConnection connection, NetworkPlayer player)
		{
			Log.Debug(NetworkLogFlags.PlayerID, "Assigned ", player, " to ", connection);

			connection.Tag = player;
		}

		internal static void _UnassignConnectionPlayerID(NetConnection connection)
		{
			Log.Debug(NetworkLogFlags.PlayerID, "Unassigned ", connection.Tag, " from ", connection);

			connection.Tag = null;
		}

		internal static bool _HasConnectionPlayerID(NetConnection connection)
		{
			return (connection.Tag != null && (NetworkPlayer)connection.Tag != NetworkPlayer.unassigned);
		}

		internal static NetworkPlayer _GetConnectionPlayerID(NetConnection connection)
		{
			return (connection.Tag != null) ? (NetworkPlayer)connection.Tag : NetworkPlayer.unassigned;
		}

		internal NetworkPlayer _FindConnectionPlayerID(NetConnection connection)
		{
			if (isServer)
				return _ServerFindConnectionPlayerID(connection);

			if (isClient)
				return _ClientFindConnectionPlayerID(connection);

			// TODO: what if _isCellServer?
			return NetworkPlayer.unassigned;
		}

		internal abstract NetworkPlayer _ServerFindConnectionPlayerID(NetConnection connection);
		internal abstract NetworkPlayer _ClientFindConnectionPlayerID(NetConnection connection);

		public NetworkEmulation emulation
		{
			get { return _emulation; }
		}

		public NetworkConfig config
		{
			get { return _config; }
		}

		// TODO: use this in internal rpcs
		internal void _AssertSenderIsRemote(NetworkMessage msg)
		{
			if (msg.sender == _localPlayer)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertSenderIsServer(NetworkMessage msg)
		{
			if (!msg.sender.isServer)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertSenderIsServerOrCellServer(NetworkMessage msg)
		{
			if (!msg.sender.isServerOrCellServer)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsServerListening()
		{
			if(!(isServer && _status == NetworkStatus.Connected)){Utility.Exception( "Must be a initialized server");}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsServerListeningOrShuttingdown()
		{
			if (!isServer || (_status != NetworkStatus.Connected && _status != NetworkStatus.Disconnecting))
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsServerOrCellServer()
		{
			if (!isServerOrCellServer)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsClientConnected()
		{
			if (!isClient || _status != NetworkStatus.Connected)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsClientConnecting()
		{
			if (!isClient || _status != NetworkStatus.Connecting)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsCellServerConnected()
		{
			if (!isCellServer || _status != NetworkStatus.Connected)
			{
				throw new NetworkException("CellServer in not in connected state when it should be in that state.");
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsCellServerConnecting()
		{
			if (!isCellServer || _status != NetworkStatus.Connecting)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsClientOrCellServerConnectedOrConnecting()
		{
			if ((!isClient && !isCellServer) || (_status != NetworkStatus.Connected && _status != NetworkStatus.Connecting))
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsConnected()
		{
			if (_status != NetworkStatus.Connected)
			{
				//TODO: Utility.Exception("Server or client must be fully initialized to preform this action");
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsConnectedOrDisconnecting()
		{
			if (_status != NetworkStatus.Connected && _status != NetworkStatus.Disconnecting)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsClientConnectedOrDisconnecting()
		{
			if (!isClient || (_status != NetworkStatus.Connected && _status != NetworkStatus.Disconnecting))
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsClientOrCellServerConnected()
		{
			if (!isClientOrCellServer || _status != NetworkStatus.Connected)
			{
				// TODO: throw NetworkException
			}
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsDisconnected()
		{
			if (_status != NetworkStatus.Disconnected)
			{
				// TODO: throw NetworkException
			}
		}

		internal void _AssertIsDebug()
		{
#if !DEBUG
			// TODO: throw NetworkException
#endif
		}

		// TODO: use this in internal rpcs
		internal void _AssertIsAuthoritative()
		{
			if (isAuthoritativeServer && !isServerOrCellServer)
			{
				throw new NetworkException("Can't execute this command on a non-authoritative " + _localPlayer);
			}
		}

		internal void _AssertRPC(NetworkViewID viewID, RPCMode mode)
		{
			_AssertIsConnected();

			if (viewID == NetworkViewID.unassigned)
			{
				throw new ArgumentOutOfRangeException("viewID");
			}

			if (mode != RPCMode.Server)
			{
				_AssertIsAuthoritative();
			}
		}

		internal void _AssertRPC(NetworkViewID viewID, NetworkPlayer target)
		{
			_AssertIsConnected();

			if (viewID == NetworkViewID.unassigned)
			{
				throw new ArgumentOutOfRangeException("viewID");
			}

			if (target == NetworkPlayer.unassigned)
			{
				throw new ArgumentOutOfRangeException("target");
			}

			if (target != NetworkPlayer.server)
			{
				_AssertIsAuthoritative();
			}
		}

		internal void _AssertRPC(NetworkViewID viewID, IEnumerable<NetworkPlayer> targets)
		{
			_AssertIsConnected();

			if (viewID == NetworkViewID.unassigned)
			{
				throw new ArgumentOutOfRangeException("viewID");
			}

			foreach (var target in targets)
			{
				if (target == NetworkPlayer.unassigned)
				{
					throw new ArgumentException("targets", "Can't contain NetworkPlayer.unassigned");
				}
			}

			if (isAuthoritativeServer && isClient)
			{
				foreach (var target in targets)
				{
					if (target != NetworkPlayer.server)
					{
						// TODO: throw NetworkException
					}
				}
			}
		}

		internal void _AssertGroup(NetworkGroup group)
		{
			if (group == NetworkGroup.unassigned)
			{
				throw new ArgumentOutOfRangeException("group");
			}
		}

		internal void _AssertViewID(NetworkViewID viewID)
		{
			if (viewID == NetworkViewID.unassigned)
			{
				throw new ArgumentOutOfRangeException("viewID");
			}
		}

		internal void _AssertPlayer(NetworkPlayer target)
		{
			if (target == NetworkPlayer.unassigned)
			{
				throw new ArgumentOutOfRangeException("target");
			}
		}
	}
}
