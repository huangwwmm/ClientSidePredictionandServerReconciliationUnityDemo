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

#if UNITY_BUILD

namespace uLink
{
	/// <summary>
	/// Use methods in this class to communicate with a stand alone uLink Master Server. 
	/// </summary>
	/// <remarks> The Master 
	/// Server can be used as a listing server (lobby) for game servers. 
	/// You can advertise game
	/// hosts or fetch host lists for your specific game type using this class. The
	/// methods here are used to communicate with the Master Server itself
	/// which is hosted separately without the need of the Unity editor. 
	/// For an overview of the Master Server as well as
	/// usage introduction see the section Master Server in the manual.
	/// <para>
	/// Some of the methods are used for local discovery of servers in LAN
	/// and things regarding that and don't require the standalone master server to exist.
	/// </para>
	/// </remarks>
	public static class MasterServer
	{
		static MasterServer()
		{
			GetPrefs();
		}

		/// <summary>
		/// Load preferences for master server from the previously stored PlayerPrefs.
		/// </summary>
		public static void GetPrefs()
		{
			comment = NetworkPrefs.Get("MasterServer.comment", String.Empty);
			dedicatedServer = NetworkPrefs.Get("MasterServer.dedicatedServer", false);
			gameLevel = NetworkPrefs.Get("MasterServer.gameLevel", String.Empty);
			gameMode = NetworkPrefs.Get("MasterServer.gameMode", String.Empty);
			gameName = NetworkPrefs.Get("MasterServer.gameName", String.Empty);
			gameType = NetworkPrefs.Get("MasterServer.gameType", String.Empty);
			ipAddress = NetworkPrefs.Get("MasterServer.ipAddress", ipAddress);
			password = NetworkPrefs.Get("MasterServer.password", String.Empty);
			port = NetworkPrefs.Get("MasterServer.port", 23466);
			updateRate = NetworkPrefs.Get("MasterServer.updateRate", 0.05f);
		}

		/// <summary>
		/// Save preferences for master server in PlayerPrefs.
		/// </summary>
		public static void SetPrefs()
		{
			NetworkPrefs.Set("MasterServer.comment", comment);
			NetworkPrefs.Set("MasterServer.dedicatedServer", dedicatedServer);
			NetworkPrefs.Set("MasterServer.gameLevel", gameLevel);
			NetworkPrefs.Set("MasterServer.gameMode", gameMode);
			NetworkPrefs.Set("MasterServer.gameName", gameName);
			NetworkPrefs.Set("MasterServer.gameType", gameType);
			NetworkPrefs.Set("MasterServer.ipAddress", ipAddress);
			NetworkPrefs.Set("MasterServer.password", password);
			NetworkPrefs.Set("MasterServer.port", port);
			NetworkPrefs.Set("MasterServer.updateRate", updateRate);
		}

		/// <summary>
		/// The last <see cref="uLink.NetworkConnectionError"/> returned by the master server. 
		/// </summary>
		/// <value>Default value is <see cref="uLink.NetworkConnectionError.NoError"/></value>
		public static NetworkConnectionError lastError
		{
			get { return Network._singleton.lastMasterError; }
			set { Network._singleton.lastMasterError = value; }
		}

		/// <summary>
		/// Mark this server as a dedicated server (it is running without a human player). 
		/// </summary>
		/// <value>Default value is <c>false</c></value>
		/// <remarks>
		/// If running as a server, the connection count defines the player count
		/// and this is reported when registering on the master server. By
		/// default the master server assumes this server instance is not a dedicated
		/// server and thus the player count is incremented by one (to account
		/// for the "player" running the game server). If this is not desired, 
		/// for example when hosting servers in a data center,
		/// this variable can be set to <c>true</c> before registering game servers 
		/// and then only the client connection count
		/// is reported in the host data as the player count.
		/// </remarks>
		public static bool dedicatedServer
		{
			get { return Network._singleton.dedicatedServer; }
			set { Network._singleton.dedicatedServer = value; }
		}

