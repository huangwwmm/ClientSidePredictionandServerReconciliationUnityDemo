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

#define USE_RELEASE_SIMULATION
#define USE_RELEASE_STATISTICS

using System;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using uLink;

// TODO: if recv and send buffer size for custom socket returns 0 then silently ignore it.

namespace Lidgren.Network
{
	/// <summary>
	/// Base class for NetClient, NetServer and NetPeer
	/// </summary>
	internal abstract partial class NetBase : IDisposable
	{
		internal NetworkSocket m_socket;
		private NetworkEndPoint m_senderRemote;
		internal bool m_isBound;
		internal byte[] m_localRndSignature;
		internal NetDiscovery m_discovery;

		protected bool m_shutdownComplete;
		protected NetFrequencyCounter m_heartbeatCounter;

		// ready for reading by the application
		internal NetQueue<IncomingNetMessage> m_receivedMessages;
		private NetQueue<NetBuffer> m_unsentOutOfBandMessages;
		private NetQueue<NetworkEndPoint> m_unsentOutOfBandRecipients;
		private Queue<SUSystemMessage> m_susmQueue;
		//internal List<NetworkEndPoint> m_holePunches;
		private double m_lastHolePunch;

		internal NetConfiguration m_config;
		internal NetBuffer m_receiveBuffer;
		internal NetBuffer m_sendBuffer;
		internal NetBuffer m_tempBuffer;

		internal NetMessageType m_enabledMessageTypes;

		internal double m_localTimeOffset;

		public double LocalTimeOffset { get { return m_localTimeOffset; } set { m_localTimeOffset = value; } }

		/// <summary>
		/// Gets or sets what types of messages are delivered to the client
		/// </summary>
		public NetMessageType EnabledMessageTypes { get { return m_enabledMessageTypes; } set { m_enabledMessageTypes = value; } }

		/// <summary>
		/// Average number of heartbeats performed per second; over a 3 seconds window
		/// </summary>
		public float HeartbeatAverageFrequency { get { return m_heartbeatCounter.AverageFrequency; } }
		
		/// <summary>
		/// Enables or disables a particular type of message
		/// </summary>
		public void SetMessageTypeEnabled(NetMessageType type, bool enabled)
		{
			if (enabled)
			{
#if !DEBUG
				if ((type | NetMessageType.DebugMessage) == NetMessageType.DebugMessage)
					throw new NetException("Not possible to enable Debug messages in a Release build!");
				if ((type | NetMessageType.VerboseDebugMessage) == NetMessageType.VerboseDebugMessage)
					throw new NetException("Not possible to enable VerboseDebug messages in a Release build!");
#endif
				m_enabledMessageTypes |= type;
			}
			else
			{
				m_enabledMessageTypes &= (~type);
			}
		}

		/// <summary>
		/// Gets the configuration for this NetBase instance
		/// </summary>
		public NetConfiguration Configuration { get { return m_config; } }

		public NetworkEndPoint ListenEndPoint
		{
			get
			{
				return m_socket.listenEndPoint;
			}
		}
		/// <summary>
		/// Gets which port this netbase instance listens on, or -1 if it's not listening.
		/// </summary>
		public int ListenPort
		{
			get
			{
				if (m_isBound)
					return ((NetworkEndPoint)m_socket.listenEndPoint).port;
				return NetworkEndPoint.unassignedPort;
			}
		}

		/// <summary>
		/// Is the instance listening on the socket?
		/// </summary>
		public bool IsListening { get { return m_isBound; } }

		protected NetBase(NetConfiguration config)
		{
			Debug.Assert(config != null, "Config must not be null");
			if (string.IsNullOrEmpty(config.ApplicationIdentifier))
				throw new ArgumentException("Must set ApplicationIdentifier in NetConfiguration!");
			m_config = config;
			m_receiveBuffer = new NetBuffer(config.ReceiveBufferSize);
			m_sendBuffer = new NetBuffer(config.SendBufferSize);
			//by WuNan @2016/09/28 14:26:34
			#if (UNITY_IOS  || UNITY_TVOS ) && !UNITY_EDITOR

			if(uLink.NetworkUtility.IsSupportIPv6())
			{
				m_senderRemote = (EndPoint)new NetworkEndPoint(IPAddress.IPv6Any, 0);
			}
			else
			{
				m_senderRemote = (EndPoint)new NetworkEndPoint(IPAddress.Any, 0);
			}

			#else
			m_senderRemote = (EndPoint)new NetworkEndPoint(IPAddress.Any, 0);
			#endif
			m_statistics = new NetBaseStatistics();
			m_receivedMessages = new NetQueue<IncomingNetMessage>(4);
			m_tempBuffer = new NetBuffer(256);
			m_discovery = new NetDiscovery(this);
			m_heartbeatCounter = new NetFrequencyCounter(3.0f);

			m_localRndSignature = new byte[NetConstants.SignatureByteSize];
			NetRandom.Instance.NextBytes(m_localRndSignature);

			m_unsentOutOfBandMessages = new NetQueue<NetBuffer>();
			m_unsentOutOfBandRecipients = new NetQueue<NetworkEndPoint>();
			m_susmQueue = new Queue<SUSystemMessage>();

			// default enabled message types
			m_enabledMessageTypes =
				NetMessageType.Data | NetMessageType.StatusChanged;
		}

