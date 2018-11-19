#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision$
// $LastChangedBy$
// $LastChangedDate$
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
namespace uLink
{
#if UNITY_BUILD
	using P2P = NetworkP2P;
#else
	using P2P = NetworkP2PBase;
#endif

	/// <summary>
	/// A class containing some extra information about the sender of this network P2P message.
	/// </summary>
	/// <remarks>You should declare a parameter of this type at the end of your 
	/// RPC's parameter list to get this information.</remarks>
	/// <example>
	/// <code>
	/// [RPC]
	/// void P2PRPCSample(string normalParam,NetworkP2PMessageInfo info)
	/// {
	///		//use the info parameter here to get more info about the sender and flags of the message.
	/// }
	/// </code>
	/// </example>
	public class NetworkP2PMessageInfo
	{
		/// <summary>
		/// The <see cref="uLink.NetworkPeer"/> who sent the message.
		/// </summary>
		public readonly NetworkPeer sender;

		/// <summary>
		/// The flags used when sending this message.
		/// </summary>
		public readonly NetworkFlags flags;

		/// <summary>
		/// The <see cref="uLink.NetworkP2P"/> this message was sent from.
		/// </summary>
		public readonly P2P networkP2P;

		internal NetworkP2PMessageInfo(NetworkP2PMessage msg, NetworkP2PBase p2p)
		{
			sender = new NetworkPeer(msg.connection.RemoteEndpoint);
			flags = msg.flags;
			networkP2P = p2p as P2P;
		}

		/// <summary>
		/// Initializes a NetworkP2PMessageInfo. 
		/// You should not need to use this in most situations.
		/// Don't use it unless you really know what you are doing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="flags"></param>
		/// <param name="networkP2P"></param>
		public NetworkP2PMessageInfo(NetworkPeer sender, NetworkFlags flags, P2P networkP2P)
		{
			this.sender = sender;
			this.flags = flags;
			this.networkP2P = networkP2P;
		}

		public override string ToString()
		{
			return "Sender: " + sender;
		}
	}
}
