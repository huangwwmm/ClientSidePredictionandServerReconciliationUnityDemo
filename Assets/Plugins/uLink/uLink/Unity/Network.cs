#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12012 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-01 10:17:33 +0200 (Tue, 01 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;
using Object = UnityEngine.Object;

namespace uLink
{
	/// <summary>
	/// The central class in uLink. Contains core functionality in uLink.
	/// </summary>
	/// <remarks>
	/// This class enables several core uLink features such as: starting servers,
	/// connecting clients, dynamically instantiating network aware objects and
	/// security/encryption features. Functions marked as Message callbacks
	/// are events triggered by uLink, which can be implemented in your scripts to
	/// handle network events with custom code in a client or a server.
	/// </remarks>
	public static class Network
	{
		internal static NetworkUnity _singleton = new NetworkUnity();

		static Network()
		{
			GetPrefs();
		}

		/// <summary>
		/// Gets the persistent Network properties in <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		public static void GetPrefs()
		{
			symmetricKeySize = NetworkPrefs.Get("Network.symmetricKeySize", 128);
			incomingPassword = NetworkPrefs.Get("Network.incomingPassword", String.Empty);
			sendRate = NetworkPrefs.Get("Network.sendRate", 15f);
			trackRate = NetworkPrefs.Get("Network.trackRate", 2f);
			trackMaxDelta = NetworkPrefs.Get("Network.trackMaxDelta", 0f);
			isAuthoritativeServer = NetworkPrefs.Get("Network.isAuthoritativeServer", false);
			useRedirect = NetworkPrefs.Get("Network.useRedirect", false);
			redirectIP = NetworkPrefs.Get("Network.redirectIP", String.Empty);
			redirectPort = NetworkPrefs.Get("Network.redirectPort", 0);
			useDifferentStateForOwner = NetworkPrefs.Get("Network.useDifferentStateForOwner", true);
			rpcTypeSafe = (RPCTypeSafe)NetworkPrefs.Get("Network.rpcTypeSafe", (int)RPCTypeSafe.OnlyInEditor);
			useProxy = NetworkPrefs.Get("Network.useProxy", false);
			requireSecurityForConnecting = NetworkPrefs.Get("Network.requireSecurityForConnecting", false);

			emulation.GetPrefs();
			config.GetPrefs();
		}

		/// <summary>
		/// Sets the persistent Network properties in <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		/// <remarks>
		/// The method can't update the saved values in the persistent <see cref="uLink.NetworkPrefs.resourcePath"/> file,
		/// because that assumes the file is editable (i.e. the running project isn't built) and would require file I/O permission.
		/// Calling this will only update the values in memory.
		/// </remarks>
		public static void SetPrefs()
		{
			NetworkPrefs.Set("Network.symmetricKeySize", symmetricKeySize);
			NetworkPrefs.Set("Network.incomingPassword", incomingPassword);
			NetworkPrefs.Set("Network.sendRate", sendRate);
			NetworkPrefs.Set("Network.trackRate", trackRate);
			NetworkPrefs.Set("Network.trackMaxDelta", trackMaxDelta);
			NetworkPrefs.Set("Network.isAuthoritativeServer", isAuthoritativeServer);
			NetworkPrefs.Set("Network.useRedirect", useRedirect);
			NetworkPrefs.Set("Network.redirectIP", redirectIP);
			NetworkPrefs.Set("Network.redirectPort", redirectPort);
			NetworkPrefs.Set("Network.useDifferentStateForOwner", useDifferentStateForOwner);
			NetworkPrefs.Set("Network.rpcTypeSafe", (int)rpcTypeSafe);
			NetworkPrefs.Set("Network.useProxy", useProxy);
			NetworkPrefs.Set("Network.requireSecurityForConnecting", requireSecurityForConnecting);

			emulation.SetPrefs();
			config.SetPrefs();
		}

		/// <summary>
		/// Gets or sets if a uLink client requires a public key before connecting to the server.
		/// </summary>
		/// <remarks>This value is only used in clients. If true, the client can only connect 
		/// to a server if the public key has been provided with <see cref="publicKey"/>. 
		/// Default value is <c>false</c>.</remarks>
		public static bool requireSecurityForConnecting
		{
			get { return _singleton.requireSecurityForConnecting; }
			set { _singleton.requireSecurityForConnecting = value; }
		}

		[Obsolete("Network.batchSendAtEndOfFrame is deprecated, please use Network.config.batchSendAtEndOfFrame instead")]
		public static bool batchSendAtEndOfFrame
		{
			get { return _singleton.batchSendAtEndOfFrame; }
			set { _singleton.batchSendAtEndOfFrame = value; }
		}

		/// <summary>
		/// Gets or sets the uLink licenseKey.
		/// </summary>
		/// <remarks>
		/// You can use the <see cref="uLinkEnterLicenseKey"/> component to add your license key to your game servers.
		/// Note that you should not put your license key in your client applications because it will be exposed for piracy.
		/// You can use defines to only include the string in server version of your code or use the key in editor in a prefab which is only inserted in server scenes.
		/// </remarks>
		public static string licenseKey
		{
			get { return _singleton.licenseKey; }
			set { _singleton.licenseKey = value; }
		}

		/// <summary>
		/// Gets or sets the symmetricKeySize.
		/// </summary>
		public static int symmetricKeySize
		{
			get { return _singleton.symmetricKeySize; }
			set { _singleton.symmetricKeySize = value; }
		}

		/// <summary>
		/// Gets the last returned <see cref="uLink.NetworkConnectionError"/>.
		/// </summary>
		/// <value>Default value is <see cref="uLink.NetworkConnectionError.NoError"/></value>
		public static NetworkConnectionError lastError
		{
			get { return _singleton.lastError; }
			set { _singleton.lastError = value; }
		}

		/// <summary>
		/// Gets an array of all instantiated networkViews.
		/// </summary>
		public static NetworkView[] networkViews
		{
			get
			{
				var collection = _singleton.networkViews;
				var array = new NetworkView[collection.Count];
				int i = 0;

				foreach (var element in collection)
				{
					array[i++] = element as NetworkView;
				}

				return array;
			}
		}

		/// <summary>
		/// Gets the number of instantiated networkViews.
		/// </summary>
		public static int networkViewCount
		{
			get
			{
				return _singleton.networkViewCount;
			}
		}

		/// <summary>
		/// Gets all connected players.
		/// </summary>
		/// <remarks>
		/// On a client this array contains only the server.
		/// </remarks>
		/// <example>
		/// This C# example can be run on a server with GUI. When you hit the button a player will be disconnected.
		/// <code>
		/// void OnGUI() {
		///    if (GUILayout.Button ("Disconnect first player")) {
		///        if (uLink.Network.connections.length > 0) {
		///            Debug.Log("Disconnecting: "+
		///                uLink.Network.connections[0].ipAddress+":"+uLink.Network.connections[0].port);
		///            uLink.Network.CloseConnection(uLink.Network.connections[0], true);
		///        } 
		///    }    
		/// }
		/// </code>
		/// </example>
		public static NetworkPlayer[] connections { get { return _singleton.connections; } }

		public static int connectionCount { get { return _singleton.connectionCount; } }

		/// <summary>
		/// Gets or sets the connection tester IP. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future.</remarks>
		[Obsolete("Network.connectionTesterIP is not implemented yet.")]
		public static string connectionTesterIP { get { return _singleton.connectionTesterIP; } set { _singleton.connectionTesterIP = value; } }

		/// <summary>
		/// Gets or sets the connection tester port. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future.</remarks>
		[Obsolete("Network.connectionTesterPort is not implemented yet.")]
		public static int connectionTesterPort { get { return _singleton.connectionTesterPort; } set { _singleton.connectionTesterPort = value; } }

		/// <summary>
		/// Gets or sets the password for the server (for incoming connections).
		/// </summary>
		/// <remarks>This must be matched in the 
		/// clients. Pass "" to specify no password (this is default).</remarks>
		/// <example>
		/// <code>
		/// void ConnectToServer () //On the client
		/// {
		///    Network.Connect("127.0.0.1", 25000, "HolyMoly");
		/// }
		/// 
		/// void LaunchServer () //On the server
		/// {
		///    Network.incomingPassword = "HolyMoly";
		///    Network.InitializeServer(32, 25000);
		/// }
		/// </code></example>
		/// <seealso cref="uLink.Network.Connect"/>
		public static string incomingPassword { get { return _singleton.incomingPassword; } set { _singleton.incomingPassword = value; } }

		/// <summary>
		/// Gets a value indicating whether this instance is a uLink client.
		/// </summary>
		/// <value><c>true</c> if this instance is client; otherwise, <c>false</c>. 
		/// Returns true even if the client's <see cref="status"/> is connecting.</value>
		/// <seealso cref="isServer"/>
		public static bool isClient { get { return _singleton.isClient; } }

		/// <summary>
		/// Enable or disable the processing of incoming network messages. No messages are discarded.
		/// </summary>
		/// <remarks>This feature can be used to stop all incoming network traffic in a client, 
		/// for example when the client is loading a level. The statesync and RPC messages are not discarded by uLink. 
		/// They are delivered later when this value is set to <c>true</c>.
		/// There is no limit for the message queue, so please do not turn this off in a client unless you plan 
		/// to turn it on again, otherwise the client might end up with a memory leak.
		/// </remarks>
		/// <value>
		/// 	<c>true</c> when incoming network messages are processed; otherwise, <c>false</c>. Default value is true.
		/// </value>
		public static bool isMessageQueueRunning { get { return _singleton.isMessageQueueRunning; } set { _singleton.isMessageQueueRunning = value; } }

		/// <summary>
		/// Gets a value indicating whether this instance is a server.
		/// </summary>
		/// <value><c>true</c> if this instance is server; otherwise, <c>false</c>. 
		/// Returns true even if the server's <see cref="status"/> is connecting.</value>
		/// <seealso cref="isClient"/>
		public static bool isServer { get { return _singleton.isServer; } }

		/// <summary>
		/// Gets or sets the maximum number of connections/players allowed on a server.
		/// </summary>
		/// <remarks>
		/// This cannot be set higher than 
		/// the connection count given in <see cref="O:uLink.Network.InitializeServer"/>.
		/// In addition, there are two special values, 0 and -1. Setting it to 0 means no new connections 
		/// can be made but the existing ones stay connected. Setting it to -1 means the maximum 
		/// connection count is set to the number of currently open connections. 
		/// </remarks>
		public static int maxConnections { get { return _singleton.maxConnections; } set { _singleton.maxConnections = value; } }

		/// <summary>
		/// Gets or sets the maximum number of manualViewIDs that are available for usage.
		/// </summary>
		/// <value>Default value is 1000.</value>
		/// <remarks>
		/// Increase this value if there is a need to assign more than 1000 manualViewIDs in the game.
		/// </remarks>
		[Obsolete("Network.maxManualViewIDs is deprecated, it's not needed anymore.")]
		public static int maxManualViewIDs { get { return NetworkViewID.maxManual.subID - NetworkViewID.minManual.subID; } set { } }

		/// <summary>
		/// The minimum number of entries in the clients pool of unused ViewIDs.
		/// </summary>
		/// <value>Default value is 1000.</value>
		/// <remarks>
		/// This value is interesting when creating a game with clients that make lots of Instantiate calls, 
		/// thus being authoritative clients.
		/// A ViewID pool is given to each client when it connects. The size of the pool is dictated by this property plus 
		/// the property <see cref="uLink.Network.minimumUsedViewIDs"/>. The client starts to allocate these viewIDs 
		/// one by one for every Instantiate call. When the unused (free) number of viewIDs reach this minimum in the client,
		/// uLink will make a call to the server to get more (unused) viewIDs.   
		/// The server and clients should be in sync regarding this value. Setting this higher only on the server has the effect that it sends more 
		/// view ID numbers to clients, than they really want. Setting this higher only on clients 
		/// means they request more view IDs more often, for example twice in a row, as the pools 
		/// received from the server don't contain enough numbers.
		/// 
		/// </remarks>
		/// <example><code>
		/// void Awake () {
		///    // Use this setting on both client and server.
		///    Network.minimumAllocatableViewIDs = 500;
		/// }
		/// </code></example>
		[Obsolete("Network.minimumAllocatableViewIDs is deprecated, it's not needed anymore.")]
		public static int minimumAllocatableViewIDs { get { return NetworkViewID.maxManual.subID - NetworkViewID.minManual.subID; } set { } }

		/// <summary>
		/// The number of viewIDs each client is supposed to use (allocate) in a normal game session. 
		/// </summary>
		/// <value>Default value is 1</value>
		/// <remarks>
		/// This value is interesting when creating a game with clients that make lots of Instantiate 
		/// calls, thus being authoritative clients. A ViewID pool is given to each client when it 
		/// connects. The size of the pool is dictated by this property plus the property 
		/// <see cref="uLink.Network.minimumAllocatableViewIDs"/>. To make the pool size bigger by default just 
		/// increase this number. The motive for increasing this number is that the clients making 
		/// many Instantiate calls want to get bigger "chunks" of unused viewIDs when the client connects and 
		/// also whenever the client reaches its <see cref="uLink.Network.minimumAllocatableViewIDs"/>. This reduces the 
		/// amount if uLink internal RPCs to the server to allocate more viewIDs.
		/// </remarks>
		/// <example>In an FPS game where players shoot at each other with rockets and the rockets are 
		/// network aware objects it could be wise to increase this number.</example>
		[Obsolete("Network.minimumUsedViewIDs is deprecated, it's not needed anymore.")]
		public static int minimumUsedViewIDs { get { return NetworkViewID.maxManual.subID - NetworkViewID.minManual.subID; } set { } }

		/// <summary>
		/// Gets or sets the NAT facilitator IP. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future. </remarks>
		[Obsolete("Network.natFacilitatorIP is not implemented yet.")]
		public static string natFacilitatorIP { get { return _singleton.natFacilitatorIP; } set { _singleton.natFacilitatorIP = value; } }

		/// <summary>
		/// Gets or sets the NAT facilitator port. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future. </remarks>
		[Obsolete("Network.natFacilitatorPort is not implemented yet.")]
		public static int natFacilitatorPort { get { return _singleton.natFacilitatorPort; } set { _singleton.natFacilitatorPort = value; } }

		/// <summary>
		/// Gets the <see cref="uLink.NetworkStatus"/> of this network peer (<see cref="uLink.NetworkPeerType"/>).
		/// It shows wether the peer is connected/disconnected/...
		/// </summary>
		public static NetworkStatus status { get { return _singleton.status; } }

		/// <summary>
		/// Gets the UDP port number for the socket uLink has opened.
		/// </summary>
		/// <return>0 if the socket is not open.</return>
		public static int listenPort { get { return _singleton.listenPort; } }

		// TODO: add documentation.
		public static NetworkEndPoint listenEndPoint { get { return _singleton.listenEndPoint; } }

		/// <summary>
		/// Gets the type of this network peer (i.e. server, client, disconnected ...). 
		/// </summary>
		/// <seealso cref="isServer"/>
		/// <seealso cref="isClient"/>
		public static NetworkPeerType peerType { get { return _singleton.peerType; } }

		/// <summary>
		/// Gets the player at this peer/host.
		/// </summary>
		/// <remarks>On the server this get call will return the special 
		/// <see cref="uLink.NetworkPlayer"/> indicating this is a server 
		/// (uLink.NetworkPlayer.<see cref="uLink.NetworkPlayer.server"/>).
		/// </remarks>
		/// <seealso cref="uLink.NetworkPlayer.server"/>
		/// <seealso cref="connections"/>
		public static NetworkPlayer player { get { return _singleton._localPlayer; } }

		/// <summary>
		/// Indicates if it is required that clients connect via a proxy
		/// </summary>
		/// <remarks>
		/// Set this value to true in a game server before regestering the server in the master server.
		/// Read more about the master server and the proxy server in the Master Server and Proxy manual chapter.
		/// In uLink, master server provides the proxy server functionality as well so the port and ip of the proxy server are the same that you use for master server. 
		/// </remarks>
		public static bool useProxy { get { return _singleton.useProxy; } set { _singleton.useProxy = value; } }