		internal void EnqueueReceivedMessage(IncomingNetMessage msg)
		{
			m_receivedMessages.Enqueue(msg);				
		}

		/// <summary>
		/// Creates an outgoing net message
		/// </summary>
		internal OutgoingNetMessage CreateOutgoingMessage()
		{
			// no recycling for messages
			OutgoingNetMessage msg = new OutgoingNetMessage();
			msg.m_sequenceNumber = -1;
			msg.m_numSent = 0;
			msg.m_nextResend = double.MaxValue;
			msg.m_msgType = NetMessageType.Data;
			msg.m_data = CreateBuffer();
			return msg;
		}

		/// <summary>
		/// Creates an incoming net message
		/// </summary>
		internal IncomingNetMessage CreateIncomingMessage()
		{
			// no recycling for messages
			IncomingNetMessage msg = new IncomingNetMessage();
			msg.m_msgType = NetMessageType.Data;
			msg.m_data = CreateBuffer();
			return msg;
		}

		/// <summary>
		/// Called to bind to socket and start heartbeat thread.
		/// The socket will be bound to listen on any network interface unless the <see cref="NetConfiguration.Address"/> explicitly specifies an interface. 
		/// </summary>
		public void Start()
		{
			if (m_isBound)
				return;
			
			// TODO: this check should be done somewhere earlier, in uLink preferably.
			if (m_config.StartPort > m_config.EndPort)
			{
				throw new NetException("The start port (" + m_config.StartPort + ") must be less or equal to the end port (" + m_config.EndPort + ")");
			}

			//by WuNan @2016/09/28 14:26:22
			var bindIP = String.IsNullOrEmpty(m_config.AddressStr) ?
#if (UNITY_IOS  || UNITY_TVOS ) && !UNITY_EDITOR				
				(uLink.NetworkUtility.IsSupportIPv6() ? IPAddress.IPv6Any : IPAddress.Any) : NetUtility.Resolve(m_config.AddressStr);
#else
				IPAddress.Any : NetUtility.Resolve(m_config.AddressStr);
#endif

			Log.Debug(LogFlags.Socket, "Creating non-blocking UDP socket");

			var sock = NetworkSocket.Create(m_config.SocketType);

			Log.Debug(LogFlags.Socket, "Successfully created Socket");

			for (int port = m_config.StartPort; port <= m_config.EndPort; port++)
			{
				try
				{
					sock.Bind(new NetworkEndPoint(bindIP, port));

					m_isBound = true;
					break;
				}
				catch (SocketException ex)
				{
					Log.Debug(LogFlags.Socket, "Failed to bind to specific port ", port, ": ", ex);
					
					if (port == m_config.EndPort)
					{
						try
						{
							sock.Close(0);
						}
						catch
						{
						}

						throw new NetException("Failed to bind to port range " + m_config.StartPort + "-" + m_config.EndPort, ex);
					}
				}
			}

			m_socket = sock;

			LogWrite("Listening on " + m_socket.listenEndPoint);

			if (m_config.ReceiveBufferSize != 0)
			{
				try
				{
					m_socket.receiveBufferSize = m_config.ReceiveBufferSize;
					m_config.ReceiveBufferSize = m_socket.receiveBufferSize; // make sure we have the actual size
				}
				catch (Exception ex)
				{
					Log.Warning(LogFlags.Socket, "Unable to set socket ", SocketOptionName.ReceiveBuffer, " size to ", m_config.ReceiveBufferSize, ": ", ex);
				}
			}
			else
			{
				try
				{
					m_config.ReceiveBufferSize = m_socket.receiveBufferSize;

					Log.Debug(LogFlags.Socket, "Socket ", SocketOptionName.ReceiveBuffer, " is set to OS-specific default ", m_config.ReceiveBufferSize);
				}
				catch (Exception ex)
				{
					Log.Warning(LogFlags.Socket, "Unable to get socket ", SocketOptionName.ReceiveBuffer, ": ", ex);
				}
			}

			if (m_config.SendBufferSize != 0)
			{
				try
				{
					m_socket.sendBufferSize = m_config.SendBufferSize;
					m_config.SendBufferSize = m_socket.sendBufferSize; // make sure we have the actual size
				}
				catch (Exception ex)
				{
					Log.Warning(LogFlags.Socket, "Unable to set socket ", SocketOptionName.SendBuffer, " size to ", m_config.SendBufferSize, ": ", ex);
				}
			}
			else
			{
				try
				{
					m_config.SendBufferSize = m_socket.sendBufferSize;

					Log.Debug(LogFlags.Socket, "Socket ", SocketOptionName.SendBuffer, " is set to OS-specific default ", m_config.SendBufferSize);
				}
				catch (Exception ex)
				{
					Log.Warning(LogFlags.Socket, "Unable to get socket ", SocketOptionName.SendBuffer, ": ", ex);
				}
			}

			m_receiveBuffer.EnsureBufferSizeInBytes(m_config.ReceiveBufferSize);
			m_sendBuffer.EnsureBufferSizeInBytes(m_config.SendBufferSize);

			// TODO: ugly hack to determine if server
			if (this is NetServer)
			{
				m_socket.Listen(m_config.MaxConnections);
			}

			// display simulated networking conditions in debug log
			if (m_simulatedLoss > 0.0f)
				LogWrite("Simulating " + (m_simulatedLoss * 100.0f) + "% loss");
			if (m_simulatedMinimumLatency > 0.0f || m_simulatedLatencyVariance > 0.0f)
				LogWrite("Simulating " + ((int)(m_simulatedMinimumLatency * 1000.0f)) + " - " + NetTime.ToMillis(m_simulatedMinimumLatency + m_simulatedLatencyVariance) + " ms roundtrip latency");
			if (m_simulatedDuplicateChance > 0.0f)
				LogWrite("Simulating " + (m_simulatedDuplicateChance * 100.0f) + "% chance of packet duplication");

			if (m_config.m_throttleBytesPerSecond > 0)
				LogWrite("Throttling to " + m_config.m_throttleBytesPerSecond + " bytes per second");

			m_isBound = true;
			m_shutdownComplete = false;
			m_statistics.Reset();
		}

