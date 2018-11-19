using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Diagnostics;
using uLink;

namespace Lidgren.Network
{
	/// <summary>
	/// Represents a connection between this host and a remote endpoint
	/// </summary>
	[DebuggerDisplay("RemoteEndpoint = {m_remoteEndPoint}")]
	internal partial class NetConnection
	{
		private NetBase m_owner;
		internal NetworkEndPoint m_remoteEndPoint;
		internal NetQueue<OutgoingNetMessage> m_unsentMessages = new NetQueue<OutgoingNetMessage>(6);
		internal NetConnectionStatus m_status;
		private byte[] m_localHailData; // outgoing hail data
		internal byte[] m_remoteRndSignature;
		internal int m_remoteRndSeqNr;
		internal int m_localRndSeqNr;
		private byte[] m_remoteHailData; // incoming hail data (with connect or response)
		private double m_ackWithholdingStarted;
		private float m_throttleDebt;
		private double m_lastSentUnsentMessages;
		internal bool m_approved;

		private bool m_requestDisconnect;
		private float m_requestLinger;
		private bool m_requestSendGoodbye;
		private double m_futureClose;
		private string m_futureDisconnectReason;

		private bool m_isInitiator; // if true: we sent Connect; if false: we received Connect
		internal double m_handshakeInitiated;
		private int m_handshakeAttempts;
		private string m_applicationIdentifier = "";

		internal double m_connectLocalSentTime;
		internal double m_connectRemoteRecvTime;

		/// <summary>
		/// Gets or sets local (outgoing) hail data
		/// </summary>
		public byte[] LocalHailData { get { return m_localHailData; } set { m_localHailData = value; } }

		/// <summary>
		/// Gets remote (incoming) hail data, if available
		/// </summary>
		public byte[] RemoteHailData { get { return m_remoteHailData; } }

		/// <summary>
		/// Remote endpoint for this connection
		/// </summary>
		public NetworkEndPoint RemoteEndpoint { get { return m_remoteEndPoint; } }

		/// <summary>
		/// For application use
		/// </summary>
		public object Tag = null;

		/// <summary>
		/// Number of message which has not yet been sent
		/// </summary>
		public int UnsentMessagesCount { get { return m_unsentMessages.Count; } }

		/// <summary>
		/// Gets the status of the connection
		/// </summary>
		public NetConnectionStatus Status { get { return m_status; } }

		public string GetStatusString()
		{
			switch (m_status)
			{
				case NetConnectionStatus.Connected: return "Connected";
				case NetConnectionStatus.Connecting: return "Connecting";
				case NetConnectionStatus.Reconnecting: return "Reconnecting";
				case NetConnectionStatus.Disconnecting: return "Disconnecting";
				case NetConnectionStatus.Disconnected: return "Disconnected";
			}

			return "Undefined";
		}

		internal void SetStatus(NetConnectionStatus status, string reason)
		{
			// Connecting status is given to NetConnection at startup, so we must treat it differently
			if (m_status == status && status != NetConnectionStatus.Connecting)
				return;

			//m_owner.LogWrite("New connection status: " + status + " (" + reason + ")");
			m_status = status;

			if (m_status == NetConnectionStatus.Connected)
			{
				// TODO: no need for further hole punching
			}

			m_owner.NotifyStatusChange(this, reason);
		}

		internal NetConnection(NetBase owner, NetworkEndPoint remoteEndPoint, byte[] localHailData, byte[] remoteHailData, byte[] remoteRndSignature, int remoteRndSeqNr)
		{
			m_owner = owner;
			m_remoteEndPoint = remoteEndPoint;
			m_localHailData = localHailData;
			m_remoteHailData = remoteHailData;
			m_remoteRndSignature = remoteRndSignature;
			m_remoteRndSeqNr = remoteRndSeqNr;
			m_localRndSeqNr = NetRandom.Instance.Next(0, NetConstants.NumSequenceNumbers - 1);
			m_futureClose = double.MaxValue;

			m_throttleDebt = owner.m_config.m_throttleBytesPerSecond; // slower start

			m_statistics = new NetConnectionStatistics(this);
			m_status = NetConnectionStatus.Connecting; // to prevent immediate removal on heartbeat thread

			ResetReliability();
			InitializeReliability();
			InitializeFragmentation();
			InitializeStringTable();
			//InitializeCongestionControl(32);
		}