		/// <summary>Not in use.</summary>
		/// <remarks>Read the Master Server and Proxy manual chapter. It explains how to use the Proxy Server.</remarks>
		public static string proxyIP { get { return _singleton.proxyIP; } set { _singleton.proxyIP = value; } }
		/// <summary>Not in use.</summary>
		/// <remarks>Read the Master Server and Proxy manual chapter. It explains how to use the Proxy Server.</remarks>
		/// 
		public static string proxyPassword { get { return _singleton.proxyPassword; } set { _singleton.proxyPassword = value; } }
		/// <summary>Not in use.</summary>
		/// <remarks>Read the Master Server and Proxy manual chapter. It explains how to use the Proxy Server.</remarks>
		public static int proxyPort { get { return _singleton.proxyPort; } set { _singleton.proxyPort = value; } }

		/// <summary>
		/// Gets or sets the send rate for state synchronizations from this peer/host.
		/// </summary>
		/// <value>The default value is 15.</value>
		/// <remarks>Fast paced games like FPS games should use 15-25. Other games can use any value. This value is the most (server-side) bandwidth 
		/// sensitive configuration in uLink, at least if the usage of statesync in the game is extensive.
		/// If you want to send different information with different rates in uLink, then you can use RPCs with coroutines/InvokeRepeating.
		/// </remarks>
		/// <example>
		/// <code>
		/// void Awake ()
		/// {
		///     // Increase default send rate
		///     uLink.Network.sendRate = 25;
		/// }
		/// </code>
		/// </example>
		public static float sendRate { get { return _singleton.sendRate; } set { _singleton.sendRate = value; } }

		/// <summary>
		/// Cell Server only: Gets or sets the send rate for track position messages sent from this cell server.
		/// </summary>
		/// <remarks>
		/// Read pikko server's manual for more information.
		/// </remarks>
		public static float trackRate { get { return _singleton.trackRate; } set { _singleton.trackRate = value; } }

		/// <summary>
		/// Cell Server only: Gets or sets the max delta distance that an object can move without sending track position messages (from this cell server).
		/// </summary>
		/// <remarks>
		/// Read pikko server's manual for more information.
		/// </remarks>
		public static float trackMaxDelta { get { return _singleton.trackMaxDelta; } set { _singleton.trackMaxDelta = value; } }

		/// <summary>
		/// Gets the current network time in seconds. This is the client's <see cref="localTime"/>
		/// plus the offset of the server's time, compared to its own.
		/// </summary>
		/// <remarks>
		/// This value starts at 0 when the server starts. 
		/// If a uLink client hasn't yet connected to a server or a server hasn't been initialized, 0 is returned.
		/// <para>
		/// The network time on all connected clients is synchronized by uLink. 
		/// Each client is synchronized just once, during the internal uLink connection sequence.
		/// The ping time during the client's connection is divided in half to 
		/// set the time offset for this client, 
		/// that will be used in the client until it is disconnected.
		/// </para>
		/// <para>
		/// Be aware of the fact that this time value, in the client, is based on the local clock after 
		/// the initial connection and therefore changes in ping time can make the server time and client 
		/// time appear to run with fluctuating speeds. Try to compare client time stamps with other client stamps 
		/// and server time stamps with other server time stamps to avoid subtile bugs that will appear 
		/// only when the ping time changes. When comparing client time stamps with server time stamps, be careful.
		/// </para>
		/// <para>
		/// uLink uses two separate local CPU clocks to make sure the value for Network.time is always correct 
		/// and very accurate on several different hardwares.
		/// </para>
		/// </remarks>
		/// 
		/// <example>This value can, for example, be used to compare with the time 
		/// returned in <see cref="uLink.NetworkMessageInfo"/>.
		/// <code>
		/// class SomeClass : uLink.MonoBehaviour
		/// {
		///		float something;
		///		double transitTime;
		/// 
		///		void uLink_OnSerializeNetworkView (uLink.BitStream stream, uLink.NetworkMessageInfo info)
		///		{
		///			float horizontalInput = 0.0;
		///			if (stream.isWriting)
		///			{
		///				// Sending
		///				horizontalInput = transform.position.x;
		///				stream.WriteFloat(horizontalInput);
		///			} 
		///			else
		///			{
		///				// Receiving
		///				transitTime	= Network.time - info.timestamp;
		///				horizontalInput = stream.ReadFloat();
		///				something = horizontalInput;
		///			}
		///		}
		/// 
		///		void OnGUI()
		///		{
		///			GUILayout.Label("Last transmission time: " + transitTime.ToString());
		///		}
		/// }
		/// </code></example>
		[Obsolete("Network.time is deprecated, please use NetworkTime.serverTime instead")]
		public static double time { get { return _singleton.serverTime; } }

		/// <summary>
		/// Gets the current network time in milliseconds. 
		/// </summary>
		/// <remarks>Works just like <see cref="uLink.Network.time"/>, except the 
		/// returned value is rounded to the the closest millisecond. 
		/// </remarks>
		/// <example>This can, for example, be used to compare with the time 
		/// returned in <see cref="uLink.NetworkMessageInfo"/>.
		/// <code>
		/// class SomeClass : uLink.MonoBehaviour
		/// {
		///		float something;
		///		ulong transitTimeInMillis;
		/// 
		///		void uLink_OnSerializeNetworkView (uLink.BitStream stream, uLink.NetworkMessageInfo info)
		///		{
		///			float horizontalInput = 0.0;
		///			if (stream.isWriting)
		///			{
		///				// Sending
		///				horizontalInput = transform.position.x;
		///				stream.WriteFloat(horizontalInput);
		///			} 
		///			else
		///			{
		///				// Receiving
		///				transitTimeInMillis = Network.timeInMillis - info.timestampInMillis;
		///				horizontalInput = stream.ReadFloat();
		///				something = horizontalInput;
		///			}
		///		}
		/// 
		///		void OnGUI()
		///		{
		///			GUILayout.Label("Last transmission time: " + timestampInMillis.ToString());
		///		}
		///	}
		/// </code></example>
		[Obsolete("Network.localTime is deprecated, please use NetworkTime.serverTime instead")]
		public static ulong timeInMillis { get { return (ulong)NetTime.ToMillis(_singleton.serverTime); } }

		/// <summary>
		/// The local time of the player. This timer is started as soon as uLink starts working.
		/// this is a high precision value calculated using the operating system's high precision timers and is not
		/// smoothed out for animation or any other purpose like (UnityEngine.Time.time)
		/// </summary>
		[Obsolete("Network.localTime is deprecated, please use NetworkTime.localTime instead")]
		public static double localTime { get { return NetworkTime.localTime; } }

		/// <summary>
		/// Same as <see cref="localTime"/> but in milliseconds.
		/// </summary>
		[Obsolete("Network.localTimeInMillis is deprecated, please use NetworkTime.localTime instead")]
		public static ulong localTimeInMillis { get { return (ulong)NetTime.ToMillis(NetworkTime.localTime); } }

		/// <summary>
		/// Gets or sets a value indicating whether to use NAT punchthrough.
		/// </summary>
		/// <value><c>true</c> if using NAT punchthrough; otherwise, <c>false</c>. Default is false.</value>
		/// <remarks>
		/// Not Implemented.
		/// Read more on this subject in the manual and the <see cref="uLink.MasterServer"/>.
		/// </remarks>
		public static bool useNat { get { return _singleton.useNat; } set { _singleton.useNat = value; } }

		/// <summary>
		/// Query for the next available network view ID number and allocate it.
		/// </summary>
		/// <returns>The allocated ViewID.</returns>
		/// <remarks>
		/// Only use this function if you are an advanced user and know what you are doing. For
		/// all common situations in a multiplayer game you should be fine using 
		/// <see cref="O:uLink.Network.Instantiate"/> 
		/// to instantiate network aware prefabs, or if it is a
		/// static game object in the hierarchy view of the scene, give it a manual view ID during development.
		/// If you choose to use this function, this viewID number can then be assigned to the 
		/// network view of an instantiated object (not instantiated via the Network class).
		/// <para>
		/// This method can only be called in an authoritative server or in an authoritative client.
		/// </para><para>
		/// The owner of this viewID will become the local <see cref="uLink.NetworkPlayer"/>
		/// </para><para>
		/// When running an authoritative server, the next logical step after calling this method on the server is to send an RPC to 
		/// <see cref="uLink.RPCMode">RPCMode.All</see> including the new viewID and in the RPC receiving code write a call to 
		/// <see cref="O:uLink.NetworkViewBase.SetViewID"/>.
		/// This will make all connected peers aware of how to connect this new viewID to a 
		/// Game Object. It must be a Game Object that has an attached uLink NetworkView script component.
		/// </para>
		/// <para>
		/// uLink never allocated the same viewID right after a call to <see cref="uLink.Network.DeallocateViewID"/>. 
		/// The reason for this is to avoid subtile bugs where a viewID is resued so fast that some connected peers might 
		/// miss this very important event (for example due to UDP packets arriving in the wrong order) and execute 
		/// RPC code on the wrong game object. Therefore it is safe to make several fast calls to AllocateViewID 
		/// and DeallocateViewID right after each other.
		/// </para>
		/// <para>
		/// ViewIDs are represented with a 16 bit number in uLink. This makes the maximum number of viewIDs 65536.
		/// </para>
		/// </remarks>
		/// <example>
		/// The example below demonstrates a simple method to do this. Note that for this to 
		/// work there must be a uLink.NetworkView component attached to the object which has this script and 
		/// it must have the script as its observed property. There must be a Cube prefab present 
		/// also with a uLink.NetworkView component which watches something (like the Transform of the Cube). The 
		/// cubePrefab variable in the script must be set to that cube prefab. This is the simplest 
		/// method of using AllocateViewID. 
		/// <code>
		/// class SomeClass : uLink.MonoBehaviour
		/// {
		///		public Transform cubePrefab;
		/// 
		///		void OnGUI ()
		///		{
		///			if (GUILayout.Button("SpawnBox"))
		///			{
		///				var viewID = uLink.Network.AllocateViewID();
		///				uLink.NetworkView.Get(this).RPC("SpawnBox", uLink.RPCMode.AllBuffered, viewID, transform.position);    
		///			}
		///		}
		/// 
		///		[RPC]
		///		void SpawnBox (Link.NetworkViewID viewID, Vector3 location)
		///		{
		///			// Instantiate the prefab locally
		///			Transform clone;
		///			clone = Instantiate(cubePrefab, location, Quaternion.identity) as Transform;
		///			uLink.NetworkView nView;
		///			nView = clone.GetComponent&ls;uLink.NetworkView&gt;();
		///			nView.viewID = viewID;
		///		}
		///	}
		/// </code>
		/// </example>
		public static NetworkViewID AllocateViewID() { return _singleton.AllocateViewID(); }

		/// <summary>
		/// Allocates one viewID and sets the owner at the same time.
		/// </summary>
		/// <param name="owner">The <see cref="uLink.NetworkPlayer"/> that you want to be the owner of this ViewID</param>
		/// <returns>The allocated ID.</returns>
		/// <remarks>
		/// Works just like <see cref="O:uLink.Network.AllocateViewID"/>.
		/// </remarks>
		public static NetworkViewID AllocateViewID(NetworkPlayer owner) { return _singleton.AllocateViewID(owner); }

		/// <summary>
		/// Allocates an array of viewIDs at once.
		/// </summary>
		/// <param name="count">Number of the ViewIDs that you want to allocate.</param>
		/// <returns>An array of allocated ViewIDs.</returns>
		/// <remarks>
		/// Works just like <see cref="O:uLink.Network.AllocateViewID"/>.
		/// <para>
		/// The owner of this array of viewIDs will become the local <see cref="uLink.NetworkPlayer"/>
		/// </para>
		/// </remarks>
		public static NetworkViewID[] AllocateViewIDs(int count) { return _singleton.AllocateViewIDs(count); }

		/// <summary>
		/// Allocates an array of viewIDs at once and sets the owner for all of them.
		/// </summary>
		/// <param name="count">Number of ViewIDs that you want to allocate.</param>
		/// <param name="owner">The NetworkPlayer that you want to be the owner of the allocated ViewIDs.</param>
		/// <returns>An array of allocated ViewIDs.</returns>
		/// <remarks>
		/// Works just like <see cref="O:uLink.Network.AllocateViewID"/>.
		/// </remarks>
		public static NetworkViewID[] AllocateViewIDs(int count, NetworkPlayer owner) { return _singleton.AllocateViewIDs(count, owner); }

		/// <summary>
		/// Returns an unused viewID to the pool of unused IDs. 
		/// </summary>
		/// <param name="viewID">The ViewID that you want to deallocate.</param>
		/// <returns>Wether the operation was successful or not.</returns>
		/// <remarks>
		/// Can only be called in an authoritative server or in authoritative clients. 
		/// Can only be called on the same peer as the viewID was allocated.
		/// </remarks>
		public static bool DeallocateViewID(NetworkViewID viewID) { return _singleton.DeallocateViewID(viewID); }
		/// <summary>
		/// Returns an array of viewIDs to the pool of unused IDs. 
		/// </summary>
		/// <param name="viewIDs">The array of ViewIDs which you want to deallocate.</param>
		/// <returns>Wether the operation was successful or not.</returns>
		/// <remarks>
		/// Can only be called in an authoritative server or in authoritative clients. 
		/// Can only be called on the same peer as the viewIDs was allocated.
		/// </remarks>
		public static bool DeallocateViewIDs(NetworkViewID[] viewIDs) { return _singleton.DeallocateViewIDs(viewIDs); }

		/// <summary>
		/// Returns all viewIDs for one <see cref="uLink.NetworkPlayer"/> to the pool of unused IDs. 
		/// </summary>
		/// <param name="owner">The NetworkPlayer that you want to deallocate ViewIDs owned by it.</param>
		/// <returns>Wether the operation was successful or not.</returns>
		/// <remarks>
		/// Can only be called in an authoritative server or in authoritative clients. 
		/// Can only be called on the same peer as the viewIDs was allocated.
		/// </remarks>
		public static bool DeallocateViewIDs(NetworkPlayer owner) { return _singleton.DeallocateViewIDs(owner); }

		/// <summary>
		/// Close the connection to another system.
		/// </summary>
		/// <param name="target">Defines which system to close the connection to</param>
		/// <param name="sendDisconnectionNotification">if set to <c>true</c> sends a reliable disconnection notification to target.</param>
		/// <remarks>If we are a client the only possible connection to close is the server connection, 
		/// if we are a server the target player will be kicked out.  If sendDisconnectionNotification is 
		/// <c>false</c> the connection is dropped, if <c>true</c> a disconnect notification is reliably 
		/// sent to the remote peer and thereafter the connection is dropped.
		/// <para>
		/// You can use this to kick players which cheat or have slow connections drop from the game.
		/// </para>
		/// </remarks>
		/// <seealso cref="uLink.Network.Disconnect"/>
		/// <seealso cref="uLink.Network.DisconnectImmediate"/>
		public static void CloseConnection(NetworkPlayer target, bool sendDisconnectionNotification) { _singleton.CloseConnection(target, sendDisconnectionNotification); }

		/// <summary>
		/// Close the connection to another system.
		/// </summary>
		/// <param name="target">Defines which system to close the connection to</param>
		/// <param name="sendDisconnectionNotification">if set to <c>true</c> sends a reliable disconnection notification to target.</param>
		/// <param name="timeout">Will wait this long (seconds) for the ack of the uLink internal disconnect notification RPC before dropping the client anyway.</param>
		/// <remarks>If we are a client the only possible connection to close is the server connection, 
		/// if we are a server the target player will be kicked out.  If sendDisconnectionNotification is 
		/// <c>false</c> the connection is dropped, if <c>true</c> a disconnect notification is reliably 
		/// sent to the remote peer and thereafter the connection is dropped.
		/// <para>
		/// You can use this to kick players which cheat or have slow connections drop from the game.
		/// </para>
		/// </remarks>
		/// <seealso cref="uLink.Network.Disconnect"/>
		/// <seealso cref="uLink.Network.DisconnectImmediate"/>
		public static void CloseConnection(NetworkPlayer target, bool sendDisconnectionNotification, int timeout) { _singleton.CloseConnection(target, sendDisconnectionNotification, timeout); }

