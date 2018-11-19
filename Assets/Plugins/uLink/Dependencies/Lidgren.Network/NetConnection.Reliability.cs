using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using uLink;

namespace Lidgren.Network
{
	internal sealed partial class NetConnection
	{
		public delegate double ResendFunction(int numSent, double avgRTT);

		internal double[] m_earliestResend = new double[NetConstants.NumReliableChannels];
		internal Queue<int> m_acknowledgesToSend = new Queue<int>(4);
		//internal ushort[] m_nextExpectedSequence;
		internal List<OutgoingNetMessage>[] m_storedMessages = new List<OutgoingNetMessage>[NetConstants.NumReliableChannels];
		//internal List<NetMessage> m_withheldMessages;
		//internal uint[][] m_receivedSequences;
		private int[] m_nextSequenceToSend = new int[NetConstants.NumSequenceChannels];
		//private uint[] m_currentSequenceRound;


		// next expected UnreliableOrdered
		internal int[] m_nextExpectedSequenced = new int[NetConstants.NumReliableChannels];
		public BitArray[] m_reliableReceived = new BitArray[NetConstants.NumReliableChannels];
		internal List<IncomingNetMessage>[] m_withheldMessages = new List<IncomingNetMessage>[NetConstants.NumReliableChannels];
		private int[] m_allReliableReceivedUpTo = new int[NetConstants.NumReliableChannels];

		internal void InitializeReliability()
		{
			//m_withheldMessages = new List<NetMessage>(2);
			//m_nextExpectedSequence = new ushort[NetConstants.NumSequenceChannels];

			for (int i = 0; i < m_nextSequenceToSend.Length; i++)
				m_nextSequenceToSend[i] = m_localRndSeqNr;

			//m_currentSequenceRound = new uint[NetConstants.NumSequenceChannels];
			//for (int i = 0; i < m_currentSequenceRound.Length; i++)
			//	m_currentSequenceRound[i] = NetConstants.NumKeptSequenceNumbers;

			//m_receivedSequences = new uint[NetConstants.NumSequenceChannels][];
			//for (int i = 0; i < m_receivedSequences.Length; i++)
			//	m_receivedSequences[i] = new uint[NetConstants.NumKeptSequenceNumbers];

			for (int i = 0; i < m_earliestResend.Length; i++)
				m_earliestResend[i] = double.MaxValue;

			m_acknowledgesToSend.Clear();
		}

		internal void ResetReliability()
		{
			for (int i = 0; i < m_storedMessages.Length; i++)
			{
				if (m_storedMessages[i] != null)
					m_storedMessages[i].Clear();
			}

			for (int i = 0; i < m_allReliableReceivedUpTo.Length; i++)
				m_allReliableReceivedUpTo[i] = m_remoteRndSeqNr;

			for (int i = 0; i < m_withheldMessages.Length; i++)
			{
				if (m_withheldMessages[i] != null)
					m_withheldMessages[i].Clear();
			}

			for (int i = 0; i < m_nextExpectedSequenced.Length; i++)
				m_nextExpectedSequenced[i] = m_remoteRndSeqNr;
		
			for (int i = 0; i < NetConstants.NumReliableChannels; i++)
			{
				if (m_reliableReceived[i] != null)
				{
					for (int o = 0; o < m_reliableReceived[i].Length; o++)
						m_reliableReceived[i][o] = false;
				}
			}

			m_acknowledgesToSend.Clear();
		}

		/// <summary>
		/// Returns positive numbers for early, 0 for as expected, negative numbers for late message
		/// </summary>
		private int Relate(int receivedSequenceNumber, int expected)
		{
			int diff = expected - receivedSequenceNumber;
			if (diff < -NetConstants.EarlyArrivalWindowSize)
				diff += NetConstants.NumSequenceNumbers;
			else if (diff > NetConstants.EarlyArrivalWindowSize)
				diff -= NetConstants.NumSequenceNumbers;
			return -diff;
		}

