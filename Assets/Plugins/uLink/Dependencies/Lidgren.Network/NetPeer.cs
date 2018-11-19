using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using uLink;

namespace Lidgren.Network
{
	/// <summary>
	/// A client which can initiate and accept multiple connections
	/// </summary>
	internal class NetPeer : NetServer
	{
		public NetPeer(NetConfiguration config)
			: base(config)
		{
			m_allowOutgoingConnections = true;
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing hailData to the server
		/// </summary>
		public NetConnection Connect(string host, int port)
		{
			IPAddress ip = NetUtility.Resolve(host);
			if (ip == null)
				throw new NetException("Unable to resolve host");
			return Connect(new NetworkEndPoint(ip, port), null);
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing hailData to the server
		/// </summary>
		public NetConnection Connect(string host, int port, byte[] hailData)
		{
			IPAddress ip = NetUtility.Resolve(host);
			if (ip == null)
				throw new NetException("Unable to resolve host");
			return Connect(new NetworkEndPoint(ip, port), hailData);
		}

		/// <summary>
		/// Connects to the specified endpoint
		/// </summary>
		public NetConnection Connect(NetworkEndPoint remoteEndpoint)
		{
			return Connect(remoteEndpoint, null);
		}

		/// <summary>
		/// Connects to the specified endpoint; passing (outgoing) hailData to the server
		/// </summary>
		public NetConnection Connect(NetworkEndPoint remoteEndpoint, byte[] hailData)
		{
			return Connect(remoteEndpoint, hailData, m_config.ApplicationIdentifier);
		}

		/// <summary>
		/// Connects to the specified endpoint; passing (outgoing) hailData to the server
		/// </summary>
		public NetConnection Connect(NetworkEndPoint remoteEndpoint, byte[] localHailData, string appId)
		{
			// ensure we're bound to socket
			if (!m_isBound)
				Start();

			NetConnection connection;
			if (m_connectionLookup.TryGetValue(remoteEndpoint, out connection))
			{
				// Already connected to this remote endpoint
				if (connection.Status == NetConnectionStatus.Connected || connection.Status == NetConnectionStatus.Connecting || connection.Status == NetConnectionStatus.Reconnecting)
				{
					return connection;
				}

				// fulhacks (ugly hack): should create a new NetConnection instead.
				connection.Reset();
			}
			else
			{
				// find empty slot
				if (m_config.MaxConnections != -1 && m_connections.Count >= m_config.MaxConnections)
					throw new NetException("No available slots!");

				// create new connection
				connection = new NetConnection(this, remoteEndpoint, localHailData, null, null, 0);
				
				m_connections.Add(connection);
				m_connectionLookup.Add(remoteEndpoint, connection);
			}

			// connect
			connection.Connect(appId);

			return connection;
		}

		/// <summary>
		/// Emit a discovery signal to your subnet
		/// </summary>
		public void DiscoverLocalPeers(int port)
		{
			m_discovery.SendDiscoveryRequest(new NetworkEndPoint(IPAddress.Broadcast, port), true);
		}

		/// <summary>
		/// Emit a discovery signal to a certain host
		/// </summary>
		public void DiscoverKnownPeer(string host, int serverPort)
		{
			IPAddress address = NetUtility.Resolve(host);
			var endPoint = new NetworkEndPoint(address, serverPort);
			m_discovery.SendDiscoveryRequest(endPoint, false);
		}

		/// <summary>
		/// Emit a discovery signal to a host or subnet
		/// </summary>
		public void DiscoverKnownPeer(NetworkEndPoint endPoint, bool useBroadcast)
		{
			m_discovery.SendDiscoveryRequest(endPoint, useBroadcast);
		}
	}
}
