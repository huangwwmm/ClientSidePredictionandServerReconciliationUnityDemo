#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11924 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-04-22 12:47:31 +0200 (Sun, 22 Apr 2012) $
#endregion
using System;

namespace uLink
{
	/// <summary>
	/// The available types of network start events. See <see cref="uLink.Network.uLink_OnPreStartNetwork"/>.
	/// </summary>
	public enum NetworkStartEvent : byte
	{
		/// <summary>
		/// A master server connection is about to be initialized
		/// </summary>
		MasterServer = 0,
		/// <summary>
		/// The game server is about to be initialized
		/// </summary>
		Server = 1,
		/// <summary>
		/// A client is about to be initialized
		/// </summary>
		Client = 2,
		/// <summary>
		/// A cell server is about to be initialized
		/// </summary>
		CellServer = 3,
	}

	/// <summary>
	/// The available network statuses. See <see cref="uLink.Network.status"/>. See also <see cref="uLink.Network.peerType"/>.
	/// </summary>
	public enum NetworkStatus : byte
	{
		/// <summary>
		/// This peer is disconnected
		/// </summary>
		Disconnected = 0,
		/// <summary>
		/// This peer is connecting
		/// </summary>
		Connecting = 1,
		/// <summary>
		/// This peer is connected
		/// </summary>
		Connected = 2,
		/// <summary>
		/// This peer is disconnecting
		/// </summary>
		Disconnecting = 3,
	}

	/// <summary>
	/// The available log levels that can be set for minimum uLink logging and also can be set per log category.
	/// </summary>
	public enum NetworkLogLevel : byte
	{
		/// <summary>
		/// Logs nothing.
		/// </summary>
		Off = 0,
		/// <summary>
		/// Logs errors only
		/// </summary>
		Error = 1,
		/// <summary>
		/// Logs warnings and errors.
		/// </summary>
		Warning = 2,
		/// <summary>
		/// Logs info messages, warnings and errors.
		/// </summary>
		Info = 3,
		/// <summary>
		/// Logs debug messages, info messages, warnings and errors. The most detailed log level available.
		/// </summary>
		Debug = 4,
		/// <summary>
		/// Deprecated, please use NetworkLogLevel.Info instead
		/// </summary>
		[Obsolete("NetworkLogLevel.Informational is deprecated, please use NetworkLogLevel.Info instead")]
		Informational = Info,
		/// <summary>
		/// Deprecated, please use NetworkLogLevel.Debug instead
		/// </summary>
		[Obsolete("NetworkLogLevel.Full is deprecated, please use NetworkLogLevel.Debug instead")]
		Full = Debug,
	}

	// TODO: add more specific flags like P2PHandover, P2PRPC, InternalRPC, etc
	// TODO: use Allocator for all viewIDs, playerIDs, childIDs?

	/// <summary>
	/// The different log categories available in uLink. Read more in <see cref="uLink.NetworkLog"/>.
	/// </summary>
	[Flags]
	public enum NetworkLogFlags : uint
	{
		None = 0,

		Client = 1 << 0,
		Server = 1 << 1,
		MasterServer = 1 << 2,
		P2P = 1 << 3,
		RPC = 1 << 4,
		Observed = 1 << 5,
		StateSync = 1 << 6,
		Instantiate = 1 << 7,
		AuthoritativeServer = 1 << 8,
		ClockSync = 1 << 9,
		NetworkView = 1 << 10,
		Event = 1 << 11,
		BadMessage = 1 << 12,
		Allocator = 1 << 13,
		Security = 1 << 14,
		Encryption = 1 << 15,
		Handover = 1 << 16,
		CellServer = 1 << 17,
		Timestamp = 1 << 18,
		Buffered = 1 << 19,
		BitStreamCodec = 1 << 20,
		Utility = 1 << 21,
		Group = 1 << 22,
		PlayerID = 1 << 23,
		Socket = 1 << 24,
		InternalHelper = 1 << 25,
		Initialization = 1 << 26, // TODO: use this in instance and static ctor!
		Compression = 1 << 27,

