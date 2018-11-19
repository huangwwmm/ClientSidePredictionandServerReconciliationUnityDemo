#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision$
// $LastChangedBy$
// $LastChangedDate$
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Reflection;
using System.Text;
using System.Net;
using System.Collections.Generic;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

// TODO: add multiple my peers by getting local ip and internet ip

namespace uLink
{
#if UNITY_BUILD
	using P2P = NetworkP2P;
#else
	using P2P = NetworkP2PBase;
#endif

	/// <summary>
	/// For setting <see cref="uLink.NetworkP2P.Handover"/> mode.
	/// </summary>
	[Flags]
	public enum NetworkP2PHandoverFlags : byte
	{
		/// <summary>
		/// This is the normal mode which destroys the object in its current node and instantiates it in the other node and also redirects its owner to it.
		/// </summary>
		Normal,

		/// <summary>
		/// If set, the object in the original/current node is not destroyed.
		/// </summary>
		DontDestroyOriginal,

		/// <summary>
		/// If set, the owner <see cref="uLink.NetworkPlayer"/> will not be redirected to the new node.
		/// </summary>
		DontRedirectOwner
	}

	/// <summary>
	/// Abstract base class for the class <see cref="uLink.NetworkP2P"/>. 
	/// </summary>
	public abstract class NetworkP2PBase
#if UNITY_BUILD
 : MonoBehaviour
#endif
	{
		private struct PendingHandover
		{
			public readonly NetworkPlayer owner;
			public readonly NetworkViewBase[] instances;

			public PendingHandover(NetworkPlayer owner, NetworkP2PHandoverInstance[] instances)
			{
				this.owner = owner;

				if (instances != null)
				{
					this.instances = new NetworkViewBase[instances.Length];

					for (int i = 0; i < instances.Length; i++)
						this.instances[i] = instances[i]._networkView;
				}
				else
				{
					this.instances = null;
				}
			}
		}

		private class PendingHandovers
		{
			private uint _lastID = 0;
			private readonly Dictionary<uint, PendingHandover> _handovers = new Dictionary<uint, PendingHandover>();

			public uint Add(PendingHandover handover)
			{
				_handovers[++_lastID] = handover;
				return _lastID;
			}

			public bool Remove(uint handoverID, out PendingHandover handover)
			{
				if (_handovers.TryGetValue(handoverID, out handover))
				{
					_handovers.Remove(handoverID);
					return true;
				}

				return false;
			}
		}

#if UNITY_BUILD
		[SerializeField]
		[Obfuscation(Feature = "renaming")]
#endif
		private string _peerType = String.Empty;

#if UNITY_BUILD
		[SerializeField]
		[Obfuscation(Feature = "renaming")]
#endif
		private string _peerName = String.Empty;

		[NonSerialized]
		private bool _peerTypeOrNameIsDirty = true;

		/// <summary>
		/// The comment field for this NetworkP2P,
		/// This field can be used for anything you want and is returned in the peer info when you use
		/// <see cref="uLink.NetworkP2PBase.DiscoverLocalPeers"/>
		/// </summary>
		[SerializeField]
		public string comment = String.Empty;

		[NonSerialized]
		private double _timeOfDiscoveryRequest = 0;
		[NonSerialized]
		private double _timeOfKnownPeersRequest = 0;

		[NonSerialized]
		private readonly Dictionary<NetworkEndPoint, PeerData> _discoveredPeers = new Dictionary<NetworkEndPoint, PeerData>();
		[NonSerialized]
		private readonly Dictionary<NetworkEndPoint, PeerData> _knownPeers = new Dictionary<NetworkEndPoint, PeerData>();

		private const NetworkFlags INTERNAL_FLAGS = NetworkFlags.TypeUnsafe | NetworkFlags.Unbuffered | NetworkFlags.Unencrypted | NetworkFlags.NoTimestamp;

		[NonSerialized]
		private NetworkFlags _defaultFlags = INTERNAL_FLAGS;

		/// <summary>
		/// Set this string to use a password check for incoming P2P connections. 
		/// </summary>
		/// <remarks>
		/// Default value for incomingPassword is an empty string. Then there is no password check when som other peer
		/// tries to connect to this peer. But if this string is set, the password must be provided in 
		/// the connection request to this peer.</remarks>
		[SerializeField]
		public string incomingPassword = String.Empty;

		[NonSerialized]
		internal bool _batchSendAtEndOfFrame = true;

#if UNITY_BUILD
		internal NetworkBase _network { get { return Network._singleton; } }
#elif TEST_BUILD
		internal readonly NetworkBase _network;
#endif

		[NonSerialized]
		internal NetPeer _peer;
		[NonSerialized]
		private NetBuffer _readBuffer;

		[NonSerialized]
		private readonly Dictionary<NetworkEndPoint, NetworkPeer> _connectedPeers = new Dictionary<NetworkEndPoint, NetworkPeer>();
		//[NonSerialized]
		//private readonly Dictionary<int, NetworkP2PHandoverInstance[]> _pendingHandovers = new Dictionary<int, NetworkP2PHandoverInstance[]>();
		[NonSerialized]
		private readonly ParameterWriterCache _rpcWriter = new ParameterWriterCache(false);
		[NonSerialized]
		private NetConnectionStatus _lastConnectionStatus = NetConnectionStatus.Disconnected;

		/// <summary>
		/// Gets the last returned <see cref="uLink.NetworkConnectionError"/>.
		/// </summary>
#if UNITY_BUILD
		[NonSerialized]
#endif
		public NetworkConnectionError lastError = NetworkConnectionError.NoError;

		[NonSerialized]
		private readonly NetworkP2PConfig _config;


#if TEST_BUILD
		internal NetworkP2PBase(NetworkBase network)
#else
		/// <summary>
		/// Creates a new instance of the class.
		/// </summary>
		public NetworkP2PBase()
#endif
		{
			_config = new NetworkP2PConfig(this);
#if TEST_BUILD
			_network = network;
#endif
		}

		/// <summary>
		/// The type of this peer, This is mainly used when using the discovery feature of 
		/// the <see cref="uLink.NetworkP2P"/> class to find the compatible peers. 
		/// When you try to discover peers on the network, peers with the same type will be returned.
		/// </summary>
		public string peerType { get { return _peerType; } set { _peerType = value; _peerTypeOrNameIsDirty = true; } }
		/// <summary>
		/// Name of this peer/node. Can be used for any purpose. This is accessible in the list of discovered peers.
		/// </summary>
		public string peerName { get { return _peerName; } set { _peerName = value; _peerTypeOrNameIsDirty = true; } }

		/// <summary>
		/// Whether RPC:s sent over this connection should be type-safe or not by default.
		/// </summary>
		public bool isTypeSafeByDefault
		{
			get { return (_defaultFlags & NetworkFlags.TypeUnsafe) == 0; }
			set { if (value) _defaultFlags &= ~NetworkFlags.TypeUnsafe; else _defaultFlags |= NetworkFlags.TypeUnsafe; }
		}

		/// <summary>
		/// Whether this P2P node is listening for incoming connections or not.
		/// </summary>
		public bool isListening { get { return (_peer != null); } }

		/// <summary>
		/// Returns the port that we are listening to.
		/// If we are not listening to any ports, -1 will be returned.
		/// </summary>
		public int listenPort { get { return isListening ? _peer.ListenPort : -1; } }

		/// <summary>
		/// Maximum number of connections which this peer can have.
		/// </summary>
		public int maxConnections
		{
			get { return isListening ? _peer.Configuration.MaxConnections : 0; }
			set { if (isListening) _peer.Configuration.MaxConnections = value; }
		}

		/// <summary>
		/// Currently connected remote peers.
		/// </summary>
		public NetworkPeer[] connections
		{
			get
			{
				return Utility.ToArray(_connectedPeers.Values);
			}
		}

		/// <summary>
		/// All current remote peers whether connected, connecting, disconnecting or recently disconnected.
		/// </summary>
		public KeyValuePair<NetworkPeer, NetworkStatus>[] allConnections
		{
			get
			{
				if (_peer == null) return new KeyValuePair<NetworkPeer, NetworkStatus>[0];

				var connections = _peer.Connections;
				var result = new KeyValuePair<NetworkPeer, NetworkStatus>[connections.Count];
				int i = 0;

				foreach (var connection in connections)
				{
					var status = _ConvertLidgrenStatus(connection.Status);
					result[i++] = new KeyValuePair<NetworkPeer, NetworkStatus>(new NetworkPeer(connection.RemoteEndpoint), status);
				}

				return result;
			}
		}

		/// <summary>
		/// The <see cref="uLink.NetworkP2PConfig"/> instance which can be used to configure this peer's low level settings.
		/// Be careful, unlike the static <see cref="uLink.Network.config"/> this is an instance property and can
		/// be accessed using a reference to a NetworkP2P object.
		/// </summary>
		public NetworkP2PConfig config
		{
			get
			{
				return _config;
			}
		}

		/*
		internal NetworkP2PBase(NetworkBaseServer network)
		{
			_network = network;
		}
		*/

		protected abstract bool OnRPC(string name, BitStream stream, NetworkP2PMessageInfo info);
		protected abstract void OnEvent(string name, object value);

		/// <summary>
		/// Transforms the provided position and rotation from world space to local space.
		/// </summary>
		/// <param name="pos">The position to apply the transformation on.</param>
		/// <param name="rot">The rotation to apply the transformation on.</param>
		public virtual void InverseTransform(ref Vector3 pos, ref Quaternion rot) { }

		/// <summary>
		/// Transforms a position and rotation from local space to world space.
		/// </summary>
		/// <param name="pos">The position to apply the transformation on.</param>
		/// <param name="rot">The rotation to apply the transformation on.</param>
		public virtual void Transform(ref Vector3 pos, ref Quaternion rot) { }

		/// <summary>
		/// Gets the <see cref="uLink.NetworkStatus"/> of a remote node connected to this peer.
		/// </summary>
		public NetworkStatus GetStatus(NetworkPeer target)
		{
			if (!isListening) return NetworkStatus.Disconnected;

			var connection = _peer.GetConnection(target.endpoint);
			return connection != null ? _ConvertLidgrenStatus(connection.Status) : NetworkStatus.Disconnected;
		}

		/// <summary>
		/// Gets the average ping time between the target peer and ourself.
		/// </summary>
		/// <param name="target">The peer that we want to know the average ping between it and ourself.</param>
		/// <returns></returns>
		public int GetAveragePing(NetworkPeer target)
		{
			if (isListening)
			{
				var connection = _peer.GetConnection(target.endpoint);
				if (connection != null) return (int)(connection.AverageRoundtripTime * 1000);
			}

			return -1;
		}

		/// <summary>
		/// Gets the last ping between us and the target.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public int GetLastPing(NetworkPeer target)
		{
			if (isListening)
			{
				var connection = _peer.GetConnection(target.endpoint);
				if (connection != null) return (int)(connection.LastRoundtripTime * 1000);
			}

			return -1;
		}

		/// <summary>
		/// Gets the <see cref="uLink.NetworkStatistics"/> for a remote node connected to this peer, which can be used to get connection statistics, bandwidth, packet counts etc.
		/// </summary>
		public NetworkStatistics GetStatistics(NetworkPeer target)
		{
			if (!isListening) return null;

			var connection = _peer.GetConnection(target.endpoint);
			return connection != null ? new NetworkStatistics(connection) : null;
		}

		private NetworkStatus _ConvertLidgrenStatus(NetConnectionStatus status)
		{
			switch (status)
			{
				case NetConnectionStatus.Connected: return NetworkStatus.Connected;
				case NetConnectionStatus.Connecting:
				case NetConnectionStatus.Reconnecting: return NetworkStatus.Connecting;
				case NetConnectionStatus.Disconnecting: return NetworkStatus.Disconnecting;
				default: return NetworkStatus.Disconnected;
			}
		}

		/// <summary>
		/// Tries to connect to another peer.
		/// </summary>
		/// <param name="target">The peer that we want to connect to.</param>
		/// <param name="incomingPassword">The password that the other peer expect us to send.</param>
		/// <remarks>It's only required to use this overload with password if <see cref="uLink.NetworkP2P.incommingPassword"/> is set in the target peer.</remarks>
		public void Connect(NetworkPeer target, string incomingPassword)
		{
			_AssertIsListening();

			Log.Info(NetworkLogFlags.P2P, "P2P connecting to ", target);

			if (String.IsNullOrEmpty(incomingPassword))
				_peer.Connect(target.endpoint);
			else
				_peer.Connect(target.endpoint, Encoding.UTF8.GetBytes(incomingPassword));
		}

		/// <summary>
		/// Closes the connection to a remote node.
		/// </summary>
		public void CloseConnection(NetworkPeer target, bool sendDisconnectionNotification)
		{
			CloseConnection(target, sendDisconnectionNotification, Constants.DEFAULT_DICONNECT_TIMEOUT);
		}

		/// <summary>
		/// Closes the connection to a remote node after the given timeout has expired
		/// </summary>
		public void CloseConnection(NetworkPeer target, bool sendDisconnectionNotification, int timeout)
		{
			_AssertIsListening();

			Log.Info(NetworkLogFlags.P2P, "P2P disconnecting from ", target);

			NetConnection connection = _peer.GetConnection(target.endpoint);

			connection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, timeout / 1000.0f, sendDisconnectionNotification, (timeout == 0));
		}