		/// <summary>
		/// Process a user message
		/// </summary>
		internal void HandleUserMessage(IncomingNetMessage msg, double now)
		{
			// also accepted as ConnectionEstablished
			if (m_isInitiator == false && m_status == NetConnectionStatus.Connecting)
			{
				m_owner.LogWrite("Received user message; interpreted as ConnectionEstablished", this);
				m_statistics.Reset();
				SetInitialPongEntryApprox(now - m_handshakeInitiated);
				SetStatus(NetConnectionStatus.Connected, "Connected");
			}

			if (m_status != NetConnectionStatus.Connected && m_status != NetConnectionStatus.Disconnecting)
			{
				m_owner.LogWrite("Received user message but we're not connected...", this);
				//Disconnect("We're not connected", 0, true, true); // we can't send disconnect because then we fail the redirect test for a reason that needs to be investigated.
				return;
			}

			//
			// Unreliable
			//
			if (msg.m_sequenceChannel == NetChannel.Unreliable)
			{
				AcceptMessage(msg);
				return;
			}

			//
			// Unreliable Sequenced
			//
			if (msg.m_sequenceChannel >= NetChannel.UnreliableInOrder1 && msg.m_sequenceChannel <= NetChannel.UnreliableInOrder15)
			{
				// relate to expected
				int seqChanNr = (int)msg.m_sequenceChannel - (int)NetChannel.UnreliableInOrder1;
				int sdiff = Relate(msg.m_sequenceNumber, m_nextExpectedSequenced[seqChanNr]);

				if (sdiff < 0)
				{
					// Reject late sequenced message
					m_owner.LogVerbose("Rejecting late sequenced " + msg);
					m_statistics.CountDroppedSequencedMessage();
					return;
				}
				AcceptMessage(msg);
				int nextExpected = msg.m_sequenceNumber + 1;
				if (nextExpected > NetConstants.NumSequenceNumbers)
					nextExpected = 0;
				m_nextExpectedSequenced[seqChanNr] = nextExpected;
				return;
			}

			//
			// Reliable and ReliableOrdered
			//

			// Send ack, regardless of anything
			m_acknowledgesToSend.Enqueue(((int)msg.m_sequenceChannel << 16) | msg.m_sequenceNumber);

			// relate to all received up to
			int relChanNr = (int)msg.m_sequenceChannel - (int)NetChannel.ReliableUnordered;
			int arut = m_allReliableReceivedUpTo[relChanNr];
			int diff = Relate(msg.m_sequenceNumber, arut);

			if (diff < 0)
			{
				// Reject duplicate
				m_statistics.CountDuplicateMessage(msg);
				m_owner.LogVerbose("Rejecting(1) duplicate reliable " + msg, this);
				return;
			}

			bool isOrdered = (msg.m_sequenceChannel >= NetChannel.ReliableInOrder1);

			if (arut == msg.m_sequenceNumber)
			{
				// Right on time
				AcceptMessage(msg);
				PostAcceptReliableMessage(msg, arut);
				return;
			}

			// get bools list we must check
			BitArray recList = m_reliableReceived[relChanNr];
			if (recList == null)
			{
				recList = new BitArray(NetConstants.NumSequenceNumbers);
				m_reliableReceived[relChanNr] = recList;
			}

			if (recList[msg.m_sequenceNumber])
			{
				// Reject duplicate
				m_statistics.CountDuplicateMessage(msg);
				m_owner.LogVerbose("Rejecting(2) duplicate reliable " + msg, this);
				return;
			}

			// It's an early reliable message
			if (m_reliableReceived[relChanNr] == null)
				m_reliableReceived[relChanNr] = new BitArray(NetConstants.NumSequenceNumbers);
			m_reliableReceived[relChanNr][msg.m_sequenceNumber] = true;

			if (!isOrdered)
			{
				AcceptMessage(msg);
				return;
			}

			// Early ordered message; withhold
			List<IncomingNetMessage> wmlist = m_withheldMessages[relChanNr];
			if (wmlist == null)
			{
				wmlist = new List<IncomingNetMessage>();
				m_withheldMessages[relChanNr] = wmlist;
			}

			m_owner.LogVerbose("Withholding " + msg + " (waiting for " + arut + ")", this);
			wmlist.Add(msg);
			return;
		}

