#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8625 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-20 00:06:57 +0200 (Sat, 20 Aug 2011) $
#endregion
using System.Net;
using Lidgren.Network;

// TODO: split this class into Incoming and Outgoing to remove buffer reallocation when sending.

namespace uLink
{
	internal class UnconnectedP2PMessage
	{
		internal enum InternalCode : byte
		{
			None = 0,
			DiscoverPeerRequest = 1,
			DiscoverPeerResponse = 2,
			KnownPeerRequest = 3,
			KnownPeerResponse = 4,
		}

		public readonly BitStream stream;
		public readonly NetworkEndPoint endpoint;

		public readonly InternalCode internCode;

		public UnconnectedP2PMessage(NetBuffer buffer, NetworkEndPoint endpoint)
		{
			this.endpoint = endpoint;

			stream = new BitStream(buffer, false);

			internCode = (InternalCode) buffer.ReadByte();
		}

		public UnconnectedP2PMessage(InternalCode internCode)
		{
			this.internCode = internCode;

			// TODO: optimize by calculating initial buffer size by bit-flags & args

			// TODO: remove the default 4 bytes allocated by the default NetBuffer constructor.

			stream = new BitStream(false);
			var buffer = stream._buffer;

			buffer.Write((byte) internCode);

			// TODO: use built-in codecs!

			buffer.PositionBits = buffer.LengthBits;
		}

		public override string ToString()
		{
			return internCode + " from " + endpoint;
		}

		public void Execute(NetworkP2PBase network)
		{
			switch (internCode)
			{
				case InternalCode.DiscoverPeerRequest: network._UnconnectedRPCDiscoverPeerRequest(stream.ReadPeerDataFilter(), stream.ReadDouble(), endpoint); break;
				case InternalCode.DiscoverPeerResponse: network._UnconnectedRPCDiscoverPeerResponse(stream.ReadLocalPeerData(), stream.ReadDouble(), endpoint); break;
				case InternalCode.KnownPeerRequest: network._UnconnectedRPCKnownPeerRequest(stream.ReadDouble(), stream.ReadBoolean(), endpoint); break;
				case InternalCode.KnownPeerResponse: network._UnconnectedRPCKnownPeerResponse(stream.ReadLocalPeerData(), stream.ReadDouble(), endpoint); break;
				default:
					Log.Error(NetworkLogFlags.RPC, "Unknown internal Unconnected P2P RPC: ", internCode, " from ", endpoint);
					break;
			}
		}
	}
}
