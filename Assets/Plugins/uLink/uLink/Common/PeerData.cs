#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8611 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-08-18 22:41:23 +0200 (Thu, 18 Aug 2011) $
#endregion
using System;
using System.Net;
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// Data structure for holding individual host (server) information.
	/// </summary>
	/// <remarks>
	/// The host list retrieved from a master server uses this class to represent individual servers. 
	/// See <see cref="uLink.MasterServer.PollHostList"/>
	/// </remarks>
	public sealed class PeerData : LocalPeerData
	{
		/// <summary>
		/// The public IP address and port for this host on the Internet. 
		/// </summary>
		public NetworkEndPoint externalEndpoint = NetworkEndPoint.unassigned;

		/// <summary>
		/// Returns the round trip ping time in milliseconds from the master server to the 
		/// game server, or if this is a WellKnownHost the ping time is instead meassured 
		/// between the client and the host.
		/// </summary>
		/// <remarks>
		/// The most usable ping time is for WellKnownHosts since it is meassured between 
		/// the client and the game server(s). A player is usually looking for a nearby server 
		/// with a nice low ping time. 
		/// <para>
		/// The ping time between the master server and the game server can be useful in some situations.
		/// How useful it is depends on where servers are hosted. uLink provides this value to be used in 
		/// any way you want.</para>
		/// <para>
		/// If the host needs a proxy this ping time is 
		/// not correct. Do not use this ping time in the proxy case.  
		/// </para>
		/// <para>
		/// Pingtime between a server and connected players is available via <see cref="uLink.Network.GetAveragePing"/>
		/// </para>
		/// </remarks>
		public int ping;

		/// <summary>
		/// Server public port, used by clients on the Internet to connect to the server.
		/// </summary>
		public int externalPort { get { return externalEndpoint.port; } }

		/// <summary>
		/// Server public IP address, used by clients on the Internet to connect to the server.
		/// </summary>
		public string externalIP { get { return externalEndpoint.ipAddress.ToString(); } }

		/// <summary>
		/// Server public port, used by clients on the Internet to connect to the server. Same as <see cref="uLink.HostData.externalPort"/>.
		/// </summary>
		public int port { get { return externalPort; } }

		/// <summary>
		/// Server public IP address, used by clients on the Internet to connect to the server. Same as <see cref="uLink.HostData.externalIP"/>.
		/// </summary>
		public string ipAddress { get { return externalIP; } }

		public PeerData() { }

		public PeerData(PeerData data)
			: base(data)
		{
			externalEndpoint = data.externalEndpoint;
			ping = data.ping;
		}

		public PeerData(LocalPeerData localPeerData, NetworkEndPoint externalEndpoint, int ping)
			: base(localPeerData)
		{
			this.externalEndpoint = externalEndpoint;
			this.ping = ping;
		}

		internal PeerData(NetworkEndPoint externalEndpoint)
		{
			this.externalEndpoint = externalEndpoint;
		}

		internal PeerData(NetBuffer buffer)
			: base(buffer)
		{
			externalEndpoint = buffer.ReadEndPoint();
			ping = buffer.ReadInt32();
		}

		internal new void _Write(NetBuffer buffer)
		{
			base._Write(buffer);

			buffer.Write(externalEndpoint);
			buffer.Write(ping);
		}

		public override string ToString()
		{
			return base.ToString() +
				"externalEndpoint: " + externalEndpoint + "\n" +
				"ping: " + ping + "\n";
		}

		public override bool Equals(object other)
		{
			return Equals(other as PeerData);
		}

		public bool Equals(PeerData other)
		{
			return other != null && externalEndpoint.Equals(other.externalEndpoint);
		}

		public override int GetHashCode()
		{
			return externalEndpoint.GetHashCode();
		}
	}
}
