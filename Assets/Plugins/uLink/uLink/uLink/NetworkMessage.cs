#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12061 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-14 21:25:28 +0200 (Mon, 14 May 2012) $
#endregion

using System;
using Lidgren.Network;

// TODO: split this class into Incoming and Outgoing to remove buffer reallocation when sending?

namespace uLink
{
#if PIKKO_BUILD
	sealed class NetworkBaseLocal
	{
		internal NetworkPlayer _localPlayer = NetworkPlayer.unassigned;

		internal static NetworkPlayer _GetConnectionPlayerID(NetConnection connection)
		{
			return (connection.Tag != null) ? (NetworkPlayer)connection.Tag : NetworkPlayer.unassigned;
		}

		internal readonly static NetworkBaseLocal instance = new NetworkBaseLocal();
	}
#endif

	internal class NetworkMessage
	{
#if !NO_POOLING
		//TODO: Make the pool size configurable for customers.
		internal static BitStreamPool _bitStreamPoolOutgoingTypeSafe = new BitStreamPool(1000, true); 
		internal static BitStreamPool _bitStreamPoolOutgoing = new BitStreamPool(1000, false);
#endif

		internal enum InternalCode : byte
		{
			None = 0,
			//AllocRequest = 1, // TODO: obsolete
			//AllocResponse = 2, // TODO: obsolete
			BufferedRPCs = 3,
			CellConnectResponse = 4,
			CellConnectRequest = 5,
			ClientConnectRequest = 6,
			ClientConnectResponse = 7,
			ConnectDenied = 8,
			Create = 9,
			//Dealloc = 10, // TODO: obsolete
			DestroyByPlayerID = 11,
			DestroyByViewID = 12,
			DisconnectPlayerID = 13,
			HandoverRequest = 14,
			HandoverResponse = 15,
			Redirect = 16,
			SecurityRequest = 17,
			SecurityResponse = 18,
			StateSyncOwner = 19,
			StateSyncProxy = 20,
			MultiTrackPosition = 21,
			PlayerIDConnected = 22,
			PlayerIDDisconnected = 23,
			MultiStateSyncProxy = 24,
			MultiStateSyncOwner = 25,
			RedirectPlayerID = 26,
			UnsecurityRequest = 27,
			UnsecurityResponse = 28,
			DestroyAll = 29,
			DestroyInGroup = 30,
			DestroyInGroupByPlayerID = 31,
			LicenseCheck = 32,
			ChangeGroup = 33,
			//ResyncClockRequest = 34, // TODO: obsolete
			//ResyncClockResponse = 35, // TODO: obsolete
			StateSyncCellProxy = 36,
			MultiStateSyncCellProxy = 37,
			RepairAuthFromProxyRequest = 38,
			MastDebugInfo = 39,
			ChangeAuthFlags = 40,
			RemoveRPCs = 41,
			Test = 42, // TODO: ???
			StateSyncOwnerDeltaCompressed = 43,
			StateSyncProxyDeltaCompressed = 44,
			StateSyncOwnerDeltaCompressedInit= 45,
			StateSyncProxyDeltaCompressedInit = 46,
		}

		internal enum Channel : byte
		{
			RPC = NetChannel.ReliableInOrder1,
			StateSyncProxy = NetChannel.ReliableInOrder2,
			StateSyncOwner = NetChannel.ReliableInOrder3,
			StateSyncCellProxy = NetChannel.ReliableInOrder4,
		}

		[Flags]
		internal enum HeaderFlags : byte
		{
			HasOriginalSenderPlayerID = 1 << 0,

			ToServer = 0 << 1,
			HasTargetPlayerID = 1 << 1,
			Broadcast = 2 << 1,
			HasBroadcastExcludePlayerID = 3 << 1,
			DestinationType = HasBroadcastExcludePlayerID,

			HasInternalCode = 0 << 3,
			HasNameAndViewID = 1 << 3,
			HasInternalCodeAndViewID = 2 << 3,
			HasNameAndViewIDAndIsBuffered = 3 << 3,
			ExecutionType = HasNameAndViewIDAndIsBuffered,

			NoTimestamp = 0 << 5,
			Has16BitTimestamp = 1 << 5,
			Has24BitTimestamp = 2 << 5,
			Has40BitTimestamp = 3 << 5,
			TimestampType = Has40BitTimestamp,

			HasTypeCodes = 1 << 7,