		/// <summary>
		/// Queue message for sending; takes ownership of the NetBuffer, don't reuse it after this call
		/// </summary>
		public void SendMessage(NetBuffer data, NetChannel channel)
		{
			SendMessage(data, channel, null);
		}

		// TODO: Use this with TRUE isLibraryThread for internal sendings (acks etc)

		internal void SendMessage(NetBuffer data, NetChannel channel, NetBuffer receiptData)
		{
			if (m_status != NetConnectionStatus.Connected)
				throw new NetException("Status must be Connected to send messages");

			if (data.LengthBytes > m_owner.m_config.m_maximumTransmissionUnit && channel != NetChannel.Unreliable)
			{
				//
				// Fragmented message
				//

				int dataLen = data.LengthBytes;
				int chunkSize = m_owner.m_config.m_maximumTransmissionUnit - 12; // header
				int numFragments = dataLen / chunkSize;
				if (chunkSize * numFragments < dataLen)
					numFragments++;

				ushort fragId = m_nextSendFragmentId++;

				for (int i = 0; i < numFragments; i++)
				{
					OutgoingNetMessage fmsg = m_owner.CreateOutgoingMessage();
					fmsg.m_type = NetMessageLibraryType.UserFragmented;
					fmsg.m_msgType = NetMessageType.Data;

					NetBuffer fragBuf = m_owner.CreateBuffer();
					fragBuf.Write(fragId);
					fragBuf.WriteVariableUInt32((uint)i);
					fragBuf.WriteVariableUInt32((uint)numFragments);

					if (i < numFragments - 1)
					{
						// normal fragment
						fragBuf.Write(data.Data, i * chunkSize, chunkSize);
					}
					else
					{
						// last fragment
						int bytesInLast = dataLen - (chunkSize * (numFragments - 1));
						fragBuf.Write(data.Data, i * chunkSize, bytesInLast);

						// add receipt only to last message
						fmsg.m_receiptData = receiptData;
					}
					fmsg.m_data = fragBuf;
					fmsg.m_data.m_refCount = 1; // since it's just been created

					fmsg.m_numSent = 0;
					fmsg.m_nextResend = double.MaxValue;
					fmsg.m_sequenceChannel = channel;
					fmsg.m_sequenceNumber = -1;

					m_unsentMessages.Enqueue(fmsg);
				}

				// TODO: recycle the original, unfragmented data

				return;
			}

			//
			// Normal, unfragmented, message
			//

			OutgoingNetMessage msg = m_owner.CreateOutgoingMessage();
			msg.m_msgType = NetMessageType.Data;
			msg.m_type = NetMessageLibraryType.User;
			msg.m_data = data;
			msg.m_data.m_refCount++; // it could have been sent earlier also
			msg.m_numSent = 0;
			msg.m_nextResend = double.MaxValue;
			msg.m_sequenceChannel = channel;
			msg.m_sequenceNumber = -1;
			msg.m_receiptData = receiptData;

			m_unsentMessages.Enqueue(msg);
		}

		internal void Connect()
		{
			//Here is a bug fix for uLink. If this is Connect() nr 2 from a peer trying to connect to a server the 
			//wrong ApplicationIdentifier was used before the fix. At least if the first Connect request was made with 
			//NetPeer.Connect(NetworkEndPoint remoteEndpoint, byte[] localHailData, string appId)
			//Bug fix: Use the ApplicationIdentifier stored in this NetConnection (if it exists).
			if (m_applicationIdentifier != "")
				Connect(m_applicationIdentifier);
			else
				Connect(m_owner.Configuration.ApplicationIdentifier);
		}

		internal void Connect(string appId)
		{
			m_isInitiator = true;
			m_futureClose = double.MaxValue;
			m_futureDisconnectReason = null;
			m_applicationIdentifier = appId; //Store appId. It will be used in reconnect attempts.

			m_owner.LogVerbose("Sending Connect request to " + m_remoteEndPoint, this);
			var msgBuffer = m_owner.GetTempBuffer();
			msgBuffer.Write(appId);
			msgBuffer.Write(m_owner.m_localRndSignature);
			msgBuffer.Write(m_localRndSeqNr);

			double localTimeSent = NetTime.Now + m_owner.m_localTimeOffset;
			msgBuffer.Write(localTimeSent);

			if (m_localHailData != null && m_localHailData.Length > 0)
				msgBuffer.Write(m_localHailData);

			m_handshakeInitiated = localTimeSent;
			SendSingleUnreliableSystemMessage(NetSystemType.Connect, msgBuffer);
			SetStatus(NetConnectionStatus.Connecting, "Connecting");
		}

