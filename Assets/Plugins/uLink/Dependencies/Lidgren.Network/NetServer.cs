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
using System.Text;
using System.Threading;
using uLink;

namespace Lidgren.Network
{
	/// <summary>
	/// A server which can accept connections from multiple NetClients
	/// </summary>
	internal class NetServer : NetBase
	{
		protected List<NetConnection> m_connections;
		protected uLink.Dictionary<NetworkEndPoint, NetConnection> m_connectionLookup;
		protected uLink.Dictionary<NetworkEndPoint, NetConnection> m_pendingLookup;
		protected bool m_allowOutgoingConnections; // used by NetPeer
		
		/// <summary>
		/// Gets a copy of the list of connections
		/// </summary>
		public List<NetConnection> Connections
		{
			get
			{
				return m_connections;
			}
		}

		/// <summary>
		/// Creates a new NetServer
		/// </summary>
		public NetServer(NetConfiguration config)
			: base(config)
		{
			m_connections = new List<NetConnection>();
			m_connectionLookup = new uLink.Dictionary<NetworkEndPoint, NetConnection>();
			m_pendingLookup = new uLink.Dictionary<NetworkEndPoint, NetConnection>();
		}
		
		/// <summary>
		/// Reads and sends messages from the network
		/// </summary>
		public override void Heartbeat()
		{
			if (!m_isBound) return;

			NetProfiler.BeginSample("_GetTime");
			double now = NetTime.Now;
			NetProfiler.EndSample();

			NetProfiler.BeginSample("_UpdateCounter");
			m_heartbeatCounter.Count(now);
			NetProfiler.EndSample();
						
			// read messages from network
			BaseHeartbeat(now);

			NetProfiler.BeginSample("_UpdateConnections");
			UpdateConnections(now);
			NetProfiler.EndSample();
		}

		private void UpdateConnections(double now)
		{
			List<NetConnection> deadConnections = null;
			foreach (NetConnection conn in m_connections)
			{
				if (conn.m_status == NetConnectionStatus.Disconnected)
				{
					if (deadConnections == null)
						deadConnections = new List<NetConnection>();
					deadConnections.Add(conn);
					continue;
				}

				conn.Heartbeat(now);
			}

			if (deadConnections != null)
			{
				foreach (NetConnection conn in deadConnections)
				{
					m_connections.Remove(conn);
					m_connectionLookup.Remove(conn.RemoteEndpoint);
					m_pendingLookup.Remove(conn.m_remoteEndPoint);
				}
			}
		}

		public override NetConnection GetConnection(NetworkEndPoint remoteEndpoint)
		{
			NetConnection retval;
			if (m_connectionLookup.TryGetValue(remoteEndpoint, out retval))
				return retval;
			return null;
		}