		/// <summary>
		/// The IP address of the master server. Can be the domain name or the IP number.
		/// </summary>
		/// <value>Default value is <c>"unityparkdemo.muchdifferent.com"</c></value>
		/// <remarks>
		/// MuchDifferents master server is for testing purposes only and shouldn't be used
		/// for deployment. It's not guaranteed to be always available and we usually only host the latest master server.
		/// </remarks>
		public static string ipAddress
		{
			get { return Network._singleton.ipAddress; }
			set { Network._singleton.ipAddress = value; }
		}

		/// <summary>
		/// The connection port of the master server.
		/// </summary>
		/// <value>Default value is <c>23466</c></value>
		public static int port
		{
			get { return Network._singleton.port; }
			set { Network._singleton.port = value; }
		}

		/// <summary>
		/// Gets a rolling average roundtrip ping time, measured between this client and the master server. 
		/// Returns 0 if there is no connection to the master server.
		/// </summary>
		public static float ping
		{
			get { return Network._singleton.ping; }
		}

		/// <summary>
		/// The password required to make a connection to the master server.
		/// </summary>
		/// <remarks>
		/// This password value, in a running master server, is set by the operator as 
		/// a start argument when starting the master server.
		/// See the manual page for master server for more information.
		/// </remarks>
		public static string password
		{
			get { return Network._singleton.masterPassword; }
			set { Network._singleton.masterPassword = value; }
		}

		/// <summary>
		/// Is this game server registered in the master server?
		/// </summary>
		/// <remarks>
		/// Use this in the game server, not in a client. Default value is <c>false</c>.
		/// </remarks>
		public static bool isRegistered
		{
			get { return Network._singleton.masterRegistered; }
		}

		/// <summary>
		/// The minimum update rate for master server host information update. 
		/// </summary>
		/// <remarks>
		/// Normally host updates are only sent if something in the host
		/// information has changed (like connected players). The update rate
		/// defines the minimum amount of time which may elapse between host
		/// updates. The default value is 80 seconds minimum update rate (where
		/// a check is made for changes). So if one host update is sent and then
		/// some field changes 10 seconds later then the update will possibly be
		/// sent 70 seconds later (at the next change check). If this is set to
		/// 0 then no updates are sent, only initial registration information. 
		/// </remarks>
		/// <example>
		/// <code>
		/// void StartServer()
		/// {
		///    Network.InitializeServer(32, 25002);
		///    // No host information updates after initial registration
		///    MasterServer.updateRate = 0;
		///    MasterServer.RegisterHost("MyUniqueGameType", "JohnDoes game", "l33t game for all");
		/// }
		/// </code>
		/// </example>
		public static float updateRate
		{
			get { return Network._singleton.updateRate; }
			set { Network._singleton.updateRate = value; }
		}

		/// <summary>
		/// Clear the host list which was received from the stand alone master server. See <see cref="PollHostList"/>. 
		/// </summary>
		/// <remarks>See the code example in <see cref="RequestHostList(System.String)"/></remarks>
		public static void ClearHostList() { Network._singleton.ClearHostList(); }

		/// <summary>
		/// Returns the latest host list received from the MasterServer. See <see cref="RequestHostList(System.String)"/>.
		/// </summary>
		/// <remarks>You can clear the current host list with <see cref="ClearHostList"/>. 
		/// Then way you can be sure that the next list returned is up to date.
		/// See the code example in <see cref="RequestHostList(System.String)"/></remarks>
		public static HostData[] PollHostList() { return Network._singleton.PollHostList(); }

		/// <summary>
		/// Register this game server on the master server. 
		/// </summary>
		/// <param name="gameType">Game type for this game server</param>
		/// <param name="gameName">Game name for this game server</param>
		/// <remarks>
		/// This method will also set the values for, <see cref="gameType"/> 
		/// and <see cref="gameName"/>.
		/// Remember to set the master server ip and port before calling this function.
		/// see the code example in <see cref="RegisterHost(string gameType, string gameName, string comment, string gameMode, string gameLevel)"/>
		/// </remarks>
		public static void RegisterHost(string gameType, string gameName) { Network._singleton.RegisterHost(gameType, gameName); }