			// TODO: change HeaderFlags to ushort and add:
			// HasGroup = 1 << 8,
			// WasEncryptedToPikkoServer = 1 << 9,
		}

		public NetworkFlags flags;

#if PIKKO_BUILD
		internal NetBuffer buffer { get { return stream._buffer; } } //don't want to make everything incompatible
#endif

		public readonly BitStream stream;
		public readonly NetConnection connection;
		public readonly NetChannel channel;

		public readonly double localTimeSent;
		public readonly double monotonicServerTimeSent;
		public readonly double rawServerTimeSent;

		public readonly string name;
		public InternalCode internCode; // TODO: fix ugly hack!

		// NOTE: fullhacks this should be readonly but isn't because we might need to fix it if bug assigning it incorrectly.
		public NetworkPlayer sender;

		public readonly NetworkPlayer target;
		public readonly NetworkPlayer exclude;
		public readonly NetworkViewID viewID;

		public const byte ENCRYPTED_SIGNATURE = (byte)(HeaderFlags.HasTargetPlayerID | HeaderFlags.HasNameAndViewIDAndIsBuffered);

		// Constructor used when creating and destroying network objects
		public NetworkMessage(NetworkBaseLocal network)
		{
			flags = 0;
			stream = null;
			connection = null;
			channel = NetChannel.ReliableUnordered;
			localTimeSent = NetworkTime.localTime;
			monotonicServerTimeSent = network._GetMonotonicServerTime(localTimeSent);
			rawServerTimeSent = network._GetRawServerTime(localTimeSent);
			name = String.Empty;
			internCode = InternalCode.None;
			sender = network._localPlayer;
			target = NetworkPlayer.unassigned;
			exclude = NetworkPlayer.unassigned;
			viewID = NetworkViewID.unassigned;
		}

		//Incoming message (backward-compatibility)
		public NetworkMessage(NetworkBaseLocal network, NetBuffer buffer, NetConnection connection, NetChannel channel, bool wasEncrypted)
			: this(network, buffer, connection, channel, wasEncrypted, NetworkTime.localTime)
		{ }

		//Incoming message
		public NetworkMessage(NetworkBaseLocal network, NetBuffer buffer, NetConnection connection, NetChannel channel, bool wasEncrypted, double localTimeRecv)
		{
			flags = 0;

			if (!wasEncrypted) flags |= NetworkFlags.Unencrypted;
			flags = (((int)channel & (int)NetChannel.ReliableUnordered) == (int)NetChannel.ReliableUnordered) ?
				flags &= ~NetworkFlags.Unreliable : flags |= NetworkFlags.Unreliable;

			var headerFlags = (HeaderFlags)buffer.ReadByte();

			// TODO: make sure a regular uLink server doesn't accept messages with a original sender. We should create static create Message methods instead of the constructor?

			if ((headerFlags & HeaderFlags.HasOriginalSenderPlayerID) == HeaderFlags.HasOriginalSenderPlayerID)
			{
				sender = new NetworkPlayer(buffer);
			}
			else
			{
				sender = NetworkBaseLocal._GetConnectionPlayerID(connection);
			}

			switch (headerFlags & HeaderFlags.DestinationType)
			{
				case HeaderFlags.ToServer: target = NetworkPlayer.server; exclude = NetworkPlayer.unassigned; break;
				case HeaderFlags.HasTargetPlayerID: target = new NetworkPlayer(buffer); exclude = NetworkPlayer.unassigned; break;
				case HeaderFlags.Broadcast: target = NetworkPlayer.unassigned; exclude = NetworkPlayer.unassigned; break;
				case HeaderFlags.HasBroadcastExcludePlayerID: target = NetworkPlayer.unassigned; exclude = new NetworkPlayer(buffer); break;
			}

			switch (headerFlags & HeaderFlags.ExecutionType)
			{
				case HeaderFlags.HasInternalCode: viewID = NetworkViewID.unassigned; name = String.Empty; internCode = (InternalCode)buffer.ReadByte(); flags |= NetworkFlags.Unbuffered; break;
				case HeaderFlags.HasNameAndViewID: viewID = new NetworkViewID(buffer); name = buffer.ReadString(); internCode = InternalCode.None; flags |= NetworkFlags.Unbuffered; break;
				case HeaderFlags.HasInternalCodeAndViewID: viewID = new NetworkViewID(buffer); name = String.Empty; internCode = (InternalCode)buffer.ReadByte(); if (internCode != InternalCode.Create) flags |= NetworkFlags.Unbuffered; break;
				case HeaderFlags.HasNameAndViewIDAndIsBuffered: viewID = new NetworkViewID(buffer); name = buffer.ReadString(); internCode = InternalCode.None; break;
			}

			if ((headerFlags & HeaderFlags.TimestampType) != HeaderFlags.NoTimestamp)
			{
				int numberOfBits;
				switch (headerFlags & HeaderFlags.TimestampType)
				{
					case HeaderFlags.Has16BitTimestamp: numberOfBits = 16; break;
					case HeaderFlags.Has24BitTimestamp: numberOfBits = 24; break;
					case HeaderFlags.Has40BitTimestamp: numberOfBits = 40; break;
					default: numberOfBits = 0; break; // something is wrong
				}

				ulong encodedTimestamp = buffer.ReadUInt64(numberOfBits);

#if PIKKO_BUILD
				double serverTimeRecv = localTimeRecv;
#else
				double serverTimeRecv = network._EnsureAndUpdateMonotonicTimestamp(localTimeRecv + network.rawServerTimeOffset);
#endif

				double monotonicTransitTime = _GetElapsedTimeFromEncodedTimestamp(serverTimeRecv, encodedTimestamp, numberOfBits);

#if !PIKKO_BUILD
				monotonicServerTimeSent = serverTimeRecv - monotonicTransitTime;

				// TODO: ugly hack:
				double rawServerTimeRecv = localTimeRecv + network.rawServerTimeOffset;
				double rawTransitTime = _GetElapsedTimeFromEncodedTimestamp(rawServerTimeRecv, encodedTimestamp, numberOfBits);

				rawServerTimeSent = rawServerTimeRecv - rawTransitTime;
				localTimeSent = localTimeRecv - rawTransitTime;
#endif
			}

			if ((headerFlags & HeaderFlags.HasTypeCodes) == 0)
			{
				flags |= NetworkFlags.TypeUnsafe;
			}

			stream = new BitStream(buffer, isTypeSafe);
			this.connection = connection;
			this.channel = channel;
		}