		internal void Heartbeat(double now)
		{
			if (m_status == NetConnectionStatus.Disconnected)
				return;

			//CongestionHeartbeat(now);

			if (m_status == NetConnectionStatus.Connecting)
			{
				NetProfiler.BeginSample("_CheckHandshake");
				CheckHandshake(now);
				NetProfiler.EndSample();
			}
			else if (m_status == NetConnectionStatus.Connected)
			{
				// send ping?
				NetProfiler.BeginSample("_CheckPing");
				CheckPing(now);
				NetProfiler.EndSample();
			}

			if (m_requestDisconnect)
			{
				NetProfiler.BeginSample("_InitiateDisconnect");
				InitiateDisconnect();
				NetProfiler.EndSample();
			}

			if (now > m_futureClose)
			{
				NetProfiler.BeginSample("_FinalizeDisconnect");
				FinalizeDisconnect();
				NetProfiler.EndSample();
			}

			// Resend all packets that has reached a mature age
			NetProfiler.BeginSample("_ResendMessages");
			ResendMessages(now);
			NetProfiler.EndSample();

			// send all unsent messages
			NetProfiler.BeginSample("_SendOutgoingMessages");
			SendUnsentMessages(now);
			NetProfiler.EndSample();
		}

		private void CheckHandshake(double now)
		{
			if (now - m_handshakeInitiated > m_owner.Configuration.HandshakeAttemptRepeatDelay)
			{
				if (m_handshakeAttempts >= m_owner.Configuration.HandshakeAttemptsMaxCount)
				{
					Disconnect("No answer from remote host", 0, false, true);
					return;
				}
				m_handshakeAttempts++;
				if (m_isInitiator)
				{
					m_owner.LogWrite("Re-sending Connect", this);
					Connect();
				}
				else
				{
					m_owner.LogWrite("Re-sending ConnectResponse", this);

					var responseBuffer = m_owner.GetTempBuffer();
					responseBuffer.Write(m_owner.m_localRndSignature);
					responseBuffer.Write(m_localRndSeqNr);
					responseBuffer.Write(m_remoteRndSignature);
					responseBuffer.Write(m_remoteRndSeqNr);

					double localTimeSent = m_connectLocalSentTime;
					responseBuffer.Write(localTimeSent);
					double remoteTimeRecv = m_connectRemoteRecvTime;
					responseBuffer.Write(remoteTimeRecv);
					double remoteTimeSent = NetTime.Now + m_owner.m_localTimeOffset;
					responseBuffer.Write(remoteTimeSent);

					if (m_localHailData != null)
						responseBuffer.Write(m_localHailData);

					m_handshakeInitiated = remoteTimeSent;
					SendSingleUnreliableSystemMessage(NetSystemType.ConnectResponse, responseBuffer);
				}
			}
		}

