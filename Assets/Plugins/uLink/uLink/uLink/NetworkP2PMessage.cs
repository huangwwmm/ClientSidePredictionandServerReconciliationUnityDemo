#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8621 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-19 20:58:36 +0200 (Fri, 19 Aug 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

// TODO: split this class into Incoming and Outgoing to remove buffer reallocation when sending.

namespace uLink
{
	internal class NetworkP2PMessage
	{
		internal enum InternalCode : byte
		{
			None = 0,
			HandoverRequest = 1,
			HandoverResponse = 2,
		}

		[Flags]
		private enum HeaderFlags : byte
		{
			NotInternal = 0 << 0,
			IsHandoverRequest = 1 << 0,
			IsHandoverResponse = 2 << 0,
			InternalCode = 3 << 0,

			IsTypeSafe = 1 << 7,
		}

		public NetworkFlags flags;

		public readonly BitStream stream;
		public readonly NetConnection connection;
		public readonly NetChannel channel;

		public readonly string name;
		public readonly InternalCode internCode;

		public NetworkP2PMessage(NetBuffer buffer, NetConnection connection, NetChannel channel)
		{
			flags = 0;

			// read header from stream:

			var headerFlags = (HeaderFlags)buffer.ReadByte();

			internCode = (InternalCode) (headerFlags & HeaderFlags.InternalCode);
			isTypeSafe = ((headerFlags & HeaderFlags.IsTypeSafe) == HeaderFlags.IsTypeSafe);
			isReliable = ((int)channel & (int)NetChannel.ReliableUnordered) == (int)NetChannel.ReliableUnordered;

			if (!isInternal)
			{
				name = buffer.ReadString();
			}
			else
			{
				name = String.Empty;
			}

			stream = new BitStream(buffer, isTypeSafe);
			this.connection = connection;
			this.channel = channel;
		}

		public NetworkP2PMessage(NetworkFlags flags, string name, InternalCode internCode)
		{
			this.flags = flags;

			// TODO: optimize by calculating initial buffer size by bit-flags & args

			// TODO: remove the default 4 bytes allocated by the default NetBuffer constructor.

			stream = new BitStream(isTypeSafe);
			var buffer = stream._buffer;

			connection = null;
			channel = (isReliable) ? NetChannel.ReliableInOrder1 : NetChannel.Unreliable;

			this.name = name;
			this.internCode = internCode;

			// write header to stream:

			HeaderFlags headerFlags = (HeaderFlags)internCode;
			if (isTypeSafe) headerFlags |= HeaderFlags.IsTypeSafe;

			buffer.Write((byte)headerFlags);

			if (internCode == InternalCode.None) buffer.Write(name);

			buffer.PositionBits = buffer.LengthBits;
		}

		public NetBuffer GetSendBuffer()
		{
			return stream._ShareBuffer();
		}

		public bool isInternal
		{
			get { return internCode != InternalCode.None; }
		}

		public bool isReliable
		{
			get { return (flags & NetworkFlags.Unreliable) == 0; }
			set { if (value) flags &= ~NetworkFlags.Unreliable; else flags |= NetworkFlags.Unreliable; }
		}

		public bool isEncryptable
		{
			get { return (flags & NetworkFlags.Unencrypted) == 0; }
			set { if (value) flags &= ~NetworkFlags.Unencrypted; else flags |= NetworkFlags.Unencrypted; }
		}

		public bool isTypeSafe
		{
			get { return (flags & NetworkFlags.TypeUnsafe) == 0; }
			set { if (value) flags &= ~NetworkFlags.TypeUnsafe; else flags |= NetworkFlags.TypeUnsafe; }
		}

		public override string ToString()
		{
			string str;

			if (isInternal) str = "Internal: " + internCode;
			else str = "Name: " + name;

			str += ", from: " + connection.RemoteEndpoint;
			
			str += ", channel: " + channel;

			return str;
		}

		public void ExecuteInternal(NetworkP2PBase network)
		{
			switch (internCode)
			{
				case InternalCode.HandoverRequest: network._RPCHandoverRequest(stream.ReadNetworkPlayer(), stream.ReadNetworkP2PHandoverInstances(), stream.ReadUInt32(), stream.ReadEndPoint(), stream, this); break;
				case InternalCode.HandoverResponse: network._RPCHandoverResponse(stream.ReadUInt32(), stream.ReadPassword(), stream.ReadUInt16(), this); break;
				default:
					Log.Debug(NetworkLogFlags.RPC, "Unknown internal RPC: ", internCode);
					break;
			}
		}
	}
}
