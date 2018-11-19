#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12120 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-05-17 17:46:42 +0200 (Thu, 17 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Net;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	internal abstract class NetworkBaseServer : NetworkBaseClient
	{
		private struct GroupMembership
		{
			public readonly NetworkPlayer owner;
			public readonly NetworkGroup group;
			public readonly GroupData groupData;

			public GroupMembership(NetworkPlayer owner, NetworkGroup group, GroupData groupData)
			{
				this.owner = owner;
				this.group = group;
				this.groupData = groupData;
			}
		}

		private struct GroupBuffer
		{
			public Dictionary<NetworkViewID, BufferedCreate> creates;
			public Dictionary<NetworkViewID, System.Collections.Generic.Dictionary<string, List<BufferedMessage>>> rpcs;

			public static GroupBuffer Create()
			{
				return new GroupBuffer
					{
						creates = new Dictionary<NetworkViewID, BufferedCreate>(),
						rpcs = new Dictionary<NetworkViewID, System.Collections.Generic.Dictionary<string, List<BufferedMessage>>>()
					};
			}
		}

		private readonly Dictionary<NetworkViewID, GroupMembership> _viewMemberships = new Dictionary<NetworkViewID, GroupMembership>();

		private readonly Dictionary<NetworkViewID, HashSet<NetworkPlayer>> _viewCulling = new Dictionary<NetworkViewID, HashSet<NetworkPlayer>>();

		private readonly Dictionary<NetworkGroup, GroupData> _groupsOnConnected = new Dictionary<NetworkGroup, GroupData>();
		private readonly Dictionary<NetworkGroup, GroupBuffer> _groupsNotHidden = new Dictionary<NetworkGroup, GroupBuffer>();

		internal string _masterIP = "unityparkdemo.muchdifferent.com";
		internal int _masterPort = 23466;
		internal string _masterPassword = String.Empty;
		internal Password _masterPasswordHash = Password.empty;

		internal NetworkEndPoint _masterExternalEndpoint = NetworkEndPoint.unassigned;
		internal bool _masterRegistered = false;

		internal float _intervalUpdateHostData = 20.0f;
		internal double _nextUpdateHostData = 0;

		internal double _nextHandoverTimeoutCheck = 0;

		private NetworkMasterMessage _masterPendingRegister;
		private NetConnection _masterConnection;

		private NetPeer _server;

		private NetworkClientAllocator _clientAllocator = new NetworkClientAllocator(false);

		private readonly Dictionary<Password, HandoverSession> _handoverSessions = new Dictionary<Password, HandoverSession>();

		private readonly Dictionary<NetworkPlayer, NetConnection> _userConnections = new Dictionary<NetworkPlayer, NetConnection>();
		private readonly Dictionary<NetworkPlayer, NetworkStatistics> _userConnStats = new Dictionary<NetworkPlayer, NetworkStatistics>();

		private readonly Dictionary<NetworkPlayer, NetworkEndPoint> _disconnectedClientsPendingRemoval = new Dictionary<NetworkPlayer, NetworkEndPoint>();

		private int _lastBufferedIndex;
		private int _lastBufferedCount;
		private readonly Dictionary<NetworkGroup, GroupBuffer> _rpcBuffer = new Dictionary<NetworkGroup, GroupBuffer>();
		
#if !NO_POOLING
		//TODO: Make the pool size configurable for customers.
		private NetBufferPool _netBufferPoolIncomingMessages = new NetBufferPool(1000);
#endif

		private int _maxConnections;
		private int _limitedConnections;

		private readonly Dictionary<NetworkEndPoint, SecurityLayer> _userSecurity = new Dictionary<NetworkEndPoint, SecurityLayer>();

		public string _serverPassword = String.Empty;
		public Password _serverPasswordHash = Password.empty;

		private bool _useRedirect;
		public string redirectIP = String.Empty;
		public string _redirectPassword = String.Empty;
		public Password _redirectPasswordHash = Password.empty;
		public int redirectPort;

		public NetworkConnectionError lastMasterError = NetworkConnectionError.NoError;

		private ParameterWriter _approvalWriter;

		/* TODO: reuse these variables?
		
		private struct BufferedMessage
		{
			public Message message;
			public NetChannel channel;
			public NetConnection exclude;
			public NetConnection target;
		}

		// A buffer of messages to be sent.
		private List<BufferedMessage> bufferedMessages = new List<BufferedMessage>();
		private float bufferedMessageSendDelay = 1f;
		private double nextBufferedMessageSendTime;
		*/

		public bool masterRegistered
		{
			get { return _masterRegistered; }
		}

		public string masterPassword
		{
			get
			{
				return _masterPassword;
			}

			set
			{
				_masterPassword = value;
				_masterPasswordHash = new Password(value);
			}
		}

		public int maxConnections
		{
			get
			{
				return _limitedConnections;
			}

			set
			{
				if (value == -1)
				{
					_limitedConnections = _userConnections.Count;
				}
				else
				{
					if(!(value >= 0)){Utility.Exception( "maxConnections cannot be set to ", value);}
					if(!(value <= _maxConnections)){Utility.Exception( "maxConnections cannot be set higher than the connection count given in Network.InitializeServer.");}

					_limitedConnections = value;
				}
			}
		}

		public string incomingPassword
		{
			get
			{
				return _serverPassword;
			}

			set
			{
				_serverPassword = value;
				_serverPasswordHash = new Password(value);
			}
		}

		public string redirectPassword
		{
			get
			{
				return _redirectPassword;
			}

			set
			{
				_redirectPassword = value;
				_redirectPasswordHash = new Password(value);
			}
		}

		public bool useRedirect
		{
			get { return _useRedirect; }
			set { _useRedirect = value; _ApplyRedirectSettings(); }
		}

		public void InitializeSecurity(bool includingCurrentPlayers)
		{
			Log.Info(NetworkLogFlags.Security, "Initial security layer is now required");
			requireSecurityForConnecting = true;

			if (includingCurrentPlayers && _server != null)
			{
				foreach (var connection in _server.Connections)
				{
					_AddSecurity(connection);
				}
			}
		}

		public void UninitializeSecurity(bool includingCurrentPlayers)
		{
			Log.Info(NetworkLogFlags.Security, "Initial security layer is no longer required");
			requireSecurityForConnecting = false;

			if (includingCurrentPlayers)
			{
				foreach (var pair in _userSecurity)
				{
					_DisableSecurity(_server.GetConnection(pair.Key));
				}
			}
		}

		public void InitializeSecurity(NetworkPlayer player)
		{
			_AssertIsServerListening();

			NetConnection connection = _FindConnection(player);
			if (connection == null)
			{
				// TODO: throw NetworkException
				return;
			}

			_AddSecurity(connection);
		}

		public void UninitializeSecurity(NetworkPlayer player)
		{
			_AssertIsServerListening();

			NetConnection connection = _FindConnection(player);
			if (connection == null)
			{
				// TODO: throw NetworkException
				return;
			}

			_DisableSecurity(connection);
		}

		private void _AddSecurity(NetConnection connection)
		{
			if (_userSecurity.ContainsKey(connection.RemoteEndpoint)) return;

			Log.Info(NetworkLogFlags.Security, "Adding security layer for ", connection);

			var security = new SecurityLayer(privateKey);
			_userSecurity[connection.RemoteEndpoint] = security;

			var publicKey = security.GetPublicKey();

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.SecurityRequest, NetworkPlayer.unassigned);
			msg.stream.WritePublicKey(publicKey);
			_ServerSendTo(msg, connection);
		}

		private void _DisableSecurity(NetConnection connection)
		{
			SecurityLayer security;
			if (!_userSecurity.TryGetValue(connection.RemoteEndpoint, out security)) return;

			if (security.status != NetworkSecurityStatus.Enabled)
			{
				Log.Error(NetworkLogFlags.Security, "Can't disable security layer for ", connection, " until it is fully enabled");
				return;
			}

			Log.Info(NetworkLogFlags.Security, "Disabling security layer for ", connection);

			var symKey = security.GetSymmetricKey();

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.UnsecurityRequest, NetworkPlayer.unassigned);
			msg.stream.WriteSymmetricKey(symKey);
			_ServerSendTo(msg, connection);

			security.BeginDisabling();
		}

		internal override NetworkSecurityStatus _ServerGetSecurityStatus(NetworkPlayer target)
		{
			_AssertIsServerListening();

			if (target == NetworkPlayer.server) return NetworkSecurityStatus.Disabled;

			var connection = _ServerGetConnection(target);
			return connection != null ? _GetConnectionSecurityStatus(connection.RemoteEndpoint) : NetworkSecurityStatus.Disabled;
		}

		internal override bool _ServerIsConnected(NetworkPlayer target)
		{
			_AssertIsServerListening();

			if (target == NetworkPlayer.server) return true;

			return _userConnections.ContainsKey(target);
		}

		internal override NetConnection _ServerGetConnection(NetworkPlayer target)
		{
			_AssertIsServerListening();

			return _FindConnection(target);
		}

		internal override NetworkPlayer _ServerFindConnectionPlayerID(NetConnection connection)
		{
			foreach (var pair in _userConnections)
			{
				if (pair.Value.RemoteEndpoint.Equals(connection.RemoteEndpoint))
				{
					Log.Debug(NetworkLogFlags.PlayerID, "Duplicate connections objects: ", (pair.Value != connection));
					Log.Debug(NetworkLogFlags.PlayerID, "Stored connection tag: ", pair.Value.Tag);
					return pair.Key;
				}
			}

			return NetworkPlayer.unassigned;
		}

		internal override NetworkEndPoint _ServerFindInternalEndPoint(NetworkPlayer target)
		{
			if (!isServer || (_status != NetworkStatus.Connected && _status != NetworkStatus.Disconnecting))
			{
				return NetworkEndPoint.unassigned;
			}

			return (target == NetworkPlayer.server) ? Utility.Resolve(listenPort) : NetworkEndPoint.unassigned;
		}

		internal override NetworkEndPoint _ServerFindExternalEndPoint(NetworkPlayer target)
		{
			if (!isServer || (_status != NetworkStatus.Connected && _status != NetworkStatus.Disconnecting))
			{
				return NetworkEndPoint.unassigned;
			}

			if (target == NetworkPlayer.server)
			{
				return _masterRegistered ? _masterExternalEndpoint : NetworkEndPoint.unassigned;
			}

			NetConnection connection;
			if (_userConnections.TryGetValue(target, out connection))
			{
				return connection.RemoteEndpoint;
			}

			NetworkEndPoint endpoint;
			if (_disconnectedClientsPendingRemoval.TryGetValue(target, out endpoint))
			{
				return endpoint;
			}
			
			return NetworkEndPoint.unassigned;
		}

		private NetworkSecurityStatus _GetConnectionSecurityStatus(NetworkEndPoint endpoint)
		{
			SecurityLayer security;
			return _userSecurity.TryGetValue(endpoint, out security) ? security.status : NetworkSecurityStatus.Disabled;
		}

		public Password PasswordProtectHandoverSession(NetworkPlayer player, NetworkP2PHandoverInstance[] instances, BitStream data, NetworkEndPoint clientDebugInfo)
		{
			_AssertIsServerListening();

			var random = new byte[10];
			Password password;

			do
			{
				NetRandom.Instance.NextBytes(random);
				password = new Password(incomingPassword + Convert.ToBase64String(random));
			} while (_handoverSessions.ContainsKey(password));

			//Start timeout to avoid "frozen avatar" - If client can't connect to this new server with the newly instantiated avatar.
			double maxDelay = _server.Configuration.HandshakeAttemptsMaxCount * _server.Configuration.HandshakeAttemptRepeatDelay;
			double localTimeout = NetworkTime.localTime + maxDelay;

			_handoverSessions.Add(password, new HandoverSession(player, localTimeout, instances, data, clientDebugInfo));

			return password;
		}

		public NetworkConnectionError InitializeServer(int maximumConnections, int localStartPort, int localEndPort, bool useProxy)
		{
			this.useProxy = useProxy;
			return InitializeServer(maximumConnections, localStartPort, localEndPort);
		}

		public NetworkConnectionError InitializeServer(int maximumConnections, int localListenPort, bool useProxy)
		{
			this.useProxy = useProxy;
			return InitializeServer(maximumConnections, localListenPort, localListenPort);
		}

		public NetworkConnectionError InitializeServer(int maximumConnections, int localListenPort)
		{
			return InitializeServer(maximumConnections, localListenPort, localListenPort);
		}

		public NetworkConnectionError InitializeServer(int maximumConnections, int localStartPort, int localEndPort)
		{
			if (maximumConnections < 0)
			{
				throw new ArgumentOutOfRangeException("maximumConnections", maximumConnections, "Can't be negative");
			}

			GoogleAnalytics.TrackEvent(Constants.ANALYTICS_CATEGORY, String.Concat("InitializeServer:", (this as NetworkBaseMaster).gameName),
				Utility.EscapeURL(String.Concat(localStartPort, "-", localEndPort, (useProxy ? "(useProxy)" : String.Empty))));

			if (_status != NetworkStatus.Disconnected)
			{
				lastError = NetworkConnectionError.AlreadyConnectedToAnotherServer;
				return lastError;
			}

			int idCount = NetworkPlayer.maxClient.id - NetworkPlayer.minClient.id;
			if (maximumConnections > idCount)
			{
				maximumConnections = idCount;
				Log.Warning(NetworkLogFlags.Server, "Maximum number of connections", maximumConnections, " can't be higher than ", idCount);
			}

			_maxConnections = maximumConnections;
			_limitedConnections = maximumConnections;

			_PreStart(NetworkStartEvent.Server);

			var config = new NetConfiguration(Constants.CONFIG_NETWORK_IDENTIFIER);
			config.MaxConnections = maximumConnections;
			config.StartPort = localStartPort;
			config.EndPort = localEndPort;
			config.AnswerDiscoveryRequests = false;
			config.SendBufferSize = Constants.DEFAULT_SEND_BUFFER_SIZE;
			config.ReceiveBufferSize = Constants.DEFAULT_RECV_BUFFER_SIZE;

			_server = new NetPeer(config);
			_ConfigureNetBase(_server);

			try
			{
				_server.Start();
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.Server, "Server failed to start: ", e);
				_Cleanup();

				lastError = NetworkConnectionError.CreateSocketOrThreadFailure;
				return lastError;
			}

			Log.Info(NetworkLogFlags.Server, "Server initialized on port ", listenPort);

			_SynchronizeInitialServerTime(0, 0, 0, 0);
			_peerType = NetworkPeerType.Server;
			_status = NetworkStatus.Connected;

			_SetupPlayer(NetworkPlayer.server);

			_ApplyAllGroupFlags();

			_Start();
			_Notify("OnServerInitialized", _localPlayer);

			lastError = NetworkConnectionError.NoError;
			return lastError;
		}

		public void CloseConnection(NetworkPlayer target, bool sendDisconnectionNotification)
		{
			CloseConnection(target, sendDisconnectionNotification, Constants.DEFAULT_DICONNECT_TIMEOUT);
		}

		public void CloseConnection(NetworkPlayer target, bool sendDisconnectionNotification, int timeout)
		{
			if (target == NetworkPlayer.server)
			{
				if(!(!isServer)){Utility.Exception( "Server can't close connection to it self");}

				if (sendDisconnectionNotification)
					Disconnect(timeout);
				else
					DisconnectImmediate();
			}
			else if (isCellServer)
			{
				var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DisconnectPlayerID, NetworkPlayer.server);
				msg.stream.WriteNetworkPlayer(target);
				msg.stream.WriteBoolean(sendDisconnectionNotification);
				_ClientSendRPC(msg);
			}
			else if (isServer)
			{
				NetConnection connection = _FindConnection(target);

				if (connection != null)
				{
					_CloseConnection(connection, sendDisconnectionNotification, timeout);
				}
				else
				{
					// TODO: if missing connection remove reserved password protected player
				}
			}
			else if (isClient)
			{
				throw new NetworkException("Client trying to close connection to another client " + target);
			}
		}

		private void _CloseConnection(NetConnection connection, bool sendDisconnectionNotification)
		{
			_CloseConnection(connection, sendDisconnectionNotification, Constants.DEFAULT_DICONNECT_TIMEOUT);
		}

		private void _CloseConnection(NetConnection connection, bool sendDisconnectionNotification, int timeout)
		{
			if (isServer)
			{
				Log.Debug(NetworkLogFlags.Server, "Server disconnected ", connection);
				connection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, timeout / 1000.0f, sendDisconnectionNotification, true);

				_OnPlayerDisconnected(Constants.REASON_NORMAL_DISCONNECT, connection);
			}
		}

		public void RedirectConnection(NetworkPlayer target, string host, int port, string password)
		{
			NetworkEndPoint redirectTo = Utility.Resolve(host, port);

			RedirectConnection(target, redirectTo, password);
		}

		public void RedirectConnection(NetworkPlayer target, int port, string password)
		{
			RedirectConnection(target, port, new Password(password));
		}

		public void RedirectConnection(NetworkPlayer target, int port, Password passwordHash)
		{
			//by WuNan @2016/09/28 14:30:25
#if (UNITY_IOS  || UNITY_TVOS ) && !UNITY_EDITOR
			NetworkEndPoint redirectTo;
			if(uLink.NetworkUtility.IsSupportIPv6())
			{
				redirectTo = new NetworkEndPoint(IPAddress.IPv6Any, port);
			}
			else
			{
				redirectTo = new NetworkEndPoint(IPAddress.Any, port);
			}
#else
			NetworkEndPoint redirectTo = new NetworkEndPoint(IPAddress.Any, port);
#endif

			RedirectConnection(target, redirectTo, passwordHash);
		}

		public void RedirectConnection(NetworkPlayer target, NetworkEndPoint redirectTo, string password)
		{
			RedirectConnection(target, redirectTo, new Password(password));
		}

		public void RedirectConnection(NetworkPlayer target, NetworkEndPoint redirectTo, Password passwordHash)
		{
			_AssertIsConnected();
			_AssertIsAuthoritative();

			Log.Debug(NetworkLogFlags.Server, "Server redirecting ", target, " to ", redirectTo);

			/* Cell servers also send the PlayerID of the redirecting player. */
			if (isCellServer)
			{
				var msg = new NetworkMessage(this, NetworkMessage.InternalCode.RedirectPlayerID, NetworkPlayer.server);
				msg.stream.WriteNetworkPlayer(target);
				msg.stream.WriteEndPoint(redirectTo);
				msg.stream.WritePassword(passwordHash);
				_ClientSendRPC(msg);
			}
			else
			{
				var msg = new NetworkMessage(this, NetworkMessage.InternalCode.Redirect, target);
				msg.stream.WriteEndPoint(redirectTo);
				msg.stream.WritePassword(passwordHash);
				_ServerSendTo(msg);
			}

			CloseConnection(target, true);
		}

		private void _RedirectConnection(NetConnection connection, string host, int port, Password password)
		{
			_AssertIsAuthoritative();

			NetworkEndPoint redirectTo = Utility.Resolve(host, port);

			Log.Debug(NetworkLogFlags.Server, "Server redirecting ", connection, " to ", redirectTo);

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.Redirect, NetworkPlayer.unassigned);
			msg.stream.WriteEndPoint(redirectTo);
			msg.stream.WritePassword(password);
			_ServerSendTo(msg, connection);

			_CloseConnection(connection, true);
		}

		private bool _ServerCheckMessages()
		{
#if !NO_POOLING
			NetBuffer buffer = _netBufferPoolIncomingMessages.GetNext();
#else
			NetBuffer buffer;
#endif
			NetMessageType type;
			NetConnection connection;
			NetworkEndPoint endpoint;
			NetChannel channel;
			double localTimeRecv;

			// TODO: optimize by adding buffer pool

#if !NO_POOLING
			while (isMessageQueueRunning && _server.ReadMessage(buffer, out type, out connection, out endpoint, out channel, out localTimeRecv))
#else
			while (isMessageQueueRunning && _server.ReadMessage(buffer = _server.CreateBuffer(), out type, out connection, out endpoint, out channel, out localTimeRecv))
#endif
			{
				switch (type)
				{
					case NetMessageType.Data:
						_OnLidgrenMessage(buffer, connection, channel, localTimeRecv);
						break;

					case NetMessageType.StatusChanged:
						_OnLidgrenStatusChanged(buffer.ReadString(), connection);
						break;

					case NetMessageType.ConnectionApproval:
						_OnLidgrenConnectionApproval(connection);
						break;

					case NetMessageType.ConnectionRejected:
						_OnLidgrenConnectionRejected(buffer.ReadString(), connection, endpoint);
						break;

					case NetMessageType.OutOfBandData:
						_OnLidgrenOutOfBandMessage(buffer, endpoint);
						break;

					case NetMessageType.DebugMessage:
					case NetMessageType.VerboseDebugMessage:
						Log.Debug(NetworkLogFlags.BadMessage, "Server debug message: ", buffer.ReadString(), ", endpoint: ", endpoint, " (", connection, ")"); // TODO: when are this called
						break;

					case NetMessageType.BadMessageReceived:
						Log.Warning(NetworkLogFlags.BadMessage, "Server received bad message: ", buffer.ReadString(), ", endpoint: ", endpoint, " (", connection, ")");
						break;
				}

				if (_server == null)
				{
					Log.Debug(NetworkLogFlags.Server, "Server was suddenly shutdown probably due to DisconnectImmediate was called");
					return false;
				}
			}

			return true;
		}

		private void _OnLidgrenStatusChanged(string reason, NetConnection sender)
		{
			Log.Debug(NetworkLogFlags.Server, "Server's internal status of ", sender, " is now ", sender.Status);

			if (sender.Status == NetConnectionStatus.Connected)
			{
				_OnLidgrenClientConnected(sender);
			}
			else if (sender.Status == NetConnectionStatus.Disconnected || sender.Status == NetConnectionStatus.Disconnecting)
			{
				_OnPlayerDisconnected(reason, sender);
			}
		}

		private void _OnLidgrenMessage(NetBuffer buffer, NetConnection connection, NetChannel channel, double localTimeRecv)
		{
			if (_masterConnection != null && _masterConnection.RemoteEndpoint.Equals(connection.RemoteEndpoint))
			{
				_HandleMasterMessage(buffer);
				return;
			}

			if (useRedirect)
			{
				Log.Debug(NetworkLogFlags.BadMessage, "Server dropping incoming message from ", connection, " because redirect is enabled");
				return;
			}

			bool isEncrypted = SecurityLayer.IsEncrypted(buffer);

			if (isEncrypted)
			{
				SecurityLayer security;
				if (!_userSecurity.TryGetValue(connection.RemoteEndpoint, out security) || !security.ServerDecrypt(ref buffer))
				{
					Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Server dropping encrypted incoming message from ", connection, " because decryption failed");
					return;
				}
			}

			NetworkMessage msg;

			try
			{
				msg = new NetworkMessage(this, buffer, connection, channel, isEncrypted, localTimeRecv);
			}
			catch (Exception e)
			{
				Log.Debug(NetworkLogFlags.BadMessage, "Server failed to parse a incoming message from ", connection, " on channel ", channel, ": ", e.Message);
				return;
			}

			_ServerHandleRPC(msg);
		}

		private void _OnLidgrenOutOfBandMessage(NetBuffer buffer, NetworkEndPoint endpoint)
		{
			Log.Debug(NetworkLogFlags.BadMessage, "Server received unconnected message from ", endpoint);

			_HandleUnconnectedMessage(buffer, endpoint);
		}

		private void _OnLidgrenConnectionApproval(NetConnection connection)
		{
			var senderPasswordHash = new Password(connection.RemoteHailData ?? new byte[0]);

			if (_status == NetworkStatus.Disconnecting)
			{
				Log.Debug(NetworkLogFlags.Server, connection, " was disapproved because server is shutting down");
				connection.Disapprove(Constants.REASON_TOO_MANY_PLAYERS);
			}
			else if (_userConnections.Count >= _limitedConnections)
			{
				Log.Debug(NetworkLogFlags.Server, connection, " was disapproved because of too many players");
				connection.Disapprove(Constants.REASON_TOO_MANY_PLAYERS);
			}
			else if (_handoverSessions.ContainsKey(senderPasswordHash))
			{
				Log.Debug(NetworkLogFlags.Server, connection, " was approved by password protected session");
				connection.Approve();
			}
			else if (_serverPasswordHash.isEmpty || senderPasswordHash == _serverPasswordHash)
			{
				Log.Debug(NetworkLogFlags.Server, connection, " was approved", (String.IsNullOrEmpty(incomingPassword) ? "" : " by incoming password"));
				connection.Approve();
			}
			else
			{
				Log.Debug(NetworkLogFlags.Server, connection, " was disapproved because of invalid password");
				connection.Disapprove(Constants.REASON_INVALID_PASSWORD);
			}

			_UnassignConnectionPlayerID(connection); // not necessery but just because we're paraniod about lidgren...
		}

		private void _OnLidgrenConnectionRejected(string reason, NetConnection connection, NetworkEndPoint endpoint)
		{
			if (connection != null && _masterConnection != null && _masterConnection.RemoteEndpoint.Equals(connection.RemoteEndpoint))
			{
				_MasterFailConnection(reason);
				return;
			}

			switch (reason)
			{
				case Constants.NOTIFY_MAX_CONNECTIONS:
					Log.Warning(NetworkLogFlags.Server, "Incoming connection from ", endpoint, " was rejected due to too many players");
					break;

				case Constants.NOTIFY_CONNECT_TO_SELF:
					Log.Warning(NetworkLogFlags.Server, "Server isn't allowed to connect to self");
					break;

				case Constants.NOTIFY_LIMITED_PLAYERS:
					Log.Warning(NetworkLogFlags.Server, "Incoming connection from ", endpoint, " was rejected due to limited players");
					break;

				case Constants.NOTIFY_BAD_APP_ID:
					Log.Warning(NetworkLogFlags.Server, "Incoming connection from ", endpoint, " was rejected due to incompatible uLink version");
					break;

			}
		}

		internal void _MasterFailConnection(string reason)
		{
			Log.Debug(NetworkLogFlags.MasterServer, "Server connection was rejected by master server because: ", reason);

			switch (reason)
			{
				case Constants.REASON_TOO_MANY_PLAYERS:
					_MasterFailConnection(NetworkConnectionError.TooManyConnectedPlayers);
					break;

				case Constants.REASON_INVALID_PASSWORD:
					_MasterFailConnection(NetworkConnectionError.InvalidPassword);
					break;

				case Constants.REASON_CONNECTION_BANNED: // TODO: this need so be implemented
					_MasterFailConnection(NetworkConnectionError.ConnectionBanned);
					break;

				case Constants.REASON_LIMITED_PLAYERS:
					_MasterFailConnection(NetworkConnectionError.LimitedPlayers);
					break;

				case Constants.REASON_BAD_APP_ID:
					_MasterFailConnection(NetworkConnectionError.IncompatibleVersions);
					break;

				default:
					_MasterFailConnection(NetworkConnectionError.ConnectionFailed);
					break;
			}
		}

		private void _MasterFailConnection(NetworkConnectionError error)
		{
			bool notify = (_masterPendingRegister != null);

			_masterConnection = null;
			_masterExternalEndpoint = NetworkEndPoint.unassigned;
			_masterRegistered = false;
			_masterPendingRegister = null;

			if (notify)
			{
				lastMasterError = error;
				_Notify("OnFailedToConnectToMasterServer", error);
			}
		}

		private void _OnPlayerConnected(NetworkPlayer newPlayer, NetConnection connection, BitStream stream)
		{
			Log.Info(NetworkLogFlags.Server, "Server has assigned ", connection, " to ", newPlayer);
			_userConnections.Add(newPlayer, connection);
			_userConnStats.Add(newPlayer, new NetworkStatistics(connection));

			var remainder = stream.GetRemainingBitStream();
			_SetLoginData(newPlayer, remainder);

			_ServerSendNonHiddenBufferedRPCsTo(newPlayer);

			foreach (var groupData in _groupsOnConnected)
			{
				groupData.Value.users.GetOrAdd(newPlayer).Add(NetworkViewID.unassigned);
			}

			_Notify("OnPlayerConnected", newPlayer);
		}

		private void _OnLidgrenClientConnected(NetConnection connection)
		{
			Log.Debug(NetworkLogFlags.Server, "Server is now internally connected to ", connection);

			if (_masterConnection != null && _masterConnection.RemoteEndpoint.Equals(connection.RemoteEndpoint))
			{
				_masterConnection.SendMessage(_masterPendingRegister.stream._buffer, NetChannel.ReliableInOrder1);
				_masterPendingRegister = null;
				return;
			}

			if (useRedirect)
			{
				_RedirectConnection(connection, redirectIP, redirectPort, _redirectPasswordHash);
				return;
			}

			if (requireSecurityForConnecting)
			{
				_AddSecurity(connection);
			}
		}

		private void _OnPlayerDisconnected(string reason, NetConnection connection)
		{
			Log.Info(NetworkLogFlags.Server, "Server is ", connection.Status, " from ", connection, " because ", reason);

			if (_masterConnection != null && _masterConnection.RemoteEndpoint.Equals(connection.RemoteEndpoint))
			{
				_MasterFailConnection(NetworkConnectionError.ConnectionFailed);
				return;
			}

			if (_userSecurity.Remove(connection.RemoteEndpoint))
			{
				Log.Debug(NetworkLogFlags.Security, "Removed security layer for ", connection, " because connection is closed");
			}

			var target = _GetConnectionPlayerID(connection);

			if (target != NetworkPlayer.unassigned)
			{
				Log.Debug(NetworkLogFlags.Server, "Unassigning ", target, " with ", connection);

				_UnassignConnectionPlayerID(connection);

				_userConnections.Remove(target);
				_userConnStats.Remove(target);
				_RemoveLoginData(target);
				_clientAllocator.Deallocate(target, NetworkTime.localTime + _recyclingDelayForPlayerID);

				foreach (var data in _groups)
				{
					var users = data.Value.users;

					HashSet<NetworkViewID> userViews;
					if (users.TryGetValue(target, out userViews))
					{
						userViews.Remove(NetworkViewID.unassigned);

						if (userViews.Count == 0) users.Remove(target);
					}
				}

				foreach (var info in _viewCulling)
				{
					info.Value.Remove(target);
				}

				// NOTE: we'll temporarily store the disconnected player's endpoint in case the
				// application wants to access it in the OnPlayerDisconnected callback.
				// This pending dictionary will be cleared at the end of this update frame.
				_disconnectedClientsPendingRemoval.Add(target, connection.RemoteEndpoint);

				// we should not do more clean up since that is up to the application to decide.

				_Notify("OnPlayerDisconnected", target);
			}
		}

		private NetConnection _FindConnection(NetworkPlayer target)
		{
			NetConnection connection;
			return (_userConnections.TryGetValue(target, out connection)) ? connection : null;
		}

		internal override NetworkPlayer[] _ServerGetPlayers()
		{
			var players = new NetworkPlayer[_userConnections.Count];
			_userConnections.Keys.CopyTo(players, 0);

			return players;
		}

		internal override int _ServerGetPlayerCount()
		{
			return _userConnections.Count;
		}

		internal int _ServerGetPlayerLimit()
		{
			return _limitedConnections;
		}

		internal override int _ServerGetAveragePing(NetworkPlayer target)
		{
			if (target == NetworkPlayer.server) return 0;

			NetConnection connection = _FindConnection(target);

			return (connection != null) ? (int)(connection.AverageRoundtripTime * 1000) : -1;
		}

		internal override int _ServerGetLastPing(NetworkPlayer target)
		{
			if (target == NetworkPlayer.server) return 0;

			NetConnection connection = _FindConnection(target);

			return (connection != null) ? (int)(connection.LastRoundtripTime * 1000) : -1;
		}

		/// <summary>
		/// Get the statistics object associated with the target player connection. Server has no connection to itself.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		internal override NetworkStatistics _ServerGetStatistics(NetworkPlayer target)
		{
			if (target == NetworkPlayer.server) return null;
			NetworkStatistics statistics;
			return (_userConnStats.TryGetValue(target, out statistics)) ? statistics : null;
		}

		internal override void _ServerUpdate(double deltaTime)
		{
			if (_server != null)
			{
				if (isMessageQueueRunning)
				{
					NetProfiler.BeginSample("_ServerCheckMessages");
					bool ok = _ServerCheckMessages();
					NetProfiler.EndSample();

					if (!ok) return;
				}

				/* TODO: reuse this?
				
				double now = time;
				if (now > nextBufferedMessageSendTime)
				{
					nextBufferedMessageSendTime = now + bufferedMessageSendDelay;
					SendBufferedMessages();
				}
				*/

				if (!isServerTimeAutoSynchronized)
				{
					_server.LocalTimeOffset = rawServerTimeOffset;
				}

				double localTime = NetworkTime.localTime;

				_UpdateSmoothServerTime(localTime, deltaTime);

				NetProfiler.BeginSample("_UpdateMasterHostData");
				_UpdateMasterHostData(localTime);
				NetProfiler.EndSample();

				NetProfiler.BeginSample("_UpdateConnectionStats");
				_ServerUpdateConnectionStats(localTime);
				NetProfiler.EndSample();

				NetProfiler.BeginSample("_CheckHandoverTimeouts");
				_ServerCheckHandoverTimeouts(localTime);
				NetProfiler.EndSample();

				// Heartbeat must be called _exactly_ before resetting the outgoing buffer pool!
				// Otherwise a new outgoing message can overwrite the buffer of a unsent message.
				NetProfiler.BeginSample("_SafeUpdate");
				if (!NetUtility.SafeHeartbeat(_server))
				{
					_NetworkShutdown();
					return;
				}
				NetProfiler.EndSample();
				
#if !NO_POOLING
				NetProfiler.BeginSample("_ServerResetBitStreamPools");
				_netBufferPoolIncomingMessages.ReportFrameFinished(); //Reset the NetBufferPool each frame.
				NetProfiler.EndSample();
#endif

				// NOTE: we remove any lingering endpoints for disconnected players. The reason we even have
				// this pending dictionary is to allow the application to still get endpoints of disconnected
				// players at least for the duration of the frame/update.
				_disconnectedClientsPendingRemoval.Clear();
			}
		}

		internal override void _ServerDisconnect(float timeout)
		{
			if (_server != null)
			{
				Log.Info(NetworkLogFlags.Server, "Server disconnecting with timeout ", timeout, " s");

				_maxConnections = 0;
				_limitedConnections = 0;

				foreach (var connection in _server.Connections)
				{
					Log.Debug(NetworkLogFlags.Server, "Server disconnecting from ", connection);
					connection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, timeout, true, true);

					_UnassignConnectionPlayerID(connection);
				}

				// TODO: do we really need to call OnPlayerDisconnected? won't this happen in LidgrenStatusChanged for each connection? Anwser: no ganranty

				var buffer = new List<NetworkPlayer>(_userConnections.Keys);
				_userConnections.Clear();
				_userConnStats.Clear();
				// all other records will be cleared by _ServerCleanup

				foreach (var target in buffer)
				{
					_Notify("OnPlayerDisconnected", target);
				}
			}
		}

		internal override void _ServerCleanup()
		{
			_clientAllocator.Clear();
			_userConnections.Clear();
			_userConnStats.Clear();
			_disconnectedClientsPendingRemoval.Clear();
			_rpcBuffer.Clear();
			_userSecurity.Clear();
			_handoverSessions.Clear();
			_nextUpdateHostData = 0;
			_nextHandoverTimeoutCheck = 0;
			_masterConnection = null;
			_masterExternalEndpoint = NetworkEndPoint.unassigned;
			_masterRegistered = false;
			_masterPendingRegister = null;
			_viewMemberships.Clear();
			_groups.Clear();
			_groupsOnConnected.Clear();
			_groupsNotHidden.Clear();

			if (_server != null)
			{
				Log.Debug(NetworkLogFlags.Server, "Server cleanup...");
				_server.Dispose();
				_server = null;
			}

			_MasterDisconnect();
		}

		internal abstract void _MasterDisconnect();

		internal override NetBase _ServerGetNetBase()
		{
			return _server;
		}

		/* TODO: reuse this:
		
		private void BufferMessage(Message message, NetChannel channel, NetConnection exclude, NetConnection target)
		{
			BufferedMessage m;

			m.message = message;
			m.channel = channel;
			m.exclude = exclude;
			m.target = target;

			bufferedMessages.Add(m);
		}

		private void SendBufferedMessage(BufferedMessage bufferedMessage)
		{
			if (bufferedMessage.exclude != null)
				ServerSendToAllExcept(bufferedMessage.message, bufferedMessage.channel, bufferedMessage.exclude);
			else if (bufferedMessage.target != null)
				_ServerSendTo(bufferedMessage.message, bufferedMessage.channel, bufferedMessage.target);
			else if (bufferedMessage.exclude == null && bufferedMessage.target == null)
				ServerSendToAll(bufferedMessage.message, bufferedMessage.channel);
			else
				Log.Warning("Inconsistent buffered message in SendBufferedMessage", Tag.Warning);
		}

		private void SendBufferedMessages()
		{
			Log.Debug("Sending " + bufferedMessages.Count + " buffered message(s)", Tag.StateSync);

			foreach (BufferedMessage m in bufferedMessages)
				SendBufferedMessage(m);

			bufferedMessages.Clear();
		}
		*/

		internal override void _ServerSendStateSync(NetworkMessage state)
		{
			// TODO: BufferMessage(state.stream.GetBuffer(), StateSync.GetChannel(), state.connection, null);

			// TODO: we have to do this (instead of calling _ServerBroadcast) in case the statesync is only meant for the owner and not everyone.
			_ServerSendRPC(state);
		}

		private void _ServerHandleRPC(NetworkMessage rpc)
		{
			// TODO: sanity check rpc data

			Log.Debug(NetworkLogFlags.RPC, "Server handling ", rpc);

			Log.Debug(NetworkLogFlags.Timestamp, "Server got message ", rpc.name, " with timestamp ", rpc.localTimeSent, " s");

			if (rpc.isFromServer)
			{
				Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, rpc, " is from another server and will be dropped!");
				return;
			}

			if (!rpc.isOnlyToServer)
			{
				if (!isAuthoritativeServer)
				{
					// TODO: optimize by stripping unnecessary data
					_ServerSendRPC(new NetworkMessage(this, rpc));
				}
				else
				{
					Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.AuthoritativeServer, "Preventing message ", rpc.name, " by non-authoritative ", rpc.sender, " from being forwarded");
				}
			}
			else if (rpc.isBuffered)
			{
				_BufferRPC(rpc);
				return;
			}

			if (rpc.isToServerOrAll)
			{
				_ExecuteRPC(rpc);
			}
		}

		internal override void _ServerSendRPC(NetworkMessage rpc)
		{
			Log.Debug(NetworkLogFlags.RPC, "Server sending ", rpc);

			if (rpc.isBuffered)
			{
				_BufferRPC(rpc);

				if (rpc.exclude == NetworkPlayer.server) return; // TODO: fix fulhacks!
			}

			if (rpc.isBroadcast)
			{
				_ServerBroadcast(rpc);
			}
			else // assume this RPC has a target
			{
				_ServerSendTo(rpc);
			}
		}

		internal void _ServerSendTo(NetworkMessage rpc)
		{
			_ServerSendTo(rpc, rpc.target);
		}

		internal void _ServerSendTo(NetworkMessage rpc, NetworkPlayer target)
		{
			NetConnection connection = _FindConnection(target);
			if (connection != null)
			{
				_ServerSendTo(rpc, connection);
			}
			else
			{
				Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, "Private RPC ", (rpc.hasName ? rpc.name: "(internal RPC " + rpc.internCode + ")") + " was not sent because a connection to ", rpc.target, " was not found!");
			}
		}

		private void _ServerBroadcast(NetworkMessage msg)
		{
			NetProfiler.BeginSample("_ServerBroadcast");
			Dictionary<NetworkPlayer, NetConnection> recipients = null;

			NetProfiler.BeginSample("Recipient Collecting");
			if (msg.hasViewID)
			{
				bool isCullable = msg.isCullable &
					msg.internCode != NetworkMessage.InternalCode.Create &
					msg.internCode != NetworkMessage.InternalCode.DestroyByViewID;

				GroupMembership membership;
				if (_viewMemberships.TryGetValue(msg.viewID, out membership))
				{
					var groupData = membership.groupData;
					if (isCullable | ((groupData.flags & NetworkGroupFlags.HideGameObjects) != 0))
					{
						var members = groupData.users;
						recipients = new Dictionary<NetworkPlayer, NetConnection>();
						foreach (var member in members)
						{
							var target = member.Key;

							NetConnection connection;
							if (_userConnections.TryGetValue(target, out connection))
							{
								recipients.Add(target, connection);
							}
						}
					}
				}

				// If we still have no recipients we just gather every user connection
				if (recipients == null) recipients = new Dictionary<NetworkPlayer, NetConnection>(_userConnections);

				if (isCullable)
				{
					HashSet<NetworkPlayer> culling;
					if (_viewCulling.TryGetValue(msg.viewID, out culling))
					{
						foreach (var cull in culling)
						{
							recipients.Remove(cull);
						}
					}
				}
			}
			else
			{
				// Has no view ID, so get all connections
				recipients = new Dictionary<NetworkPlayer, NetConnection>(_userConnections);
			}
			NetProfiler.EndSample();

			// Don't send message to sender if it's being forwarded
			if (msg.sender != NetworkPlayer.server)
			{
				recipients.Remove(msg.sender);
			}

			if (msg.hasExcludeID)
			{
				recipients.Remove(msg.exclude);
			}

			NetProfiler.BeginSample("Send data");
			foreach (var recipient in recipients)
			{
				var connection = recipient.Value;
				_ServerSendTo(msg, connection);
			}
			NetProfiler.EndSample();

			if (!batchSendAtEndOfFrame)
			{
				Log.Debug(NetworkLogFlags.RPC, "Force send message directly, instead of at end of frame");

				double now = NetTime.Now;

				foreach (var recipient in recipients)
				{
					var connection = recipient.Value;
					connection.SendUnsentMessages(now);
				}
			}

			NetProfiler.EndSample();
		}

		private void _ServerSendTo(NetworkMessage msg, NetConnection recipient)
		{
			NetBuffer buffer = msg.GetSendBuffer();

			if (msg.isEncryptable)
			{
				SecurityLayer security;
				if (_userSecurity.TryGetValue(recipient.RemoteEndpoint, out security) && !security.ServerEncrypt(ref buffer))
				{
					Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Server dropping outgoing ", msg, " to endpoint ", recipient.RemoteEndpoint, " because encryption failed");
					return;
				}
			}

			Log.Debug(NetworkLogFlags.RPC, "Server sending ", msg, " to ", recipient);

			try
			{
				_server.SendMessage(buffer, recipient, msg.channel);
			}
			catch (NetException e)
			{
				if (e.Message == "Status must be Connected to send messages")
				{
					Log.Debug(NetworkLogFlags.BadMessage, "Failed to send message because the recipient is no longer connected");
				}
				else
				{
					throw;
				}
			}

			if (!batchSendAtEndOfFrame)
			{
				Log.Debug(NetworkLogFlags.RPC, "Force send message directly, instead of at end of frame");

				recipient.SendUnsentMessages(NetTime.Now);
			}
		}

		private void _ServerSendDestroyInGroup(NetworkPlayer target, NetworkGroup group)
		{
			// TODO: fulhacks workaround! This check should instead be done somewhere else!
			if (!IsConnected(target)) return;

			// TODO: fulhacks workaround! This check should instead be done somewhere else!
			GroupData data;
			if (!_groups.TryGetValue(group, out data) || data.views.Count == 0) return;

			Log.Debug(NetworkLogFlags.Group, "Sending destroy all ", data.views.Count, " object(s) in ", group, " to ", target);

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyInGroup, target);
			msg.stream.WriteNetworkGroup(group);
			_SendRPC(msg);
		}

		private BufferedStateSyncDeltaCompressedInit _GetBufferedStateSyncDeltaCompressedInit(NetworkViewID viewID, NetworkPlayer target)
		{
			NetworkViewBase nv;
			if (_enabledViews.TryGetValue(viewID, out nv) && nv.stateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed)
			{
				if (nv.owner == target)
				{
					if (nv._prevOwnerStateSerialization != null) return new BufferedStateSyncOwnerDeltaCompressedInit(nv, target);
				}
				else
				{
					if (nv._prevProxyStateSerialization != null) return new  BufferedStateSyncProxyDeltaCompressedInit(nv, target);
				}
			}

			return null;
		}

		private void _ServerSendNonHiddenBufferedRPCsTo(NetworkPlayer target)
		{
			Log.Debug(NetworkLogFlags.RPC | NetworkLogFlags.Buffered, "Server sending all non-hidden buffered Instantiates and RPCs to ", target);

			var bufferedMsgs = new List<BufferedMessage>(_lastBufferedCount);
			int createCount = 0;
			int stateCount = 0;
			int rpcCount = 0;

			foreach (var groupBuffer in _groupsNotHidden)
			{
				foreach (var create in groupBuffer.Value.creates)
				{
					var bufferedCreate = create.Value;
					Utility.InsertSorted(bufferedMsgs, bufferedCreate);
					createCount++;

					var bufferedViewID = create.Key;
					var bufferedState = _GetBufferedStateSyncDeltaCompressedInit(bufferedViewID, target);
					if (bufferedState != null)
					{
						Utility.InsertSorted(bufferedMsgs, bufferedState);
						stateCount++;
					}
				}

				foreach (var viewBuffer in groupBuffer.Value.rpcs)
				{
					foreach (var rpcs in viewBuffer.Value)
					{
						foreach (var rpc in rpcs.Value)
						{
							Utility.InsertSorted(bufferedMsgs, rpc);
							rpcCount++;
						}
					}
				}
			}

			Log.Debug(NetworkLogFlags.Buffered, "Buffer RPC contains ", createCount, " Instantiates and ", rpcCount, " Custom RPCs and ", stateCount, " state syncs");

			if (bufferedMsgs.Count == 0) return; // TODO: is this check really necessary?

			_lastBufferedCount = bufferedMsgs.Count;
			var buffers = new SerializedBuffer[_lastBufferedCount];

			for (int i = 0; i < bufferedMsgs.Count; i++)
			{
				buffers[i] = new SerializedBuffer(bufferedMsgs[i].msg.stream);
			}

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.BufferedRPCs, target);
			msg.stream.WriteSerializedBuffers(buffers);
			_ServerSendTo(msg);
		}

		private void _ServerSendBufferedRPCsTo(NetworkPlayer target, NetworkGroup group)
		{
			Log.Debug(NetworkLogFlags.RPC | NetworkLogFlags.Buffered, "Server sending all buffered Instantiates and RPCs in ", group, " to ", target);

			GroupBuffer groupBuffer;
			if (!_rpcBuffer.TryGetValue(group, out groupBuffer)) return;

			var bufferedMsgs = new List<BufferedMessage>(groupBuffer.creates.Count + groupBuffer.rpcs.Count);
			int stateCount = 0;
	
			foreach (var create in groupBuffer.creates)
			{
				var bufferedCreate = create.Value;
				Utility.InsertSorted(bufferedMsgs, bufferedCreate);

				var bufferedViewID = create.Key;
				var bufferedState = _GetBufferedStateSyncDeltaCompressedInit(bufferedViewID, target);
				if (bufferedState != null)
				{
					Utility.InsertSorted(bufferedMsgs, bufferedState);
					stateCount++;
				}
			}

			foreach (var viewBuffer in groupBuffer.rpcs)
			{
				foreach (var rpcs in viewBuffer.Value)
				{
					foreach (var rpc in rpcs.Value)
					{
						if (rpc.msg.exclude != target) Utility.InsertSorted(bufferedMsgs, rpc);
					}
				}
			}

			Log.Debug(NetworkLogFlags.Buffered, "Buffer RPC contains ", groupBuffer.creates.Count, " Instantiates and ", bufferedMsgs.Count - groupBuffer.creates.Count, " Custom RPCs and ", stateCount, " state syncs");

			if (bufferedMsgs.Count == 0) return; // TODO: is this check really necessary? isn't group buffer removed if it was empty?

			var buffers = new SerializedBuffer[bufferedMsgs.Count];

			for (int i = 0; i < bufferedMsgs.Count; i++)
			{
				buffers[i] = new SerializedBuffer(bufferedMsgs[i].msg.stream);
			}

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.BufferedRPCs, target);
			msg.stream.WriteSerializedBuffers(buffers);
			_ServerSendTo(msg);
		}

		private void _BufferRPC(NetworkMessage rpc)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Buffering RPC sent by player ", rpc.sender);

			// TODO: set long timestamp!

			if (rpc.internCode == NetworkMessage.InternalCode.Create)
			{
				// TODO: fix fulhacks!
				var oldPos = rpc.stream._buffer.PositionBits;
				var owner = rpc.stream.ReadNetworkPlayer();
				var group = rpc.stream.ReadNetworkGroup();
				rpc.stream._buffer.PositionBits = oldPos;

				if (group != NetworkGroup.unassigned)
				{
					_AddPlayerToGroup(owner, group, rpc.viewID);
				}
				
				_lastBufferedIndex++;
				var creates = _rpcBuffer.GetOrAdd(group, GroupBuffer.Create).creates;
				creates.Add(rpc.viewID, new BufferedCreate(rpc, _lastBufferedIndex, owner));
			}
			else if (rpc.hasViewID)
			{
				var group = _GetGroup(rpc.viewID);
				var rpcs = _rpcBuffer.GetOrAdd(group, GroupBuffer.Create).rpcs.GetOrAdd(rpc.viewID).GetOrAdd(rpc.name);

				rpcs.Add(new BufferedMessage(rpc, ++_lastBufferedIndex));
			}
			else
			{
				Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.Buffered, "Server can't buffer internal RPC: ", rpc.internCode);
				return;
			}
		}

		private NetworkGroup _GetGroup(NetworkViewID viewID)
		{
			GroupMembership membership;
			return _viewMemberships.TryGetValue(viewID, out membership) ? membership.group : NetworkGroup.unassigned;
		}

		internal void _AddScope(NetworkViewID viewID, HashSet<NetworkPlayer> scopeCulling)
		{
			_viewCulling[viewID] = scopeCulling;
		}

		internal void _RemoveScope(NetworkViewID viewID)
		{
			_viewCulling.Remove(viewID);
		}

		public void ResetScope(NetworkPlayer target)
		{
			var toBeRemoved = new List<NetworkViewID>();

			foreach (var pair in _viewCulling)
			{
				pair.Value.Remove(target);

				if (pair.Value.Count == 0) toBeRemoved.Add(pair.Key);
			}

			for (int i = 0; i < toBeRemoved.Count; i++)
			{
				_viewCulling.Remove(toBeRemoved[i]);
				// NOTE: we aren't nulling NetworkViews _viewCulling ref because it's not really necessery (althought would be cleaner).
			}
		}

		internal void _ChangeGroup(NetworkViewBase nv, NetworkGroup newGroup)
		{
			var oldGroup = nv.group;
			if (oldGroup == newGroup) return;

			Log.Debug(NetworkLogFlags.Group, "Changing ", nv, "'s group from ", oldGroup, " to ", newGroup);
			
			// change group on the server
			nv._data.group = newGroup;

			var viewID = nv.viewID;
			var owner = nv.owner;

			BufferedCreate create = null;
			System.Collections.Generic.Dictionary<string, List<BufferedMessage>> rpcs = null;

			// edit the create and RPC buffer
			GroupBuffer oldBuffer;
			if (_rpcBuffer.TryGetValue(oldGroup, out oldBuffer))
			{
				if (oldBuffer.creates.TryGetValue(viewID, out create))
				{
					Log.Debug(NetworkLogFlags.Group, "Modifying buffered instantiate of ", viewID, " to ", newGroup);

					// TODO: fulfulhacks!
					var stream = create.msg.stream;
					int oldPos = stream._buffer.PositionBits;
					var data = stream._data;
					stream._buffer.PositionBytes = (data[0] & (byte)NetworkMessage.HeaderFlags.HasOriginalSenderPlayerID) == 0 ? 9 : 11;
					stream.ReadNetworkPlayer(); // skip variable-int playerID.
					int index = stream._buffer.PositionBytes;
					stream._buffer.PositionBits = oldPos;


					data[index] = (byte)(newGroup.id & 0x00FF);
					data[index + 1] = (byte)((newGroup.id & 0xFF00) >> 8);

					oldBuffer.creates.Remove(viewID);

					var newBuffer = _rpcBuffer.GetOrAdd(newGroup, GroupBuffer.Create);
					newBuffer.creates[viewID] = create;
				}

				if (oldBuffer.rpcs.TryGetValue(viewID, out rpcs))
				{
					Log.Debug(NetworkLogFlags.Group, "Modifying buffered RPC(s) of ", viewID, " to ", newGroup);

					oldBuffer.rpcs.Remove(viewID);

					var newBuffer = _rpcBuffer.GetOrAdd(newGroup, GroupBuffer.Create); // TODO: optimize!
					newBuffer.rpcs[viewID] = rpcs;
				}
			}
			
			HashSet<NetworkPlayer> oldPlayers;
			HashSet<NetworkPlayer> newPlayers;
			
			// remove from old group and list all member players of that group
			if (oldGroup != NetworkGroup.unassigned)
			{
				Log.Debug(NetworkLogFlags.Group, "Removing ", viewID, " from ", oldGroup);

				var oldData = _RemovePlayerFromGroup(owner, oldGroup, viewID);

				oldPlayers = (oldData != null && oldData.users.Count != 0) ?
					new HashSet<NetworkPlayer>(oldData.users.Keys) : new HashSet<NetworkPlayer>();

				oldPlayers.Remove(owner);
				oldPlayers.Remove(NetworkPlayer.server); // TODO: just in case, but shouldn't ever be the case!
			}
			else
			{
				oldPlayers = new HashSet<NetworkPlayer>(_userConnections.Keys);
				oldPlayers.Remove(owner);
			}

			// add to new group and list all member players of that group
			if (newGroup != NetworkGroup.unassigned)
			{
				Log.Debug(NetworkLogFlags.Group, "Adding ", viewID, " to ", newGroup);

				var newData = _AddPlayerToGroup(owner, newGroup, viewID);

				newPlayers = (newData != null && newData.users.Count != 0) ?
					new HashSet<NetworkPlayer>(newData.users.Keys) : new HashSet<NetworkPlayer>();

				newPlayers.Remove(owner);
				newPlayers.Remove(NetworkPlayer.server); // TODO: just in case, but shouldn't ever be the case!
			}
			else
			{
				newPlayers = new HashSet<NetworkPlayer>(_userConnections.Keys);
				newPlayers.Remove(owner);

				_viewMemberships.Remove(viewID);
			}

			var isOldHidden = (oldGroup.flags & NetworkGroupFlags.HideGameObjects) != 0;
			var isNewHidden = (newGroup.flags & NetworkGroupFlags.HideGameObjects) != 0;

			if (isOldHidden)
			{
				var createPlayers = new HashSet<NetworkPlayer>(newPlayers);
				createPlayers.ExceptWith(oldPlayers);

				// send buffered create and rpcs to new players which previously couldn't see the object
				if (createPlayers.Count != 0 && (create != null || rpcs != null))
				{
					Log.Debug(NetworkLogFlags.Group, "Sending buffered Instantiate and/or RPC(s) to ", createPlayers.Count, " new player(s)");

					var bufferedMsgs = new List<BufferedMessage>(1);
					if (create != null) Utility.InsertSorted(bufferedMsgs, create);

					if (rpcs != null)
					{
						foreach (var list in rpcs)
						{
							foreach (var rpc in list.Value)
							{
								Utility.InsertSorted(bufferedMsgs, rpc);
							}
						}
					}

					if (bufferedMsgs.Count != 0)
					{
						var buffers = new SerializedBuffer[bufferedMsgs.Count];

						for (int i = 0; i < bufferedMsgs.Count; i++)
						{
							buffers[i] = new SerializedBuffer(bufferedMsgs[i].msg.stream);
						}

						var createMsg = new NetworkMessage(this, NetworkMessage.InternalCode.BufferedRPCs);
						createMsg.stream.WriteSerializedBuffers(buffers);

						foreach (var target in createPlayers)
						{
							_ServerSendTo(createMsg, target);
						}
					}
				}
			}

			if (isNewHidden)
			{
				var destroyPlayers = new HashSet<NetworkPlayer>(oldPlayers);
				destroyPlayers.ExceptWith(newPlayers);

				// send destroy to old players which no longer will see the object
				if (destroyPlayers.Count != 0)
				{
					Log.Debug(NetworkLogFlags.Group, "Sending Destroy to ", destroyPlayers.Count, " previous player(s)");

					var destroyMsg = new NetworkMessage(this, NetworkMessage.InternalCode.DestroyByViewID, viewID);

					foreach (var target in destroyPlayers)
					{
						_ServerSendTo(destroyMsg, target);
					}
				}
			}

			HashSet<NetworkPlayer> changePlayers;

			if (isOldHidden || isNewHidden)
			{
				changePlayers = new HashSet<NetworkPlayer>(newPlayers);
				changePlayers.IntersectWith(oldPlayers);
			}
			else
			{
				changePlayers = new HashSet<NetworkPlayer>(_userConnections.Keys);
				changePlayers.Remove(owner);
			}
			
			// send change group to players which will continue seeing the object
			if (changePlayers.Count != 0 || owner != NetworkPlayer.server)
			{
				Log.Debug(NetworkLogFlags.Group, "Sending GroupChange to ", changePlayers.Count, " previous player(s)");

				var changeMsg = new NetworkMessage(this, NetworkMessage.InternalCode.ChangeGroup);
				changeMsg.stream.WriteNetworkViewID(viewID);
				changeMsg.stream.WriteNetworkGroup(newGroup);

				foreach (var target in changePlayers)
				{
					_ServerSendTo(changeMsg, target);
				}

				if (owner != NetworkPlayer.server)
				{
					Log.Debug(NetworkLogFlags.Group, "Sending GroupChange to owner ", owner);

					_ServerSendTo(changeMsg, owner);
				}
			}
		}

		public void AddPlayerToGroup(NetworkPlayer target, NetworkGroup group)
		{
			_AssertPlayer(target);
			if(!(target != NetworkPlayer.server)){Utility.Exception( "Can't add server to a group.");}
			_AssertGroup(group);
			_AssertIsServerListening();

			_AddPlayerToGroup(target, group, NetworkViewID.unassigned);
		}

		public void RemovePlayerFromGroup(NetworkPlayer target, NetworkGroup group)
		{
			_AssertPlayer(target);
			if(!(target != NetworkPlayer.server)){Utility.Exception( "Can't remove server from a group.");}
			_AssertGroup(group);
			_AssertIsServerListening();

			GroupData data;
			if (_groups.TryGetValue(group, out data))
			{
				HashSet<NetworkViewID> views;
				if (data.users.TryGetValue(target, out views))
				{
					if (views.Count == 1 && views.Contains(NetworkViewID.unassigned))
					{
						data.users.Remove(target);
						if ((group.flags & NetworkGroupFlags.HideGameObjects) != 0) _ServerSendDestroyInGroup(target, group);
					}
					else
					{
						Utility.Exception("Can't remove ", target, " from ", group, " because player still has ", views.Count, " object(s) in that group.");
					}
				}
			}
		}

		protected override void _ServerAddPlayerToGroup(NetworkPlayer owner, NetworkGroup group, NetworkViewID viewID, GroupData groupData, int userSetCount)
		{
			if (isServer)
			{
				if ((groupData.flags & NetworkGroupFlags.HideGameObjects) == 0 && groupData.users.Count == 1 && groupData.views.Count == 1 && !_groupsNotHidden.ContainsKey(group))
				{
					Log.Debug(NetworkLogFlags.Group, "Adding ", group, " to non-hidden group buffer list");
					var groupBuffer = _rpcBuffer.GetOrAdd(group, GroupBuffer.Create);
					_groupsNotHidden.Add(group, groupBuffer);
				}

				if ((viewID == NetworkViewID.unassigned || _viewMemberships.TryAdd(viewID, new GroupMembership(owner, group, groupData))) && owner != NetworkPlayer.server && userSetCount == 1 && (groupData.flags & NetworkGroupFlags.HideGameObjects) != 0)
				{
					_ServerSendBufferedRPCsTo(owner, group);
				}
			}
		}

		protected override void _ServerRemovePlayerFromGroup(NetworkPlayer owner, NetworkGroup group, NetworkViewID viewID, GroupData groupData, int userSetCount)
		{
			if (isServer && (viewID == NetworkViewID.unassigned || _viewMemberships.Remove(viewID)) && owner != NetworkPlayer.server && userSetCount == 0 && (groupData.flags & NetworkGroupFlags.HideGameObjects) != 0)
			{
				_ServerSendDestroyInGroup(owner, group);
			}
		}

		internal void _UngroupViewID(NetworkViewID viewID)
		{
			GroupMembership info;
			if (_viewMemberships.TryGetValue(viewID, out info))
			{
				_RemovePlayerFromGroup(info.owner, info.group, viewID);
			}
		}

		internal void _UngroupPlayer(NetworkPlayer owner)
		{
			foreach (var pair in _groups)
			{
				var data = pair.Value;

				HashSet<NetworkViewID> views;
				if (data.users.TryGetValue(owner, out views))
				{
					data.users.Remove(owner);
					data.views.RemoveWhere(delegate(NetworkViewID viewID) { return views.Contains(viewID); });

					//_viewMemberships.RemoveWhere(delegate(NetworkViewID viewID) { return views.Contains(viewID); });

					if (owner != NetworkPlayer.server && (data.flags & NetworkGroupFlags.HideGameObjects) != 0) _ServerSendDestroyInGroup(owner, pair.Key);
				}
			}
		}

		internal void _UngroupAllInGroup(NetworkGroup group)
		{
			if (group == NetworkGroup.unassigned) return;

			GroupData data;
			if (_groups.TryGetValue(group, out data))
			{
				/*
				foreach (var pair in data.users)
				{
					var target = pair.Key;
					if (target != NetworkPlayer.server) _ServerSendDestroyInGroup(target, group);
				}
				*/

				//_viewMemberships.RemoveWhere(delegate(NetworkViewID viewID) { return data.views.Contains(viewID); });

				data.users.Clear();
				data.views.Clear();
			}
		}

		internal void _UngroupPlayerInGroup(NetworkPlayer owner, NetworkGroup group)
		{
			if (group == NetworkGroup.unassigned) return;

			GroupData data;
			if (_groups.TryGetValue(group, out data))
			{
				HashSet<NetworkViewID> views;
				if (data.users.TryGetValue(owner, out views))
				{
					data.users.Remove(owner);
					data.views.RemoveWhere(delegate(NetworkViewID viewID) { return views.Contains(viewID); });

					//_viewMemberships.RemoveWhere(delegate(NetworkViewID viewID) { return data.views.Contains(viewID); });

					if (owner != NetworkPlayer.server && (data.flags & NetworkGroupFlags.HideGameObjects) != 0) _ServerSendDestroyInGroup(owner, group);
				}
			}
		}

		internal void _UngroupAll(bool includeManual)
		{
			// TODO: don't remove manual viewIDs if includeManual is false!

			foreach (var groupPair in _groups)
			{
				var members = groupPair.Value;

				// TODO: fulhacks if statement workaround! This check should instead be done somewhere else I think!
				if (isServer && status == NetworkStatus.Connected) // make sure server is still initialized
				{
					/*
					foreach (var userPair in members.users)
					{
						var player = userPair.Key;
						if (player != NetworkPlayer.server) _ServerSendDestroyInGroup(player, groupPair.Key);
					}
					*/
				}

				members.users.Clear();
				members.views.Clear();
			}

			_groups.Clear();
		}

		internal void _RemoveInstantiates(NetworkPlayer owner)
		{
			// TODO: Optimize this with a seperate _BufferedCreateRPCsByOwner dictionary<player, list<viewID>>

			Log.Debug(NetworkLogFlags.Buffered, "Removing all buffered Instantiates for owner ", owner);

			foreach (var groupBuffer in _rpcBuffer)
			{
				var pairs = new List<KeyValuePair<NetworkViewID, BufferedCreate>>(groupBuffer.Value.creates);

				for (int i = 0; i < pairs.Count; i++)
				{
					var pair = pairs[i];
					if (pair.Value.owner == owner)
					{
						groupBuffer.Value.creates.Remove(pair.Key);
					}
				}
			}
		}

		internal void _RemoveInstantiates(NetworkPlayer owner, NetworkGroup group)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing all buffered Instantiates for owner ", owner, " and in ", group);

			GroupBuffer groupBuffer;
			if (_rpcBuffer.TryGetValue(group, out groupBuffer))
			{
				var pairs = new List<KeyValuePair<NetworkViewID, BufferedCreate>>(groupBuffer.creates);

				for (int i = 0; i < pairs.Count; i++)
				{
					var pair = pairs[i];
					if (pair.Value.owner == owner)
					{
						groupBuffer.creates.Remove(pair.Key);
					}
				}
			}
		}

		internal void _RemoveInstantiatesInGroup(NetworkGroup group)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing buffered Instantiates in ", group);

			GroupBuffer groupBuffer;
			if (_rpcBuffer.TryGetValue(group, out groupBuffer))
			{
				groupBuffer.creates.Clear();
			}
		}

		internal void _RemoveInstantiate(NetworkViewID viewID)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing buffered Instantiate with ", viewID);

			var group = _GetGroup(viewID);

			GroupBuffer groupBuffer;
			if (_rpcBuffer.TryGetValue(group, out groupBuffer))
			{
				groupBuffer.creates.Remove(viewID);
			}
		}

		internal void _RemoveAllInstantiates()
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing all buffered Instantiates");

			foreach (var groupBuffer in _rpcBuffer)
			{
				groupBuffer.Value.creates.Clear();
			}
		}

		internal void _RemoveRPCs(NetworkPlayer sender)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs sent by ", sender);

			foreach (var groupBuffer in _rpcBuffer)
			{
				foreach (var viewBuffer in groupBuffer.Value.rpcs)
				{
					foreach (var rpcs in viewBuffer.Value)
					{
						rpcs.Value.RemoveAll(delegate(BufferedMessage value) { return value.msg.sender == sender; });
					}
				}
			}
		}

		internal void _RemoveRPCs(NetworkPlayer sender, NetworkGroup group)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs sent by ", sender, " and in ", group);

			GroupBuffer groupBuffer;
			if (_rpcBuffer.TryGetValue(group, out groupBuffer))
			{
				foreach (var viewBuffer in groupBuffer.rpcs)
				{
					foreach (var rpcs in viewBuffer.Value)
					{
						rpcs.Value.RemoveAll(delegate(BufferedMessage value) { return value.msg.sender == sender; });
					}
				}
			}
		}

		internal void _RemoveRPCsInGroup(NetworkGroup group)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs in ", group);

			GroupBuffer groupBuffer;
			if (_rpcBuffer.TryGetValue(group, out groupBuffer))
			{
				groupBuffer.rpcs.Clear();
			}
		}

		internal void _RemoveRPCs(NetworkViewID viewID)
		{
			Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs with ", viewID);

			var group = _GetGroup(viewID);

			GroupBuffer groupBuffer;
			if (_rpcBuffer.TryGetValue(group, out groupBuffer))
			{
				groupBuffer.rpcs.Remove(viewID);
			}
		}

		internal void _RemoveAllRPCs(bool includeManual)
		{
			// TODO: don't remove manual viewIDs if includeManual is false!

			Log.Debug(NetworkLogFlags.Buffered, "Removing all buffered RPCs");

			foreach (var groupBuffer in _rpcBuffer)
			{
				groupBuffer.Value.rpcs.Clear();
			}
		}

		public void RemoveInstantiates(NetworkPlayer owner)
		{
			// TODO: add support if cell server
			_AssertIsServerListening();
			_RemoveInstantiates(owner);
		}

		public void RemoveInstantiates(NetworkPlayer owner, NetworkGroup group)
		{
			// TODO: add support if cell server
			_AssertGroup(group);
			_AssertIsServerListening();
			_RemoveInstantiates(owner, group);
		}

		public void RemoveInstantiatesInGroup(NetworkGroup group)
		{
			// TODO: add support if cell server
			_AssertGroup(group);
			_AssertIsServerListening();
			_RemoveInstantiatesInGroup(group);
		}

		public void RemoveInstantiate(NetworkViewID viewID)
		{
			// TODO: add support if cell server
			_AssertIsServerListening();
			_RemoveInstantiate(viewID);
		}

		public void RemoveAllInstantiates()
		{
			// TODO: add support if cell server
			_AssertIsServerListening();
			_RemoveAllInstantiates();
		}

		public void RemoveRPCs(NetworkPlayer sender)
		{
			if (isCellServer)
			{
				_RemoveRPCsInPikko(sender, NetworkViewID.unassigned, string.Empty);
			}
			else
			{
				// TODO: add support if cell server
				_AssertIsServerListening();
				_RemoveRPCs(sender);
			}
		}

		public void RemoveRPCs(NetworkPlayer sender, NetworkGroup group)
		{
			// TODO: add support if cell server
			_AssertGroup(group);
			_AssertIsServerListening();
			_RemoveRPCs(sender, group);
		}

		public void RemoveRPCsInGroup(NetworkGroup group)
		{
			// TODO: add support if cell server
			_AssertGroup(group);
			_AssertIsServerListening();
			_RemoveRPCsInGroup(group);
		}

		public void RemoveRPCs(NetworkViewID viewID)
		{
			if (isCellServer)
			{
				_RemoveRPCsInPikko(NetworkPlayer.unassigned, viewID, string.Empty);
			}
			else
			{
				_AssertIsServerListening();
				_RemoveRPCs(viewID);
			}
		}

		public void RemoveAllRPCs(bool includeManual)
		{
			if (isCellServer)
			{
				_RemoveRPCsInPikko(NetworkPlayer.unassigned, NetworkViewID.unassigned, string.Empty);
			}
			else
			{
				_AssertIsServerListening();
				_RemoveAllRPCs(includeManual);
			}
		}

		public void RemoveRPCsByName(NetworkGroup group, string rpcName)
		{
			// TODO: add support if cell server

			_AssertIsServerListening();

			Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs in ", group, " and name ", rpcName);

			GroupBuffer groupBuffer;
			if (_rpcBuffer.TryGetValue(group, out groupBuffer))
			{
				foreach (var viewBuffer in groupBuffer.rpcs)
				{
					viewBuffer.Value.Remove(rpcName);
				}
			}
		}

		public void RemoveRPCsByName(NetworkViewID viewID, string rpcName)
		{
			if (isCellServer)
			{
				_RemoveRPCsInPikko(NetworkPlayer.unassigned, viewID, rpcName);
			}
			else
			{
				_AssertIsServerListening();

				Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs with ", viewID, " and name ", rpcName);

				var group = _GetGroup(viewID);

				GroupBuffer groupBuffer;
				if (_rpcBuffer.TryGetValue(group, out groupBuffer))
				{
					System.Collections.Generic.Dictionary<string, List<BufferedMessage>> viewBuffer;
					if (groupBuffer.rpcs.TryGetValue(viewID, out viewBuffer))
					{
						viewBuffer.Remove(rpcName);
					}
				}
			}
		}

		public void RemoveRPCsByName(NetworkPlayer sender, string rpcName)
		{
			if (isCellServer)
			{
				_RemoveRPCsInPikko(sender, NetworkViewID.unassigned, rpcName);
			}
			else
			{
				_AssertIsServerListening();

				Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs sent by ", sender, " with name ", rpcName);

				foreach (var groupBuffer in _rpcBuffer)
				{
					foreach (var viewBuffer in groupBuffer.Value.rpcs)
					{
						List<BufferedMessage> rpcs;
						if (viewBuffer.Value.TryGetValue(rpcName, out rpcs))
						{
							rpcs.RemoveAll(delegate(BufferedMessage value) { return value.msg.sender == sender; });
						}
					}
				}
			}
		}

		public void RemoveRPCsByName(string rpcName)
		{
			if (isCellServer)
			{
				_RemoveRPCsInPikko(NetworkPlayer.unassigned, NetworkViewID.unassigned, rpcName);
			}
			else
			{
				_AssertIsServerListening();

				Log.Debug(NetworkLogFlags.Buffered, "Removing buffered RPCs with name ", rpcName);

				foreach (var groupBuffer in _rpcBuffer)
				{
					foreach (var viewBuffer in groupBuffer.Value.rpcs)
					{
						viewBuffer.Value.Remove(rpcName);
					}
				}
			}
		}

		protected override void _ServerPreDestroyBy(NetworkViewID viewID)
		{
			_RemoveRPCs(viewID);
			_RemoveInstantiate(viewID);
			_UngroupViewID(viewID);
		}

		protected override void _ServerPreDestroyBy(NetworkPlayer owner)
		{
			_RemoveRPCs(owner);
			_RemoveInstantiates(owner);
			_UngroupPlayer(owner);
		}

		protected override void _ServerPreDestroyBy(NetworkGroup group)
		{
			_RemoveInstantiatesInGroup(group);
			_RemoveRPCsInGroup(group);
			_UngroupAllInGroup(group);
		}

		protected override void _ServerPreDestroyBy(NetworkPlayer owner, NetworkGroup group)
		{
			_RemoveInstantiates(owner, group);
			_RemoveRPCs(owner, group);
			_UngroupPlayerInGroup(owner, group);
		}

		protected override void _ServerPreDestroyAll(bool includeManual)
		{
			// TODO: don't remove for manual viewIDs

			_RemoveAllRPCs(includeManual);
			_RemoveAllInstantiates();
			_UngroupAll(includeManual);
		}

		private void _SendConnectDenied(NetConnection connection, NetworkConnectionError error)
		{
			Log.Debug(NetworkLogFlags.Server, "Server denying connect request from ", connection, " with error: ", error);

			var msg = new NetworkMessage(this, NetworkMessage.InternalCode.ConnectDenied, NetworkPlayer.unassigned);
			msg.stream.WriteInt32((int)error);
			_ServerSendTo(msg, connection);

			connection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, Constants.DEFAULT_DICONNECT_TIMEOUT / 1000.0f);
		}

		internal void _RPCClientConnectRequest(BitStream stream, NetworkMessage msg)
		{
			_AssertSenderIsRemote(msg);

			if (msg.connection.Status != NetConnectionStatus.Connected)
			{
				Log.Error(NetworkLogFlags.BadMessage | NetworkLogFlags.Server, "Server dropping client connect request from ", msg.connection, " because internal connection status is ", msg.connection.Status);
				return;
			}

			if (requireSecurityForConnecting && (_GetConnectionSecurityStatus(msg.connection.RemoteEndpoint) != NetworkSecurityStatus.Enabled || (msg.flags & NetworkFlags.Unencrypted) != 0))
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Server dropping client connect request from ", msg.connection, " because required security layer is missing");
				return;
			}

			if (_HasConnectionPlayerID(msg.connection))
			{
				Log.Warning(NetworkLogFlags.BadMessage | NetworkLogFlags.Server, "Server dropping client connect request from ", _GetConnectionPlayerID(msg.connection), " because it is already assigned");
				return;
			}

			var passwordHash = new Password(msg.connection.RemoteHailData ?? new byte[0]);

			Log.Debug(NetworkLogFlags.Server, "Client trying to connect with password ", passwordHash);

			HandoverSession handover;
			if (_handoverSessions.TryGetValue(passwordHash, out handover))
			{
				_handoverSessions.Remove(passwordHash);

				Log.Debug(NetworkLogFlags.Handover, "Server got client connect request as part of a handover, ", handover.player);
			}
			else
			{
				handover.instances = new NetworkP2PHandoverInstance[0];
				handover.data = null;
			}

			var approval = new NetworkPlayerApproval(this, msg, handover.instances, handover.data);

			_Notify("OnPlayerApproval", approval);

			if (!isServer)
			{
				Log.Warning(NetworkLogFlags.Server, "Server should not be shutdown during player approval");
				return;
			}

			if (approval._status == NetworkPlayerApprovalStatus.AutoApproving)
			{
				_ApproveClient(approval, msg, new object[0]);
			}
		}

		internal void _DenyClient(NetworkPlayerApproval approval, NetworkMessage msg, NetworkConnectionError reason)
		{
			Log.Debug(NetworkLogFlags.Server, "Server disapproved ", msg.connection, " with reason ", reason);
			_SendConnectDenied(msg.connection, reason);
		}

		internal void _ApproveClient(NetworkPlayerApproval approval, NetworkMessage msg, object[] approvalData)
		{
			NetworkPlayer newPlayer;

			if (approval._manualPlayerID != NetworkPlayer.unassigned)
			{
				if (!TryAllocatePlayerID(approval._manualPlayerID))
				{
					Log.Error(NetworkLogFlags.Server, "Failed to assign manual PlayerID ", approval._manualPlayerID, " because it is already assigned to another player. Sending uLink.NetworkConnectionError.DetectedDuplicatePlayerID to client");
					_SendConnectDenied(msg.connection, NetworkConnectionError.DetectedDuplicatePlayerID);
					return;
				}

				newPlayer = approval._manualPlayerID;
			}
			else
			{
				newPlayer = AllocatePlayerID();
			}

			Log.Debug(NetworkLogFlags.Server, "Server assigning ", newPlayer, " to ", msg.connection);

			if (newPlayer == NetworkPlayer.unassigned)
			{
				_SendConnectDenied(msg.connection, NetworkConnectionError.TooManyConnectedPlayers);
				return;
			}

			// TODO: is this check really necessary?
			if (_userConnections.ContainsKey(newPlayer))
			{
				Log.Error(NetworkLogFlags.Server, "PlayerID ", newPlayer.id, " is already assigned to another player. Sending uLink.NetworkConnectionError.DetectedDuplicatePlayerID to client");
				_SendConnectDenied(msg.connection, NetworkConnectionError.DetectedDuplicatePlayerID);
				return;
			}

			newPlayer.SetLocalData(approval.localData);

			_AssignConnectionPlayerID(msg.connection, newPlayer);

			var response = new NetworkMessage(this, NetworkMessage.InternalCode.ClientConnectResponse, NetworkPlayer.unassigned);
			response.stream.WriteNetworkPlayer(newPlayer);

			if (ParameterWriter.CanPrepare(approvalData))
			{
				if (!_approvalWriter.IsPreparedFor(approvalData)) _approvalWriter = new ParameterWriter(approvalData);
				_approvalWriter.WritePrepared(response.stream, approvalData);
			}
			else
			{
				ParameterWriter.WriteUnprepared(response.stream, approvalData);
			}

			_ServerSendTo(response, msg.connection);

			// to send the RPC as quickly as possible.
			if (!NetUtility.SafeHeartbeat(_server))
			{
				_NetworkShutdown();
				return;
			}

			foreach (var instance in approval.handoverInstances)
			{
				if (instance._isInstantiatable) instance.InstantiateNow(newPlayer);
			}

			var remainder = msg.stream.GetRemainingBitStream();
			_OnPlayerConnected(newPlayer, msg.connection, remainder);

			if (_GetConnectionSecurityStatus(msg.connection.RemoteEndpoint) == NetworkSecurityStatus.Enabled)
			{
				_Notify("OnSecurityInitialized", newPlayer);
			}
		}

		private void _ApplyRedirectSettings()
		{
			if (_useRedirect)
			{
				// TODO: redirect all open connections
			}
		}

		internal void _ApplyGroupFlags(NetworkGroup group, NetworkGroupFlags newFlags)
		{
			if (isServer)
			{
				var groupData = _groups.GetOrAdd(group, () => new GroupData(group));
				var oldFlags = groupData.flags;

				if ((newFlags & NetworkGroupFlags.AddNewPlayers) != (oldFlags & NetworkGroupFlags.AddNewPlayers))
				{
					if ((newFlags & NetworkGroupFlags.AddNewPlayers) != 0)
					{
						_groupsOnConnected.Add(group, groupData);
					}
					else
					{
						_groupsOnConnected.Remove(group);
					}
				}

				if ((newFlags & NetworkGroupFlags.HideGameObjects) != (oldFlags & NetworkGroupFlags.HideGameObjects))
				{
					if ((newFlags & NetworkGroupFlags.HideGameObjects) == 0)
					{
						Log.Debug(NetworkLogFlags.Group, "Adding ", group, " to non-hidden group buffer list");
						var groupBuffer = _rpcBuffer.GetOrAdd(group, GroupBuffer.Create);
						_groupsNotHidden.Add(group, groupBuffer);

						GroupData data;
						if (_groups.TryGetValue(group, out data) && data.views.Count != 0)
						{
							foreach (var pair in _userConnections)
							{
								var target = pair.Key;
								if (!groupData.users.ContainsKey(target))
								{
									_ServerSendBufferedRPCsTo(target, group);
								}
							}
						}
					}
					else
					{
						Log.Debug(NetworkLogFlags.Group, "Removing ", group, " from non-hidden group buffer list");
						_groupsNotHidden.Remove(group);
						
						GroupData data;
						if (_groups.TryGetValue(group, out data) && data.views.Count != 0)
						{
							foreach (var pair in _userConnections)
							{
								var target = pair.Key;
								if (!groupData.users.ContainsKey(target))
								{
									_ServerSendDestroyInGroup(target, group);
								}
							}
						}
					}
				}

				groupData.flags = newFlags;
			}
			else if (isCellServer)
			{
				// TODO: implement for cell server.
			}
			else if (isClient)
			{
				Log.Warning(NetworkLogFlags.Group, "Group flags will be ignored on the client.");
			}
		}

		internal void _ApplyAllGroupFlags()
		{
			foreach (var pair in NetworkGroup._flags)
			{
				var flags = pair.Value;
				var group = pair.Key;

				if ((flags & NetworkGroupFlags.AddNewPlayers) != 0)
				{
					var groupData = _groups.GetOrAdd(group, () => new GroupData(group));
					_groupsOnConnected.Add(group, groupData);
				}

				if ((flags & NetworkGroupFlags.HideGameObjects) == 0)
				{
					Log.Debug(NetworkLogFlags.Group, "Adding ", group, " to non-hidden group buffer list");
					var groupBuffer = _rpcBuffer.GetOrAdd(group, GroupBuffer.Create);
					_groupsNotHidden.Add(group, groupBuffer);
				}
			}
		}

		public NetworkPlayer AllocatePlayerID()
		{
			_AssertIsServerListening();

			if (_userConnections.Count >= _maxConnections)
			{
				Log.Error(NetworkLogFlags.Server | NetworkLogFlags.PlayerID, "Server can't allocate a new player because max limit is reached: ", _maxConnections);
				return NetworkPlayer.unassigned;
			}

			var newPlayer = _clientAllocator.Allocate();

			Log.Debug(NetworkLogFlags.Server | NetworkLogFlags.PlayerID, "Server allocated new ", newPlayer);
			return newPlayer;
		}

		public bool TryAllocatePlayerID(NetworkPlayer player)
		{
			_AssertIsServerListening();

			if (_userConnections.Count >= _maxConnections)
			{
				Log.Error(NetworkLogFlags.Server | NetworkLogFlags.PlayerID, "Server can't allocate a new player because max limit is reached: ", _maxConnections);
				return false;
			}

			if (!_clientAllocator.TryAllocate(player))
			{
				Log.Debug(NetworkLogFlags.Server | NetworkLogFlags.PlayerID, "Server failed to allocated desired ", player);
				return false;
			}

			Log.Debug(NetworkLogFlags.Server | NetworkLogFlags.PlayerID, "Server allocated desired ", player);
			return true;
		}

		// TODO: this function might not be safe to use so don't use it for the time being.
		public bool DeallocatePlayerID(NetworkPlayer player)
		{
			_AssertIsServerListening();

			if (_ServerIsConnected(player))
			{
				Log.Error(NetworkLogFlags.Server, "Server can't deallocate ", player, " because it's connected.");
				return false;
			}

			_clientAllocator.Deallocate(player, NetworkTime.localTime + _recyclingDelayForPlayerID);
			return true;
		}

		internal void _RPCSecurityResponse(SymmetricKey symKey, NetworkMessage msg)
		{
			var endpoint = msg.connection.RemoteEndpoint;

			if (!msg.isEncryptable)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Server dropping security response because it was not encrypted for endpoint ", endpoint);
				return;
			}

			SecurityLayer security;
			if (!_userSecurity.TryGetValue(endpoint, out security))
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Server dropping security response because it was not requested for endpoint ", endpoint);
				return;
			}

			Log.Debug(NetworkLogFlags.Security, "Server enabling security layer for endpoint ", endpoint);
			security.Enable(symKey);

			if (msg.sender != NetworkPlayer.unassigned)
			{
				_Notify("OnSecurityInitialized", msg.sender);
			}
		}

		internal void _RPCUnsecurityResponse(NetworkMessage msg)
		{
			NetworkEndPoint endpoint = msg.connection.RemoteEndpoint;

			if (!msg.isEncryptable)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Server dropping security response because it was not encrypted for endpoint ", endpoint);
				return;
			}

			SecurityLayer security;
			if (!_userSecurity.TryGetValue(endpoint, out security) || security.status != NetworkSecurityStatus.Disabling)
			{
				Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.Security, "Server dropping unsecurity response because it was not requested for endpoint ", endpoint);
				return;
			}

			Log.Debug(NetworkLogFlags.Security, "Server removing security layer for endpoint ", endpoint);
			_userSecurity.Remove(endpoint);

			if (msg.sender != NetworkPlayer.unassigned)
			{
				_Notify("OnSecurityUninitialized", msg.sender);
			}
		}

		internal void _RPCLicenseCheck(NetworkMessage msg)
		{
			// Do nothing
		}

		public void RegisterHost()
		{
			_PreStart(NetworkStartEvent.MasterServer);

			var data = _MasterGetLocalHostData(true, true);
			if (data == null) return;

			NetworkEndPoint target = Utility.Resolve(_masterIP, _masterPort);

			if (_masterConnection != null && _masterConnection.Status == NetConnectionStatus.Connected && _masterConnection.RemoteEndpoint.Equals(target))
			{
				var msg = new NetworkMasterMessage(NetworkMasterMessage.InternalCode.UpdateHostData);
				msg.stream.WriteLocalHostData(data);

				_server.SendMessage(msg.stream._buffer, _masterConnection, NetChannel.ReliableInOrder1);
			}
			else
			{
				UnregisterHost();

				_masterConnection = _server.Connect(target, _masterPasswordHash.hash, Constants.CONFIG_MASTER_IDENTIFIER);

				_masterPendingRegister = new NetworkMasterMessage(NetworkMasterMessage.InternalCode.RegisterRequest);
				_masterPendingRegister.stream.WriteLocalHostData(data);
			}
		}

		public void UnregisterHost()
		{
			if (_masterConnection != null && _masterConnection.Status != NetConnectionStatus.Disconnected)
			{
				_masterConnection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, 0, true, true);
			}

			_masterRegistered = false;
			_masterPendingRegister = null;
		}

		private void _UpdateMasterHostData(double localTime)
		{
			if (_masterConnection != null && _masterConnection.Status == NetConnectionStatus.Connected)
			{
				if (!Single.IsInfinity(_intervalUpdateHostData) && _nextUpdateHostData <= localTime)
				{
					_nextUpdateHostData = localTime + _intervalUpdateHostData;

					var data = _MasterGetLocalHostData(true, false);

					if (data != null)
					{
						var msg = new NetworkMasterMessage(NetworkMasterMessage.InternalCode.UpdateHostData);
						msg.stream.WriteLocalHostData(data);

						_server.SendMessage(msg.stream._buffer, _masterConnection, NetChannel.ReliableInOrder1);
					}
				}
			}
		}

		private void _ServerUpdateConnectionStats(double localTime)
		{
			var timeSinceLastCalc = localTime - _statsLastCalculation;
			if (timeSinceLastCalc > _statsCalculationInterval)
			{
				foreach (var stat in _userConnStats)
				{
					stat.Value._Update(timeSinceLastCalc);
				}
				_statsLastCalculation = NetworkTime.localTime;
			}
		}

		private void _ServerCheckHandoverTimeouts(double localTime)
		{
			if (_handoverSessions.Count == 0 || _nextHandoverTimeoutCheck > localTime)
			{
				return;
			}

			_nextHandoverTimeoutCheck = localTime + _server.Configuration.HandshakeAttemptRepeatDelay;
			Log.Debug(NetworkLogFlags.Handover, "Checking for handover timeouts in all ", _handoverSessions.Count, " current sessions");

			var toBeRemoved = new List<KeyValuePair<Password, HandoverSession>>();

			foreach (var pair in _handoverSessions)
			{
				if (pair.Value.localTimeout < localTime)
				{
					Log.Warning(NetworkLogFlags.Handover, "Handover session timeout for ", pair.Value.player, " (", pair.Value.clientDebugInfo, ")");
					toBeRemoved.Add(pair);
				}
			}

			if (toBeRemoved.Count != 0)
			{
				if (Log.IsDebugLevel(NetworkLogFlags.Handover))
				{
					string debug = "Dump current connections and status (additional debug info for handover session timeout):\n";
					foreach (var connection in _server.Connections)
					{
						debug += connection.Tag + " (" + connection.RemoteEndpoint + ") " + connection.Status + " \n";
					}

					Log.Debug(NetworkLogFlags.Handover, debug);
				}

				foreach (var pair in toBeRemoved)
				{
					_handoverSessions.Remove(pair.Key);
				}

				foreach (var pair in toBeRemoved)
				{
					_Notify("OnHandoverTimeout", pair.Value.player);
				}
			}
		}

		internal void _HandleMasterMessage(NetBuffer buffer)
		{
			NetworkMasterMessage msg;

			try
			{
				msg = new NetworkMasterMessage(buffer);
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.MasterServer, "Failed to parse a incoming message from master server: ", e.Message);
				return;
			}

			Log.Debug(NetworkLogFlags.MasterServer, "Received message from master server: ", msg);

			msg.Execute(this as NetworkBase);
		}

		internal void _MasterRPCRegisterResponse(NetworkEndPoint externalEndpoint)
		{
			_masterRegistered = true;
			_masterExternalEndpoint = externalEndpoint;

			_MasterNotifyEvent(MasterServerEvent.RegistrationSucceeded);
		}

		internal void _MasterRPCRegisterFailed(int errorCode)
		{
			_MasterNotifyEvent((MasterServerEvent)errorCode);
		}

		internal void _MasterRPCProxyClient(NetworkEndPoint client, Password password, ushort sessionPort, Password sessionPassword)
		{
			Log.Info(NetworkLogFlags.MasterServer | NetworkLogFlags.Server, "Received proxy client ", client, password.isEmpty ? " without password" : " with password", " on session port ", sessionPort);

			if (!_serverPasswordHash.isEmpty && password != _serverPasswordHash) return; // TODO: should return InvalidPassword

			if (_userConnections.Count >= _limitedConnections) return; // TODO: tell the master server that the proxy session failed.

			var proxyClient = _server.Connect(new NetworkEndPoint(_masterConnection.RemoteEndpoint.ipAddress, sessionPort), sessionPassword.hash);
			_UnassignConnectionPlayerID(proxyClient);
		}

		internal abstract LocalHostData _MasterGetLocalHostData(bool errorCheck, bool notifyOnError);
		internal abstract void _MasterNotifyEvent(MasterServerEvent eventCode);

		internal void _UnconnectedRPCDiscoverHostRequest(HostDataFilter filter, double remoteTime, NetworkEndPoint endpoint)
		{
			var data = _MasterGetLocalHostData(true, false);
			if (data == null) return;

			if (filter.Match(data))
			{
				var msg = new UnconnectedMessage(UnconnectedMessage.InternalCode.DiscoverHostResponse);
				msg.stream.WriteLocalHostData(data);
				msg.stream.WriteDouble(remoteTime);
				msg.stream.WriteEndPoint(endpoint);
				_UnconnectedRPC(msg, endpoint);
			}
		}

		internal void _UnconnectedRPCKnownHostRequest(double remoteTime, bool forceResponse, NetworkEndPoint endpoint)
		{
			var data = _MasterGetLocalHostData(!forceResponse, false);
			if (data == null) return;

			var msg = new UnconnectedMessage(UnconnectedMessage.InternalCode.KnownHostResponse);
			msg.stream.WriteLocalHostData(data);
			msg.stream.WriteDouble(remoteTime);
			msg.stream.WriteEndPoint(endpoint);
			_UnconnectedRPC(msg, endpoint);
		}

		internal void _UnconnectedRPCLicenseRequest(LocalHostData target, bool shutdown, byte[] signature, NetworkEndPoint endpoint)
		{
			// Do nothing
		}

		internal void _UnconnectedRPCPreConnectRequest(NetworkEndPoint endpoint)
		{
			if (isServer && status == NetworkStatus.Connected)
			{
				_UnconnectedRPC(new UnconnectedMessage(UnconnectedMessage.InternalCode.PreConnectResponse), endpoint);
			}
		}

		private void _UnconnectedRPC(UnconnectedMessage msg, NetworkEndPoint target)
		{
			if (_server != null && _server.IsListening)
			{
				Log.Debug(NetworkLogFlags.RPC, "Server is sending unconnected RPC ", msg.internCode, " to ", target);

				_server.SendOutOfBandMessage(msg.stream._buffer, target);
			}
		}
	}
}
