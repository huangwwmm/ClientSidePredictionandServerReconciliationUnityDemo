#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12077 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-05-17 06:35:58 +0200 (Thu, 17 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
#if PIKKO_BUILD || DRAGONSCALE
using System.Collections.Generic; //trying to find out why this is needed - partial subclasses other files?
#endif
using System.Globalization;
using System.Net;
using Lidgren.Network;

// TODO: add support for player guid

namespace uLink
{
	/// <summary>
	/// This struct represents a client/player or the server.
	/// </summary>
	/// <remarks>The server is always represented by the static field 
	/// <see cref="uLink.NetworkPlayer.server"/></remarks>
	public struct NetworkPlayer : IEquatable<NetworkPlayer>, IComparable<NetworkPlayer>, IComparable
	{
		//by WuNan @2016/08/27 14:54:43 编译不过，没有引用，暂时注释掉
		//public static readonly NetworkUtility.Comparer<NetworkPlayer> comparer = NetworkUtility.Comparer<NetworkPlayer>.comparer;

		internal static readonly Dictionary<NetworkPlayer, object> _userData = new Dictionary<NetworkPlayer, object>();

		/// <summary>
		/// Returned when calling uLink.Network.<see cref="uLink.Network.player"/> before the network has been initialized.
		/// See <see cref="O:uLink.Network.InitializeServer"/> and <see cref="uLink.Network.Connect(System.Net.NetworkEndPoint)"/>.
		/// </summary>
		public static readonly NetworkPlayer unassigned = new NetworkPlayer(0);

		/// <summary>
		/// Represents the special network player for the server.
		/// Returned when calling uLink.Network.<see cref="uLink.Network.player"/> on the server
		/// or when calling itself on any client or the server.
		/// </summary>
		public static readonly NetworkPlayer server = new NetworkPlayer(1);

		// TODO: ugly fulhacks to be removed!
		public static readonly NetworkPlayer cellProxies = new NetworkPlayer(-1);

		/// <summary>
		/// Minimum <see cref="uLink.NetworkPlayer.id"/> value a assigned client can have.
		/// </summary>
		public static readonly NetworkPlayer minClient = new NetworkPlayer(2);

		/// <summary>
		/// Maximum <see cref="uLink.NetworkPlayer.id"/> value a assigned client can have.
		/// </summary>
		public static readonly NetworkPlayer maxClient = new NetworkPlayer(Int32.MaxValue);

		/// <summary>
		/// Minimum <see cref="uLink.NetworkPlayer.id"/> value a assigned cellserver can have.
		/// </summary>
		public static readonly NetworkPlayer minCellServer = new NetworkPlayer(Int32.MinValue);

		/// <summary>
		/// Maximum <see cref="uLink.NetworkPlayer.id"/> value a assigned cellserver can have.
		/// </summary>
		public static readonly NetworkPlayer maxCellServer = new NetworkPlayer(-2);

		/// <summary>
		/// Gets the unique id number for this player.
		/// </summary>
		public readonly int id;

		/// <summary>
		/// Gets a value indicating whether this instance is unassigned.
		/// </summary>
		public bool isUnassigned { get { return (id == unassigned.id); } }

		/// <summary>
		/// Gets a value indicating whether this instance is the server.
		/// </summary>
		public bool isServer { get { return (id == server.id); } }

		/// <summary>
		/// Gets a value indicating whether this instance is a client.
		/// </summary>
		public bool isClient { get { return (minClient.id <= id & id <= maxClient.id); } }

		/// <summary>
		/// Gets a value indicating whether this instance is a cellserver.
		/// </summary>
		public bool isCellServer { get { return (minCellServer.id <= id & id <= maxCellServer.id); } } //NOTE: Treating this as "isCellProxy" in most contexts because there's never a reason to send to the cell auth via another ID than NetworkPlayer.server or RPCMode.Server.

		/// <summary>
		/// Gets a value indicating whether this instance is the server or a cellserver.
		/// </summary>
		public bool isServerOrCellServer { get { return isServer | isCellServer; } }

		/// <summary>
		/// Gets a value indicating whether this instance represents cell proxies.
		/// </summary>
		public bool isCellProxies { get { return (id == cellProxies.id); } }

#if UNITY_BUILD
		/// <summary>
		/// Gets a value indicating whether this instance is connected to the network.
		/// </summary>
		public bool isConnected { get { return Network._singleton.IsConnected(this); } }