		internal void SendUnsentMessages(double now)
		{
			// Add any acknowledges to unsent messages
			if (m_acknowledgesToSend.Count > 0)
			{
				if (m_unsentMessages.Count < 1)
				{
					// Wait before sending acknowledges?
					if (m_ackMaxDelayTime > 0.0f)
					{
						if (m_ackWithholdingStarted == 0.0)
						{
							m_ackWithholdingStarted = now;
						}
						else
						{
							if (now - m_ackWithholdingStarted < m_ackMaxDelayTime)
								return; // don't send (only) acks just yet
							// send acks "explicitly" ie. without any other message being sent
							m_ackWithholdingStarted = 0.0;
						}
					}
				}

				// create ack messages and add to m_unsentMessages
				CreateAckMessages();
			}

			if (m_unsentMessages.Count < 1)
				return;

			// throttling
			float throttle = m_owner.m_config.ThrottleBytesPerSecond;
			float maxSendBytes = float.MaxValue;
			if (throttle > 0)
			{
				double frameLength = now - m_lastSentUnsentMessages;

				//int wasDebt = (int)m_throttleDebt;
				if (m_throttleDebt > 0)
					m_throttleDebt -= (float)(frameLength * (double)m_owner.m_config.ThrottleBytesPerSecond);
				//int nowDebt = (int)m_throttleDebt;
				//if (nowDebt != wasDebt)
				//	LogWrite("THROTTLE worked off -" + (nowDebt - wasDebt) + " bytes = " + m_throttleDebt);

				m_lastSentUnsentMessages = now;

				maxSendBytes = throttle - m_throttleDebt;
				if (maxSendBytes < 0)
					return; // throttling; no bytes allowed to be sent
			}

			int mtu = m_owner.Configuration.MaximumTransmissionUnit;
			bool useCoalescing = m_owner.Configuration.UseMessageCoalescing;

			int messagesInPacket = 0;

			var sendBuffer = m_owner.m_sendBuffer;

			sendBuffer.Reset();
			while (m_unsentMessages.Count > 0)
			{
				OutgoingNetMessage msg = m_unsentMessages.Peek();
				int estimatedMessageSize = msg.m_data.LengthBytes + 5;

				// check if this message fits the throttle window
				if (estimatedMessageSize > maxSendBytes) // TODO: Allow at last one message if no debt
					break;

				// need to send packet and start a new one?
				if (messagesInPacket > 0)
				{
					if (!useCoalescing || (sendBuffer.LengthBytes + estimatedMessageSize > mtu))
					{
						m_owner.SendPacket(this);
						int sendLen = sendBuffer.LengthBytes;
						m_statistics.CountPacketSent(sendLen);
						//LogWrite("THROTTLE Send packet +" + sendLen + " bytes = " + m_throttleDebt + " (maxSendBytes " + maxSendBytes + " estimated " + estimatedMessageSize + ")");
						m_throttleDebt += sendLen;

						sendBuffer.Reset();
					}
				}

				if (msg.m_sequenceNumber == -1)
					AssignSequenceNumber(msg);

				// pop and encode message
				m_unsentMessages.Dequeue();
				int pre = sendBuffer.m_bitLength;
				msg.m_data.m_readPosition = 0;
				msg.Encode(sendBuffer);

				int encLen = (sendBuffer.m_bitLength - pre) / 8;
				m_statistics.CountMessageSent(msg, encLen);
				maxSendBytes -= encLen;

				if (msg.m_sequenceChannel >= NetChannel.ReliableUnordered)
				{
					// reliable; store message (incl. buffer)
					msg.m_numSent++;
					StoreMessage(now, msg);
				}
				else
				{
					// not reliable, don't store - recycle...
					NetBuffer b = msg.m_data;
					b.m_refCount--;

					msg.m_data = null;

					// ... unless someone else is using the buffer
					if (b.m_refCount <= 0)
						m_owner.RecycleBuffer(b);

					//m_owner.m_messagePool.Push(msg);
				}
				messagesInPacket++;
			}

			// send current packet
			if (messagesInPacket > 0)
			{
				m_owner.SendPacket(this);
				int sendLen = sendBuffer.LengthBytes;
				m_statistics.CountPacketSent(sendLen);
				//LogWrite("THROTTLE Send packet +" + sendLen + " bytes = " + m_throttleDebt);
				m_throttleDebt += sendLen;
			}
		}