		/// <summary>
		/// Reads all packets and create messages
		/// </summary>
		protected void BaseHeartbeat(double now)
		{
			// discovery
			NetProfiler.BeginSample("_CheckLocalDiscovery");
			m_discovery.Heartbeat(now);
			NetProfiler.EndSample();

			// Send queued system messages
			NetProfiler.BeginSample("_SendQueuedSystemMessages");
			SendQueuedSystemMessages();
			NetProfiler.EndSample();

			// Send out-of-band messages
			NetProfiler.BeginSample("_SendQueuedUnconnectedMessages");
			SendQueuedOutOfBandMessages();
			NetProfiler.EndSample();

//#if DEBUG
			NetProfiler.BeginSample("_SendLagSimulatedPackets");
			SendDelayedPackets(now);
			NetProfiler.EndSample();
//#endif

			// TODO: move this up in order
			NetProfiler.BeginSample("_ReceivePackets");
			ReceivePackets(now);
			NetProfiler.EndSample();
		}

		private void SendQueuedSystemMessages()
		{
			if (m_susmQueue.Count > 0)
			{
				while (m_susmQueue.Count > 0)
				{
					SUSystemMessage su = m_susmQueue.Dequeue();
					SendSingleUnreliableSystemMessage(su.Type, su.Data, su.Destination, null);
				}
			}
		}

		private void SendQueuedOutOfBandMessages()
		{
			if (m_unsentOutOfBandMessages.Count > 0)
			{
				while (m_unsentOutOfBandMessages.Count > 0)
				{
					NetBuffer buf = m_unsentOutOfBandMessages.Dequeue();
					NetworkEndPoint ep = m_unsentOutOfBandRecipients.Dequeue();
					DoSendOutOfBandMessage(buf, ep);
				}
			}
		}