		/// <summary>
		/// Run this when current ARUT arrives
		/// </summary>
		private void PostAcceptReliableMessage(NetMessage msg, int arut)
		{
			int seqChan = (int)msg.m_sequenceChannel;
			int relChanNr = seqChan - (int)NetChannel.ReliableUnordered;

			// step forward until next AllReliableReceivedUpTo (arut)
			bool nextArutAlreadyReceived = false;
			do
			{
				if (m_reliableReceived[relChanNr] == null)
					m_reliableReceived[relChanNr] = new BitArray(NetConstants.NumSequenceNumbers);
				m_reliableReceived[relChanNr][arut] = false;
				arut++;
				if (arut >= NetConstants.NumSequenceNumbers)
					arut = 0;
				nextArutAlreadyReceived = m_reliableReceived[relChanNr][arut];
				if (nextArutAlreadyReceived)
				{
					// ordered?
					if (seqChan >= (int)NetChannel.ReliableInOrder1)
					{
						// this should be a withheld message
						int wmlidx = (int)seqChan - (int)NetChannel.ReliableUnordered;
						bool foundWithheld = false;
						foreach (IncomingNetMessage wm in m_withheldMessages[wmlidx])
						{
							if ((int)wm.m_sequenceChannel == seqChan && wm.m_sequenceNumber == arut)
							{
								// Found withheld message due for delivery
								m_owner.LogVerbose("Releasing withheld message " + wm, this);
								AcceptMessage(wm);
								foundWithheld = true;
								m_withheldMessages[wmlidx].Remove(wm);
								break;
							}
						}
						if (!foundWithheld)
							throw new NetException("Failed to find withheld message!");
					}
				}
			} while (nextArutAlreadyReceived);

			m_allReliableReceivedUpTo[relChanNr] = arut;
		}

		private void AssignSequenceNumber(OutgoingNetMessage msg)
		{
			int idx = (int)msg.m_sequenceChannel;
			int nr = m_nextSequenceToSend[idx];
			msg.m_sequenceNumber = nr;
			nr++;
			if (nr >= NetConstants.NumSequenceNumbers)
				nr = 0;
			m_nextSequenceToSend[idx] = nr;
		}

		internal void StoreMessage(double now, OutgoingNetMessage msg)
		{
			int chanBufIdx = (int)msg.m_sequenceChannel - (int)NetChannel.ReliableUnordered;

			List<OutgoingNetMessage> list = m_storedMessages[chanBufIdx];
			if (list == null)
			{
				list = new List<OutgoingNetMessage>();
				m_storedMessages[chanBufIdx] = list;
			}
			list.Add(msg);

			// schedule resend
			double nextResend = now + m_owner.Configuration.ResendFunction(msg.m_numSent, m_currentAvgRoundtrip);
			msg.m_nextResend = nextResend;

			// if (msg.m_numSent >= 10) UnityEngine.Debug.Log(now + ": Try no " + msg.m_numSent + ": " + NetUtility.BytesToHex(msg.m_data.ToArray()));

			m_owner.LogVerbose("Stored " + msg + " @ " + NetTime.ToMillis(now) + " next resend in " + NetTime.ToMillis(msg.m_nextResend - now) + " ms", this);

			// earliest?
			if (nextResend < m_earliestResend[chanBufIdx])
				m_earliestResend[chanBufIdx] = nextResend;
		}

		internal void ResendMessages(double now)
		{
			for (int i = 0; i < m_storedMessages.Length; i++)
			{
				List<OutgoingNetMessage> list = m_storedMessages[i];
				if (list == null || list.Count < 1)
					continue;

				if (now > m_earliestResend[i])
				{
					//TODO: try to find a way to avoid this, this is a quick fix to avoid quadratic time complexity
					var newlist = new List<OutgoingNetMessage>(list.Count);

					double newEarliest = double.MaxValue;
					foreach (OutgoingNetMessage msg in list)
					{
						double resend = msg.m_nextResend;
						if (now > resend)
						{
							// Re-enqueue message in unsent list
							m_owner.LogVerbose("Resending " + msg +
								" now: " + NetTime.ToMillis(now) +
								" nextResend: " + NetTime.ToMillis(msg.m_nextResend), this);
							m_statistics.CountMessageResent(msg.m_type);
							m_unsentMessages.Enqueue(msg);
						}
						else
						{
							newlist.Add(msg);
						}
						if (resend < newEarliest)
							newEarliest = resend;
					}

					m_storedMessages[i] = newlist;
					m_earliestResend[i] = newEarliest;
				}
			}
		}