		/// <summary>
		/// Register this game server on the master server. 
		/// </summary>
		/// <param name="gameType">Game type for this game server</param>
		/// <param name="gameName">Game name for this game server</param>
		/// <param name="comment">Comment for this game server</param>
		/// <remarks>
		/// This method will also set the values for, <see cref="gameType"/>, 
		/// <see cref="gameName"/> and <see cref="comment"/>.
		/// Remember to set the master server ip and port before calling this function.
		/// see the code example in <see cref="RegisterHost(string gameType, string gameName, string comment, string gameMode, string gameLevel)"/>		
		/// </remarks>
		public static void RegisterHost(string gameType, string gameName, string comment) { Network._singleton.RegisterHost(gameType, gameName, comment); }

		/// <summary>
		/// Register this game server on the master server. 
		/// </summary>
		/// <param name="gameType">Game type for this game server</param>
		/// <param name="gameName">Game name for this game server</param>
		/// <param name="comment">Comment for this game server</param>
		/// <param name="gameMode">Game mode of this game server</param>
		/// <param name="gameLevel">Game level of this game server</param>
		/// <remarks>
		/// This method will also set the values for, <see cref="gameType"/>, 
		/// <see cref="gameName"/>, <see cref="comment"/>, <see cref="gameMode"/> and <see cref="gameLevel"/>.
		/// Remember to set the master server ip and port before calling this function.
		/// <para>
		/// Parameters other than game type are not used exclusively by master server, you can let the user set
		/// them to desired values and show them in the UI for other users or do whatever you want with them.
		/// Be careful to don't use the values that you get directly in your game with reflection or in any other way
		/// that potentially can make security risks.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// void uLink_OnServerInitialized()
		/// {
		/// 	uLink.MasterServer.ipAddress = "127.0.0.1";
		/// 	uLink.MasterServer.port = 23466;
		/// 	uLink.MasterServer.RegisterHost("Fighting", "uLinkGame", "Awesome Game", "Versus Mode", "Normal");
		/// }
		/// </code>
		/// </example>
		public static void RegisterHost(string gameType, string gameName, string comment, string gameMode, string gameLevel) { Network._singleton.RegisterHost(gameType, gameName, comment, gameMode, gameLevel); }

		/// <summary>
		/// Register this game server on the master server. 
		/// </summary>
		/// <remarks>
		/// You have to set <see cref="gameType"/> and <see cref="gameName"/> before calling this method.
		/// Also, remember to set the master server ip and port before calling this function.
		/// You can also use <see cref="RegisterHost(string gameType, string gameName)"/>, so you can set
		/// values for <see cref="gameType"/> and <see cref="gameName"/> while registering server.
		/// </remarks>
		/// <example>
		/// <code>
		/// void uLink_OnServerInitialized()
		/// {
		/// 	uLink.MasterServer.ipAddress = "127.0.0.1";
		/// 	uLink.MasterServer.port = 23466;
		/// 	uLink.MasterServer.gameType = "unique type";
		/// 	uLink.MasterServer.RegisterHost();
		/// }
		/// </code>
		/// </example>
		public static void RegisterHost() { Network._singleton.RegisterHost(); }

		/// <summary>
		/// Unregister this game server on the master server. 
		/// </summary>
		/// <remarks>
		/// Does nothing if the server is not registered or has already unregistered.
		/// You usually call this when you want to shutdown/update a server.
		/// </remarks>
		public static void UnregisterHost() { Network._singleton.UnregisterHost(); }

		/// <summary>
		/// Request a host list from the master server for a specific game type.
		/// </summary>
		/// <param name="gameType">Game type of game servers</param>
		/// <remarks>
		/// This request is asynchronous, it does not return the result right away. Instead, the result 
		/// list is available through uLink.MasterServer.<see cref="PollHostList"/> when it has arrived.
		/// </remarks>
		/// <example><code>
		/// void Awake()
		/// {
		///    // Make sure list is empty and request a new list
		///    MasterServer.ClearHostList();
		///    MasterServer.RequestHostList("uLinkGames");
		/// }
		///
		/// void Update()
		/// {
		///    // If any hosts were received, display game name, the clear host list again
		///    if (MasterServer.PollHostList().length != 0) {
		///       HostData[] hostData = MasterServer.PollHostList();
		///       for (int i = 0; i < hostData.length; i++) {
		///       Debug.Log("Game name: " + hostData[i].gameName);
		///    }
		///       MasterServer.ClearHostList();
		///    }
		/// }
		/// </code></example>
		public static void RequestHostList(string gameType) { Network._singleton.RequestHostList(gameType); }

