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
using System.Diagnostics;
using System.Net;
using uLink;

namespace Lidgren.Network
{
	internal sealed partial class NetConnection
	{
		private static readonly FrequencyMuter _negativeRoundTripMuter = new FrequencyMuter(60 * 5, 10, 60);

		const int pongHistoryCount = 5; // should preferably be a odd number

		struct PongEntry : IComparable<PongEntry>, IEquatable<PongEntry>
		{
			public readonly double roundtripTime;
			public readonly double timeOffset;

			public PongEntry(double roundtripTime, double timeOffset)
			{
				this.roundtripTime = roundtripTime;
				this.timeOffset = timeOffset;
			}

			public PongEntry(double localTimeSent, double remoteTimeRecv, double remoteTimeSent, double localTimeRecv)
			{
				roundtripTime = (localTimeRecv - localTimeSent) - (remoteTimeSent - remoteTimeRecv);
				timeOffset = (remoteTimeSent - localTimeRecv) + (roundtripTime / 2);

				if (roundtripTime < 0)
				{
					if (roundtripTime < -0.0011 && _negativeRoundTripMuter.NextEvent())
						Log.Warning(LogFlags.Socket, "Latest calculated roundtripTime (", roundtripTime, ") must not be negative, adjusting it to zero");

					roundtripTime = 0;
				}
			}

			public int CompareTo(PongEntry other)
			{
				return roundtripTime.CompareTo(other.roundtripTime);
			}

			public bool Equals(PongEntry other)
			{
				return roundtripTime.Equals(other.roundtripTime) && timeOffset.Equals(other.timeOffset);
			}

			public override bool Equals(object other)
			{
				return (other is PongEntry) && Equals((PongEntry)other);
			}

			public override int GetHashCode()
			{
				return 0;
			}

			public override string ToString()
			{
				return "roundtrip " + NetTime.ToMillis(roundtripTime) + " ms, offset " + timeOffset + " s";
			}
		}

		private double m_lastSentPing;
		internal double m_lastPongReceived;
		private readonly PongEntry[] m_pongHistory = new PongEntry[pongHistoryCount];
		private readonly PongEntry[] m_pongHistorySorted = new PongEntry[pongHistoryCount];

		private double m_latestRoundtrip = 0.5f; // large to avoid initial resends
		private double m_currentAvgRoundtrip = 0.5f; // large to avoid initial resends

		// Remote time = local time + m_currentTimeOffset
		private double m_latestTimeOffset = 0;
		private double m_currentAvgTimeOffset = 0;

		private float m_ackMaxDelayTime = 0.0f;


		/// <summary>
		/// Gets the current average roundtrip time
		/// </summary>
		public float AverageRoundtripTime { get { return (float)m_currentAvgRoundtrip; } }

		public float LastRoundtripTime { get { return (float)m_latestRoundtrip; } }

		public double AverageRemoteTimeOffset { get { return m_currentAvgTimeOffset; } }

		public double LastRemoteTimeOffset { get { return m_latestTimeOffset; } }

		private void SetInitialPongEntryApprox(double approxRTT)
		{
			if (approxRTT < 0) approxRTT = 0;
			else if (approxRTT > 2) approxRTT = 2;

			SetInitialPongEntry(new PongEntry(approxRTT, 0));
		}

		private void SetInitialPongEntry(PongEntry entry)
		{
			for (int i = 0; i < pongHistoryCount; i++)
			{
				m_pongHistory[i] = entry;
			}

			for (int i = 0; i < pongHistoryCount; i++)
			{
				m_pongHistorySorted[i] = entry;
			}

			m_owner.LogWrite("Initializing pong history (" + entry + ")", this);

			UpdateValuesBasedOnPongHistory();
		}

		private void CheckPing(double now)
		{
			if (m_status == NetConnectionStatus.Connected &&
				now - m_lastSentPing > m_owner.Configuration.PingFrequency
			)
			{
				// check for timeout
				if (now - m_lastPongReceived > m_owner.Configuration.TimeoutDelay)
				{
					// Time out!
					Disconnect("Connection timed out; no pong for " + (now - m_lastPongReceived) + " seconds", NetConstants.DisconnectLingerTime, true, true);
					return;
				}

				// send ping
				m_owner.LogVerbose(NetTime.Now + ": Sending ping...");
				m_lastSentPing = NetTime.Now;
				SendPing();
			}
		}

