#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11266 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-02-02 21:26:58 +0100 (Thu, 02 Feb 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Collections.Generic;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

// TODO: add MultiClientSimulator e.g. AddConnection

namespace uLink
{
	internal abstract class NetworkBaseClient : NetworkBaseLocal
	{
		private struct FallbackConnect
		{
			public NetworkEndPoint server;
			public bool useProxy;
			public string incomingPassword;
			public object[] loginData;
		}

		private FallbackConnect _fallbackConnect;

		public bool useProxy = false; // TODO: also be able to only use for client?

		// TODO: implement these:
		public string proxyIP = "";
		public string proxyPassword = "";
		public int proxyPort = 0;

		private double _proxyConnectTimeout = Double.NaN;

		// TODO: use this by the client and server
		public bool useNat = false;

		private NetworkViewIDAllocator _viewIDAllocator = new NetworkViewIDAllocator(false);

		private readonly Dictionary<NetworkViewID, KeyValuePair<NetworkPlayer, NetworkGroup>> _allocatedViewIDs = new Dictionary<NetworkViewID, KeyValuePair<NetworkPlayer, NetworkGroup>>();

		private double _preConnectTimeout = Double.NaN;
		private readonly Dictionary<NetworkEndPoint, string> _preConnectTo = new Dictionary<NetworkEndPoint, string>();

		// TODO: reuse _loginData if redirected before connected
		private object[] _loginData = new object[0];

		private NetClient _client = null;
		private NetworkStatistics _clientConnStats = null;
		protected double _statsCalculationInterval = 0.5;

		protected double _statsLastCalculation = 0.0;

		private SecurityLayer _security = null;

		private BitStream _approvalData = null;

		private float _trackPositionRate = 2f;
		private float _maxSqrDeltaTrackPosition = 0;

		public bool requireSecurityForConnecting;
		private double _securityRequestTimeout = Double.NaN;

		public int symmetricKeySize = SymmetricKey.DEFAULT_BIT_STRENGTH;

		internal NetworkEndPoint _masterGaveMyEndpoint = NetworkEndPoint.unassigned;

		public double statsCalculationInterval
		{
			get { return _statsCalculationInterval; }
			set { _statsCalculationInterval = (value > 0.0) ? value : 0.0; }
		}

		private Vector3 _cellPosition;
		internal Vector3 cellPosition
		{
			get
			{
				if (_peerType != NetworkPeerType.CellServer) throw new NetworkException("Must be a cell server to read cellPosition");
				return _cellPosition;
			}
		}

		private readonly List<NetworkViewBase> _trackViews = new List<NetworkViewBase>();
		private int _trackViewIndex;
		private NetworkMessage _trackMessage;
		private int _trackMessageHeaderPos;
		private double _lastTrackSent = 0;
		private int _numberOfTracks;

		public float trackRate
		{
			set
			{
				_trackPositionRate = value;
			}

			get
			{
				return _trackPositionRate;
			}
		}

		public float trackMaxDelta
		{
			set
			{
				_maxSqrDeltaTrackPosition = value * value;
			}

			get
			{
				return (float) Math.Sqrt(_maxSqrDeltaTrackPosition);
			}
		}

		public BitStream approvalData
		{
			get
			{
				_AssertIsClientConnectedOrDisconnecting();
				return _approvalData;
			}
		}

		public object[] loginData
		{
			get
			{
				_AssertIsClientConnectedOrDisconnecting();
				return _loginData;
			}
		}

		public static bool _CheckTarget(NetworkEndPoint target)
		{
			if (target.isUnassigned || target.isNone || target.isAny || target.isBroadcast || target.port == 0)
			{
				return false;
			}

			return true;
		}

		public static bool _CheckTargets(NetworkEndPoint[] targets)
		{
			foreach (var target in targets)
			{
				if (!_CheckTarget(target)) return false;
			}

			return true;
		}

		public NetworkConnectionError Connect(string[] hosts, int remotePort, string incomingPassword, params object[] loginData)
		{
			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, "Connect", String.Concat(Utility.EscapeURL(String.Join(";", hosts)), ":", remotePort));
			return Connect(Utility.Resolve(hosts, remotePort), incomingPassword, loginData);
		}