		private void ReceivePackets(double now)
		{
			int networkResets = 0;
			int connectionResets = 0;
			int availableData = 0;

			try
			{
				for (;;)
				{
					try
					{
						if (m_socket == null) return;

						availableData = m_socket.availableData;
						if (availableData <= 0) return;

						m_receiveBuffer.Reset();

						double localTimeRecv;
						int bytesReceived = m_socket.ReceivePacket(m_receiveBuffer.Data, 0, m_receiveBuffer.Data.Length, out m_senderRemote, out localTimeRecv);
						if (bytesReceived <= 0) continue;

						m_statistics.CountPacketReceived(bytesReceived);
						m_receiveBuffer.LengthBits = bytesReceived * 8;

						var ipsender = (NetworkEndPoint)m_senderRemote;

						if (localTimeRecv == 0) localTimeRecv = NetTime.Now;

						// lookup the endpoint (if we have a connection)
						var sender = GetConnection(ipsender);
						if (sender != null)
						{
							sender.m_statistics.CountPacketReceived(bytesReceived, localTimeRecv);
							sender.m_lastPongReceived = localTimeRecv; // TODO: fulhack!
						}

						// try parsing the packet
						try
						{
							// create messages from packet
							while (m_receiveBuffer.PositionBits < m_receiveBuffer.LengthBits)
							{
								int beginPosition = m_receiveBuffer.PositionBits;

								// read message header
								var msg = CreateIncomingMessage();
								msg.m_sender = sender;
								if (!msg.ReadFrom(m_receiveBuffer, ipsender)) break;

								// statistics
								if (sender != null)
								{
									sender.m_statistics.CountMessageReceived(msg.m_type, msg.m_sequenceChannel, (m_receiveBuffer.PositionBits - beginPosition) / 8, now);
								}

								// handle message
								HandleReceivedMessage(msg, ipsender, localTimeRecv);
							}
						}
						catch (Exception e) // silent exception handlers make debugging impossible, comment out when you wonder "WTF is happening???"
						{
							// TODO: fix so logging doesn't allocate an argument array (for using params object[]).
							// Enabling us to hopefully do to log calls without overhead, if the log level isn't on.

#if DEBUG // NOTE: in production it might not be desirable to log bad packets, because of targeted DoS attacks.
							Log.Error(LogFlags.Socket, "Failed parsing an incoming packet from ", ipsender, ": ", e);
#endif
						}
					}
					catch (SocketException sex)
					{
						if (sex.ErrorCode == 10040 || sex.SocketErrorCode == SocketError.MessageSize)
						{
							int bufferSize = m_receiveBuffer.Data.Length;
							Log.Error(LogFlags.Socket, "Failed to read available data (", availableData, " bytes) because it's larger than the receive buffer (", bufferSize, " bytes). This might indicate that the socket receive buffer size is not correctly configured: ", sex);
						}
						
						if (sex.ErrorCode == 10052 || sex.SocketErrorCode == SocketError.NetworkReset)
						{
							networkResets++;
							if (networkResets > 2000) break;
						}
						else if (sex.ErrorCode == 10054 || sex.SocketErrorCode == SocketError.ConnectionReset)
						{
							connectionResets++;
							if (connectionResets > 2000) break;
						}
						else
						{
							throw;
						}
					}
				}
			}
			catch (SocketException sex)
			{
				if (sex.ErrorCode == 10035 || sex.SocketErrorCode == SocketError.WouldBlock)
				{
					// NOTE: add independent ability to log warnings instead of this hack.
					Log.Warning(LogFlags.Socket, "Receive Buffer is empty, ignore error: ", sex.Message);

					return;
				}

				throw new NetException(NetTime.Now + " (" + DateTime.Now + "): Could not receive packet", sex);
			}
			catch (Exception ex)
			{
				throw new NetException(NetTime.Now + " (" + DateTime.Now + "): Could not receive packet", ex);
			}
			finally
			{
				if (networkResets > 0)
					Log.Debug(LogFlags.Socket, NetTime.Now, " (", DateTime.Now, "): Ignored number of socket network reset errors: ", networkResets);

				if (connectionResets > 0)
					Log.Debug(LogFlags.Socket, NetTime.Now, " (", DateTime.Now, "): Ignored number of socket connection reset errors: ", connectionResets);
			}
		}

		public abstract void Heartbeat();

		public abstract NetConnection GetConnection(NetworkEndPoint remoteEndpoint);

		internal abstract void HandleReceivedMessage(IncomingNetMessage message, NetworkEndPoint senderEndpoint, double timestamp);

		internal abstract void HandleConnectionForciblyClosed(NetConnection connection, SocketException sex);