		/// <summary>
		/// Request a host list from the master server that matches a specific filter.
		/// </summary>
		/// <param name="filter">The <see cref="uLink.HostDataFilter"/> of game servers</param>
		/// <example>
		/// <code>
		/// void Awake()
		/// {
		///    // Make sure list is empty and request a new list
		///    MasterServer.ClearHostList();
		///    HostDataFilter filter = new HostDataFilter("uLinkGame");
		///    MasterServer.RequestHostList(filter);
		/// }
		/// 
		/// void Update()
		/// {
		///    // If any hosts were received, display game name, the clear host list again
		///    if (MasterServer.PollHostList().length != 0) {
		///       HostData[] hostData = MasterServer.PollHostList();
		///       for (int i = 0; i < hostData.length; i++) {
		///       Debug.Log("Game name: " + hostData[i].gameName);
		///    }
		///       MasterServer.ClearHostList();
		///    }
		/// }
		/// </code>
		/// </example>
		public static void RequestHostList(HostDataFilter filter) { Network._singleton.RequestHostList(filter); }


		/// <summary>
		/// Returns the latest host list received from the MasterServer and makes a new host list request 
		/// if the last request is older than requestInterval.
		/// </summary>
		/// <param name="gameType">Game type for game servers</param>
		/// <param name="requestInterval">The minimum time between host list request</param>
		/// <remarks>
		/// This method is convenient to run in Update() in a client. This way you can write one code line to 
		/// always get the latest host list and also make sure the list is refreshed with a specified interval.
		/// If you request host list sooner than specified interval, no request will be sent.
		/// </remarks>
		public static HostData[] PollAndRequestHostList(string gameType, float requestInterval) { return Network._singleton.PollAndRequestHostList(gameType, requestInterval); }
		
		/// <summary>
		/// Returns the latest host list received from the MasterServer and makes a new host list request 
		/// if the last request is older than requestInterval.
		/// </summary>
		/// <param name="filter">The <see cref="uLink.HostDataFilter"/> of game servers</param>
		/// <param name="requestInterval">The minimum time between host list request</param>
		/// <remarks>
		/// This method is convenient to run in Update() in a client. This way you can write one code line to 
		/// always get the latest host list and also make sure the list is refreshed with a specified interval.
		/// If you request host list sooner than specified interval, no request will be sent.
		/// </remarks>
		public static HostData[] PollAndRequestHostList(HostDataFilter filter, float requestInterval) { return Network._singleton.PollAndRequestHostList(filter, requestInterval); }


		/// <summary>
		/// Gets or sets the gameType string for this game server.
		/// </summary>
		/// <remarks>
		/// When you request a game server list from master server, it will find server using its gameType
		/// </remarks>
		public static string gameType
		{
			get { return Network._singleton.gameType; }
			set { Network._singleton.gameType = value; }
		}

		/// <summary>
		/// Gets or sets the gameName string for this game server.
		/// </summary>
		/// <remarks>
		/// Name of the game server which you can let the user set. This is not used internally but you can use
		/// it in any way you see fit (i.e. Name of the user which is hosting a game). 
		/// </remarks>
		public static string gameName
		{
			get { return Network._singleton.gameName; }
			set { Network._singleton.gameName = value; }
		}

		/// <summary>
		/// Gets or sets the gameMode string for this game server.
		/// </summary>
		/// <remarks>
		/// Just like the game name you can let the user set it, i.e. Death Match.
		/// </remarks>
		public static string gameMode
		{
			get { return Network._singleton.gameMode; }
			set { Network._singleton.gameMode = value; }
		}

		/// <summary>
		/// Gets or sets the gameLevel string for this game server.
		/// </summary>
		/// <remarks>
		/// Level of the game server which you can let the user set.
		/// </remarks>
		public static string gameLevel
		{
			get { return Network._singleton.gameLevel; }
			set { Network._singleton.gameLevel = value; }
		}

		/// <summary>
		/// Gets or sets the comment string for this game server.
		/// </summary>
		/// <remarks>
		/// Just like <see cref="gameName"/> and <see cref="gameMode"/> its not used internally. It's a Comment for the game server.
		/// you can use it for generic data, slogans ...
		/// </remarks>
		public static string comment
		{
			get { return Network._singleton.comment; }
			set { Network._singleton.comment = value; }
		}