		/// <summary>
		/// Create ack message(s) for sending
		/// </summary>
		private void CreateAckMessages()
		{
			int mtuBits = ((m_owner.m_config.m_maximumTransmissionUnit - 12) / 3) * 8;

			OutgoingNetMessage ackMsg = null;
			int numAcks = m_acknowledgesToSend.Count;
			for (int i = 0; i < numAcks; i++)
			{
				if (ackMsg == null)
				{
					ackMsg = m_owner.CreateOutgoingMessage();
					ackMsg.m_sequenceChannel = NetChannel.Unreliable;
					ackMsg.m_type = NetMessageLibraryType.Acknowledge;
				}

				int ack = m_acknowledgesToSend.Dequeue();

				ackMsg.m_data.Write((byte)((ack >> 16) & 255));
				ackMsg.m_data.Write((byte)(ack & 255));
				ackMsg.m_data.Write((byte)((ack >> 8) & 255));

				//NetChannel ac = (NetChannel)(ack >> 16);
				//int asn = ack & ushort.MaxValue;
				//LogVerbose("Sending ack " + ac + "|" + asn);

				if (ackMsg.m_data.LengthBits >= mtuBits && m_acknowledgesToSend.Count > 0)
				{
					// send and begin again
					m_unsentMessages.Enqueue(ackMsg);
					ackMsg = null;
				}
			}

			if (ackMsg != null)
				m_unsentMessages.EnqueueFirst(ackMsg); // push acks to front of queue

			m_statistics.CountAcknowledgesSent(numAcks);
		}