		/// <summary>
		/// Notify application that a connection changed status
		/// </summary>
		internal void NotifyStatusChange(NetConnection connection, string reason)
		{
			if ((m_enabledMessageTypes & NetMessageType.StatusChanged) != NetMessageType.StatusChanged)
				return; // disabled
			
			//NotifyApplication(NetMessageType.StatusChanged, reason, connection);
			NetBuffer buffer = CreateBuffer(reason.Length + 2);
			buffer.Write(reason);
			buffer.Write((byte)connection.Status);

			IncomingNetMessage msg = new IncomingNetMessage();
			msg.m_data = buffer;
			msg.m_msgType = NetMessageType.StatusChanged;
			msg.m_sender = connection;
			msg.m_senderEndPoint = NetworkEndPoint.unassigned;

			EnqueueReceivedMessage(msg);
		}
		
		internal OutgoingNetMessage CreateSystemMessage(NetSystemType systemType)
		{
			OutgoingNetMessage msg = CreateOutgoingMessage();
			msg.m_type = NetMessageLibraryType.System;
			msg.m_sequenceChannel = NetChannel.Unreliable;
			msg.m_sequenceNumber = 0;
			msg.m_data.Write((byte)systemType);
			return msg;
		}

		/// <summary>
		/// Send a single, out-of-band unreliable message
		/// </summary>
		public void SendOutOfBandMessage(NetBuffer data, NetworkEndPoint recipient)
		{
			m_unsentOutOfBandMessages.Enqueue(data);
			m_unsentOutOfBandRecipients.Enqueue(recipient);
		}

		/// <summary>
		/// Send a NAT introduction messages to one and two, allowing them to connect
		/// </summary>
		public void SendNATIntroduction(
			NetworkEndPoint one,
			NetworkEndPoint two
		)
		{
			NetBuffer toOne = CreateBuffer();
			toOne.Write(two);
			QueueSingleUnreliableSystemMessage(NetSystemType.NatIntroduction, toOne, one, false);

			NetBuffer toTwo = CreateBuffer();
			toTwo.Write(one);
			QueueSingleUnreliableSystemMessage(NetSystemType.NatIntroduction, toTwo, two, false);
		}

		/// <summary>
		/// Send a NAT introduction messages to ONE about contacting TWO
		/// </summary>
		public void SendSingleNATIntroduction(
			NetworkEndPoint one,
			NetworkEndPoint two
		)
		{
			NetBuffer toOne = CreateBuffer();
			toOne.Write(two);
			QueueSingleUnreliableSystemMessage(NetSystemType.NatIntroduction, toOne, one, false);
		}

		/// <summary>
		/// Send a single, out-of-band unreliable message
		/// </summary>
		internal void DoSendOutOfBandMessage(NetBuffer data, NetworkEndPoint recipient)
		{
			var sendBuffer = m_sendBuffer;

			sendBuffer.Reset();

			// message type and channel
			sendBuffer.Write((byte)((int)NetMessageLibraryType.OutOfBand | ((int)NetChannel.Unreliable << 3)));

			sendBuffer.Write((ushort)0); //TODO remove to save 2 bytes (DAVID)

			// payload length; variable byte encoded
			if (data == null)
			{
				sendBuffer.WriteVariableUInt32((uint)0);
			}
			else
			{
				int dataLen = data.LengthBytes;
				sendBuffer.WriteVariableUInt32((uint)(dataLen));
				sendBuffer.Write(data.Data, 0, dataLen);
			}

			if (recipient.isBroadcast)
			{
				bool wasSSL = m_suppressSimulatedLag;
				try
				{
					m_suppressSimulatedLag = true;
					//m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
					SendPacket(recipient);
				}
				finally
				{
					//m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
					m_suppressSimulatedLag = wasSSL;
				}
			}
			else
			{
				SendPacket(recipient);
			}

			// unreliable; we can recycle this immediately
			RecycleBuffer(data);
		}

		/// <summary>
		/// Thread-safe SendSingleUnreliableSystemMessage()
		/// </summary>
		internal void QueueSingleUnreliableSystemMessage(
			NetSystemType tp,
			NetBuffer data,
			NetworkEndPoint remoteEP,
			bool useBroadcast)
		{
			var susm = new SUSystemMessage();
			susm.Type = tp;
			susm.Data = data;
			susm.Destination = remoteEP;
			susm.UseBroadcast = useBroadcast;
			m_susmQueue.Enqueue(susm);
		}

