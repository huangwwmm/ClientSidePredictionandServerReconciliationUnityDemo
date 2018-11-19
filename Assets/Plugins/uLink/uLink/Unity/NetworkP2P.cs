#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12057 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-05-10 16:28:27 +0200 (Thu, 10 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
//by WuNan @2016/08/27 14:09:05
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace uLink
{
	/// <summary>
	/// Enables peer to peer communication between nodes. Contains implementation for the uLink.NetworkP2P script.
	/// </summary>
	/// <remarks>
	/// This class provides methods for creating and maintaining P2P connections. It enables peers to send
	/// RPC:s and transfer game objects between them. For more info, see the "P2P" chapter in the uLink manual.
	/// </remarks>
	[AddComponentMenu("uLink Basics/Network P2P")]
	public class NetworkP2P : NetworkP2PBase
	{
		[SerializeField]
		[Obfuscation(Feature = "renaming")]
		private int _listenPort = -1;

		[SerializeField]
		[Obfuscation(Feature = "renaming")]
		private int _maxConnections = 64;

		/// <summary>
		/// Included here because <see cref="rpcReceiver"/> can be 
		/// set to <see cref="uLink.RPCReceiver">RPCReceiver.OnlyObservedComponent</see>. Otherwise this field is not used.
		/// </summary>
		[SerializeField]
		// WARNING: Unity 5.x (or later) will automagically assign a "dead" GameObject (which behaves like it's null)
		// to this variable because it's serialized (and Unity is trying to catch "unassigned references").
		// So the variable won't actually be null and therefore we can't use .IsNull() or similar extensions!
		public Component observed = null;

		/// <summary>
		/// Gets or sets the receiver(s) for incoming RPCs to this game object.
		/// </summary>
		/// <value>Default is <see cref="uLink.RPCReceiver">RPCReceiver.ThisGameObject</see>
		/// </value>
		/// <remarks>All scripts attached to the same prefab/GameObject as this
		/// NetworkP2P component will be able to get this RPC and can therefore contain
		/// code for RPC receiving. If you want to put RPC receiving code in
		/// scripts attached to a root gameobject or scripts attached to a child
		/// game object, this can be done, but this property needs to be changed
		/// then.</remarks>
		[SerializeField]
		public RPCReceiver rpcReceiver = RPCReceiver.ThisGameObject;

		[SerializeField]
		[Obfuscation(Feature = "renaming")]
		// TODO: rename to rpcReceiverGameObjects, can't yet due to Unity serialize
		private GameObject[] listOfGameObjects = new GameObject[0];

		/// <summary>
		/// The GameObject's which receive RPCs send to this node using the <see cref="uLink.NetworkP2P"/> API. 
		/// This is only important if <see cref="rpcReceiver"/> is set to <see cref="RPCReceiver.GameObjects"/>
		/// </summary>
		public GameObject[] rpcReceiverGameObjects
		{
			get { return listOfGameObjects; }
			set { listOfGameObjects = value; }
		}

		[NonSerialized]
		private RPCInstanceCache _rpcInstances = new RPCInstanceCache(typeof(NetworkP2PMessageInfo).TypeHandle);

		[Obsolete("NetworkP2P.isOpen is deprecated, please use property NetworkP2P.isListening instead.")]
		public bool isOpen { get { return isListening; } }

		/// <summary>
		/// The listen port for this P2P node.
		/// </summary>
		/// <remarks>You can set it to 0 to be set by the OS to an empty port.</remarks>
		public new int listenPort
		{
			get
			{
				return _listenPort;
			}

			set
			{
				if (!isListening)
				{
					_listenPort = value;
					if (UnityEngine.Application.isPlaying) _Open();
				}
				else if (_listenPort != value)
				{
					_listenPort = value;
					_Close();
					_Open();
				}
			}
		}

		/// <summary>
		/// Maximum allowed incoming connections for this node.
		/// </summary>
		public new int maxConnections
		{
			get
			{
				return _maxConnections;
			}

			set
			{
				_maxConnections = value;
				base.maxConnections = value;
			}
		}

		/*
		public NetworkP2P()
			: base(Network._singleton)
		{
		}
		*/

		[Obsolete("NetworkP2P.Open is deprecated, please use set property NetworkP2P.listenPort instead.")]
		public void Open(int listenPort, int maxConnections)
		{
			_listenPort = listenPort;
			_maxConnections = maxConnections;
			_Open();
		}

		[Obsolete("NetworkP2P.Close is deprecated, please use 'NetworkP2P.enabled = false' instead.")]
		public void Close()
		{
			_Close();
		}

		private void _Open()
		{
			_Open(_listenPort, _maxConnections);

			if (isListening)
			{
				_listenPort = base.listenPort;
				enabled = true;
			}
			else
			{
				enabled = false;
			}
		}

		protected void Awake()
		{
			isTypeSafeByDefault = UnityEngine.Application.isEditor;
		}

		protected void OnEnable()
		{
			_Open();
		}

		protected void OnDisable()
		{
			_Close();
		}

		protected void LateUpdate()
		{
			_Update();
		}

		protected void OnApplicationQuit()
		{
			_Close();
		}

		protected override void OnEvent(string eventName, object value)
		{
			string methodName = NetworkUnity.EVENT_PREFIX + eventName;

			Profiler.BeginSample(methodName);
			SendMessage(methodName, value, SendMessageOptions.DontRequireReceiver);
			Profiler.EndSample();
		}

		protected override bool OnRPC(string rpcName, BitStream stream, NetworkP2PMessageInfo info)
		{
			Profiler.BeginSample("RPC: " + rpcName);

			var rpc = _rpcInstances.Find(this, rpcReceiver, observed, listOfGameObjects, rpcName);
			var success = rpc.Execute(stream, info);

			Profiler.EndSample();
			return success;
		}

		/// <summary>
		/// Advanced usage only: To increase performance uLink makes a cache of RPC receivers. The cache is populated 
		/// the first time an RPC is received. All RPC after the first call will be delivered to the same
		/// RPC receiver (a script component). The cache can be cleared by this method.
		/// </summary>
		public void ClearCachedRPCs()
		{
			_rpcInstances.Clear();
		}

		/// <summary>
		/// Inverses the transformation provided from world space to the local space of the component which this NetworkP2P
		/// component is attached to.
		/// </summary>
		/// <param name="pos">The position of the transform that you want to apply the transformation on.</param>
		/// <param name="rot">The rotation of the transform that you want to apply the transformation on.</param>
		public override void InverseTransform(ref UnityEngine.Vector3 pos, ref Quaternion rot)
		{
			pos = transform.InverseTransformPoint(pos);
			rot = Quaternion.Inverse(transform.rotation) * rot;
		}

		/// <summary>
		/// Transforms the provided transform position and rotation from local space of the transform that this NetworkP2P is
		/// attached to, to the world space.
		/// </summary>
		/// <param name="pos">The position that you want to transform.</param>
		/// <param name="rot">The rotation that you want to transform.</param>
		public override void Transform(ref Vector3 pos, ref Quaternion rot)
		{
			pos = transform.TransformPoint(pos);
			rot = transform.rotation * rot;
		}

		/// <summary>
		/// Creates a P2P connection to another node using its ip address and port number. 
		/// </summary>
		/// <param name="host">The ip address of other node</param>
		/// <param name="remotePort">The port number of other node</param>
		/// <remarks>
		/// If the other node requires password to allow connections use <see cref="Connect(System.String, System.Int32, System.String)"/>
		/// </remarks>
		public void Connect(string host, int remotePort)
		{
			Connect(host, remotePort, String.Empty);
		}

		/// <summary>
		/// Creates a P2P connection to another node using its ip address, port number and also
		/// password if other node requires password to allow connection.
		/// </summary>
		/// <param name="host">The ip address of other node</param>
		/// <param name="remotePort">The port number of other node</param>
		/// <param name="incomingPassword">The password which the other node requires to allow connections</param>
		public void Connect(string host, int remotePort, string incomingPassword)
		{
			Connect(new NetworkPeer(host, remotePort), incomingPassword);
		}

		/// <summary>
		/// Creates a P2P connection to another node using its <see cref="uLink.NetworkPeer"/>.
		/// </summary>
		/// <param name="target">The <see cref="uLink.NetworkPeer"/> of other node</param>
		/// <remarks>
		/// If the other node requires password to allow connections use <see cref="Connect(uLink.NetworkPeer, System.String)"/>
		/// </remarks>
		public void Connect(NetworkPeer target)
		{
			Connect(target, String.Empty);
		}

		/// <summary>
		/// Creates a P2P connection to another node using its <see cref="uLink.NetworkPeer"/> and also a password
		/// if the other node requires password to allow connection.
		/// </summary>
		/// <param name="target">The <see cref="uLink.NetworkPeer"/> of other node</param>
		/// <param name="incomingPassword">The password which the other node requires to allow connections</param>
		public new void Connect(NetworkPeer target, string incomingPassword)
		{
			if (!enabled) enabled = true;
			base.Connect(target, incomingPassword);
		}

		/// <summary>
		/// Moves a network aware object to a remote node.
		/// </summary>
		/// <param name="obj">The network aware object which you want to move to another remote node</param>
		/// <param name="target">The <see cref="uLink.NetworkPeer"/> of the remote node which <c>obj</c> should be moved to</param>
		/// <remarks>
		/// The object will be instantiated with a new viewID on the receiving end.
		/// </remarks>
		[Obsolete("NetworkP2P.Replicate is deprecated, please use NetworkP2P.Handover (with NetworkP2PHandoverFlags.DontRedirectOwner and/or NetworkP2PHandoverFlags.DontDestroyOriginal) instead.")]
		public void Replicate(Object obj, NetworkPeer target) { base.Replicate(Require(obj), target); }

		/// <summary>
		/// Moves a network aware object to a remote node.
		/// Adjusts the objects position and rotation on the receiving node according to the given relative parameters.
		/// </summary>
		/// <param name="obj">The network aware object which you want to move to another remote node</param>
		/// <param name="target">The <see cref="uLink.NetworkPeer"/> of the remote port which <c>obj</c> should be moved to</param>
		/// <param name="relativePos">The position which the <c>obj</c>'s position should be set to in other node</param>
		/// <param name="relativeRot">The rotation which the <c>obj</c>'s rotation should be set to in other node</param>
		/// <remarks>
		/// The object will be instantiated with a new viewID on the receiving end.
		/// </remarks>
		[Obsolete("NetworkP2P.Replicate is deprecated, please use NetworkP2P.Handover (with NetworkP2PHandoverFlags.DontRedirectOwner and/or NetworkP2PHandoverFlags.DontDestroyOriginal) instead.")]
		public void Replicate(Object obj, NetworkPeer target, Vector3 relativePos, Quaternion relativeRot) { base.Replicate(Require(obj), target, relativePos, relativeRot); }

		/// <summary>
		/// Moves a network aware object to a remote node.
		/// </summary>
		/// <param name="obj">The network aware object which you want to move to another remote node</param>
		/// <param name="target">The <see cref="uLink.NetworkPeer"/> which <c>obj</c> should be moved to</param>
		/// <param name="flags">The <see cref="uLink.NetworkP2PHandoverFlags"/> which sets the setting of HandOver</param>
		/// <param name="handoverData">The data which can be used in <see cref="NetworkP2P.uLink_OnHandoverNetworkView"/> in other node</param>
		/// <remarks>
		/// Can only be invoked server-side.
		/// owner redirection and instantiations can be customized
		/// using the <c>flags</c> argument.
		/// </remarks>
		public void Handover(Object obj, NetworkPeer target, NetworkP2PHandoverFlags flags, params object[] handoverData) { base.Handover(Require(obj), target, flags, handoverData); }

		/// <summary>
		/// Moves a network aware object to a remote node.
		/// Adjusts the objects position and rotation on the receiving node according to the given relative parameters.
		/// </summary>
		/// <param name="obj">The network aware object which you want to move to another remote node</param>
		/// <param name="target">The <see cref="uLink.NetworkPeer"/> which <c>obj</c> should be moved to</param>
		/// <param name="relativePos">The position which the <c>obj</c>'s position should be set to in other node</param>
		/// <param name="relativeRot">The rotation which the <c>obj</c>'s rotation should be set to in other node</param>
		/// <param name="flags">The <see cref="uLink.NetworkP2PHandoverFlags"/> which sets the setting of HandOver</param>
		/// <param name="handoverData">The data which can be used in <see cref="NetworkP2P.uLink_OnHandoverNetworkView"/> in other node</param>
		/// <remarks>
		/// Can only be invoked server-side.
		/// owner redirection and instantiations can be customized
		/// using the <c>flags</c> argument.
		/// </remarks>
		public void Handover(Object obj, NetworkPeer target, Vector3 relativePos, Quaternion relativeRot, NetworkP2PHandoverFlags flags, params object[] handoverData) { base.Handover(Require(obj), target, relativePos, relativeRot, flags, handoverData); }

		/// <summary>
		/// Moves a network object and it's owner to another server. 
		/// </summary>
		/// <param name="obj">The network aware object which you want to move to another remote node</param>
		/// <param name="target">The <see cref="uLink.NetworkPeer"/> which <c>obj</c> should be moved to</param>
		/// <remarks>
		/// Can only be invoked server-side.
		/// The player's client will automatically be reconnected to the new server and
		/// the object will be instantiated with a new viewID on the receiving end.
		/// </remarks>
		public void Handover(Object obj, NetworkPeer target) { base.Handover(Require(obj), target); }

		/// <summary>
		/// Moves a network object and it's owner to another server. 
		/// Adjusts the players position and rotation on the receiving peer according to the given relative parameters.
		/// </summary>
		/// <remarks>
		/// Can only be invoked server-side.
		/// The player's client will automatically be reconnected to the new server.
		/// </remarks>
		public void Handover(Object obj, NetworkPeer target, Vector3 relativePos, Quaternion relativeRot) { base.Handover(Require(obj), target, relativePos, relativeRot); }

		[Obsolete("Handover argument keepPlayerID is deprecated, please call Handover without it.")]
		public void Handover(Object obj, NetworkPeer target, bool keepPlayerID) { base.Handover(Require(obj), target); }

		[Obsolete("Handover argument keepPlayerID is deprecated, please call Handover without it.")]
		public void Handover(Object obj, NetworkPeer target, bool keepPlayerID, Vector3 offsetPos, Quaternion offsetRot) { base.Handover(Require(obj), target, offsetPos, offsetRot); }

		public override string ToString()
		{
			return ToHierarchyString();
		}

		/// <summary>
		/// Returns <see cref="uLink.NetworkP2P"/> component of <c>gameObject</c> and <c>null</c> 
		/// if there's no <see cref="uLink.NetworkP2P"/> component attached to <c>gameObject</c>.
		/// </summary>
		/// <param name="gameObject">The GameObject which you want its <see cref="uLink.NetworkP2P"/></param>
		/// <returns></returns>
		public static NetworkP2P Get(GameObject gameObject) { return gameObject.GetComponent<NetworkP2P>(); }

		/// <summary>
		/// Returns <see cref="uLink.NetworkP2P"/> of the first GameObject it find which has <c>component</c> and also a <see cref="uLink.NetworkP2P"/>,
		/// if there's no GameObject with <c>component</c> or none of the GameObjects with <c>component</c> have <see cref="uLink.NetworkP2P"/>, returns <c>null</c>.
		/// </summary>
		/// <param name="component">The component which you want its GameObject's <see cref="uLink.NetworkP2P"/></param>
		/// <returns></returns>
		public static NetworkP2P Get(Component component) { return component.GetComponent<NetworkP2P>(); }

		private static NetworkView Require(Object obj)
		{
			NetworkView nv = null;

			if (obj is NetworkView)
			{
				nv = obj as NetworkView;
			}
			else if (obj is Component)
			{
				nv = (obj as Component).GetComponent<NetworkView>();
			}
			else if (obj is GameObject)
			{
				nv = (obj as GameObject).GetComponent<NetworkView>();
			}
			else if (obj.IsNullOrDestroyed())
			{
				throw new ArgumentNullException("obj");
			}

			if (nv.IsNullOrDestroyed())
			{
				throw new ArgumentException("Missing NetworkView", "obj");
			}

			return nv;
		}

#if UNITY_DOC
		/// <summary>
		/// Message callback: Called on the sender side when a <see cref="Connect(uLink.NetworkPeer)"/> was invoked and has completed.
		/// </summary>
		public void uLink_OnPeerInitialized() { }

		/// <summary>
		/// Message callback: Called when either the local or a remote peer has changed status to connected.
		/// </summary>
		/// <param name="peer">The newly connected peer.</param>
		public void uLink_OnPeerConnected(uLink.NetworkPeer peer) { }

		/// <summary>
		/// Message callback: Called when either the local or a remote peer has changed status to disconnected.
		/// </summary>
		/// <param name="peer">The disconnected peer.</param>
		public void uLink_OnPeerDisconnected(uLink.NetworkPeer peer) { }

		/// <summary>
		/// Message callback: Called on the sender side when a <see cref="Connect(uLink.NetworkPeer)"/> was invoked and has failed.
		/// </summary>
		/// <remarks>
		/// Remark about the connection timeout for P2P connections: A connection attempt will be 
		/// done every 2.5 seconds, 5 times in a row. After that, at around 12.5 seconds, this
		/// callback will be triggered with a uLink.NetworkConnectionError.ConnectionTimeout.
		/// </remarks>
		public void uLink_OnFailedToConnectToPeer(uLink.NetworkConnectionError error) { }

		/// <summary>
		/// Message callback: Called when discovered local peers or received known peers data
		/// </summary>
		/// <param name="ev">The <see cref="uLink.NetworkP2PEvent"/> which this callback is invoked on</param>
		/// <example>
		/// You should have a peer with remote port 8000 with its <see cref="uLink.NetworkP2PBase.peerType"/>
		/// sets to 'PeerType' for this example to work and also connect a peer to it since discovery method
		/// calls in <see cref="uLink.NetworkP2P.uLink_OnPeerConnected"/>.
		/// <code>
		/// private void uLink_OnPeerConnected(NetworkPeer peer)
		/// {
		///     networkP2P.DiscoverLocalPeers(new PeerDataFilter("PeerType"), 8000);
		/// }
		/// 
		/// private void uLink_OnPeerEvent(uLink.NetworkP2PEvent ev)
		/// {
		/// 	if (ev == uLink.NetworkP2PEvent.LocalPeerDiscovered)
		/// 	{
		/// 		var peers = networkP2P.PollDiscoveredPeers();
		/// 		Debug.Log(peers[0].ping);
		/// 	}
		/// }
		/// </code>
		/// </example>
		public void uLink_OnPeerEvent(uLink.NetworkP2PEvent ev) { }

#endif
	}
}

#endif
