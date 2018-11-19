#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12165 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-24 12:31:56 +0200 (Thu, 24 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

//by WuNan @2016/08/27 14:33:08
using Quaternion = UnityEngine.Quaternion;
// TODO: optimize type-specific RPC-receiver by looking up typeName using typeHandle

namespace uLink
{
	/// <summary>
	/// Dictates who will receive RPCs sent to a game Object.
	/// Read about this enum in <see cref="uLink.NetworkView.rpcReceiver"/>.
	/// </summary>
	public enum RPCReceiver : byte
	{
		/// <summary>
		/// Does not listen for incoming RPCs to this networkView, RPCs will be ignored.
		/// </summary>
		Off,

		/// <summary>
		/// Forwards incoming RPCs only to the observed component property, if it is a MonoBehaviour.
		/// </summary>
		OnlyObservedComponent,

		/// <summary>
		/// Forwards incoming RPCs to all MonoBehaviours in this gameobject. Default value.
		/// </summary>
		ThisGameObject,

		/// <summary>
		/// Forwards incoming RPCs to all MonoBehaviours in this gameobject and also to all 
		/// MonoBehaviours in all of this GameObject's children.
		/// </summary>
		ThisGameObjectAndChildren,

		/// <summary>
		/// Forwards incoming RPCs to all MonoBehaviours in the root gameobject, which this
		/// gameobject belongs to, and also to all MonoBehaviours in all the root's children.
		/// </summary>
		RootGameObjectAndChildren,

		/// <summary>
		/// Forwards incoming RPCs to all MonoBehaviours in all GameObjects activated in the scene.
		/// </summary>
		AllActiveGameObjects,

		/// <summary>
		/// Forwards incoming RPCs to all MonoBehaviours in the GameObjects specified by the rpcReceiverGameObjects property.
		/// </summary>
		GameObjects
	}

	/// <summary>
	/// Very important class in uLink, contains implementation for the uLink.NetworkView script.
	/// </summary>
	/// <remarks>
	/// By adding the script uLink.NetworkView as a component in your GameObject
	/// or a prefab in Unity it is ready to be used for sending and receiving
	/// StateSyncs and RPCs. The networkView is every network aware object's 
	/// only way to get data from the network and send data over the network.
	/// That is why you will find methods like 
	/// <see cref="O:uLink.NetworkView.RPC"/> and 
	/// <see cref="O:uLink.NetworkView.UnreliableRPC"/> 
	/// here.
	/// <para>
	/// If you have objects which you don't need RPCs and State synch, then there is no need for them to have NetworkViews.
	/// It's true even if they use Network class to do operations like <see cref="uLink..Network.Connect"/>
	/// </para>
	/// <para>
	/// Multiple NetworkViews are supported for compatibility reasons but they are not recommended and RPCs
	/// will be sent to the first NetworkView. If you want to observe multiple components for State Sync then use the utility component
	/// ObservedList to achieve what you want.
	/// </para>
	/// </remarks>
	/// <example>
	/// The common ways of interacting with the NetworkView instance for network aware objects
	/// in C# is writing classes 
	/// that extends <see cref="uLink.MonoBehaviour"/> and then accessing the networkView
	/// property available in all subclasses to <see cref="uLink.MonoBehaviour"/>.
	/// <code>
	/// public class Example : uLink.MonoBehaviour {
	///	
	///	private int myID = networkView.viewID.id; 
	///	
	/// }
	/// </code>
	/// The common way of interacting with the NetworkView in a JavaScript is writing
	/// scripts that retrieves the networkView like in the code below. 
	/// <code>
	/// 
	/// private var networkView = uLink.NetworkView.Get(this);
	/// private var myID = networkView.viewID.id;
	/// 
	/// </code>    
	/// 
	/// For this code to work, remember to add the 
	/// script you write as a script component to the prefab/GameObject.
	/// </example>
	[AddComponentMenu("uLink Basics/Network View")]
	public class NetworkView : NetworkViewBase
	{
		/// <summary>
		/// The Component that this networkView should serialize when StateSync is being sent or is received.
		/// </summary>
		/// <remarks>
		/// Note that <see cref="rpcReceiver"/> can be 
		/// set to <see cref="uLink.RPCReceiver">RPCReceiver.OnlyObservedComponent</see>. 
		/// In that case this field also dictates the rpcReceiver for this NetworkView.
		/// <para>
		/// Read more about the Component class in the Unity documentation.
		/// </para>
		/// </remarks>
		[SerializeField]
		// WARNING: Unity 5.x (or later) will automagically assign a "dead" GameObject (which behaves like it's null)
		// to this variable because it's serialized (and Unity is trying to catch "unassigned references").
		// So the variable won't actually be null and therefore we can't use .IsNull() or similar extensions!
		public Component observed = null;

		[NonSerialized]
		private NetworkObserved _observedCache;

		[SerializeField]
		[Obfuscation(Feature = "renaming")]
		public int _manualViewID = -1;

		/// <summary>
		/// Gets or sets the receivers for incoming RPCs to this network view.
		/// </summary>
		/// <value>Default is <see cref="uLink.RPCReceiver.ThisGameObject"/>
		/// </value>
		/// <remarks>All scripts attached to the same prefab/GameObject as this
		/// NetworkView will be able to get this RPC and can therefore contain
		/// code for RPC receiving. If you want to put RPC receiving code in
		/// scripts attached to a root GameObject or scripts attached to a child
		/// gameobject, this can be done, but this property needs to be changed
		/// then.</remarks>
		[SerializeField]
		public RPCReceiver rpcReceiver = RPCReceiver.ThisGameObject;

