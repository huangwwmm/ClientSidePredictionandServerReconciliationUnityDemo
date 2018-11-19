#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
using Lidgren.Network;

namespace uLink
{
	internal class StateSync
	{
		public NetworkViewID viewID;
		public SerializedBuffer data;

		public StateSync(NetworkViewID viewID, SerializedBuffer data)
		{
			this.viewID = viewID;
			this.data = data;
		}

		public StateSync(NetBuffer buffer)
		{
			viewID = new NetworkViewID(buffer);
			data = new SerializedBuffer(buffer);
		}

		internal static void _Read(NetBuffer buffer, out NetworkViewID viewID, out NetBuffer data)
		{
			viewID = new NetworkViewID(buffer);
			data = SerializedBuffer._Read(buffer);
		}

		public void Write(NetBuffer buffer)
		{
			_Write(buffer, viewID, data.buffer);
		}

		internal static void _Write(NetBuffer buffer, NetworkViewID viewID, NetBuffer data)
		{
			viewID._Write(buffer);
			SerializedBuffer._Write(buffer, data);
		}
	}
}