		/// <summary>
		/// Gets a value indicating the security status of this player.
		/// </summary>
		public NetworkSecurityStatus securityStatus { get { return Network._singleton.GetSecurityStatus(this); } }

		/// <summary>
		/// Gets a value indicating whether this player has security turned on.
		/// </summary>
		public bool hasSecurity { get { return securityStatus == NetworkSecurityStatus.Enabled; } }

		/// <summary>
		/// Gets the <see cref="uLink.NetworkStatistics"/> for this player, which can be used to get connection statistics, bandwidth, packet counts etc.
		/// </summary>
		public NetworkStatistics statistics { get { return Network.GetStatistics(this); } }
		
		/// <summary>
		/// Gets last ping time for a player in milliseconds.
		/// In the client you should only call <c>uLink.NetworkPlayer.server.lastPing</c> because the only available target 
		/// is the server. In the server you can check the ping time for any connected player.
		/// </summary>
		public int lastPing { get { return Network._singleton.GetLastPing(this); } }

		/// <summary>
		/// Gets average ping time for this player in milliseconds.
		/// </summary>
		/// <returns>Average ping time for a player in milliseconds. If target is unknown or not connected, then returns -1.</returns>
		/// <remarks>Calculates the average of the last few pings, making this a moving average.
		/// In the client you should only call <c>uLink.NetworkPlayer.server.averagePing</c> because the only available target 
		/// is the server. In the server you can check the ping time for any connected player.
		/// </remarks>
		public int averagePing { get { return Network._singleton.GetAveragePing(this); } }

		/// <summary>
		/// Gets the loginData sent by the player when the player connected.
		/// </summary>
		/// <remarks>
		/// Use this to get the loginData for any player on the server. When a player connects, the player can send extra 
		/// parameters in the <see cref="uLink.Network.Connect(uLink.HostData, System.String, System.Object[])"/> method 
		/// arguments. These parameter will be stored in the client and sent from the client. These parameters are received by 
		/// the server as loginData and stored in the server for the complete game 
		/// session (until the player disconnects) and can be retrieved this way on the server. 
		/// In a client the only available login data is your own loginData, sent using the 
		/// one of the <see cref="uLink.Network.Connect(uLink.HostData, System.String, System.Object[])"/> methods.
		/// </remarks>
		public BitStream loginData { get { return Network._singleton.GetLoginData(this); } }

		[Obsolete("NetworkPlayer.userData is deprecated, please use NetworkPlayer.localData instead")]
		public object userData { get { return GetLocalData(); } set { SetLocalData(value); } }

		/// <summary>
		/// Returns the <see cref="System.Net.NetworkEndPoint"/> for this <see cref="uLink.NetworkPlayer"/>.
		/// </summary>
		public NetworkEndPoint endpoint { get { return Network._singleton.FindAvailableEndPoint(this); } }

		/// <summary>
		/// Gets the ip address for this NetworkPlayer.
		/// </summary>
		public string ipAddress { get { return endpoint.ipAddress.ToString(); } }

		/// <summary>
		/// Gets the UDP port for this NetworkPlayer.
		/// </summary>
		public int port { get { return endpoint.port; } }

		/// <summary>
		/// Gets the external IP address of the player.
		/// </summary>
		/// <remarks>For clients behind NAT, this is usually the IP address of the firewall which client is using to 
		/// connect to the network.</remarks>
		public NetworkEndPoint externalEndpoint { get { return Network._singleton.FindExternalEndPoint(this); } }

		/// <summary>
		/// Gets the external IP for this NetworkPlayer.
		/// </summary>
		/// <remarks>The external IP for a client is usually the IP for the NAT-capable firewall/router this client 
		/// is placed behind.</remarks>
		public string externalIP { get { return externalEndpoint.ipAddress.ToString(); } }

		/// <summary>
		/// Gets the external port for this NetworkPlayer.
		/// </summary>
		/// <remarks>The external port for a client is usually the port chosen by 
		/// the NAT-capable firewall/router this client is placed behind.</remarks>
		public int externalPort { get { return externalEndpoint.port; } }

		/// <summary>
		/// The internal IP end point of the player.
		/// </summary>
		/// <remarks>In clients behind NAT, This is the end point that the firewall/router uses to connect to the player's machine.</remarks>
		public NetworkEndPoint internalEndpoint { get { return Network._singleton.FindInternalEndPoint(this); } }