		/// <summary>Connects this client to the specified server, registerd in the master server.</summary>
		/// <param name="host">This is the <see cref="uLink.HostData"/> received from the <see cref="uLink.MasterServer"/>.</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, NetworkConnectionError.<see cref="NetworkConnectionError.NoError"/> is returned</returns>
		public static NetworkConnectionError Connect(HostData host) { return Connect(host, String.Empty); }

		/// <summary>Connects this client to the specified server.</summary>
		/// <param name="server">The server (includes host and port)</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, NetworkConnectionError.<see cref="uLink.NetworkConnectionError.NoError"/> is returned</returns>
		public static NetworkConnectionError Connect(NetworkEndPoint server) { return Connect(server, String.Empty); }

		/// <summary>Connects this client to the specified server.</summary>
		/// <param name="host">Hostname as string or IP address as a String (four numbers with dots between)</param>
		/// <param name="remotePort">Server port number</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, NetworkConnectionError.<see cref="NetworkConnectionError.NoError"/> is returned</returns>
		public static NetworkConnectionError Connect(string host, int remotePort) { return Connect(host, remotePort, String.Empty); }

		/// <summary>Connects this client to the specified server.</summary>
		/// <param name="hosts">Try to connect to the servers in this array, one by one, until a 
		/// connections is established</param>
		/// <param name="remotePort">Server port number</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, NetworkConnectionError.<see cref="NetworkConnectionError.NoError"/> is returned</returns>
		public static NetworkConnectionError Connect(string[] hosts, int remotePort) { return Connect(hosts, remotePort, String.Empty); }

		/// <summary>Connects this client to the specified server, registerd in the master server.</summary>
		/// <param name="host">This is the <see cref="uLink.HostData"/> received from the <see cref="uLink.MasterServer"/>.</param>
		/// <param name="password">Sends a password (salted and hashed) inside this connection request. 
		/// Use this as a game/level/instance password if it suits the needs of the game.</param>
		/// <param name="loginData">Put any number of arguments here as loginData. The data will be 
		/// delivered to the server and the server can handle the data as a <see cref="uLink.BitStream"/> 
		/// in the notification <see cref="uLink.Network.uLink_OnPlayerApproval"/>. The server can use this loginData  
		/// to make a choice between approving the client or denying the client. Use this feature for 
		/// things like Avatar name, preferred team membership, username, password, client type, etc. 
		/// It is possible and recommended to encrypt password and loginData. Set 
		/// <see cref="uLink.Network.publicKey"/> in
		/// the client to turn on encryption before calling this method.</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, NetworkConnectionError.<see cref="NetworkConnectionError.NoError"/> is returned</returns>
		/// <seealso cref="uLink.Network.uLink_OnPlayerApproval"/>
		public static NetworkConnectionError Connect(HostData host, string password, params object[] loginData) { return _singleton.Connect(host, password, loginData); }

		/// <summary>Connects this client to the specified server.</summary>
		/// <param name="server">The server (includes host and port)</param>
		/// <param name="password">Sends a password (salted and hashed) inside this connection request. 
		/// Use this as a game/level/instance password if it suits the needs of the game.</param>
		/// <param name="loginData">Put any number of arguments here as loginData. The data will be 
		/// delivered to the server and the server can handle the data as a <see cref="uLink.BitStream"/> 
		/// in the notification <see cref="uLink.Network.uLink_OnPlayerApproval"/>. The server can use this loginData  
		/// to make a choice between approving the client or denying the client. Use this feature for 
		/// things like Avatar name, preferred team membership, username, password, client type, etc. 
		/// It is possible and recommended to encrypt password and loginData. Set 
		/// <see cref="uLink.Network.publicKey"/> in
		/// the client to turn on encryption before calling this method.</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, <see cref="uLink.NetworkConnectionError.NoError"/> is returned</returns>
		/// <seealso cref="uLink.Network.uLink_OnPlayerApproval"/>
		public static NetworkConnectionError Connect(NetworkEndPoint server, string password, params object[] loginData) { return _singleton.Connect(server, password, loginData); }

		/// <summary>Connects this client to the specified server.</summary>
		/// <param name="host">Hostname as string or IP address as a String (four numbers with dots between)</param>
		/// <param name="remotePort">Server port number</param>
		/// <param name="password">Sends a password (salted and hashed) inside this connection request. 
		/// Use this as a game/level/instance password if it suits the needs of the game.</param>
		/// <param name="loginData">Put any number of arguments here as loginData. The data will be 
		/// delivered to the server and the server can handle the data as a <see cref="uLink.BitStream"/> 
		/// in the notification <see cref="uLink.Network.uLink_OnPlayerApproval"/>. The server can use this loginData  
		/// to make a choice between approving the client or denying the client. Use this feature for 
		/// things like Avatar name, prefered team membership, username, password, client type, etc. 
		/// It is possible and recommended to encrypt password and loginData. Set 
		/// <see cref="uLink.Network.publicKey"/> in
		/// the client to turn on encryption before calling this method.</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, NetworkConnectionError.<see cref="NetworkConnectionError.NoError"/> is returned</returns>
		/// <seealso cref="uLink.Network.uLink_OnPlayerApproval"/>
		public static NetworkConnectionError Connect(string host, int remotePort, string password, params object[] loginData) { return _singleton.Connect(host, remotePort, password, loginData); }

		/// <summary>Connects this client to the specified server.</summary>
		/// <param name="hosts">Try to connect to the servers in this array, one by one, until a 
		/// connections is established</param>
		/// <param name="remotePort">Server port number</param>
		/// <param name="password">Sends a password (salted and hashed) inside this connection request. 
		/// Use this as a game/level/instance password if it suits the needs of the game.</param>
		/// <param name="loginData">Put any number of arguments here as loginData. The data will be 
		/// delivered to the server and the server can handle the data as a <see cref="uLink.BitStream"/> 
		/// in the notification <see cref="uLink.Network.uLink_OnPlayerApproval"/>. The server can use this loginData  
		/// to make a choice between approving the client or denying the client. Use this feature for 
		/// things like Avatar name, prefered team membership, username, password, client type, etc. 
		/// It is possible and recommended to encrypt password and loginData. Set 
		/// <see cref="uLink.Network.publicKey"/> in
		/// the client to turn on encryption before calling this method.</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors, NetworkConnectionError.<see cref="uLink.NetworkConnectionError.NoError"/> is returned</returns>
		/// <seealso cref="uLink.Network.uLink_OnPlayerApproval"/>
		public static NetworkConnectionError Connect(string[] hosts, int remotePort, string password, params object[] loginData) { return _singleton.Connect(hosts, remotePort, password, loginData); }

		[Obsolete("Network.Connect(String) is deprecated, please use NetworkLog.Connect(HostData) instead")]
		public static NetworkConnectionError Connect(string ipOrHostInclPort) { return Connect(ipOrHostInclPort, String.Empty); }

		[Obsolete("Network.Connect(String, String) is deprecated, please use NetworkLog.Connect(HostData, String) instead")]
		public static NetworkConnectionError Connect(string ipOrHostInclPort, string password, params object[] loginData) { return _singleton.Connect(ipOrHostInclPort, password, loginData); }

		/// <summary>
		///  Destroy the game object associated with this <see cref="uLink.NetworkView"/> across the 
		///  network and remove the buffered RPCs for this <see cref="uLink.NetworkView"/> from the uLink RPC buffer.
		/// </summary>
		/// <remarks>The object is destroyed locally and remotely.</remarks>
		/// <returns><c>True</c> if the object was successfully destroyed.</returns>
		public static bool Destroy(NetworkView networkView) { return _singleton.Destroy(networkView); }

		/// <summary>
		///  Destroy the game object associated with this <see cref="uLink.NetworkViewID"/> across the 
		///  network and remove the buffered RPCs for this <see cref="uLink.NetworkViewID"/> from the uLink RPC buffer.
		/// </summary>
		/// <remarks>The object is destroyed locally and remotely.</remarks>
		/// <returns><c>True</c> if the object was successfully destroyed.</returns>
		public static bool Destroy(NetworkViewID viewID) { return _singleton.Destroy(viewID); }

		/// <summary>
		///  Destroy this game object across the network and remove the buffered RPCs 
		///  for the <see cref="uLink.NetworkViewID"/> attached to this gameObject from the uLink RPC buffer.
		/// </summary>
		/// <remarks>The object is destroyed locally and remotely.</remarks>
		/// <returns><c>True</c> if the object was successfully destroyed.</returns>
		public static bool Destroy(GameObject gameObject) { return _singleton.Destroy(gameObject); }

		/// <summary>
		/// Destroy all network aware objects owned by this player across the
		/// network and remove the buffered RPCs from the uLink RPC buffer.
		/// </summary>
		/// <remarks>
		/// <para>The objects owned by this player are destroyed locally and
		/// remotely. It is common to call this method in the callback 
		/// <see cref="uLink_OnPlayerDisconnected(uLink.NetworkPlayer)"/>.
		/// </para>
		/// <para>
		/// All buffered RPCs (including the instantiating RPCs) for the objects 
		/// owned by this player are removed from the uLink RPC buffer. This is 
		/// done by internal calls to <see cref="RemoveInstantiates(uLink.NetworkPlayer)"/>
		/// and <see cref="RemoveRPCs(uLink.NetworkPlayer)"/>.
		/// </para>
		/// <para>
		/// Who owns a network instantiated object is determined when the 
		/// <see cref="O:uLink.Network.Instantiate"/> method is used. If the
		/// owner hasn't been specified in the arguments to Instantiate, it is
		/// the caller of Instantiate. When you insert objects in scen and assign manual ViewIDs to them, all of them will be owned by server.
		/// </para></remarks>
		public static void DestroyPlayerObjects(NetworkPlayer target) { _singleton.DestroyPlayerObjects(target); }

		/// <summary>
		/// Destroy all dynamically allocated or instantiated network aware objects across the network and remove all buffered RPCs from the uLink RPC buffer.
		/// </summary>
		/// <remarks>This won't affect objects assigned manual ViewIDs. To destroy them as well see <see cref="uLink.Network.DestroyAll(System.Boolean)"/></remarks>
		public static void DestroyAll() { _singleton.DestroyAll(false); }

		/// <summary>
		/// Destroy all network aware objects across the network and remove all buffered RPCs from the uLink RPC buffer.
		/// </summary>
		/// <param name="includeManual">If true, also destroy objects assigned manual ViewIDs; otherwise skip them.</param>
		public static void DestroyAll(bool includeManual) { _singleton.DestroyAll(includeManual); }

		/// <summary>
		/// Closes the network connection.
		/// </summary>
		/// <remarks>This disconnect function uses a default timeout value of 200 ms. If this is a client, it will send 
		/// a reliable disconnect message to the server and wait for the ack during the timeout. During the timeout, 
		/// incoming and outgoing statesync and RPCs will be handled as normal.
		/// 
		/// The complete network state, like security and password, is reset by this function. 
		/// 
		/// If this is a server, it will disconnect all clients after sending a disconnect messages to them.</remarks>
		/// <seealso cref="uLink.Network.DisconnectImmediate"/>
		/// <seealso cref="uLink.Network.CloseConnection"/>
		public static void Disconnect() { Disconnect(Constants.DEFAULT_DICONNECT_TIMEOUT); }

		/// <summary>
		/// Closes the network connection.
		/// </summary>
		/// <param name="timeout">Indicates how long (in milliseconds) this network connection will wait for an ack message and during that wait time it will continue to receive RPCs and statesyncs.</param>
		/// <remarks>If this is a client, it will try to send 
		/// a disconnect message to the server before this timeout. During the timeout, incoming statesync and RPCs will be handled as normal.
		/// If the timeout passes and the ack has not been received, the client will disconnect and clean up anyway.
		/// 
		/// The network state, like security and password, is reset by this function. 
		/// 
		/// If this is a server, it will disconnect all clients after sending a disconnect messages to them.</remarks>
		/// <seealso cref="uLink.Network.DisconnectImmediate"/>
		/// <seealso cref="uLink.Network.CloseConnection"/>
		public static void Disconnect(int timeout) { _singleton.Disconnect(timeout); }

		/// <summary>
		/// Closes the network connection immediately without any notification to others.
		/// </summary>
		/// <remarks>The only way others will notice this is by a timeout. Read more about such timeouts 
		/// in the callback <see cref="uLink.Network.uLink_OnPlayerDisconnected"/>.</remarks>
		///<seealso cref="uLink.Network.CloseConnection"/>
		///<seealso cref="uLink.Network.Disconnect"/>
		public static void DisconnectImmediate() { _singleton.DisconnectImmediate(); }

		/// <summary>
		/// Gets average ping time for a player in milliseconds.
		/// </summary>
		/// <param name="target">The NetworkPlayer which you want to get your average ping against.</param>
		/// <returns>Average ping time for a player in milliseconds. If target is unknown or not connected, then returns -1</returns>
		/// <remarks>Calculates the average of the last few pings, making this a moving average.
		/// In the client you should call GetAveragePing(uLink.NetworkPlayer.server) because the only available target is the server. 
		/// In the server you can check the ping time to any connected player.
		/// </remarks>
		public static int GetAveragePing(NetworkPlayer target) { return _singleton.GetAveragePing(target); }

		/// <summary>
		/// Gets last ping time for a player in milliseconds.
		/// </summary>
		/// <param name="target">The NetworkPlayer that you want to know the last ping time for.</param>
		/// <returns>Last ping time for a player in milliseconds. If target is unknown or not connected, then returns -1</returns>
		/// <remarks>In the client you should call GetLastPing(uLink.NetworkPlayer.server) because the only available target is the server. 
		/// In the server you can check the ping time to any connected player.
		/// </remarks>
		public static int GetLastPing(NetworkPlayer target) { return _singleton.GetLastPing(target); }

		/// <summary>
		/// Gets a <see cref="uLink.NetworkStatistics"/> object for the specified remote player.
		/// </summary>
		/// <param name="target">The player at the other end of the connection that you want statistics for.</param>
		/// <returns>A <see cref="uLink.NetworkStatistics"/> object if there is a connection to the player, or null otherwise</returns>
		public static NetworkStatistics GetStatistics(NetworkPlayer target) { return _singleton.GetStatistics(target); }

		/// <summary>
		/// Check if this machine has a public IP address.
		/// </summary>
		/// <remarks>
		/// It checks all the network interfaces for IPv4 public addresses and returns true if one address is found.
		/// <para>
		/// If the server does not have any public address, clients outside of the network that it has an address in can not
		/// access it without using proxy server.
		/// </para>
		/// </remarks>
		public static bool HavePublicAddress() { return _singleton.HavePublicAddress(); }

		/// <summary>
		/// Initializes the server.
		/// </summary>
		/// <param name="maximumConnections">The maximum number of connections/players.</param>
		/// <param name="listenPort">The UDP port that server listens to it for client connections.</param>
		/// <example><code>
		/// void LaunchServer () {
		///    uLink.Network.incomingPassword = "uLinkIsAwesome";
		///    uLink.Network.InitializeServer(32, 25000);
		/// }
		/// </code></example>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors <see cref="uLink.NetworkConnectionError.NoError"/> is returned</returns>
		/// <remarks>When you want to connect to a server with a password, the client should send the password in <see cref="uLink.Network.Connect"/></remarks>
		public static NetworkConnectionError InitializeServer(int maximumConnections, int listenPort) { return _singleton.InitializeServer(maximumConnections, listenPort); }