		/// <summary>
		/// Clear the host list of discovered game servers in the LAN. See <see cref="PollDiscoveredHosts"/>. 
		/// </summary>
		public static void ClearDiscoveredHosts() { Network._singleton.ClearDiscoveredHosts(); }

		/// <summary>
		/// Returns the latest host list of all discovered host in the LAN. See <see cref="O:DiscoverLocalHosts"/>.
		/// </summary>
		/// <remarks>You can clear the current host list with <see cref="ClearDiscoveredHosts"/>. 
		/// That way you can be sure that the next list returned 
		/// (after calling <see cref="O:DiscoverLocalHosts"/>) is up to date.
		/// </remarks>
		public static HostData[] PollDiscoveredHosts() { return Network._singleton.PollDiscoveredHosts(); }

		/// <summary>
		/// Request a host list of all available game servers in the LAN for a specific game type.
		/// </summary>
		/// <param name="gameType">The game type of the game server(s)</param>
		/// <param name="remotePort">The port number of the game server(s)</param>
		/// <remarks>
		/// This request is asynchronous and it is sent to the IPAdress <see cref="System.Net.IPAddress.Broadcast"/>.
		/// uLink collects all the answers from running game servers in the LAN and stores the result internally.
		/// This method does not return the result. Instead, the result 
		/// list is populated when results come in one by one, and the result is available through 
		/// uLink.MasterServer.<see cref="PollDiscoveredHosts"/>.  
		/// </remarks>
		/// <seealso cref="PollDiscoveredHosts"/>
		/// <seealso cref="PollAndDiscoverLocalHosts"/>
		public static void DiscoverLocalHosts(string gameType, int remotePort) { Network._singleton.DiscoverLocalHosts(gameType, remotePort); }
		
		/// <summary>
		/// Request a host list of all available game servers in the LAN using a filter.
		/// </summary>
		/// <param name="filter">The filter for finding only specific game servers.</param>
		/// <param name="remotePort">The port number of the game server(s)</param>
		/// <remarks>
		/// This request is asynchronous and it is sent to the IPAdress <see cref="System.Net.IPAddress.Broadcast"/>.
		/// uLink collects all the answers from running game servers in the LAN and stores the result internally.
		/// This method does not return the result. Instead, the result 
		/// list is populated when results come in one by one, and the result is available through 
		/// uLink.MasterServer.<see cref="PollDiscoveredHosts"/>.  
		/// </remarks>
		/// <seealso cref="PollDiscoveredHosts"/>
		/// <seealso cref="PollAndDiscoverLocalHosts"/>
		public static void DiscoverLocalHosts(HostDataFilter filter, int remotePort) { Network._singleton.DiscoverLocalHosts(filter, remotePort); }
		
		/// <summary>
		/// Request a host list of all available game servers in the LAN for a specific game type.
		/// </summary>
		/// <param name="gameType">The <see cref="gameType"/> of the game server(s)</param>
		/// <param name="remoteStartPort">The lowest port number of the game servers</param>
		/// <param name="remoteEndPort">The highest port number of the game servers</param>
		/// <remarks>
		/// This request is asynchronous and it is sent to the IPAdress <see cref="System.Net.IPAddress.Broadcast"/>.
		/// uLink collects all the answers from running game servers in the LAN and stores the result internally.
		/// The request will be sent to all UDP ports beginning with remoteStartPort and ending with remoteEndPort.
		/// The usage of several ports is necessary when there are several game servers hosted on a single machine, 
		/// since the game servers on one machine need one unique port each.
		/// This method does not return the result. Instead, the result 
		/// list is populated when results come in one by one, and the result is available through 
		/// uLink.MasterServer.<see cref="PollDiscoveredHosts"/>.  
		/// </remarks>
		/// <seealso cref="PollDiscoveredHosts"/>
		/// <seealso cref="PollAndDiscoverLocalHosts"/>
		public static void DiscoverLocalHosts(string gameType, int remoteStartPort, int remoteEndPort) { Network._singleton.DiscoverLocalHosts(gameType, remoteStartPort, remoteEndPort); }
		
