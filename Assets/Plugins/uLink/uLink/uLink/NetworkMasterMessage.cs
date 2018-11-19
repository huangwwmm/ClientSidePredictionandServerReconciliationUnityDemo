#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8625 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-20 00:06:57 +0200 (Sat, 20 Aug 2011) $
#endregion
using Lidgren.Network;

// TODO: split this class into Incoming and Outgoing to remove buffer reallocation when sending.

namespace uLink
{
	internal class NetworkMasterMessage
	{
		internal enum InternalCode : byte
		{
			None = 0,
			HostListRequest = 1,
			HostListResponse = 2,
			RegisterRequest = 3,
			RegisterResponse = 4,
			UpdateHostData = 5,
			ProxyRequest = 6,
			ProxyResponse = 7,
			ProxyClient = 8,
			ProxyFailed = 9,
			RegisterFailed = 10,
			ConnectionTestRequest, // TODO: impl
			ConnectionTestResponse, // TODO: impl
			NATTestRequest, // TODO: impl
			NATTestResponse, // TODO: impl
			NATIntroduction, // TODO: impl
		}

		public readonly BitStream stream;

		public readonly InternalCode internCode;

		public NetworkMasterMessage(NetBuffer buffer)
		{
			stream = new BitStream(buffer, false);

			internCode = (InternalCode) buffer.ReadByte();
		}

		public NetworkMasterMessage(InternalCode internCode)
		{
			this.internCode = internCode;

			// TODO: optimize by calculating initial buffer size by bit-flags & args

			// TODO: remove the default 4 bytes allocated by the default NetBuffer constructor.

			stream = new BitStream(false);
			var buffer = stream._buffer;

			buffer.Write((byte) internCode);

			buffer.PositionBits = buffer.LengthBits;
		}

		public override string ToString()
		{
			return internCode.ToString();
		}

		public void Execute(NetworkBase network)
		{
			switch (internCode)
			{
				case InternalCode.HostListResponse: network._MasterRPCHostListResponse(stream.ReadHostDatas()); break;
				case InternalCode.RegisterResponse: network._MasterRPCRegisterResponse(stream.ReadEndPoint()); break;
				case InternalCode.ProxyResponse: network._MasterRPCProxyResponse(stream.ReadUInt16(), stream.ReadPassword()); break;
				case InternalCode.ProxyClient: network._MasterRPCProxyClient(stream.ReadEndPoint(), stream.ReadPassword(), stream.ReadUInt16(), stream.ReadPassword()); break;
				case InternalCode.ProxyFailed: network._MasterRPCProxyFailed(stream.ReadInt32()); break;
				case InternalCode.RegisterFailed: network._MasterRPCRegisterFailed(stream.ReadInt32()); break;
				default:
					Log.Error(NetworkLogFlags.RPC, "Unknown internal Master RPC: ", internCode);
					break;
			}
		}
	}
}