		[SerializeField]
		[Obfuscation(Feature = "renaming")]
		// TODO: rename to rpcReceiverGameObjects, can't yet due to Unity serialize
		private GameObject[] listOfGameObjects = new GameObject[0];

		/// <summary>
		/// The GameObject's which receive RPCs send to this network aware object.
		/// This is only valid if <see cref="rpcReceiver"/> is set to <see cref="RPCReceiver.Gameobjects"/>
		/// </summary>
		public GameObject[] rpcReceiverGameObjects
		{
			get { return listOfGameObjects; }
			set { listOfGameObjects = value; }
		}

		[NonSerialized]
		private RPCInstanceCache _rpcInstances = new RPCInstanceCache(typeof(NetworkMessageInfo).TypeHandle);

		/// <summary>
		/// You can customize the process of instantiation and destruction of this network aware object by assigning
		/// methods to <see cref="uLink.Network.destroyer"/> and <see cref="uLink.NetworkInstantiator.instantiator"/> delegates of this field. 
		/// </summary>
		/// <remarks>
		/// For more information <see cref="uLink.NetworkInstantiator"/>
		/// </remarks>
		[NonSerialized]
		public NetworkInstantiator instantiator = new NetworkInstantiator(null, NetworkInstantiator.defaultDestroyer);

		[SerializeField]
		[Obfuscation(Feature = "renaming")]
		// WARNING: Unity 5.x (or later) will automagically assign a "dead" GameObject (which behaves like it's null)
		// to this variable because it's serialized (and Unity is trying to catch "unassigned references").
		// So the variable won't actually be null and therefore we can't use .IsNull() or similar extensions!
		private GameObject _prefabRoot;

		/// <summary>
		/// Gets or sets the root gameobject of the gameobject that this networkView is attached to.
		/// </summary>
		public GameObject prefabRoot
		{
			get
			{
				var retval = root._prefabRoot;
				return retval.IsNotNullOrUnassigned() ? retval : root.gameObject;
			}

			set { root._prefabRoot = value; }
		}

		/*
		public NetworkView()
			: base(Network._singleton)
		{
		}
		*/

		/// <summary>
		/// Returns networkView of this networkView's parent.
		/// </summary>
		public new NetworkView parent { get { return base.parent as NetworkView; } }

		/// <summary>
		/// Returns networkView of this networkView's root.
		/// </summary>
		public new NetworkView root { get { return base.root as NetworkView; } }

		/// <summary>
		/// Gets the position of this network aware object.
		/// </summary>
		/// <remarks>This is the position of the root of the gameObject hierarchy so if the networkview is attached to a child GameObject
		/// then it will retun the root's position.
		/// You might for example group all of your NPCs as children of a NPCRoot GameObject,
		/// then that NPCRoot's position will be returned.
		/// </remarks>
		public override Vector3 position
		{
			get { return prefabRoot.transform.position; }
			set { prefabRoot.transform.position = value; }
		}

		/// <summary>
		/// Gets the rotation of this network aware object.
		/// </summary>
		public override Quaternion rotation
		{
			get { return prefabRoot.transform.rotation; }
			set { prefabRoot.transform.rotation = value; }
		}

		protected override bool hasSerializeProxy { get { _observedCache.UpdateBinding(observed); return _observedCache.serializeProxy != null; } }
		protected override bool hasSerializeOwner { get { _observedCache.UpdateBinding(observed); return _observedCache.serializeOwner != null; } }
		protected override bool hasSerializeHandover { get { _observedCache.UpdateBinding(observed); return _observedCache.serializeHandover != null; } }
		protected override bool hasSerializeCellProxy { get { _observedCache.UpdateBinding(observed); return _observedCache.serializeCellProxy != null; } }

		/// <summary>
		/// Returns networkView of the specified child.
		/// </summary>
		/// <param name="childIndex">Index of this network aware GameObject's child in hierarchy.</param>
		/// <returns></returns>
		public new NetworkView GetChild(int childIndex)
		{
			return base.GetChild(childIndex) as NetworkView;
		}

		protected void Awake()
		{
			if (_manualViewID > 0)
			{
				SetManualViewID(_manualViewID);
			}
			else
			{
				NetworkInstantiatorUtility._DoAutoSetupNetworkViewOnAwake(this);
			}
		}

		protected void OnDestroy()
		{
			// TODO: change _childID when enabled is changed

			_network.RemoveNetworkView(this);
		}

		protected override bool OnSerializeProxy(BitStream stream, NetworkMessageInfo info)
		{
			Profiler.BeginSample(NetworkObserved.EVENT_SERIALIZE_PROXY, this);

			if (_observedCache.serializeProxy == null)
			{
				Log.Error(NetworkLogFlags.StateSync, NetworkObserved.EVENT_SERIALIZE_PROXY, " is missing in ", observed.GetType());

				Profiler.EndSample();
				return false;
			}

			var failsafeBuffer = stream._buffer;

			int oldPos = stream._buffer.PositionBits;
			int oldLen = stream._buffer.LengthBits;

			try
			{
				_observedCache.serializeProxy(stream, info);
			}
			catch (Exception ex)
			{
				Log.Error(NetworkLogFlags.StateSync, "Exception was thrown in ", NetworkObserved.EVENT_SERIALIZE_PROXY, ": ", ex);

				stream._buffer = failsafeBuffer;
				Profiler.EndSample();
				return false;
			}

			if (oldLen == stream._buffer.LengthBits && oldPos == stream._buffer.PositionBits)
			{
				Log.Debug(NetworkLogFlags.StateSync, "Nothing was read or written in ", NetworkObserved.EVENT_SERIALIZE_PROXY, " for ", this);

				Profiler.EndSample();
				return false;
			}

			Profiler.EndSample();
			return true;
		}