		/// <summary>
		/// Pushes a single system message onto the wire directly
		/// </summary>
		internal void SendSingleUnreliableSystemMessage(
			NetSystemType tp,
			NetBuffer data,
			NetworkEndPoint remoteEP,
			NetConnection connection)
		{
			// packet number
			var sendBuffer = m_sendBuffer;

			sendBuffer.Reset();

			// message type and channel
			sendBuffer.Write((byte)((int)NetMessageLibraryType.System | ((int)NetChannel.Unreliable << 3)));
			//TODO: remove this line (David)
			sendBuffer.Write((ushort)0);

			// payload length; variable byte encoded
			if (data == null)
			{
				sendBuffer.WriteVariableUInt32((uint)1);
				sendBuffer.Write((byte)tp);
			}
			else
			{
				int dataLen = data.LengthBytes;
				sendBuffer.WriteVariableUInt32((uint)(dataLen + 1));
				sendBuffer.Write((byte)tp);
				sendBuffer.Write(data.Data, 0, dataLen);
			}

			SendPacket(m_sendBuffer.Data, m_sendBuffer.LengthBytes, remoteEP, connection);
		}

		/// <summary>
		/// Pushes a single packet onto the wire from m_sendBuffer
		/// </summary>
		/// <returns>True if the packet was sent or dropped (i.e. "completed"), false if non-blocking return.</returns>
		internal bool SendPacket(NetworkEndPoint remoteEP)
		{
			return SendPacket(m_sendBuffer.Data, m_sendBuffer.LengthBytes, remoteEP);
		}

		internal bool SendPacket(NetConnection connection)
		{
			return SendPacket(m_sendBuffer.Data, m_sendBuffer.LengthBytes, connection.RemoteEndpoint, connection);
		}

		internal bool SendPacket(byte[] data, int length, NetworkEndPoint remoteEP)
		{
			return SendPacket(data, length, remoteEP, null);
		}

		/// <summary>
		/// Pushes a single packet onto the wire
		/// </summary>
		/// <returns>True if the packet was sent or dropped (i.e. "completed"), false if non-blocking return.</returns>
		internal bool SendPacket(byte[] data, int length, NetworkEndPoint remoteEP, NetConnection connection)
		{
			if (length <= 0 || length > m_config.SendBufferSize)
			{
				string str = "Invalid packet size " + length + "; Must be between 1 and NetConfiguration.SendBufferSize - Invalid value: " + length;
				LogWrite(str);
				throw new NetException(str);
			}

			if (!m_isBound)
				Start();

#if DEBUG || USE_RELEASE_SIMULATION
			if (!m_suppressSimulatedLag)
			{
				bool send = SimulatedSendPacket(data, length, remoteEP);
				if (!send)
				{
					m_statistics.CountPacketSent(length);
					return true;
				}
			}
#endif
			int orgLength = length;

			// TODO: cleanup fulhaacks: packets below 60 or 64 are padded, if too low sometimes even dropped!
			if (length < 18 && data.Length - length >= 18) // 18 udp payload + 42 udp header = 60 bytes
			{
				for (int i = length; i < 18; i++) data[i] = 0;
				length = 18;
			}

			try
			{
				//m_socket.SendTo(data, 0, length, SocketFlags.None, remoteEP);

				//the original CPU busy polling loop wasn't very acceptable in PikkoServer ^^
				int bytesSent;
				//do
				//{
				bytesSent = m_socket.SendPacket(data, 0, length, remoteEP);
				//} while (bytesSent == 0);

				if (bytesSent > 0 && bytesSent < orgLength)
					Log.Warning(LogFlags.Socket, NetTime.Now, " (", DateTime.Now, "): Socket sent fewer bytes of the packet (", bytesSent, ") than requested (", orgLength, ")");
				else if (bytesSent > length)
					Log.Warning(LogFlags.Socket, NetTime.Now, " (", DateTime.Now, "): Socket sent more bytes of the packet (", bytesSent, ") than requested and padding (", length, ")");

				//LogVerbose("Sent " + bytesSent + " bytes");
#if DEBUG || USE_RELEASE_STATISTICS
				if (!m_suppressSimulatedLag && bytesSent > 0)
					m_statistics.CountPacketSent(orgLength);
#endif
				return bytesSent > 0;
			}
			catch (SocketException sex)
			{
				if (sex.ErrorCode == 10035 || sex.SocketErrorCode == SocketError.WouldBlock)
				{
					// NOTE: add independent ability to log warnings instead of this hack.
					Log.Warning(LogFlags.Socket, "Send buffer is full, if you see this too often it might be a sign that you need to increase the send buffer size: ", sex.Message);

#if PIKKO_BUILD
					PikkoServer.PikkoStats.IncrementSendBufferDrops();
#endif
					return true; //drop packet instead of trying to resend it
				}

				if (sex.ErrorCode == 10065 || sex.SocketErrorCode == SocketError.HostUnreachable)
				{
					if (connection != null)
					{
						Log.Warning(LogFlags.Socket, "Disconnecting from ", remoteEP, " because it's not reachable or internet/network connection is unstable: ", sex.Message);

						connection.Disconnect("Host Unreachable", 0, false, true);
					}
					else
					{
						Log.Warning(LogFlags.Socket, "Packet has been dropped because either ", remoteEP, " is unreachable or internet/network connection is unstable: ", sex.Message);
					}

					return true;
				}
				/*
				if (sex.SocketErrorCode == SocketError.ConnectionReset ||
					sex.SocketErrorCode == SocketError.ConnectionRefused ||
					sex.SocketErrorCode == SocketError.ConnectionAborted)
				{
					LogWrite("Remote socket forcefully closed: " + sex.SocketErrorCode);
					// TODO: notify connection somehow?
					return;
				}
				*/

				throw new NetException(NetTime.Now + " (" + DateTime.Now + "): Could not send packet (" + length + " bytes) to " + remoteEP, sex);
			}
			catch (Exception ex)
			{
				throw new NetException(NetTime.Now + " (" + DateTime.Now + "): Could not send packet (" + length + " bytes) to " + remoteEP, ex);
			}
		}