		/// <summary>
		/// Initializes the server and the bool useProxy indicates if clients must connect via a proxy server.
		/// </summary>
		/// <param name="maximumConnections">Maximum number of connections/players</param>
		/// <param name="listenPort">The UDP port that server listens to it for client connections.</param>
		/// <param name="useProxy">Indicates wether clients should use a proxy server to connect to this server or not</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors <see cref="uLink.NetworkConnectionError.NoError"/> is returned
		/// </returns>
		/// <remarks>
		/// You should use a proxy server if you want to host your game on the internet on a machine which doesn't have a public address.
		/// See the master server manual page for more information.
		/// </remarks>
		public static NetworkConnectionError InitializeServer(int maximumConnections, int listenPort, bool useProxy) { return _singleton.InitializeServer(maximumConnections, listenPort, useProxy); }

		/// <summary>
		/// Initializes the server and the bool useProxy indicates if clients must connect via a proxy server.
		/// </summary>
		/// <param name="maximumConnections">Maximum number of connections which this server accept</param>
		/// <param name="listenStartPort">The starting port number which server tries to listen to.</param>
		/// <param name="listenEndPort">The ending port number which the server will try to listen to.</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors <see cref="uLink.NetworkConnectionError.NoError"/> is returned</returns>
		/// <remarks>
		/// This specific version of the method is specificly good if you want to start serveres automatically and can not be sure which 
		/// ports are open and aren't being used by other servers. 
		/// </remarks>
		public static NetworkConnectionError InitializeServer(int maximumConnections, int listenStartPort, int listenEndPort) { return _singleton.InitializeServer(maximumConnections, listenStartPort, listenEndPort, useProxy); }

		/// <summary>
		/// Initializes the server and the bool useProxy indicates if clients must connect via a proxy server.
		/// </summary>
		/// <param name="maximumConnections">Maximum number of connections which this server accept</param>
		/// <param name="listenStartPort">The starting port number which server tries to listen to.</param>
		/// <param name="listenEndPort">The ending port number which the server will try to listen to.</param>
		/// <param name="useProxy">Indicates wether clients should use a proxy server to connect to this server or not</param>
		/// <returns>One of the enum <see cref="uLink.NetworkConnectionError"/> values, 
		/// if no errors <see cref="uLink.NetworkConnectionError.NoError"/> is returned</returns>
		/// <remarks>
		/// This specific version of the method is specificly good if you want to start serveres automatically and can not be sure which 
		/// ports are open and aren't being used by other servers.
		/// You should use a proxy server if you want to host your game on the internet on a machine which doesn't have a public address.
		/// See the master server manual page for more information.
		/// </remarks>
		public static NetworkConnectionError InitializeServer(int maximumConnections, int listenStartPort, int listenEndPort, bool useProxy) { return _singleton.InitializeServer(maximumConnections, listenStartPort, listenEndPort); }

