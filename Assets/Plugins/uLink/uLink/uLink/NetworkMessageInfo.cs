#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8909 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-31 17:54:35 +0200 (Wed, 31 Aug 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using Lidgren.Network;

namespace uLink
{
#if UNITY_BUILD
	using NV = NetworkView;
#else
	using NV = NetworkViewBase;
#endif

	/// <summary>
	/// A class containing some extra information about the sender of this network message.
	/// To get this info, you usually add an extra parameter at the end of the parameter list of the RPC.
	/// uLink automatically fills the parameter with an appropriate argument of this type.
	/// </summary>
	public class NetworkMessageInfo
	{
		/// <summary>
		/// The <see cref="uLink.NetworkPlayer"/> who sent the message.
		/// </summary>
		public readonly NetworkPlayer sender;

		/// <summary>
		/// The time (seconds) when the message was sent, according to <see cref="uLink.NetworkTime.localTime"/>.
		/// </summary>
		public readonly double localTimestamp;

		/// <summary>
		/// The time (seconds) when the message was sent, according to <see cref="uLink.NetworkTime.serverTime"/>.
		/// </summary>
		public readonly double serverTimestamp;

		/// <summary>
		/// The time (seconds) when the message was sent, according to <see cref="uLink.NetworkTime.rawServerTime"/>.
		/// </summary>
		public readonly double rawServerTimestamp;

		/// <summary>
		/// Calculates the relative time (in seconds) since the message was sent.
		/// </summary>
		public double elapsedTimeSinceSent
		{
			get
			{
				return NetworkTime.localTime - localTimestamp;
			}
		}

		[Obsolete("NetworkMessageInfo.timestamp is deprecated, please use NetworkMessageInfo.serverTimestamp instead")]
		public double timestamp
		{
			get
			{
				return serverTimestamp;
			}
		}

		[Obsolete("NetworkMessageInfo.timestampInMillis is deprecated, please use NetworkMessageInfo.serverTimestamp instead")]
		public ulong timestampInMillis
		{
			get
			{
				return (ulong)NetTime.ToMillis(serverTimestamp);
			}
		}

		/// <summary>
		/// The <see cref="uLink.NetworkView"/> this message was sent from.
		/// </summary>
		public readonly NV networkView;

		/// <summary>
		/// The flags used when sending this message.
		/// </summary>
		public readonly NetworkFlags flags;

		internal NetworkMessageInfo(NetworkBaseLocal network, NetworkFlags flags, NetworkViewBase nv)
		{
			sender = network._localPlayer;
			localTimestamp = NetworkTime.localTime;
			serverTimestamp = network._GetMonotonicServerTime(localTimestamp);
			rawServerTimestamp = network._GetRawServerTime(localTimestamp);
			networkView = nv as NV;
			this.flags = flags;
		}

		internal NetworkMessageInfo(NetworkMessage msg, NetworkViewBase nv)
		{
			sender = msg.sender;
			localTimestamp = msg.localTimeSent;
			serverTimestamp = msg.monotonicServerTimeSent;
			rawServerTimestamp = msg.rawServerTimeSent;
			networkView = nv as NV;
			flags = msg.flags;
		}

		/// <summary>
		/// Creates a new instance of NetworkMessageInfo using the provided arguments.
		/// The class is initialized using the info parameter, except the networkview property which is initialized using the nv parameter.
		/// </summary>
		public NetworkMessageInfo(NetworkMessageInfo info, NetworkViewBase nv)
		{
			sender = info.sender;
			localTimestamp = info.localTimestamp;
			serverTimestamp = info.serverTimestamp;
			rawServerTimestamp = info.rawServerTimestamp;
			flags = info.flags;
			networkView = nv as NV;
		}

		[Obsolete("This variant of NetworkMessageInfo's constructor is deprecated, please use another constructor.")]
		public NetworkMessageInfo(NetworkPlayer sender, double localTimestamp, NetworkFlags flags, NetworkViewBase networkView)
		{
			this.sender = sender;
			this.localTimestamp = localTimestamp;
			this.networkView = networkView as NV;
			this.flags = flags;
		}

		[Obsolete("This variant of NetworkMessageInfo's constructor is deprecated, please use another constructor.")]
		public NetworkMessageInfo(NetworkPlayer sender, double localTimestamp, double rawServerTimestamp, NetworkFlags flags, NetworkViewBase networkView)
		{
			this.sender = sender;
			this.localTimestamp = localTimestamp;
			this.rawServerTimestamp = rawServerTimestamp;
			this.networkView = networkView as NV;
			this.flags = flags;
		}

		/// <summary>
		/// Creates a new instance of NetworkMessageInfo using the supplied arguments.
		/// </summary>
		public NetworkMessageInfo(NetworkPlayer sender, double localTimestamp, double serverTimestamp, double rawServerTimestamp, NetworkFlags flags, NetworkViewBase networkView)
		{
			this.sender = sender;
			this.localTimestamp = localTimestamp;
			this.serverTimestamp = serverTimestamp;
			this.rawServerTimestamp = rawServerTimestamp;
			this.networkView = networkView as NV;
			this.flags = flags;
		}

		/// <summary>
		/// Creates a new instance of NetworkMessageInfo using the supplied arguments.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="timestampInMillis"></param>
		/// <param name="flags"></param>
		/// <param name="networkView"></param>
		[Obsolete("The parameter timestampInMillis (in NetworkMessageInfo's constructor) is deprecated, please use another constructor.")]
		public NetworkMessageInfo(NetworkPlayer sender, ulong timestampInMillis, NetworkFlags flags, NetworkViewBase networkView)
		{
			this.sender = sender;
			localTimestamp = timestampInMillis * 0.001;
			this.networkView = networkView as NV;
			this.flags = flags;
		}

		public override string ToString()
		{
			return "Sender: " + sender + ", local timestamp: " + localTimestamp;
		}
	}
}