		protected void _Open(int listenPort, int maxConnections)
		{
			if (isListening || listenPort < 0) return;

			if (maxConnections <= 0)
			{
				Log.Error(NetworkLogFlags.P2P, "Can't open ", this, " with invalid max connections ", maxConnections);
				return;
			}

			var config = new NetConfiguration(Constants.CONFIG_P2P_IDENTIFIER);
			config.MaxConnections = maxConnections;
			config.StartPort = listenPort;
			config.EndPort = listenPort;
			config.AnswerDiscoveryRequests = false;
			config.SendBufferSize = Constants.DEFAULT_SEND_BUFFER_SIZE;
			config.ReceiveBufferSize = Constants.DEFAULT_RECV_BUFFER_SIZE;

			_peer = new NetPeer(config);
			_Configure(_peer);

			try
			{
				_peer.Start();
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.P2P, "Failed to open ", this, ": ", e.Message);

				_peer = null;
				return;
			}

			Log.Info(NetworkLogFlags.P2P, "Opened ", this, " on port ", listenPort);

			_readBuffer = _peer.CreateBuffer();

			OnEvent("OnPeerInitialized", null);
		}

		private void _Configure(NetBase net)
		{
			net.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, true);
			net.SetMessageTypeEnabled(NetMessageType.ConnectionRejected, true);

#if DEBUG // TODO: fix this
			net.SetMessageTypeEnabled(NetMessageType.BadMessageReceived, true);
			net.SetMessageTypeEnabled(NetMessageType.DebugMessage, true);
			net.SetMessageTypeEnabled(NetMessageType.VerboseDebugMessage, true);