		// Outgoing forwarded message (non-auth server uses this)
		public NetworkMessage(NetworkBaseLocal network, NetworkMessage msg) : this(network, msg, msg.sender)
		{
		}

		// Outgoing forwarded message (non-auth server uses this)
		public NetworkMessage(NetworkBaseLocal network, NetworkMessage msg, NetworkPlayer originalSender)
		{
			flags = msg.flags;
			connection = msg.connection;
			channel = msg.channel;
			localTimeSent = msg.localTimeSent;
			monotonicServerTimeSent = msg.monotonicServerTimeSent;
			rawServerTimeSent = msg.rawServerTimeSent;
			name = msg.name;
			internCode = msg.internCode;
			sender = originalSender;
			target = msg.target;
			exclude = msg.exclude;
			viewID = msg.viewID;

			stream = new BitStream(isTypeSafe);
			var buffer = stream._buffer;

			HeaderFlags headerFlags = 0;

			if (sender != network._localPlayer) headerFlags |= HeaderFlags.HasOriginalSenderPlayerID;

			if (isBroadcast) headerFlags |= (hasExcludeID) ? HeaderFlags.HasBroadcastExcludePlayerID : HeaderFlags.Broadcast;
			else if (hasTargetID) headerFlags |= HeaderFlags.HasTargetPlayerID;

			if (hasViewID && isInternal) headerFlags |= HeaderFlags.HasInternalCodeAndViewID;
			else if (hasViewID) headerFlags |= (isBuffered) ? HeaderFlags.HasNameAndViewIDAndIsBuffered : HeaderFlags.HasNameAndViewID;
			else headerFlags |= HeaderFlags.HasInternalCode;

			// TODO: use 24bit timestamp if needed
			if (hasTimestamp) headerFlags |= HeaderFlags.Has16BitTimestamp;

			if (isTypeSafe) headerFlags |= HeaderFlags.HasTypeCodes;

			buffer.Write((byte)headerFlags);

			if (sender != network._localPlayer) sender._Write(buffer);
			if (hasTargetID) target._Write(buffer);
			if (hasExcludeID) exclude._Write(buffer);
			if (hasViewID) viewID._Write(buffer);

			if (internCode == InternalCode.None) buffer.Write(name);
			else buffer.Write((byte)internCode);

			if (hasTimestamp)
			{
#if PIKKO_BUILD
				ulong timestampInMillis = (ulong)NetTime.ToMillis(localTimestamp);
#else
				ulong timestampInMillis = (ulong)NetTime.ToMillis(rawServerTimeSent);
#endif

				// TODO: use 24bit timestamp if needed
				buffer.Write(timestampInMillis, 16);
			}

			buffer.PositionBits = buffer.LengthBits;

			int argpos = msg.stream._buffer.PositionBytes;
			stream._buffer.Write(msg.stream._buffer.Data, argpos, msg.stream._buffer.LengthBytes - argpos);
		}

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, NetworkFlags flags, Channel channel, string name, InternalCode internCode, NetworkPlayer target, NetworkPlayer exclusion, NetworkViewID viewID)
		{
			if (target != NetworkPlayer.unassigned) flags |= NetworkFlags.Unbuffered;
			this.flags = flags;

			this.channel = ((flags & NetworkFlags.Unreliable) == 0) ? (NetChannel)channel : NetChannel.Unreliable;

			if (isBuffered) if(!(isReliable)){Utility.Exception( "Message can not be buffered and unreliable");}

			// TODO: optimize by calculating initial buffer size by bit-flags & args

			// TODO: optimize by stripping unnecessary data if we have a direct connection i.e. is server

			// TODO: remove the default 4 bytes allocated by the default NetBuffer constructor.

#if !NO_POOLING
			if (isReliable)
			{
				stream = new BitStream(true, isTypeSafe); //The Bitstream for reliable RPCs (including buffered RPCs) should not be written to pooled Bitreams, that would be dangerous during resend.
			}
			else
			{
				stream =  (isTypeSafe) ? _bitStreamPoolOutgoingTypeSafe.GetNext() : _bitStreamPoolOutgoing.GetNext();
			}
#else
			stream = new BitStream(true, isTypeSafe);
#endif
			var buffer = stream._buffer;

			connection = null;

			this.name = name;
			this.internCode = internCode;
			sender = network._localPlayer;

			this.target = target;
			exclude = exclusion;
			this.viewID = viewID;

			// write header to stream:

			HeaderFlags headerFlags = 0;

			// TODO: optimize away exclude and target PlayerID when sent from a regular uLink Server by faking it into a simple broadcast message

			if (isBroadcast) headerFlags |= (hasExcludeID)? HeaderFlags.HasBroadcastExcludePlayerID : HeaderFlags.Broadcast;
			else if (hasTargetID) headerFlags |= HeaderFlags.HasTargetPlayerID;

			if (hasViewID && isInternal) headerFlags |= HeaderFlags.HasInternalCodeAndViewID;
			else if (hasViewID) headerFlags |= (isBuffered) ? HeaderFlags.HasNameAndViewIDAndIsBuffered : HeaderFlags.HasNameAndViewID;
			else headerFlags |= HeaderFlags.HasInternalCode;

			if (hasTimestamp) headerFlags |= (isBuffered) ? HeaderFlags.Has40BitTimestamp : HeaderFlags.Has16BitTimestamp;

			if (isTypeSafe) headerFlags |= HeaderFlags.HasTypeCodes;

			buffer.Write((byte)headerFlags);

			if (hasTargetID) this.target._Write(buffer);
			if (hasExcludeID) exclude._Write(buffer);
			if (hasViewID) this.viewID._Write(buffer);

			if (isInternal) buffer.Write((byte)internCode);
			else buffer.Write(name);

			if (hasTimestamp)
			{
				localTimeSent = NetworkTime.localTime;
				rawServerTimeSent = localTimeSent + network.rawServerTimeOffset;

#if PIKKO_BUILD
				ulong timestampInMillis = (ulong)NetTime.ToMillis(localTimestamp);
#else
				rawServerTimeSent = localTimeSent + network.rawServerTimeOffset;
				ulong timestampInMillis = (ulong)NetTime.ToMillis(rawServerTimeSent);
#endif
				buffer.Write(timestampInMillis, (isBuffered) ? 40 : 16);
			}

			buffer.PositionBits = buffer.LengthBits;
		}

#if !PIKKO_BUILD

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, NetworkFlags flags, Channel channel, string name, RPCMode mode, NetworkPlayer exclusion, NetworkViewID viewID)
			: this(
			network,
			((mode & RPCMode.Buffered) != 0) ? flags : (flags | NetworkFlags.Unbuffered),
			channel,
			name,
			InternalCode.None,
			(mode == RPCMode.Server) ? NetworkPlayer.server : ((mode == RPCMode.Owner) ? network._FindNetworkViewOwner(viewID) : NetworkPlayer.unassigned),
			exclusion,
			viewID)
		{
		}

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, NetworkFlags flags, Channel channel, string name, RPCMode mode, NetworkViewID viewID)
			: this(
			network,
			flags,
			channel,
			name,
			mode,
			(mode == RPCMode.AllExceptOwner || mode == RPCMode.OthersExceptOwner) ? network._FindNetworkViewOwner(viewID)
				: ((mode == RPCMode.Buffered) ? NetworkPlayer.server : NetworkPlayer.unassigned), // TODO: fix fulhacks!
			viewID)
		{
		}

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, NetworkFlags flags, Channel channel, InternalCode internCode, RPCMode mode, NetworkPlayer exclusion)
			: this(
			network,
			((mode & RPCMode.Buffered) != 0) ? flags : (flags | NetworkFlags.Unbuffered),
			channel,
			String.Empty,
			internCode,
			(mode == RPCMode.Server) ? NetworkPlayer.server : NetworkPlayer.unassigned,
			exclusion,
			NetworkViewID.unassigned)
		{
		}

