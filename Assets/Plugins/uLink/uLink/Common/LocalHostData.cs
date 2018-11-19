#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8612 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-08-18 22:42:20 +0200 (Thu, 18 Aug 2011) $
#endregion
using System;
using System.Net;
using Lidgren.Network;

// TODO: add support for host guid

namespace uLink
{
	/// <summary>
	/// Base data structure for holding individual host (server) information, for a LAN server without an external IP address.
	/// </summary>
	/// <remarks>
	/// Host information for servers with and external public address is stored in the subclass <see cref="uLink.HostData"/>.
	/// </remarks>
	public class LocalHostData
	{
		/// <summary>
		/// The type of the game (like MyUniqueGameType).
		/// </summary>
		public string gameType = String.Empty;

		/// <summary>
		/// The name of the game (like John Doe's Game)
		/// </summary>
		public string gameName = String.Empty;

		/// <summary>
		/// Use this string to describe the mode this game server is running (deathmatch, free for all..)
		/// </summary>
		public string gameMode = String.Empty;

		/// <summary>
		/// Use this string to describe the game level for the server.
		/// </summary>
		public string gameLevel = String.Empty;

		/// <summary>
		/// Currently connected number of players
		/// </summary>
		public int connectedPlayers;

		/// <summary>
		/// The maximum number of players that is allowed on this server.
		/// </summary>
		public int playerLimit;

		/// <summary>
		/// Does the server require a password?
		/// </summary>
		public bool passwordProtected;

		/// <summary>
		/// Is the server dedicated or hosted by a player?
		/// </summary>
		public bool dedicatedServer;

		/// <summary>
		/// Does this server require NAT punchthrough?
		/// </summary>
		public bool useNat;

		/// <summary>
		/// Does this server require that clients connect via a proxy?
		/// </summary>
		/// <remarks>
		/// Read more about the proxy server in the Master Server and Proxy manual chapter.
		/// </remarks>
		public bool useProxy;

		/// <summary>
		/// A miscellaneous comment about the server
		/// </summary>
		public string comment = String.Empty;

		/// <summary>
		/// Use this string to describe the platform needed for connecting to this game server.
		/// </summary>
		public string platform = String.Empty;

		/// <summary>
		/// The time when this data (about the host) was collected.
		/// </summary>
		public DateTime timestamp = DateTime.MinValue;

		/// <summary>
		/// The IP address and port for this host in the local network (LAN). 
		/// </summary>
		public NetworkEndPoint internalEndpoint = NetworkEndPoint.unassigned;

		/// <summary>
		/// Server private port in the local network (LAN). 
		/// </summary>
		public int internalPort { get { return internalEndpoint.port; } }

		/// <summary>
		/// Server private IP address in the local network (LAN). 
		/// </summary>
		public string internalIP { get { return internalEndpoint.ipAddress.ToString(); } }

		/// <summary>
		/// Server private port in the local network (LAN). Same as <see cref="uLink.LocalHostData.internalPort"/>.
		/// </summary>
		public int port { get { return internalPort; } }

		/// <summary>
		/// Server private IP address in the local network (LAN). Same as <see cref="uLink.LocalHostData.internalIP"/>.
		/// </summary>
		public string ipAddress { get { return internalIP; } }

		public LocalHostData() { }

		public LocalHostData(LocalHostData data)
			: this(data.gameType, data.gameName, data.gameMode, data.gameLevel, data.connectedPlayers, data.playerLimit, data.passwordProtected, data.dedicatedServer, data.useNat, data.useProxy, data.comment, data.platform, data.timestamp, data.internalEndpoint)
		{ }

		public LocalHostData(string gameType, string gameName, string gameMode, string gameLevel, int connectedPlayers, int playerLimit, bool passwordProtected, bool dedicatedServer, bool useNat, bool useProxy, string comment, string platform, DateTime timestamp, NetworkEndPoint internalEndpoint)
		{
			this.gameType = gameType;
			this.gameName = gameName;
			this.gameMode = gameMode;
			this.gameLevel = gameLevel;
			this.connectedPlayers = connectedPlayers;
			this.playerLimit = playerLimit;
			this.passwordProtected = passwordProtected;
			this.dedicatedServer = dedicatedServer;
			this.useNat = useNat;
			this.useProxy = useProxy;
			this.comment = comment;
			this.platform = platform;
			this.internalEndpoint = internalEndpoint;
			this.timestamp = timestamp;
		}