		// TODO: remove UserDefined enums (we can't just obsolete them because that would still show up in edit settings)
		UserDefined3 = 1 << 28,
		UserDefined2 = 1 << 29,
		UserDefined1 = 1 << 30,

		All = 0xFFFFFFFF
	}

	/// <summary>
	/// The role of a peer/host for a network aware object (created with <see cref="O:uLink.Network.Instantiate"/>).
	/// </summary>
	/// <remarks>Read more in the manual about the three roles for network aware objects.</remarks>
	[Obsolete("NetworkRole is deprecated, please use NetworkView.isOwner or NetworkView.hasAuthority instead.")]
	public enum NetworkRole : byte
	{
		/// <summary>
		/// The peer/host is disconnected from the network.
		/// </summary>
		Disconnected = 0,
		/// <summary>
		/// This peer/host has a proxy of this network aware object.
		/// </summary>
		Proxy = 1,
		/// <summary>
		/// This peer/host is the owner of this network aware object.
		/// </summary>
		Owner = 2,
		/// <summary>
		/// This peer/host is the creator of this network aware object.
		/// </summary>
		Creator = 3
	}

	/// <summary>
	/// Used to control how RPCs will be handled by uLink.
	/// </summary>
	/// <remarks>Turn on one or many the these bit flags to make uLink handle
	/// the RPC exactly the way you want it. The buffer flag argument will overrule
	/// the buffer setting that the <see cref="uLink.RPCMode"/> argument in the
	/// <see cref="uLink.NetworkView.RPC(uLink.NetworkFlags, System.String, uLink.RPCMode, System.Object[])"/> function 
	/// usually  controls. If they are conflicting, uLink  log a warning.</remarks>
	[Flags]
	public enum NetworkFlags : byte
	{
		/// <summary>
		/// This is the base value. The RPC will be reliable, buffered, encrypted, typesafe and include a timestamp.
		/// </summary>
		Normal = 0,
		/// <summary>
		/// The RPC is sent over an unreliable network channel in uLink. Default is OFF.
		/// </summary>
		Unreliable = 1 << 0,
		/// <summary>
		/// The RPC is not stored in the RPC buffer on the server. This flag
		/// overrules the <see cref="uLink.RPCMode"/> buffer setting. Default Value is OFF.
		/// </summary>
		/// <value></value>
		Unbuffered = 1 << 1,
		/// <summary>
		/// The RPC is never encrypted, even if security is turned on. Default value is OFF.
		/// </summary>
		Unencrypted = 1 << 2,
		/// <summary>
		/// The RPC has no timestamp (to save bandwidth). Default value is OFF.
		/// </summary>
		NoTimestamp = 1 << 3,
		/// <summary>
		/// The types of the arguments in the RPC will not be checked when this RPC is received. Default value is OFF.
		/// </summary>
		TypeUnsafe = 1 << 4,
		/// <summary>
		/// The RPC is not to be culled due to Scope or Group (except when the NetworkView is hidden). Default value is OFF.
		/// </summary>
		NoCulling = 1 << 5,
	}

	/// <summary>
	/// The supported state synchronization modes for network aware objects in uLink
	/// </summary>
	public enum NetworkStateSynchronization : byte
	{
		/// <summary>
		/// This NetworkView will not send any statesync traffic
		/// </summary>
		Off = 0,
		/// <summary>
		/// The statesync traffic will be unreliable (uses least server side resources, but some packets can be lost in the network).
		/// </summary>
		Unreliable = 2,
		// /// <summary>
		// /// The statesync traffic will be unreliable and delta compressed (to save bandwidth).
		// /// </summary>
		//UnreliableDeltaCompressed = 3, // TODO: impl later 
		/// <summary>
		/// The statesync traffic will be reliable (all packets will arrive at destination).
		/// </summary>
		Reliable = 4,
		/// <summary>
		/// The statesync traffic will be reliable and delta compressed (to save bandwidth).
		/// </summary>
		ReliableDeltaCompressed = 5,
	}