		protected override bool OnSerializeOwner(BitStream stream, NetworkMessageInfo info)
		{
			Profiler.BeginSample(NetworkObserved.EVENT_SERIALIZE_OWNER, this);

			if (_observedCache.serializeOwner == null)
			{
				Log.Error(NetworkLogFlags.StateSync, NetworkObserved.EVENT_SERIALIZE_OWNER, " is missing in ", observed.GetType());

				Profiler.EndSample();
				return false;
			}

			var failsafeBuffer = stream._buffer;

			int oldPos = stream._buffer.PositionBits;
			int oldLen = stream._buffer.LengthBits;

			try
			{
				_observedCache.serializeOwner(stream, info);
			}
			catch (Exception ex)
			{
				Log.Error(NetworkLogFlags.StateSync, "Exception was thrown in ", NetworkObserved.EVENT_SERIALIZE_OWNER, ": ", ex);

				stream._buffer = failsafeBuffer;
				Profiler.EndSample();
				return false;
			}

			if (oldLen == stream._buffer.LengthBits && oldPos == stream._buffer.PositionBits)
			{
				Log.Debug(NetworkLogFlags.StateSync, "Nothing was read or written in ", NetworkObserved.EVENT_SERIALIZE_OWNER, " for ", this);

				Profiler.EndSample();
				return false;
			}

			Profiler.EndSample();
			return true;
		}

		protected override bool OnSerializeHandover(BitStream stream, NetworkMessageInfo info)
		{
			Profiler.BeginSample(NetworkObserved.EVENT_SERIALIZE_HANDOVER, this);

			if (_observedCache.serializeHandover == null)
			{
				Log.Error(NetworkLogFlags.StateSync, NetworkObserved.EVENT_SERIALIZE_HANDOVER, " is missing in ", observed.GetType());

				Profiler.EndSample();
				return false;
			}

			var failsafeBuffer = stream._buffer;

			int oldPos = stream._buffer.PositionBits;
			int oldLen = stream._buffer.LengthBits;

			try
			{
				_observedCache.serializeHandover(stream, info);
			}
			catch (Exception ex)
			{
				Log.Error(NetworkLogFlags.StateSync, "Exception was thrown in ", NetworkObserved.EVENT_SERIALIZE_HANDOVER, ": ", ex);

				stream._buffer = failsafeBuffer;
				Profiler.EndSample();
				return false;
			}

			if (oldLen == stream._buffer.LengthBits && oldPos == stream._buffer.PositionBits)
			{
				Log.Debug(NetworkLogFlags.StateSync, "Nothing was read or written in ", NetworkObserved.EVENT_SERIALIZE_HANDOVER, " for ", this);

				Profiler.EndSample();
				return false;
			}

			Profiler.EndSample();
			return true;
		}

		protected override bool OnSerializeCellProxy(BitStream stream, NetworkMessageInfo info)
		{
			Profiler.BeginSample(NetworkObserved.EVENT_SERIALIZE_CELLPROXY, this);

			if (_observedCache.serializeCellProxy == null)
			{
				Log.Error(NetworkLogFlags.StateSync, NetworkObserved.EVENT_SERIALIZE_CELLPROXY, " is missing in ", observed.GetType());

				Profiler.EndSample();
				return false;
			}

			var failsafeBuffer = stream._buffer;

			int oldPos = stream._buffer.PositionBits;
			int oldLen = stream._buffer.LengthBits;

			try
			{
				_observedCache.serializeCellProxy(stream, info);
			}
			catch (Exception ex)
			{
				Log.Error(NetworkLogFlags.StateSync, "Exception was thrown in ", NetworkObserved.EVENT_SERIALIZE_CELLPROXY, ": ", ex);

				stream._buffer = failsafeBuffer;
				Profiler.EndSample();
				return false;
			}

			if (oldLen == stream._buffer.LengthBits && oldPos == stream._buffer.PositionBits)
			{
				Log.Debug(NetworkLogFlags.StateSync, "Nothing was read or written in ", NetworkObserved.EVENT_SERIALIZE_CELLPROXY, " for ", this);

				Profiler.EndSample();
				return false;
			}

			Profiler.EndSample();
			return true;
		}

		protected override bool OnRPC(string rpcName, BitStream stream, NetworkMessageInfo info)
		{
			Profiler.BeginSample("RPC: " + rpcName, this);

			var rpc = _rpcInstances.Find(this, rpcReceiver, observed, listOfGameObjects, rpcName);
			var success = rpc.Execute(stream, info);

			Profiler.EndSample();
			return success;
		}