		public NetworkConnectionError Connect(NetworkEndPoint[] hosts, string incomingPassword, params object[] loginData)
		{
			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, "Connect", Utility.EscapeURL(Utility.Join(";", hosts)));
			return _PreConnectTo(NetworkPeerType.Client, hosts, incomingPassword, loginData);
		}

		public NetworkConnectionError Connect(string host, int remotePort, string incomingPassword, params object[] loginData)
		{
			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, "Connect", Utility.EscapeURL(host) + remotePort);
			return Connect(Utility.Resolve(host, remotePort), incomingPassword, loginData);
		}

		public NetworkConnectionError Connect(string ipOrHostInclPort, string incomingPassword, params object[] loginData)
		{
			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, "Connect", Utility.EscapeURL(ipOrHostInclPort));
			return _ProxyStart(NetworkUtility.ResolveEndPoint(ipOrHostInclPort, 0), incomingPassword, loginData);
		}

		public NetworkConnectionError Connect(HostData host, string incomingPassword, params object[] loginData)
		{
			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, "Connect", Utility.EscapeURL(host.externalEndpoint.ToString()));

			if (host.externalEndpoint.isPrivate) // the host is in the MasterServer's LAN
			{
				// TODO: ugly fulhacks!! should be part of the HostData instead.
				var masterEndPoint = _MasterGetMasterServerEndPoint();
				if (masterEndPoint.isIPEndPoint && masterEndPoint.isPublic) // we are not in the MasterServer's LAN
				{
					var assumeExternalEndpoint = new NetworkEndPoint(masterEndPoint.ipAddress, host.externalEndpoint.port);
					return Connect(assumeExternalEndpoint, incomingPassword, loginData);
				}
			}

			// TODO: should make sure the internalEndpoint is the host we're looking for by comparing GUID during handshake! Otherwise it could be some other server, if the LAN consists of multiple NATs.

			if (!_masterGaveMyEndpoint.isUnassigned && _masterGaveMyEndpoint.Equals(host.externalEndpoint)) // the host is in my LAN
			{
				_fallbackConnect = new FallbackConnect
				{
					server = host.externalEndpoint,
					useProxy = host.useProxy,
					incomingPassword = incomingPassword,
					loginData = loginData
				};

				return Connect(host.internalEndpoint, incomingPassword, loginData);
			}

			return host.useProxy ?
				_ProxyStart(host.externalEndpoint, incomingPassword, loginData) :
				Connect(host.externalEndpoint, incomingPassword, loginData);
				//TODO: Connect(new[] { host.externalEndpoint, host.internalEndpoint }, incomingPassword, loginData);
		}

		public NetworkConnectionError Connect(NetworkEndPoint server, string incomingPassword, params object[] loginData)
		{
			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, "Connect", Utility.EscapeURL(server.ToString()));
			return Connect(server, new Password(incomingPassword), loginData);
		}

		public NetworkConnectionError Connect(NetworkEndPoint server, Password passwordHash, params object[] loginData)
		{
			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, "Connect", Utility.EscapeURL(server.ToString()));

			NetworkConnectionError error = _ConnectTo(NetworkStartEvent.Client, server, passwordHash, false, loginData);

			if (error == NetworkConnectionError.NoError)
			{
				_peerType = NetworkPeerType.Client;
				_status = NetworkStatus.Connecting;
				_Start();
				return NetworkConnectionError.NoError;
			}

			return error;
		}

		public NetworkConnectionError InitializeCellServer(int maxClients, string[] pikkoServerHosts, int remotePort, string pikkoServerPassword)
		{
			return InitializeCellServer(maxClients, Utility.Resolve(pikkoServerHosts, remotePort), pikkoServerPassword);
		}

		public NetworkConnectionError InitializeCellServer(int maxClients, NetworkEndPoint[] pikkoServers, string pikkoServerPassword)
		{
			// TODO: impl maxClients

			return _PreConnectTo(NetworkPeerType.CellServer, pikkoServers, pikkoServerPassword);
		}

		public NetworkConnectionError InitializeCellServer(int maxClients, string pikkoServerHost, int remotePort, string pikkoServerPassword)
		{
			return InitializeCellServer(maxClients, Utility.Resolve(pikkoServerHost, remotePort), pikkoServerPassword);
		}

		public NetworkConnectionError InitializeCellServer(int maxClients, NetworkEndPoint pikkoServer, string pikkoServerPassword)
		{
			return InitializeCellServer(maxClients, pikkoServer, new Password(pikkoServerPassword));
		}

		public NetworkConnectionError InitializeCellServer(int maxClients, NetworkEndPoint pikkoServer, Password passwordHash, bool largeSequenceNumbers = false)
		{
			if (!isAuthoritativeServer)
			{
				throw new NetworkException("Cell-Server must be authoritative, please enable Network.isAuthoritativeServer.");
			}

			// TODO: impl maxClients

			NetworkConnectionError error = _ConnectTo(NetworkStartEvent.CellServer, pikkoServer, passwordHash, largeSequenceNumbers);

			if (error == NetworkConnectionError.NoError)
			{
				_peerType = NetworkPeerType.CellServer;
				_status = NetworkStatus.Connecting;
				_Start();
				return NetworkConnectionError.NoError;
			}

			return error;
		}

		public NetworkConnectionError _ClientStart(NetworkStartEvent nsEvent, bool largeSequenceNumbers, params object[] loginData)
		{
			if (_status != NetworkStatus.Disconnected)
			{
				_FailConnectionAttempt(NetworkConnectionError.AlreadyConnectedToAnotherServer);
				return NetworkConnectionError.AlreadyConnectedToAnotherServer;
			}

			_disconnectionType = NetworkDisconnection.Disconnected;

			_PreStart(nsEvent);

			if (NetworkGroup._flags.Count > 1)
			{
				Log.Warning(NetworkLogFlags.Group, "Group flags will be ignored on the client.");
			}

			var config = new NetConfiguration(Constants.CONFIG_NETWORK_IDENTIFIER);
			config.MaxConnections = 1;
			config.StartPort = 0;
			config.EndPort = 0;
			config.AnswerDiscoveryRequests = false;
			config.SendBufferSize = Constants.DEFAULT_SEND_BUFFER_SIZE;
			config.ReceiveBufferSize = Constants.DEFAULT_RECV_BUFFER_SIZE;

			_client = new NetClient(config);
			_ConfigureNetBase(_client);

			_client.SetMessageTypeEnabled(NetMessageType.PongReceived, true);

			try
			{
				_client.Start();
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.Client, "Client failed to start: ", e);
				_Cleanup();

				_FailConnectionAttempt(NetworkConnectionError.CreateSocketOrThreadFailure);
				return NetworkConnectionError.CreateSocketOrThreadFailure;
			}

			Log.Info(NetworkLogFlags.Client, "Client initialized on port ", _client.ListenPort);

			_loginData = loginData;

			lastError = NetworkConnectionError.NoError;
			return NetworkConnectionError.NoError;
		}

		public void _ClientConnect(NetworkEndPoint target, Password passwordHash)
		{
			Log.Debug(NetworkLogFlags.Client, "Connecting to ", target);

			_client.Connect(target, passwordHash.hash);
		}

		public NetworkConnectionError _ConnectTo(NetworkStartEvent nsEvent, NetworkEndPoint target, Password passwordHash, bool largeSequenceNumbers, params object[] loginData)
		{
			if (!_CheckTarget(target))
			{
				_FailConnectionAttempt(NetworkConnectionError.IncorrectParameters);
				return NetworkConnectionError.IncorrectParameters;
			}

			var error = _ClientStart(nsEvent, largeSequenceNumbers, loginData);

			if (error == NetworkConnectionError.NoError)
			{
				_ClientConnect(target, passwordHash);
			}

			return error;
		}

		public NetworkConnectionError _PreConnectTo(NetworkPeerType type, NetworkEndPoint[] targets, string incomingPassword, params object[] loginData)
		{
			if (!_CheckTargets(targets))
			{
				_FailConnectionAttempt(NetworkConnectionError.IncorrectParameters);
				return NetworkConnectionError.IncorrectParameters;
			}

			var error = _ClientStart(type == NetworkPeerType.Client ? NetworkStartEvent.Client : NetworkStartEvent.CellServer, false, loginData);

			if (error == NetworkConnectionError.NoError)
			{
				_preConnectTimeout = NetworkTime.localTime + Constants.DEFAULT_HANDSHAKE_TIMEOUT;

				foreach (var target in targets)
				{
					_preConnectTo[target] = incomingPassword;
					_UnconnectedRPC(new UnconnectedMessage(UnconnectedMessage.InternalCode.PreConnectRequest), target);
				}

				_peerType = type;
				_status = NetworkStatus.Connecting;
			}

			return error;
		}

		private NetworkConnectionError _ProxyStart(NetworkEndPoint target, string incomingPassword, params object[] loginData)
		{
			if (!_CheckTarget(target))
			{
				_FailConnectionAttempt(NetworkConnectionError.IncorrectParameters);
				return NetworkConnectionError.IncorrectParameters;
			}

			var error = _ClientStart(NetworkStartEvent.Client, false, loginData);

			if (error == NetworkConnectionError.NoError)
			{
				_proxyConnectTimeout = NetworkTime.localTime + Constants.DEFAULT_HANDSHAKE_TIMEOUT;

				_MasterSendProxyRequest(target, incomingPassword);

				_peerType = NetworkPeerType.Client;
				_status = NetworkStatus.Connecting;
			}

			return error;
		}

		public void _ProxyConnectTo(NetworkEndPoint session, Password sessionPassword)
		{
			if (Double.IsNaN(_proxyConnectTimeout)) return;
			_proxyConnectTimeout = Double.NaN;

			_ClientConnect(session, sessionPassword);
		}

		public void _ProxyFailed(NetworkConnectionError error)
		{
			_FailConnectionAttempt(error);
		}

		internal abstract void _MasterSendProxyRequest(NetworkEndPoint host, string password);
		internal abstract NetworkEndPoint _MasterGetMasterServerEndPoint();

		internal override NetworkSecurityStatus _ClientGetSecurityStatus(NetworkPlayer target)
		{
			return (_security != null) ? _security.status : NetworkSecurityStatus.Disabled;
		}

		internal override bool _ClientIsConnected(NetworkPlayer target)
		{
			return (target == NetworkPlayer.server && _status == NetworkStatus.Connected);
		}

		internal override NetConnection _ClientGetConnection(NetworkPlayer target)
		{
			return target == NetworkPlayer.server? _client.ServerConnection : null;
		}

		internal override NetworkPlayer _ClientFindConnectionPlayerID(NetConnection connection)
		{
			return connection.RemoteEndpoint.Equals(_client.ServerConnection.RemoteEndpoint) ? NetworkPlayer.server : NetworkPlayer.unassigned;
		}

		internal override NetworkEndPoint _ClientFindInternalEndPoint(NetworkPlayer target)
		{
			return (target != NetworkPlayer.unassigned && target == _localPlayer) ? Utility.Resolve(listenPort) : NetworkEndPoint.unassigned;
		}

		internal override NetworkEndPoint _ClientFindExternalEndPoint(NetworkPlayer target)
		{
			if (target == _localPlayer)
				return _masterGaveMyEndpoint;

			if (target == NetworkPlayer.server && _client != null && _client.ServerConnection != null)
				return _client.ServerConnection.RemoteEndpoint;

			return NetworkEndPoint.unassigned;
		}

		internal override NetworkPlayer[] _ClientGetPlayers()
		{
			return new[] { NetworkPlayer.server };
		}

		internal override int _ClientGetAveragePing(NetworkPlayer target)
		{
			return target == _localPlayer ? 0 : (target == NetworkPlayer.server && _client.ServerConnection != null ? (int)(_client.ServerConnection.AverageRoundtripTime * 1000) : -1);
		}

		internal override int _ClientGetLastPing(NetworkPlayer target)
		{
			return target == _localPlayer ? 0 : (target == NetworkPlayer.server && _client.ServerConnection != null ? (int)(_client.ServerConnection.LastRoundtripTime * 1000) : -1);
		}

		/// <summary>
		/// Gets the statistics object associated with the target player. For the client, only the server target is allowed.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		internal override NetworkStatistics _ClientGetStatistics(NetworkPlayer target)
		{
			if (target == NetworkPlayer.server && _client.ServerConnection != null)
			{
				return _clientConnStats;
			}
			return null;
		}

		private void _DoTrackPositions(double deltaTime)
		{
			if (_trackPositionRate <= 0)
			{
				_lastTrackSent = NetworkTime.localTime;
				_trackViews.Clear();
				return;
			}
			
			if (_trackViews.Count == 0) _trackViews.AddRange(_enabledViews.Values);

			var trackViews = _trackViews;

			const int maxSizeOfSinglePositionUpdate = 22; // max 20 bytes per position update (viewID (variable int 32, max 5 bytes) + coords (15 bytes)). Possible to find size without hardcoding this?
			int bitCountLimit = 8 * (config.maximumTransmissionUnit - 20 - maxSizeOfSinglePositionUpdate);
			int trackCountLimit = Math.Min(trackViews.Count, byte.MaxValue); //due to header

			int localIndex = _trackViewIndex;
			int maxUpdatesThisFrame = Math.Min(trackViews.Count, (int)(1 + deltaTime * trackViews.Count));

			for (int i = 0; i < maxUpdatesThisFrame; i++)
			{
				var nv = trackViews[localIndex];

				if (nv.IsNotDestroyed() && nv.enabled && nv.isCellAuthority)
				{
					var lastPos = nv._lastTrackPosition;
					var currentPos = nv.position;
					var delta = new Vector3(currentPos.x - lastPos.x, currentPos.y - lastPos.y, currentPos.z - lastPos.z);

					if (delta.x * delta.x + delta.y * delta.y + delta.z * delta.z > _maxSqrDeltaTrackPosition)
					{
						var msg = _trackMessage;
						if (msg == null)
						{
							msg = new NetworkMessage(this, NetworkFlags.TypeUnsafe | NetworkFlags.NoTimestamp | NetworkFlags.Unbuffered | NetworkFlags.Unreliable, NetworkMessage.Channel.RPC, NetworkMessage.InternalCode.MultiTrackPosition, NetworkPlayer.server);
							_trackMessageHeaderPos = msg.stream._bitCount;
							msg.stream.WriteByte((byte)0);
							_trackMessage = msg;
							_numberOfTracks = 0;
						}

						nv._lastTrackPosition = currentPos;
							
						msg.stream.WriteNetworkViewID(nv.viewID);
						msg.stream.WriteVector3(currentPos);
						_numberOfTracks++;

						if (msg.stream._bitCount >= bitCountLimit || _numberOfTracks >= trackCountLimit)
						{
							_SendTrackMessage();
						}
					}
				}

				localIndex += 1;
				if (localIndex >= trackViews.Count)
				{
					trackViews.Clear();
					trackViews.AddRange(_enabledViews.Values);
					localIndex = 0;

					if (trackViews.Count == 0) break;
				}
			}

			_trackViewIndex = localIndex;

			bool waitedTooLong = ((NetworkTime.localTime - _lastTrackSent) > (1.0f / _trackPositionRate));
			if (waitedTooLong) _SendTrackMessage();
		}

		private void _SendTrackMessage()
		{
			_lastTrackSent = NetworkTime.localTime;
			if (_trackMessage == null) return;
			
			//adjust header
			var msg = _trackMessage;
			var oldPos = msg.stream._bitCount;
			msg.stream._bitCount = _trackMessageHeaderPos;
			msg.stream.WriteByte((byte)_numberOfTracks);
			msg.stream._bitCount = oldPos;

			_trackMessage = null;
			_ClientSendRPC(msg);
		}

		private void _ClientUpdateConnectionStats(double localTime)
		{
			double timeSinceLastCalc = localTime - _statsLastCalculation;
			if (timeSinceLastCalc > _statsCalculationInterval)
			{
				if (_clientConnStats != null)
				{
					_clientConnStats._Update(timeSinceLastCalc);
					_statsLastCalculation = localTime;
				}
			}
		}

		internal override void _ClientUpdate(double deltaTime)
		{
			if (_client != null)
			{
				if (isMessageQueueRunning)
				{
					NetProfiler.BeginSample("_ClientCheckMessages");
					bool ok = _ClientCheckMessages();
					NetProfiler.EndSample();

					if (!ok) return;
				}

				if (_peerType == NetworkPeerType.CellServer && _status == NetworkStatus.Connected)
				{
					_DoTrackPositions(deltaTime);
				}

				double localTime = NetworkTime.localTime;

				_UpdateSmoothServerTime(localTime, deltaTime);

				_ClientUpdateConnectionStats(localTime);

				if (_status == NetworkStatus.Connecting)
				{
					if (!Double.IsNaN(_preConnectTimeout) && _preConnectTimeout <= localTime)
					{
						_FailConnectionAttempt(NetworkConnectionError.ConnectionTimeout);
						return;
					}

					if (!Double.IsNaN(_proxyConnectTimeout) && _proxyConnectTimeout <= localTime)
					{
						_FailConnectionAttempt(NetworkConnectionError.ConnectionTimeout);
						return;
					}

					if (!Double.IsNaN(_securityRequestTimeout) && _securityRequestTimeout <= localTime)
					{
						_FailConnectionAttempt(NetworkConnectionError.ServerAuthenticationTimeout);
						return;
					}
				}

				// Heartbeat must be called _exactly_ before resetting the outgoing buffer pool!
				// Otherwise a new outgoing message can overwrite the buffer of a unsent message.
				if (!NetUtility.SafeHeartbeat(_client))
				{
					_NetworkShutdown();
					return;
				}
			}
		}

		private bool _ClientCheckMessages()
		{
			NetBuffer buffer;
			NetMessageType type;
			NetConnection connection;
			NetworkEndPoint endpoint;
			NetChannel channel;
			double localTimeRecv;

			// TODO: optimize by adding buffer pool
			while (isMessageQueueRunning && _client != null && _client.ReadMessage(buffer = _client.CreateBuffer(), out type, out connection, out endpoint, out channel, out localTimeRecv))
			{
				switch (type)
				{
					case NetMessageType.Data:
						_OnLidgrenMessage(buffer, channel, localTimeRecv);
						break;

					case NetMessageType.StatusChanged:
						{
							var reason = buffer.ReadString();
							var newLidgrenStatus = (NetConnectionStatus)buffer.ReadByte();
							if (!_OnLidgrenStatusChanged(newLidgrenStatus, reason)) return false;
						}
						break;

					case NetMessageType.ConnectionRejected:
						_OnLidgrenConnectionRejected(buffer.ReadString());
						break;

					case NetMessageType.OutOfBandData:
						_HandleUnconnectedMessage(buffer, endpoint);
						break;

					case NetMessageType.PongReceived:
						if (connection != null && connection == _client.ServerConnection)
						{
							_ResynchronizeServerTime(connection);
						}
						break;

					case NetMessageType.DebugMessage:
					case NetMessageType.VerboseDebugMessage:
						Log.Debug(NetworkLogFlags.BadMessage, "Client debug message: ", buffer.ReadString(), ", endpoint: ", endpoint); // TODO: when is this called
						break;

					case NetMessageType.BadMessageReceived:
						Log.Warning(NetworkLogFlags.BadMessage, "Client received bad message: ", buffer.ReadString(), ", endpoint: ", endpoint);
						break;
				}

				if (_client == null)
				{
					Log.Debug(NetworkLogFlags.Client, "Client was suddenly disconnected probably due to DisconnectImmediate was called");
					return false;
				}
			}

			return true;
		}

		internal override void _ClientDisconnect(float timeout)
		{
			if (_client != null && _client.ServerConnection != null)
			{
				Log.Info(NetworkLogFlags.Client, "Client disconnecting");
				_client.ServerConnection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, timeout, true, true);
				_clientConnStats = null;
			}
		}

		internal override void _ClientCleanup()
		{
			_loginData = new object[0];
			_viewIDAllocator.Clear();
			_allocatedViewIDs.Clear();
			_security = null;
			_preConnectTimeout = Double.NaN;
			_preConnectTo.Clear();
			_proxyConnectTimeout = Double.NaN;
			_securityRequestTimeout = Double.NaN;
			_clientConnStats = null;

			if (_client != null)
			{
				Log.Info(NetworkLogFlags.Client, "Client cleanup...");
				_client.Dispose();
				_client = null;
			}
		}

		private void _OnConnectedToServer()
		{
			_clientConnStats = new NetworkStatistics(_client.ServerConnection);
			_Notify("OnConnectedToServer", _client.ServerConnection.RemoteEndpoint);
		}

		private void _OnConnectedToPikkoServer(bool isFirstCellServer)
		{
			_clientConnStats = new NetworkStatistics(_client.ServerConnection);
			_Notify("OnConnectedToPikkoServer", isFirstCellServer);
		}

		private void _OnLidgrenConnectionRejected(string reason)
		{
			Log.Info(NetworkLogFlags.Client, "Client connection was rejected by server because: ", reason);

			switch (reason)
			{
				case Constants.REASON_TOO_MANY_PLAYERS:
					_FailConnectionAttempt(NetworkConnectionError.TooManyConnectedPlayers);
					break;

				case Constants.REASON_INVALID_PASSWORD:
					_FailConnectionAttempt(NetworkConnectionError.InvalidPassword);
					break;

				case Constants.REASON_CONNECTION_BANNED: // TODO: this need so be implemented in UnityLink
					_FailConnectionAttempt(NetworkConnectionError.ConnectionBanned);
					break;

				case Constants.REASON_LIMITED_PLAYERS:
					_FailConnectionAttempt(NetworkConnectionError.LimitedPlayers);
					break;

				case Constants.REASON_BAD_APP_ID:
					_FailConnectionAttempt(NetworkConnectionError.IncompatibleVersions);
					break;
					

				default:
					_FailConnectionAttempt(NetworkConnectionError.ConnectionFailed);
					break;
			}
		}

		private bool _OnLidgrenStatusChanged(NetConnectionStatus newLidgrenStatus, string reason)
		{
			Log.Debug(NetworkLogFlags.Client, "Client internal status changed to ", newLidgrenStatus, " because ", reason);

			if (newLidgrenStatus == NetConnectionStatus.Connected)
			{
				_OnLidgrenConnectedToServer();
			}
			else if (newLidgrenStatus == NetConnectionStatus.Disconnected)
			{
				Log.Info(NetworkLogFlags.Client, "Client was disconnected because ", reason);

				switch (_status)
				{
					case NetworkStatus.Connected:
						_OnLidgrenDisconnectedFromServer(reason);
						break;
					case NetworkStatus.Connecting:
						if (!_fallbackConnect.server.isUnassigned)
						{
							Log.Info(NetworkLogFlags.Client, "About to attempt fallback connect to ", _fallbackConnect.server);
							_Cleanup();

							var error = _fallbackConnect.useProxy?
								_ProxyStart(_fallbackConnect.server, _fallbackConnect.incomingPassword, _fallbackConnect.loginData) :
								Connect(_fallbackConnect.server, _fallbackConnect.incomingPassword, _fallbackConnect.loginData);

							_fallbackConnect.server = NetworkEndPoint.unassigned;
							if (error == NetworkConnectionError.NoError) break;
						}

						// This state transition happens when all lidgren connection attempts have failed
						_FailConnectionAttempt(NetworkConnectionError.ConnectionFailed); // TODO: maybe use timeout error instead?
						break;
				}

				return false;
			}
			else if (newLidgrenStatus == NetConnectionStatus.Disconnecting)
			{
				Log.Info(NetworkLogFlags.Client, "Client is disconnecting because ", reason);

				_OnLidgrenDisconnectingFromServer(reason);
			}
			
			return true;
		}

		private void _OnLidgrenConnectedToServer()
		{
			Log.Debug(NetworkLogFlags.Client, "Client is now internally connected to server");

			if (requireSecurityForConnecting || publicKey != null)
			{
				_securityRequestTimeout = NetworkTime.localTime + _client.Configuration.HandshakeAttemptsMaxCount * _client.Configuration.HandshakeAttemptRepeatDelay;
			}
			else
			{
				_SendConnectRequest();
			}
		}

		private void _OnLidgrenDisconnectingFromServer(string reason)
		{
			if (reason != Constants.REASON_NORMAL_DISCONNECT)
				_disconnectionType = NetworkDisconnection.LostConnection;

			_status = NetworkStatus.Disconnecting;
		}

		private void _OnLidgrenDisconnectedFromServer(string reason)
		{
			if (reason != Constants.REASON_NORMAL_DISCONNECT)
				_disconnectionType = NetworkDisconnection.LostConnection;

			DisconnectImmediate();
		}

		private void _OnLidgrenMessage(NetBuffer buffer, NetChannel channel, double localTimeRecv)
		{
			bool isEncrypted = SecurityLayer.IsEncrypted(buffer);

			if (isEncrypted)
			{
				if (_security == null || !_security.ClientDecrypt(ref buffer))
				{
					Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Client dropping encrypted incoming message from server because decryption failed");
					return;
				}
			}

			NetworkMessage msg;

			try
			{
				msg = new NetworkMessage(this, buffer, _client.ServerConnection, channel, isEncrypted, localTimeRecv);
			}
			catch (Exception e)
			{
				Log.Debug(NetworkLogFlags.BadMessage, "Client failed to parse a incoming message on channel ", channel, ": ", e.Message);
				return;
			}

			Log.Debug(NetworkLogFlags.RPC, "Client received ", msg);

			Log.Debug(NetworkLogFlags.Timestamp, "Client got message ", msg.name, " with local timestamp ", msg.localTimeSent, " s");

			// TODO: sanity check rpc data

			_ExecuteRPC(msg);
		}

		internal void _RemoveRPCsInPikko(NetworkPlayer playerFilter, NetworkViewID viewFilter, string nameFilter)
		{
			if (playerFilter == NetworkPlayer.cellProxies) throw new System.ArgumentException("NetworkPlayer.cellProxies cannot own any objects");

			var rpc = new NetworkMessage(this, NetworkFlags.TypeUnsafe | NetworkFlags.Unbuffered | NetworkFlags.NoTimestamp, NetworkMessage.Channel.RPC,
				null, NetworkMessage.InternalCode.RemoveRPCs, playerFilter, NetworkPlayer.unassigned, viewFilter);
			
			rpc.stream.WriteString(nameFilter);

			_ClientSendRPC(rpc);
		}

		private void _SendConnectRequest()
		{
			// TODO: resend if no ConnectResponse within a certain timeout

			NetworkMessage msg;

			if (_peerType == NetworkPeerType.CellServer)
			{
				msg = new NetworkMessage(this, NetworkMessage.InternalCode.CellConnectRequest, NetworkPlayer.server);
			}
			else
			{
				msg = new NetworkMessage(this, NetworkMessage.InternalCode.ClientConnectRequest, NetworkPlayer.server);
				ParameterWriter.WriteUnprepared(msg.stream, _loginData);
			}

			_ClientSendRPC(msg);

			// to send the RPC as quickly as possible.
			if (!NetUtility.SafeHeartbeat(_client))
			{
				_NetworkShutdown();
				return;
			}
		}

		internal override NetBase _ClientGetNetBase()
		{
			return _client;
		}

		internal override void _ClientSendRPC(NetworkMessage rpc)
		{
			Log.Debug(NetworkLogFlags.RPC, "Client sending ", rpc);

			_ClientSend(rpc);
		}

		internal override void _ClientSendStateSync(NetworkMessage state)
		{
			_ClientSend(state);
		}

		private void _ClientSend(NetworkMessage msg)
		{
			if (msg == null | _client == null) return;

			NetBuffer buffer = msg.GetSendBuffer();

			if (msg.isEncryptable)
			{
				if (_security != null && !_security.ClientEncrypt(ref buffer))
				{
					Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Client dropping outgoing message '", msg.name, "' because encryption failed");
					return;
				}
			}

			try
			{
				_client.SendMessage(buffer, msg.channel);
			}
			catch (NetException e)
			{
				if (e.Message == "You must be connected first!")
				{
					Log.Debug(NetworkLogFlags.BadMessage, "Client failed to send message because client is no longer connected");
					return;
				}
				
				throw;
			}

			if (!batchSendAtEndOfFrame)
			{
				var serverConnection = _client.ServerConnection;
				if (serverConnection != null)
				{
					Log.Debug(NetworkLogFlags.RPC, "Force send message directly, instead of at end of frame");
					serverConnection.SendUnsentMessages(NetTime.Now);
				}
				else
				{
					Log.Debug(NetworkLogFlags.RPC, "Failed to force send message directly, because server connection is missing");
				}
			}
		}

		internal void _SetupPlayer(NetworkPlayer myPlayer)
		{
			_localPlayer = myPlayer;
			Log.Debug(NetworkLogFlags.Client | NetworkLogFlags.Server, "Local player assigned as ", myPlayer); // TODO: if server then "Client assigned as Server" which is not so pretty...
		}

		internal void _RPCConnectDenied(int errorCode, NetworkMessage msg)
		{
			_AssertSenderIsServer(msg);

			var error = (NetworkConnectionError) errorCode;
			Log.Debug(NetworkLogFlags.Client, "Client connection was denied by server with error: ", error);

			_FailConnectionAttempt(error);
		}

		internal void _RPCClientConnectResponse(NetworkPlayer newPlayer, BitStream stream, NetworkMessage msg)
		{
			_AssertIsClientConnecting();
			_AssertSenderIsServer(msg);

			if ((requireSecurityForConnecting || publicKey != null) && (_security == null || (msg.flags & NetworkFlags.Unencrypted) == NetworkFlags.Unencrypted))
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Client dropping server connect response because required security layer is missing");
				return;
			}

			if (newPlayer != NetworkPlayer.unassigned)
			{
				_AssignConnectionPlayerID(msg.connection, NetworkPlayer.server);

				_SynchronizeInitialServerTime(_client.ServerConnection);
				_SetupPlayer(newPlayer);

				_approvalData = stream.GetRemainingBitStream();

				Log.Info(NetworkLogFlags.Client, "Client connection to server is complete");

				_status = NetworkStatus.Connected;
				_OnConnectedToServer();

				if (_security != null)
				{
					_Notify("OnSecurityInitialized", _localPlayer);
				}
			}
			else
			{
				Log.Error(NetworkLogFlags.Client, "Client can't connect because of too many players");
				_FailConnectionAttempt(NetworkConnectionError.TooManyConnectedPlayers);
			}
		}

		internal void _RPCCellConnectResponse(NetworkPlayer newPlayer, bool isFirstCellServer, NetworkMessage msg)
		{
			_AssertIsCellServerConnecting();
			_AssertSenderIsServer(msg);

			if ((requireSecurityForConnecting || publicKey != null) && (_security == null || (msg.flags & NetworkFlags.Unencrypted) == NetworkFlags.Unencrypted))
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "CellServer dropped server connect response because required security layer is missing");
				return;
			}

			_AssignConnectionPlayerID(msg.connection, NetworkPlayer.server);

			_SynchronizeInitialServerTime(_client.ServerConnection);
			_SetupPlayer(newPlayer);

			Log.Info(NetworkLogFlags.CellServer, "CellServer connection to PikkoServer is complete");

			_status = NetworkStatus.Connected;
			_OnConnectedToPikkoServer(isFirstCellServer);
		}

		internal void _RPCRedirect(NetworkEndPoint redirectTo, Password passwordHash, NetworkMessage msg)
		{
			_AssertIsClientOrCellServerConnectedOrConnecting();
			_AssertSenderIsServerOrCellServer(msg);

			if (redirectTo.isAny)
			{
				Log.Debug(NetworkLogFlags.Client, "Client redirected to the same IP ", msg.connection.RemoteEndpoint.ipAddress, " as the current server but with remote port ", redirectTo.port);

				redirectTo = new NetworkEndPoint(msg.connection.RemoteEndpoint.ipAddress, redirectTo.port);
			}
			else
			{
				Log.Debug(NetworkLogFlags.Client, "Client redirected to ", redirectTo);
			}

			// TODO: can the application fuck things up in OnDisconnectedFromServer or OnRedirectingToServer?

			NetworkPeerType oldType = _peerType;
			var oldData = _loginData;

			_disconnectionType = NetworkDisconnection.Redirecting;
			DisconnectImmediate();

			_Notify("OnRedirectingToServer", redirectTo);

			// TODO: reuse maxAllowedClients and pass it to InitializeCellServer

			NetworkConnectionError error = (oldType == NetworkPeerType.CellServer) ?
				InitializeCellServer(0, redirectTo, passwordHash, true) : Connect(redirectTo, passwordHash, oldData);

			if (error != NetworkConnectionError.NoError)
			{
				Log.Debug(NetworkLogFlags.Client, "Client failed to connect to redirected server: ", error);
			}
		}

		internal void _RPCBufferedRPCs(SerializedBuffer[] buffers, NetworkMessage msg)
		{
			_AssertIsClientConnected();
			_AssertSenderIsServer(msg);

			Log.Debug(NetworkLogFlags.RPC | NetworkLogFlags.Buffered, "Client has received all ", buffers.Length, " buffered message(s)");

			var bufferedRPCs = new NetworkBufferedRPC[buffers.Length];

			for (int i = 0; i < bufferedRPCs.Length; i++)
			{
				var bufferedMsg = new NetworkMessage(this, buffers[i].buffer, msg.connection, msg.channel, (msg.flags & NetworkFlags.Unencrypted) == 0);

				bufferedRPCs[i] = new NetworkBufferedRPC(bufferedMsg);
			}

			_Notify("OnPreBufferedRPCs", bufferedRPCs);

			foreach (var rpc in bufferedRPCs)
			{
				if (rpc._autoExecute) _ExecuteRPC(rpc._msg);
			}
		}

		protected override NetworkViewID _AllocateViewID(NetworkPlayer owner, NetworkGroup group)
		{
			var viewID = _viewIDAllocator.Allocate(_localPlayer);

			// TODO: fulhacks, this might not be problem free to do.
			if (owner.isCellServer || owner == NetworkPlayer.cellProxies) owner = NetworkPlayer.server;

			_allocatedViewIDs.Add(viewID, new KeyValuePair<NetworkPlayer, NetworkGroup>(owner, group));

			return viewID;
		}

		protected override NetworkViewID[] _AllocateViewIDs(int count, NetworkPlayer owner, NetworkGroup group)
		{
			var viewIDs = _viewIDAllocator.Allocate(_localPlayer, count);

			var pair = new KeyValuePair<NetworkPlayer, NetworkGroup>(owner, group);

			foreach (var viewID in viewIDs)
			{
				_allocatedViewIDs.Add(viewID, pair);
			}

			return viewIDs;
		}

		public bool DeallocateViewID(NetworkViewID viewID)
		{
			var nv = _FindNetworkView(viewID);
			if (nv.IsNotNull())
			{
				Log.Error(NetworkLogFlags.NetworkView, "Can't deallocate ", viewID, " because it is used by ", nv);
				Log.Error(NetworkLogFlags.NetworkView, "To properly deallocate it use NetworkView.DeallocateViewID() instead");
				return false;
			}

			return _DeallocateViewID(viewID);
		}

		public bool DeallocateViewIDs(NetworkViewID[] viewIDs)
		{
			foreach (var viewID in viewIDs)
			{
				var nv = _FindNetworkView(viewID);
				if (nv.IsNotNull())
				{
					Log.Error(NetworkLogFlags.NetworkView, "Can't deallocate ", viewID, " because it is used by ", nv);
					Log.Error(NetworkLogFlags.NetworkView, "To properly deallocate it use NetworkView.DeallocateViewID() instead");
					return false;
				}
			}

			return _DeallocateViewIDs(viewIDs);
		}

		public bool DeallocateViewIDs(NetworkPlayer owner)
		{
			foreach (var pair in _allocatedViewIDs)
			{
				if (pair.Value.Key == owner)
				{
					var nv = _FindNetworkView(pair.Key);
					if (nv.IsNotNull())
					{
						Log.Error(NetworkLogFlags.NetworkView, "Can't deallocate ", pair.Key, " because it is used by ", nv);
						Log.Error(NetworkLogFlags.NetworkView, "To properly deallocate it use NetworkView.DeallocateViewID() instead");
						return false;
					}
				}
			}

			return _DeallocateViewIDs(owner);
		}

		internal bool _DeallocateViewID(NetworkViewID viewID)
		{
			if (!_allocatedViewIDs.ContainsKey(viewID))
			{
				Log.Debug(NetworkLogFlags.NetworkView, "Can't deallocate ", viewID, " because it is already deallocated or was never allocated here");
				return false;
			}

			_allocatedViewIDs.Remove(viewID);
			_viewIDAllocator.Deallocate(viewID, NetworkTime.localTime + _recyclingDelayForViewID);

			return true;
		}

		private bool _DeallocateViewIDs(NetworkViewID[] viewIDs)
		{
			if (_allocatedViewIDs.Count == 0) return false;

			foreach (var viewID in viewIDs)
			{
				if (!_allocatedViewIDs.ContainsKey(viewID))
				{
					Log.Debug(NetworkLogFlags.NetworkView, "Can't deallocate ", viewID, " because it is already deallocated or was never allocated here");
					return false;
				}
			}

			double timeToRecycle = NetworkTime.localTime + _recyclingDelayForViewID;

			foreach (var viewID in viewIDs)
			{
				_allocatedViewIDs.Remove(viewID);
				_viewIDAllocator.Deallocate(viewID, timeToRecycle);
			}

			return true;
		}

		private bool _UnsafeDeallocateViewIDs(List<NetworkViewID> viewIDs)
		{
			if (viewIDs.Count == 0) return false;

			double timeToRecycle = NetworkTime.localTime + _recyclingDelayForViewID;

			for (int i = 0; i < viewIDs.Count; i++)
			{
				var viewID = viewIDs[i];

				_allocatedViewIDs.Remove(viewID);
				_viewIDAllocator.Deallocate(viewID, timeToRecycle);
			}

			return true;
		}

		private bool _DeallocateViewIDs(NetworkPlayer owner)
		{
			var buffer = new List<NetworkViewID>();

			foreach (var pair in _allocatedViewIDs)
			{
				if (pair.Value.Key == owner)
				{
					buffer.Add(pair.Key);
				}
			}

			return _UnsafeDeallocateViewIDs(buffer);
		}

		private bool _DeallocateViewIDs(NetworkGroup group)
		{
			var buffer = new List<NetworkViewID>();

			foreach (var pair in _allocatedViewIDs)
			{
				if (pair.Value.Value == group)
				{
					buffer.Add(pair.Key);
				}
			}

			return _UnsafeDeallocateViewIDs(buffer);
		}

		private bool _DeallocateViewIDs(NetworkPlayer owner, NetworkGroup group)
		{
			var buffer = new List<NetworkViewID>();

			foreach (var pair in _allocatedViewIDs)
			{
				if (pair.Value.Key == owner && pair.Value.Value == group)
				{
					buffer.Add(pair.Key);
				}
			}

			return _UnsafeDeallocateViewIDs(buffer);
		}

		private void _DeallocateAllViewIDs()
		{
			double timeToRecycle = NetworkTime.localTime + _recyclingDelayForViewID;

			foreach (var pair in _allocatedViewIDs)
			{
				_viewIDAllocator.Deallocate(pair.Key, timeToRecycle);
			}

			_allocatedViewIDs.Clear();
		}

		internal void _RPCSecurityRequest(PublicKey pubKey, NetworkMessage msg)
		{
			_AssertIsClientOrCellServerConnectedOrConnecting();
			_AssertSenderIsServer(msg);

			// TODO: assert that is message was encrypted if Network.publicKey != null

			if (_security != null)
			{
				// TODO: log error
				return;
			}

			if (publicKey != null && !publicKey.Equals(pubKey))
			{
				_FailConnectionAttempt(NetworkConnectionError.RSAPublicKeyMismatch);
				return;
			}

			_securityRequestTimeout = Double.NaN;

			Log.Debug(NetworkLogFlags.Security, "Client adding security layer because server requested it");
			_security = new SecurityLayer(pubKey);

			SymmetricKey symKey = SymmetricKey.Generate(symmetricKeySize);
			
			var response = new NetworkMessage(this, NetworkMessage.InternalCode.SecurityResponse, NetworkPlayer.server);
			response.stream.WriteSymmetricKey(symKey);
			_ClientSendRPC(response);

			Log.Debug(NetworkLogFlags.Security, "Client enabling security layer");
			_security.Enable(symKey);

			if (_status == NetworkStatus.Connecting)
			{
				_SendConnectRequest();
			}
			else
			{
				_Notify("OnSecurityInitialized", _localPlayer);
			}
		}

		internal void _RPCUnsecurityRequest(SymmetricKey symKey, NetworkMessage msg)
		{
			_AssertIsClientOrCellServerConnectedOrConnecting();
			_AssertSenderIsServer(msg);

			if (!msg.isEncryptable)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Client dropping unsecurity request because it was not encrypted");
				return;
			}

			// TODO: assert that is message was encrypted if Network.publicKey != null

			if (_security == null)
			{
				// TODO: log error
				return;
			}

			if (!_security.GetSymmetricKey().Equals(symKey))
			{
				// TODO: log error
				return;
			}

			var response = new NetworkMessage(this, NetworkMessage.InternalCode.UnsecurityResponse, NetworkPlayer.server);
			_ClientSendRPC(response);

			Log.Debug(NetworkLogFlags.Security, "Client disabling and removing security layer because server requested it");
			_security = null;

			if (_status != NetworkStatus.Connecting)
			{
				_Notify("OnSecurityUninitialized", _localPlayer);
			}
		}

		internal NetworkViewBase _RPCCreate(NetworkPlayer owner, NetworkGroup group, NetworkAuthFlags authFlags, Vector3 pos, Quaternion rot, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, BitStream stream, NetworkMessage msg)
		{
			if (isAuthoritativeServer && !msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing creation of ", msg.viewID, " by non-authoritative ", msg.sender);
				return null;
			}

			BitStream remainder = stream.GetRemainingBitStream();
			return _Create(authFlags, true, pos, rot, msg.viewID, owner, group, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, remainder, msg);
		}

		internal void _RPCHandoverRequest(NetworkViewID viewID, NetworkMessage msg)
		{
			Log.Debug(NetworkLogFlags.Handover, "CellServer received handover requested for ", viewID);

			_AssertIsCellServerConnected();

			var nv = _FindNetworkView(viewID);
			if (nv.IsNull())
			{
				Log.Error(NetworkLogFlags.Handover, "Got handover request, but ", viewID, " is not instantiated here");
				return;
			}

			if (!nv.isCellAuthority)
			{
				Log.Error(NetworkLogFlags.Handover, "Got handover request, but ", nv, " is not the CellAuthority");
				return;
			}

			var handover = new NetworkMessage(this, NetworkFlags.TypeUnsafe | NetworkFlags.Unbuffered, NetworkMessage.Channel.RPC, NetworkMessage.InternalCode.HandoverResponse, msg.sender);
			handover.stream.WriteNetworkViewID(viewID);
			handover.stream.WriteNetworkPlayer(nv.owner);
			handover.stream.WriteNetworkGroup(nv.group);
			handover.stream.WriteByte((byte)nv._data.authFlags); // TODO: ugly hack to void authority permission check
			handover.stream.WriteVector3(nv.position);
			handover.stream.WriteQuaternion(nv.rotation);
			handover.stream.WriteString(nv.proxyPrefab);
			handover.stream.WriteString(nv.ownerPrefab);
			handover.stream.WriteString(nv.serverPrefab);
			handover.stream.WriteString(nv.cellAuthPrefab);
			handover.stream.WriteString(nv.cellProxyPrefab);
			handover.stream.WriteBytes(nv.initialData._ToArray());

			var msgInfo = new NetworkMessageInfo(handover, nv);

			int serializedStateIndex = handover.stream._buffer.m_bitLength;
			if (nv._hasSerializeHandover)
			{
				Log.Debug(NetworkLogFlags.Handover | NetworkLogFlags.StateSync, "Serializing handover state with ", nv.viewID);

				nv._SerializeHandover(handover.stream, msgInfo);
			}

			if (!isCellServer || _status != NetworkStatus.Connected)
			{
				Log.Error(NetworkLogFlags.Handover, "CellServer tried to send handover response but could not due to status being ", _status);
				return;
			}

			// You might think there is a race condition here:
			// that because the new CellAuth *might* (although unlikely) be instantiated before the new CellProxy,
			// the CellAuth could send an RPC or StateSync to it's CellProxies and this new CellProxy (which doesn't exist yet)
			// might miss it because it's instantiation might have taken too long.
			// But you're forgetting that the CellServer is single-threaded and can't execute such RPCs or StateSync before
			// it has reached end of the frame and uLink's internal Update.
			Log.Debug(NetworkLogFlags.Handover, "CellServer sending handover response");
			_ClientSendRPC(handover);

			var position = nv.position;
			var rotation = nv.rotation;
			var owner = nv.owner;
			var group = nv.group;
			var flags = nv._data.authFlags; // TODO: ugly hack to void authority permission check
			var proxyPrefab = nv.proxyPrefab;
			var ownerPrefab = nv.ownerPrefab;
			var serverPrefab = nv.serverPrefab;
			var cellAuthPrefab = nv.cellAuthPrefab;
			var cellProxyPrefab = nv.cellProxyPrefab;
			var initialData = nv.initialData;
			initialData._buffer.m_readPosition = 0; // reset the bitstream

			_DestroyNetworkView(nv);

			var cellProxy = _Create(flags, true, position, rotation, viewID, owner, group, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, initialData, msg);
			if (cellProxy != null)
			{
				//cellProxy._lastCellProxyTimestamp = msgInfo.timestampInMillis;

				if (cellProxy._hasSerializeHandover)
				{
					handover.stream._isWriting = false;
					handover.stream._buffer.m_readPosition = serializedStateIndex;

					Log.Debug(NetworkLogFlags.Handover | NetworkLogFlags.StateSync, "Deserializing handover state for cell auth -> cell proxy with ", cellProxy.viewID);

					cellProxy._SerializeHandover(handover.stream, msgInfo);
				}
			}
		}

		internal void _RPCRepairAuthFromProxyRequest(NetworkViewID viewID, NetworkMessage msg)
		{
			Log.Debug(NetworkLogFlags.Handover, "CellServer received repair cell auth from proxy requested for ", viewID);

			_AssertIsCellServerConnected();

			var nv = _FindNetworkView(viewID);
			if (nv.IsNull())
			{
				Log.Error(NetworkLogFlags.Handover, "Got repair cell auth from proxy request, but ", viewID, " is not instantiated here");
				return;
			}

			var msgInfo = new NetworkMessageInfo(this, NetworkFlags.Normal, nv);

			//TODO: rather than relying on cell proxy serialization callback, we should introduce a new one for repairs...
			//the signature with NetworkMessageInfo is strange, 
			BitStream cellProxyState;
			if (nv._hasSerializeCellProxy)
			{
				cellProxyState = new BitStream(true, false);

				Log.Debug(NetworkLogFlags.Handover | NetworkLogFlags.StateSync, "Serializing CellProxy state for repair with ", nv.viewID);

				if (!nv._SerializeCellProxy(cellProxyState, msgInfo))
				{
					cellProxyState = null;
				}
			}
			else
			{
				cellProxyState = null;
			}

			var position = nv.position;
			var rotation = nv.rotation;
			var owner = nv.owner;
			var group = nv.group;
			var flags = nv._data.authFlags; // TODO: ugly hack to void authority permission check
			var proxyPrefab = nv.proxyPrefab;
			var ownerPrefab = nv.ownerPrefab;
			var serverPrefab = nv.serverPrefab;
			var cellAuthPrefab = nv.cellAuthPrefab;
			var cellProxyPrefab = nv.cellProxyPrefab;
			var initialData = nv.initialData;
			initialData._buffer.m_readPosition = 0; // reset the bitstream

			_DestroyNetworkView(nv);

			var cellAuth = _Create(flags, false, position, rotation, viewID, owner, group, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, initialData, msg);
			if (cellAuth != null)
			{
				cellAuth._lastCellProxyTimestamp = msgInfo.timestampInMillis;

				if (cellProxyState != null)
				{
					cellProxyState._isWriting = false;
					if (cellAuth._hasSerializeCellProxy) // we have to ask otherwise it wont update the observed binding cache.
					{
						cellAuth._SerializeCellProxy(cellProxyState, msgInfo);
					}
				}
			}
		}

		internal void _RPCMastDebugInfo(Vector3 mastPosition, NetworkMessage msg)
		{
			this._cellPosition = mastPosition;
		}

		internal void _RPCHandoverResponse(NetworkViewID viewID, NetworkPlayer owner, NetworkGroup group, NetworkAuthFlags authFlags, Vector3 pos, Quaternion rot, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, byte[] initialBytes, BitStream stream, NetworkMessage msg)
		{
			Log.Debug(NetworkLogFlags.Handover, "CellServer received handover response for ", viewID);

			_AssertIsCellServerConnected();

			var initialData = new BitStream(initialBytes, false);
			NetworkViewBase nv = _Create(authFlags, false, pos, rot, viewID, owner, group, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, initialData, msg);

			if (nv._hasSerializeHandover)
			{
				Log.Debug(NetworkLogFlags.Handover, "Deserializing handover state for cell auth -> cell auth with ", viewID);

				nv._SerializeHandover(stream, new NetworkMessageInfo(msg, nv));
			}
		}

		internal void _RPCChangeGroup(NetworkViewID viewID, NetworkGroup newGroup, NetworkMessage msg)
		{
			var nv = _FindNetworkView(viewID);
			if (nv.IsNull())
			{
				// log error
				return;
			}

			Log.Info(NetworkLogFlags.Group, "Changing ", viewID, "'s group from ", nv.group, " to ", newGroup);

			nv.SetViewID(viewID, nv.owner, newGroup);
		}

		public bool Destroy(NetworkViewBase nv)
		{
			var viewID = nv.viewID;

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyByViewID, viewID);
			_SendRPC(msg);

			if (isAuthoritativeServer && !isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing destruction of ", viewID, " by non-authoritative ");
				return false;
			}

			Log.Debug(NetworkLogFlags.NetworkView, "Destroying GameObject with ", viewID);