	/// <summary>
	/// The available choices for property "Securable" in a uLink.NetworkView component. 
	/// </summary>
	[Flags]
	public enum NetworkSecurable : byte
	{
		/// <summary>
		///  Secure nothing
		/// </summary>
		None = 0,
		/// <summary>
		/// Secure only RPCs which will be sent by this network aware object
		/// </summary>
		OnlyRPCs = 1 << 0,
		/// <summary>
		/// Secure only state synchronization for this network aware object
		/// </summary>
		OnlyStateSynchronization = 1 << 1,
		/// <summary>
		/// Secure both RPCs and state synchronization for this network aware object
		/// </summary>
		Both = OnlyRPCs | OnlyStateSynchronization
	}

	/// <summary>
	/// The available reasons for a disconnection event in uLink. 
	/// </summary>
	/// <remarks>See uLink.Network.<see cref="uLink.Network.uLink_OnDisconnectedFromServer"/></remarks>
	public enum NetworkDisconnection : byte
	{
		/// <summary>
		/// The client lost its connection to server.
		/// </summary>
		LostConnection,
		/// <summary>
		/// The client disconnected from server.
		/// </summary>
		Disconnected,
		/// <summary>
		/// The client disconnected from its already connected server to connect to another server.
		/// </summary>
		Redirecting,
	}

	/// <summary>
	/// The available modes for sending RPCs in a peer-to-peer network.
	/// </summary>
	/// <remarks>see <see cref="uLink.NetworkP2P"/></remarks>
	public enum PeerMode : byte
	{
		/// <summary>
		/// Send the RPC to all peers except myself.
		/// </summary>
		Others = 0,
		/// <summary>
		/// Send the RPC to all peers including myself.
		/// </summary>
		All = 1
	}

	/// <summary>
	/// Indicates how a RPC should be treated by uLink.
	/// </summary>
	public enum RPCMode : byte
	{
		/// <summary>
		/// The RPC will only be sent to the server. This is the only allowed RPCMode in clients when the server is authoritative.
		/// </summary>
		Server = 0,
		/// <summary>
		/// The RPC will be sent to every connected peer and I will not get the RPC myself.
		/// </summary>
		Others = 1,
		/// <summary>
		/// The RPC will be sent to every connected peer, including myself.
		/// </summary>
		All = 2,
		/// <summary>
		/// The RPC will only be sent to the owner of the network aware object.
		/// </summary>
		Owner = 3,
		/// <summary>
		/// The RPC will only be added to RPC buffer, which is sent to new players when their connection is established.
		/// </summary>
		Buffered = 4,
		/// <summary>
		/// The RPC will be sent to every connected peer and I will not get the RPC myself. The server will also buffer this RPC.
		/// </summary>
		OthersBuffered = Others | Buffered,
		/// <summary>
		/// The RPC will be sent to every connected peer, including myself. The server will also buffer this RPC.
		/// </summary>
		AllBuffered = All | Buffered,
		/// <summary>
		/// The RPC will be sent to every connected peer, but not to myself and not to the owner of the network aware object.
		/// </summary>
		OthersExceptOwner = 9,
		/// <summary>
		/// The RPC will be sent to every connected peer, but not to the owner of the network aware object.
		/// </summary>
		AllExceptOwner = 10,
		/// <summary>
		/// The RPC will sent to every connected peer except me and owner of the network aware object. The server will also buffer this RPC.
		/// </summary>
		OthersExceptOwnerBuffered = OthersExceptOwner | Buffered,
		/// <summary>
		/// The RPC will sent to every connected peer except owner, including myself. The server will also buffer this RPC.
		/// </summary>
		AllExceptOwnerBuffered = AllExceptOwner | Buffered,
	}

