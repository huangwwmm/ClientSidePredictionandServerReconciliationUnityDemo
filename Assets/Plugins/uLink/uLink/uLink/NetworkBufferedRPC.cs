#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10139 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:11:15 +0100 (Tue, 29 Nov 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;

// TODO: add class NetworkBufferedInstantiate (which can also be used in NetworkPlayerApproval) and method NetworkBufferedRPC.GetInstantiate()!!

namespace uLink
{
	/// <summary>
	/// Represents a buffered message. 
	/// </summary>
	/// <remarks>
	/// Buffered messages is a uLink tool that can be used to give newly connected players all 
	/// the important RPCs they need to join a game that is already running.
	/// Read more about when to use this class in 
	/// uLink.Network.<see cref="uLink.Network.uLink_OnPreBufferedRPCs"/> 
	/// and the manual chapter about RPCs.
	/// </remarks>
	public class NetworkBufferedRPC
	{
		internal bool _autoExecute;

		internal readonly NetworkMessage _msg;

		/// <summary>
		/// The <see cref="uLink.NetworkPlayer"/> who sent the buffered message.
		/// </summary>
		public NetworkPlayer sender { get { return _msg.sender; } }

		[Obsolete("NetworkBufferedRPC.timestamp is deprecated, please use NetworkBufferedRPC.serverTimestamp instead")]
		public double timestamp { get { return serverTimestamp; } }

		/// <summary>
		/// The local timestamp when the buffered message was first sent, similar to <see cref="uLink.NetworkMessageInfo.localTimestamp"/>.
		/// </summary>
		public double localTimestamp { get { return _msg.localTimeSent; } }

		/// <summary>
		/// The server timestamp when the buffered message was first sent, similar to <see cref="uLink.NetworkMessageInfo.serverTimestamp"/>.
		/// </summary>
		public double serverTimestamp { get { return _msg.monotonicServerTimeSent; } }

		/// <summary>
		/// The raw server timestamp when the buffered message was first sent, similar to <see cref="uLink.NetworkMessageInfo.rawServerTimestamp"/>.
		/// </summary>
		public double rawServerTimestamp { get { return _msg.rawServerTimeSent; } }

		/// <summary>
		/// Calculates the relative time (in seconds) since the message was sent.
		/// </summary>
		public double elapsedTimeSinceSent { get { return NetworkTime.localTime - localTimestamp; } }

		/// <summary>
		/// Gets the <see cref="uLink.NetworkViewID"/> for this buffered message.
		/// </summary>
		public NetworkViewID viewID { get { return _msg.viewID; } }
		
		/// <summary>
		/// The network flags that was used when this buffered message was sent.
		/// </summary>
		public NetworkFlags flags { get { return _msg.flags; } }

		[Obsolete("NetworkBufferedRPC.name is deprecated, please use NetworkBufferedRPC.rpcName instead")]
		public string name { get { return rpcName; } }

		/// <summary>
		/// Gets the name of this buffered RPC.
		/// </summary>
		/// <value>The name as a string. If it is an internal Instantiate or StateSync RPC, this call will return an empty string. 
		/// See <see cref="isInstantiate"/>.</value>
		public string rpcName { get { return _msg.name; } }

		/// <summary>
		/// Gets a value indicating whether this buffered message is a uLink internal instantiate RPC.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this is Instantiate; otherwise, <c>false</c>.
		/// </value>
		public bool isInstantiate { get { return _msg.internCode == NetworkMessage.InternalCode.Create; } }

		/// <summary>
		/// Gets a value indicating whether this buffered message is a StateSync message.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this is StateSync; otherwise, <c>false</c>.
		/// </value>
		public bool isStateSync
		{
			get
			{
				return _msg.internCode == NetworkMessage.InternalCode.StateSyncProxy | _msg.internCode == NetworkMessage.InternalCode.StateSyncProxyDeltaCompressed | _msg.internCode == NetworkMessage.InternalCode.StateSyncProxyDeltaCompressedInit
					 | _msg.internCode == NetworkMessage.InternalCode.StateSyncOwner | _msg.internCode == NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressed | _msg.internCode == NetworkMessage.InternalCode.StateSyncOwnerDeltaCompressedInit;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this buffered message is a user-defined RPC.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this is RPC; otherwise, <c>false</c>.
		/// </value>
		public bool isRPC { get { return _msg.internCode == NetworkMessage.InternalCode.None; } }

		internal NetworkBufferedRPC(NetworkMessage msg)
		{
			_autoExecute = true;
			_msg = msg;
		}

		/// <summary>
		/// Disable automatic execution before invoking uLink_ConnectedToServer().
		/// </summary>
		/// <remarks>Call this if you want to control the execution of buffered RPCs in clients.
		/// They can be executed at a later time by using the <see cref="ExecuteNow"/> or simply ignored. Also, 
		/// take at look at the documentation for the callback <see cref="uLink.Network.uLink_OnPreBufferedRPCs"/>
		/// </remarks>
		public void DontExecuteOnConnected()
		{
			_autoExecute = false;
		}
		
#if UNITY_BUILD
		/// <summary>
		/// Executes the RPC. 
		/// </summary>
		/// <remarks>
		/// This will also call <see cref="DontExecuteOnConnected"/>
		/// </remarks>
		public void ExecuteNow()
		{
			DontExecuteOnConnected();
			Network._singleton._ExecuteRPC(_msg);
		}
#endif
	}
}