		internal LocalHostData(NetBuffer buffer)
		{
			gameType = buffer.ReadString();
			gameName = buffer.ReadString();
			gameMode = buffer.ReadString();
			gameLevel = buffer.ReadString();
			connectedPlayers = buffer.ReadInt32();
			playerLimit = buffer.ReadInt32();
			passwordProtected = (buffer.ReadByte() == 1);
			dedicatedServer = (buffer.ReadByte() == 1);
			useNat = (buffer.ReadByte() == 1);
			useProxy = (buffer.ReadByte() == 1);
			comment = buffer.ReadString();
			platform = buffer.ReadString();
			timestamp = DateTime.FromBinary(buffer.ReadInt64());
			internalEndpoint = buffer.ReadEndPoint();
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.Write(gameType);
			buffer.Write(gameName);
			buffer.Write(gameMode);
			buffer.Write(gameLevel);
			buffer.Write(connectedPlayers);
			buffer.Write(playerLimit);
			buffer.Write((byte)(passwordProtected ? 1:0));
			buffer.Write((byte)(dedicatedServer ? 1:0));
			buffer.Write((byte)(useNat ? 1 : 0));
			buffer.Write((byte)(useProxy ? 1 : 0));
			buffer.Write(comment);
			buffer.Write(platform);
			buffer.Write(timestamp.ToBinary());
			buffer.Write(internalEndpoint);
		}

		public void CopyFrom(LocalHostData data)
		{
			gameType = data.gameType;
			gameName = data.gameName;
			gameMode = data.gameMode;
			gameLevel = data.gameLevel;
			connectedPlayers = data.connectedPlayers;
			playerLimit = data.playerLimit;
			passwordProtected = data.passwordProtected;
			dedicatedServer = data.dedicatedServer;
			useNat = data.useNat;
			useProxy = data.useProxy;
			comment = data.comment;
			platform = data.platform;
			internalEndpoint = data.internalEndpoint;
			timestamp = data.timestamp;
		}

		/// <summary>
		/// Returns true if internalEnpoint, gameType and gameName has been set, otherwise false.
		/// </summary>
		public bool IsDefined()
		{
			if (gameType == null) return false;
			if (gameName == null) return false;
			if (internalEndpoint.isUnassigned) return false;

			return true;
		}

		public override string ToString()
		{
			return
				"gameType: " + gameType + "\n" +
				"gameName: " + gameName + "\n" +
				"gameLevel: " + gameLevel + "\n" +
				"connectedPlayers: " + connectedPlayers + "\n" +
				"playerLimit: " + playerLimit + "\n" +
				"passwordProtected: " + passwordProtected + "\n" +
				"dedicatedServer: " + dedicatedServer + "\n" +
				"useNat: " + useNat + "\n" +
				"useProxy: " + useProxy + "\n" +
				"comment: " + comment + "\n" +
				"platform: " + platform + "\n" +
				"timestamp: " + timestamp + "\n" +
				"internalEndpoint: " + internalEndpoint + "\n";
		}

		public override bool Equals(object other)
		{
			return Equals(other as LocalHostData);
		}

		public bool Equals(LocalHostData other)
		{
			return other != null
				&& internalEndpoint.Equals(other.internalEndpoint)
				&& gameType == other.gameType
				&& gameName == other.gameName
				&& gameMode == other.gameMode
				&& gameLevel == other.gameLevel
				&& connectedPlayers == other.connectedPlayers
				&& playerLimit == other.playerLimit
				&& passwordProtected == other.passwordProtected
				&& dedicatedServer == other.dedicatedServer
				&& useNat == other.useNat
				&& useProxy == other.useProxy
				&& comment == other.comment
				&& platform == other.platform;
		}

		public override int GetHashCode()
		{
			return internalEndpoint.GetHashCode();
		}
	}
}