		/*
		internal void HandleUserMessage(NetMessage msg)
		{
			int seqNr = msg.m_sequenceNumber;
			int chanNr = (int)msg.m_sequenceChannel;
			bool isDuplicate = false;

			int relation = RelateToExpected(seqNr, chanNr, out isDuplicate);

			//
			// Unreliable
			//
			if (msg.m_sequenceChannel == NetChannel.Unreliable)
			{
				// It's all good; add message
				if (isDuplicate)
				{
					m_statistics.CountDuplicateMessage(msg);
					m_owner.LogVerbose("Rejecting duplicate " + msg, this);
				}
				else
				{
					AcceptMessage(msg);
				}
				return;
			}

			//
			// Reliable unordered
			//
			if (msg.m_sequenceChannel == NetChannel.ReliableUnordered)
			{
				// send acknowledge (even if duplicate)
				m_acknowledgesToSend.Enqueue((chanNr << 16) | msg.m_sequenceNumber);

				if (isDuplicate)
				{
					m_statistics.CountDuplicateMessage(msg);
					m_owner.LogVerbose("Rejecting duplicate " + msg, this);
					return; // reject duplicates
				}

				// It's good; add message
				AcceptMessage(msg);

				return;
			}

			ushort nextSeq = (ushort)(seqNr + 1);

			if (chanNr < (int)NetChannel.ReliableInOrder1)
			{
				//
				// Sequenced
				//
				if (relation < 0)
				{
					// late sequenced message
					m_statistics.CountDroppedSequencedMessage();
					m_owner.LogVerbose("Dropping late sequenced " + msg, this);
					return;
				}

				// It's good; add message
				AcceptMessage(msg);

				m_nextExpectedSequence[chanNr] = nextSeq;
				return;
			}
			else
			{
				//
				// Ordered
				// 

				// send ack (regardless)
				m_acknowledgesToSend.Enqueue((chanNr << 16) | msg.m_sequenceNumber);

				if (relation < 0)
				{
					// late ordered message
#if DEBUG
					if (!isDuplicate)
						m_owner.LogWrite("Ouch, weird! Late ordered message that's NOT a duplicate?! seqNr: " + seqNr + " expecting: " + m_nextExpectedSequence[chanNr], this);
#endif
					// must be duplicate
					m_owner.LogVerbose("Dropping duplicate message " + seqNr, this);
					m_statistics.CountDuplicateMessage(msg);
					return; // rejected; don't advance next expected
				}

				if (relation > 0)
				{
					// early message; withhold ordered
					m_owner.LogVerbose("Withholding " + msg + " (expecting " + m_nextExpectedSequence[chanNr] + ")", this);
					m_withheldMessages.Add(msg);
					return; // return without advancing next expected
				}

				// It's right on time!
				AcceptMessage(msg);

				// ordered; release other withheld messages?
				bool released = false;
				do
				{
					released = false;
					foreach (NetMessage wm in m_withheldMessages)
					{
						if ((int)wm.m_sequenceChannel == chanNr && wm.m_sequenceNumber == nextSeq)
						{
							m_owner.LogVerbose("Releasing withheld message " + wm, this);
							m_withheldMessages.Remove(wm);
							AcceptMessage(wm);
							// no need to set rounds for this message; it was one when first related() and withheld
							nextSeq++;
							if (nextSeq >= NetConstants.NumSequenceNumbers)
								nextSeq -= NetConstants.NumSequenceNumbers;
							released = true;
							break;
						}
					}
				} while (released);
			}

			// Common to Sequenced and Ordered

			//m_owner.LogVerbose("Setting next expected for " + (NetChannel)chanNr + " to " + nextSeq);
			m_nextExpectedSequence[chanNr] = nextSeq;

			return;
		}
		*/