	/// <summary>
	/// A peer can be only one of these types. See <see cref="uLink.Network.peerType"/>. See also <see cref="uLink.Network.status"/>.
	/// </summary>
	public enum NetworkPeerType : byte
	{
		/// <summary>
		/// The peer is disconnected
		/// </summary>
		Disconnected = 0,
		/// <summary>
		/// The peer is a server
		/// </summary>
		Server = 1,
		/// <summary>
		/// The peer is a client
		/// </summary>
		Client = 2,
		/// <summary>
		/// This peer is a cell server
		/// </summary>
		CellServer = 3,

		[Obsolete("NetworkPeerType.Connecting no longer returned, use \"Network.status == NetworkStatus.Connecting\" instead.")]
		Connecting = 4,
	}

	/// <summary>
	/// The available connection errors in uLink.
	/// </summary>
	/// <remarks>Use the value UserDefined1 for signaling your own custom error situation to the client.
	/// Send this error code from the server code you write for the the callback 
	/// <see cref="uLink.Network.uLink_OnPlayerApproval"/>.
	/// If you need more custom error codes, just add the integers 1, 2, 3 and so on to the user defined value.
	/// </remarks>
	/// <example>
	/// Define your own error codes like this in a script the server and the clients can both access.
	/// <code>
	/// public int MyErrorCode1 = uLink.NetworkConnectionError.UserDefined1;
	/// public int MyErrorCode2 = uLink.NetworkConnectionError.UserDefined1 + 1;
	/// public int MyErrorCode3 = uLink.NetworkConnectionError.UserDefined1 + 2;
	/// </code>
	/// </example>
	public enum NetworkConnectionError : int
	{
		InternalDirectConnectFailed = -5,
		EmptyConnectTarget = -4,
		IncorrectParameters = -3,
		CreateSocketOrThreadFailure = -2,
		/// <summary>
		/// The client is already connected to another server. uLink client can only connect to one uLink server at the same time.
		/// </summary>
		AlreadyConnectedToAnotherServer = -1,
		/// <summary>
		/// It means no error is returned but you should wait for <see cref="uLink.Network.uLink.OnConnectedToServer"/>
		/// to see if the connection was successful or not, the connection might time out and fails later on.
		/// </summary>
		NoError = 0,
		ConnectionFailed = 14,
		/// <summary>
		/// The server has the most number of clients which it can have (the max
		/// number of clients is set when initializing the server), so it can not accept new players.
		/// </summary>
		TooManyConnectedPlayers = 17,
		/// <summary>
		/// The public key of the client doesn't match with the private key of the server.
		/// See the manual chapter for security for more information.
		/// </summary>
		RSAPublicKeyMismatch = 20,
		ConnectionBanned = 21,
		/// <summary>
		/// The password sent by the client is different from what server expects as incoming password.
		/// </summary>
		InvalidPassword = 22,
		DetectedDuplicatePlayerID = 23,
		NATTargetNotConnected = 61,
		NATTargetConnectionLost = 62,
		NATPunchthroughFailed = 63,
		/// <summary>
		/// Client and server use different incompatible uLink versions.
		/// </summary>
		IncompatibleVersions = 64,
		ServerAuthenticationTimeout = 65,

		ConnectionTimeout = 70,
		LimitedPlayers = 71,
		/// <summary>
		/// Server is authoritative but client did not set <see cref="uLink.Network.isAuthoritativeServer"/> to <c>true</c>.
		/// </summary>
		IsAuthoritativeServer = 80,
		/// <summary>
		/// Server did not approve the client. However you don't have to use this value as the reason when denying.
		/// </summary>
		ApprovalDenied = 81,

		ProxyTargetNotConnected = 90,
		ProxyTargetNotRegistered = 91,
		ProxyServerNotEnabled = 92,
		ProxyServerOutOfPorts = 93,

		NetworkShutdown = 100,

		/// <summary>
		/// This can be used for any user defined purpose. You can use UserDefined + X where X is a positive number
		/// to have more user defined values.
		/// </summary>
		UserDefined1 = 128,
	}