#endif

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, NetworkFlags flags, Channel channel, InternalCode internCode, NetworkPlayer target)
			: this(
			network,
			flags,
			channel,
			String.Empty,
			internCode,
			target,
			NetworkPlayer.unassigned,
			NetworkViewID.unassigned)
		{
		}

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, InternalCode internCode, NetworkPlayer target)
			: this(
			network,
			NetworkFlags.TypeUnsafe | NetworkFlags.NoTimestamp | NetworkFlags.Unbuffered,
			Channel.RPC,
			String.Empty,
			internCode,
			target,
			NetworkPlayer.unassigned,
			NetworkViewID.unassigned)
		{
		}

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, InternalCode internCode, NetworkViewID viewID)
			: this(
			network,
			NetworkFlags.TypeUnsafe | NetworkFlags.NoTimestamp | NetworkFlags.Unbuffered,
			Channel.RPC,
			String.Empty,
			internCode,
			NetworkPlayer.unassigned,
			NetworkPlayer.unassigned,
			viewID)
		{
		}

		// Outgoing message
		public NetworkMessage(NetworkBaseLocal network, InternalCode internCode)
			: this(
			network,
			NetworkFlags.TypeUnsafe | NetworkFlags.NoTimestamp | NetworkFlags.Unbuffered,
			Channel.RPC,
			String.Empty,
			internCode,
			NetworkPlayer.unassigned,
			NetworkPlayer.unassigned,
			NetworkViewID.unassigned)
		{
		}

		public NetBuffer GetSendBuffer()
		{
			return stream._ShareBuffer();
		}

		public bool isBuffered
		{
			get { return (flags & NetworkFlags.Unbuffered) == 0; }
			//set { if (value) flags &= ~NetworkFlags.Unbuffered; else flags |= NetworkFlags.Unbuffered; }
		}

		public bool isReliable
		{
			get { return channel >= NetChannel.ReliableUnordered; }
			//get { return (flags & NetworkFlags.Unreliable) == 0; }
			//set { if (value) flags &= ~NetworkFlags.Unreliable; else flags |= NetworkFlags.Unreliable; }
		}

		public bool isEncryptable
		{
			get { return (flags & NetworkFlags.Unencrypted) == 0; }
			set { flags = value ? (flags & ~NetworkFlags.Unencrypted) : (flags | NetworkFlags.Unencrypted); }
		}

		public bool isCullable
		{
			get { return (flags & NetworkFlags.NoCulling) == 0; }
			set { flags = value ? (flags & ~NetworkFlags.NoCulling) : (flags | NetworkFlags.NoCulling); }
		}

		public bool hasTimestamp
		{
			get { return (flags & NetworkFlags.NoTimestamp) == 0; }
			//set { if (value) flags &= ~NetworkFlags.NoTimestamp; else flags |= NetworkFlags.NoTimestamp; }
		}

		public bool isTypeSafe
		{
			get { return (flags & NetworkFlags.TypeUnsafe) == 0; }
			//set { if (value) flags &= ~NetworkFlags.TypeUnsafe; else flags |= NetworkFlags.TypeUnsafe; }
		}

		public bool isBroadcast
		{
			get { return target == NetworkPlayer.unassigned; }
		}

		public bool hasTargetID
		{
			get { return target != NetworkPlayer.unassigned && target != NetworkPlayer.server; }
		}

		public bool hasExcludeID
		{
			get { return exclude != NetworkPlayer.unassigned; }
		}

		public bool hasViewID
		{
			get { return viewID != NetworkViewID.unassigned; }
		}

		public bool hasName
		{
			get { return !String.IsNullOrEmpty(name); }
		}

		public bool isCustom
		{
			get { return internCode == InternalCode.None; }
		}

		public bool isInternal
		{
			get { return internCode != InternalCode.None; }
		}

		public bool isToServerOrAll
		{
			get { return target == NetworkPlayer.unassigned || target == NetworkPlayer.server; }
		}

		public bool isOnlyToServer
		{
			get { return target == NetworkPlayer.server; }
		}

		public bool isFromServer
		{
			get { return sender == NetworkPlayer.server; }
		}

		public override string ToString()
		{
			string str;

			if (isInternal) str = "Internal RPC '" + internCode;
			else str = "RPC '" + name;
			
			str += "' (from " + sender + " " + connection;

			if (hasTargetID) str += " to " + target;
			if (isBroadcast) str += ", broadcast";
			if (hasExcludeID) str += ", exclude " + exclude;
			if (hasViewID) str += ", " + viewID;

			if (isBuffered) str += ", buffered";

			if (hasTimestamp)
			{
				str += ", local timestamp " + localTimeSent + "s (server timestamp " + monotonicServerTimeSent + "s, raw server timestamp " + rawServerTimeSent + "s )";
			}

			if (isTypeSafe) str += ", typesafe";
			
			str += ", channel " + channel + ")";

			return str;
		}