		/// <summary>
		/// Request a host list of all available game servers in the LAN using a filter.
		/// </summary>
		/// <param name="filter">The <see cref="uLink.HostDataFilter"/> for finding only specific game servers</param>
		/// <param name="remoteStartPort">The lowest port number of the game servers</param>
		/// <param name="remoteEndPort">The highest port number of the game servers</param>
		/// <remarks>
		/// This request is asynchronous and it is sent to the IPAdress <see cref="System.Net.IPAddress.Broadcast"/>.
		/// uLink collects all the answers from running game servers in the LAN and stores the result internally.
		/// The request will be sent to all UDP ports beginning with remoteStartPort and ending with remoteEndPort.
		/// The usage of several ports is necessary when there are several game servers hosted on a single machine, 
		/// since the game servers on one machine need one unique port each.
		/// This method does not return the result. Instead, the result 
		/// list is populated when results come in one by one, and the result is available through 
		/// uLink.MasterServer.<see cref="PollDiscoveredHosts"/>.  
		/// </remarks>
		/// <seealso cref="PollDiscoveredHosts"/>
		/// <seealso cref="PollAndDiscoverLocalHosts"/>
		public static void DiscoverLocalHosts(HostDataFilter filter, int remoteStartPort, int remoteEndPort) { Network._singleton.DiscoverLocalHosts(filter, remoteStartPort, remoteEndPort); }

		/// <summary>
		/// Returns the latest host list discovered on the LAN and makes a new host list request 
		/// if the last request is older than requestInterval.
		/// </summary>
		/// <param name="gameType">The <see cref="gameType>"/> of the game servers</param>
		/// <param name="remotePort">The port number of the game servers</param>
		/// <param name="discoverInterval">The minimum time between message broadcastings</param>
		/// <remarks>
		/// This method is convenient to run in Update() in a client. This way you can write one code line to 
		/// always get the latest host list and also make sure the list is refreshed with a specified interval.
		/// If you try to discover hosts sooner than specified interval, no message will be broadcasted.
		/// </remarks>
		public static HostData[] PollAndDiscoverLocalHosts(string gameType, int remotePort, float discoverInterval) { return Network._singleton.PollAndDiscoverLocalHosts(gameType, remotePort, discoverInterval); }
		
		/// <summary>
		/// Returns the latest host list discovered on the LAN and makes a new host list request 
		/// if the last request is older than requestInterval.
		/// </summary>
		/// <param name="filter">The <see cref="uLink.HostDataFilter"/> for finding only specific game servers</param>
		/// <param name="remotePort">The port number of game servers</param>
		/// <param name="discoverInterval">The minimum time between message broadcastings</param>
		/// <remarks>
		/// This method is convenient to run in Update() in a client. This way you can write one code line to 
		/// always get the latest host list and also make sure the list is refreshed with a specified interval.
		/// If you try to discover hosts sooner than specified interval, no message will be broadcasted.
		/// </remarks>
		public static HostData[] PollAndDiscoverLocalHosts(HostDataFilter filter, int remotePort, float discoverInterval) { return Network._singleton.PollAndDiscoverLocalHosts(filter, remotePort, discoverInterval); }
		
		/// <summary>
		/// Returns the latest host list discovered on the LAN and makes a new host list request 
		/// if the last request is older than requestInterval.
		/// </summary>
		/// <param name="gameType">The <see cref="gameType"/> of the game servers</param>
		/// <param name="remoteStartPort">The lowest port number of the game servers</param>
		/// <param name="remoteEndPort">The highest port number of the game servers</param>
		/// <param name="discoverInterval">The minimum time between message broadcastings</param>
		/// <remarks>
		/// This method is convenient to run in Update() in a client. This way you can write one code line to 
		/// always get the latest host list and also make sure the list is refreshed with a specified interval.
		/// If you try to discover hosts sooner than specified interval, no message will be broadcasted.
		/// </remarks>
		public static HostData[] PollAndDiscoverLocalHosts(string gameType, int remoteStartPort, int remoteEndPort, float discoverInterval) { return Network._singleton.PollAndDiscoverLocalHosts(gameType, remoteStartPort, remoteEndPort, discoverInterval); }
		