	/// <summary>
	/// The available return values when testing a network connection's NAT
	/// capabilities.
	/// </summary>
	/// <remarks>
	/// See <see cref="O:uLink.Network.TestConnection"/>
	/// and <see cref="uLink.Network.TestConnectionNAT"/>.
	/// </remarks>
	public enum ConnectionTesterStatus : sbyte
	{
		Error = -2,
		Undetermined = -1,
		[Obsolete("No longer returned, use newer connection tester enums instead.")]
		PrivateIPNoNATPunchthrough = 0,
		[Obsolete("No longer returned, use newer connection tester enums instead.")]
		PrivateIPHasNATPunchThrough = 1,
		PublicIPIsConnectable = 2,
		PublicIPPortBlocked = 3,
		PublicIPNoServerStarted = 4,
		LimitedNATPunchthroughPortRestricted = 5,
		LimitedNATPunchthroughSymmetric = 6,
		NATpunchthroughFullCone = 7,
		NATpunchthroughAddressRestrictedCone = 8,
	}

	/// <summary>
	/// The available response codes when communicating with a stand-alone <see cref="uLink.MasterServer"/>.
	/// </summary>
	public enum MasterServerEvent : byte
	{
		RegistrationFailedGameName = 0,
		RegistrationFailedGameType = 1,
		RegistrationFailedNoServer = 2,
		/// <summary>
		/// Registration of the server in master server has been done successfully.
		/// </summary>
		RegistrationSucceeded = 3,
		/// <summary>
		/// Host list received after <see cref="uLink.MasterServer.PollHostList"/> was invoked in client. 
		/// </summary>
		HostListReceived = 4,

		/// <summary>
		/// Local host discovered after <see cref="uLink.MasterServer.DiscoverLocalHosts"/> was invoked in client.
		/// </summary>
		LocalHostDiscovered = 5,
		/// <summary>
		/// Known host data received after <see cref="uLink.MasterServer.PollKnownHostData"/> was invoked in client.
		/// </summary>
		KnownHostDataReceived = 6,

		/// <summary>
		/// MasterServer is not running as proxy server but the game server requires a proxy server.
		/// </summary>
		/// <remarks>
		/// See MasterServer manual page for more information.
		/// </remarks>
		RegistrationFailedNoProxy = 7,
	}

	/// <summary>
	/// The event name inside <see cref="uLink.NetworkP2P.uLink_OnPeerEvent"/>.
	/// </summary>
	public enum NetworkP2PEvent : byte
	{
		RegistrationFailedGameName = 0,
		RegistrationFailedGameType = 1,

		/// <summary>
		/// local peers discovered after <see cref="uLink.NetworkP2PBase.DiscoverLocalPeers"/> was invoked.
		/// </summary>
		LocalPeerDiscovered = 5,
		/// <summary>
		/// Local peers data received after <see cref="uLink.NetworkP2PBase.RequestKnownPeerData"/> was invoked. 
		/// </summary>
		KnownPeerDataReceived = 6,
	}

	/// <summary>
	/// These are the network group settings which can be set per group.
	/// </summary>
	[Flags]
	public enum NetworkGroupFlags : byte
	{
		/// <summary>
		/// None of the flags are not set.
		/// </summary>
		None = 0x0,
		/// <summary>
		/// Add new players to group automatically when they connect to server
		/// </summary>
		AddNewPlayers = 0x1,
		/// <summary>
		/// If set, GameObjects that belong to group will be destroyed in players which are not 
		/// a member of this group and will be instantiated in the players as soon as they become
		/// member.
		/// </summary>
		HideGameObjects = 0x2,
	}

	/// <summary>
	/// The data types uLink can Serialize. Read more about this in the manual section for data types and serialization.
	/// </summary>
	public enum BitStreamTypeCode : byte
	{
		[Obsolete("BitStreamTypeCode.Object is deprecated, please use BitStreamTypeCode.Undefined instead.")]
		Object = 0,

		Undefined = 0,