		/// <summary>
		/// Try to execute the RPC with specified parameters in this network aware gameobject.
		/// Returns <c>true</c> if executing RPC was successful, else returns <c>false</c>
		/// </summary>
		/// <param name="rpcName">Name of the RPC</param>
		/// <param name="parameters">The parameters which should pass to the RPC</param>
		/// <remarks>The execution can go wrong for two reasons, The RPC might not exist or an exception
		/// can be thrown inside the RPC which causes the execution to fail.</remarks>
		public bool ExecuteRPC(string rpcName, object[] parameters)
		{
			var rpc = _rpcInstances.Find(this, rpcReceiver, observed, listOfGameObjects, rpcName);
			var success = rpc.Execute(parameters);
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
		/// Set the scope of the network view in relation to a specific network player.
		/// This can be used to turn StateSync updates and RPCs temporarily on and off for a specific player, to reduce the 
		/// network traffic and thus bandwidth demands on the server and player. 
		/// </summary>
		/// <remarks>The SetScope function must be called from the server in an authoritative server architecture; it will return an error if called from the client.
		/// 
		/// This can be used to implement relevant sets. If a player can't see a network view object, then relevancy can be turned off for that player. When the player (or 
		/// the object) later moves and is able to see the object, the relevancy must be turned on again. Read more in the 
		/// uLink manual about distance culling and occlusion culling.
		/// </remarks>
		/// <param name="target">The player affected by this scope change.</param>
		/// <param name="relevancy">Set to true or false depending on if you want the player to receive StateSync updates and RPCs from the network view or not.</param>
		/// <returns>The previous relevancy for the specific player.</returns>
		public new bool SetScope(NetworkPlayer target, bool relevancy)
		{
			Awake(); // might not be necessary, but just in case.
			return base.SetScope(target, relevancy);
		}

		/// <summary>
		/// Sends a reliable RPC. Receivers are dictated by <see cref="uLink.RPCMode"/>.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// The called method must have the [RPC] attribute for C-Sharp
		/// code. A NetworkView must be attached to the GameObject where the
		/// RPC method is being called. It doesn't matter if the NetworkView
		/// is being used for something else or just for the RPC method. If it
		/// is just for the RPC method, state synchronization should be turned
		/// off and the observed property can be set to none. RPC method names
		/// should be unique across the scene, if two RPC methods in
		/// different scripts have the same name only one of them is called when
		/// RPC is invoked. Reliable RPC calls are always guaranteed to be executed in
		/// the same order as they are sent. The communication group set for the
		/// network view, with NetworkView.group, is used for the RPC call. To
		/// get information on the RPC itself, you can add a <see cref="uLink.NetworkMessageInfo"/>
		/// parameter to the receiving method's declaration, which will automatically
		/// contain the information. You don't need to change the way you call
		/// the RPC method when you do this. For more information see the RPC
		/// section of the uLink manual. 
		/// </remarks>
		/// <example>
		/// <code>
		/// public Transform cubePrefab;
		/// void OnGUI ()
		/// {
		///    if (GUILayout.Button("SendMessage"))
		///    {
		///       networkView.RPC("TransferStrings",
		///       RPCMode.All, "uLink ", "makes ", "me " "happy.");
		///    }
		/// }
		///
		/// [RPC]
		/// void TransferStrings (String s1, String s2 , String s3, String s4) 
		/// {
		///     Debug.Log("Got: " s1 + s2 + s3 + s4); 
		/// }
		/// </code>
		/// </example>
		public void RPC(string rpcName, RPCMode mode, params object[] args) { RPC(_rpcFlags, rpcName, mode, args); }

		/// <summary>
		/// Sends a reliable RPC. Receivers are dictated by <see cref="uLink.RPCMode"/>.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// to the RPC method executed there (as argument)</param>
		/// <remarks>
		/// If you want to send only one argument which is an array type, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(string rpcName, RPCMode mode, params object[] args)"/>
		/// </remarks>
		public void RPC<T>(string rpcName, RPCMode mode, T arg) { RPC(_rpcFlags, rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends a reliable RPC to the specified <see cref="uLink.NetworkPlayer"/>.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <seealso cref="RPC(System.String,uLink.RPCMode,System.Object[])"/>
		public void RPC(string rpcName, NetworkPlayer target, params object[] args) { RPC(_rpcFlags, rpcName, target, args); }

		/// <summary>
		/// Sends a reliable RPC to the specified <see cref="uLink.NetworkPlayer"/>.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// to the RPC method executed there (as argument)</param>
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// </remarks>
		/// <seealso cref="RPC(string rpcName, RPCMode mode, params object[] args)"/>
		public void RPC<T>(string rpcName, NetworkPlayer target, T arg) { RPC(_rpcFlags, rpcName, target, (object)arg); }

		/// <summary>
		/// Sends a reliable RPC to several NetworkPlayers.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		/// <remarks>This can be used to send something to team mates, private chat ...</remarks>
		public void RPC(string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { RPC(_rpcFlags, rpcName, targets, args); }

		/// <summary>
		/// Sends a reliable RPC to several NetworkPlayers.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args)"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { RPC(_rpcFlags, rpcName, targets, (object)arg); }

		/// <summary>
		/// Sends an RPC. Receivers are dictated by <see cref="uLink.RPCMode"/>.
		/// The <c>flags</c> parameter can be used to customize the way that the RPC is sent.
		/// </summary>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to specify optional <see cref="uLink.NetworkFlags"/>
		/// . By setting the flags you have the power to control the handling of
		/// this RPC in uLink. Primarily you should use 
		/// <see cref="O:uLink.NetworkView.RPC"/> or 
		/// <see cref="O:uLink.NetworkView.UnreliableRPC"/> or 
		/// <see cref="O:uLink.NetworkView.UnencryptedRPC"/> (without NetworkFlags parameter). But
		/// sometimes those three methods are not enough,  then it is OK to
		/// use this method. Note that the buffer flag will override the buffer setting you can control
		/// via the <see cref="uLink.RPCMode"/> argument.
		/// </remarks>
		/// <example>
		/// If you want to send an RPC that is encrypted AND unreliable you can
		/// do that. Or if you want to send an RPC that has no timestamp, to
		/// save bandwidth, it is possible by setting the correct flag.
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode,System.Object[])"/>
		public new void RPC(NetworkFlags flags, string rpcName, RPCMode mode, params object[] args) { Awake(); base.RPC(flags, rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC. Receivers are dictated by <see cref="uLink.RPCMode"/>.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(uLink.NetworkFlags, System.String, uLink.RPCMode, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(NetworkFlags flags, string rpcName, RPCMode mode, T arg) { Awake(); base.RPC(flags, rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/>.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <seealso cref="RPC(System.String, uLink.RPCMode,System.Object[])"/>
		public new void RPC(NetworkFlags flags, string rpcName, NetworkPlayer target, params object[] args) { Awake(); base.RPC(flags, rpcName, target, args); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/>
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>		
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="args"></param>
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(NetworkFlags flags, string rpcName, NetworkPlayer target, params object[] args)"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode ,System.Object[])"/>
		public void RPC<T>(NetworkFlags flags, string rpcName, NetworkPlayer target, T arg) { Awake(); base.RPC(flags, rpcName, target, (object)arg); }

		/// <summary>
		/// Sends an RPC to several network players. 
		/// </summary>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <remarks>
		/// Use this RPC method to specify optional <see cref="uLink.NetworkFlags"/>
		/// . By setting the flags you have the power to control the handling of
		/// this RPC in uLink. Primarily you should use 
		/// <see cref="O:uLink.NetworkView.RPC"/> or 
		/// <see cref="O:uLink.NetworkView.UnreliableRPC"/> or 
		/// <see cref="O:uLink.NetworkView.UnencryptedRPC"/> (without NetworkFlags parameter). But
		/// sometimes those three methods are not enough,  then it is OK to
		/// use this method. Note that the buffer flag will override the buffer setting you can control
		/// via the <see cref="uLink.RPCMode"/> argument.
		/// </remarks>
		/// <example>
		/// If you want to send an RPC that is encrypted AND unreliable you can
		/// do that. Or if you want to send an RPC that has no timestamp, to
		/// save bandwidth, it is possible by setting the correct flag.
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public new void RPC(NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { Awake(); base.RPC(flags, rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC to several network players. 
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args)"/>
		/// </remarks>
		/// <seealso cref="RPC(string rpcName, RPCMode mode, params object[] args)"/>
		public void RPC<T>(NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { Awake(); base.RPC(flags, rpcName, targets, (object)arg); }

		/// <summary>
		/// Sends an unreliable RPC.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// Works exactly like <see cref="O:uLink.NetworkView.RPC"/>, except that it is sent over an unreliable channel in uLink. This saves resources on the server and the client because there is no need to handle resends in uLink.
		/// Unreliable RPCs can be used for everything which if they are not received, the information will be soon replaced
		/// by something else and the failure of receiving it can be tolerated.
		/// </remarks>
		public void UnreliableRPC(string rpcName, RPCMode mode, params object[] args) { RPC(_rpcFlags | NetworkFlags.Unreliable, rpcName, mode, args); }

		/// <summary>
		/// Sends an unreliable RPC.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// Works exactly like <see cref="O:uLink.NetworkView.RPC<T>"/>, except that it is sent over an unreliable channel in uLink. This saves resources on the server and the client because there is no need to handle resends in uLink.
		/// </remarks>
		public void UnreliableRPC<T>(string rpcName, RPCMode mode, T arg) { RPC(_rpcFlags | NetworkFlags.Unreliable, rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends an unreliable RPC to the specified <see cref="uLink.NetworkPlayer"/>.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Works exactly like <see cref="O:uLink.NetworkView.RPC"/>, except that it is sent over an unreliable channel in uLink. This saves resources on the server and the client because there is no need to handle resends in uLink.
		/// </remarks>
		public void UnreliableRPC(string rpcName, NetworkPlayer target, params object[] args) { RPC(_rpcFlags | NetworkFlags.Unreliable, rpcName, target, args); }

		/// <summary>
		/// Sends an unreliable RPC to the specified <see cref="uLink.NetworkPlayer"/>.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// Works exactly like <see cref="O:uLink.NetworkView.RPC<T>"/>, except that it is sent over an unreliable channel in uLink. This saves resources on the server and the client because there is no need to handle resends in uLink.
		/// </remarks>
		public void UnreliableRPC<T>(string rpcName, NetworkPlayer target, T arg) { RPC(_rpcFlags | NetworkFlags.Unreliable, rpcName, target, (object)arg); }

		/// <summary>
		/// Sends an unreliable RPC to several network players.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Works exactly like <see cref="O:uLink.NetworkView.RPC"/>, except that it is sent over an unreliable channel in uLink. This saves resources on the server and the client because there is no need to handle resends in uLink.
		/// </remarks>
		public void UnreliableRPC(string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { RPC(_rpcFlags | NetworkFlags.Unreliable, rpcName, targets, args); }

		/// <summary>
		/// Sends an unreliable RPC to the several <see cref="uLink.NetworkPlayer"/>s.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// Works exactly like <see cref="O:uLink.NetworkView.RPC<T>"/>, except that it is sent over an unreliable channel in uLink. This saves resources on the server and the client because there is no need to handle resends in uLink.
		/// </remarks>
		public void UnreliableRPC<T>(string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { RPC(_rpcFlags | NetworkFlags.Unreliable, rpcName, targets, (object)arg); }

		/// <summary>
		/// Sends an RPC without encryption.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// This method makes it possible
		/// to send unencrypted RPCs even when security has been turned on.
		/// </remarks>
		public void UnencryptedRPC(string rpcName, RPCMode mode, params object[] args) { RPC(_rpcFlags | NetworkFlags.Unencrypted, rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC without encryption.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// This method makes it possible
		/// to send unencrypted RPCs even when security has been turned on.
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// </remarks>
		public void UnencryptedRPC<T>(string rpcName, RPCMode mode, T arg) { RPC(_rpcFlags | NetworkFlags.Unencrypted, rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends an RPC the specified <see cref="uLink.NetworkPlayer"/> without encryption.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// This method makes it possible
		/// to send unencrypted RPCs even when security has been turned on.
		/// </remarks> 
		public void UnencryptedRPC(string rpcName, NetworkPlayer target, params object[] args) { RPC(_rpcFlags | NetworkFlags.Unencrypted, rpcName, target, args); }

		/// <summary>
		/// Sends an RPC the specified <see cref="uLink.NetworkPlayer"/> without encryption.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// This method makes it possible
		/// to send unencrypted RPCs even when security has been turned on.
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// </remarks> 
		public void UnencryptedRPC<T>(string rpcName, NetworkPlayer target, T arg) { RPC(_rpcFlags | NetworkFlags.Unencrypted, rpcName, target, (object)arg); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers without encryption.
		/// </summary>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args"></param>
		/// <remarks>
		/// This method makes it possible
		/// to send unencrypted RPCs even when security has been turned on.
		/// </remarks> 
		public void UnencryptedRPC(string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { RPC(_rpcFlags | NetworkFlags.Unencrypted, rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers without encryption.
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args"></param>	
		/// <remarks>
		/// This method makes it possible
		/// to send unencrypted RPCs even when security has been turned on.
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// </remarks> 
		public void UnencryptedRPC<T>(string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { RPC(_rpcFlags | NetworkFlags.Unencrypted, rpcName, targets, (object)arg); }

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script (parameter type should be the type of one of your scripts). 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(string rpcName, RPCMode mode, params object[] args)"/>
		public void RPC(Type type, string rpcName, RPCMode mode, params object[] args) { RPC(type.Name + ':' + rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type)
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(System.Type, System.String, uLink.RPCMode, System.Object[])"/>
		/// </remarks> 
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(Type type, string rpcName, RPCMode mode, T arg) { RPC(type.Name + ':' + rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/> and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script (parameter type should be the type of one of your scripts). 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode,System.Object[])"/>
		public void RPC(Type type, string rpcName, NetworkPlayer target, params object[] args) { RPC(type.Name + ':' + rpcName, target, args); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/> and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(System.Type, System.String, uLink.NetworkPlayer,System.Oject[])"/>
		/// </remarks> 
		/// <seealso cref="RPC(System.String, uLink.RPCMode,System.Object[])"/>
		public void RPC<T>(Type type, string rpcName, NetworkPlayer target, T arg) { RPC(type.Name + ':' + rpcName, target, (object)arg); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script (parameter type should be the type of one of your scripts). 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(Type type, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { RPC(type.Name + ':' + rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information <see cref="RPC(System.Type, System.String, IEnumerable<NetworkPlayer>, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(string rpcName, RPCMode mode, params object[] args)"/>
		public void RPC<T>(Type type, string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { RPC(type.Name + ':' + rpcName, targets, (object)arg); }

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type).
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script (parameter type should be the type of one of your scripts). 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(Type type, NetworkFlags flags, string rpcName, RPCMode mode, params object[] args) { RPC(flags, type.Name + ':' + rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified Type (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(Type, uLink.NetworkFlags, System.String, uLink.RPCMode, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(Type type, NetworkFlags flags, string rpcName, RPCMode mode, T arg) { RPC(flags, type.Name + ':' + rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/> and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script (parameter type should be the type of one of your scripts). 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(Type type, NetworkFlags flags, string rpcName, NetworkPlayer target, params object[] args) { RPC(flags, type.Name + ':' + rpcName, target, args); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/> and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send	
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can not understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(System.Type, uLink.NetworkFlags, System.String, uLink.NetworkPlayer, System.Object[])"/>
		/// </remarks> 
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(Type type, NetworkFlags flags, string rpcName, NetworkPlayer target, T arg) { RPC(flags, type.Name + ':' + rpcName, target, (object)arg); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script (parameter type should be the type of one of your scripts). 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(Type type, NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { RPC(flags, type.Name + ':' + rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and it is also restricted to the specified Type (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(System.Type, uLink.NetworkFlags, System.String, IEnumerable<NetworkPlayer>, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(Type type, NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { RPC(flags, type.Name + ':' + rpcName, targets, (object)arg); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script. 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(UnityEngine.MonoBehaviour type, string rpcName, RPCMode mode, params object[] args) { RPC(type.GetType(), rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(UnityEngine.MonoBehaviour, System.String, uLink.RPCMode, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(UnityEngine.MonoBehaviour type, string rpcName, RPCMode mode, T arg) { RPC(type.GetType(), rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/> restricted to the specified MonoBehavior (parameter type).
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script. 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(UnityEngine.MonoBehaviour type, string rpcName, NetworkPlayer target, params object[] args) { RPC(type.GetType(), rpcName, target, args); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/> restricted to the specified MonoBehavior (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(UnityEngine.MonoBehaviour, System.string, uLink.NetworkPlayer, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(UnityEngine.MonoBehaviour type, string rpcName, NetworkPlayer target, T arg) { RPC(type.GetType(), rpcName, target, (object)arg); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and also restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <seealso cref="RPC(System.string, uLink.RPCMode, System.Object[])"/>
		public void RPC(UnityEngine.MonoBehaviour type, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { RPC(type.GetType(), rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and also restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(UnityEngine.MonoBehaviour, System.String, IEnumerable<NetworkPlayer>, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(UnityEngine.MonoBehaviour type, string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { RPC(type.GetType(), rpcName, targets, (object)arg); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script. 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, RPCMode mode, params object[] args) { RPC(type.GetType(), flags, rpcName, mode, args); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="mode">The receiver is dictated by this parameter. If
		/// this RPC should be buffered is also dictated by the 
		/// <see cref="uLink.RPCMode"/>.</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, RPCMode mode, params object[] args)"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, RPCMode mode, T arg) { RPC(type.GetType(), flags, rpcName, mode, (object)arg); }

		/// <summary>
		/// Sends an RPC restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="args">The arguments that the remote receiver will send
		/// to the RPC method executed there (as argument).</param>
		/// <remarks>
		/// Use this RPC method to send an RPC to a specific script. 
		/// Use this method if there are two scripts including an RPC with the exact same name. When there are
		/// two scripts with an RPC with the exact same name, it is ambiguous which script should receive the RPC.
		/// By specifying the type, this ambiguity is avoided.
		/// </remarks> 
		/// <example>
		/// These two code lines shows the two common ways of sending an RPC and restrict the receiver to only a specific type of script.
		/// <code>
		/// uLink.NetworkView.Get(this).RPC(typeof(MyEpicScript), "MyRPC", ...);
		/// uLink.NetworkView.Get(this).RPC(this, "MyRPC", ...);
		/// </code>
		/// </example>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, NetworkPlayer target, params object[] args) { RPC(type.GetType(), flags, rpcName, target, args); }

		/// <summary>
		/// Sends an RPC to the specified <see cref="uLink.NetworkPlayer"/> and it also restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="target">The target can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer)</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// For more information see <see cref="RPC(UnityEngine.MonoBehaviour, uLink.NetworkFlags, System.String, uLink.NetworkPlayer, System.Object[])"/>
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, NetworkPlayer target, T arg) { RPC(type.GetType(), flags, rpcName, target, (object)arg); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and also restricted to the specified MonoBehavior (parameter type)
		/// </summary>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="args"></param>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args) { RPC(type.GetType(), flags, rpcName, targets, args); }

		/// <summary>
		/// Sends an RPC to several NetworkPlayers and also restricted to the specified MonoBehavior (parameter type).
		/// </summary>
		/// <typeparam name="T">Type of the argument you want to pass to the RPC</typeparam>
		/// <param name="type">The only script class that will receive this RPC</param>
		/// <param name="rpcName">Name of the RPC.</param>
		/// <param name="flags">Use this to control exactly how uLink will
		/// handle this RPC.</param>
		/// <param name="targets">An IEnumerable which contains all of the players that this RPC should sends to.
		/// The targets can be any player or the <see cref="uLink.NetworkPlayer.server"/> (the server is a predefined NetworkPlayer).</param>
		/// <param name="arg">The arguments from type <c>T</c> that the remote receiver will send
		/// For more information see <see cref="RPC(UnityEngine.MonoBehaviour, uLink.NetworkFlags, System.String, IEnumerable<uLink.NetworkPlayer>, System.Object[])"/>
		/// <remarks>
		/// If you want to send only one argument as an array, you should use this generic RPC overload, because
		/// the reflection functionality in .Net can only understand the method signature correctly in this way.
		/// </remarks>
		/// <seealso cref="RPC(System.String, uLink.RPCMode, System.Object[])"/>
		public void RPC<T>(UnityEngine.MonoBehaviour type, NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, T arg) { RPC(type.GetType(), flags, rpcName, targets, (object)arg); }

		/// <summary>
		/// Removed all custom buffered RPCs for this network view.
		/// </summary>
		/// <remarks>
		/// If any buffered RPCs has been sent via this network view, they can be removed using this method.
		/// </remarks>
		public void RemoveRPCs() { _network.RemoveRPCs(viewID); }

		/// <summary>
		/// Returns this network aware gameobject's place in hierarchy and its <see cref="viewID"/> in string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Concat(ToHierarchyString(), " (", viewID.ToString(), ")");
		}

		/// <summary>
		/// Returns name of the prefab used to instantiate this local object
		/// or name of the object if the prefab name doesn't exist.
		/// </summary>
		/// <returns></returns>
		public string ToPrefabString()
		{
			return String.IsNullOrEmpty(localPrefab) ? this.GetName() : localPrefab;
		}

		/// <summary>
		/// Returns the NetworkView instance that has the specified viewID.
		/// </summary>
		/// <returns>Returns a NetworkView instance or <c>null</c> if nothing was found</returns>
		public static NetworkView Find(NetworkViewID viewID) { return Network._singleton._FindNetworkView(viewID) as NetworkView; }

		/// <summary>
		/// Returns an array of NetworkView instances, that all have the specified owner.
		/// </summary>
		/// <returns>Returns an array of NetworkView instance. If nothing was found, the array will be empty. Never returns null.</returns>
		public static NetworkView[] FindByOwner(NetworkPlayer owner)
		{
			var bases = Network._singleton._FindNetworkViewsByOwner(owner);
			var deriveds = new NetworkView[bases.Length];

			Array.Copy(bases, deriveds, bases.Length);
			return deriveds;
		}

		/// <summary>
		/// Determines if any NetworkView has the specified owner.
		/// </summary>
		/// <returns>Returns true if there are any enabled NetworkView instances owned by specified player. Returns false if no such NetworkView exists.</returns>
		public static bool AnyByOwner(NetworkPlayer owner)
		{
			return Network._singleton._DoesOwnerHaveAnyNetworkViews(owner);
		}

		/// <summary>
		/// Determines if any NetworkView is part of the specified group.
		/// </summary>
		/// <returns>Returns true if there are any enabled NetworkView instances inside the specified group. Returns false if no such NetworkView exists.</returns>
		public static bool AnyInGroup(NetworkGroup group)
		{
			return Network._singleton._DoesGroupHaveAnyNetworkViews(group);
		}

		/// <summary>
		/// Returns an array of NetworkView instances, that all belong to specified group.
		/// </summary>
		/// <returns>Returns an array of NetworkView instance. If nothing was found, the array length is 0. Never returns null.</returns>
		public static NetworkView[] FindInGroup(NetworkGroup group)
		{
			var bases = Network._singleton._FindNetworkViewsInGroup(group);
			var deriveds = new NetworkView[bases.Length];

			Array.Copy(bases, deriveds, bases.Length);
			return deriveds;
		}

		/// <summary>
		/// Gets the NetworkView instance from the specified GameObject.
		/// </summary>
		/// <param name="gameObject">Read about the gameobject class in the Unity documentation.</param>
		public static NetworkView Get(GameObject gameObject) { return gameObject.GetComponent<NetworkView>(); }

		/// <summary>
		/// Gets the NetworkView instance from the gameObject which specified Component is attached to.
		/// </summary>
		/// <param name="component">Read about the Component class in the Unity documentation.</param>
		public static NetworkView Get(Component component) { return component.GetComponent<NetworkView>(); }
		
#if UNITY_DOC
		/// <summary>
		/// Message callback: Called on both sending server and receiving server when a <see cref="O:Handover"/> was 
		/// invoked.
		/// </summary>
		/// <remarks>
		/// When a character should be transferred from one server to another, call the method <see cref="O:Handover"/> 
		/// on the sending server.
		/// Place the code for serializing the character's server side state in the uLink_OnHandoverNetworkView 
		/// callback method. It will be called automatically right after the call to <see cref="O:Handover"/>.
		/// uLink will send the serialized state of the character over the p2p link between the two servers. 
		/// <para>
		/// Make sure the p2p link between the two servers is working properly before calling <see cref="O:Handover"/>. 
		/// uLink_OnHandoverNetworkView works very similar to 
		/// <see cref="uLink.Network.uLink_OnSerializeNetworkView(uLink.BitStream,uLink.NetworkMessageInfo)"/> 
		/// because both are used 
		/// as the place to put code for serializing and deserializing the state of an object.
		/// The same programming skills can be used to code this callback as coding 
		/// <see cref="uLink.Network.uLink_OnSerializeNetworkView(uLink.BitStream,uLink.NetworkMessageInfo)"/>.
		/// </para>
		/// <para>The implementation of this callback should serialize all important server side state that will be needed 
		/// in the new game server. Be aware that this is usually more state than is needed for state synchronization, 
		/// since the server side object will be moved completely from the old server to the new.
		/// The object will then be deleted from the old server and recreated on the new server. This is done automatically by uLink.
		/// </para>
		/// <para>
		/// Similar to when performing a network instantiate, the 
		/// <see cref="uLink.Network.uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo)"/> 
		/// callback will be called on 
		/// the object on the new server. The networkView.initialData member of the NetworkMessageInfo argument will contain the 
		/// same data as in the original instantiation on the first server did include.
		/// </para>
		/// <para>
		/// After this, uLink_OnHandoverNetworkView will be called on the new object. It is passed the stream 
		/// containing the object, and your code to deserialize the saved state will be executed. 
		/// </para>
		/// <para>
		/// If the client doesn't succeed to reconnect to the new server, the new server must do the cleanup for the
		/// client. Do not forget this cleanup, otherwise there will be frozen avatars waiting for ever for the correct player 
		/// to connect. See <see cref="uLink.NetworkP2P.uLink_OnHandoverTimeout"/> 
		/// </para>
		/// </remarks>
		public void uLink_OnHandoverNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info) { }

#endif
	}
}

#endif