		internal void SendPing()
		{
			var buffer = m_owner.GetTempBuffer();

			double localTimeSent = NetTime.Now + m_owner.m_localTimeOffset;
			buffer.Write(localTimeSent);

#if DEBUG
			long dateData = DateTime.Now.ToBinary();
			buffer.Write(dateData);
#endif

			SendSingleUnreliableSystemMessage(NetSystemType.Ping, buffer);
		}

		internal void SendPong(double localTimeSent, double remoteTimeRecv)
		{
			var buffer = m_owner.GetTempBuffer();

			buffer.Write(localTimeSent);
			buffer.Write(remoteTimeRecv);

			double remoteTimeSent = NetTime.Now + m_owner.m_localTimeOffset;
			buffer.Write(remoteTimeSent);

#if DEBUG
			long dateData = DateTime.Now.ToBinary();
			buffer.Write(dateData);
#endif

			SendSingleUnreliableSystemMessage(NetSystemType.Pong, buffer);
		}

		private void ReceivedPing(NetMessage ping, double remoteTimeRecv)
		{
			double localTimeSent = ping.m_data.ReadDouble();

			m_owner.LogVerbose(NetTime.Now + ": Received ping; sending pong...");
			//TODO: consider if just overwriting another packet being is OK, should pongs have such high priority?
			SendPong(localTimeSent, remoteTimeRecv);
		}

		private void ReceivedPong(NetMessage pong, double localTimeRecv)
		{
			m_lastPongReceived = localTimeRecv;

			double localTimeSent = pong.m_data.ReadDouble();
			double remoteTimeRecv = pong.m_data.ReadDouble();
			double remoteTimeSent = pong.m_data.ReadDouble();

			if (localTimeSent > localTimeRecv)
			{
				m_owner.LogVerbose(NetTime.Now + ": Received pong; but with a bad timestamp, so we're dropping it.");
				return;
			}

			var entry = new PongEntry(localTimeSent, remoteTimeRecv, remoteTimeSent, localTimeRecv);
			AddPongEntry(entry);

			m_owner.LogVerbose(NetTime.Now + ": Got pong (" + entry + ")");

			if ((m_owner.m_enabledMessageTypes & NetMessageType.PongReceived) == NetMessageType.PongReceived)
			{
				// NOTE: the NetMessageType.PongReceived app notification/message no longer has an NetBuffer
				// and thus no longer a serialized int32 that can be read. Keep that in mind if you get
				// null exception when handling NetMessageType.PongReceived in your main loop.
				m_owner.NotifyApplication(NetMessageType.PongReceived, this);
			}
		}

		private void AddPongEntry(PongEntry newEntry)
		{
			var oldEntry = m_pongHistory[pongHistoryCount - 1];

			for (int i = pongHistoryCount - 1; i > 0; i--)
			{
				m_pongHistory[i] = m_pongHistory[i - 1];
			}

			m_pongHistory[0] = newEntry;

			int oldIndex = Array.BinarySearch(m_pongHistorySorted, oldEntry);
			if (oldIndex >= 0)
			{
				NetUtility.ReplaceSortedItem(m_pongHistorySorted, oldIndex, newEntry);
			}
			else
			{
				string pongHistoryValues = "";
				foreach (var pong in m_pongHistorySorted)
				{
					pongHistoryValues += "\n (" + pong + ")";
				}

				Log.Error(LogFlags.Socket, "Can't find pong entry (", oldEntry, ") in sorted history: ", pongHistoryValues);
			}

			UpdateValuesBasedOnPongHistory();
		}

		void UpdateValuesBasedOnPongHistory()
		{
			m_latestRoundtrip = m_pongHistory[0].roundtripTime;
			m_latestTimeOffset = m_pongHistory[0].timeOffset;

			// NOTE: We use median as "average" to avoid occasional extreme value.
			// We're assuming that pongHistoryCount is odd, to simplify getting the median.
			m_currentAvgRoundtrip = m_pongHistorySorted[pongHistoryCount / 2].roundtripTime;

			// NOTE: The most accurate timeOffset, comes from the
			// ping-pong that spent the least amount of time in transit.
			// Hence why we use the timeOffset with the lowest RTT.
			m_currentAvgTimeOffset = m_pongHistorySorted[0].timeOffset;

			// TODO: someone should probably take a look at this to make sure it's a good algorithm.
			m_ackMaxDelayTime = (float)(
				m_owner.m_config.m_maxAckWithholdTime * m_latestRoundtrip * m_owner.m_config.m_resendTimeMultiplier
			);
		}
	}
}