		internal void HandleSystemMessage(IncomingNetMessage msg, double timestamp)
		{
			msg.m_data.PositionBits = 0;
			NetSystemType sysType = (NetSystemType)msg.m_data.ReadByte();
			switch (sysType)
			{
				case NetSystemType.Disconnect:
					if (m_status == NetConnectionStatus.Disconnected)
						return;
					Disconnect(msg.m_data.ReadString(), NetConstants.DisconnectLingerTime, false, false);
					break;
				case NetSystemType.ConnectionRejected:
					string reason = msg.m_data.ReadString();
					m_owner.NotifyApplication(NetMessageType.ConnectionRejected, reason, msg.m_sender, msg.m_senderEndPoint);
					Disconnect(reason, 0.0f, false, true);
					break;
				case NetSystemType.Connect:
					{
						// ConnectReponse must have been losts

						/*if (m_isInitiator) // this might be useful in the future
						{
							m_owner.LogWrite("Received connect but we're the initiator...", this);
							return; // ignore
						}*/

						if (Status != NetConnectionStatus.Connecting && Status != NetConnectionStatus.Connected)
						{
							m_owner.LogWrite("Received connect but we're not connecting or connected...", this);
							return; // ignore if disconnecting
						}

						string appIdent = msg.m_data.ReadString();
						if (appIdent != m_owner.m_config.ApplicationIdentifier)
						{
							if ((m_owner.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								m_owner.NotifyApplication(NetMessageType.BadMessageReceived,
														  "Connect for different application identification received: " + appIdent, null,
														  msg.m_senderEndPoint);
							return;
						}

						// read random identifier
						var remoteRndSignature = msg.m_data.ReadBytes(NetConstants.SignatureByteSize);
						if (!m_owner.Configuration.AllowConnectionToSelf && NetUtility.CompareElements(remoteRndSignature, m_owner.m_localRndSignature))
						{
							// don't allow self-connect
							if ((m_owner.m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								m_owner.NotifyApplication(NetMessageType.ConnectionRejected, "Connection to self not allowed", null,
														  msg.m_senderEndPoint);
							return;
						}

						int remoteRndSeqNr = msg.m_data.ReadInt32();

						if (m_remoteRndSignature != null)
						{
							if (!NetUtility.CompareElements(remoteRndSignature, m_remoteRndSignature) || remoteRndSeqNr != m_remoteRndSeqNr)
							{
								// this is not the same connection.
								Disconnect("Not the same connection", 0, false, true);
								return;
							}
						}
						else // this might happen if both try and connect at the same time.
						{
							m_remoteRndSignature = remoteRndSignature;
							m_remoteRndSeqNr = remoteRndSeqNr;
							ResetReliability();
						}

						m_connectLocalSentTime = msg.m_data.ReadDouble();
						m_connectRemoteRecvTime = timestamp + m_owner.m_localTimeOffset;

						// read hail data
						m_remoteHailData = null;
						int hailBytesCount = (msg.m_data.LengthBits - msg.m_data.PositionBits) / 8;
						if (hailBytesCount > 0)
							m_remoteHailData = msg.m_data.ReadBytes(hailBytesCount);

						// send response; even if connected
						var responseBuffer = m_owner.GetTempBuffer();
						responseBuffer.Write(m_owner.m_localRndSignature);
						responseBuffer.Write(m_localRndSeqNr);
						responseBuffer.Write(m_remoteRndSignature);
						responseBuffer.Write(m_remoteRndSeqNr);

						double localTimeSent = m_connectLocalSentTime;
						responseBuffer.Write(localTimeSent);
						double remoteTimeRecv = m_connectRemoteRecvTime;
						responseBuffer.Write(remoteTimeRecv);
						double remoteTimeSent = NetTime.Now + m_owner.m_localTimeOffset;
						responseBuffer.Write(remoteTimeSent);

						if (m_localHailData != null)
							responseBuffer.Write(m_localHailData);

						m_handshakeInitiated = remoteTimeSent;
						SendSingleUnreliableSystemMessage(NetSystemType.ConnectResponse, responseBuffer);
						break;
					}
				case NetSystemType.ConnectResponse:
					{
						/*if (m_isInitiator) // this might be useful in the future
						{
							m_owner.LogWrite("Received connect but we're the initiator...", this);
							return; // ignore
						}*/

						if (m_status != NetConnectionStatus.Connecting && m_status != NetConnectionStatus.Connected)
						{
							m_owner.LogWrite("Received connection response but we're not connecting or connected...", this);
							return;
						}

						var remoteRndSignature = msg.m_data.ReadBytes(NetConstants.SignatureByteSize);
						if (!m_owner.Configuration.AllowConnectionToSelf && NetUtility.CompareElements(remoteRndSignature, m_owner.m_localRndSignature))
						{
							// don't allow self-connect
							if ((m_owner.m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								m_owner.NotifyApplication(NetMessageType.ConnectionRejected, "Connection to self not allowed", null,
														  msg.m_senderEndPoint);
							return;
						}

						int remoteRndSeqNr = msg.m_data.ReadInt32();

						if (m_remoteRndSignature != null)
						{
							if (!NetUtility.CompareElements(remoteRndSignature, m_remoteRndSignature) || remoteRndSeqNr != m_remoteRndSeqNr)
							{
								// this is not the same connection.
								Disconnect("Not the same connection", 0, false, true);
								return;
							}
						}
						else // this might happen if both try and connect at the same time.
						{
							m_remoteRndSignature = remoteRndSignature;
							m_remoteRndSeqNr = remoteRndSeqNr;
							ResetReliability();
						}

						var myRndSignature = msg.m_data.ReadBytes(NetConstants.SignatureByteSize);
						var myRndSeqNr = msg.m_data.ReadInt32();

						if (!NetUtility.CompareElements(myRndSignature, m_owner.m_localRndSignature) || myRndSeqNr != m_localRndSeqNr)
						{
							// this is not the same connection.
							Disconnect("Not the same connection", 0, false, true);
							return;
						}
						
						double localTimeSent = msg.m_data.ReadDouble();
						double remoteTimeRecv = msg.m_data.ReadDouble();
						double remoteTimeSent = msg.m_data.ReadDouble();
						double localTimeRecv = timestamp + m_owner.m_localTimeOffset;

						// read hail data
						m_remoteHailData = null;
						int numHailBytes = (msg.m_data.LengthBits - msg.m_data.PositionBits) / 8;
						if (numHailBytes > 0)
							m_remoteHailData = msg.m_data.ReadBytes(numHailBytes);

						// Send connection established
						var responseBuffer = m_owner.GetTempBuffer();
						responseBuffer.Write(m_owner.m_localRndSignature);
						responseBuffer.Write(m_localRndSeqNr);
						responseBuffer.Write(m_remoteRndSignature);
						responseBuffer.Write(m_remoteRndSeqNr);
						
						double intiatorLocalTimeSent = remoteTimeSent;
						responseBuffer.Write(intiatorLocalTimeSent);
						double intiatorRemoteTimeRecv = localTimeRecv;
						responseBuffer.Write(intiatorRemoteTimeRecv);
						double intiatorRemoteTimeSent = NetTime.Now + m_owner.m_localTimeOffset;
						responseBuffer.Write(intiatorRemoteTimeSent);

						if (m_localHailData != null)
							responseBuffer.Write(m_localHailData);

						SendSingleUnreliableSystemMessage(NetSystemType.ConnectionEstablished, responseBuffer);

						// send first ping 250ms after connected
						m_lastSentPing = timestamp - m_owner.Configuration.PingFrequency + 0.1 + (NetRandom.Instance.NextFloat() * 0.25f);
						m_statistics.Reset();
						SetInitialPongEntry(new PongEntry(localTimeSent, remoteTimeRecv, remoteTimeSent, localTimeRecv));
						SetStatus(NetConnectionStatus.Connected, "Connected");
						break;
					}
				case NetSystemType.ConnectionEstablished:
					{
						if (m_status != NetConnectionStatus.Connecting)
						{
							if ((m_owner.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								m_owner.NotifyApplication(NetMessageType.BadMessageReceived,
								                          "Received connection response but we're not connecting...", this, msg.m_senderEndPoint);
							return;
						}

						var remoteRndSignature = msg.m_data.ReadBytes(NetConstants.SignatureByteSize);
						if (!m_owner.Configuration.AllowConnectionToSelf && NetUtility.CompareElements(remoteRndSignature, m_owner.m_localRndSignature))
						{
							// don't allow self-connect
							if ((m_owner.m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								m_owner.NotifyApplication(NetMessageType.ConnectionRejected, "Connection to self not allowed", null,
														  msg.m_senderEndPoint);
							return;
						}

						int remoteRndSeqNr = msg.m_data.ReadInt32();

						if (m_remoteRndSignature != null)
						{
							if (!NetUtility.CompareElements(remoteRndSignature, m_remoteRndSignature) || remoteRndSeqNr != m_remoteRndSeqNr)
							{
								// this is not the same connection.
								Disconnect("Not the same connection", 0, false, true);
								return;
							}
						}
						else // this might happen if both try and connect at the same time.
						{
							m_remoteRndSignature = remoteRndSignature;
							m_remoteRndSeqNr = remoteRndSeqNr;
							ResetReliability();
						}

						var myRndSignature = msg.m_data.ReadBytes(NetConstants.SignatureByteSize);
						var myRndSeqNr = msg.m_data.ReadInt32();

						if (!NetUtility.CompareElements(myRndSignature, m_owner.m_localRndSignature) || myRndSeqNr != m_localRndSeqNr)
						{
							// this is not the same connection.
							Disconnect("Not the same connection", 0, false, true);
							return;
						}

						double localTimeSent = msg.m_data.ReadDouble();
						double remoteTimeRecv = msg.m_data.ReadDouble();
						double remoteTimeSent = msg.m_data.ReadDouble();
						double localTimeRecv = timestamp + m_owner.m_localTimeOffset;

						// read hail data
						if (m_remoteHailData == null)
						{
							int hbc = (msg.m_data.LengthBits - msg.m_data.PositionBits)/8;
							if (hbc > 0)
								m_remoteHailData = msg.m_data.ReadBytes(hbc);
						}

						// send first ping 100-350ms after connected
						m_lastSentPing = timestamp - m_owner.Configuration.PingFrequency + 0.1 + (NetRandom.Instance.NextFloat()*0.25f);
						m_statistics.Reset();
						SetInitialPongEntry(new PongEntry(localTimeSent, remoteTimeRecv, remoteTimeSent, localTimeRecv));
						SetStatus(NetConnectionStatus.Connected, "Connected");
						break;
					}
				case NetSystemType.Ping:
					// also accepted as ConnectionEstablished
					if (m_isInitiator == false && m_status == NetConnectionStatus.Connecting)
					{
						m_owner.LogWrite("Received ping; interpreted as ConnectionEstablished", this);
						m_statistics.Reset();
						SetInitialPongEntryApprox(timestamp - m_handshakeInitiated);
						SetStatus(NetConnectionStatus.Connected, "Connected");
					}
					
					if (m_status != NetConnectionStatus.Connected)
					{
						m_owner.LogWrite("Received ping but we're not connected...", this);
						if (m_status == NetConnectionStatus.Disconnected)
						{
							// this might cause a issue (especially if both are trying to connect at the same time).
							Disconnect("We're not connected", 0, true, true);
						}
						return;
					}

					ReceivedPing(msg, timestamp + m_owner.m_localTimeOffset);
					break;
				case NetSystemType.Pong:
					ReceivedPong(msg, timestamp + m_owner.m_localTimeOffset);
					break;
				case NetSystemType.StringTableAck:
					ushort val = msg.m_data.ReadUInt16();
					StringTableAcknowledgeReceived(val);
					break;
				default:
					m_owner.LogWrite("Undefined behavior in NetConnection for system message " + sysType, this);
					break;
			}
		}

		/// <summary>
		/// Disconnects from remote host; lingering for 'lingerSeconds' to allow packets in transit to arrive
		/// </summary>
		public void Disconnect(string reason, float lingerSeconds)
		{
			Disconnect(reason, lingerSeconds, true, false);
		}

		internal void Disconnect(string reason, float lingerSeconds, bool sendGoodbye, bool forceRightNow)
		{
			if (m_status == NetConnectionStatus.Disconnected)
				return;

			SendUnsentMessages(NetTime.Now);

			m_futureDisconnectReason = reason;
			m_requestLinger = lingerSeconds;
			m_requestSendGoodbye = sendGoodbye;
			m_requestDisconnect = true;

			if (forceRightNow)
				InitiateDisconnect();
		}

		private void InitiateDisconnect()
		{
			if (m_requestSendGoodbye)
			{
				//just do this without regard for other packet and overwrite it
				var scratch = m_owner.GetTempBuffer();
				scratch.Write(string.IsNullOrEmpty(m_futureDisconnectReason) ? "" : m_futureDisconnectReason);
				m_owner.SendSingleUnreliableSystemMessage(
					NetSystemType.Disconnect,
					scratch,
					m_remoteEndPoint,
					null
				);
			}

			if (m_requestLinger <= 0)
			{
				FinalizeDisconnect();
				m_futureClose = double.MaxValue;
			}
			else
			{
				SetStatus(NetConnectionStatus.Disconnecting, m_futureDisconnectReason);
				m_futureClose = NetTime.Now + m_requestLinger;
			}

			m_requestDisconnect = false;
		}

		private void FinalizeDisconnect()
		{
			SetStatus(NetConnectionStatus.Disconnected, m_futureDisconnectReason);
			ResetReliability();

			try
			{
				m_owner.m_socket.Disconnect(m_remoteEndPoint);
			}
			catch (Exception ex)
			{
				Log.Warning(LogFlags.Socket, "Failed to call NetworkSocket.Disconnect for connection ", m_remoteEndPoint, ": ", ex);
			}
		}

		internal void Reset()
		{
			m_remoteRndSignature = null;
			m_remoteRndSeqNr = 0;
			m_localRndSeqNr = NetRandom.Instance.Next(0, NetConstants.NumSequenceNumbers - 1);
			m_futureClose = double.MaxValue;

			m_throttleDebt = m_owner.m_config.m_throttleBytesPerSecond; // slower start

			m_statistics = new NetConnectionStatistics(this);
			m_unsentMessages.Clear();
			m_status = NetConnectionStatus.Connecting; // to prevent immediate removal on heartbeat thread

			ResetReliability();
			InitializeReliability();
			InitializeFragmentation();
			InitializeStringTable();
		}

		internal void SendSingleUnreliableSystemMessage(NetSystemType tp, NetBuffer data)
		{
			m_owner.SendSingleUnreliableSystemMessage(tp, data, m_remoteEndPoint, this);
		}

		/*
		public override bool Equals(object other)
		{
			return Equals(other as NetConnection);
		}

		public bool Equals(NetConnection other)
		{
			return other != null && other.RemoteEndpoint.Equals(RemoteEndpoint);
		}

		public override int GetHashCode()
		{
			return RemoteEndpoint.GetHashCode();
		}
		*/

		public override string ToString()
		{
			return "Connection " + m_remoteEndPoint;
		}
	}
}