		/// <overloads>The most basic form of all overloaded Instantiate functions is 
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>. 
		/// Please read the documentation for that function first.
		/// </overloads>
		/// <summary>
		/// THE MOST BASIC FORM: Creates a network aware object. Instantiates the specified prefab.
		/// </summary>
		/// <remarks>
		/// We recommend registering the prefab with the utility script uLinkRegisterPrefabs before calling
		/// this method. Read more about this script in the uLink manual chapter about instatiating objects.
		/// <para>
		/// The given prefab will be instantiated on all clients and the server in the game.
		/// State synchronization is automatically set up so there is no extra work involved
		/// to start that. Internally in uLink, this is a reliable and buffered internal RPC call.
		/// </para>
		/// <para>
		/// All new clients connecting to the server at a later time will get this RPC so
		/// that the object is automatically instantiated right after the connection has
		/// been established (at those clients). This call will default to setting the
		/// caller (<see cref="uLink.NetworkPlayer">uLink.NetworkPlayer</see>) as owner and
		/// creator for this object. Read more about the three object roles in the uLink
		/// manual. 
		/// </para>
		/// <para>
		/// Be aware that it is possible to remove this buffered RPC by calling
		/// the <see cref="RemoveRPCs(uLink.NetworkPlayer)">uLink.Network.RemoveRPCs</see>
		/// function.</para>
		/// </remarks>
		/// <param name="prefab">The prefab to instantiate in server and all clients regardless of their role.</param>
		/// <param name="position">The position that the prefab will be instantiated.</param>
		/// <param name="rotation">The rotation of the object that will be instantiated.</param>
		/// <param name="group">The group number of the object.</param>
		/// <param name="initialData">Other initial data for this prefab that is needed
		/// right at the instantiation. Could be anything beside position and rotation. For
		/// example color, equipment, buffs, hitpoints, etc.</param>
		/// <returns>
		/// The newly instantiated GameObject.
		/// </returns>
		/// <example>
		/// <code>
		/// // Immediately instantiates a new connected player's character
		/// // when successfully connected to the server.
		/// // Note: The server is non-authoritative in this example.
		/// public Transform playerPrefab;
		/// 
		/// void uLink_OnConnectedToServer ()
		/// {
		///    uLink.Network.Instantiate(playerPrefab, transform.position, transform.rotation, 0); //0 means no group
		/// }
		/// </code>
		/// </example>
		public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(prefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except the fact that it accepts it's prefab as a component and returns that component of the instantiated GameObject.
		/// </summary>
		/// <remarks>When you want to send only one array as initialData, if you don't use the generic overload of Instantaite
		/// .NET's reflection capability will think that each of the array elements is an argument by itself, so if you send an int[3] it will consider it as (int,int,int)</remarks>
		/// <returns>The component of the instantiated prefab which has the type of the generic parameter.</returns>
		public static TComponent Instantiate<TComponent>(TComponent prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component { return _singleton.Instantiate(prefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except the prefab is specified as string (name of the prefab).
		/// </summary>
		/// <remarks>
		/// The prefab can be in the resources folder instead of being registered.
		/// </remarks>
		public static GameObject Instantiate(string prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(prefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except prefab is of type object.
		/// </summary>
		public static Object Instantiate(Object prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(prefab, position, rotation, group, initialData); }

		/// <summary>
		/// Creates a network aware object. Instantiates one prefab for proxies and another for the creator. Can be used for NPCs which are owned by the server.
		/// </summary>
		/// <remarks>This Instatiate is a more advanced form of 
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>.
		/// This Instantiate is perfect for creating NPCs in an authoritative server. 
		/// <para>
		/// When called on the server, the server will have two roles for this object, owner and creator. 
		/// All clients will have the role proxy for this object and they will all get an instantiated 
		/// prefab dictated by the argument proxyPrefab. The beauty of this is that the server side 
		/// instantiated GameObject can have more (sensitive and secret) properties that will not 
		/// be synchronized to clients.
		/// </para>
		/// <para>
		/// The rest of the arguments are handled just like in 
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>. 
		/// Read more about the three object roles in the uLink manual. 
		/// </para>
		/// </remarks>
		public static GameObject Instantiate(GameObject othersPrefab, GameObject ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(othersPrefab, ownerPrefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except proxyPrefab and ownerPrefab are components and the reutrning value is that component of the instantiated prefab.
		/// </summary>
		/// <remarks>When you want to send only one array as initialData, if you don't use the generic overload of Instantaite
		/// .NET's reflection capability will think that each of the array elements is an argument by itself, so if you send an int[3] it will consider it as (int,int,int)</remarks>
		///<returns>The component of the prefab which has the same type of the specified generic type.</returns>
		public static TComponent Instantiate<TComponent>(TComponent othersPrefab, TComponent ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component { return _singleton.Instantiate(othersPrefab, ownerPrefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except proxyPrefab and ownerPrefabs are passed as strings (GameObject names).
		/// </summary>
		/// <remarks>
		/// You can put the gameObject in the resources folder instead of registering it in the RegisterPrefabs component.
		/// </remarks>
		public static GameObject Instantiate(string othersPrefab, string ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(othersPrefab, ownerPrefab, position, rotation, group, initialData); }

		/// <summary>
		/// Creates a network aware object and owner is one the first argument. Instantiates the specified prefab. 
		/// </summary>
		/// <remarks>This Instantiate function adds an extra parameter to the basic
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>.
		/// "owner". This can be used to explicitly set the owner of this object to
		/// another <see cref="uLink.NetworkPlayer"/>. This Instantiate is perfect for creating player controlled objects 
		/// on the server. It can be used in both authoritative servers and non-authoritative servers. 
		/// <para>
		/// The rest of the arguments are handled just like in 
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>. 
		/// </para>
		/// <para>Read more about the three object roles in the uLink manual.
		/// </para>
		/// </remarks>
		public static GameObject Instantiate(NetworkPlayer owner, GameObject prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(owner, prefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except the fact that the prefab argument should be a component and the same component of the instantiated prefab will be returned.
		/// </summary>
		/// <remarks>When you want to send only one array as initialData, if you don't use the generic overload of Instantaite
		/// .NET's reflection capability will think that each of the array elements is an argument by itself, so if you send an int[3] it will consider it as (int,int,int)</remarks>
		/// <returns>The component of the instantiated prefab which has the type of the generic parameter.</returns>
		public static TComponent Instantiate<TComponent>(NetworkPlayer owner, TComponent prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component { return _singleton.Instantiate(owner, prefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except prefab argument is a string.
		/// </summary>
		/// <remarks>
		/// The prefab can be in the resources folder instead of being registered using the RegisterPrefabs component.
		/// </remarks>
		public static GameObject Instantiate(NetworkPlayer owner, string prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(owner, prefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except prefab argument is of type object.
		/// </summary>
		public static Object Instantiate(NetworkPlayer owner, Object prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(owner, prefab, position, rotation, group, initialData); }

		/// <summary>
		/// Creates a network aware object. Instantiates three different prefabs, for the owner, for the creator and for the proxies. 
		/// </summary>
		/// <remarks>This is a more advanced form of 
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>.
		/// This Instantiate is perfect for creating each player's avatar in an authoritative server. 
		/// When called on the server, the server will have the "creator" role for this object. The 
		/// "owner" role is given to the <see cref="uLink.NetworkPlayer"/> that will control this avatar. 
		/// All other clients will have the "proxy" role for this object.
		/// <para>The beauty of this is that the server side instantiated GameObject, the creator prefab, 
		/// can have more (sensitive and secret) 
		/// properties that should never be synchronized to any clients. 
		/// </para>
		/// <para>
		/// Also, if the game uses statesync between the creator and the owner, this statesync can send
		/// completely different properties compared to the statesync between the creator and the proxies.
		/// The prefabs need to be registered using the RegisterPrefab component.
		/// Read more on this topic in the messages <see cref="uLink_OnSerializeNetworkView"/> and 
		/// <see cref="uLink_OnSerializeNetworkViewOwner"/>. 
		/// </para>
		/// <para>
		/// The rest of the arguments are handled just like in 
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>. 
		/// </para>
		/// <para>
		/// Read more about the three object roles in the uLink manual. 
		/// </para>
		/// <para>
		/// It is possible to set proxyPrefab and/or ownerPrefab to null to never use that role. 
		/// One example when it is smart to set
		/// proxyPrefab no null is a situation when you want to statesync an object between the owner
		/// (a player) and the creator (the server), but no other clients should be aware this object. 
		/// </para>
		/// </remarks>
		public static GameObject Instantiate(NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except proxyPrefab, ownerPrefab and serverPrefab are of type component and the same component of the instantiated prefab will be returned.
		/// </summary>
		/// <remarks>When you want to send only one array as initialData, if you don't use the generic overload of Instantaite
		/// .NET's reflection capability will think that each of the array elements is an argument by itself, so if you send an int[3] it will consider it as (int,int,int)</remarks>
		public static TComponent Instantiate<TComponent>(NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except proxyPrefab, ownerPrefab and serverPrefab are string arguments
		/// and the prefabs can be in the resources folder.
		/// </summary>
		public static GameObject Instantiate(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData); }

		/// <summary>
		/// Advanced form: Should only be used if an allocated <see cref="uLink.NetworkViewID"/> is already known. 
		/// You should only use this in the specific cases which you want to implement specific pooling algorithms for networkviews
		/// and other places which normal Instantiate calls and the Instantiate poll utility script doesn't work on your case.
		/// </summary>
		public static GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(uLink.NetworkViewID,uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except proxyPrefab, ownerPrefab and serverPrefab are of type component and the same component of the instantiated prefab will be returned.
		/// </summary>
		/// <remarks>When you want to send only one array as initialData, if you don't use the generic overload of Instantaite
		/// .NET's reflection capability will think that each of the array elements is an argument by itself, so if you send an int[3] it will consider it as (int,int,int)</remarks>
		public static TComponent Instantiate<TComponent>(NetworkViewID viewID, NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate(uLink.NetworkViewID,uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/> except proxyPrefab, ownerPrefab and serverPrefab are strings.
		/// The prefabs can be in the resources folder as well as being registered using the RegisterPrefab component.
		/// </summary>
		public static GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData); }

		// TODO: document
		public static GameObject Instantiate(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData); }

		// TODO: document
		public static GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData); }

		// TODO: document
		public static GameObject Instantiate(NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, GameObject cellAuthPrefab, GameObject cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData); }

		// TODO: document
		public static GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, GameObject cellAuthPrefab, GameObject cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData); }

		// TODO: document
		public static TComponent Instantiate<TComponent>(NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, TComponent cellAuthPrefab, TComponent cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData); }

		// TODO: document
		public static TComponent Instantiate<TComponent>(NetworkViewID viewID, NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, TComponent cellAuthPrefab, TComponent cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData); }

		// TODO: document
		public static GameObject Instantiate<InitialData>(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, InitialData initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, (object)initialData); }

		// TODO: document
		public static GameObject Instantiate<InitialData>(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, InitialData initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, (object)initialData); }

		// TODO: document
		public static GameObject Instantiate<InitialData>(NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, GameObject cellAuthPrefab, GameObject cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, InitialData initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, (object)initialData); }

		// TODO: document
		public static GameObject Instantiate<InitialData>(NetworkViewID viewID, NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, GameObject cellAuthPrefab, GameObject cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, InitialData initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, (object)initialData); }

		// TODO: document
		public static TComponent Instantiate<TComponent, InitialData>(NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, TComponent cellAuthPrefab, TComponent cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, InitialData initialData) where TComponent : Component { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, (object)initialData); }

		// TODO: document
		public static TComponent Instantiate<TComponent, InitialData>(NetworkViewID viewID, NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, TComponent cellAuthPrefab, TComponent cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, InitialData initialData) where TComponent : Component { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, (object)initialData); }


		/// <overloads>The most basic form of all overloaded Instantiate functions is 
		/// <see cref="Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>. 
		/// Please read the documentation for that function first.
		/// </overloads>
		/// <summary>
		/// THE MOST BASIC FORM: Creates a network aware object. Instantiates the specified prefab.
		/// The initialData parameter is a generic parameter.
		/// </summary>
		/// <remarks>
		/// We recommend registering the prefab with the utility script uLinkRegisterPrefabs before calling
		/// this method. Read more about this script in the uLink manual chapter about instantiating objects.
		/// <para>
		/// The given prefab will be instantiated on all clients and the server in the game.
		/// State synchronization is automatically set up so there is no extra work involved
		/// to start that. Internally in uLink, this is a reliable and buffered internal RPC call.
		/// </para>
		/// <para>
		/// All new clients connecting to the server at a later time will get this RPC so
		/// that the object is automatically instantiated right after the connection has
		/// been established (at those clients). This call will default to setting the
		/// caller (<see cref="uLink.NetworkPlayer">uLink.NetworkPlayer</see>) as owner and
		/// creator for this object. Read more about the three object roles in the uLink
		/// manual. 
		/// </para>
		/// <para>
		/// Be aware that it is possible to remove this buffered RPC by calling
		/// the <see cref="RemoveRPCs(uLink.NetworkPlayer)">uLink.Network.RemoveRPCs</see>
		/// function.</para>
		/// </remarks>
		/// <param name="prefab">The prefab.</param>
		/// <param name="position">The position.</param>
		/// <param name="rotation">The rotation.</param>
		/// <param name="group">The group number.</param>
		/// <param name="initialData">Other initial data for this prefab that is needed. The data can be read in <see cref="uLink_OnNetworkInstantiate"/>
		/// right at the instantiation. Could be anything beside position and rotation. For
		/// example color, equipment, buffs, hitpoints, etc.</param>
		/// <returns>
		/// The newly instantiated GameObject.
		/// </returns>
		/// <example>
		/// <code>
		/// // Immediately instantiates a new connected player's character
		/// // when successfully connected to the server.
		/// // Note: The server is non-authoritative in this example.
		/// public Transform playerPrefab;
		/// 
		/// void uLink_OnConnectedToServer ()
		/// {
		///		//The uLink_OnNetworkInstantiate can read the initial data as a Vector2 instead of bitstream.
		///    uLink.Network.Instantiate<Vector2>(playerPrefab, transform.position, transform.rotation, 0,Vector2.right); 
		/// }
		/// </code>
		/// </example>
		public static GameObject Instantiate<TInitialData>(GameObject prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(prefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except prefab is of type component.
		/// </summary>
		public static TComponent Instantiate<TComponent, TInitialData>(TComponent prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) where TComponent : Component { return _singleton.Instantiate(prefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except prefab is of type string and can be in the resources folder.
		/// </summary>
		public static GameObject Instantiate<TInitialData>(string prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(prefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except prefab is of type object.
		/// </summary>
		public static Object Instantiate<TInitialData>(Object prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(prefab, position, rotation, group, (object)initialData); }

		/// <summary>
		/// Creates a network aware object. Instantiates one prefab for proxies and another for the creator. Can be used for NPCs.
		/// </summary>
		/// <remarks>This Instantiate is a more advanced form of 
		/// <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/>.
		/// This Instantiate is perfect for creating NPCs in an authoritative server. 
		/// <para>
		/// When called on the server, the server will have two roles for this object, owner and creator. 
		/// All clients will have the role proxy for this object and they will all get an instantiated 
		/// prefab dictated by the argument proxyPrefab. The beauty of this is that the server side 
		/// instantiated GameObject can have more (sensitive and secret) properties that will not 
		/// be synchronized to clients.
		/// </para>
		/// <para>
		/// The rest of the arguments are handled just like in 
		/// <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/>. 
		/// Read more about the three object roles in the uLink manual. 
		/// </para>
		/// </remarks>
		public static GameObject Instantiate<TInitialData>(GameObject proxyPrefab, GameObject ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(proxyPrefab, ownerPrefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except proxyPrefab and ownerPrefab are of type  component and the same component type of the created prefab is returned.
		/// </summary>
		public static TComponent Instantiate<TComponent, TInitialData>(TComponent proxyPrefab, TComponent ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) where TComponent : Component { return _singleton.Instantiate(proxyPrefab, ownerPrefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except proxyPrefab and ownerPrefab are of type string
		/// and can be in the resources folder instead of being registered using RegisterPrefabs component.
		/// </summary>
		public static GameObject Instantiate<TInitialData>(string proxyPrefab, string ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(proxyPrefab, ownerPrefab, position, rotation, group, (object)initialData); }

		/// <summary>
		/// Creates a network aware object and owner is one the first argument. Instantiates the specified prefab. 
		/// </summary>
		/// <remarks>This Instantiate function adds an extra parameter to the basic
		/// <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/>.
		/// "owner". This can be used to explicitly set the owner of this object to
		/// another <see cref="uLink.NetworkPlayer"/>. This Instantiate is perfect for creating player controlled objects 
		/// on the server. It can be used in both authoritative servers and non-authoritative servers. 
		/// <para>
		/// The rest of the arguments are handled just like in 
		/// <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/>. 
		/// </para>
		/// <para>Read more about the three object roles in the uLink manual.
		/// </para>
		/// </remarks>
		public static GameObject Instantiate<TInitialData>(NetworkPlayer owner, GameObject prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(owner, prefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except prefab is of type component
		/// and the same type component of the instantiated prefab is returned.
		/// </summary>
		public static TComponent Instantiate<TComponent, TInitialData>(NetworkPlayer owner, TComponent prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) where TComponent : Component { return _singleton.Instantiate(owner, prefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except prefab is of type string
		/// and can be in the resources folder instead of being registered in RegisterPrefabs component.
		/// </summary>
		public static GameObject Instantiate<TInitialData>(NetworkPlayer owner, string prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(owner, prefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except prefab is of type object.
		/// </summary>
		public static Object Instantiate<TInitialData>(NetworkPlayer owner, Object prefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(owner, prefab, position, rotation, group, (object)initialData); }

		/// <summary>
		/// Creates a network aware object. Instantiates three different prefabs, for the owner, for the creator and for the proxies. 
		/// </summary>
		/// <remarks>This is a more advanced form of 
		/// <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/>.
		/// This Instantiate is perfect for creating each player's avatar in an authoritative server. 
		/// When called on the server, the server will have the "creator" role for this object. The 
		/// "owner" role is given to the <see cref="uLink.NetworkPlayer"/> that will control this avatar. 
		/// All other clients will have the "proxy" role for this object.
		/// <para>The beauty of this is that the server side instantiated GameObject, the creator prefab, 
		/// can have more (sensitive and secret) 
		/// properties that should never be synchronized to any clients. 
		/// </para>
		/// <para>
		/// Also, if the game uses statesync between the creator and the owner, this statesync can send
		/// completely different properties compared to the statesync between the creator and the proxies. 
		/// Read more on this topic in the messages <see cref="uLink_OnSerializeNetworkView"/> and 
		/// <see cref="uLink_OnSerializeNetworkViewOwner"/>. 
		/// </para>
		/// <para>
		/// The rest of the arguments are handled just like in 
		/// <see cref="Instantiate<TInitialData>(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/>. 
		/// </para>
		/// <para>
		/// Read more about the three object roles in the uLink manual. 
		/// </para>
		/// <para>
		/// It is possible to set proxyPrefab and/or ownerPrefab to null to never use that role. 
		/// One example when it is smart to set
		/// proxyPrefab to null is a situation when you want to statesync an object between the owner
		/// (a player) and the creator (the server), but no other clients should be aware of this object. 
		/// </para>
		/// </remarks>
		public static GameObject Instantiate<TInitialData>(NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except proxyPrefab, ownerPrefab and serverPrefab are of type component
		/// and the component of the same type in the instantiated prefab is returned.
		/// </summary>
		public static TComponent Instantiate<TComponent, TInitialData>(NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) where TComponent : Component { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except proxyPrefab, ownerPrefab and serverPrefab are of type string
		/// and can be in the resources folder instead of being registered in the RegisterPrefabs component.
		/// </summary>
		public static GameObject Instantiate<TInitialData>(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, (object)initialData); }

		/// <summary>
		/// Advanced form: Should only be used if an allocated <see cref="uLink.NetworkViewID"/> is already known. Don't use it
		/// unless you have some specific need which can not be answered by other overloads
		/// and the instantiate pool utility script.
		/// </summary>
		public static GameObject Instantiate<TInitialData>(NetworkViewID viewID, NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(uLink.NetworkViewID,uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except proxyPrefab, ownerPrefab and serverPrefab are of type component
		/// and the component of the same type in the instantiated prefab is returned.
		/// </summary>
		public static TComponent Instantiate<TComponent, TInitialData>(NetworkViewID viewID, NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) where TComponent : Component { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, (object)initialData); }
		/// <summary>
		/// Same as <see cref="Instantiate<TInitialData>(uLink.NetworkViewID,uLink.NetworkPlayer,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,TInitialData)"/> except proxyPrefab, ownerPrefab and serverPrefab are strings
		/// and the prefabs can be in the resources folder instead of being registered using the RegisterPrefabs component.
		/// </summary>
		public static GameObject Instantiate<TInitialData>(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, TInitialData initialData) { return _singleton.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, (object)initialData); }


		/// <overloaded>Remove buffered RPCs</overloaded>
		/// <summary>
		/// Remove all buffered RPCs that was sent by target player (argument sender).
		/// </summary>
		/// <param name="sender">The NetworkPlayer that you want to remove its RPCs</param>
		/// <remarks>Does not remove buffered Instantiate RPCs.
		/// Usually you use it when a player disconnects or when the old RPCs are no longer meaningful (i.e. game level changed).
		/// </remarks>
		public static void RemoveRPCs(NetworkPlayer sender) { _singleton.RemoveRPCs(sender); }

		/// <summary>
		/// Remove all buffered RPCs that was sent by target player (argument sender) for just one <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		/// <param name="sender">The NetworkPlayer that you want to remove its RPCs</param>
		/// <param name="group">The network group that you want to remove its RPCs</param>
		/// <remarks>Does not remove buffered Instantiate RPCs.
		/// It's useful for culling and other scenarios which you add and remove players to/from groups much.
		/// </remarks>
		public static void RemoveRPCs(NetworkPlayer sender, NetworkGroup group) { _singleton.RemoveRPCs(sender, group); }

		/// <summary>
		/// Remove all buffered RPCs which belong to one viewID.
		/// </summary>
		/// <param name="viewID">The ViewID of the networked object that you want to remove buffered RPCs for.</param>
		/// <remarks>Does not remove buffered Instantiate RPCs.
		/// Usually you use it when your networked object has a state which old RPCs are no longer valid/useful..</remarks>
		public static void RemoveRPCs(NetworkViewID viewID) { _singleton.RemoveRPCs(viewID); }

		/// <summary>
		/// Remove all buffered RPCs (excluding those with manual viewIDs).
		/// </summary>
		/// <remarks>Does not remove buffered Instantiate RPCs. Also won't affect objects assigned manual ViewIDs. To remove buffered RPCs for them, see <see cref="uLink.Network.DestroyAll(System.Boolean)"/></remarks>
		public static void RemoveAllRPCs() { _singleton.RemoveAllRPCs(false); }

		/// <summary>
		/// Remove all buffered RPCs.
		/// </summary>
		/// <param name="includeManual">If true, also remove buffered RPCs for objects assigned manual ViewIDs; otherwise skip them.</param>
		/// <remarks>Does not remove buffered Instantiate RPCs.</remarks>
		public static void RemoveAllRPCs(bool includeManual) { _singleton.RemoveAllRPCs(includeManual); }

		/// <summary>
		/// Remove all RPCs which belongs to one group.
		/// </summary>
		/// <param name="group">The network group that you want to remove all of its RPCs for all players.</param>
		/// <remarks>
		/// This usually can be used when you want to use the group for something completely different and want to clean it up.
		/// </remarks>
		public static void RemoveRPCsInGroup(NetworkGroup group) { _singleton.RemoveRPCsInGroup(group); }

		/// <overloaded>Remove buffered RPCs by RPC name</overloaded>
		/// <summary>
		/// Remove all buffered RPCs which belong to viewIDs in the specificed group and has the specified name.
		/// </summary>
		/// <remarks>Does not remove buffered Instantiate RPCs.</remarks>
		public static void RemoveRPCsByName(NetworkGroup group, string rpcName) { _singleton.RemoveRPCsByName(group, rpcName); }

		/// <overloaded>Remove buffered RPCs by RPC name</overloaded>
		/// <summary>
		/// Remove all buffered RPCs which belong to one viewID and has the specified name.
		/// </summary>
		/// <remarks>Does not remove buffered Instantiate RPCs.</remarks>
		public static void RemoveRPCsByName(NetworkViewID viewID, string rpcName) { _singleton.RemoveRPCsByName(viewID, rpcName); }

		/// <summary>
		/// Remove all buffered RPCs that was sent by target player (argument sender) and has the specified name.
		/// </summary>
		/// <remarks>Does not remove buffered Instantiate RPCs.</remarks>
		public static void RemoveRPCsByName(NetworkPlayer sender, string rpcName) { _singleton.RemoveRPCsByName(sender, rpcName); }

		/// <summary>
		/// Remove all buffered RPCs that has the specified name.
		/// </summary>
		/// <remarks>Does not remove buffered Instantiate RPCs.</remarks>
		public static void RemoveRPCsByName(string rpcName) { _singleton.RemoveRPCsByName(rpcName); }

		/// <summary>
		/// Remove one instantiating RPC from the RPC buffer.
		/// </summary>
		/// <param name="viewID">The ViewID that you want to remove the instantiate calls that are for that network object</param>
		public static void RemoveInstantiate(NetworkViewID viewID) { _singleton.RemoveInstantiate(viewID); }
		/// <summary>
		/// Remove all instantiating RPC which has this sender from the RPC buffer.
		/// </summary>
		/// <param name="sender">The player that you want to remove all of its Instantiate RPCs</param>
		/// <remarks>
		/// The player who sends the RPC matters and not the owner.
		/// </remarks>
		public static void RemoveInstantiates(NetworkPlayer sender) { _singleton.RemoveInstantiates(sender); }
		/// <summary>
		/// Remove all instantiating RPC from RPC buffer with this sender and this group.
		/// </summary>
		/// <param name="sender">The player which you want to remove its instantiate RPCs</param>
		/// <param name="group">The group that the Instantiate call should be in it, to be removed</param>
		public static void RemoveInstantiates(NetworkPlayer sender, NetworkGroup group) { _singleton.RemoveInstantiates(sender, group); }
		/// <summary>
		/// Remove all instantiating RPCs from RPC buffer belonging to the group.
		/// </summary>
		/// <param name="group">The group that you want to remove all instantiate RPCs in it</param>
		public static void RemoveInstantiatesInGroup(NetworkGroup group) { _singleton.RemoveInstantiatesInGroup(group); }

		[Obsolete("Network.RemoveInstantiatingRPC is deprecated, please use Network.RemoveInstantiate instead")]
		public static void RemoveInstantiatingRPC(NetworkViewID viewID) { RemoveInstantiate(viewID); }

		/// <overloaded>Obsolete.</overloaded>
		[Obsolete("Network.RemoveInstantiatingRPCs is deprecated, please use Network.RemoveInstantiates instead")]
		public static void RemoveInstantiatingRPCs(NetworkPlayer owner) { RemoveInstantiates(owner); }

		[Obsolete("Network.RemoveInstantiatingRPCs is deprecated, please use Network.RemoveInstantiates instead")]
		public static void RemoveInstantiatingRPCs(NetworkPlayer owner, NetworkGroup group) { RemoveInstantiates(owner, group); }

		[Obsolete("Network.RemoveInstantiatingRPCsInGroup is deprecated, please use Network.RemoveInstantiatesInGroup instead")]
		public static void RemoveInstantiatingRPCsInGroup(NetworkGroup group) { RemoveInstantiatesInGroup(group); }

		/// <summary>
		/// Removes all instantiating RPCs from the RPC buffer regardless of the sender player and group.
		/// </summary>
		public static void RemoveAllInstantiates() { _singleton.RemoveAllInstantiates(); }

		/// <summary>
		/// Sets the flags for a network group.
		/// </summary>
		/// <param name="group">The group number which you want to change its settings</param>
		/// <param name="flags">The settings values which you want to set.</param>
		/// <remarks>
		/// This can be used to change the settings of the group. Using bitwise operators and the enum values you can
		/// turn on/off all settings as you do in other enums.
		/// <para>
		/// You can automatically add all new players to this group automatically. 
		/// You can set if you want to Hide gameobjects of the group from those
		/// who are not in the group or not.
		/// </para>
		/// <para>
		/// See the groups page in manual for more information.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// void Start()
		/// {
		///		uLink.SetGroupFlags(1,NetworkGroupFlags.AddNewPlayers);
		/// }
		/// </code>
		/// </example>
		public static void SetGroupFlags(NetworkGroup group, NetworkGroupFlags flags) { group.SetFlags(flags); }

		/// <summary>
		/// Gets the group settings.
		/// </summary>
		/// <param name="group">The group that you want to set its settings.</param>
		/// <returns>The <see cref="NetworkGroupFlags"/> representing the group's settings</returns>
		/// <remarks>See the groups manual page for more information</remarks>
		public static NetworkGroupFlags GetGroupFlags(NetworkGroup group) { return group.GetFlags(); }

		/// <summary>
		/// Adds a <see cref="uLink.NetworkPlayer"/> to a group.
		/// </summary>
		/// <param name="target">The player that you want to add to a specific group.</param>
		/// <param name="group">The group that you want to add a player to.</param>
		/// <remarks>
		/// A player is in a group if there is an object in the group which is owned by it or you add it explicitly to the group by this call.
		/// Usually this can be used to manage game sessions/network culling with groups.
		/// <para>
		/// See the manual page related to groups and the example project related to groups feature.
		/// </para>
		/// </remarks>
		/// <seealso cref="RemovePlayerFromGroup"/>
		public static void AddPlayerToGroup(NetworkPlayer target, NetworkGroup group) { _singleton.AddPlayerToGroup(target, group); }

		/// <summary>
		/// Removes a <see cref="uLink.NetworkPlayer"/> from a group
		/// </summary>
		/// <param name="target">The player that you want to remove from a group.</param>
		/// <param name="group">The group that you want to remove the player from.</param>
		/// <remarks>
		/// See the groups manual page for more information. You can check the example project related to groups as well. 
		/// </remarks>
		/// <seealso cref="AddPlayerToGroup"/>
		public static void RemovePlayerFromGroup(NetworkPlayer target, NetworkGroup group) { _singleton.RemovePlayerFromGroup(target, group); }

		/// <summary>
		/// Sends an RPC.
		/// </summary>
		/// <remarks>Works exactly like <see cref="NetworkView.RPC(System.String,uLink.NetworkPlayer,System.Object[])"/>.
		/// The only difference is that you should pass the ViewID of the object which you want to send the RPC to.
		/// </remarks>
		public static void RPC(NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args) { _singleton.RPC(viewID, rpcName, target, args); }

		/// <summary>
		/// Sends an RPC.
		/// </summary>
		/// <param name="viewID">ViewID of the networked object that you want to send the RPC to.</param>
		/// <param name="rpcName">Name of the RPC that you want to call</param>
		/// <param name="targets">The <see cref="uLink.NetworkPlayer"/>s that you want to send the RPC to them.</param>
		/// <param name="args">The arguments that you want to send to the RPC</param>
		/// <remarks>Works exactly like <see cref="NetworkView.RPC(System.String,IEnumerable&lt;uLink.NetworkPlayer&gt;,System.Object[])"/>.
		/// The only difference is that you should pass the ViewID of the object which you want to send the RPC to.
		/// </remarks>
		public static void RPC(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { _singleton.RPC(viewID, rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC.
		/// </summary>
		/// <remarks>Works exactly like <see cref="NetworkView.RPC(System.String,uLink.RPCMode,System.Object[])"/></remarks>
		public static void RPC(NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args) { _singleton.RPC(viewID, rpcName, mode, args); }
		/// <summary>
		/// Sends an RPC.
		/// </summary>
		/// <remarks>Works exactly like <see cref="NetworkView.RPC(uLink.NetworkFlags,System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public static void RPC(NetworkFlags flags, NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args) { _singleton.RPC(flags, viewID, rpcName, target, args); }

		/// <summary>
		/// Sends an RPC.
		/// </summary>
		/// <remarks>Works exactly like <see cref="NetworkView.RPC(uLink.NetworkFlags,System.String,IEnumerale&lt;uLink.NetworkPlayer&gt;,System.Object[])"/></remarks>
		public static void RPC(NetworkFlags flags, NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { _singleton.RPC(flags, viewID, rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC.
		/// </summary>
		/// <remarks>Works exactly like <see cref="NetworkView.RPC(uLink.NetworkFlags,System.String,uLink.RPCMode,System.Object[])"/></remarks>
		public static void RPC(NetworkFlags flags, NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args) { _singleton.RPC(flags, viewID, rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC over an unreliable channel in uLink.
		/// </summary>
		/// <remarks>Works exactly like <see cref="uLink.NetworkView.UnreliableRPC(System.String,uLink.NetworkPlayer,System.Object[])"/></remarks>
		public static void UnreliableRPC(NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args) { _singleton.UnreliableRPC(viewID, rpcName, target, args); }

		/// <summary>
		/// Sends an RPC over an unreliable channel in uLink.
		/// </summary>
		/// <remarks>Works exactly like <see cref="uLink.NetworkView.UnreliableRPC(System.String,IEnumerable&lt;uLink.NetworkPlayer&gt;,System.Object[])"/></remarks>
		public static void UnreliableRPC(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { _singleton.UnreliableRPC(viewID, rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC over an unreliable channel in uLink.
		/// </summary>
		/// <remarks>Works exactly like <see cref="uLink.NetworkView.UnreliableRPC(System.String,uLink.RPCMode,System.Object[])"/></remarks>
		public static void UnreliableRPC(NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args) { _singleton.UnreliableRPC(viewID, rpcName, mode, args); }

		/// <summary>
		/// Send an RPC.
		/// </summary>
		///<remarks>Works exactly like <see cref="uLink.NetworkView.UnencryptedRPC(System.String,uLink.NetworkPlayer,System.Object[])"</remarks>
		public static void UnencryptedRPC(NetworkViewID viewID, string rpcName, NetworkPlayer target, params object[] args) { _singleton.UnencryptedRPC(viewID, rpcName, target, args); }

		/// <summary>
		/// Send an RPC.
		/// </summary>
		///<remarks>Works exactly like <see cref="uLink.NetworkView.UnencryptedRPC(System.String,IEnumerable&lt;uLink.NetworkPlayer&gt;,System.Object[])"</remarks>
		public static void UnencryptedRPC(NetworkViewID viewID, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { _singleton.UnencryptedRPC(viewID, rpcName, targets, args); }
		/// <summary>
		/// Send an RPC.
		/// </summary>
		///<remarks>Works exactly like <see cref="uLink.NetworkView.UnencryptedRPC(System.String,uLink.RpcMode,System.Object[])"</remarks>
		public static void UnencryptedRPC(NetworkViewID viewID, string rpcName, RPCMode mode, params object[] args) { _singleton.UnencryptedRPC(viewID, rpcName, mode, args); }

		/// <summary>
		/// Sets the level prefix. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future. </remarks>
		[Obsolete("Network.SetLevelPrefix is not implemented yet.")]
		public static void SetLevelPrefix(int prefix) { _singleton.SetLevelPrefix(prefix); }

		/// <summary>
		/// Turn on or off the receiving of network traffic. Deprecated.
		/// </summary>
		/// <remarks>SetReceivingEnabled is deprecated, please use <see cref="uLink.Network.AddPlayerToGroup"/> or <see cref="uLink.Network.RemovePlayerFromGroup"/> instead.</remarks>
		[Obsolete("Network.SetReceivingEnabled is deprecated, please use Network.AddPlayerToGroup or Network.RemovePlayerFromGroup instead.")]
		public static void SetReceivingEnabled(NetworkPlayer player, NetworkGroup group, bool enabled) { _singleton.SetReceivingEnabled(player, group, enabled); }

		/// <summary>
		/// Turn on or off the sending of network traffic for one group only. Deprecated.
		/// </summary>
		/// <remarks>SetSendingEnabled is deprecated, please use <see cref="uLink.Network.AddPlayerToGroup"/> or <see cref="uLink.Network.RemovePlayerFromGroup"/> instead.</remarks>
		[Obsolete("Network.SetSendingEnabled is deprecated, please use Network.AddPlayerToGroup or Network.RemovePlayerFromGroup instead.")]
		public static void SetSendingEnabled(NetworkGroup group, bool enabled) { _singleton.SetSendingEnabled(group, enabled); }

		/// <summary>
		/// Turn on or off the sending of network traffic. Deprecated.
		/// </summary>
		/// <remarks>SetSendingEnabled is deprecated, please use <see cref="uLink.Network.AddPlayerToGroup"/> or <see cref="uLink.Network.RemovePlayerFromGroup"/> instead.</remarks>
		[Obsolete("Network.SetSendingEnabled is deprecated, please use Network.AddPlayerToGroup or Network.RemovePlayerFromGroup instead.")]
		public static void SetSendingEnabled(NetworkPlayer player, NetworkGroup group, bool enabled) { _singleton.SetSendingEnabled(player, group, enabled); }

		/// <summary>
		/// Test this machines network connection. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future. </remarks>
		[Obsolete("Network.TestConnection is not implemented yet.")]
		public static ConnectionTesterStatus TestConnection() { return TestConnection(false); }

		/// <summary>
		/// Test this machines network connection. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future. </remarks>
		[Obsolete("Network.TestConnection is not implemented yet.")]
		public static ConnectionTesterStatus TestConnection(bool forceTest) { return _singleton.TestConnection(forceTest); }

		/// <summary>
		/// Test the connecction specifically for NAT punchthrough connectivity. Not implemented yet.
		/// </summary>
		/// <remarks>Not implemented yet. If feature requested, will likely be part of uLink in the future. </remarks>
		[Obsolete("Network.TestConnectionNAT is not implemented yet.")]
		public static ConnectionTesterStatus TestConnectionNAT() { return _singleton.TestConnectionNAT(); }

		/// <summary>
		/// Gets or sets a <see cref="uLink.RPCTypeSafe"/> value indicating whether the parameters in RPCs will be type
		/// safe or not.
		/// </summary>
		/// <value>Type safety is set to <see cref="uLink.RPCTypeSafe.OnlyInEditor"/> by default,
		/// which means it will only be turned on when running uLink in the Unity editor, so that you can find
		/// bugs early in development. I.e. if you run uLink outside the editor it will be turned off by default.
		/// Change this value to overrule the default behavior.
		/// It is common to run clients outside the editor and the server in the editor,
		/// and uLink can handle this without problems, but in that case you will only get type saftey
		/// when sending from the server (in the editor) and not on RPCs that are sent from the clients (outside the editor).</value>
		/// <remarks>If type safety is turned on it will increase RPC's packet size by
		/// one extra byte for each RPC parameter, so that it can tell the receiver the
		/// expected <see cref="uLink.BitStreamTypeCode"/>. The purpose is to avoid subtile bugs like
		/// sending an integer but receiving it as a float. When type safety is
		/// turned on uLink will throw an exception when a RPC parameter
		/// doesn't match the recievers function declaration. The price for type
		/// safety is increased bandwidth.</remarks>
		public static RPCTypeSafe rpcTypeSafe { get { return _singleton.rpcTypeSafe; } set { _singleton.rpcTypeSafe = value; } }

		/// <summary>
		/// This <see cref="uLink.NetworkFlags"/> is the default set of flags which are used whenever you don't send specific flags to your RPC calls. 
		/// </summary>
		public static NetworkFlags defaultRPCFlags { get { return _singleton.defaultRPCFlags; } }

		/// <summary>
		/// Gets a value indicating whether this instance is a cell server.
		/// </summary>
		/// <remarks>Cell servers are game servers connected using PikkoServer in a distributed 
		/// system of game servers to make the virtual world seamless. Read more 
		/// about this in the PikkoServer MMO manual chapter.</remarks>
		public static bool isCellServer { get { return _singleton.isCellServer; } }

		/// <summary>
		/// Returns true if this is a uLink client or cell server, otherwise false.
		/// </summary>
		public static bool isClientOrCellServer { get { return _singleton.isClientOrCellServer; } }

		/// <summary>
		/// Returns true if this is a uLink server or cell server, otherwise false.
		/// </summary>
		public static bool isServerOrCellServer { get { return _singleton.isServerOrCellServer; } }

		/// <summary>
		/// Gets or sets a value indicating whether this instance has an authoritative server.
		/// </summary>
		/// <remarks>This is an important strategic choice when building a 
		/// multiplayer game. By making the server authoritative, clients are prevented from performing dangerous operations 
		/// such as sending RPC:s to other players (via the server). Authoritative servers will not receive statesync 
		/// from clients. Most commercial game servers are authoritative for security reasons.
		/// Keep in mind that when using an authoritative server, both client and server should set this to true.
		/// Read more in the uLink manual's section on Authoritative server.</remarks>
		public static bool isAuthoritativeServer { get { return _singleton.isAuthoritativeServer; } set { _singleton.isAuthoritativeServer = value; } }

		/// <summary>
		/// Dictates if the statesync sent to the owner will be different from proxy statesync and thus handled separately.
		/// </summary>
		/// <value>Default is true</value>
		/// <remarks>This value is only useful in an authoritative server. When <c>true</c>, the statesync to owner 
		/// will be sent separately and thus can be received separately. 
		/// Take a look at the callback <see cref="uLink_OnSerializeNetworkViewOwner"/>, it will be called on the 
		/// receiver that is the owner for the object and on the server sending this kind of statesync. 
		/// Statesync to proxy objects will be handled the normal way in <see cref="uLink_OnSerializeNetworkView"/>. 
		/// When this value is set to false, the callback <see cref="uLink_OnSerializeNetworkView"/> will be used for all statesyncs. 
		/// </remarks>
		public static bool useDifferentStateForOwner { get { return _singleton.useDifferentStateForOwner; } set { _singleton.useDifferentStateForOwner = value; } }

		/// <summary>
		/// Setting this value to true in the server causes all future connection attempts 
		/// to be automatically redirected.
		/// </summary>
		/// <remarks>All connection attempts to this server will be redirected to the server 
		/// specified by the <see cref="redirectIP"/> and <see cref="redirectPort"/> (and 
		/// optionally <see cref="redirectPassword"/>) properties.
		/// </remarks>
		public static bool useRedirect { get { return _singleton.useRedirect; } set { _singleton.useRedirect = value; } }
		/// <summary>
		/// The host domain name or IP to connect to when the <see cref="useRedirect"/> property is set to true.
		/// </summary>
		/// <remarks>
		/// If the redirect is to a uLink server with the same IP, only the port is different, 
		/// set this property to <see cref="System.Net.IPAddress.Any"/>.
		/// </remarks>
		public static string redirectIP { get { return _singleton.redirectIP; } set { _singleton.redirectIP = value; } }
		/// <summary>
		/// The password to use (if needed) when the <see cref="useRedirect"/> property is set to true.
		/// </summary>
		public static string redirectPassword { get { return _singleton.redirectPassword; } set { _singleton.redirectPassword = value; } }
		/// <summary>
		/// The port to connect to when the <see cref="useRedirect"/> property is set to true.
		/// </summary>
		public static int redirectPort { get { return _singleton.redirectPort; } set { _singleton.redirectPort = value; } }

		/// <summary>
		/// Gets the approval data sent from the server, that can be used in the client right after a connection is established
		/// </summary>
		/// <remarks>This is the <see cref="BitStream"/> the client can read when the approval data needs to be handled.
		/// The code for checking approvalData should always be placed in the callback <see cref="uLink_OnConnectedToServer"/>. 
		/// If you need to read the approvalData later (again) for some reason, it is available via this propery.
		/// </remarks>
		/// <example>ApprovalData can be anything the server sends to a newly connected client. It could be information about 
		/// which level to load and information about which RPC groups the client should avoid.</example>
		public static BitStream approvalData { get { return _singleton.approvalData; } }

		/// <summary>
		/// A client can get the loginData via this property, the loginData that 
		/// was included and sent to the server when Connect was called. 
		/// </summary>
		/// <remarks>
		/// The purpose of this property is that sometimes it is handy to to check 
		/// what data was sent to the server when the connection was made.</remarks>
		public static object[] loginData { get { return _singleton.loginData; } }

		/// <overloads>Redirects the client to another server. 
		/// Triggers the callback <see cref="uLink_OnRedirectingToServer"/> in the client.
		/// </overloads>
		/// <summary>
		/// Redirects the client to another server on the same host.
		/// </summary>
		/// <param name="target">The player that you want to tell him/her to redirect and connect to another server.</param>
		/// <param name="port">The port that the new server that you want to connect to is listening to.</param>
		/// <remarks>Use this function on the server to make a client 
		/// disconnect and after that connect to a new server.
		/// Triggers the callback <see cref="uLink_OnRedirectingToServer"/> in the client.
		/// <para>
		/// This can be used for multiple purposes from custom handover logic to upgrading servers.
		/// </para>
		/// </remarks>
		public static void RedirectConnection(NetworkPlayer target, int port) { RedirectConnection(target, port, String.Empty); }

		/// <summary>
		/// Redirects the client to another server on the same host.
		/// </summary>
		/// <remarks>Use this function on the server to make a client 
		/// disconnect and after that connect to a new server.
		/// Triggers the callback <see cref="uLink_OnRedirectingToServer"/> in the client.
		/// </remarks>
		public static void RedirectConnection(NetworkPlayer target, int port, string password) { _singleton.RedirectConnection(target, port, password); }

		/// <summary>
		/// Redirects the client to another server.
		/// </summary>
		/// <param name="target">The player that you want to redirect to another server.</param>
		/// <param name="redirectTo">The IP adress of the server that you want to connect to.</param>
		/// <remarks>Use this function on the server to make a client 
		/// disconnect and after that connect to a new server.
		/// Triggers the callback <see cref="uLink_OnRedirectingToServer"/> in the client.
		/// </remarks>
		public static void RedirectConnection(NetworkPlayer target, NetworkEndPoint redirectTo) { RedirectConnection(target, redirectTo, String.Empty); }

		/// <summary>
		/// Redirects the client to another server.
		/// </summary>
		/// <remarks>Use this function on the server to make a client 
		/// disconnect and after that connect to a new server.
		/// Triggers the callback <see cref="uLink_OnRedirectingToServer"/> in the client.
		/// </remarks>
		public static void RedirectConnection(NetworkPlayer target, NetworkEndPoint redirectTo, string password) { _singleton.RedirectConnection(target, redirectTo, password); }

		/// <summary>
		/// Redirects the client to another server.
		/// </summary>
		/// <remarks>Use this function on the server to make a client 
		/// disconnect and after that connect to a new server.
		/// Triggers the callback <see cref="uLink_OnRedirectingToServer"/> in the client.
		/// </remarks>
		public static void RedirectConnection(NetworkPlayer target, string host, int port) { RedirectConnection(target, host, port, String.Empty); }

		/// <summary>
		/// Redirects the client to another server.
		/// </summary>
		/// <remarks>Use this function on the server to make a client 
		/// disconnect and after that connect to a new server.
		/// Triggers the callback <see cref="uLink_OnRedirectingToServer"/> in the client.
		/// </remarks>
		public static void RedirectConnection(NetworkPlayer target, string host, int port, string password) { _singleton.RedirectConnection(target, host, port, password); }

		/// <summary>
		/// Specifies a user-defined public RSA key. This method should be used on the client-side only.
		/// </summary>
		/// <remarks>
		/// By setting the public key that corresponds to the server all connection attempts from this 
		/// client will be encrypted and the connection
		/// will only be successful to a server with the corresponding private key.
		/// See the manual page about network security and encryption.
		/// </remarks>
		public static PublicKey publicKey { get { return _singleton.publicKey; } set { _singleton.publicKey = value; } }

		/// <summary>
		/// Gets of sets a user-defined private RSA key. This should be set on the server-side only.
		/// </summary>
		/// <remarks>
		/// See manual pages for security and encryption for more information. 
		/// The key should not be accessible to the clients.
		/// You should call this in a code which only compiles for the server andthe string containing 
		/// the key should not be in the files
		/// which clients can access, otherwise they'll be able to use it on their own cheated servers as well.
		/// </remarks>
		public static PrivateKey privateKey { get { return _singleton.privateKey; } set { _singleton.privateKey = value; } }

		/// <summary>
		/// Initializes security for all current and future players on the server.
		/// </summary>
		/// <remarks>
		/// Use this only on the server. <see cref="uLink_OnSecurityInitialized"/> callback is sent as security is turned on for each players session.
		/// Security in clients can be turned on before connecting by assigning <see cref="uLink.Network.publicKey"/>.
		/// <para>
		/// Keep in mind that you don't have to and should not send everything in secure mode and you can use <see cref="uLink.NetworkFlags"/> to send RPCs
		/// in secure or insecure mode.
		/// </para>
		/// </remarks>
		/// <seealso cref="UnInitializeSecurity()"/>
		public static void InitializeSecurity() { InitializeSecurity(true); }

		/// <summary>
		/// Initializes security for all future players and optionally already connected players.
		/// </summary>
		/// <param name="includingCurrentPlayers">Whether or not to intialize security for already connected players.</param>
		/// <remarks>
		/// Use this only on the server. <see cref="uLink_OnSecurityInitialized"/> callback is sent as security is turned on for each players session.
		/// Security in clients can be turned on before connecting by assigning <see cref="uLink.Network.publicKey"/>.
		/// <para>
		/// Keep in mind that you don't have to and should not send everything in secure mode and you can use <see cref="uLink.NetworkFlags"/> to send RPCs
		/// in secure or insecure mode.
		/// </para>
		/// </remarks>
		/// <see cref="UnInitializeSecurity(bool)"/>
		public static void InitializeSecurity(bool includingCurrentPlayers) { _singleton.InitializeSecurity(includingCurrentPlayers); }

		/// <summary>
		/// Initializes security for a specific player.
		/// </summary>
		/// <remarks>
		/// Use this only on the server. <see cref="uLink_OnSecurityInitialized"/> callback is sent as security is turned on for each players session.
		/// Security in clients can be turned on before connecting by assigning <see cref="uLink.Network.publicKey"/>.
		/// <para>
		/// Keep in mind that you don't have to and should not send everything in secure mode and you can use <see cref="uLink.NetworkFlags"/> to send RPCs
		/// in secure or insecure mode.
		/// </para>
		/// </remarks>
		///<seealso cref="UnInitializeSecurity(uLink.NetworkPlayer)"/>
		public static void InitializeSecurity(NetworkPlayer target) { _singleton.InitializeSecurity(target); }

		/// <summary>
		/// Removes security for all current and future players.
		/// </summary>
		/// <remarks>
		/// Use this only on the server. <see cref="uLink_OnSecurityUninitialized"/>
		/// callbacks are sent as security is removed from each players session.
		/// </remarks>
		/// <seealso cref="InitializeSecurity()"/>
		public static void UninitializeSecurity() { UninitializeSecurity(true); }

		/// <summary>
		/// Remove security for all future players and optionally already connected players.
		/// </summary>
		/// <param name="includingCurrentPlayers">Whether or not to remove security for already connected players.</param>
		/// <remarks>
		/// Use this only on the server. <see cref="uLink_OnSecurityUninitialized"/> callback is sent as security is removed from each players session.
		/// </remarks>
		/// <seealso cref="InitializeSecurity(bool)"/>
		public static void UninitializeSecurity(bool includingCurrentPlayers) { _singleton.UninitializeSecurity(includingCurrentPlayers); }

		/// <summary>
		/// Removes security for a specific player.
		/// </summary>
		/// <remarks>
		/// Use this only on the server. <see cref="uLink_OnSecurityUninitialized"/> callback is sent once security is removed from the players session.
		/// </remarks>
		/// <seealso cref="InitializeSecurity(uLink.NetworkPlayer)"/>
		public static void UninitializeSecurity(NetworkPlayer target) { _singleton.UninitializeSecurity(target); }

		/// <summary>
		/// Starts the server as a cell server and connects it to PikkoServer.
		/// </summary>
		/// <remarks>See PikkoServer MMO manual page for more information.</remarks>
		public static NetworkConnectionError InitializeCellServer(int maxClients, string pikkoServerHost) { return InitializeCellServer(maxClients, pikkoServerHost, Constants.DEFAULT_PIKKO_REMOTE_PORT); }
		/// <summary>
		/// Starts the server as a cell server and connects it to PikkoServer. It tries addresses one by one to see, where it can connect to.
		/// </summary>
		/// <remarks>See PikkoServer MMO manual page for more information.</remarks>
		public static NetworkConnectionError InitializeCellServer(int maxClients, string[] pikkoServerHosts) { return InitializeCellServer(maxClients, pikkoServerHosts, Constants.DEFAULT_PIKKO_REMOTE_PORT); }
		/// <summary>
		/// Starts the server as a cell server and connects it PikkoServer.
		/// </summary>
		/// <remarks>See PikkoServer MMO manual page for more information.</remarks>
		public static NetworkConnectionError InitializeCellServer(int maxClients, NetworkEndPoint pikkoServer) { return _singleton.InitializeCellServer(maxClients, pikkoServer, String.Empty); }
		/// <summary>
		/// Starts the server as a cell server and connects it PikkoServer.
		/// </summary>
		/// <remarks>See PikkoServer MMO manual page for more information.</remarks>
		public static NetworkConnectionError InitializeCellServer(int maxClients, string pikkoServerHost, int remotePort) { return _singleton.InitializeCellServer(maxClients, pikkoServerHost, remotePort, String.Empty); }
		/// <summary>
		/// Starts the server as a cell server and connects it PikkoServer.
		/// </summary>
		/// <remarks>See PikkoServer MMO manual page for more information.</remarks>
		public static NetworkConnectionError InitializeCellServer(int maxClients, string[] pikkoServerHosts, int remotePort) { return _singleton.InitializeCellServer(maxClients, pikkoServerHosts, remotePort, String.Empty); }

		/// <summary>
		/// Gets or sets the emulation of network problems like max bandwidth, packet loss, duplicate packets and latecy fluctuations.
		/// </summary>
		/// <remarks>
		/// Use this to test your game with some network issues that are common in the real world before releasing your game. 
		/// </remarks>
		public static NetworkEmulation emulation { get { return _singleton.emulation; } }

		/// <summary>
		/// Gets or sets configuration for low-level connection parameters, like timeouts, handshake attempts and ping frequency.
		/// </summary>
		/// <remarks>
		/// Use this to fine-tune the behavior of the underlying uLink protocol for maximum networking performance.
		/// </remarks>
		public static NetworkConfig config { get { return _singleton.config; } }

		/// <summary>
		/// Gets or sets the loglevel for uLink logging.
		/// </summary>
		[Obsolete("Network.logLevel is deprecated, please use NetworkLog.minLevel instead")]
		public static NetworkLogLevel logLevel { get { return Log.minLevel; } set { Log.minLevel = value; } }

		/// <summary>
		/// This method retries to update the Network time of the clients with the server. 
		/// It only should be called on the client.
		/// </summary>
		/// <param name="durationInSeconds">The duration that it can take to synchronize the time.</param>
		[Obsolete("Network.ResynchronizeClock is deprecated, time is automatically synchronized continuously")]
		public static void ResynchronizeClock(double durationInSeconds) {}

		[Obsolete("Network.ResynchronizeClock is deprecated, time is automatically synchronized continuously")]
		public static void ResynchronizeClock(ulong intervalMillis) {}

		[Obsolete("Network.cellPosition is deprecated, please use PikoServer.cellPosition instead")]
		public static Vector3 cellPosition { get { return PikkoServer.cellPosition; } }

#if UNITY_DOC

		/// <summary>
		/// Message callback: Called on the server whenever a <see cref="O:InitializeServer"/> 
		/// has completed.
		/// </summary>
		/// <remarks>
		/// Use this callback to write server side code that should be run after 
		/// the network socket has been opened.
		/// </remarks>
		/// <example><code>
		/// // C# code example
		/// void uLink_OnServerInitialized() {
		///    Debug.Log("Server initialized and ready");
		/// }
		/// </code></example>
		public static void uLink_OnServerInitialized() { }

		/// <summary>
		/// Message callback: Called on the server whenever a <see cref="O:Disconnect"/> 
		/// was invoked and has completed.
		/// </summary>
		/// <remarks>
		/// Use this callback to write server side code that should be run
		/// after the network connection has been shutdown.
		/// </remarks>
		/// <example><code>
		/// // C# code example
		/// void uLink_OnServerUninitialized() {
		///    Debug.Log("Server uninitialized");
		///	   //Save data to database and then quit...
		/// }
		/// </code></example>
		public static void uLink_OnServerUninitialized() { }

		/// <summary>
		/// Message callback: Called on the server whenever a new player has successfully connected.
		/// </summary>
		/// <remarks>Use this callback to write server side code for newly connected players.</remarks>
		/// <example><code>
		/// // C# code example
		/// private int playerCount = 0;
		/// 
		/// void uLink_OnPlayerConnected(uLink.NetworkPlayer player) {
		///    playerCount += 1;
		///    Debug.Log("Player " + playerCount.ToString() +
		///    " connected from " + player.ipAddress.ToString() +
		///    ":" + player.port.ToString());
		/// }
		/// </code></example>
		public static void uLink_OnPlayerConnected(NetworkPlayer player) { }

		/// <summary>
		/// Message callback: Called on the client when it has successfully connected to a server
		/// </summary>
		/// <remarks>Use this callback to check the approvalData sent from the server and anything else which requires to be done after connecting to server.</remarks>
		/// <example>
		/// This example works nicely with the example code in <see cref="uLink_OnPlayerApproval"/>.
		/// <code>
		/// // C# code example 
		/// void uLink_OnConnectedToServer() {
		///    Debug.Log("Connected to server");
		///    // Will check the number of the level to load. 
		///    // The server in this example sends level as an int.
		///    var levelNumber =  uLink.Network.approvalData.Read&lt;int&gt;();
		///    Debug.Log("Got level number " + levelNumber + " from server");
		///    // Use the levelNumber to load the correct things...
		/// }
		/// </code></example>
		public static void uLink_OnConnectedToServer(uLink.NetworkEndPoint server) { }

		/// <summary>
		/// Message callback: Called in a uLink cell server when it has connected to PikkoServer.
		/// </summary>
		/// <remarks>Only used in a PikkoServer environment.</remarks>
		public static void uLink_OnConnectedToPikkoServer(bool isFirstCellServer) { }

		/// <summary>
		/// Message callback: Called on the server whenever a player disconnects from the server.
		/// </summary>
		/// <remarks>This callback is activated in two situations: When the player disconnects in a 
		/// controlled manner and when a player is disconnected by a 
		/// <see cref="uLink.NetworkConfig.timeoutDelay">timeout</see> in the server indicating 
		/// that the client hasn't responded at all for a while.
		/// </remarks>
		/// <example><code>
		/// // C# code example
		/// void uLink_OnPlayerDisconnected(uLink.NetworkPlayer player) {
		///    Debug.Log("Cleaning up after player " + player.ToString());
		///    // Remove all the player's objects from the live game and remove 
		///    // buffered RPCs for these objects from the uLink RPC buffer. 
		///    uLink.Network.DestroyPlayerObjects(player);
		/// }
		/// </code></example>
		public static void uLink_OnPlayerDisconnected(NetworkPlayer player) { }

		/// <summary>
		/// Message callback: Called on the client when disconnected from server.
		/// </summary>
		/// <remarks>
		/// Called on the client when the connection was lost or you disconnected from the server. 
		/// The <see cref="uLink.NetworkDisconnection"/> enum will indicate if the connection was cleanly 
		/// disconnected or if the connection was lost (default timeout 6 sec).
		/// </remarks>
		/// <example><code>
		/// // C# code example
		/// void uLink_OnDisconnectedFromServer(uLink.NetworkDisconnection info) {
		///    if (info == uLink.NetworkDisconnection.LostConnection)
		///       Debug.Log("Lost connection to the server after timeout");
		///    else
		///       Debug.Log("Successfully dissconnected from the server");
		/// }
		/// </code></example>
		public static void uLink_OnDisconnectedFromServer(NetworkDisconnection mode) { }

		/// <summary>
		/// Called when you disconnect from PikkoServer 
		/// </summary>
		public static void uLink_OnDisconnectedFromPikkoServer(NetworkDisconnection mode) { }

		/// <summary>
		/// Message callback: Called on the client when a connection attempt fails for some reason.
		/// </summary>
		/// <param name="error">The failure reason.</param>
		/// <example><code>
		/// void uLink_OnFailedToConnect(uLink.NetworkConnectionError error)
		/// {
		///    Debug.Log("Could not connect to server: "+ error);
		/// }
		/// </code></example>
		public static void uLink_OnFailedToConnect(NetworkConnectionError error) { }

		/// <summary>
		/// Called on clients or servers when reporting events from the <see cref="uLink.MasterServer"/>. 
		/// </summary>
		/// <remarks>
		/// Like, for example, when a host list has been received or host registration succeeded.
		/// </remarks>
		/// <example>
		/// You have to start a stand-alone Masterserver at port 38001 at the localhost for 
		/// this example code to work.
		/// <code>
		/// void Start() {
		///    uLink.Network.InitializeServer(32, 25000);
		/// }
		///
		/// void uLink_OnServerInitialized() {
		///     ulink.MasterServer.ipAddress = "127.0.0.1";
		///     ulink.MasterServer.port = 38001;
		///     uLink.MasterServer.RegisterHost( "MyGameVer1.0.0_42"
		///    , "My Game Instance", "This is a comment and place to store data");
		/// }
		///
		/// void uLink_OnMasterServerEvent(uLink.MasterServerEvent msEvent) {
		///    if (msEvent == uLink.MasterServerEvent.RegistrationSucceeded) {
		///       Debug.Log("Server registered");
		///    }
		/// }
		/// </code></example>
		public static void uLink_OnMasterServerEvent(MasterServerEvent msEvent) { }

		/// <summary>
		/// Message callback: Called when a connection attempt to a Master Server fails for some reason.
		/// </summary>
		/// <param name="error">The error.</param>
		public static void uLink_OnFailedToConnectToMasterServer(NetworkConnectionError error) { }

		/// <summary>
		/// Message callback: Called when a connection attempt to a PikkoServer fails for some reason.
		/// </summary>
		/// <param name="error">The error.</param>
		/// <remarks>Will be used for the uLink with PikkoServer. 
		/// PikkoServer is the server side product for building server clusters for 
		/// seamless virtual worlds with high player densities. PikkoServer is built by the same team as 
		/// uLink. uLink clients and uLink servers can connect to PikkoServer just like a connection to normal 
		/// uLink server, but now the server can handle more than a million of uLink network packages per second.</remarks>
		public static void uLink_OnFailedToConnectToPikkoServer(NetworkConnectionError error) { }

		/// <summary>
		/// Message callback: Called on objects which have been network instantiated with Network.Instantiate
		/// </summary>
		/// <remarks> This is the place to handle initialData. If the Instantiate function was called with initialData,
		/// that data can be retrieved here as a <see cref="BitStream"/> via info.networkView.initialData. 
		/// This callback is also useful for disabling or enabling components for this replica of the network aware object 
		/// which have just been instantiated, and some game logic details are supposed to depend on things like if this 
		/// peer is the proxy, the owner or the creator. Usually it's the best if you completely separate the prefabs for different roles however
		/// and don't enable/disable components for roles. 
		/// </remarks>
		/// <example><code>
		/// // C# code example for receiving initialData
		/// void uLink_OnNetworkInstantiate (uLink.NetworkMessageInfo info) {
		///    Color avatarColor;       
		///    avatarColor = info.networkView.initialData.Read&lt;Color&gt;();
		///    Debug.Log("Got Color " + avatarColor); 
		///    Debug.Log("The owner is " + info.networkView.owner);
		///    Debug.Log("The creator is " + info.networkView.creator); 
		///    Debug.Log("And I am " + uLink.Network.player);
		/// }
		/// </code></example>
		public static void uLink_OnNetworkInstantiate(NetworkMessageInfo info) { }

		/// <summary>
		/// Message callback: Used to customize synchronization of variables in a script observed by a network view component.
		/// </summary>
		/// <remarks>
		/// This callback is used for writing custom code to serialize and deserialize the state of game objects
		/// and let uLink send the serialized state over the network.
		/// <para>
		/// uLink automatically determines if the variables should be serialized or deserialized.
		/// The programer only needs to check what is going on by checking if 
		/// <see cref="uLink.BitStream.isWriting"/> is true or false. 
		/// </para>
		/// <para>
		/// In an authoritative server the sender is always the server. 
		/// In an authoritative server this callback will always perform proxy statesync to clients. 
		/// If <see cref="useDifferentStateForOwner"/> is set to <c>true</c>, which is deafult, then 
		/// owner statesync is handled in another callback: 
		/// <see cref="uLink_OnSerializeNetworkViewOwner(uLink.BitStream,uLink.NetworkMessageInfo)"/>. 
		/// </para>
		/// <para>
		/// With a non-authoritative server uLink_OnSerializeNetworkView is 
		/// the only callback you can use for serialization. 
		/// In that case the sender and receiver depends on who is the owner for the object, i.e. the 
		/// owner sends and all other receive (including the server unless it is the owner). 
		/// </para>
		/// </remarks>
		/// <example>
		/// This is a C# example code for serialization of an object's position and a health property.
		/// Put this code in a script. Make sure the observed property of the uLink.NetworkView 
		/// component attached to the Game Object is
		/// set to the script containing this code. Attach this script to the Game Object (or prefab) and 
		/// then drag the script in the inspector view to the observed property of the uLink.NetworkView 
		/// component attached to the same Game Object. 
		/// <para>Make sure this script code is available in both the sender and the receiver. One easy way 
		/// of doing that is to attach this script code to the prefab used for the proxy (the receiver) and 
		/// also to the prefab used for the creator (the sender).
		/// </para>
		/// <code>
		/// public int currentHealth;  
		/// 
		/// void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
		/// {
		/// if (stream.isWriting)
		///    {
		///       //Code executed in the sender
		///       stream.Write(transform.position);
		///       stream.Write(currentHealth);
		///    }
		///    else
		///    {
		///       //Code executed in the receiver
		///       transform.position = stream.Read&lt;Vector3&gt;();
		///       currentHealth = stream.Read&lt;int&gt;();
		///    }
		/// }
		/// </code>
		/// This example is javascript code. Note that javascript in Unity can handle generic types. 
		/// The example serializes the position of the object and a score property.
		/// <code>
		/// var score : int; 
		///  
		/// function uLink_OnSerializeNetworkView(stream : uLink.BitStream, 
		///    info : uLink.NetworkMessageInfo) 
		/// { 
		///    if (stream.isWriting) 
		///    { 
		///       stream.Write.&lt;Vector3&gt;(transform.position);
		///       stream.Write.&lt;int&gt;(score);   
		///    } 
		///    else 
		///    { 
		///       transform.position = stream.Read.&lt;Vector3&gt;();
		///       score = stream.Read.&lt;int&gt;();
		///    } 
		/// } 
		/// </code>
		/// </example>
		public static void uLink_OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) { }

		/// <summary>
		/// Message callback: Used to customize synchronization of variables in a script observed by a network view component.
		/// </summary>
		/// <remarks>
		/// This callback is convenient to use since the statesync sent from the server to the owner of an 
		/// object is often very different from statesync sent to proxy objects. Information that are private 
		/// to the owner of the object can be synchronized between the server and the owner via this callback.
		/// Example of private information is score, ammo, fatigue, health, or whatever fits the game. 
		/// <para>
		/// It is important to understand that this callback should not be used to send position, 
		/// rotation or keyboard input from the client to the server. We recommend using RPCs for sending 
		/// those things from a client to a server.
		/// </para>
		/// The owner and proxy roles are explained in the uLink manual chapter covering object instantiation.
		/// <para>
		/// Read more about proxy statesync in the callback <see cref="uLink_OnSerializeNetworkView"/>.
		/// </para>
		/// <para>
		/// This callback can only be used in an authoritative server.
		/// </para>
		/// <para>uLink automatically determines if the variables should be serialized or deserialized.
		/// The programer only needs to check what is going on by checking if 
		/// <see cref="uLink.BitStream.isWriting"/> is true or false. 
		/// </para>
		/// <para>The sender is always the server. This callback can only be used 
		/// when <see cref="useDifferentStateForOwner"/> is set to <c>true</c>, which is the default value.
		/// </para>
		/// </remarks>
		/// <example>
		/// This example is C# code.
		/// <code>
		/// public int currentHealth;  
		/// public ulong score;
		/// public short ammo;
		/// 
		/// void uLink_OnSerializeNetworkViewOwner(uLink.BitStream stream, uLink.NetworkMessageInfo info)
		/// {
		/// if (stream.isWriting) //Always is server
		///    {
		///       //Code executed on the sender = the creator prefab
		///       stream.Write(currentHealth);
		///       stream.Write(score);
		///       stream.Write(ammo);
		///    }
		///    else //Always is the network player who is the owner.
		///    {
		///       //Code executed on the receiver = the owner prefab
		///       currentHealth = stream.Read&lt;int&gt;();
		///       score = stream.Read&lt;ulong&gt;();
		///       ammo = stream.Read&lt;short&gt;();
		///    }
		/// }
		/// </code>
		/// </example>
		public static void uLink_OnSerializeNetworkViewOwner(BitStream stream, NetworkMessageInfo info) { }

		// TODO: document!
		public static void uLink_OnSerializeNetworkViewCellProxy(BitStream stream, NetworkMessageInfo info) { }

		/// <summary>
		/// Message callback: Allows newly connected client to check the RPC
		/// buffer before it is executed in the client.
		/// </summary>
		/// <remarks>
		/// If it is important in the game client to handle all buffered RPCs in
		/// a controlled manner this callback can be used. The code example
		/// below, written in C#, catches this message and does not execute any
		/// custom buffered RPCs, because <see cref="NetworkBufferedRPC.DontExecuteOnConnected"/> is called
		/// on every custom buffered RPC. Manual execution (see 
		/// uLink.NetworkBufferedRPC.<see cref="NetworkBufferedRPC.ExecuteNow"/>) 
		/// can be handy if you want to
		/// execute some of the RPCs in the RPC buffer at a later time in the client. 
		/// Only use this code
		/// example for debugging or if you need to examine some buffered RPC
		/// for some reason in a specific game situation. Remember that the
		/// uLink internal RPC for instantiating network aware objects will be
		/// included in the array.</remarks>
		/// <example>
		/// This example is C# code.<code>
		/// void uLink_OnPreBufferedRPCs(uLink.NetworkBufferedRPC[] bufferedArray)
		/// {
		///    Debug.Log("Message uLink_OnPreBufferedRPCs was detected!");
		///    foreach (uLink.NetworkBufferedRPC rpc in bufferedArray)
		///    {
		///       if (rpc.isInstantiate) 
		///       {
		///          Debug.Log("Got bufferd instantiate");
		///       } 
		///       else
		///       {
		///           Debug.Log("Found RPC with name = " + rpc.rpcName);
		///           rpc.DontExecuteOnConnected();
		///           //Write more logic here for doing manual execution later...
		///       }
		///    }
		/// }	
		///
		/// </code></example>
		public static void uLink_OnPreBufferedRPCs(NetworkBufferedRPC[] rpcs) { }

		/// <summary>
		/// Message callback: Called on the client when the server has ordered
		/// the client to reconnect to another server.
		/// </summary>
		/// <param name="newServer">The new server.</param>
		/// <remarks>A uLink client can receive an internal RPC from a uLink
		/// server with an order to disconnect and reconnect to another server.
		/// This event happens during a handover and it can also be triggered by
		/// calling 
		/// <see cref="RedirectConnection(uLink.NetworkPlayer,System.Net.NetworkEndPoint)"/>
		/// on the server. Read more about handovers in the manual section on
		/// Peer-To-Peer.
		/// </remarks>
		/// public static void uLink_OnRedirectingToServer(System.Net.NetworkEndPoint newServer) { }
		public static void uLink_OnRedirectingToServer(uLink.NetworkEndPoint newServer) { }

		/// <summary>
		/// Message callback: Called on both the client and server when security setup is finished for a player.
		/// </summary>
		public static void uLink_OnSecurityInitialized(NetworkPlayer player) { }

		/// <summary>
		/// Message callback: Called on both the client and server when security has been removed for a player.
		/// </summary>
		public static void uLink_OnSecurityUninitialized(NetworkPlayer player) { }

		/// <summary>
		/// Message callback: Called on the server when a client is trying to
		/// connect to the server.
		/// </summary>
		/// <remarks>Use this callback to check the loginData sent from a
		/// client. Use the content of the loginData to decide if the
		/// connection is OK or not OK. Also put the code to send <see cref="approvalData"/> back to the client
		/// in this callback.
		/// 
		/// If the the connection is not approved, a 
		/// <see cref="uLink_OnFailedToConnect(uLink.NetworkConnectionError)"/> notification 
		/// will be invoked on the client</remarks>
		/// <example>
		/// Examples of loginData is IP, user name and password, client version and/or team membership
		/// requests. An example of <see cref="approvalData"/>, sent to the client, is level
		/// number to load. To read the <see cref="approvalData"/> on the client side, take a look at the code example in
		/// <see cref="uLink_OnPlayerConnected(uLink.NetworkPlayer)"/>.
		/// <para>
		/// Usually checks for acceptance of players can be done in uLobby as well and then you can use this callback for other stuff.
		/// </para>
		/// <code>
		/// //Server side javascript code
		/// void uLink_OnPlayerApproval(uLink.NetworkPlayerApproval approval) {
		///    Debug.Log("Player wants to connect. Will check team number requested by client first.");
		///    int firstLevel = 1;
		///    var teamNumber = approval.loginData.Read&lt;int&gt;(); 
		///    if (teamNumber == 3) //Change to your own verification logic
		///    {
		///       Debug.Log("Client is approved.");
		///       approval.Approve(firstLevel); 
		///       //Player is now connected. The integer 1 will be sent as approvalData to the client.
		///    }
		///    else
		///    {       
		///       //All other team numbers are not available in this example. 
		///       Debug.Log("Client connection attempt was denied.");
		///       approval.Deny(uLink.NetworkConnectionError.UserDefined1);  
		///	      //you can use uLink.NetworkConnectionError.UserDefined1+x with different values of x for other user defined errors.
		///       //Client will get a callback uLink_OnFailedToConnect.
		///    }   
		/// }
		/// 
		/// //Client side code example
		/// string passwd = "";
		/// int teamNumber = 3; 
		/// //loginData in the Connect call is teamNumber.
		///
		///		void Awake() {
		///			uLink.Network.Connect("127.0.0.1", 7101, passwd, teamNumber);
		///		}
		///
		///		void uLink_OnConnectedToServer() {
		///			Debug.Log("Connected to server");
		///			// Will check the number of the level to load. 
		///			// The server in this example sends level as an int.
		///			var levelNumber =  uLink.Network.approvalData.Read&lt;int&gt;();
		///			Debug.Log("Got level number " + levelNumber + " from server");
		///			// Use the levelNumber to load the correct things...
		///		}
		///
		/// </code></example>
		public static void uLink_OnPlayerApproval(uLink.NetworkPlayerApproval approval) { }

		/// <summary>
		/// Message callback: Called just before a client or a server initializes a network connection.
		/// </summary>
		/// <remarks>
		/// Use this callback to execute code that needs to be run before the network gets initialized. 
		/// Be aware that this callback is also called just before a client or server makes a connection 
		/// to the master server. Check the nsEvent parameter to detect the event type.</remarks>
		public static void uLink_OnPreStartNetwork(uLink.NetworkStartEvent nsEvent) { }

		/// <summary>
		/// Message callback: Called on the server when a client does not succeeds to connect after an handover.
		/// </summary>
		/// <remarks>
		/// <para>This callback is activated on the destination server if the player never succeeds to connect 
		/// to the destination server after an handover has been started. Default 
		/// timeout value is 12.5 seconds. The value is based on multiplying the two values 
		/// <see cref="uLink.NetworkConfig.handshakeRetriesMaxCount"/> and
		/// <see cref="uLink.NetworkConfig.handshakeRetryDelay"/>.
		/// </para>
		/// <para>There is no need to clean up the player owned objects in this callback, this is done internally by uLink 
		/// because the player owned objects that was part of the handover is not instantiated in the new server until
		/// the client succeeds with the uLink connect. When the client does not connect and this timeout is triggered,
		/// the (waiting) player owned objects, that was part of the handover, will be removed by uLink internally.
		/// </para>
		/// </remarks>
		/// <example><code>
		/// // C# code example
		/// void uLink_OnHandoverTimeout(uLink.NetworkPlayer player) {
		///    Debug.Log("Player never connected after a handover: " + player);
		/// }
		/// </code></example>
		public static void uLink_OnHandoverTimeout(NetworkPlayer player) { }
#endif
	}
}

#endif