		internal override void HandleReceivedMessage(IncomingNetMessage message, NetworkEndPoint senderEndpoint, double timestamp)
		{
			double now = NetTime.Now;

			int payLen = message.m_data.LengthBytes;

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

			if (message.m_sender == null)
			{
				//
				// Handle unconnected message
				//

				// not a connected sender; only allow System messages
				if (message.m_type != NetMessageLibraryType.System)
				{
					if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						NotifyApplication(NetMessageType.BadMessageReceived, "Rejecting non-system message from unconnected source: " + message, null, message.m_senderEndPoint);
					return;
				}

				// read type of system message
				NetSystemType sysType = (NetSystemType)message.m_data.ReadByte();
				switch (sysType)
				{
					case NetSystemType.Connect:

						LogVerbose("Connection request received from " + senderEndpoint);

						NetConnection conn;
						if (m_pendingLookup.TryGetValue(senderEndpoint, out conn))
						{
							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Ignore connection request received because already pending", conn, senderEndpoint);
							return;
						}

						if (m_connectionLookup.TryGetValue(senderEndpoint, out conn))
						{
							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Ignore connection request received because already connected", conn, senderEndpoint);
							return;
						}

						// check app id
						if (payLen < 4)
						{
							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Malformed Connect message received from " + senderEndpoint, null, senderEndpoint);
							return;
						}
						string appIdent = message.m_data.ReadString();
						if (appIdent != m_config.ApplicationIdentifier)
						{
							if ((m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								NotifyApplication(NetMessageType.ConnectionRejected, "Bad app id", null, senderEndpoint);

							// send connection rejected
							NetBuffer rejreason = new NetBuffer("Bad app id");
							QueueSingleUnreliableSystemMessage(
								NetSystemType.ConnectionRejected,
								rejreason,
								senderEndpoint,
								false
							);
							return;
						}

						// read random identifer
						var rndSignature = message.m_data.ReadBytes(NetConstants.SignatureByteSize);
						if (NetUtility.CompareElements(rndSignature, m_localRndSignature))
						{
							// don't allow self-connect
							if ((m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								NotifyApplication(NetMessageType.ConnectionRejected, "Connection to self not allowed", null, senderEndpoint);
							return;
						}

						int rndSeqNr = message.m_data.ReadInt32();

						double localTimeSent = message.m_data.ReadDouble();
						double remoteTimeRecv = timestamp + m_localTimeOffset;

						int bytesReadSoFar = message.m_data.PositionBytes;
						int hailLen = message.m_data.LengthBytes - bytesReadSoFar;
						byte[] hailData = null;
						if (hailLen > 0)
						{
							hailData = new byte[hailLen];
							Buffer.BlockCopy(message.m_data.Data, bytesReadSoFar, hailData, 0, hailLen);
						}

						if (m_config.m_maxConnections != -1 && m_connections.Count >= m_config.m_maxConnections)
						{
							if ((m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								NotifyApplication(NetMessageType.ConnectionRejected, "Max connections", null, senderEndpoint);

							// send connection rejected
							NetBuffer rejreason = new NetBuffer("Server full");
							QueueSingleUnreliableSystemMessage(
								NetSystemType.ConnectionRejected,
								rejreason,
								senderEndpoint,
								false
							);

							return;
						}

#if LIMITED_BUILD
						if (m_connections.Count >= 2)
						{
							if ((m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								NotifyApplication(NetMessageType.ConnectionRejected, "I'm special", null, senderEndpoint);

							// send connection rejected
							NetBuffer rejreason = new NetBuffer("Special server");
							QueueSingleUnreliableSystemMessage(
								NetSystemType.ConnectionRejected,
								rejreason,
								senderEndpoint,
								false
							);

							return;
						}
#endif

						// Create connection
						LogWrite("New connection: " + senderEndpoint);
						conn = new NetConnection(this, senderEndpoint, null, hailData, rndSignature, rndSeqNr);
						m_pendingLookup.Add(senderEndpoint, conn);

						conn.m_connectLocalSentTime = localTimeSent;
						conn.m_connectRemoteRecvTime = remoteTimeRecv;

						// Connection approval?
						if ((m_enabledMessageTypes & NetMessageType.ConnectionApproval) == NetMessageType.ConnectionApproval)
						{
							// Ask application if this connection is allowed to proceed
							IncomingNetMessage app = CreateIncomingMessage();
							app.m_msgType = NetMessageType.ConnectionApproval;
							if (hailData != null)
								app.m_data.Write(hailData);
							app.m_sender = conn;
							conn.m_approved = false;
							EnqueueReceivedMessage(app);
							// Don't add connection; it's done as part of the approval procedure
							return;
						}

						// it's ok
						AddConnection(now, conn);
						break;
					case NetSystemType.ConnectionEstablished:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Connection established received from non-connection! " + senderEndpoint, null, senderEndpoint);
						return;
					case NetSystemType.Discovery:
						if (m_config.AnswerDiscoveryRequests)
							m_discovery.HandleRequest(message, senderEndpoint);
						break;
					case NetSystemType.DiscoveryResponse:
						if (m_allowOutgoingConnections)
						{
							// NetPeer
							IncomingNetMessage resMsg = m_discovery.HandleResponse(message, senderEndpoint);
							if (resMsg != null)
							{
								resMsg.m_senderEndPoint = senderEndpoint;
								EnqueueReceivedMessage(resMsg);
							}
						}
						break;
					case NetSystemType.Ping:
						{
							var tempBuffer = GetTempBuffer();
							tempBuffer.Write("We're not connected");
							SendSingleUnreliableSystemMessage(
								NetSystemType.Disconnect,
								tempBuffer,
								senderEndpoint,
								null
							);

							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Ignore " + this + " receiving system type " + sysType + ": " + message + " from unconnected source", null, senderEndpoint);
							
							break;
						}
					default:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Ignore " + this + " receiving system type " + sysType + ": " + message + " from unconnected source", null, senderEndpoint);
						break;
				}
				// done
				return;
			}

			// ok, we have a sender
			if (message.m_type == NetMessageLibraryType.Acknowledge)
			{
				message.m_sender.HandleAckMessage(now, message);
				return;
			}

			if (message.m_type == NetMessageLibraryType.System)
			{
				//
				// Handle system messages from connected source
				//

				if (payLen < 1)
				{
					if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						NotifyApplication(NetMessageType.BadMessageReceived, "Received malformed system message; payload length less than 1 byte", null, senderEndpoint);
					return;
				}
				NetSystemType sysType = (NetSystemType)message.m_data.ReadByte();
				switch (sysType)
				{
					case NetSystemType.Connect:
					case NetSystemType.ConnectionEstablished:
					case NetSystemType.Ping:
					case NetSystemType.Pong:
					case NetSystemType.Disconnect:
					case NetSystemType.ConnectionRejected:
					case NetSystemType.StringTableAck:
						message.m_sender.HandleSystemMessage(message, now);
						break;
					case NetSystemType.ConnectResponse:
						if (m_allowOutgoingConnections)
						{
							message.m_sender.HandleSystemMessage(message, now);
						}
						else
						{
							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Undefined behaviour for server and system type " + sysType, null, senderEndpoint);
						}
						break;
					case NetSystemType.Discovery:
						// Allow discovery even if connected
						if (m_config.AnswerDiscoveryRequests)
							m_discovery.HandleRequest(message, senderEndpoint);
						break;
					default:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Undefined behaviour for server and system type " + sysType, null, senderEndpoint);
						break;
				}
				return;
			}

			message.m_sender.HandleUserMessage(message, now);
		}

		internal void AddConnection(double now, NetConnection conn)
		{
			conn.SetStatus(NetConnectionStatus.Connecting, "Connecting");

			if (m_connections.Contains(conn))
			{
				// already added
				conn.m_approved = true; // just to be sure // TODO: is this really needed?
				return;
			}

			LogWrite("Adding connection " + conn);

			// send response; even if connected
			var responseBuffer = GetTempBuffer();
			responseBuffer.Write(m_localRndSignature);
			responseBuffer.Write(conn.m_localRndSeqNr);
			responseBuffer.Write(conn.m_remoteRndSignature);
			responseBuffer.Write(conn.m_remoteRndSeqNr);
			
			double localTimeSent = conn.m_connectLocalSentTime;
			responseBuffer.Write(localTimeSent);
			double remoteTimeRecv = conn.m_connectRemoteRecvTime;
			responseBuffer.Write(remoteTimeRecv);
			double remoteTimeSent = NetTime.Now + m_localTimeOffset;
			responseBuffer.Write(remoteTimeSent);

			if (conn.LocalHailData != null)
				responseBuffer.Write(conn.LocalHailData);

			conn.m_handshakeInitiated = remoteTimeSent;
			conn.SendSingleUnreliableSystemMessage(NetSystemType.ConnectResponse, responseBuffer);
			
			conn.m_approved = true;
			m_connections.Add(conn);
			m_connectionLookup.Add(conn.m_remoteEndPoint, conn);
			m_pendingLookup.Remove(conn.m_remoteEndPoint);
		}

		/// <summary>
		/// Sends a message to a specific connection
		/// </summary>
		public void SendMessage(NetBuffer data, NetConnection recipient, NetChannel channel)
		{
			if (recipient == null)
				throw new ArgumentNullException("recipient");
			recipient.SendMessage(data, channel);
		}

		/// <summary>
		/// Sends a message to the specified connections; takes ownership of the NetBuffer, don't reuse it after this call
		/// </summary>
		public void SendMessage(NetBuffer data, IEnumerable<NetConnection> recipients, NetChannel channel)
		{
			if (recipients == null)
				throw new ArgumentNullException("recipients");

			foreach (NetConnection recipient in recipients)
				recipient.SendMessage(data, channel);
		}

		/// <summary>
		/// Sends a message to all connections to this server
		/// </summary>
		public void SendToAll(NetBuffer data, NetChannel channel)
		{
			foreach (NetConnection conn in m_connections)
			{
				if (conn.Status == NetConnectionStatus.Connected)
					conn.SendMessage(data, channel);
			}
		}

		/// <summary>
		/// Sends a message to all connections to this server, except 'exclude'
		/// </summary>
		public void SendToAll(NetBuffer data, NetChannel channel, NetConnection exclude)
		{
			foreach (NetConnection conn in m_connections)
			{
				if (conn.Status == NetConnectionStatus.Connected && conn != exclude)
					conn.SendMessage(data, channel);
			}
		}

		internal override void HandleConnectionForciblyClosed(NetConnection connection, SocketException sex)
		{
			if (connection != null)
				connection.Disconnect("Connection forcibly closed", 0, false, false);
			return;
		}

		public override void Shutdown(string reason)
		{
			double now = NetTime.Now;
			foreach (NetConnection conn in m_connections)
			{
				if (conn.m_status != NetConnectionStatus.Disconnected)
				{
					conn.Disconnect(reason, 0, true, true);
					conn.SendUnsentMessages(now); // give disconnect message a chance to get out
				}
			}
			base.Shutdown(reason);
		}
	}
}