#if !PIKKO_BUILD
		public void ExecuteInternal(NetworkBase network)
		{
			// TODO: optimize arg reading!

			switch (internCode)
			{
				case InternalCode.BufferedRPCs: network._RPCBufferedRPCs(stream.ReadSerializedBuffers(), this); break;
				case InternalCode.CellConnectResponse: network._RPCCellConnectResponse(stream.ReadNetworkPlayer(), stream.ReadBoolean(), this); break;
				case InternalCode.ClientConnectRequest: network._RPCClientConnectRequest(stream, this); break;
				case InternalCode.ClientConnectResponse: network._RPCClientConnectResponse(stream.ReadNetworkPlayer(), stream, this); break;
				case InternalCode.ConnectDenied: network._RPCConnectDenied(stream.ReadInt32(), this); break;
				case InternalCode.Create: network._RPCCreate(stream.ReadNetworkPlayer(), stream.ReadNetworkGroup(), (NetworkAuthFlags)stream.ReadByte(), stream.ReadVector3(), stream.ReadQuaternion(), stream.ReadString(), stream.ReadString(), stream.ReadString(), stream.ReadString(), stream.ReadString(), stream, this); break;
				case InternalCode.DestroyByPlayerID: network._RPCDestroyByPlayerID(stream.ReadNetworkPlayer(), this); break;
				case InternalCode.DestroyByViewID: network._RPCDestroyByViewID(this); break;
				case InternalCode.HandoverRequest: network._RPCHandoverRequest(stream.ReadNetworkViewID(), this); break;
				case InternalCode.HandoverResponse: network._RPCHandoverResponse(stream.ReadNetworkViewID(), stream.ReadNetworkPlayer(), stream.ReadNetworkGroup(), (NetworkAuthFlags)stream.ReadByte(), stream.ReadVector3(), stream.ReadQuaternion(), stream.ReadString(), stream.ReadString(), stream.ReadString(), stream.ReadString(), stream.ReadString(), stream.ReadBytes(), stream, this); break;
				case InternalCode.Redirect: network._RPCRedirect(stream.ReadEndPoint(), stream.ReadPassword(), this); break;
				case InternalCode.SecurityRequest: network._RPCSecurityRequest(stream.ReadPublicKey(), this); break;
				case InternalCode.SecurityResponse: network._RPCSecurityResponse(stream.ReadSymmetricKey(), this); break;
				case InternalCode.UnsecurityRequest: network._RPCUnsecurityRequest(stream.ReadSymmetricKey(), this); break;
				case InternalCode.UnsecurityResponse: network._RPCUnsecurityResponse(this); break;
				case InternalCode.StateSyncOwner: network._RPCStateSyncOwner(stream, this); break;
				case InternalCode.StateSyncProxy: network._RPCStateSyncProxy(stream, this); break;
				case InternalCode.PlayerIDConnected: network._RPCPlayerIDConnected(stream.ReadNetworkPlayer(), stream.ReadEndPoint(), stream, this); break;
				case InternalCode.PlayerIDDisconnected: network._RPCPlayerIDDisconnected(stream.ReadNetworkPlayer(), stream.ReadInt32(), this); break;
				case InternalCode.MultiStateSyncProxy: network._RPCMultiStateSyncProxy(this); break;
				case InternalCode.MultiStateSyncOwner: network._RPCMultiStateSyncOwner(stream.ReadStateSyncs(), this); break;
				case InternalCode.DestroyAll: network._RPCDestroyAll(!stream.isEOF && stream.ReadBoolean(), this); break;
				case InternalCode.DestroyInGroup: network._RPCDestroyInGroup(stream.ReadNetworkGroup(), this); break;
				case InternalCode.DestroyInGroupByPlayerID: network._RPCDestroyInGroupByPlayerID(stream.ReadNetworkPlayer(), stream.ReadNetworkGroup(), this); break;
				case InternalCode.LicenseCheck: network._RPCLicenseCheck(this); break;
				case InternalCode.ChangeGroup: network._RPCChangeGroup(stream.ReadNetworkViewID(), stream.ReadNetworkGroup(), this); break;
				case InternalCode.StateSyncCellProxy: network._RPCStateSyncCellProxy(stream, this); break;
				case InternalCode.MultiStateSyncCellProxy: network._RPCMultiStateSyncCellProxy(this); break;
				case InternalCode.RepairAuthFromProxyRequest: network._RPCRepairAuthFromProxyRequest(stream.ReadNetworkViewID(), this); break;
				case InternalCode.MastDebugInfo: network._RPCMastDebugInfo(stream.ReadVector3(), this); break;
				case InternalCode.ChangeAuthFlags: network._RPCChangeAuthFlags(stream.ReadNetworkViewID(), (NetworkAuthFlags)stream.ReadByte(), stream.ReadVector3(), this); break;
				case InternalCode.StateSyncOwnerDeltaCompressed: network._RPCStateSyncOwnerDeltaCompressed(stream, this); break;
				case InternalCode.StateSyncProxyDeltaCompressed: network._RPCStateSyncProxyDeltaCompressed(stream, this); break;
				case InternalCode.StateSyncOwnerDeltaCompressedInit: network._RPCStateSyncOwnerDeltaCompressedInit(stream, this); break;
				case InternalCode.StateSyncProxyDeltaCompressedInit: network._RPCStateSyncProxyDeltaCompressedInit(stream, this); break;
				default:
					Log.Debug(NetworkLogFlags.RPC, "Unknown internal RPC: ", internCode, " from ", connection, " channel ", channel);
					break;
			}
		}
