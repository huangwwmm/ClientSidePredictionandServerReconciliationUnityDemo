#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8632 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-20 03:18:31 +0200 (Sat, 20 Aug 2011) $
#endregion
using Lidgren.Network;

namespace uLink
{
	internal class SerializedBuffer
	{
		public NetBuffer buffer;

		public SerializedBuffer(BitStream stream)
		{
			buffer = stream._buffer;
		}

		public SerializedBuffer(NetBuffer buffer)
		{
			this.buffer = _Read(buffer);
		}

		internal static NetBuffer _Read(NetBuffer buffer)
		{
			int bits = (int)buffer.ReadVariableUInt32() * 8;
			var newBuffer = new NetBuffer(buffer, bits);
			buffer.PositionBits += bits;
			return newBuffer;
		}

		public void Write(NetBuffer buffer)
		{
			_Write(buffer, this.buffer);
		}

		internal static void _Write(NetBuffer buffer, NetBuffer data)
		{
			int size = data.LengthBytes;
			buffer.WriteVariableUInt32((uint)size);
			buffer.Write(data.Data, 0, size);
		}
	}
}
