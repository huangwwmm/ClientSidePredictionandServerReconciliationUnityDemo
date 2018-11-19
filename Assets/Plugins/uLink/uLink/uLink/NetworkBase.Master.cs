#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12248 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-31 09:35:06 +0200 (Thu, 31 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	internal abstract class NetworkBaseMaster : NetworkBaseNAT 
	{
		private double _timeOfHostListRequest = 0;
		private double _timeOfDiscoveryRequest = 0;
		private double _timeOfKnownHostsRequest = 0;

		private string _gameType = String.Empty;
		private string _gameName = String.Empty;
		private bool _gameTypeOrNameIsDirty = true;

		public string gameMode = String.Empty;
		public string gameLevel = String.Empty;
		public string comment = String.Empty;
		public bool dedicatedServer;
		public IPAddress localIpAddress;

		private Dictionary<int, NetworkMasterMessage> _pendingMessages = new Dictionary<int, NetworkMasterMessage>();

		private readonly List<HostData> _registeredHosts = new List<HostData>();
		private readonly Dictionary<NetworkEndPoint, HostData> _discoveredHosts = new Dictionary<NetworkEndPoint, HostData>();
		private readonly Dictionary<NetworkEndPoint, HostData> _knownHosts = new Dictionary<NetworkEndPoint, HostData>();

		// TODO: make readonly
		private NetClient _master;
		private NetBuffer _readBuffer;

		public string ipAddress { get { return _masterIP; } set { _masterIP = value; } }
		public int port { get { return _masterPort; } set { _masterPort = value; } }

		// TODO: fix asserts for null string instead of "value ?? String.Empty".
		public string gameType { get { return _gameType; } set { _gameType = value ?? String.Empty; _gameTypeOrNameIsDirty = true; } }
		public string gameName { get { return _gameName; } set { _gameName = value ?? String.Empty; _gameTypeOrNameIsDirty = true; } }
		
		public float ping 
		{
			get
			{
				if (_master == null || _master.ServerConnection == null || _master.ServerConnection.Status != NetConnectionStatus.Connected)
					return 0;
				else
					return _master.ServerConnection.AverageRoundtripTime;
			}
		}

		public float updateRate
		{
			set
			{
				_intervalUpdateHostData = 1.0f / value;

				if (!Single.IsInfinity(_intervalUpdateHostData))
				{
					_nextUpdateHostData = NetworkTime.localTime + _intervalUpdateHostData;
				}
			}

			get
			{
				return 1.0f / _intervalUpdateHostData;
			}
		}

		private void _MasterInit()
		{
			var config = new NetConfiguration(Constants.CONFIG_MASTER_IDENTIFIER);
			config.MaxConnections = 1;
			config.PingFrequency = 6F; //Higher than default 3F to avoid too high load on the master server when there are thousands of clients.
			config.TimeoutDelay = 60F;
			config.AnswerDiscoveryRequests = false;
			config.SendBufferSize = Constants.DEFAULT_SEND_BUFFER_SIZE;
			config.ReceiveBufferSize = Constants.DEFAULT_RECV_BUFFER_SIZE;

			_master = new NetClient(config);
			_ConfigureNetBase(_master);

			_readBuffer = _master.CreateBuffer();
		}

		internal override NetBase _MasterGetNetBase()
		{
			return _master;
		}

		internal override void _MasterSendProxyRequest(NetworkEndPoint host, string password)
		{
			_MasterConnect();

			Log.Info(NetworkLogFlags.MasterServer | NetworkLogFlags.Client, "Sending proxy request for host ", host, String.IsNullOrEmpty(password) ? " without password" : " with password");

			var msg = new NetworkMasterMessage(NetworkMasterMessage.InternalCode.ProxyRequest);
			msg.stream.WriteEndPoint(host);
			msg.stream.WritePassword(new Password(password));

			if (_master.Status == NetConnectionStatus.Connected)
			{
				_master.SendMessage(msg.stream._buffer, NetChannel.ReliableInOrder1);
			}
			else
			{
				_pendingMessages[(int)NetworkMasterMessage.InternalCode.ProxyRequest] = msg;
			}
		}

		internal override NetworkEndPoint _MasterGetMasterServerEndPoint()
		{
			return _master != null ? _master.ServerConnection.RemoteEndpoint : NetworkEndPoint.unassigned;
		}

		internal override LocalHostData _MasterGetLocalHostData(bool errorCheck, bool notifyOnError)
		{
			if (errorCheck)
			{
				if (_gameTypeOrNameIsDirty)
				{
					_gameTypeOrNameIsDirty = false;
					notifyOnError = true;
				}

				if (!isServer || _status != NetworkStatus.Connected)
				{
					if (notifyOnError) _MasterNotifyEvent(MasterServerEvent.RegistrationFailedNoServer);
					return null;
				}

				if (String.IsNullOrEmpty(_gameType))
				{
					if (notifyOnError) _MasterNotifyEvent(MasterServerEvent.RegistrationFailedGameType);
					return null;
				}

				if (String.IsNullOrEmpty(_gameName))
				{
					if (notifyOnError) _MasterNotifyEvent(MasterServerEvent.RegistrationFailedGameName);
					return null;
				}
			}

			if (localIpAddress == null) localIpAddress = Utility.TryGetLocalIP();
			var localEndPoint = new NetworkEndPoint(localIpAddress, listenPort);

			int countServerAsPlayer = (dedicatedServer ? 0 : 1);
			
			var data = new LocalHostData(_gameType, _gameName, gameMode, gameLevel, connectionCount + countServerAsPlayer, _ServerGetPlayerLimit() + countServerAsPlayer, !String.IsNullOrEmpty(incomingPassword), dedicatedServer, useNat, useProxy, comment, OnGetPlatform(), DateTime.UtcNow, localEndPoint);
			return data;
		}

		public void ClearHostList()
		{
			_registeredHosts.Clear();
		}

		public HostData[] PollHostList()
		{
			return _registeredHosts.ToArray();
		}

		public void RequestHostList(string filterGameType)
		{
			RequestHostList(new HostDataFilter(filterGameType));
		}

		public void RequestHostList(HostDataFilter filter)
		{
			_timeOfHostListRequest = NetworkTime.localTime;

			_MasterConnect();

			var msg = new NetworkMasterMessage(NetworkMasterMessage.InternalCode.HostListRequest);
			msg.stream.WriteHostDataFilter(filter);

			if (_master.Status == NetConnectionStatus.Connected)
			{
				_master.SendMessage(msg.stream._buffer, NetChannel.ReliableInOrder1);
			}
			else
			{
				_pendingMessages[(int)NetworkMasterMessage.InternalCode.HostListRequest] = msg; 
			}
		}

		public HostData[] PollAndRequestHostList(string filterGameType, float requestInterval)
		{
			return PollAndRequestHostList(new HostDataFilter(filterGameType), requestInterval);
		}

		public HostData[] PollAndRequestHostList(HostDataFilter filter, float requestInterval)
		{
			if (!Single.IsInfinity(requestInterval))
			{
				var nextRequest = _timeOfHostListRequest + requestInterval;
				if (nextRequest <= NetworkTime.localTime)
				{
					RequestHostList(filter);
				}
			}

			return PollHostList();
		}

		public void RegisterHost(string gameType, string gameName)
		{
			_gameType = gameType;
			_gameName = gameName;
			_gameTypeOrNameIsDirty = true;

			RegisterHost();
		}

		public void RegisterHost(string gameType, string gameName, string comment)
		{
			this.comment = comment;

			RegisterHost(gameType, gameName);
		}

		public void RegisterHost(string gameType, string gameName, string comment, string gameMode, string gameLevel)
		{
			this.gameMode = gameMode;
			this.gameLevel = gameLevel;

			RegisterHost(gameType, gameName, comment);
		}

		public void ClearDiscoveredHosts()
		{
			_discoveredHosts.Clear();
		}

		public HostData[] PollDiscoveredHosts()
		{
			return Utility.ToArray(_discoveredHosts.Values);
		}

		public void DiscoverLocalHosts(string filterGameType, int remotePort)
		{
			DiscoverLocalHosts(new HostDataFilter(filterGameType), remotePort);
		}

		public void DiscoverLocalHosts(string filterGameType, int remoteStartPort, int remoteEndPort)
		{
			DiscoverLocalHosts(new HostDataFilter(filterGameType), remoteStartPort, remoteEndPort);
		}

		public void DiscoverLocalHosts(HostDataFilter filter, int remotePort)
		{
			DiscoverLocalHosts(filter, remotePort, remotePort);
		}

		public void DiscoverLocalHosts(HostDataFilter filter, int remoteStartPort, int remoteEndPort)
		{
			if (remoteEndPort - remoteStartPort >= 20)
			{
				Log.Warning(NetworkLogFlags.MasterServer, "Sending broadcast packets on more than 20 ports (with frequent interval) to discover local hosts, may cause some routers to block UDP traffic or behave undesirably.");
			}

			_timeOfDiscoveryRequest = NetworkTime.localTime;

			for (int port = remoteStartPort; port <= remoteEndPort; port++)
			{
				var msg = new UnconnectedMessage(UnconnectedMessage.InternalCode.DiscoverHostRequest);
				msg.stream.WriteHostDataFilter(filter);
				msg.stream.WriteDouble(NetworkTime.localTime);

				var broadcast = new NetworkEndPoint(IPAddress.Broadcast, port);
				_SendUnconnectedRPC(msg, broadcast);
			}
		}

		public HostData[] PollAndDiscoverLocalHosts(string filterGameType, int remotePort, float discoverInterval)
		{
			return PollAndDiscoverLocalHosts(new HostDataFilter(filterGameType), remotePort, discoverInterval);
		}

		public HostData[] PollAndDiscoverLocalHosts(string filterGameType, int remoteStartPort, int remoteEndPort, float discoverInterval)
		{
			return PollAndDiscoverLocalHosts(new HostDataFilter(filterGameType), remoteStartPort, remoteEndPort, discoverInterval);
		}

		public HostData[] PollAndDiscoverLocalHosts(HostDataFilter filter, int remotePort, float discoverInterval)
		{
			if (!Single.IsInfinity(discoverInterval))
			{
				var nextRequest = _timeOfDiscoveryRequest + discoverInterval;
				if (nextRequest <= NetworkTime.localTime)
				{
					DiscoverLocalHosts(filter, remotePort);
				}
			}

			return PollDiscoveredHosts();
		}

		public HostData[] PollAndDiscoverLocalHosts(HostDataFilter filter, int remoteStartPort, int remoteEndPort, float discoverInterval)
		{
			if (!Single.IsInfinity(discoverInterval))
			{
				var nextRequest = _timeOfDiscoveryRequest + discoverInterval;
				if (nextRequest <= NetworkTime.localTime)
				{
					DiscoverLocalHosts(filter, remoteStartPort, remoteEndPort);
				}
			}

			return PollDiscoveredHosts();
		}

		public void ClearKnownHosts()
		{
			_knownHosts.Clear();
		}

		public HostData PollKnownHostData(string host, int remotePort)
		{
			return PollKnownHostData(Utility.Resolve(host, remotePort));
		}

		public HostData PollKnownHostData(NetworkEndPoint target)
		{
			HostData data;

			if (!_knownHosts.TryGetValue(target, out data)) return null;

			return data;
		}

		public HostData[] PollKnownHosts()
		{
			return Utility.ToArray(_knownHosts.Values);
		}

		public void RequestKnownHostData(string host, int remotePort)
		{
			RequestKnownHostData(Utility.Resolve(host, remotePort));
		}

		public void RequestKnownHostData(NetworkEndPoint target)
		{
			if (!_knownHosts.ContainsKey(target)) _knownHosts.Add(target, new HostData(target));

			var msg = new UnconnectedMessage(UnconnectedMessage.InternalCode.KnownHostRequest);
			msg.stream.WriteDouble(NetworkTime.localTime);
			msg.stream.WriteBoolean(false);
			_SendUnconnectedRPC(msg, target);
		}

		public void AddKnownHostData(string host, int remotePort)
		{
			AddKnownHostData(Utility.Resolve(host, remotePort));
		}

		public void AddKnownHostData(NetworkEndPoint target)
		{
			if (!_knownHosts.ContainsKey(target)) AddKnownHostData(new HostData(target));
		}

		public void AddKnownHostData(HostData data)
		{
			_knownHosts[data.externalEndpoint] = data;
		}

		public void RemoveKnownHostData(string host, int remotePort)
		{
			RemoveKnownHostData(Utility.Resolve(host, remotePort));
		}

		public void RemoveKnownHostData(NetworkEndPoint target)
		{
			_knownHosts.Remove(target);
		}

		public void RequestKnownHosts()
		{
			_timeOfKnownHostsRequest = NetworkTime.localTime;

			foreach (var pair in _knownHosts)
			{
				RequestKnownHostData(pair.Key);
			}
		}

		public HostData[] PollAndRequestKnownHosts(float requestInterval)
		{
			if (!Single.IsInfinity(requestInterval))
			{
				var nextRequest = _timeOfKnownHostsRequest + requestInterval;
				if (nextRequest <= NetworkTime.localTime)
				{
					RequestKnownHosts();
				}
			}

			return PollKnownHosts();
		}

		internal override void _MasterDisconnect()
		{
			if (_master == null) return;

			if (_master.ServerConnection != null && _master.ServerConnection.Status != NetConnectionStatus.Disconnected)
			{
				_master.ServerConnection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, 0, true, true);
			}

			// TODO: ugly fucking hack to workaround a lidgren bug!
			_master.Dispose();
			_master = null;
			_readBuffer = null;
			_MasterInit();
			///////////////////////////////////////////////////////

			_pendingMessages.Clear();
		}

		private void _MasterStart()
		{
			if (_master == null)
			{
				_MasterInit();
			}

			if (_master.IsListening) return;

			_PreStart(NetworkStartEvent.MasterServer);

			try
			{
				_master.Start();
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.MasterServer, "Failed to start MasterServer connection: ", e);

				_MasterFailConnection(NetworkConnectionError.CreateSocketOrThreadFailure);
			}

			_Start();
		}

		private void _MasterConnect()
		{
			_MasterStart();

			NetworkEndPoint target = Utility.Resolve(ipAddress, port);

			if (_master.Status == NetConnectionStatus.Connected || _master.Status == NetConnectionStatus.Connecting || _master.Status == NetConnectionStatus.Reconnecting)
			{
				if (_master.ServerConnection.RemoteEndpoint.Equals(target)) return;

				_MasterDisconnect();
			}

			try
			{
				_master.Connect(target, _masterPasswordHash.hash);
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.MasterServer, "Failed to connect to MasterServer: ", e);

				_MasterFailConnection(NetworkConnectionError.ConnectionFailed);
			}
		}

		internal override void _MasterUpdate(double deltaTime)
		{
			if (_master != null)
			{
				_MasterCheckMessages();

				if (!NetUtility.SafeHeartbeat(_master))
				{
					_MasterDisconnect();
					return;
				}
			}
		}

		private void _MasterCheckMessages()
		{
			NetMessageType type;
			NetworkEndPoint endpoint;

			while (_master.ReadMessage(_readBuffer, out type, out endpoint))
			{
				switch (type)
				{
					case NetMessageType.Data:
						_HandleMasterMessage(_readBuffer);
						break;

					case NetMessageType.StatusChanged:
						_OnLidgrenStatusChanged();
						break;

					case NetMessageType.ConnectionRejected:
						_MasterDisconnect();
						_MasterFailConnection(_readBuffer.ReadString());
						break;

					case NetMessageType.OutOfBandData:
						_HandleUnconnectedMessage(_readBuffer, endpoint);
						break;

					case NetMessageType.DebugMessage:
					case NetMessageType.VerboseDebugMessage:
						Log.Debug(NetworkLogFlags.BadMessage, "Debug message: ", _readBuffer.ReadString()); // TODO: when are this called
						break;

					case NetMessageType.BadMessageReceived:
						Log.Warning(NetworkLogFlags.BadMessage, "Received bad message: ", _readBuffer.ReadString());
						break;
				}
			}
		}

		private void _OnLidgrenStatusChanged()
		{
			Log.Info(NetworkLogFlags.MasterServer, "MasterServer connection status is now ", _master.Status);

			if (_master.Status == NetConnectionStatus.Connected)
			{
				foreach (var msg in _pendingMessages)
				{
					_master.SendMessage(msg.Value.stream._buffer, NetChannel.ReliableInOrder1);
				}

				_pendingMessages.Clear();

				if (_master.ServerConnection.RemoteHailData != null && _master.ServerConnection.RemoteHailData.Length != 0)
				{
					try
					{
						_masterGaveMyEndpoint = new NetworkEndPoint(new IPAddress(_master.ServerConnection.RemoteHailData), 0);
					}
					catch (Exception ex)
					{
						Log.Error(NetworkLogFlags.MasterServer, "Failed to receive valid external IP ", NetUtility.BytesToHex(_master.ServerConnection.RemoteHailData), " from MasterServer: ", ex);
					}
					
				}
			}
			else if (_master.Status == NetConnectionStatus.Disconnected)
			{
				_MasterDisconnect();
			}
		}

		internal override void _MasterNotifyEvent(MasterServerEvent eventCode)
		{
			_Notify("OnMasterServerEvent", eventCode);
		}

		internal void _UnconnectedRPCDiscoverHostResponse(LocalHostData localData, double localTime, NetworkEndPoint endpoint)
		{
			var ping = (int)NetworkTime._GetElapsedTimeInMillis(localTime);
			var data = new HostData(localData, endpoint, ping);

			if (_discoveredHosts.ContainsKey(endpoint))
				_discoveredHosts[endpoint] = data;
			else
				_discoveredHosts.Add(endpoint, data);

			_MasterNotifyEvent(MasterServerEvent.LocalHostDiscovered);
		}

		internal void _UnconnectedRPCKnownHostResponse(LocalHostData localData, double localTime, NetworkEndPoint endpoint)
		{
			if (!_knownHosts.ContainsKey(endpoint)) return;

			var ping = (int)NetworkTime._GetElapsedTimeInMillis(localTime);
			var data = new HostData(localData, endpoint, ping);
			_knownHosts[endpoint] = data;

			_MasterNotifyEvent(MasterServerEvent.KnownHostDataReceived);
		}

		internal void _MasterRPCHostListResponse(HostData[] hosts)
		{
			_registeredHosts.Clear();
			_registeredHosts.AddRange(hosts);

			_MasterNotifyEvent(MasterServerEvent.HostListReceived);
		}

		internal void _MasterRPCProxyResponse(ushort sessionPort, Password sessionPassword)
		{
			Log.Info(NetworkLogFlags.MasterServer | NetworkLogFlags.Client, "Received proxy response with session port ", sessionPort);

			_ProxyConnectTo(new NetworkEndPoint(_master.ServerConnection.RemoteEndpoint.ipAddress, sessionPort), sessionPassword);
		}

		internal void _MasterRPCProxyFailed(int errorCode)
		{
			Log.Info(NetworkLogFlags.MasterServer | NetworkLogFlags.Client, "Received proxy failed with error code ", errorCode);

			_ProxyFailed((NetworkConnectionError)errorCode);
		}

		private void _SendUnconnectedRPC(UnconnectedMessage msg, NetworkEndPoint target)
		{
			_MasterStart();

			Log.Debug(NetworkLogFlags.RPC, "Sending unconnected RPC ", msg.internCode, " to ", target);

			_master.SendOutOfBandMessage(msg.stream._buffer, target);
		}

		internal abstract string OnGetPlatform();

		private void _MasterFailConnection(NetworkConnectionError error)
		{
			// TODO: cleanup

			lastMasterError = error;
			_Notify("OnFailedToConnectToMasterServer", error);
		}
	}
}