		/// <summary>
		/// Emit receipt event
		/// </summary>
		internal void FireReceipt(NetConnection connection, NetBuffer receiptData)
		{
			if ((m_enabledMessageTypes & NetMessageType.Receipt) != NetMessageType.Receipt)
				return; // disabled

			IncomingNetMessage msg = CreateIncomingMessage();
			msg.m_sender = connection;
			msg.m_msgType = NetMessageType.Receipt;
			msg.m_data = receiptData;

			EnqueueReceivedMessage(msg);
		}

		/// <summary>
		/// Reads the content of an available message into 'intoBuffer' and returns true. If no message is available it returns false.
		/// </summary>
		/// <returns>true if a message was read</returns>
		public bool ReadMessage(NetBuffer intoBuffer, out NetMessageType type)
		{
			NetworkEndPoint senderEndPoint; // unused
			return ReadMessage(intoBuffer, out type, out senderEndPoint);
		}

		public bool ReadMessage(NetBuffer intoBuffer, out NetMessageType type, out NetworkEndPoint senderEndpoint)
		{
			NetChannel channel; // unused
			return ReadMessage(intoBuffer, out type, out senderEndpoint, out channel);
		}

		public bool ReadMessage(NetBuffer intoBuffer, out NetMessageType type, out NetworkEndPoint senderEndpoint, out NetChannel channel)
		{
			NetConnection sender; // unused
			return ReadMessage(intoBuffer, out type, out sender, out senderEndpoint, out channel);
		}

		public bool ReadMessage(NetBuffer intoBuffer, out NetMessageType type, out NetConnection sender, out NetworkEndPoint senderEndpoint)
		{
			NetChannel channel; // unused
			return ReadMessage(intoBuffer, out type, out sender, out senderEndpoint, out channel);
		}

		public bool ReadMessage(
			NetBuffer intoBuffer,
			out NetMessageType type,
			out NetConnection sender,
			out NetworkEndPoint senderEndPoint,
			out NetChannel channel)
		{
			double localTimeRecv; // unused
			return ReadMessage(intoBuffer, out type, out sender, out senderEndPoint, out channel, out localTimeRecv);
		}

		public bool ReadMessage(
			NetBuffer intoBuffer,
			out NetMessageType type,
			out NetConnection sender,
			out NetworkEndPoint senderEndPoint,
			out NetChannel channel,
			out double localTimeRecv)
		{
			if (intoBuffer == null)
				throw new ArgumentNullException("intoBuffer");

			if (m_receivedMessages.Count < 1)
			{
				type = NetMessageType.None;
				sender = null;
				senderEndPoint = NetworkEndPoint.unassigned;
				channel = NetChannel.Unreliable;
				localTimeRecv = 0;
				return false;
			}

			IncomingNetMessage msg = m_receivedMessages.Dequeue();

			if (msg == null)
			{
				type = NetMessageType.None;
				sender = null;
				senderEndPoint = NetworkEndPoint.unassigned;
				channel = NetChannel.Unreliable;
				localTimeRecv = 0;
				return false;
			}

			senderEndPoint = msg.m_senderEndPoint;
			sender = msg.m_sender;
			localTimeRecv = NetTime.Now; // TODO: this is a ugly hack, should instead get value when reading from socket. Merging/switching to Speedgren will solve that.
			channel = msg.m_sequenceChannel;

			// recycle NetMessage object
			var content = msg.m_data;

			msg.m_data = null;
			type = msg.m_msgType;

			if (content == null)
			{
				intoBuffer.m_bitLength = 0;
				intoBuffer.m_readPosition = 0;
				return true;
			}

			// swap content of buffers
			byte[] tmp = intoBuffer.Data;
			intoBuffer.Data = content.Data;
			content.Data = tmp;

			// set correct values for returning value (ignore the other, it's being recycled anyway)
			intoBuffer.m_bitLength = content.m_bitLength;
			intoBuffer.m_readPosition = 0;

			// recycle buffer
			RecycleBuffer(content);

			return true;
		}

