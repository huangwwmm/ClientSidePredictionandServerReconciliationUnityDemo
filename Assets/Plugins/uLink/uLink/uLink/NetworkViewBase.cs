#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12165 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-24 12:31:56 +0200 (Thu, 24 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Reflection;
using System.Collections.Generic;

#if UNITY_BUILD
using UnityEngine;
#endif

// TODO: optimize RPC writing by caching codecs for based on name

namespace uLink
{
	/// <summary>
	/// Abstract base class for the class <see cref="uLink.NetworkView"/>.
	/// </summary>
	public abstract class NetworkViewBase
#if UNITY_BUILD
		: MonoBehaviour
#endif
	{
#if UNITY_BUILD
		internal NetworkBase _network { get { return Network._singleton; } }
#elif TEST_BUILD
		internal readonly NetworkBase _network;

		public bool enabled { get { return true; } }
#endif

		[NonSerialized]
		internal NetworkViewData _data = new NetworkViewData(NetworkViewID.unassigned, NetworkPlayer.unassigned, NetworkGroup.unassigned, NetworkAuthFlags.None, false, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, null);

		/// <summary>
		/// The prefab name used to find the proxy prefab.
		/// </summary>
		/// <remarks>
		/// This is only used during handover.
		/// <see cref="O:uLink.Network.Instantiate"/> automatically assigns this value during instantiation, so you don't have to worry about it.
		/// But if you have instantiated the object yourself and want to be able to handover it you will have to set this to the prefab name which uLink can use to find the proxy prefab.
		/// </remarks>
		public string proxyPrefab
		{
			get { return _data.proxyPrefab; }
			set { _data.proxyPrefab = value; }
		}

		/// <summary>
		/// The prefab name used to find the owner prefab.
		/// </summary>
		/// <remarks>
		/// This is only used during handover.
		/// <see cref="O:uLink.Network.Instantiate"/> automatically assigns this value during instantiation, so you don't have to worry about it.
		/// But if you have instantiated the object yourself and want to be able to handover it you will have to set this to the prefab name which uLink can use to find the owner prefab.
		/// </remarks>
		public string ownerPrefab
		{
			get { return _data.ownerPrefab; }
			set { _data.ownerPrefab = value; }
		}

		[Obsolete("NetworkView.creatorPrefab is deprecated, please use NetworkView.serverPrefab instead")]
		public string creatorPrefab
		{
			get { return _data.serverPrefab; }
			set { _data.serverPrefab = value; }
		}

		/// <summary>
		/// The prefab name used to find the server prefab.
		/// </summary>
		/// <remarks>
		/// This is only used during handover.
		/// <see cref="O:uLink.Network.Instantiate"/> automatically assigns this value during instantiation, so you don't have to worry about it.
		/// But if you have instantiated the object yourself and want to be able to handover it you will have to set this to the prefab name which uLink can use to find the authority prefab.
		/// </remarks>
		public string serverPrefab
		{
			get { return _data.serverPrefab; }
			set { _data.serverPrefab = value; }
		}

		/// <summary>
		/// The prefab name used to find the cell server auth prefab.
		/// </summary>
		/// <remarks>
		/// This is only used during handover.
		/// <see cref="O:uLink.Network.Instantiate"/> automatically assigns this value during instantiation, so you don't have to worry about it.
		/// But if you have instantiated the object yourself and want to be able to handover it you will have to set this to the prefab name which uLink can use to find the authority prefab.
		/// <para>
		/// This is used on Pikko server for the cell server which has authority over the networked object.
		/// </para>
		/// </remarks>
		public string cellAuthPrefab
		{
			get { return _data.cellAuthPrefab; }
			set { _data.cellAuthPrefab = value; }
		}

		/// <summary>
		/// The prefab name used to find the cell server proxy prefab.
		/// </summary>
		/// <remarks>
		/// This is only used during handover.
		/// <see cref="O:uLink.Network.Instantiate"/> automatically assigns this value during instantiation, so you don't have to worry about it.
		/// But if you have instantiated the object yourself and want to be able to handover it you will have to set this to the prefab name which uLink can use to find the authority prefab.
		/// <para>
		/// This is used on Pikko server for the cell servers which have the proxy role for the networked object.
		/// </para>
		/// </remarks>
		public string cellProxyPrefab
		{
			get { return _data.cellProxyPrefab; }
			set { _data.cellProxyPrefab = value; }
		}

		// TODO: Reactivate this for Reliable Delta Compression. /Staffan
		// private ReliableDeltaCompressor _reliableDeltaCompressor = new ReliableDeltaCompressor();

#if UNITY_BUILD
		[SerializeField]
		[Obfuscation(Feature = "renaming")]
		// WARNING: Unity 5.x (or later) will automagically assign a "dead" GameObject (which behaves like it's null)
		// to this variable because it's serialized (and Unity is trying to catch "unassigned references").
		// So the variable won't actually be null and therefore we can't use .IsNull() or similar extensions!
#endif
		internal NetworkViewBase _parent;

#if UNITY_BUILD
		[SerializeField]
		[Obfuscation(Feature = "renaming")]
#endif
		protected int _childIndex = -1;

#if UNITY_BUILD
		[SerializeField]
		[Obfuscation(Feature = "renaming")]
#endif
		protected NetworkViewBase[] _children = new NetworkViewBase[0];

		/// <summary>
		/// The kind of state synchronization used for this object.
		/// </summary>
		/// <value>Default value is <see cref="uLink.NetworkStateSynchronization.Unreliable"/>.</value>
		/// <remarks>Read more in the uLink manual under the State Synchronization section. 
		/// The <see cref="uLink.Network.sendRate"/> can be used to configure the sending frequency 
		/// for state synchronization.</remarks>
		[SerializeField]
		public NetworkStateSynchronization stateSynchronization = NetworkStateSynchronization.Unreliable;

		/// <summary>
		/// Specifies the possibility to encrypt RPC traffic or statesync traffic or both.
		/// </summary>
		/// <value>Default value is <see cref="uLink.NetworkSecurable.Both"/>.</value>
		/// <remarks>Read the manual section on secure communication for more information.</remarks>
		[SerializeField]
		public NetworkSecurable securable = NetworkSecurable.Both;

		[NonSerialized]
		internal Vector3 _lastTrackPosition;
		[NonSerialized]
		internal double _lastProxyTimestamp;
		[NonSerialized]
		internal double _lastOwnerTimestamp;
		[NonSerialized]
		internal double _lastCellProxyTimestamp;

		[NonSerialized]
		private readonly ParameterWriterCache _rpcWriter = new ParameterWriterCache(false);

		[NonSerialized]
		private HashSet<NetworkPlayer> _scopeCulling = null;
		
		[NonSerialized]
		public bool destroyOnFinalDisconnect = false;

		[NonSerialized]
		internal int _expectedProxyStateDeltaCompressedSequenceNr;
		[NonSerialized]
		internal int _expectedOwnerStateDeltaCompressedSequenceNr;
		[NonSerialized]
		internal BitStream _prevProxyStateSerialization;
		[NonSerialized]
		internal BitStream _nextProxyStateSerialization;
		[NonSerialized]
		internal BitStream _prevOwnerStateSerialization;
		[NonSerialized]
		internal BitStream _nextOwnerStateSerialization;

#if TEST_BUILD
		internal NetworkViewBase(NetworkBase network)
		{
			_network = network;
		}
#endif

		/// <summary>
		/// Gets the owner for this network aware object.
		/// </summary>
		/// <remarks>Read more in the uLink manual about the three roles for network aware objects.</remarks>
		public NetworkPlayer owner { get { return _data.owner.isServer && isCellAuthority ? _network._localPlayer : _data.owner; } }

		/// <summary>
		/// Gets a value indicating whether this instance is mine (and then I have the owner role).
		/// </summary>
		/// <value><c>true</c> if this instance is mine; otherwise, <c>false</c>.</value>
		/// <remarks>Read more in the uLink manual about the three roles for objects created 
		/// with <see cref="O:uLink.Network.Instantiate"/>.</remarks>
		public bool isMine { get { return isOwner; } }

		/// <summary>
		/// Gets a value indicating whether the current NetworkPlayer is the owner this object. Same as <see cref="uLink.NetworkViewBase.isMine"/>.
		/// </summary>
		/// <value><c>true</c> if this instance is mine; otherwise, <c>false</c>.</value>
		/// <remarks>Read more in the uLink manual about the three roles for objects created 
		/// with <see cref="O:uLink.Network.Instantiate"/>.</remarks>
		public bool isOwner
		{
			get
			{
				return
					_data.viewID != NetworkViewID.unassigned &&
					_data.owner != NetworkPlayer.unassigned &&
					owner == _network._localPlayer;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance of the network aware object is a normal uLink proxy.
		/// </summary>
		/// <remarks>
		/// This value being true implies the object does not have <see cref="O:uLink.NetworkViewBase.isAuthority"/> set to true.
		/// </remarks>
		public bool isProxy
		{
			get
			{
				return
					_data.viewID != NetworkViewID.unassigned &&
					_data.owner != NetworkPlayer.unassigned &&
					owner != _network._localPlayer && (!_network.isAuthoritativeServer || !_network.isServerOrCellServer);
			}
		}

		/// <summary>
		/// Indicates whether the current NetworkPlayer is the cell server authority of this object (in a PikkoServer) system.
		/// </summary>
		/// <remarks>
		/// A cell server authority is allowed to and responsible for mutating the state of this object.
		/// </remarks>
		public bool isCellAuthority
		{
			get
			{
				return
					_data.viewID != NetworkViewID.unassigned &&
					_data.owner != NetworkPlayer.unassigned &&
					_network.isCellServer && !isInstantiatedRemotely;
			}
		}

		[Obsolete("NetworkView.hasCellAuthority is deprecated, please use NetworkView.isCellAuthority instead")]
		public bool hasCellAuthority { get { return isCellAuthority; } }

		/// <summary>
		/// Indicates whether this object is a cell server proxy for the current NetworkPlayer (in a PikkoServer) system.
		/// </summary>
		/// <remarks>
		/// A cell server proxy provides a representation of the network object on other cell servers than the one holding the authority.
		/// The object's state should only be mutated by the cell server authority, and mirrored on this proxy to the extent desireable.
		/// This value being true implies the object does not have <see cref="O:uLink.NetworkViewBase.isCellAuthority"/> set to true.
		/// </remarks>
		public bool isCellProxy
		{
			get
			{
				return
					_data.viewID != NetworkViewID.unassigned &&
					_data.owner != NetworkPlayer.unassigned &&
					_network.isCellServer && isInstantiatedRemotely;
			}
		}

		[Obsolete("NetworkView.creator is deprecated", true)]
		public NetworkPlayer creator { get { return NetworkPlayer.unassigned; } }

		[Obsolete("NetworkView.isCreator is deprecated", true)]
		public bool isCreator { get { return false; } }
	
		/// <summary>
		/// Gets the authoritative player over this object.
		/// </summary>
		/// <value><see cref="uLink.NetworkPlayer.server"/> if authoritative server is enabled; otherwise, same as <see cref="uLink.NetworkViewBase.owner"/>.</value>
		/// <remarks>Read more in the uLink manual about the three roles for network aware objects.</remarks>
		public NetworkPlayer authority { get { return _network.isAuthoritativeServer ? (isCellAuthority ? _network._localPlayer : NetworkPlayer.server) : owner; } }

		/// <summary>
		/// Gets a value indicating whether this instance of the network aware object has authority.
		/// </summary>
		/// <value><c>true</c> if authoritative server is enabled and the instance is on the server; otherwise, same as <see cref="uLink.NetworkViewBase.isOwner"/>.</value>
		public bool isAuthority { get { return _network.isAuthoritativeServer ? (_network.isServer || (_network.isCellServer && !isInstantiatedRemotely)) : isOwner; } }

		[Obsolete("NetworkView.hasAuthority is deprecated, please use NetworkView.isAuthority instead")]
		public bool hasAuthority { get { return isAuthority; } }

		/// <summary>
		/// Gets a prefab used to instantiate this local object.
		/// </summary>
		public string localPrefab
		{
			get { return _network.isServerOrCellServer ? (_network.isServer ? serverPrefab : isInstantiatedRemotely ? cellProxyPrefab : cellAuthPrefab) : (isOwner ? ownerPrefab : proxyPrefab); }
		}

		/// <summary>
		/// Gets or sets the <see cref="uLink.BitStream"/> for initial data for 
		/// this network aware object.
		/// </summary>
		/// <remarks>The initial data can be specified in the call to one of the
		/// <see cref="O:uLink.Network.Instantiate"/> methods. 
		/// After that, the initialData can be retrieved via this
		/// property. Never set this property manually unless you know what you
		/// are doing and you are working with custom allocation of viewIDs at
		/// runtime (<see cref="O:uLink.Network.AllocateViewID"/>).
		/// </remarks>
		public BitStream initialData
		{
			get { return _data.initialData; }
			set { _data.initialData = value; }
		}

		/// <summary> 
		/// Gets the child index for this NetworkView. (When multiple NetworkViews per game object is used)
		/// </summary>
		/// <remarks>
		/// Avoid using multiple NetworkViews for game objects instantiated with <see cref="O:uLink.Network.Instantiate"/>.
		/// Read more about multiple NetworkViews in the Network Views manual chapter.
		/// </remarks>
		public int childIndex { get { return _childIndex; } }

		/// <summary>
		/// Returns the number of NetworkViews which are children of the object which has this NetworkView attached.
		/// </summary>
		public int childCount { get { return _children.Length; } }

		/// <summary>
		/// The NetworkVIew which is the parent of this one.
		/// </summary>
		/// <remarks>GameObjects with NetworkViews can have childs which have network views as well.
		/// Then RPCs sent to the parent will be sent to them as well and they share their ViewID with their parent.
		/// </remarks>
		public NetworkViewBase parent { get { return _parent; } }

		/// <summary>
		/// The NetworkView which is the root of the hierarchy of this NetworkView.
		/// </summary>
		/// <remarks>If this is the root, the result will be equal to this</remarks>
		public NetworkViewBase root { get { return _parent ?? this; } }

		internal NetworkFlags _syncFlags
		{
			get
			{
				return NetworkFlags.TypeUnsafe | NetworkFlags.Unbuffered
					| ((securable & NetworkSecurable.OnlyStateSynchronization) == 0 ? NetworkFlags.Unencrypted : NetworkFlags.Normal)
					| ((stateSynchronization == NetworkStateSynchronization.Unreliable
					/* TODO: || stateSynchronization == NetworkStateSynchronization.UnreliableDeltaCompressed*/)
						? NetworkFlags.Unreliable : NetworkFlags.Normal);
			}
		}

		internal NetworkFlags _rpcFlags
		{
			get
			{
				return _network._rpcFlags | ((securable & NetworkSecurable.OnlyRPCs) == 0 ? NetworkFlags.Unencrypted : NetworkFlags.Normal);
			}
		}

		/// <summary>
		/// Gets the view ID for this network aware object.
		/// </summary>
		/// <remarks>
		/// The set-property is obsolete, use <see cref="O:uLink.NetworkViewBase.SetViewID"/> or <see cref="uLink.NetworkViewBase.SetManualViewID"/> instead.
		/// This is only for backward compatibility with Unity's built-in networking. The owner and creator will be set to NetworkPlayer.server which may not be desired.
		/// </remarks>
		public NetworkViewID viewID
		{
			get { return _data.viewID; }
			set
			{
				Log.Warning(NetworkLogFlags.NetworkView, "Set-property NetworkView.viewID is deprecated, please use method SetViewID instead");
				SetViewID(value, NetworkPlayer.server);
			}
		}

		/// <summary>
		/// The RPC communication group used by this NetworkView.
		/// </summary>
		/// <remarks>players which are not in this group, will not receive state syncs and RPCs from the NetworkView</remarks>
		public NetworkGroup group
		{
			get { return _data.group; }
			set { _network._ChangeGroup(this, value); }
		}

		/// <summary>
		/// Get or set a bitwise combination of flags that control various options such as handover enabling.
		/// Only possible to use if the local player is the authority of this NetworkView.
		/// See the <see cref="uLink.NetworkAuthFlags"/> enum for more info.
		/// </summary>
		public NetworkAuthFlags authFlags
		{
			get
			{
				if(!(isAuthority)){Utility.Exception( "caller must have authority over the object");}
				return _data.authFlags;
			}
			set
			{
				if(!(isAuthority)){Utility.Exception( "caller must have authority over the object");}

				_data.authFlags = value;
				_network._ChangeAuthFlags(_data.viewID, _data.authFlags, position);
			}
		}

		/// <summary>
		/// Indicates if the NetworkView instantiated remotely or not.
		/// If we are the caller of Instantiaate or allocator of this NetworkView, it will return false.
		/// </summary>
		public bool isInstantiatedRemotely { get { return _data.isInstantiatedRemotely; } }

		/// <summary>
		/// Gets the position for this Network View.
		/// </summary>
		public abstract Vector3 position { get; set; }
		/// <summary>
		/// Gets the rotation for this Network View.
		/// </summary>
		public abstract Quaternion rotation { get; set; }

		protected abstract bool hasSerializeProxy { get; }
		protected abstract bool hasSerializeOwner { get; }
		protected abstract bool hasSerializeHandover { get; }
		protected abstract bool hasSerializeCellProxy { get; }

		internal bool _hasSerializeProxy
		{
			get
			{
				if (hasSerializeProxy) return true;

				foreach (var child in _children)
				{
					if (child.stateSynchronization != NetworkStateSynchronization.Off && child.hasSerializeProxy) return true;
				}

				return false;
			}
		}

		internal bool _hasSerializeOwner
		{
			get
			{
				if (hasSerializeOwner) return true;

				foreach (var child in _children)
				{
					if (child.stateSynchronization != NetworkStateSynchronization.Off && child.hasSerializeOwner) return true;
				}

				return false;
			}
		}

		internal bool _hasSerializeHandover
		{
			get
			{
				if (hasSerializeHandover) return true;

				foreach (var child in _children)
				{
					if (child.hasSerializeHandover) return true;
				}

				return false;
			}
		}

		internal bool _hasSerializeCellProxy
		{
			get
			{
				if (hasSerializeCellProxy) return true;

				foreach (var child in _children)
				{
					if (child.stateSynchronization != NetworkStateSynchronization.Off && child.hasSerializeCellProxy) return true;
				}

				return false;
			}
		}

		internal void _ClearStateSyncData()
		{
			_lastTrackPosition = new Vector3(0, 0, 0);

			_lastProxyTimestamp = 0;
			_lastOwnerTimestamp = 0;
			_lastCellProxyTimestamp = 0;

			_expectedProxyStateDeltaCompressedSequenceNr = -1;
			_expectedOwnerStateDeltaCompressedSequenceNr = -1;
			// TODO: variables for CellProxy?

			_prevProxyStateSerialization = null;
			_nextProxyStateSerialization = null;
			_prevOwnerStateSerialization = null;
			_nextOwnerStateSerialization = null;
			// TODO: variables for CellProxy?
		}

		[Obsolete("NetworkView.AssignManualViewID is deprecated, please use NetworkView.SetManualViewID instead")]
		public void AssignManualViewID(int manualID)
		{
			SetManualViewID(manualID);
		}

		/// <summary>
		/// Assigns the manual view ID for a GameObject.
		/// </summary>
		/// <remarks>This method can be used if the game programmer did not set the manualViewID for a 
		/// Game Object. The normal situation is that the game programmer places a GameObject in the Unity 
		/// editor's hierarchy view. After that, the programmer adds the component uLink.NetworkView and assigns 
		/// a manual view ID for that component. The programmers chooses a unique number like 1, 2, 3, etc.
		/// The maximum number is dictated by <see cref="uLink.Network.maxManualViewIDs"/>.
		/// This function performs exactly the same thing, it makes it possible to write a script that sets 
		/// the manual view IDs for many GameObjects in a scene. 
		/// In addition: Several overloaded versions of the method <see cref="O:uLink.Network.Instantiate"/>
		/// do the assignement of view IDs automatically. uLink uses viewIDs above the limit 
		/// <see cref="uLink.Network.maxManualViewIDs"/> for these automatic assignments. In some special situations 
		/// it can be handy to allocate a manual viewID first and then send this viewID as one of the arguments to 
		/// the uLink.Network.Instantiate method that accepts a viewID as one of the arguments.
		/// </remarks>
		public void SetManualViewID(int manualID)
		{
			if(!(manualID > 0)){Utility.Exception( "Manual ViewID must be positive");}

			SetViewID(new NetworkViewID(manualID), NetworkPlayer.server);
		}

		/// <summary>
		/// Sets the initial data for this network aware object.
		/// </summary>
		/// <remarks>Only use this function if you are working with custom allocation of viewIDs and needs to set
		/// the initial data for a network aware object. Normally, the initial data can be specified in the call to 
		/// one of the 
		/// <see cref="O:uLink.Network.Instantiate"/> 
		/// functions. Never use this function unless 
		/// you know what you are doing and you are working with custom allocation of viewIDs at runtime 
		/// (<see cref="O:uLink.Network.AllocateViewID"/>).
		/// </remarks>
		public void SetInitialData(params object[] args)
		{
			initialData = new BitStream(false);
			ParameterWriter.WriteUnprepared(initialData, args);
		}

		[Obsolete("NetworkView.Assign is deprecated, please use NetworkView.SetViewID instead")]
		public void Assign(NetworkViewID viewID, NetworkPlayer owner, NetworkPlayer creator)
		{
			SetViewID(viewID, owner);
		}

		[Obsolete("NetworkView.SetViewID argument creator is deprecated, please call SetViewID without it.")]
		public bool SetViewID(NetworkViewID viewID, NetworkPlayer owner, NetworkPlayer creator)
		{
			return SetViewID(viewID, owner);
		}

		/// <summary>
		/// Assigns the specified viewID to this NetworkView.
		/// </summary>
		/// <param name="viewID">The viewID you have created via a call to <see cref="O:uLink.Network.AllocateViewID"/></param>
		/// <param name="owner">Will become the owner of this object</param>
		/// <remarks>
		/// Before calling this method, use <see cref="O:uLink.Network.AllocateViewID"/> to get a new allocated and thus 
		/// usable viewID. Use this method on the server and on all clients 
		/// to make them all treat this viewID in the same way.
		/// </remarks>
		public bool SetViewID(NetworkViewID viewID, NetworkPlayer owner)
		{
			return SetViewID(viewID, owner, NetworkGroup.unassigned);
		}

		/// <summary>
		/// Assigns the specified viewID to this NetworkView.
		/// </summary>
		/// <param name="viewID">The viewID you have created via a call to <see cref="O:uLink.Network.AllocateViewID"/></param>
		/// <param name="owner">Will become the owner of this object</param>
		/// <param name="group">Will become the group of this object. Default value is unassigned.</param>
		/// <remarks>
		/// Before calling this method, use <see cref="O:uLink.Network.AllocateViewID"/> to get a new allocated and thus 
		/// usable viewID. Use this method on the server and on all clients 
		/// to make them all treat this viewID in the same way.
		/// </remarks>
		public bool SetViewID(NetworkViewID viewID, NetworkPlayer owner, NetworkGroup group)
		{
			return SetViewID(viewID, owner, group, false);
		}

		/// <summary>
		/// Assigns the specified viewID to this NetworkView.
		/// </summary>
		/// <param name="viewID">The viewID you have created via a call to <see cref="O:uLink.Network.AllocateViewID"/></param>
		/// <param name="owner">Will become the owner of this object</param>
		/// <param name="group">Will become the group of this object. Default value is unassigned.</param>
		/// <param name="isInstantiatedRemotely">Will let uLink know this object has been instantiated remotely. Default value is false.</param>
		/// <remarks>
		/// Before calling this method, use <see cref="O:uLink.Network.AllocateViewID"/> to get a new allocated and thus 
		/// usable viewID. Use this method on the server and on all clients 
		/// to make them all treat this viewID in the same way.
		/// </remarks>
		public bool SetViewID(NetworkViewID viewID, NetworkPlayer owner, NetworkGroup group, bool isInstantiatedRemotely)
		{
			if (_parent.IsNotNullOrUnassigned()) return _parent.SetViewID(viewID, owner, group, isInstantiatedRemotely);

			Log.Debug(NetworkLogFlags.NetworkView, "Assigning ", this, " to ", viewID, ", owner ", owner, ", group ", group, ", isInstantiatedRemotely", isInstantiatedRemotely);

			_network.RemoveNetworkView(this);

			_data.viewID = viewID;
			_data.owner = owner;
			_data.group = group;
			_data.isInstantiatedRemotely = isInstantiatedRemotely;

			if (!_network.AddNetworkView(this))
			{
				SetUnassignedViewID();
				return false;
			}

			// TODO: need to make sure this doesn't cause any weird issues/behavior in all possible scenarios.
			destroyOnFinalDisconnect = viewID.isAllocated;

			foreach (var child in _children)
			{
				child._data.viewID = viewID;
				child._data.owner = owner;
				child._data.group = group;
				child._data.isInstantiatedRemotely = isInstantiatedRemotely;
			}

			return true;
		}

		/// <summary>
		/// Assigns the specified viewID to this NetworkView.
		/// </summary>
		/// <param name="viewID">The view ID you have created via a call to <see cref="O:uLink.Network.AllocateViewID"/></param>
		/// <param name="info">The sender of <see cref="uLink.NetworkMessageInfo"/>
		/// will become the owner and the creator for this object</param>
		/// <remarks>
		/// Before calling this method, use <see cref="O:uLink.Network.AllocateViewID"/> to get a new allocated and thus 
		/// usable viewID. Use this method on the server and on all clients 
		/// to make them all treat this viewID in the same way.
		/// </remarks>
		/// <example>
		/// Can be used when instantiating NPCs in MMO games.
		/// This method is usually called in all clients after receiving the viewID from the server via some 
		/// RPC call. Then the clients will become aware that the server has the owner and creater role for 
		/// this NPC Game Object.
		/// </example>
		public bool SetViewID(NetworkViewID viewID, NetworkMessageInfo info)
		{
			return SetViewID(viewID, info.sender);
		}

		/// <summary>
		/// Assigns the specified viewID to this NetworkView.
		/// </summary>
		/// <param name="viewID">The view ID you have created via a call to <see cref="O:uLink.Network.AllocateViewID"/></param>
		/// <param name="info">The sender of <see cref="uLink.NetworkMessageInfo"/>
		/// will become the owner and the creator for this object</param>
		/// <param name="group">The group which this NetworkView will belong to</param>
		/// <remarks>
		/// Before calling this method, use <see cref="O:uLink.Network.AllocateViewID"/> to get a new allocated and thus 
		/// usable viewID. Use this method on the server and on all clients 
		/// to make them all treat this viewID in the same way.
		/// </remarks>
		/// <example>
		/// Can be used when instantiating NPCs in an MMO game.
		/// This method is usually called in all clients after receiving the viewID from the server via some 
		/// RPC call. Then the clients will become aware that the server has the owner and creater role for 
		/// this NPC Game Object.
		/// </example>
		public bool SetViewID(NetworkViewID viewID, NetworkMessageInfo info, NetworkGroup group)
		{
			return SetViewID(viewID, info.sender, group);
		}

		/// <summary>
		/// Removes the assigned viewID from this NetworkView.
		/// </summary>
		/// <remarks>
		/// Be aware that the viewID is still allocated. The next logical next is usually a call to <see cref="uLink.Network.DeallocateViewID"/>.
		/// </remarks>
		public void SetUnassignedViewID()
		{
			if (_parent.IsNotNullOrUnassigned())
			{
				_parent.SetUnassignedViewID();
				return;
			}

			_network.RemoveNetworkView(this);

			_data.viewID = NetworkViewID.unassigned;
			_data.owner = NetworkPlayer.server;
			_data.group = NetworkGroup.unassigned;
			_data.isInstantiatedRemotely = false;

			foreach (var child in _children)
			{
				child._data.viewID = NetworkViewID.unassigned;
				child._data.owner = NetworkPlayer.server;
				child._data.group = NetworkGroup.unassigned;
				child._data.isInstantiatedRemotely = false;
			}

			_children = new NetworkViewBase[0];
		}

		/// <summary>
		/// Allocates one free viewID and sets it to this NetworkView. 
		/// </summary>
		/// <remarks>
		/// Only for advanced users. Works like <see cref="O:uLink.Network.AllocateViewID"/>, but 
		/// in addition it sets the viewID to this NetworkView and the owner and 
		/// creator will become this <see cref="uLink.NetworkPlayer"/>.
		/// <para>
		/// Can only be called in an authoritative server or in authoritative clients.
		/// </para>
		/// </remarks>
		public NetworkViewID AllocateViewID()
		{
			if (_parent.IsNotNullOrUnassigned()) return _parent.AllocateViewID();

			NetworkViewID viewID = _network.AllocateViewID();
			if (viewID != NetworkViewID.unassigned) SetViewID(viewID, _network._localPlayer);
			return viewID;
		}

		/// <summary>
		/// Returns an unused viewID to the pool of unused IDs. Can only be called on the same peer as the viewID was allocated.
		/// </summary>
		/// <remarks>
		/// Works like <see cref="uLink.Network.DeallocateViewID"/>
		/// <para>
		/// Can only be called in an authoritative server or in authoritative clients.
		/// </para>
		/// </remarks>
		public bool DeallocateViewID()
		{
			if (_parent.IsNotNullOrUnassigned()) return _parent.DeallocateViewID();

			NetworkViewID viewID = _data.viewID;
			if (viewID == NetworkViewID.unassigned) return false;

			SetUnassignedViewID();
			return _network._DeallocateViewID(viewID);
		}

		/// <summary>
		/// Sets the provided arguments as children of this NetworkView.
		/// </summary>
		/// <param name="children">The network views that we want to make our children.</param>
		/// <returns>If the operation was successful or not.</returns>
		public bool SetChildren(NetworkViewBase[] children)
		{
			int count = children != null ? children.Length : 0;

			Log.Info(NetworkLogFlags.NetworkView, count, " children are about to be adopted by parent ", this);

			if (children != null)
			{
				if (Utility.IsArrayRefEqual(children, _children))
				{
					Log.Info(NetworkLogFlags.NetworkView, "Parent ", this, " already has adopted these exact children");
					return true;
				}

				foreach (var child in children)
				{
					if (child._childIndex != -1)
					{
						Log.Error(NetworkLogFlags.NetworkView, "Parent ", this, " can't adopt child ", child, " because it already has a parent");
						return false;
					}
				}
			}
			else if (_children.Length == 0)
			{
				Log.Info(NetworkLogFlags.NetworkView, "Parent ", this, " already has adopted these exact children");
				return true;
			}

			if (_children.Length != 0)
			{
				Log.Info(NetworkLogFlags.NetworkView, "Parent ", this, " is abandoning all ", _children.Length, " current children");

				foreach (var child in _children)
				{
					child._childIndex = -1;
					child._parent = null;
					child._data.viewID = NetworkViewID.unassigned;
					child._data.owner = NetworkPlayer.server;
					child._data.group = NetworkGroup.unassigned;
					child._data.isInstantiatedRemotely = false;

					Log.Debug(NetworkLogFlags.NetworkView, "Parent ", this, " is has abandoned child ", child);
				}
			}

			if (count == 0)
			{
				Log.Info(NetworkLogFlags.NetworkView, "Parent ", this, " no longer has any children");
				_children = new NetworkViewBase[0];
				return true;
			}

			_children = children;

			for (int i = 0; i < _children.Length; i++)
			{
				var child = _children[i];

				child._childIndex = i;
				child._parent = this;
				child._data.viewID = viewID;
				child._data.owner = owner;
				child._data.group = group;
				child._data.isInstantiatedRemotely = isInstantiatedRemotely;

				Log.Debug(NetworkLogFlags.NetworkView, "Parent ", this, " has adopted child ", child, " and indexed it as ", i);
			}
			
			return true;
		}

		/// <summary>
		/// Returns a child of the network view.
		/// </summary>
		/// <param name="childIndex">Index of the child.</param>
		/// <returns>The child if could be found, An exception could be thrown if you send an invalid index (System.OutOfRangeException).</returns>
		public NetworkViewBase GetChild(int childIndex)
		{
			return _children[childIndex];
		}

		/// <summary>
		/// Sets the scope of a player against this network view.
		/// </summary>
		/// <param name="target">The player that we want to set its scope against ourself.</param>
		/// <param name="relevancy">if <c>true</c> the plyaer receives our RPCs and state
		/// syncs, otherwise not.</param>
		/// <returns>The effective relevancy of this network view for the target.</returns>
		/// <remarks>
		/// The player will not receive RPCs nd state syncs but the object will exists in the player's machine.
		/// A feature like the hide game objects feature in network groups doesn't exists here.
		/// You should use that feature for situations which require it and probably use scope for area of interest management and ....
		/// </remarks>
		public bool SetScope(NetworkPlayer target, bool relevancy)
		{
			_network._AssertPlayer(target);
			if(!(target != NetworkPlayer.server)){Utility.Exception( "Can't add or remove server from the scope");}
			_network._AssertIsServerListening();

			if (_scopeCulling == null)
			{
				if (relevancy) return true;

				_scopeCulling = new HashSet<NetworkPlayer>();
				_scopeCulling.Add(target);

				_network._AddScope(viewID, _scopeCulling);
				return true;
			}

			if (relevancy && _scopeCulling.Count <= 1)
			{
				if (_scopeCulling.Count != 0 && !_scopeCulling.Contains(target)) return true;

				_scopeCulling = null;
				_network._RemoveScope(viewID);
				return false;
			}

			bool oldRelevancy = relevancy ? !_scopeCulling.Remove(target) : _scopeCulling.Add(target);
			return oldRelevancy;
		}

		/// <summary>
		/// Gets the scope of a player gainst this network view.
		/// </summary>
		/// <param name="target">The player that we	want to see our relevancy against.</param>
		/// <returns>The relevancy of this network view against the player. if <c>true</c>
		/// then the player will receive RPCs and state syncs for the network view, otherwise not.</returns>
		/// <remarks>
		/// The player will not receive RPCs nd state syncs but the object will exists in the player's machine.
		/// A feature like the hide game objects feature in network groups doesn't exists here.
		/// You should use that feature for situations which require it and probably use scope for area of interest management and ....
		/// </remarks>
		public bool GetScope(NetworkPlayer target)
		{
			_network._AssertIsServerListening();
			_network._AssertPlayer(target);

			return (_scopeCulling == null) || !_scopeCulling.Contains(target);
		}

		/// <summary>
		/// The scope for all players will be set to true against this network view.
		/// Everyone will receive our state syncs and RPCs.
		/// </summary>
		public void ResetScope()
		{
			_scopeCulling = null;
			_network._RemoveScope(viewID);
		}

		protected abstract bool OnRPC(string rpcName, BitStream stream, NetworkMessageInfo info);
		protected abstract bool OnSerializeProxy(BitStream stream, NetworkMessageInfo info);
		protected abstract bool OnSerializeOwner(BitStream stream, NetworkMessageInfo info);
		protected abstract bool OnSerializeHandover(BitStream stream, NetworkMessageInfo info);
		protected abstract bool OnSerializeCellProxy(BitStream stream, NetworkMessageInfo info);

		internal bool _CallRPC(string rpcName, BitStream stream, NetworkMessageInfo info)
		{
			if (_children.Length != 0)
			{
				int index = ((int)stream._buffer.ReadVariableUInt32()) - 1;

				if (index == -1)
				{
					Log.Debug(NetworkLogFlags.NetworkView, "Executing RPC on parent");

					return OnRPC(rpcName, stream, info);
				}

				Log.Debug(NetworkLogFlags.NetworkView, "Executing RPC on child ", index);

				var child = _children[index];
				if (child.IsNullOrDestroyed())
				{
					Log.Error(NetworkLogFlags.NetworkView, "Can't execute RPC on child ", index, " because child not found");
					return false;
				}

				return child.OnRPC(rpcName, stream, info);
			}

			return OnRPC(rpcName, stream, info);
		}

		internal bool _SerializeProxy(BitStream stream, NetworkMessageInfo info)
		{
			bool result = OnSerializeProxy(stream, info);

			foreach (var child in _children)
			{
				if (child.stateSynchronization != NetworkStateSynchronization.Off && child.hasSerializeProxy)
				{
					result |= child.OnSerializeProxy(stream, info);
				}
			}

			return result;
		}

		internal bool _SerializeOwner(BitStream stream, NetworkMessageInfo info)
		{
			bool result = OnSerializeOwner(stream, info);

			foreach (var child in _children)
			{
				if (child.stateSynchronization != NetworkStateSynchronization.Off && child.hasSerializeOwner)
				{
					result |= child.OnSerializeOwner(stream, info);
				}
			}

			return result;
		}

		internal bool _SerializeCellProxy(BitStream stream, NetworkMessageInfo info)
		{
			bool result = OnSerializeCellProxy(stream, info);

			foreach (var child in _children)
			{
				if (child.stateSynchronization != NetworkStateSynchronization.Off && child.hasSerializeCellProxy)
				{
					result |= child.OnSerializeCellProxy(stream, info);
				}
			}

			return result;
		}

		internal bool _SerializeHandover(BitStream stream, NetworkMessageInfo info)
		{
			bool result = OnSerializeHandover(stream, info);

			foreach (var child in _children)
			{
				if (child.hasSerializeHandover)
				{
					result |= child.OnSerializeHandover(stream, info);
				}
			}

			return result;
		}

		protected void RPC(NetworkFlags flags, string rpcName, RPCMode mode, params object[] args)
		{
			_network._AssertRPC(viewID, mode);

			var msg = new NetworkMessage(_network, flags, NetworkMessage.Channel.RPC, rpcName, mode, viewID);

			if (_parent.IsNotNullOrUnassigned() || _children.Length != 0)
			{
				if (_parent.IsNotNullOrUnassigned() && _childIndex == -1)
				{
					Log.Error(NetworkLogFlags.NetworkView, "Can't send RPC from ", this, " because it is missing a child index from it's parent ", _parent);
					return;
				}

				Log.Debug(NetworkLogFlags.NetworkView, "Sending RPC to child ", _childIndex, ", if -1 then it's actually the parent");

				msg.stream._buffer.WriteVariableUInt32((uint) (_childIndex + 1));
			}

			_rpcWriter.Write(msg.stream, rpcName, args);
			_network._CreateRPCMode(msg, mode);
		}

		protected void RPC(NetworkFlags flags, string rpcName, NetworkPlayer target, params object[] args)
		{
			if(!(target != NetworkPlayer.cellProxies || isCellAuthority)){Utility.Exception( "CellProxy object ", this, " can't send a RPC to other CellProxies, only the CellAuthority object can send to a CellProxy.");}
			_network._AssertRPC(viewID, target);

			var msg = new NetworkMessage(_network, flags, NetworkMessage.Channel.RPC, rpcName, NetworkMessage.InternalCode.None, target, NetworkPlayer.unassigned, viewID);

			if (_parent.IsNotNullOrUnassigned() || _children.Length != 0)
			{
				if (_parent.IsNotNullOrUnassigned() && _childIndex == -1)
				{
					Log.Error(NetworkLogFlags.NetworkView, "Can't send RPC to ", this, " because it is missing a child index from it's parent ", _parent);
					return;
				}

				Log.Debug(NetworkLogFlags.NetworkView, "Sending RPC to child ", _childIndex);

				msg.stream._buffer.WriteVariableUInt32((uint)(_childIndex + 1));
			}

			_rpcWriter.Write(msg.stream, rpcName, args);
			_network._CreatePrivateRPC(msg);
		}

		protected void RPC(NetworkFlags flags, string rpcName, IEnumerable<NetworkPlayer> targets, params object[] args)
		{
			_network._AssertRPC(viewID, targets);

			//TODO: optimize by using a Bitstream pool here (David's remark)
			var stream = new BitStream((flags & NetworkFlags.TypeUnsafe) == 0);

			if (_parent.IsNotNullOrUnassigned() || _children.Length != 0)
			{
				if (_parent.IsNotNullOrUnassigned() && _childIndex == -1)
				{
					Log.Error(NetworkLogFlags.NetworkView, "Can't send RPC to ", this, " because it is missing a child index from it's parent ", _parent);
					return;
				}

				Log.Debug(NetworkLogFlags.NetworkView, "Sending RPC to child ", _childIndex);

				stream._buffer.WriteVariableUInt32((uint)(_childIndex + 1));
			}

			_rpcWriter.Write(stream, rpcName, args);

			foreach (var target in targets)
			{
				if(!(target != NetworkPlayer.cellProxies || isCellAuthority)){Utility.Exception( "CellProxy object ", this, " can't send a RPC to other CellProxies, only the CellAuthority object can send to a CellProxy.");}
			}

			foreach (var target in targets)
			{
				var msg = new NetworkMessage(_network, flags, NetworkMessage.Channel.RPC, rpcName, NetworkMessage.InternalCode.None, target, NetworkPlayer.unassigned, viewID);

				msg.stream.AppendBitStream(stream);
				_network._CreatePrivateRPC(msg);
			}
		}

		public override string ToString()
		{
			return String.Concat("NetworkViewBase (", viewID.ToString(), ")");
		}
	}
}