		/// <summary>
		/// Returns the latest host list discovered on the LAN and makes a new host list request 
		/// if the last request is older than requestInterval.
		/// </summary>
		/// <param name="filter">The <see cref="uLink.HostDataFilter"/> for finding only specific game servers</param>
		/// <param name="remoteStartPort">The lowest port number of the game servers</param>
		/// <param name="remoteEndPort">The highest port number of the game servers</param>
		/// <param name="discoverInterval">The minimum time between message broadcastings</param>
		/// <remarks>
		/// This method is convenient to run in Update() in a client. This way you can write one code line to 
		/// always get the latest host list and also make sure the list is refreshed with a specified interval.
		/// If you try to discover hosts sooner than specified interval, no message will be broadcasted.
		/// </remarks>
		public static HostData[] PollAndDiscoverLocalHosts(HostDataFilter filter, int remoteStartPort, int remoteEndPort, float discoverInterval) { return Network._singleton.PollAndDiscoverLocalHosts(filter, remoteStartPort, remoteEndPort, discoverInterval); }
		
		/// <summary>
		/// Gets the HostData info for one favorite servers (Known Hosts) stored in the client.
		/// </summary>
		/// <param name="host">The ip address of favorite server</param>
		/// <param name="remotePort">The port number of favorite server</param>
		/// <returns>
		/// <see cref="uLink.HostData"/> of polled favorite hosts.
		/// </returns>
		public static HostData PollKnownHostData(string host, int remotePort) { return Network._singleton.PollKnownHostData(host, remotePort); }
		
		/// <summary>
		/// Gets the HostData info for one favorite servers (Known Hosts) stored in the client.
		/// </summary>
		/// <param name="endpoint">The <see cref="System.Net.NetworkEndPoint"/> of the favorite server</param>
		/// <returns>
		/// <see cref="uLink.HostData"/> of polled favorite hosts.
		/// </returns>
		public static HostData PollKnownHostData(NetworkEndPoint endpoint) { return Network._singleton.PollKnownHostData(endpoint); }

		/// <summary>
		/// Requests the HostData info for one favorite servers (Known Hosts) stored in the client. Answer can be read with <see cref="O:PollKnownHostData"/>
		/// </summary>
		/// <param name="host">The ip address of favorite server</param>
		/// <param name="remotePort">The port number of favorite server</param>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="AddKnownHostData"/>
		/// <seealso cref="RemoveKnownHostData"/>
		/// <seealso cref="ClearKnownHosts"/>
		public static void RequestKnownHostData(string host, int remotePort) { Network._singleton.RequestKnownHostData(host, remotePort); }
		
		/// <summary>
		/// Requests the HostData info for one favorite servers (Known Hosts) stored in the client
		/// using server's <see cref="System.Net.NetworkEndPoint"/>. Answer can be read with <see cref="O:PollKnownHostData"/>
		/// </summary>
		/// <param name="endpoint">The <see cref="System.Net.NetworkEndPoint"/> of the favorite server</param>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="AddKnownHostData"/>
		/// <seealso cref="RemoveKnownHostData"/>
		/// <seealso cref="ClearKnownHosts"/>
		public static void RequestKnownHostData(NetworkEndPoint endpoint) { Network._singleton.RequestKnownHostData(endpoint); }

		/// <summary>
		/// Store location info for a favorite server (Known Host) in the client.
		/// </summary>
		/// <param name="host">The ip address of favorite server</param>
		/// <param name="remotePort">The port number of favorite server</param>
		/// <remarks>
		/// It is possible to store a separate list of known hosts (favorite servers) in the local uLink client.
		/// Use the API for Known Host Data in this class to build a feature for your users to 
		/// store their favorite servers and access them easily
		/// when they try to reconnect to play the game again on the same server as before.
		/// </remarks>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="RequestKnownHostData"/>
		/// <seealso cref="RemoveKnownHostData"/>
		/// <seealso cref="ClearKnownHosts"/>
		public static void AddKnownHostData(string host, int remotePort) { Network._singleton.AddKnownHostData(host, remotePort); }

