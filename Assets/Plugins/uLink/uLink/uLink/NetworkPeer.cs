#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion

using System;
using System.Collections.Generic;
using System.Net;
using Lidgren.Network;

// TODO: add support for peer guid

namespace uLink
{
	/// <summary>
	/// Represents one peer/host i a peer-to-peer network.
	/// </summary>
	/// <remarks>There are some similarities between NetworkPeer and <see cref="uLink.NetworkPlayer"/> in client server
	/// networks. for example you can use a peer reference to send RPCs to the specific peer, just 
	/// like what you can do with network players in client server setups.</remarks>
	public struct NetworkPeer : IEquatable<NetworkPeer>
	{
		//by WuNan @2016/08/27 14:54:43 编译不过，没有引用，暂时注释掉
		//public static readonly NetworkUtility.EqualityComparer<NetworkPeer> comparer = NetworkUtility.EqualityComparer<NetworkPeer>.comparer;

		/// <summary>
		/// Represents a peer which is not initialized yet.
		/// </summary>
		public static readonly NetworkPeer unassigned = new NetworkPeer(NetworkEndPoint.unassigned);

		public readonly NetworkEndPoint endpoint;

		/// <summary>
		/// Returns the IP address of the peer as a string.
		/// </summary>
		/// <remarks>If not initialized yet, <see cref="uLink.NetworkPeer.unassigned"/>'s IP will be returned.</remarks>
		public string ipAddress { get { return endpoint.ipAddress.ToString(); } }

		/// <summary>
		/// Returns the port of the peer.
		/// </summary>
		/// <remarks>If not initialized yet, <see cref="uLink.NetworkPeer.unassigned"/>'s port will be returned.</remarks>
		public int port { get { return endpoint.port; } }

		public NetworkPeer(NetworkEndPoint endpoint)
		{
			this.endpoint = endpoint;
		}

		/// <summary>
		/// Initializes a peer.
		/// </summary>
		/// <param name="localPort">The port that we should listen to.</param>
		public NetworkPeer(int localPort)
		{
			endpoint = Utility.Resolve(localPort);
		}

		/// <summary>
		/// Initializes a peer.
		/// </summary>
		/// <param name="hostnameOrIP">Host name or IP address of the peer.</param>
		/// <param name="port">The port that peer should listen to.</param>
		public NetworkPeer(string hostnameOrIP, int port)
		{
			endpoint = Utility.Resolve(hostnameOrIP, port);
		}

		/// <summary>
		/// Initializes a peer.
		/// </summary>
		/// <param name="ip">IP address of the peer.</param>
		/// <param name="port">The port that the peer should listen to.</param>
		public NetworkPeer(IPAddress ip, int port)
		{
			endpoint = new NetworkEndPoint(ip, port);
		}

		internal NetworkPeer(NetBuffer buffer)
		{
			endpoint = buffer.ReadEndPoint();
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.Write(endpoint);
		}

		public static bool operator ==(NetworkPeer lhs, NetworkPeer rhs) { return lhs.Equals(rhs); }
		public static bool operator !=(NetworkPeer lhs, NetworkPeer rhs) { return !lhs.Equals(rhs); }

		public override int GetHashCode() { return endpoint.GetHashCode(); }

		public override bool Equals(object other)
		{
			return (other is NetworkPeer) && Equals((NetworkPeer)other);
		}

		public bool Equals(NetworkPeer other)
		{
			return endpoint.Equals(other.endpoint);
		}

		public override string ToString()
		{
			return "Peer " + endpoint;
		}
	}
}