#endif

		private static double _GetElapsedTimeFromEncodedTimestamp(double remoteTimeNow, ulong encodedTimestamp, int numberOfBits)
		{
			const long DESYNC_MARGIN_MILLIS = 1000;

			ulong nowInMillis = (ulong)(remoteTimeNow * 1000 + 0.5);
			ulong maxEncodedMillis = 0xFFFFFFFFFFFFFFFF >> (64 - numberOfBits);
			ulong encodedNow = nowInMillis & maxEncodedMillis;

			long elapsedInMillis = unchecked((long)(encodedNow - encodedTimestamp));
			if (elapsedInMillis < 0)
			{
				if (elapsedInMillis > -DESYNC_MARGIN_MILLIS) return 0;

				elapsedInMillis += unchecked((long)maxEncodedMillis);

				// make sure elapsed time is never greater than now
				if (unchecked((ulong)elapsedInMillis) >= nowInMillis) return 0;
			}

			return elapsedInMillis * 0.001;
		}

#if !NO_POOLING
		// The BitSteam pools for outgoing uLink messages
		// should be reseted at the end of every Unity frame
		// so that the BitStreams in the pool can be reused during the next Unity frame.
		internal static void ResetBitStreamPools()
		{
			_bitStreamPoolOutgoingTypeSafe.ReportFrameFinished();
			_bitStreamPoolOutgoing.ReportFrameFinished();
		}
#endif
	}
}