		/// <summary>
		/// Store location info for a favorite server (Known Host) in the client.
		/// </summary>
		/// <param name="endpoint">The <see cref="System.Net.NetworkEndPoint"/> of favorite server</param>
		/// <remarks>
		/// It is possible to store a separate list of known hosts (favorite servers) in the local uLink client.
		/// Use the API for Known Host Data in this class to build a feature for your users to 
		/// store their favorite servers and access them easily
		/// when they try to reconnect to play the game again on the same server as before.
		/// </remarks>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="RequestKnownHostData"/>
		/// <seealso cref="RemoveKnownHostData"/>
		/// <seealso cref="ClearKnownHosts"/>
		public static void AddKnownHostData(NetworkEndPoint endpoint) { Network._singleton.AddKnownHostData(endpoint); }

		/// <summary>
		/// Store location info for a favorite server (Known Host) in the client.
		/// </summary>
		/// <param name="data">The <see cref="uLink.HostData"/> of favorite server</param>
		/// <remarks>
		/// It is possible to store a separate list of known hosts (favorite servers) in the local uLink client.
		/// Use the API for Known Host Data in this class to build a feature for your users to 
		/// store their favorite servers and access them easily
		/// when they try to reconnect to play the game again on the same server as before.
		/// </remarks>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="RequestKnownHostData"/>
		/// <seealso cref="RemoveKnownHostData"/>
		/// <seealso cref="ClearKnownHosts"/>
		public static void AddKnownHostData(HostData data) { Network._singleton.AddKnownHostData(data); }

		/// <summary>
		/// Remove location info for a favorite server (Known Host) in the client using server's ip address and port number.
		/// </summary>
		/// <param name="host">The ip address of favorite server</param>
		/// <param name="remotePort">The port of favorite server</param>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="RequestKnownHostData"/>
		/// <seealso cref="AddKnownHostData"/>
		/// <seealso cref="ClearKnownHosts"/>
		public static void RemoveKnownHostData(string host, int remotePort) { Network._singleton.RemoveKnownHostData(host, remotePort); }
		
		/// <summary>
		/// Remove location info for a favorite server (Known Host) in the client using server's <see cref="System.Net.NetworkEndPoint"/>.
		/// </summary>
		/// <param name="endpoint">The <see cref="System.Net.NetworkEndPoint"/> of favorite server.</param>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="RequestKnownHostData"/>
		/// <seealso cref="AddKnownHostData"/>
		/// <seealso cref="ClearKnownHosts"/>
		public static void RemoveKnownHostData(NetworkEndPoint endpoint) { Network._singleton.RemoveKnownHostData(endpoint); }

		/// <summary>
		/// Clear all location info for all favorite servers (Known Hosts) in the client.
		/// </summary>
		/// <seealso cref="PollKnownHostData"/>
		/// <seealso cref="RequestKnownHostData"/>
		/// <seealso cref="AddKnownHostData"/>
		/// <seealso cref="RemoveKnownHostData"/>
		public static void ClearKnownHosts() { Network._singleton.ClearKnownHosts(); }

		/// <summary>
		/// Gets the list with the most recent update of HostData information for all favorite servers (Known Hosts) stored in the client.
		/// </summary>
		/// <seealso cref="RequestKnownHosts"/>
		/// <seealso cref="PollAndRequestKnownHosts"/>
		public static HostData[] PollKnownHosts() { return Network._singleton.PollKnownHosts(); }

		/// <summary>
		/// Request to make the complete list of Known Hosts available. 
		/// </summary>
		/// <remarks>
		/// The answer will be available via <see cref="PollKnownHosts"/>.
		/// </remarks>
		/// <seealso cref="PollKnownHosts"/>
		/// <seealso cref="PollAndRequestKnownHosts"/>
		public static void RequestKnownHosts() { Network._singleton.RequestKnownHosts(); }

		/// <summary>
		/// Gets the list with fresh HostData info for all favorite servers (Known Hosts) stored in the client.
		/// </summary>
		/// <remarks>This method is convenient to run in Update() in a client. This way you can write one code line to 
		/// always get the latest known host list and also make sure the list is refreshed with a specified interval.
		/// Values like <see cref="uLink.HostData.ping"/> and <see cref="uLink.LocalHostData.connectedPlayers"/> are refreshed.
		/// </remarks>
		public static HostData[] PollAndRequestKnownHosts(float requestInterval) { return Network._singleton.PollAndRequestKnownHosts(requestInterval); }
	}
}
#endif