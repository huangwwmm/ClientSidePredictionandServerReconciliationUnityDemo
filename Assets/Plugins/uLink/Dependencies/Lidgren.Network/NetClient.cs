/* Copyright (c) 2008 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using uLink;

namespace Lidgren.Network
{
	/// <summary>
	/// A client which can connect to a single NetServer
	/// </summary>
	internal class NetClient : NetBase
	{
		private NetConnection m_serverConnection;

		private bool m_connectRequested;
		private byte[] m_localHailData; // temporary container until NetConnection has been created
		private NetworkEndPoint m_connectEndpoint;

		/// <summary>
		/// Gets the connection to the server
		/// </summary>
		public NetConnection ServerConnection { get { return m_serverConnection; } }

		/// <summary>
		/// Gets the status of the connection to the server
		/// </summary>
		public NetConnectionStatus Status
		{
			get
			{
				if (m_serverConnection == null)
					return NetConnectionStatus.Disconnected;
				return m_serverConnection.Status;
			}
		}

		/// <summary>
		/// Creates a new NetClient
		/// </summary>
		public NetClient(NetConfiguration config)
			: base(config)
		{
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing no hail data
		/// </summary>
		public void Connect(string host, int port)
		{
			Connect(host, port, null);
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing hailData to the server
		/// </summary>
		public void Connect(string host, int port, byte[] hailData)
		{
			IPAddress ip = NetUtility.Resolve(host);
			if (ip == null)
				throw new NetException("Unable to resolve host");
			Connect(new NetworkEndPoint(ip, port), hailData);
		}

		/// <summary>
		/// Connects to the specified remove endpoint
		/// </summary>
		public void Connect(NetworkEndPoint remoteEndpoint)
		{
			Connect(remoteEndpoint, null);
		}

		/// <summary>
		/// Connects to the specified remote endpoint; passing hailData to the server
		/// </summary>
		public void Connect(NetworkEndPoint remoteEndpoint, byte[] hailData)
		{
			m_connectRequested = true;
			m_connectEndpoint = remoteEndpoint;
			m_localHailData = hailData;

			Start(); // start heartbeat thread etc

			m_socket.Connect(remoteEndpoint);
		}

		internal void PerformConnect()
		{
			// ensure we're bound to socket
			Start();

			m_connectRequested = false;

			if (m_serverConnection != null)
			{
				m_serverConnection.Disconnect("New connect", 0, m_serverConnection.Status == NetConnectionStatus.Connected, true);
				if (m_serverConnection.RemoteEndpoint.Equals(m_connectEndpoint))
					m_serverConnection = new NetConnection(this, m_connectEndpoint, m_localHailData, null, null, 0);
			}
			else
			{
				m_serverConnection = new NetConnection(this, m_connectEndpoint, m_localHailData, null, null, 0);
			}

			// connect
			m_serverConnection.Connect();

			m_connectEndpoint = NetworkEndPoint.unassigned;
			m_localHailData = null;
		}

		/// <summary>
		/// Initiate explicit disconnect
		/// </summary>
		public void Disconnect(string message)
		{
			if (m_serverConnection == null || m_serverConnection.Status == NetConnectionStatus.Disconnected)
			{
				LogWrite("Disconnect - Not connected!");
				return;
			}
			m_serverConnection.Disconnect(message, 1.0f, true, false);
		}

		/// <summary>
		/// Sends unsent messages and reads new messages from the wire
		/// </summary>
		public override void Heartbeat()
		{
			if (!m_isBound) return;

			double now = NetTime.Now;
			m_heartbeatCounter.Count(now);

			if (m_connectRequested)
			{
				PerformConnect();
			}

			// read messages from network
			BaseHeartbeat(now);

			if (m_serverConnection != null)
				m_serverConnection.Heartbeat(now); // will send unsend messages etc.
		}

		/// <summary>
		/// Returns ServerConnection if passed the correct endpoint
		/// </summary>
		public override NetConnection GetConnection(NetworkEndPoint remoteEndpoint)
		{
			if (m_serverConnection != null && m_serverConnection.RemoteEndpoint.Equals(remoteEndpoint))
				return m_serverConnection;
			return null;
		}

		internal override void HandleReceivedMessage(IncomingNetMessage message, NetworkEndPoint senderEndpoint, double localTimeRecv)
		{
			//LogWrite("NetClient received message " + message);
			double now = NetTime.Now;

			int payLen = message.m_data.LengthBytes;

			// Discovery response?
			if (message.m_type == NetMessageLibraryType.System && payLen > 0)
			{
				NetSystemType sysType = (NetSystemType)message.m_data.PeekByte();

				if (sysType == NetSystemType.DiscoveryResponse)
				{
					message.m_data.ReadByte(); // step past system type byte
					IncomingNetMessage resMsg = m_discovery.HandleResponse(message, senderEndpoint);
					if (resMsg != null)
					{
						resMsg.m_senderEndPoint = senderEndpoint;
						EnqueueReceivedMessage(resMsg);
					}
					return;
				}
			}

			// Out of band?
			if (message.m_type == NetMessageLibraryType.OutOfBand)
			{
				if ((m_enabledMessageTypes & NetMessageType.OutOfBandData) != NetMessageType.OutOfBandData)
					return; // drop

				// just deliver
				message.m_msgType = NetMessageType.OutOfBandData;
				message.m_senderEndPoint = senderEndpoint;
				EnqueueReceivedMessage(message);
				return;
			}

			if (message.m_sender != m_serverConnection && m_serverConnection != null)
				return; // don't talk to strange senders after this

			if (message.m_type == NetMessageLibraryType.Acknowledge)
			{
				m_serverConnection.HandleAckMessage(now, message);
				return;
			}

			// Handle system types
			if (message.m_type == NetMessageLibraryType.System)
			{
				if (payLen < 1)
				{
					if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						NotifyApplication(NetMessageType.BadMessageReceived, "Received malformed system message: " + message, m_serverConnection, senderEndpoint);
					return;
				}
				NetSystemType sysType = (NetSystemType)message.m_data.Data[0];
				switch (sysType)
				{
					case NetSystemType.ConnectResponse:
					case NetSystemType.Ping:
					case NetSystemType.Pong:
					case NetSystemType.Disconnect:
					case NetSystemType.ConnectionRejected:
					case NetSystemType.StringTableAck:
						if (m_serverConnection != null)
							m_serverConnection.HandleSystemMessage(message, localTimeRecv);
						return;
					case NetSystemType.Connect:
					case NetSystemType.ConnectionEstablished:
					case NetSystemType.Discovery:
					case NetSystemType.Error:
					default:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Undefined behaviour for client and " + sysType, m_serverConnection, senderEndpoint);
						return;
				}
			}

			Debug.Assert(
				message.m_type == NetMessageLibraryType.User ||
				message.m_type == NetMessageLibraryType.UserFragmented
			);

			if (m_serverConnection.Status == NetConnectionStatus.Connecting)
			{
				m_serverConnection.Disconnect("Received user message before ConnectResponse", 0.0f, true, true);
				return;

				/*
				// lost connectresponse packet?
				// Emulate it; 
				LogVerbose("Received user message before ConnectResponse; emulating ConnectResponse...", m_serverConnection);
				IncomingNetMessage emuMsg = CreateIncomingMessage();
				emuMsg.m_type = NetMessageLibraryType.System;
				emuMsg.m_data.Reset();
				emuMsg.m_data.Write((byte)NetSystemType.ConnectResponse);
				m_serverConnection.HandleSystemMessage(emuMsg, now);
				*/

				// ... and proceed to pick up user message
			}

			// add to pick-up queue
			m_serverConnection.HandleUserMessage(message, now);
		}

		/// <summary>
		/// Sends a message using the specified channel; takes ownership of the NetBuffer, don't reuse it after this call
		/// </summary>
		public void SendMessage(NetBuffer data, NetChannel channel)
		{
			if (m_serverConnection == null || m_serverConnection.Status != NetConnectionStatus.Connected)
				throw new NetException("You must be connected first!");
			m_serverConnection.SendMessage(data, channel);
		}

		/// <summary>
		/// Sends a message using the specified channel, with the specified data as receipt; takes ownership of the NetBuffer, don't reuse it after this call
		/// </summary>
		public void SendMessage(NetBuffer data, NetChannel channel, NetBuffer receipt)
		{
			if (m_serverConnection == null || m_serverConnection.Status != NetConnectionStatus.Connected)
				throw new NetException("You must be connected first!");
			if ((m_enabledMessageTypes & NetMessageType.Receipt) != NetMessageType.Receipt)
				LogVerbose("Warning; Receipt messagetype is not enabled!");
			m_serverConnection.SendMessage(data, channel, receipt);
		}

		/// <summary>
		/// Emit a discovery signal to your subnet
		/// </summary>
		public void DiscoverLocalServers(int serverPort)
		{
			m_discovery.SendDiscoveryRequest(new NetworkEndPoint(IPAddress.Broadcast, serverPort), true);
		}

		/// <summary>
		/// Emit a discovery signal to your subnet; polling every 'interval' second until 'timeout' seconds is reached
		/// </summary>
		public void DiscoverLocalServers(int serverPort, float interval, float timeout)
		{
			m_discovery.SendDiscoveryRequest(new NetworkEndPoint(IPAddress.Broadcast, serverPort), true, interval, timeout);
		}
		
		/// <summary>
		/// Emit a discovery signal to a single host
		/// </summary>
		public void DiscoverKnownServer(string host, int serverPort)
		{
			IPAddress address = NetUtility.Resolve(host);
			var ep = new NetworkEndPoint(address, serverPort);

			m_discovery.SendDiscoveryRequest(ep, false);
		}

		/// <summary>
		/// Emit a discovery signal to a host or subnet
		/// </summary>
		public void DiscoverKnownServer(NetworkEndPoint address, bool useBroadcast)
		{
			m_discovery.SendDiscoveryRequest(address, useBroadcast);
		}

		internal override void HandleConnectionForciblyClosed(NetConnection connection, SocketException sex)
		{
			if (m_serverConnection == null)
				return;

			if (m_serverConnection.Status == NetConnectionStatus.Connecting)
			{
				// failed to connect; server is not listening
				m_serverConnection.Disconnect("Failed to connect; server is not listening", 0, false, true);
				return;
			}

			m_connectRequested = false;
			m_serverConnection.Disconnect("Connection forcibly closed by server", 0, false, true);
			return;
		}

		/// <summary>
		/// Disconnects from server and closes socket
		/// </summary>
		public override void Shutdown(string reason)
		{
			if (m_serverConnection != null)
			{
				m_serverConnection.Disconnect(reason, 0, true, true);
				m_serverConnection.SendUnsentMessages(NetTime.Now); // give disconnect message a chance to get away
			}
			m_connectRequested = false;
			base.Shutdown(reason);
		}
	}
}