		internal void HandleAckMessage(double now, IncomingNetMessage ackMessage)
		{
			int len = ackMessage.m_data.LengthBytes;
			if ((len % 3) != 0)
			{
				if ((m_owner.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
					m_owner.NotifyApplication(NetMessageType.BadMessageReceived, "Malformed ack message; length must be multiple of 3; it's " + len, this, ackMessage.m_senderEndPoint);
				return;
			}

			for (int i = 0; i < len; i += 3) //for each channel + seq nbr in ACK
			{

				NetChannel chan = (NetChannel)ackMessage.m_data.ReadByte();
				int seqNr = ackMessage.m_data.ReadUInt16();

				// LogWrite("Acknowledgement received: " + chan + "|" + seqNr);
				m_statistics.CountAcknowledgesReceived(1);

				// remove saved message
				int relChanNr = (int)chan - (int)NetChannel.ReliableUnordered;
				if (relChanNr < 0)
				{
					if ((m_owner.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						m_owner.NotifyApplication(NetMessageType.BadMessageReceived, "Malformed ack message; indicated netchannel " + chan, this, ackMessage.m_senderEndPoint);
					continue;
				}

				List<OutgoingNetMessage> list = m_storedMessages[relChanNr];
				if (list != null)
				{
					int cnt = list.Count;
					if (cnt > 0)
					{
						for (int j = 0; j < cnt; j++) //for each stored message on channel
						{
							OutgoingNetMessage msg = list[j];
							if (msg.m_sequenceNumber == seqNr) //find correct message
							{
								// if (msg.m_numSent >= 10) UnityEngine.Debug.Log(now + ": Ack for " + msg.m_numSent + ": " + NetUtility.BytesToHex(msg.m_data.ToArray()));

								//LogWrite("Removed stored message: " + msg);
								list.RemoveAt(j);

								// reduce estimated amount of packets on wire
								//CongestionCountAck(msg.m_packetNumber);

								// fire receipt
								if (msg.m_receiptData != null)
								{
									m_owner.LogVerbose("Got ack, removed from storage: " + msg + " firing receipt; " + msg.m_receiptData, this);
									m_owner.FireReceipt(this, msg.m_receiptData);
								}
								else
								{
									m_owner.LogVerbose("Got ack, removed from storage: " + msg, this);
								}

								// recycle
								msg.m_data.m_refCount--;
								if (msg.m_data.m_refCount <= 0)
									m_owner.RecycleBuffer(msg.m_data); // time to recycle buffer
	
								msg.m_data = null;
								//m_owner.m_messagePool.Push(msg);

#if !NO_NAK
								if (j > 0)
								{
									int k;
									for (k = 0; k < j; k++) //for each message stored prior to the one matching seq nbr
									{
										var m = list[k];
										if (m.m_sequenceNumber > seqNr) break;

										// Re-enqueue message in unsent list
										m_owner.LogVerbose("Implicit NAK Resending " + m +
											" now: " + NetTime.NowInMillis +
											" nextResend: " + NetTime.ToMillis(m.m_nextResend), this);
										m_statistics.CountMessageResent(m.m_type);
										m_unsentMessages.Enqueue(m);
									}

									list.RemoveRange(0, k);
								}
#endif

								break; //exit stored message loop since this was the message corresponding to seq nbr
								//now returning to next sequence number in ACK packet
							}
						}
					}
				}
			}

			// recycle
			NetBuffer rb = ackMessage.m_data;
			rb.m_refCount = 0; // ack messages can't be used by more than one message
			ackMessage.m_data = null;

			m_owner.RecycleBuffer(rb);
			//m_owner.m_messagePool.Push(ackMessage);
		}

		public static double DefaultResendFunction(int numSent, double avgRTT)
		{
			//double rtt = (avgRTT < 0.5) ? avgRTT : 0.5; // staff...
			double resend = 0.025f + (float)avgRTT * 1.1f * (1 + numSent * numSent);
			//if (resend > 1f) resend = 1f;
			return resend;
		}

		/*
		public void DumpReliability(NetChannel channel)
		{
			int chanNr = (int)channel;
			int relChanNr = (int)channel - (int)NetChannel.ReliableUnordered;

			if (m_storedMessages != null && m_storedMessages[relChanNr] != null)
			{
				var contents = new StringBuilder();

				foreach (var msg in m_storedMessages[relChanNr])
				{
					if (msg != null)
					{
						contents.AppendFormat("SeqNr {0}, NextResend {1}, NumSent {2}, Data {3}\n", msg.m_sequenceNumber, msg.m_nextResend, msg.m_numSent, msg.m_data != null ? NetUtility.BytesToHex(msg.m_data.Data) : "Null");
					}
					else
					{
						contents.Append("Null\n");
					}
				}

				System.IO.File.WriteAllText("m_storedMessages.txt", contents.ToString());
			}
			else
			{
				System.IO.File.WriteAllText("m_storedMessages.txt", "Null");
			}

			if (m_withheldMessages != null && m_withheldMessages[relChanNr] != null)
			{
				var contents = new StringBuilder();

				foreach (var msg in m_withheldMessages[relChanNr])
				{
					if (msg != null)
					{
						contents.AppendFormat("SeqNr {0}, Data {1}\n", msg.m_sequenceNumber, msg.m_data != null ? NetUtility.BytesToHex(msg.m_data.Data) : "Null");
					}
					else
					{
						contents.Append("Null\n");
					}
				}

				System.IO.File.WriteAllText("m_withheldMessages.txt", contents.ToString());
			}
			else
			{
				System.IO.File.WriteAllText("m_withheldMessages.txt", "Null");
			}

			if (m_reliableReceived != null && m_reliableReceived[relChanNr] != null)
			{
				var contents = new StringBuilder();

				for (int i = 0; i < m_reliableReceived[relChanNr].Length; i++)
				{
					var received = m_reliableReceived[relChanNr][i];
					if (received)
					{
						contents.AppendFormat("SeqNr {0}\n", i);
					}
				}

				System.IO.File.WriteAllText("m_reliableReceived.txt", contents.ToString());
			}
			else
			{
				System.IO.File.WriteAllText("m_reliableReceived.txt", "Null");
			}

			System.IO.File.WriteAllText("m_nextSequenceToSend.txt", m_nextSequenceToSend != null ? m_nextSequenceToSend[chanNr].ToString() : "Null");
			System.IO.File.WriteAllText("m_allReliableReceivedUpTo.txt", m_allReliableReceivedUpTo != null ? m_allReliableReceivedUpTo[relChanNr].ToString() : "Null");
		}
		*/

		// debug helpers (leave be, referenced from other project(s)) =======================

		internal int m_debugMaxNextExpectedSeqNum
		{
			get
			{
				int max = int.MinValue;
				for (int i = 0; i < m_allReliableReceivedUpTo.Length; i++)
					max = Math.Max(max, m_allReliableReceivedUpTo[i]);
				return max;
			}
		}

		internal int m_debugMaxNextSeqNumToSend
		{
			get
			{
				int max = int.MinValue;
				for (int i = 0; i < m_nextSequenceToSend.Length; ++i)
					max = Math.Max(max, m_nextSequenceToSend[i]);
				return max;
			}
		}
	}
}