		[Conditional("DEBUG")]
		internal void LogWrite(string message)
		{
			if ((m_enabledMessageTypes & NetMessageType.DebugMessage) != NetMessageType.DebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.DebugMessage, message, null); //sender);
		}

		[Conditional("DEBUG")]
		internal void LogVerbose(string message)
		{
			if ((m_enabledMessageTypes & NetMessageType.VerboseDebugMessage) != NetMessageType.VerboseDebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.VerboseDebugMessage, message, null); //sender);
		}

		[Conditional("DEBUG")]
		internal void LogWrite(string message, NetConnection connection)
		{
			if ((m_enabledMessageTypes & NetMessageType.DebugMessage) != NetMessageType.DebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.DebugMessage, message, connection);
		}

		[Conditional("DEBUG")]
		internal void LogVerbose(string message, NetConnection connection)
		{
			if ((m_enabledMessageTypes & NetMessageType.VerboseDebugMessage) != NetMessageType.VerboseDebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.VerboseDebugMessage, message, connection);
		}

		internal void NotifyApplication(NetMessageType tp, NetConnection conn)
		{
			NetBuffer buf = null;
			NotifyApplication(tp, buf, conn);
		}

		internal void NotifyApplication(NetMessageType tp, string message, NetConnection conn)
		{
			NetBuffer buf = CreateBuffer(message);
			NotifyApplication(tp, buf, conn);
		}
		
		internal void NotifyApplication(NetMessageType tp, string message, NetConnection conn, NetworkEndPoint ep)
		{
			NetBuffer buf = CreateBuffer(message);
			NotifyApplication(tp, buf, conn, ep);
		}

		internal void NotifyApplication(NetMessageType tp, NetBuffer buffer, NetConnection conn)
		{
			NotifyApplication(tp, buffer, conn, NetworkEndPoint.unassigned);
		}

		internal void NotifyApplication(NetMessageType tp, NetBuffer buffer, NetConnection conn, NetworkEndPoint ep)
		{
			IncomingNetMessage msg = new IncomingNetMessage();
			msg.m_data = buffer;
			msg.m_msgType = tp;
			msg.m_sender = conn;
			msg.m_senderEndPoint = ep;

			EnqueueReceivedMessage(msg);
		}

		internal NetBuffer GetTempBuffer()
		{
			m_tempBuffer.Reset();
			return m_tempBuffer;
		}

		/// <summary>
		/// Override this to process a received NetBuffer on the networking thread (note! This can be problematic, only use this if you know what you are doing)
		/// </summary>
		public virtual void ProcessReceived(NetBuffer buffer)
		{
		}

		public virtual void Shutdown(string reason)
		{
			LogWrite("Performing shutdown (" + reason + ")");
//#if DEBUG
			// just send all delayed packets; since we won't have the possibility to do it after socket is closed
			SendDelayedPackets(NetTime.Now + this.SimulatedMinimumLatency + this.SimulatedLatencyVariance + 1000.0);
//#endif

			try
			{
				if (m_socket != null)
				{

					// This throws an exception under mono for linux, so we just ingnore it.
					try
					{
						//m_socket.Shutdown(SocketShutdown.Receive);
					}
					catch (SocketException){}
					m_socket.Close(2);
					
				}
			}
			finally
			{
				m_socket = null;
				m_isBound = false;
			}
			m_shutdownComplete = true;

			LogWrite("Socket closed");
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~NetBase()
		{
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		protected void Dispose(bool disposing)
		{
			// Unless we're already shut down, this is the equivalent of killing the process
			m_shutdownComplete = true;
			m_isBound = false;
			if (disposing)
			{
				if (m_socket != null)
				{
					m_socket.Close(0);
					m_socket = null;
				}
			}
		}
	}

	internal sealed class SUSystemMessage
	{
		public NetSystemType Type;
		public NetBuffer Data;
		public NetworkEndPoint Destination;
		public bool UseBroadcast;
	}
}