		/// <summary>
		/// Returns the internal IP address of the player.
		/// </summary>
		/// <remarks>In clients behind NAT, This is the end point that the firewall/router uses to connect to the player's machine.</remarks>
		public string internalIP { get { return internalEndpoint.ipAddress.ToString(); } }

		/// <summary>
		/// Returns the internal port of the player.
		/// </summary>
		/// <remarks>In clients behind NAT, This is the end point that the firewall/router uses to connect to the player's machine.</remarks>
		public int internalPort { get { return internalEndpoint.port; } }

		/// <summary>
		/// Gets a unique ID for this player.
		/// </summary>
		[Obsolete("NetworkPlayer.guid is deprecated, please use NetworkPlayer.ToString() instead")]
		public string guid { get { return ToString(); } }
#endif

		/// <summary>
		/// Gets or sets the localData, data that is not sent over the network.
		/// </summary>
		/// <remarks>
		/// Use this to store any kind of data for this player, data that should be stored locally only.
		/// This data will never be sent over the network. 
		/// Usually this is used on the server side for storing things per player. This could be 
		/// things like original spawn point, login time, cached data from the database, and whatever you like.
		/// The alternative is to set up one or several Dictionaries on the server for 
		/// storing data per player.
		/// </remarks>
		public object localData { get { return GetLocalData(); } set { SetLocalData(value); } }

		/// <summary>
		/// Sets the local data for this player.
		/// </summary>
		/// <remarks>See <see cref="uLink.NetworkPlayer.localData"/> for more information.</remarks>
		/// <param name="localData"></param>
		public void SetLocalData(object localData)
		{
			_userData[this] = localData;
		}

		/// <summary>
		/// Gets the local data for the player.
		/// </summary>
		/// <remarks>See <see cref="uLink.NetworkPlayer.localData"/> for more information.</remarks>
		/// <returns></returns>
		public object GetLocalData()
		{
			object localData;
			_userData.TryGetValue(this, out localData);
			return localData;
		}

		/// <summary>
		/// Returns the local data of the player casted to the specified type parameter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetLocalData<T>()
		{
			return (T)GetLocalData();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="uLink.NetworkPlayer"/> struct.
		/// </summary>
		public NetworkPlayer(int id)
		{
			this.id = id;
		}

#if !PIKKO_BUILD && !DRAGONSCALE && !NO_CRAP_DEPENDENCIES
		internal NetworkPlayer(NetworkBaseLocal network)
		{
			id = network._localPlayer.id;
		}
#endif

		internal NetworkPlayer(NetBuffer buffer)
		{
			id = buffer.ReadVariableInt32();
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.WriteVariableInt32(id);
		}

		public static bool operator ==(NetworkPlayer lhs, NetworkPlayer rhs) { return lhs.id == rhs.id; }
		public static bool operator !=(NetworkPlayer lhs, NetworkPlayer rhs) { return lhs.id != rhs.id; }
		public static bool operator >=(NetworkPlayer lhs, NetworkPlayer rhs) { return lhs.id >= rhs.id; }
		public static bool operator <=(NetworkPlayer lhs, NetworkPlayer rhs) { return lhs.id <= rhs.id; }
		public static bool operator >(NetworkPlayer lhs, NetworkPlayer rhs) { return lhs.id > rhs.id; }
		public static bool operator <(NetworkPlayer lhs, NetworkPlayer rhs) { return lhs.id < rhs.id; }

		public override int GetHashCode() { return id.GetHashCode(); }

		public override bool Equals(object other) 
		{
			return (other is NetworkPlayer) && Equals((NetworkPlayer)other);
		}

		public bool Equals(NetworkPlayer other)
		{
			return id == other.id;
		}

		public int CompareTo(NetworkPlayer other)
		{
			return id.CompareTo(other.id);
		}

		public int CompareTo(object other)
		{
			return CompareTo((NetworkPlayer)other);
		}

		public override string ToString()
		{
			string prefix;

			// TODO: preferably we want to change this so that the ToString makes more sense to the reader, instead of worrying about the correctness of outputing the id value.
			if (isUnassigned) prefix = "Unassigned ";
			else if (isServer) prefix = "Server ";
			else if (isClient) prefix = "Client ";
			else if (isCellServer) prefix = "CellServer ";
			else if (isCellProxies) prefix = "CellProxies ";
			else prefix = "";

			return "Player " + prefix + "(" + id.ToString(CultureInfo.InvariantCulture) + ")";
		}
	}
}
