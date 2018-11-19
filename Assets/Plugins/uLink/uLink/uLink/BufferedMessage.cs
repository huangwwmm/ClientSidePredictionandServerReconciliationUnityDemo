#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
using System;

namespace uLink
{
	internal class BufferedMessage : IComparable<BufferedMessage>
	{
		public readonly NetworkMessage msg;
		public readonly int bufferedIndex;

		internal BufferedMessage(NetworkMessage msg, int bufferedIndex)
		{
			this.msg = msg;
			this.bufferedIndex = bufferedIndex;
		}

		public int CompareTo(BufferedMessage other)
		{
			return bufferedIndex.CompareTo(other.bufferedIndex);
		}
	}

	internal class BufferedCreate : BufferedMessage
	{
		public readonly NetworkPlayer owner;

		internal BufferedCreate(NetworkMessage msg, int bufferedIndex, NetworkPlayer owner)
			: base(msg, bufferedIndex)
		{
			this.owner = owner;
		}
	}

	internal class BufferedStateSyncDeltaCompressedInit : BufferedMessage
	{
		internal BufferedStateSyncDeltaCompressedInit(NetworkMessage msg)
			: base(msg, Int32.MaxValue)
		{
		}

		protected static NetworkMessage _GenerateMessage(NetworkViewBase nv, NetworkMessage.Channel channel, NetworkMessage.InternalCode msgCode, byte sqNr, BitStream state, NetworkPlayer target)
		{
			var msg = new NetworkMessage(nv._network, nv._syncFlags, channel, String.Empty, msgCode, target, NetworkPlayer.unassigned, nv.viewID);

			msg.stream._buffer.Write(sqNr);
			msg.stream._buffer.Write(state._data, 0, state._buffer.LengthBytes);

			return msg;
		}
	}

	internal class BufferedStateSyncProxyDeltaCompressedInit : BufferedStateSyncDeltaCompressedInit
	{

		internal BufferedStateSyncProxyDeltaCompressedInit(NetworkViewBase nv, NetworkPlayer target)
			: base(_GenerateMessage(nv, target))
		{
		}

		private static NetworkMessage _GenerateMessage(NetworkViewBase nv, NetworkPlayer target)
		{
			var msgChannel = NetworkMessage.Channel.StateSyncProxy;
			var msgCode = NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit;
			var sqNr = (byte)(nv._expectedProxyStateDeltaCompressedSequenceNr - 1);
			var state = nv._prevProxyStateSerialization;
			return _GenerateMessage(nv, msgChannel, msgCode, sqNr, state, target);
		}
	}

	internal class BufferedStateSyncOwnerDeltaCompressedInit : BufferedStateSyncDeltaCompressedInit
	{

		internal BufferedStateSyncOwnerDeltaCompressedInit(NetworkViewBase nv, NetworkPlayer target)
			: base(_GenerateMessage(nv, target))
		{
		}

		private static NetworkMessage _GenerateMessage(NetworkViewBase nv, NetworkPlayer target)
		{
			var msgChannel = NetworkMessage.Channel.StateSyncOwner;
			var msgCode = NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit;
			var sqNr = (byte)(nv._expectedOwnerStateDeltaCompressedSequenceNr - 1);
			var state = nv._prevOwnerStateSerialization;
			return _GenerateMessage(nv, msgChannel, msgCode, sqNr, state, target);
		}
	}
}
