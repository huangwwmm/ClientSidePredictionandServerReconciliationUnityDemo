using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Net;
using uLink;

namespace Lidgren.Network
{
	internal sealed class IncomingNetMessage : NetMessage
	{
		internal NetConnection m_sender;
		internal NetworkEndPoint m_senderEndPoint;

		/// <summary>
		/// Read this message from the packet buffer
		/// </summary>
		/// <returns>new read pointer position</returns>
		internal bool ReadFrom(NetBuffer buffer, NetworkEndPoint endpoint)
		{
			m_senderEndPoint = endpoint;

			// read header
			byte header;

			// ignore zero padding in the beginning until we get something non-zero
			do
			{
				if (buffer.BytesRemaining == 0) return false;
				header = buffer.ReadByte();
			} while (header == 0);

			m_type = (NetMessageLibraryType)(header & 7);
			m_sequenceChannel = (NetChannel)(header >> 3);

			//TODO: do not read seqno for unreliable, just set it to 0 (DAVID)
			m_sequenceNumber = buffer.ReadUInt16();

			int payLen = (int)buffer.ReadVariableUInt32();
			if (payLen > buffer.BytesRemaining) return false; // bad packet

			// copy payload into message buffer
			m_data.EnsureBufferSizeInBytes(payLen);
			buffer.ReadBytes(m_data.Data, 0, payLen);
			m_data.Reset(0, payLen * 8);

			return true;
		}

		public override string ToString()
		{
			if (m_type == NetMessageLibraryType.System)
				return "[Incoming " + (NetSystemType)m_data.Data[0] + " " + m_sequenceChannel + "|" + m_sequenceNumber + "]";

			return "[Incoming " + m_type + " " + m_sequenceChannel + "|" + m_sequenceNumber + "]";
		}
	}

	internal sealed class OutgoingNetMessage : NetMessage
	{
		internal int m_numSent;
		internal double m_nextResend;
		internal NetBuffer m_receiptData;

		internal void Encode(NetBuffer intoBuffer)
		{
			Debug.Assert(m_sequenceNumber != -1);

			// message type, netchannel and sequence number
			intoBuffer.Write((byte)((int)m_type | ((int)m_sequenceChannel << 3)));
			//TODO: do not write seqno for unreliable !!!! (DAVID)
			intoBuffer.Write((ushort)m_sequenceNumber);

			// payload length
			int len = m_data.LengthBytes;
			intoBuffer.WriteVariableUInt32((uint)len);

			// copy payload
			intoBuffer.Write(m_data.Data, 0, len);

			return;
		}

		internal int _approximateEncodeLength
		{
			get
			{
				var len = m_data.LengthBytes;
				return len + NetBuffer.SizeOfVariableUInt32((uint) len) + 3;
			}
		}

		public override string ToString()
		{
			if (m_type == NetMessageLibraryType.System)
				return "[Outgoing " + (NetSystemType)m_data.Data[0] + " " + m_sequenceChannel + "|" + m_sequenceNumber + "]";

			return "[Outgoing " + m_type + " " + m_sequenceChannel + "|" + m_sequenceNumber + "]";
		}
	}

	abstract class NetMessage
	{
		internal NetMessageType m_msgType;

		internal NetMessageLibraryType m_type;
		internal NetChannel m_sequenceChannel;
		internal int m_sequenceNumber = -1;
	
		internal NetBuffer m_data;
			
		public NetMessage()
		{
			m_msgType = NetMessageType.Data;
		}		
	}
}