		Boolean,
		BooleanNullable,
		Char,
		CharNullable,
		SByte,
		SByteNullable,
		Byte,
		ByteNullable,
		Int16,
		Int16Nullable,
		UInt16,
		UInt16Nullable,
		Int32,
		Int32Nullable,
		UInt32,
		UInt32Nullable,
		Int64,
		Int64Nullable,
		UInt64,
		UInt64Nullable,
		Single,
		SingleNullable,
		Double,
		DoubleNullable,
		Decimal,
		DecimalNullable,
		DateTime,
		DateTimeNullable,
		String,
		IPEndPoint,
		EndPoint,
		NetworkEndPoint,

		NetworkPlayer,
		NetworkPlayerNullable,
		NetworkViewID,
		NetworkViewIDNullable,
		NetworkPeer,
		NetworkPeerNullable,
		Vector2,
		Vector2Nullable,
		Vector3,
		Vector3Nullable,
		Vector4,
		Vector4Nullable,
		Quaternion,
		QuaternionNullable,
		Color,
		ColorNullable,

		LocalHostData,
		HostData,
		HostDataFilter,
		PublicKey,
		PrivateKey,
		SerializedAssetBundle,
		NetworkP2PHandoverInstance,
		NetworkGroup,
		LocalPeerData,
		PeerData,
		PeerDataFilter,
		BitStream,
		NetworkCellID,

		/* ... reserved ... */

		// TODO: explain
		UserDefined1 = 64,
		/* ... */
		UserDefinedMax = 127,

		// TODO: explain
		ArrayType1 = 128, //TODO: lower this -1 because Undefined+ArrayType1 is never used, but remember that it breaks the protocol!
		/* ... */
		ArrayTypeMax = 255,
		MaxValue = ArrayTypeMax,
	}
}

/* TODO: use this at some point.

[Flags]
internal enum RPCFlags : byte
{
	Server = 0, // special case

	Clients = 1 << 0,
	CellServers = 1 << 1,
	Buffered = 1 << 2,

	Me = 1 << 3,
	Owner = 1 << 4,

	ExceptMe = 1 << 5,
	ExceptOwner = 1 << 6,
}

public enum RPCMode : byte
{
	Server = RPCFlags.Server,

	Clients = RPCFlags.Clients,
	CellServers = RPCFlags.CellServers,
	Buffered = RPCFlags.Buffered,

	Me = RPCFlags.Me,
	Owner = RPCFlags.Owner,


	All = Clients | CellServers,
	Others = All | RPCFlags.ExceptMe,
	
	AllBuffered = All | Buffered,
	OthersBuffered = Others | Buffered,

	AllExceptOwner = All | RPCFlags.ExceptOwner,
	OthersExceptOwner = Others | RPCFlags.ExceptOwner,

	AllExceptOwnerBuffered = AllExceptOwner | RPCFlags.Buffered,
	OthersExceptOwnerBuffered = OthersExceptOwner | RPCFlags.Buffered,

	
	ClientsBuffered = Clients | Buffered,
	CellServersBuffered = CellServers | Buffered,

	ClientsExceptOwnerBuffered = ClientsBuffered | RPCFlags.ExceptOwner,
	//CellServersExceptOwnerBuffered = CellServersBuffered | RPCFlags.ExceptOwner, // NOTE: doesn't make much sense


	OtherClients = Clients | RPCFlags.ExceptMe,
	OtherCellServers = CellServers | RPCFlags.ExceptMe,

	OtherClientsBuffered = OtherClients | RPCFlags.Buffered,
	OtherCellServersBuffered = OtherCellServers | RPCFlags.Buffered,

	OtherClientsExceptOwner = OtherClients | RPCFlags.ExceptOwner,
	//OtherCellServersExceptOwner = OtherCellServers | RPCFlags.ExceptOwner, // NOTE: doesn't make much sense

	OtherClientsExceptOwnerBuffered = OtherClientsBuffered | RPCFlags.ExceptOwner,
	//OtherCellServersExceptOwnerBuffered = OtherCellServersBuffered | RPCFlags.ExceptOwner, // NOTE: doesn't make much sense
}
*/
