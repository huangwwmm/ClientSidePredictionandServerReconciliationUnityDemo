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

// TODO: add support for peer data guid

namespace uLink
{
	/// <summary>
	/// Base data structure for holding individual host (server) information, for a LAN server without an external IP address.
	/// </summary>
	/// <remarks>
	/// Host information for servers with and external public address is stored in the subclass <see cref="uLink.HostData"/>.
	/// </remarks>
	public class LocalPeerData
	{
		/// <summary>
		/// The type of the game (like MyUniqueGameType).
		/// </summary>
		public string peerType = String.Empty;

		/// <summary>
		/// The name of the game (like John Doe's Game)
		/// </summary>
		public string peerName = String.Empty;

		/// <summary>
		/// Does the server require a password?
		/// </summary>
		public bool passwordProtected;

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

		public LocalPeerData() { }

		public LocalPeerData(LocalPeerData data)
			: this(data.peerType, data.peerName, data.passwordProtected, data.comment, data.platform, data.timestamp, data.internalEndpoint)
		{ }

		public LocalPeerData(string peerType, string peerName, bool passwordProtected, string comment, string platform, DateTime timestamp, NetworkEndPoint internalEndpoint)
		{
			this.peerType = peerType;
			this.peerName = peerName;
			this.passwordProtected = passwordProtected;
			this.comment = comment;
			this.platform = platform;
			this.internalEndpoint = internalEndpoint;
			this.timestamp = timestamp;
		}

		internal LocalPeerData(NetBuffer buffer)
		{
			peerType = buffer.ReadString();
			peerName = buffer.ReadString();
			passwordProtected = (buffer.ReadByte() == 1);
			comment = buffer.ReadString();
			platform = buffer.ReadString();
			timestamp = DateTime.FromBinary(buffer.ReadInt64());
			internalEndpoint = buffer.ReadEndPoint();
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.Write(peerType);
			buffer.Write(peerName);
			buffer.Write((byte)(passwordProtected ? 1:0));
			buffer.Write(comment);
			buffer.Write(platform);
			buffer.Write(timestamp.ToBinary());
			buffer.Write(internalEndpoint);
		}

		public void CopyFrom(LocalPeerData data)
		{
			peerType = data.peerType;
			peerName = data.peerName;
			passwordProtected = data.passwordProtected;
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
			if (peerType == null) return false;
			if (peerName == null) return false;
			if (internalEndpoint.isUnassigned) return false;

			return true;
		}

		public override string ToString()
		{
			return
				"peerType: " + peerType + "\n" +
				"peerName: " + peerName + "\n" +
				"passwordProtected: " + passwordProtected + "\n" +
				"comment: " + comment + "\n" +
				"platform: " + platform + "\n" +
				"timestamp: " + timestamp + "\n" +
				"internalEndpoint: " + internalEndpoint + "\n";
		}

		public override bool Equals(object other)
		{
			return Equals(other as LocalPeerData);
		}

		public bool Equals(LocalPeerData other)
		{
			return other != null
				&& internalEndpoint.Equals(other.internalEndpoint)
				&& peerType == other.peerType
				&& peerName == other.peerName
				&& passwordProtected == other.passwordProtected
				&& comment == other.comment
				&& platform == other.platform;
		}

		public override int GetHashCode()
		{
			return internalEndpoint.GetHashCode();
		}
	}
}