#if TEST_BUILD
			_Notify("OnGameObjectDestroyed", viewID);
#endif

			_PreDestroyBy(viewID);

			_DestroyNetworkView(nv.root);
			return true;
		}

		public bool Destroy(NetworkViewID viewID)
		{
			_AssertViewID(viewID);
			_AssertIsAuthoritative();

			// TODO: sanity check viewID
			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyByViewID, viewID);
			_SendRPC(msg);

			return _RPCDestroyByViewID(msg);
		}

		public void DestroyInGroup(NetworkGroup group)
		{
			_AssertGroup(group);
			_AssertIsAuthoritative();

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyInGroup, NetworkPlayer.unassigned);
			msg.stream.WriteNetworkGroup(group);
			_SendRPC(msg);

			_RPCDestroyInGroup(group, new NetworkMessage(this));
		}

		public void DestroyPlayerObjectsInGroup(NetworkGroup group, NetworkPlayer target)
		{
			_AssertGroup(group);
			_AssertPlayer(target);
			_AssertIsAuthoritative();

			// TODO: sanity check player

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyInGroupByPlayerID, NetworkPlayer.unassigned);
			msg.stream.WriteNetworkPlayer(target);
			msg.stream.WriteNetworkGroup(group);
			_SendRPC(msg);

			_RPCDestroyInGroupByPlayerID(target, group, new NetworkMessage(this));
		}

		public void DestroyPlayerObjects(NetworkPlayer target)
		{
			_AssertIsAuthoritative();

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyByPlayerID, NetworkPlayer.unassigned);
			msg.stream.WriteNetworkPlayer(target);
			_SendRPC(msg);

			_RPCDestroyByPlayerID(target, new NetworkMessage(this));
		}

		public void DestroyAll(bool includeManual)
		{
			_AssertIsAuthoritative();

			if (status != NetworkStatus.Disconnected)
			{
				var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyAll, NetworkPlayer.unassigned);
				msg.stream.WriteBoolean(includeManual);
				_SendRPC(msg);
			}

			_RPCDestroyAll(includeManual, new NetworkMessage(this));
		}

		internal bool _RPCDestroyByViewID(NetworkMessage msg)
		{
			var viewID = msg.viewID;

			if (isAuthoritativeServer && !msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing destruction of ", viewID, " by non-authoritative ", msg.sender);
				return false;
			}

			Log.Debug(NetworkLogFlags.NetworkView, "Destroying GameObject with ", viewID);

#if TEST_BUILD
			_Notify("OnGameObjectDestroyed", viewID);
#endif

			_PreDestroyBy(viewID);

			var nv = _FindNetworkView(viewID);
			if (nv.IsNull()) return false;
			
			_DestroyNetworkView(nv);
			return true;
		}

		internal void _RPCDestroyByPlayerID(NetworkPlayer target, NetworkMessage msg)
		{
			if (isAuthoritativeServer && !msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing destruction of ", target, "'s GameObject(s) by non-authoritative ", msg.sender);
				return;
			}

			List<NetworkViewBase> views;
			if (!_userViews.TryGetValue(target, out views)) return;
			var buffer = views.ToArray();

			Log.Debug(NetworkLogFlags.NetworkView, "Destroying ", buffer.Length, " GameObject(s) belonging to ", target);

#if TEST_BUILD
			_Notify("OnPlayerDestroyed", target);
#endif

			_PreDestroyBy(target);

			foreach (var nv in buffer)
			{
				_DestroyNetworkView(nv);
			}

			_userViews.Remove(target);
		}

		internal void _RPCDestroyInGroup(NetworkGroup group, NetworkMessage msg)
		{
			if (isAuthoritativeServer && !msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing destruction of GameObject(s) in ", group, " by non-authoritative ", msg.sender);
				return;
			}

			var buffer = new List<NetworkViewBase>(_enabledViews.Count);

			foreach (var nv in _enabledViews)
			{
				if (nv.Value.group == group) buffer.Add(nv.Value);
			}

			Log.Debug(NetworkLogFlags.NetworkView, "Destroying ", buffer.Count, " GameObject(s) in ", group);

			_PreDestroyBy(group);

			foreach (var nv in buffer)
			{
				_DestroyNetworkView(nv);
			}
		}

		internal void _RPCDestroyInGroupByPlayerID(NetworkPlayer target, NetworkGroup group, NetworkMessage msg)
		{
			if (isAuthoritativeServer && !msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing destruction of ", target, "'s objects in ", group, " by non-authoritative ", msg.sender);
				return;
			}

			var buffer = new List<NetworkViewBase>(_enabledViews.Count);

			foreach (var nv in _enabledViews)
			{
				if (nv.Value.owner == target && nv.Value.group == group) buffer.Add(nv.Value);
			}

			Log.Debug(NetworkLogFlags.NetworkView, "Destroying ", buffer.Count, " GameObject(s) in ", group, " belonging to ", target);

			_PreDestroyBy(target, group);

			foreach (var nv in buffer)
			{
				_DestroyNetworkView(nv);
			}
		}

		internal void _RPCDestroyAll(bool includeManual, NetworkMessage msg)
		{
			if (isAuthoritativeServer && !msg.sender.isServerOrCellServer)
			{
				Log.Warning(NetworkLogFlags.AuthoritativeServer, "Preventing destruction of all objects by non-authoritative ", msg.sender);
				return;
			}

			_DestroyAll(includeManual);
		}

		internal void _DestroyAll(bool includeManual)
		{
			Log.Debug(NetworkLogFlags.NetworkView, "Destroying all ", _enabledViews.Count,
				includeManual ? " GameObject(s) with allocated and manual view IDs" : " GameObject(s) with only allocated view IDs");

			_PreDestroyAll(includeManual);

			var buffer = Utility.ToArray(_enabledViews);

			if (includeManual)
			{
				foreach (var pair in buffer)
				{
					_DestroyNetworkView(pair.Value);
				}
			}
			else
			{
				foreach (var pair in buffer)
				{
					if (pair.Key.isAllocated) _DestroyNetworkView(pair.Value);
				}
			}
		}

		private void _PreDestroyBy(NetworkViewID viewID)
		{
			if (isServer)
			{
				_ServerPreDestroyBy(viewID);
			}

			_DeallocateViewID(viewID);
		}

		private void _PreDestroyBy(NetworkPlayer owner)
		{
			if (isServer)
			{
				_ServerPreDestroyBy(owner);
			}

			_DeallocateViewIDs(owner);
		}

		private void _PreDestroyBy(NetworkGroup group)
		{
			if (isServer)
			{
				_ServerPreDestroyBy(group);
			}

			_DeallocateViewIDs(group);
		}

		private void _PreDestroyBy(NetworkPlayer owner, NetworkGroup group)
		{
			if (isServer)
			{
				_ServerPreDestroyBy(owner, group);
			}

			_DeallocateViewIDs(owner, group);
		}

		private void _PreDestroyAll(bool includeManual)
		{
			if (isServer)
			{
				_ServerPreDestroyAll(includeManual);
			}

			_DeallocateAllViewIDs();
		}

		protected abstract void _ServerPreDestroyBy(NetworkViewID viewID);
		protected abstract void _ServerPreDestroyBy(NetworkPlayer owner);
		protected abstract void _ServerPreDestroyBy(NetworkGroup group);
		protected abstract void _ServerPreDestroyBy(NetworkPlayer owner, NetworkGroup group);
		protected abstract void _ServerPreDestroyAll(bool includeManual);

		internal void _RPCPlayerIDConnected(NetworkPlayer target, NetworkEndPoint endpoint, BitStream stream, NetworkMessage msg)
		{
			var remainder = stream.ReadBitStream();
			_SetLoginData(target, remainder);

			Log.Debug(NetworkLogFlags.CellServer, target, " has connected to PikkoServer");

			_Notify("OnPlayerConnected", target);
		}

		internal void _RPCPlayerIDDisconnected(NetworkPlayer target, int errorCode, NetworkMessage msg)
		{
			_RemoveLoginData(target);

			Log.Debug(NetworkLogFlags.CellServer, target, " has disconnected from PikkoServer");

			_Notify("OnPlayerDisconnected", target);
		}

		internal void _ChangeAuthFlags(NetworkViewID viewID, NetworkAuthFlags authFlags, Vector3 position)
		{
			// TODO: think about how this should work in independent uLink server setup.
			if (!isCellServer) return;

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.ChangeAuthFlags, viewID);
			msg.stream.WriteByte((byte)authFlags);

			// TODO: fulhacks, position shouldn't be part of the RPC actually, unless it's a specific RPC for only DontHandoverInPikkoServer
			msg.stream.WriteVector3(position);

			_SendRPC(msg);
		}

		internal void _RPCChangeAuthFlags(NetworkViewID viewID, NetworkAuthFlags authFlags, Vector3 position, NetworkMessage msg)
		{
			// TODO: fulhacks, position shouldn't be part of the RPC actually, unless it's a specific RPC for only DontHandoverInPikkoServer

			var nv = _FindNetworkView(viewID);
			if (nv.IsNotNull())
			{
				nv._data.authFlags = authFlags;
			}
		}

		internal void _UnconnectedRPCPreConnectResponse(NetworkEndPoint endpoint)
		{
			string password;

			if (!_preConnectTo.TryGetValue(endpoint, out password)) return;
			
			_ClientConnect(endpoint, new Password(password));
			_Start();

			_preConnectTimeout = Double.NaN;
			_preConnectTo.Clear();
		}

		private void _UnconnectedRPC(UnconnectedMessage msg, NetworkEndPoint target)
		{
			if (_client == null || !_client.IsListening) return;

			Log.Debug(NetworkLogFlags.RPC, "Client is sending unconnected RPC ", msg.internCode, " to ", target);

			_client.SendOutOfBandMessage(msg.stream._buffer, target);
		}

		internal void _HandleUnconnectedMessage(NetBuffer buffer, NetworkEndPoint endpoint)
		{
			UnconnectedMessage msg;

			try
			{
				msg = new UnconnectedMessage(buffer, endpoint);
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.BadMessage, peerType, " failed to parse a incoming unconnected message from ", endpoint, ": ", e.Message);
				return;
			}

			Log.Debug(NetworkLogFlags.BadMessage, peerType, " received and is executing unconnected message: ", msg);

			msg.Execute(this as NetworkBase);
		}

		public void TriggerHandover(NetworkView nv, NetworkPlayer targetCell)
		{
			if (nv.isCellAuthority && (nv.authFlags & NetworkAuthFlags.DontHandoverInPikkoServer) == 0)
			{
				Log.Warning(NetworkLogFlags.Handover, "NetworkView should have NetworkAuthFlags.DontHandoverInPikkoServer for manual handover triggering to have any effect");
			}
			else
			{
				TriggerHandover(nv.viewID, targetCell);
			}
		}

		public void TriggerHandover(NetworkViewID viewID, NetworkPlayer targetCell)
		{
			_AssertViewID(viewID);
			_AssertIsCellServerConnected();
			if (!targetCell.isCellServer) throw new NetworkException("Expecting targetCell argument to be a cell server ID, instead it was " + targetCell);

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.HandoverRequest);
			msg.stream.WriteNetworkViewID(viewID);
			msg.stream.WriteNetworkPlayer(targetCell);

			_SendRPC(msg);
		}
	}
}
