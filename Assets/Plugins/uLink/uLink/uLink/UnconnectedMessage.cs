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
	internal class UnconnectedMessage
	{
		internal enum InternalCode : byte
		{
			None = 0,
			DiscoverHostRequest = 1,
			DiscoverHostResponse = 2,
			KnownHostRequest = 3,
			KnownHostResponse = 4,
			PreConnectRequest = 5,
			PreConnectResponse = 6,
			LicenseRequest = 7,
			LicenseResponse = 8,
		}

		public readonly BitStream stream;
		public readonly NetworkEndPoint endpoint;

		public readonly InternalCode internCode;

		public UnconnectedMessage(NetBuffer buffer, NetworkEndPoint endpoint)
		{
			this.endpoint = endpoint;

			stream = new BitStream(buffer, false);

			internCode = (InternalCode) buffer.ReadByte();
		}

		public UnconnectedMessage(InternalCode internCode)
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

		public void Execute(NetworkBase network)
		{
			switch (internCode)
			{
				case InternalCode.DiscoverHostRequest: network._UnconnectedRPCDiscoverHostRequest(stream.ReadHostDataFilter(), stream.ReadDouble(), endpoint); break;
				case InternalCode.DiscoverHostResponse: network._UnconnectedRPCDiscoverHostResponse(stream.ReadLocalHostData(), stream.ReadDouble(), endpoint); break;
				case InternalCode.KnownHostRequest: network._UnconnectedRPCKnownHostRequest(stream.ReadDouble(), stream.ReadBoolean(), endpoint); break;
				case InternalCode.KnownHostResponse: network._UnconnectedRPCKnownHostResponse(stream.ReadLocalHostData(), stream.ReadDouble(), endpoint); break;
				case InternalCode.PreConnectRequest: network._UnconnectedRPCPreConnectRequest(endpoint); break;
				case InternalCode.PreConnectResponse: network._UnconnectedRPCPreConnectResponse(endpoint); break;
				case InternalCode.LicenseRequest: network._UnconnectedRPCLicenseRequest(stream.ReadLocalHostData(), stream.ReadBoolean(), stream.ReadBytes(), endpoint); break;
				default:
					Log.Debug(NetworkLogFlags.RPC, "Unknown internal Unconnected RPC: ", internCode, " from ", endpoint);
					break;
			}
		}
	}
}