#endif

			_config._Apply(net);
		}

		protected void _Close()
		{
			if (!isListening) return;

			foreach (var connection in _peer.Connections)
			{
				Log.Debug(NetworkLogFlags.P2P, "P2P disconnect ", connection);
				connection.Disconnect(Constants.REASON_NORMAL_DISCONNECT, 0, true, true);
			}

			// TODO: do we need to call OnPeerDisconnected on each connection or will this happen in LidgrenStatusChanged?

			// TODO: should we really just null everything instantly? This will disable any events like OnPeerDisconnected
			_peer.Dispose();
			_peer = null;
			_readBuffer = null;
		}

		protected void _Update()
		{
			if (!isListening)
			{
#if UNITY_BUILD
				enabled = false;
#endif
				return;
			}

			_CheckMessages();

			if (!NetUtility.SafeHeartbeat(_peer))
			{
				_Close(); // TODO: maybe we need to call something that also notifies the application-layer?
				return;
			}
		}

		private void _CheckMessages()
		{
			NetMessageType type;
			NetConnection connection;
			NetworkEndPoint endpoint;
			NetChannel channel;

			while (_peer != null && _peer.ReadMessage(_readBuffer, out type, out connection, out endpoint, out channel))
			{
				switch (type)
				{
					case NetMessageType.Data:
						_OnLidgrenMessage(_readBuffer, connection, channel);
						break;

					case NetMessageType.OutOfBandData:
						_OnLidgrenOutOfBandMessage(_readBuffer, endpoint);
						break;

					case NetMessageType.StatusChanged:
						_OnLidgrenStatusChanged(_readBuffer.ReadString(), connection);
						break;

					case NetMessageType.ConnectionApproval:
						_OnLidgrenConnectionApproval(connection);
						break;

					case NetMessageType.ConnectionRejected:
						_OnLidgrenConnectionRejected(connection, endpoint, _readBuffer.ReadString());
						break;

					case NetMessageType.DebugMessage:
					case NetMessageType.VerboseDebugMessage:
						Log.Debug(NetworkLogFlags.BadMessage, "P2P debug message: ", _readBuffer.ReadString()); // TODO: when is this called
						break;

					case NetMessageType.BadMessageReceived:
						Log.Warning(NetworkLogFlags.BadMessage, "P2P received bad message: ", _readBuffer.ReadString());
						break;
				}
			}
		}

		private void _OnLidgrenMessage(NetBuffer buffer, NetConnection connection, NetChannel channel)
		{
			Log.Debug(NetworkLogFlags.RPC, "P2P message received from ", connection);

			NetworkP2PMessage msg;

			try
			{
				msg = new NetworkP2PMessage(buffer, connection, channel);
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.BadMessage, "P2P Failed to parse a incoming message: ", e.Message);
				return;
			}

			_ExecuteRPC(msg);
		}

		private void _OnLidgrenOutOfBandMessage(NetBuffer buffer, NetworkEndPoint endpoint)
		{
			Log.Debug(NetworkLogFlags.RPC, "P2P message received from ", endpoint);

			UnconnectedP2PMessage msg;

			try
			{
				msg = new UnconnectedP2PMessage(buffer, endpoint);
			}
			catch (Exception e)
			{
				Log.Error(NetworkLogFlags.BadMessage, "P2P Failed to parse a incoming message: ", e.Message);
				return;
			}

			msg.Execute(this);
		}

		private void _OnLidgrenStatusChanged(string reason, NetConnection connection)
		{
			Log.Info(NetworkLogFlags.P2P, "P2P ", connection, " has changed status to ", connection.Status);

			if (connection.Status == NetConnectionStatus.Connected)
			{
				_OnPeerConnected(connection);
			}
			else if (_lastConnectionStatus == NetConnectionStatus.Connecting && connection.Status == NetConnectionStatus.Disconnected)
			{
				_FailConnection(NetworkConnectionError.ConnectionTimeout);
			}
			else if (connection.Status == NetConnectionStatus.Disconnected || connection.Status == NetConnectionStatus.Disconnecting)
			{
				_OnPeerDisconnected(reason, connection);
			}
			_lastConnectionStatus = connection.Status;
		}

		private void _OnPeerConnected(NetConnection connection)
		{
			if (_connectedPeers.ContainsKey(connection.RemoteEndpoint)) return;

			var peer = new NetworkPeer(connection.RemoteEndpoint);
			_connectedPeers.Add(connection.RemoteEndpoint, peer);

			OnEvent("OnPeerConnected", peer);
		}

		private void _OnPeerDisconnected(string reason, NetConnection connection)
		{
			if (!_connectedPeers.Remove(connection.RemoteEndpoint)) return;

			Log.Info(NetworkLogFlags.P2P, "P2P ", connection, " was disconnected because ", reason);

			var peer = new NetworkPeer(connection.RemoteEndpoint);
			OnEvent("OnPeerDisconnected", peer);
		}

		private void _OnLidgrenConnectionApproval(NetConnection connection)
		{
			string senderPassword = (connection.RemoteHailData != null) ? Encoding.UTF8.GetString(connection.RemoteHailData) : String.Empty;

			if (String.IsNullOrEmpty(incomingPassword) || senderPassword == incomingPassword)
			{
				Log.Debug(NetworkLogFlags.P2P, "P2P incoming ", connection, " was approved");

				try
				{
					connection.Approve();
				}
				catch (ArgumentException e)
				{
					// TODO: should this be handled in a better way?
					if (e.Message != "An element with the same key already exists in the dictionary.")
					{
						throw;
					}
				}
			}
			else
			{
				Log.Debug(NetworkLogFlags.P2P, "P2P incoming ", connection, " was disapproved because of invalid password");
				connection.Disapprove(Constants.REASON_INVALID_PASSWORD);
			}
		}

		private void _OnLidgrenConnectionRejected(NetConnection connection, NetworkEndPoint endpoint, string reason)
		{
			Log.Debug(NetworkLogFlags.P2P, "P2P ", connection, " was rejected because: ", reason);

			switch (reason)
			{
				case Constants.REASON_TOO_MANY_PLAYERS:
					_FailConnection(NetworkConnectionError.TooManyConnectedPlayers);
					break;

				case Constants.REASON_INVALID_PASSWORD:
					_FailConnection(NetworkConnectionError.InvalidPassword);
					break;

				case Constants.REASON_CONNECTION_BANNED: // TODO: this need so be implemented in UnityLink
					_FailConnection(NetworkConnectionError.ConnectionBanned);
					break;

				case Constants.REASON_LIMITED_PLAYERS:
					_FailConnection(NetworkConnectionError.LimitedPlayers);
					break;

				case Constants.REASON_BAD_APP_ID:
					_FailConnection(NetworkConnectionError.IncompatibleVersions);
					break;

				case Constants.NOTIFY_MAX_CONNECTIONS:
					Log.Warning(NetworkLogFlags.P2P, "P2P incoming connection from ", endpoint, " was rejected due to too many connections");
					break;

				case Constants.NOTIFY_CONNECT_TO_SELF:
					Log.Warning(NetworkLogFlags.P2P, "P2P isn't allowed to connect to self");
					break;

				case Constants.NOTIFY_LIMITED_PLAYERS:
					Log.Warning(NetworkLogFlags.P2P, "P2P incoming connection from ", endpoint, " was rejected due to limited connections");
					break;

				case Constants.NOTIFY_BAD_APP_ID:
					Log.Warning(NetworkLogFlags.Server, "Incoming connection from ", endpoint, " was rejected due to incompatible uLink version");
					break;

				default:
					_FailConnection(NetworkConnectionError.ConnectionFailed);
					break;
			}
		}

		private void _FailConnection(NetworkConnectionError error)
		{
			lastError = error;

			// TODO: add NetworkPeer so we know which connect failed
			OnEvent("OnFailedToConnectToPeer", error);
		}

		private void _ExecuteRPC(NetworkP2PMessage msg)
		{
			Log.Debug(NetworkLogFlags.RPC, "P2P executing RPC ", msg);

			if (!msg.isInternal)
			{
				if (String.IsNullOrEmpty(msg.name))
				{
					Log.Debug(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, "P2P dropped unnamed RPC ", msg);
					return;
				}

				OnRPC(msg.name, msg.stream, new NetworkP2PMessageInfo(msg, this));
			}
			else
			{
				msg.ExecuteInternal(this);
			}
		}

		/// <summary>
		/// Sends an unreliable RPC to another peer.
		/// </summary>
		/// <param name="rpcName">Name of the RPC to call.</param>
		/// <param name="target">The peer that we want to send the RPC to.</param>
		/// <param name="args">The arguments that we want to send to the RPC.</param>
		public void UnreliableRPC(string rpcName, NetworkPeer target, params object[] args) { RPC(_defaultFlags | NetworkFlags.Unreliable, rpcName, target, args); }

		/// <summary>
		/// Sends an unreliable RPC to a set of peers.
		/// </summary>
		/// <param name="rpcName">Name of the RPC to send.</param>
		/// <param name="targets">The peers that we want to send the RPC to.</param>
		/// <param name="args">The arguments that we want to send to the RPC.</param>
		public void UnreliableRPC(string rpcName, IEnumerable<NetworkPeer> targets, params object[] args) { RPC(_defaultFlags | NetworkFlags.Unreliable, rpcName, targets, args); }

		/// <summary>
		/// Sends an unreliable RPC to peers dictated by <c>mode</c> parameter.
		/// </summary>
		/// <param name="rpcName">Name of the RPC to call.</param>
		/// <param name="mode">This parameter dictates who will receive the RPC.</param>
		/// <param name="args">The arguments to send to the RPC</param>
		public void UnreliableRPC(string rpcName, PeerMode mode, params object[] args) { RPC(_defaultFlags | NetworkFlags.Unreliable, rpcName, mode, args); }

		///<overloads>Sends an RPC from this P2P connection</overloads>
		/// <summary>
		/// Sends a reliable RPC to a remote node.
		/// </summary>
		public void RPC(string rpcName, NetworkPeer target, params object[] args) { RPC(_defaultFlags, rpcName, target, args); }

		/// <summary>
		/// Sends a reliable RPC to a set of peers.
		/// </summary>
		/// <param name="rpcName">The name of the RPC that we want to call.</param>
		/// <param name="targets">The peers that we want to send the RPC to them.</param>
		/// <param name="args">The arguments that we want to send to the RPC.</param>
		public void RPC(string rpcName, IEnumerable<NetworkPeer> targets, params object[] args) { RPC(_defaultFlags, rpcName, targets, args); }

		/// <summary>
		/// Sends a reliable RPC to remote node(s) according to the specified PeerMode.
		/// </summary>
		public void RPC(string rpcName, PeerMode mode, params object[] args) { RPC(_defaultFlags, rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC to one remote node.
		/// </summary>
		/// <param name="flags">This parameter dictates the properties of the RPC like reliability, security ...</param>
		/// <param name="rpcName">Name of the RPC to call.</param>
		/// <param name="target">The peer that we want to send the RPC to.</param>
		/// <param name="args">The arguments that we want to send to the RPC.</param>
		public void RPC(NetworkFlags flags, string rpcName, NetworkPeer target, params object[] args)
		{
			_AssertIsListening();

			var msg = new NetworkP2PMessage(flags, rpcName, NetworkP2PMessage.InternalCode.None);
			_rpcWriter.Write(msg.stream, rpcName, args);

			NetConnection connection = _peer.GetConnection(target.endpoint);

			if (connection != null)
			{
				_peer.SendMessage(msg.stream._buffer, connection, msg.channel);
			}
			else
			{
				// TODO: log error
				return;
			}

			if (!_batchSendAtEndOfFrame)
			{
				Log.Debug(NetworkLogFlags.P2P | NetworkLogFlags.RPC, "Force send message directly, instead of at end of frame");

				connection.SendUnsentMessages(NetTime.Now);
			}
		}

		/// <summary>
		/// Sends an RPC with properties dictated by <c>flags</c> parameter to a set of peers.
		/// </summary>
		/// <param name="flags">This parameter determines properties of the RPC like reliability, security and ...</param>
		/// <param name="rpcName">Name of the RPC to call.</param>
		/// <param name="targets">The set of peers that we want to send the RPC to them.</param>
		/// <param name="args">The arguments that we want to send to the RPC.</param>
		public void RPC(NetworkFlags flags, string rpcName, IEnumerable<NetworkPeer> targets, params object[] args)
		{
			_AssertIsListening();

			var msg = new NetworkP2PMessage(flags, rpcName, NetworkP2PMessage.InternalCode.None);
			_rpcWriter.Write(msg.stream, rpcName, args);

			foreach (var target in targets)
			{
				NetConnection connection = _peer.GetConnection(target.endpoint);

				if (connection != null)
				{
					_peer.SendMessage(msg.stream._buffer, connection, msg.channel);
				}
				else
				{
					// TODO: log error
					continue;
				}

				if (!_batchSendAtEndOfFrame)
				{
					Log.Debug(NetworkLogFlags.P2P | NetworkLogFlags.RPC, "Force send message directly, instead of at end of frame");

					connection.SendUnsentMessages(NetTime.Now);
				}
			}
		}

		/// <summary>
		/// Send an RPC to remote node(s) according to the specified <see cref="uLink.PeerMode"/> and with properties dictated by <c>flags</c> parameter..
		/// </summary>
		/// <param name="flags">This parameter dictates the properties of the RPC like reliability, security and ...</param>
		/// <param name="rpcName">Name of the RPC to call.</param>
		/// <param name="mode">This parameter dictates who will receive the RPC.</param>
		/// <param name="args">The arguments that we want to send to the RPC</param>
		public void RPC(NetworkFlags flags, string rpcName, PeerMode mode, params object[] args)
		{
			_AssertIsListening();

			var msg = new NetworkP2PMessage(flags, rpcName, NetworkP2PMessage.InternalCode.None);
			_rpcWriter.Write(msg.stream, rpcName, args);

			_peer.SendToAll(msg.GetSendBuffer(), msg.channel);

			if (mode == PeerMode.All)
			{
				msg.stream._isWriting = false;
				_ExecuteRPC(msg);
			}
		}

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(System.Type,System.String,uLink.RPCMode,System.Object[])"/></remarks>
		public void RPC(Type type, string rpcName, PeerMode mode, params object[] args) { RPC(type.Name + ':' + rpcName, mode, args); }
		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(System.Type,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(Type type, string rpcName, NetworkPeer target, params object[] args) { RPC(type.Name + ':' + rpcName, target, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(System.Type,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(Type type, string rpcName, IEnumerable<NetworkPeer> targets, params object[] args) { RPC(type.Name + ':' + rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(System.Type,uLink.NetworkFlags,System.String,uLink.RPCMode,System.Object[])"/></remarks>
		public void RPC(Type type, NetworkFlags flags, string rpcName, PeerMode mode, params object[] args) { RPC(flags, type.Name + ':' + rpcName, mode, args); }
		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(System.Type,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(Type type, NetworkFlags flags, string rpcName, NetworkPeer target, params object[] args) { RPC(flags, type.Name + ':' + rpcName, target, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(System.Type,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(Type type, NetworkFlags flags, string rpcName, IEnumerable<NetworkPeer> targets, params object[] args) { RPC(flags, type.Name + ':' + rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(UnityEngine.MonoBehaviour,System.String,uLink.RPCMode,System.Object[])"/></remarks>
		public void RPC(UnityEngine.MonoBehaviour type, string rpcName, PeerMode mode, params object[] args) { RPC(type.GetType(), rpcName, mode, args); }
		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(UnityEngine.MonoBehaviour,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(UnityEngine.MonoBehaviour type, string rpcName, NetworkPeer target, params object[] args) { RPC(type.GetType(), rpcName, target, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(UnityEngine.MonoBehaviour,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(UnityEngine.MonoBehaviour type, string rpcName, IEnumerable<NetworkPeer> targets, params object[] args) { RPC(type.GetType(), rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(UnityEngine.MonoBehaviour,System.String,uLink.RPCMode,System.Object[])"/></remarks>
		public void RPC(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, PeerMode mode, params object[] args) { RPC(type.GetType(), flags, rpcName, mode, args); }
		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(UnityEngine.MonoBehaviour,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, NetworkPeer target, params object[] args) { RPC(type.GetType(), flags, rpcName, target, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <remarks>Works similar to <see cref="uLink.NetworkView.RPC(UnityEngine.MonoBehaviour,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public void RPC(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, IEnumerable<NetworkPeer> targets, params object[] args) { RPC(type.GetType(), flags, rpcName, targets, args); }

		[Obsolete("NetworkP2P.Replicate is deprecated, please use NetworkP2P.Handover (with NetworkP2PHandoverFlags.DontRedirectOwner and/or .DontDestroyOriginal) instead.")]
		public void Replicate(NetworkViewBase netView, NetworkPeer target)
		{
			Handover(netView, target, NetworkP2PHandoverFlags.DontDestroyOriginal | NetworkP2PHandoverFlags.DontRedirectOwner);
		}

		[Obsolete("NetworkP2P.Replicate is deprecated, please use NetworkP2P.Handover (with NetworkP2PHandoverFlags.DontRedirectOwner and/or .DontDestroyOriginal) instead.")]
		public void Replicate(NetworkViewBase netView, NetworkPeer target, Vector3 relativePos, Quaternion relativeRot)
		{
			Handover(netView, target, relativePos, relativeRot, NetworkP2PHandoverFlags.DontDestroyOriginal | NetworkP2PHandoverFlags.DontRedirectOwner);
		}

		[Obsolete("NetworkP2P.Handover argument keepPlayerID is deprecated, please call Handover without it.")]
		public void Handover(NetworkViewBase netView, NetworkPeer target, bool keepPlayerID) { Handover(netView, target); }

		[Obsolete("NetworkP2P.Handover argument keepPlayerID is deprecated, please call Handover without it.")]
		public void Handover(NetworkViewBase netView, NetworkPeer target, bool keepPlayerID, Vector3 relativePos, Quaternion relativeRot) { Handover(netView, target, relativePos, relativeRot); }

		/// <summary>
		/// Hands over all objects owned by a player to another peer.
		/// </summary>
		/// <param name="owner">The <see cref="uLink.NetworkPlayer"/> that you want to move its objects.</param>
		/// <param name="target">The peer that you want to move objects to it.</param>
		public void HandoverPlayerObjects(NetworkPlayer owner, NetworkPeer target)
		{
			HandoverPlayerObjects(owner, target, Vector3.zero, Quaternion.identity, NetworkP2PHandoverFlags.Normal);
		}

		/// <summary>
		/// Hands over all objects owned by a player to another peer and transforms all of them by the provided offset.
		/// </summary>
		/// <param name="owner">The <see cref="uLink.NetworkPlayer"/> that you want to move its objects.</param>
		/// <param name="target">The peer that you want to move objects to it.</param>
		/// <param name="offsetPos">Offset applied to positions of objects.</param>
		/// <param name="offsetRot">Offset applied to rotation of objects.</param>
		public void HandoverPlayerObjects(NetworkPlayer owner, NetworkPeer target, Vector3 offsetPos, Quaternion offsetRot)
		{
			HandoverPlayerObjects(owner, target, offsetPos, offsetRot, NetworkP2PHandoverFlags.Normal);
		}

		/// <summary>/// <summary>
		/// Hands over all objects owned by a player to another peer and transforms all of them by the provided offset.
		/// </summary>
		/// <param name="owner">The <see cref="uLink.NetworkPlayer"/> that you want to move its objects.</param>
		/// <param name="target">The peer that you want to move objects to it.</param>
		/// <param name="offsetPos">Offset applied to positions of objects.</param>
		/// <param name="offsetRot">Offset applied to rotation of objects.</param>
		/// <param name="flags">Dictates the properties of the handover.</param>
		/// <param name="handoverData">You can send additional information using this parameter to the receiving side of the handover.</param>
		/// <remarks>See the <see cref="uLink.NetworkP2PHandoverFlags"/> to find out what you can change in the handover.
		/// </remarks>
		public void HandoverPlayerObjects(NetworkPlayer owner, NetworkPeer target, Vector3 offsetPos, Quaternion offsetRot, NetworkP2PHandoverFlags flags, params object[] handoverData)
		{
			var netViews = _network._FindNetworkViewsByOwner(owner);
			var instances = new NetworkP2PHandoverInstance[netViews.Length];

			if (netViews.Length != 0)
			{
				for (int i = 0; i < netViews.Length; i++)
					instances[i] = new NetworkP2PHandoverInstance(netViews[i], this, offsetPos, offsetRot);

				Handover(instances, target, flags, handoverData);
			}
			else if ((flags & NetworkP2PHandoverFlags.DontRedirectOwner) == 0)
			{
				Handover(owner, target, handoverData);
			}
		}

		/// <summary>
		/// Hands over the object which has the provided network view to the <c>target</c> peer.
		/// </summary>
		/// <param name="netView">The <see cref="uLink.NetworkView"/> which we want to hand over its object.</param>
		/// <param name="target">The peer that we want to choose as destination of the handover.</param>
		public void Handover(NetworkViewBase netView, NetworkPeer target)
		{
			Handover(netView, target, NetworkP2PHandoverFlags.Normal);
		}

		/// <summary>
		/// Hands over an object to another peer.
		/// </summary>
		/// <param name="netView">NetworkView of the object that you want to handover.</param>
		/// <param name="target">The receiver peer of the handover</param>
		/// <param name="flags">properties of the handover</param>
		/// <param name="handoverData">The additional data that you want to send to the receiver of the handover.</param>
		public void Handover(NetworkViewBase netView, NetworkPeer target, NetworkP2PHandoverFlags flags, params object[] handoverData)
		{
			var instance = new NetworkP2PHandoverInstance(netView, this);

			Handover(new[] { instance }, target, flags);
		}

		/// <summary>
		/// Hand an object over to another peer.
		/// </summary>
		/// <param name="netView">NetworkView of the object that you want to handover.</param>
		/// <param name="target">The receiver peer of the handover</param>
		/// <param name="relativePos">Position of the object in the receiving side relative to the NetworkP2P object</param>
		/// <param name="relativeRot">Rotation of the object in the receiving side relative to the NetworkP2P object</param>
		public void Handover(NetworkViewBase netView, NetworkPeer target, Vector3 relativePos, Quaternion relativeRot)
		{
			Handover(netView, target, relativePos, relativeRot, NetworkP2PHandoverFlags.Normal);
		}

		/// <summary>
		/// Hand an object over to another peer.
		/// </summary>
		/// <param name="netView">NetworkView of the object that you want to handover.</param>
		/// <param name="target">The receiver peer of the handover</param>
		/// <param name="relativePos">Position of the object in the receiving side relative to the NetworkP2P object</param>
		/// <param name="relativeRot">Rotation of the object in the receiving side relative to the NetworkP2P object</param>
		/// <param name="flags">Settings of the handover.</param>
		/// <param name="handoverData">The additional data that you want to send to the receiving side of the handover.</param>
		public void Handover(NetworkViewBase netView, NetworkPeer target, Vector3 relativePos, Quaternion relativeRot, NetworkP2PHandoverFlags flags, params object[] handoverData)
		{
			var instance = new NetworkP2PHandoverInstance(netView, relativePos, relativeRot, NetworkP2PSpace.NetworkP2P);

			Handover(new[] { instance }, target, flags, handoverData);
		}

		/// <summary>
		/// Hands over a set of <see cref="uLink.NetworkP2PHandoverInstance"/> objects to another peer.
		/// </summary>
		/// <param name="instances">The array of handoverInstance objects</param>
		/// <param name="target">The peer which should receive the handovered objects.</param>
		/// <remarks>
		/// A <see cref="uLink.NetworkP2PHandoverInstance"/> object contains the whole information required for an object to
		/// be handovered and you can customize them in any way you want as well.
		/// </remarks>
		public void Handover(NetworkP2PHandoverInstance[] instances, NetworkPeer target)
		{
			Handover(instances, target, NetworkP2PHandoverFlags.Normal);
		}

		public void Handover(NetworkPlayer client, NetworkPeer target, params object[] handoverData)
		{
			_AssertIsListening();

			var msg = new NetworkP2PMessage(INTERNAL_FLAGS, "", NetworkP2PMessage.InternalCode.HandoverRequest);
			msg.stream.WriteNetworkPlayer(client);
			msg.stream._buffer.WriteVariableUInt32(0);

			NetConnection connection = _peer.GetConnection(target.endpoint);
			if (connection == null)
			{
				// TODO: log error
				return;
			}

			var pendings = (connection.Tag ?? (connection.Tag = new PendingHandovers())) as PendingHandovers;
			var pending = new PendingHandover(client, null);
			var handoverID = pendings.Add(pending);

			Log.Debug(NetworkLogFlags.Handover, "P2P handover #", handoverID, " sending no instances to ", target);

			msg.stream._buffer.Write(handoverID);
			msg.stream._buffer.Write(_network.FindExternalEndPoint(client));

			ParameterWriter.WriteUnprepared(msg.stream, handoverData);

			_peer.SendMessage(msg.stream._buffer, connection, msg.channel);
		}

		/// <summary>
		/// Hands over a set of NetworkP2PHandoverInstance objects to another peer.
		/// </summary>
		/// <param name="instances">objects to handover.</param>
		/// <param name="target">The peer which should receive the result of handover.</param>
		/// <param name="flags">Settings of the handover</param>
		/// <param name="handoverData"The additional data which you might want to send to the target peer.</param>
		/// <remarks>
		/// A <see cref="uLink.NetworkP2PHandoverInstance"/> object contains the whole information required for an object to
		/// be handovered and you can customize them in any way you want as well.
		/// </remarks>
		public void Handover(NetworkP2PHandoverInstance[] instances, NetworkPeer target, NetworkP2PHandoverFlags flags, params object[] handoverData)
		{
			_AssertIsListening();

			NetworkPlayer owner;

			if ((flags & NetworkP2PHandoverFlags.DontRedirectOwner) == 0)
			{
				_network._AssertIsServerListening();
				owner = _GetOwnerOfHandoverInstances(instances);
			}
			else
			{
				if (instances == null || instances.Length == 0)
				{
					throw new ArgumentException("Can't be null or empty", "instances");
				}

				owner = NetworkPlayer.unassigned;
			}

			var msg = new NetworkP2PMessage(INTERNAL_FLAGS, "", NetworkP2PMessage.InternalCode.HandoverRequest);
			msg.stream.WriteNetworkPlayer(owner);
			msg.stream.WriteNetworkP2PHandoverInstances(instances);

			NetConnection connection = _peer.GetConnection(target.endpoint);
			if (connection == null)
			{
				// TODO: log error
				return;
			}

			var pendings = (connection.Tag ?? (connection.Tag = new PendingHandovers())) as PendingHandovers;
			var pending = new PendingHandover(owner, (flags & NetworkP2PHandoverFlags.DontDestroyOriginal) == 0 ? instances : null);
			var handoverID = pendings.Add(pending);

			Log.Debug(NetworkLogFlags.Handover, "P2P handover #", handoverID, " sending ", instances.Length, " instance(s) to ", target);

			msg.stream._buffer.Write(handoverID);
			msg.stream._buffer.Write(_network.FindExternalEndPoint(owner));

			ParameterWriter.WriteUnprepared(msg.stream, handoverData);

			_peer.SendMessage(msg.stream._buffer, connection, msg.channel);
		}

		internal NetworkPlayer _GetOwnerOfHandoverInstances(NetworkP2PHandoverInstance[] instances)
		{
			if (instances == null || instances.Length == 0)
			{
				throw new ArgumentException("Can't be null or empty", "instances");
			}

			var hashset = new HashSet<NetworkViewID>();
			var owner = NetworkPlayer.unassigned;

			for (int i = 0; i < instances.Length; i++)
			{
				var instance = instances[i];

				if (instance == null)
				{
					throw new ArgumentNullException("instances[" + i + "]");
				}

				instance._AssertRedirectOwner();

				if (owner == NetworkPlayer.unassigned)
				{
					owner = instance._networkView.owner;

					if (owner == NetworkPlayer.server)
					{
						throw new ArgumentException("Can't be owned by the server", "instances[" + i + "]");
					}
				}
				else if (instance._networkView.owner != owner)
				{
					throw new ArgumentException("All instances must be owned by the same client player", "instances[" + i + "]");
				}

				if (!hashset.Add(instance._networkView.viewID))
				{
					throw new ArgumentException("Can't handover the same object with " + instance._networkView.viewID + " multiple times", "instances[" + i + "]");
				}
			}

			return owner;
		}

		internal void _RPCHandoverRequest(NetworkPlayer owner, NetworkP2PHandoverInstance[] instances, uint handoverID, NetworkEndPoint clientDebugInfo, BitStream stream, NetworkP2PMessage msg)
		{
			_network._AssertIsServerListening();

			Log.Debug(NetworkLogFlags.Handover, "P2P handover request #", handoverID, " from ", msg.connection);

			foreach (var instance in instances)
			{
				instance._networkP2P = this as P2P;
			}

			Password passwordHash;

			if (owner != NetworkPlayer.unassigned)
			{
				passwordHash = _network.PasswordProtectHandoverSession(owner, instances, stream, clientDebugInfo);

				Log.Debug(NetworkLogFlags.Handover, "P2P handover request #", handoverID, " has generated session password ", passwordHash);
			}
			else
			{
				passwordHash = Password.empty;

				foreach (var instance in instances)
				{
					Log.Debug(NetworkLogFlags.Handover, "P2P handover request #", handoverID, " recreating instance with remote ", instance.remoteViewID, " now");
					instance.InstantiateNow(_network._localPlayer);
				}
			}

			var response = new NetworkP2PMessage(INTERNAL_FLAGS, "", NetworkP2PMessage.InternalCode.HandoverResponse);
			response.stream.WriteUInt32(handoverID);
			response.stream.WritePassword(passwordHash);
			response.stream.WriteUInt16((ushort)_network.listenPort);
			_peer.SendMessage(response.stream._buffer, msg.connection, response.channel);
		}

		internal void _RPCHandoverResponse(uint handoverID, Password passwordHash, ushort redirectPort, NetworkP2PMessage msg)
		{
			var pendings = msg.connection.Tag as PendingHandovers;
			PendingHandover pending;

			if (pendings == null || !pendings.Remove(handoverID, out pending))
			{
				// TODO: log error
				return;
			}

			if (pending.instances != null)
			{
				Log.Debug(NetworkLogFlags.Handover, "P2P handover response #", handoverID, " from ", msg.connection, " destroying ", pending.instances.Length, " original instance(s)");

				foreach (var networkView in pending.instances)
				{
					_network.Destroy(networkView);
				}
			}
			else
			{
				Log.Debug(NetworkLogFlags.Handover, "P2P handover response #", handoverID, " from ", msg.connection, " no original instances to destroy");
			}

			if (pending.owner != NetworkPlayer.unassigned)
			{
				_network._AssertIsServerListening();

				Log.Debug(NetworkLogFlags.Handover, "P2P handover response #", handoverID, " redirecting ", pending.owner);

				var peerIP = msg.connection.RemoteEndpoint.ipAddress;
				var ownerIP = _network.FindExternalEndPoint(pending.owner).ipAddress;

				if (NetworkUtility.IsLoopbackAddress(peerIP) || (!NetworkUtility.IsPublicAddress(peerIP) && NetworkUtility.IsPublicAddress(ownerIP)))
				{
					_network.RedirectConnection(pending.owner, redirectPort, passwordHash);
				}
				else
				{
					_network.RedirectConnection(pending.owner, new NetworkEndPoint(peerIP, redirectPort), passwordHash);
				}
			}
		}

		internal void _UnconnectedRPCDiscoverPeerRequest(PeerDataFilter filter, double remoteTime, NetworkEndPoint endpoint)
		{
			var data = _GetLocalPeerData(true, false);
			if (data == null) return;

			if (filter.Match(data))
			{
				var msg = new UnconnectedP2PMessage(UnconnectedP2PMessage.InternalCode.DiscoverPeerResponse);
				msg.stream.WriteLocalPeerData(data);
				msg.stream.WriteDouble(remoteTime);
				msg.stream.WriteEndPoint(endpoint);
				_UnconnectedRPC(msg, endpoint);
			}
		}

		internal void _UnconnectedRPCDiscoverPeerResponse(LocalPeerData localData, double localTime, NetworkEndPoint endpoint)
		{
			var ping = (int)NetworkTime._GetElapsedTimeInMillis(localTime);
			var data = new PeerData(localData, endpoint, ping);

			if (_discoveredPeers.ContainsKey(endpoint))
				_discoveredPeers[endpoint] = data;
			else
				_discoveredPeers.Add(endpoint, data);

			_OnPeerEvent(NetworkP2PEvent.LocalPeerDiscovered);
		}

		internal void _UnconnectedRPCKnownPeerRequest(double remoteTime, bool forceResponse, NetworkEndPoint endpoint)
		{
			var data = _GetLocalPeerData(!forceResponse, false);
			if (data == null) return;

			var msg = new UnconnectedP2PMessage(UnconnectedP2PMessage.InternalCode.KnownPeerResponse);
			msg.stream.WriteLocalPeerData(data);
			msg.stream.WriteDouble(remoteTime);
			msg.stream.WriteEndPoint(endpoint);
			_UnconnectedRPC(msg, endpoint);
		}

		internal void _UnconnectedRPCKnownPeerResponse(LocalPeerData localData, double localTime, NetworkEndPoint endpoint)
		{
			if (!_knownPeers.ContainsKey(endpoint)) return;

			var ping = (int)NetworkTime._GetElapsedTimeInMillis(localTime);
			var data = new PeerData(localData, endpoint, ping);

			_knownPeers[endpoint] = data;

			_OnPeerEvent(NetworkP2PEvent.KnownPeerDataReceived);
		}

		private void _UnconnectedRPC(UnconnectedP2PMessage msg, NetworkEndPoint target)
		{
			if (_peer != null && _peer.IsListening)
			{
				Log.Debug(NetworkLogFlags.P2P, "Peer is sending unconnected RPC ", msg.internCode, " to ", target);

				_peer.SendOutOfBandMessage(msg.stream._buffer, target);
			}
		}

		private LocalPeerData _GetLocalPeerData(bool errorCheck, bool notifyOnError)
		{
			if (errorCheck)
			{
				if (_peerTypeOrNameIsDirty)
				{
					_peerTypeOrNameIsDirty = false;
					notifyOnError = true;
				}

				if (String.IsNullOrEmpty(_peerType))
				{
					if (notifyOnError) _OnPeerEvent(NetworkP2PEvent.RegistrationFailedGameType);
					return null;
				}

				if (String.IsNullOrEmpty(_peerName))
				{
					if (notifyOnError) _OnPeerEvent(NetworkP2PEvent.RegistrationFailedGameName);
					return null;
				}
			}

			if (_network.localIpAddress == null) _network.localIpAddress = Utility.TryGetLocalIP();
			var localEndPoint = new NetworkEndPoint(_network.localIpAddress, listenPort);

			var data = new LocalPeerData(_peerType, _peerName, !String.IsNullOrEmpty(incomingPassword), comment, _network.OnGetPlatform(), DateTime.UtcNow, localEndPoint);
			return data;
		}

		private void _OnPeerEvent(NetworkP2PEvent eventCode)
		{
			OnEvent("OnPeerEvent", eventCode);
		}

		/// <summary>
		/// Clears the list of peers discovered.
		/// </summary>
		public void ClearDiscoveredPeers()
		{
			_discoveredPeers.Clear();
		}

		/// <summary>
		/// Gets the list of peers discovered by calling <see cref="DiscoverLocalPeers"/>
		/// </summary>
		/// <returns></returns>
		public PeerData[] PollDiscoveredPeers()
		{
			return Utility.ToArray(_discoveredPeers.Values);
		}

		/// <summary>
		/// Sends a broadcast message to discover peers on the LAN.
		/// </summary>
		/// <param name="filterGameType">The <see cref="uLink.NetworkP2P.gameType"/> of the peers that we are interested in.</param>
		/// <param name="remotePort">The port that the peers are listening to.</param>
		public void DiscoverLocalPeers(string filterGameType, int remotePort)
		{
			DiscoverLocalPeers(new PeerDataFilter(filterGameType), remotePort);
		}

		/// <summary>
		/// Sends a broadcast message on multiple ports to discover peers on the LAN.
		/// </summary>
		/// <param name="filterGameType">The <see cref="uLink.NetworkP2P.gameType"/> of the peers that we are interested in.</param>
		/// <param name="remoteStartPort">The starting port number.</param>
		/// <param name="remoteEndPort">The ending port number.</param>
		public void DiscoverLocalPeers(string filterGameType, int remoteStartPort, int remoteEndPort)
		{
			DiscoverLocalPeers(new PeerDataFilter(filterGameType), remoteStartPort, remoteEndPort);
		}

		/// <summary>
		/// Sends a broadcast message to discover peers on the LAN.
		/// </summary>
		/// <param name="filter">The filter which we should filter the list based on it.</param>
		/// <param name="remotePort">The port that peers are listening to.</param>
		public void DiscoverLocalPeers(PeerDataFilter filter, int remotePort)
		{
			DiscoverLocalPeers(filter, remotePort, remotePort);
		}

		/// <summary>
		/// Sends a broadcast message on multiple ports to discover peers on the LAN.
		/// </summary>
		/// <param name="filter">The filter which we should filter the list based on it.</param>
		/// <param name="remoteStartPort">The starting port</param>
		/// <param name="remoteEndPort">The ending port</param>
		public void DiscoverLocalPeers(PeerDataFilter filter, int remoteStartPort, int remoteEndPort)
		{
			if (remoteEndPort - remoteStartPort >= 20)
			{
				Log.Warning(NetworkLogFlags.P2P, "Sending broadcast packets on more than 20 ports (with frequent interval) to discover local peers, may cause some routers to block UDP traffic or behave undesirably.");
			}

			_timeOfDiscoveryRequest = NetworkTime.localTime;

			for (int port = remoteStartPort; port <= remoteEndPort; port++)
			{
				NetworkEndPoint target = new NetworkEndPoint(IPAddress.Broadcast, port);

				var msg = new UnconnectedP2PMessage(UnconnectedP2PMessage.InternalCode.DiscoverPeerRequest);
				msg.stream.WritePeerDataFilter(filter);
				msg.stream.WriteDouble(NetworkTime.localTime);
				_UnconnectedRPC(msg, target);
			}
		}

		/// <summary>
		/// Polls the list of already discovered peers and sends a discovery request if <c>discoverInterval</c> time passed from the previous discovery request.
		/// </summary>
		/// <param name="filterGameType">The <see cref="uLink.NetworkP2P.gameType"/> tht we are interested in.</param>
		/// <param name="remotePort">The port that the peers are listening to.</param>
		/// <param name="discoverInterval">The amount of time which should pass until we can send another discovery request.</param>
		/// <returns></returns>
		/// <remarks>
		/// You can even put this method in Update and set <c>discoverInterval</c> to some time like 2 seconds
		/// Then each 2 seconds a discovery request will be sent and in each frame you poll for the discovered list.
		/// </remarks>
		public PeerData[] PollAndDiscoverLocalPeers(string filterGameType, int remotePort, float discoverInterval)
		{
			return PollAndDiscoverLocalPeers(new PeerDataFilter(filterGameType), remotePort, discoverInterval);
		}

		/// <summary>
		/// Polls the list of already discovered peers and sends a discovery request if <c>discoverInterval</c> time passed from the previous discovery request.
		/// </summary>
		/// <param name="filterGameType">The <see cref="uLink.NetworkP2P.gameType"/> tht we are interested in.</param>
		/// <param name="remoteStartPort">The starting port of the range which peers are listening to.</param>
		/// <param name="remoteEndPort">The ending port of the range which peers are listening to.</param>
		/// <param name="discoverInterval">The amount of time which should pass until we can send another discovery request.</param>
		/// <returns></returns>
		/// <remarks>
		/// You can even put this method in Update and set <c>discoverInterval</c> to some time like 2 seconds
		/// Then each 2 seconds a discovery request will be sent and in each frame you poll for the discovered list.
		/// </remarks>
		public PeerData[] PollAndDiscoverLocalPeers(string filterGameType, int remoteStartPort, int remoteEndPort, float discoverInterval)
		{
			return PollAndDiscoverLocalPeers(new PeerDataFilter(filterGameType), remoteStartPort, remoteEndPort, discoverInterval);
		}

		/// <summary>
		/// It's like <see cref="PollAndDiscoverLocalPeers(System.String,System.Int32,System.Single)"/>, except you can set
		/// a filter based on many peer properties instead of only game type.
		/// </summary>
		public PeerData[] PollAndDiscoverLocalPeers(PeerDataFilter filter, int remotePort, float discoverInterval)
		{
			if (!Single.IsInfinity(discoverInterval))
			{
				var nextRequest = _timeOfDiscoveryRequest + discoverInterval;
				if (nextRequest <= NetworkTime.localTime)
				{
					DiscoverLocalPeers(filter, remotePort);
				}
			}

			return PollDiscoveredPeers();
		}

		/// <summary>
		/// It's just like <see cref="PollAndDiscoverLocalPeers(System.String,System.Int32,System.Int32,System.Single)"/> except you can choose a more
		/// advanced filter based on properties other than game type as well.
		/// </summary>
		public PeerData[] PollAndDiscoverLocalPeers(PeerDataFilter filter, int remoteStartPort, int remoteEndPort, float discoverInterval)
		{
			if (!Single.IsInfinity(discoverInterval))
			{
				var nextRequest = _timeOfDiscoveryRequest + discoverInterval;
				if (nextRequest <= NetworkTime.localTime)
				{
					DiscoverLocalPeers(filter, remoteStartPort, remoteEndPort);
				}
			}

			return PollDiscoveredPeers();
		}

		/// <summary>
		/// Clears the list of known peers choosen as favorite.
		/// </summary>
		public void ClearKnownPeers()
		{
			_knownPeers.Clear();
		}

		/// <summary>
		/// Returns a favorite known peer's data.
		/// </summary>
		/// <param name="host">The host address of the peer.</param>
		/// <param name="remotePort">The port that this peer is listening to.</param>
		/// <returns>The peer's full data.</returns>
		public PeerData PollKnownPeerData(string host, int remotePort)
		{
			return PollKnownPeerData(Utility.Resolve(host, remotePort));
		}

		/// <summary>
		/// Returns the data related to a known peer
		/// </summary>
		/// <param name="target">The IP and port of the peer</param>
		/// <returns></returns>
		public PeerData PollKnownPeerData(NetworkEndPoint target)
		{
			PeerData data;

			if (!_knownPeers.TryGetValue(target, out data)) return null;

			return data;
		}

		/// <summary>
		/// Returns the list of all known/favorite peers requested before.
		/// </summary>
		/// <returns></returns>
		public PeerData[] PollKnownPeers()
		{
			return Utility.ToArray(_knownPeers.Values);
		}

		/// <summary>
		/// Sends a request to find out the list of known peers.
		/// </summary>
		/// <param name="host">Host address of the peers</param>
		/// <param name="remotePort">Listening port of the peers</param>
		public void RequestKnownPeerData(string host, int remotePort)
		{
			RequestKnownPeerData(Utility.Resolve(host, remotePort));
		}

		/// <summary>
		/// Requests a list of known peers.
		/// </summary>
		/// <param name="target">IP and port of the peers.</param>
		public void RequestKnownPeerData(NetworkEndPoint target)
		{
			if (!_knownPeers.ContainsKey(target)) _knownPeers.Add(target, new PeerData(target));

			var msg = new UnconnectedP2PMessage(UnconnectedP2PMessage.InternalCode.KnownPeerRequest);
			msg.stream.WriteDouble(NetworkTime.localTime);
			msg.stream.WriteBoolean(false);
			_UnconnectedRPC(msg, target);
		}

		/// <summary>
		/// Adds a peer to the likst of known/favorite peers.
		/// </summary>
		/// <param name="host">IP address/host name of the peer.</param>
		/// <param name="remotePort">The port that the peer is listening to.</param>
		public void AddKnownPeerData(string host, int remotePort)
		{
			AddKnownPeerData(Utility.Resolve(host, remotePort));
		}


		/// <summary>
		/// Adds a peer to the list of known/favorite peers.
		/// </summary>
		/// <param name="target">IP and port of the peer</param>
		public void AddKnownPeerData(NetworkEndPoint target)
		{
			if (!_knownPeers.ContainsKey(target)) AddKnownPeerData(new PeerData(target));
		}

		/// <summary>
		/// Adds a peer to the list of known peers.
		/// </summary>
		/// <param name="data">The IP and port of the peer as a PeerData class</param>
		public void AddKnownPeerData(PeerData data)
		{
			_knownPeers[data.externalEndpoint] = data;
		}

		/// <summary>
		/// Removes a peer from the known/favorite peers list.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="remotePort"></param>
		public void RemoveKnownPeerData(string host, int remotePort)
		{
			RemoveKnownPeerData(Utility.Resolve(host, remotePort));
		}

		/// <summary>
		/// Removes a peer from the known/favorite peers list.
		/// </summary>
		public void RemoveKnownPeerData(NetworkEndPoint target)
		{
			_knownPeers.Remove(target);
		}

		/// <summary>
		/// Requests a list of all known/favorite peers.
		/// </summary>
		public void RequestKnownPeers()
		{
			_timeOfKnownPeersRequest = NetworkTime.localTime;

			foreach (var pair in _knownPeers)
			{
				RequestKnownPeerData(pair.Key);
			}
		}

		/// <summary>
		/// Polls the already received list of known peers and sends another request if <c>requestInterval</c> time passed from the previous request.
		/// </summary>
		/// <param name="requestInterval">The time that should pass before we send another request.</param>
		/// <returns>The list polled from the result of previous requests.</returns>
		/// <remarks>You can use this method easily in Update.
		/// The method will poll the list in every frame but only sends the request for new lists each
		/// <c>requestInterval</c> seconds.</remarks>
		public PeerData[] PollAndRequestKnownPeers(float requestInterval)
		{
			if (!Single.IsInfinity(requestInterval))
			{
				var nextRequest = _timeOfKnownPeersRequest + requestInterval;
				if (nextRequest <= NetworkTime.localTime)
				{
					RequestKnownPeers();
				}
			}

			return PollKnownPeers();
		}

		private void _AssertIsListening()
		{
			if(!(isListening)){Utility.Exception( "NetworkP2P component must be enabled and listening");}
		}

		public override string ToString()
		{
			return "NetworkP2PBase";
		}

		/*
		public void DumpReliability(NetworkPeer target)
		{
			var connection = _peer.GetConnection(target.endpoint);
			connection.DumpReliability(NetChannel.ReliableInOrder1);
		}
		*/
	}
}
