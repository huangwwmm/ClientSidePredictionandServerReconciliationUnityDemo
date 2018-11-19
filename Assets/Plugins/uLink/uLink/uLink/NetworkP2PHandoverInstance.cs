#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision$
// $LastChangedBy$
// $LastChangedDate$
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
#if UNITY_BUILD
	using NV = NetworkView;
	using P2P = NetworkP2P;
#else
	using NV = NetworkViewBase;
	using P2P = NetworkP2PBase;
#endif

	/// <summary>
	/// The frame of reference for positions and rotations when handing objects over is specified by this.
	/// </summary>
	public enum NetworkP2PSpace : byte
	{
		/// <summary>
		/// World space should be used. 
		/// So the positions and rotations provided by unity as world space values will be used directly.
		/// </summary>
		World,
		/// <summary>
		/// The positions and rotations will be interpreted as if the object with <see cref="NetworkP2P"/> attached was the origin.
		/// </summary>
		NetworkP2P,
	}

	/// <summary>
	/// We use this to carry over information about a handover.
	/// </summary>
	/// <remarks>Read more about handovers in the peer to peer section of the manual.
	/// <para>
	/// You mostly use this class when working with handover API and events.
	/// </para>
	/// </remarks>
	public class NetworkP2PHandoverInstance
	{
		internal NV _networkView;
		internal P2P _networkP2P;
		internal bool _isInstantiatable;

		/// <summary>
		/// The Position of the object being handed over.
		/// </summary>
		public Vector3 position;
		/// <summary>
		/// The rotation of the object being handed over.
		/// </summary>
		public Quaternion rotation;
		/// <summary>
		/// The space origin (frame of reference) which the object position and rotation will be calculated based on it.
		/// </summary>
		public NetworkP2PSpace relativeTo;

		/// <summary>
		/// The view id of the object being handed over.
		/// </summary>
		public NetworkViewID remoteViewID;
		/// <summary>
		/// The <see cref="uLink.NetworkGroup"/> of the object being handed over.
		/// You usually use this as an int.
		/// </summary>
		public NetworkGroup group;
		/// <summary>
		/// The network auth flags used for the object.
		/// This is used for handovers in Pikko Server for now.
		/// </summary>
		public NetworkAuthFlags authFlags;
		/// <summary>
		/// Is the object instantiated remotely or we are the caller of its instantiate call.
		/// </summary>
		public bool isInstantiatedRemotely;
		/// <summary>
		/// Name of the prefab which is used at proxy role for the object being handed over.
		/// </summary>
		public string proxyPrefab;
		/// <summary>
		/// Name of the prefab which is used at owner role for the object being handed over.
		/// </summary>
		public string ownerPrefab;
		/// <summary>
		/// Name of the prefab which is used at server role for the object being handed over.
		/// </summary>
		public string serverPrefab;
		/// <summary>
		/// Name of the prefab which is used at cell authority role for the object being handed over.
		/// </summary>
		/// <remarks>This is used with cell servers of Pikko Server.</remarks>
		public string cellAuthPrefab;
		/// <summary>
		/// Name of the prefab which is used at cell proxy role for the object being handed over.
		/// </summary>
		/// <remarks>This is used with cell servers of Pikko server.</remarks>
		public string cellProxyPrefab;

		private byte[] _initialData;
		private byte[] _handoverData;

		/// <summary>
		/// Returns the <see cref="uLink.NetworkP2P"/> which is handing this object over.
		/// </summary>
		public P2P networkP2P { get { return _networkP2P; } }

		/// <summary>
		/// Gets or sets the <see cref="uLink.NetworkView"/> of the object being handed over.
		/// </summary>
		public NV networkView
		{
			get
			{
				return _networkView;
			}
			set
			{
				if (value.viewID == NetworkViewID.unassigned)
				{
					// TODO: throw exception
					return;
				}

				_networkView = value.root; // make sure it's the parent networkview and not a child
				remoteViewID = _networkView.viewID;
				group = _networkView.group;
				isInstantiatedRemotely = _networkView.isInstantiatedRemotely;

				proxyPrefab = _networkView.proxyPrefab;
				ownerPrefab = _networkView.ownerPrefab;
				serverPrefab = _networkView.serverPrefab;
				cellAuthPrefab = _networkView.cellAuthPrefab;
				cellProxyPrefab = _networkView.cellProxyPrefab;
			}
		}

		/// <summary>
		/// Creates and initializes a new instance of NetworkP2PHandoverInstance.
		/// </summary>
		public NetworkP2PHandoverInstance()
		{
		}

		/// <summary>
		/// Creates and initializes a new instance of NetworkP2PHandoverInstance.
		/// </summary>
		/// <param name="networkView">The network view to use for the object being handovered.</param>
		public NetworkP2PHandoverInstance(NetworkViewBase networkView)
			: this(networkView, networkView.position, networkView.rotation, NetworkP2PSpace.World)
		{
		}

		/// <summary>
		/// Creates and initializes a new instance of NetworkP2PHandoverInstance.
		/// </summary>
		/// <param name="networkView">The network view to use for the object being handovered.</param>
		/// <param name="p2p">The NetworkP2P component used for handing this object over.</param>
		public NetworkP2PHandoverInstance(NetworkViewBase networkView, NetworkP2PBase p2p)
			: this(networkView, networkView.position, networkView.rotation, NetworkP2PSpace.NetworkP2P)
		{
			p2p.InverseTransform(ref position, ref rotation);
			_networkP2P = p2p as P2P;
		}

		/// <summary>
		/// Creates and initializes a new instance of NetworkP2PHandoverInstance.
		/// </summary>
		/// <param name="networkView">The network view to use for the object being handovered.</param>
		/// <param name="p2p">The NetworkP2P component used for handing this object over.</param>
		/// <param name="offsetPos">Offset position of the object compared to the <c>p2p</c> which is handing it over.</param>
		/// <param name="offsetRot">Offset rotation of the object compared to the <c>p2p</c> which is handing it over.</param>
		public NetworkP2PHandoverInstance(NetworkViewBase networkView, NetworkP2PBase p2p, Vector3 offsetPos, Quaternion offsetRot)
			: this(networkView, p2p)
		{
			position += offsetPos;
			rotation *= offsetRot;
			_networkP2P = p2p as P2P;
		}

		/// <summary>
		/// Creates and initializes a new instance of NetworkP2PHandoverInstance.
		/// </summary>
		/// <param name="networkView">The network view to use for the object being handovered.</param>
		/// <param name="position">Position of the object being handovered.</param>
		/// <param name="rotation">Rotation to use for the object being handovered.</param>
		/// <param name="relativeTo">The space that position and rotation should be relative to.</param>
		public NetworkP2PHandoverInstance(NetworkViewBase networkView, Vector3 position, Quaternion rotation, NetworkP2PSpace relativeTo)
		{
			this.position = position;
			this.rotation = rotation;
			this.relativeTo = relativeTo;

			_networkView = networkView.root as NV; // make sure it's the parent networkview and not a child
			remoteViewID = _networkView.viewID;
			group = _networkView.group;
			authFlags = _networkView._data.authFlags; // TODO: ugly hack to void authority permission check
			isInstantiatedRemotely = _networkView.isInstantiatedRemotely;

			proxyPrefab = _networkView.proxyPrefab;
			ownerPrefab = _networkView.ownerPrefab;
			serverPrefab = _networkView.serverPrefab;
			cellAuthPrefab = _networkView.cellAuthPrefab;
			cellProxyPrefab = _networkView.cellProxyPrefab;

			_initialData = null;
			_handoverData = null;

			_isInstantiatable = false;
			_networkP2P = null;
		}

		internal NetworkP2PHandoverInstance(NetBuffer buffer)
		{
			_networkView = null;

			position = new Vector3(buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat());
			rotation = new Quaternion(buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat());
			relativeTo = (NetworkP2PSpace)buffer.ReadByte();

			remoteViewID = new NetworkViewID(buffer);
			group = new NetworkGroup(buffer);
			authFlags = (NetworkAuthFlags)buffer.ReadByte();
			isInstantiatedRemotely = buffer.ReadBoolean();

			proxyPrefab = buffer.ReadString();
			ownerPrefab = buffer.ReadString();
			serverPrefab = buffer.ReadString();

			cellAuthPrefab = buffer.ReadString();
			cellProxyPrefab = buffer.ReadString();

			uint initialSize = buffer.ReadVariableUInt32();
			_initialData = initialSize != 0 ? buffer.ReadBytes((int)initialSize) : new byte[0];

			uint handoverSize = buffer.ReadVariableUInt32();
			_handoverData = handoverSize != 0 ? buffer.ReadBytes((int)handoverSize) : new byte[0];

			_isInstantiatable = true;
			_networkP2P = null;
		}

		/// <summary>
		/// This will cause the object to not be instantiated when the object owner connected to the new server which the object
		/// is handovered to.
		/// </summary>
		public void DontInstantiateOnConnected()
		{
			_isInstantiatable = false;
		}

		/// <summary>
		/// Instantiates the handovered object with the provided network player as owner.
		/// </summary>
		/// <param name="player">The player which should own the object.</param>
		public void InstantiateNow(NetworkPlayer player)
		{
			if (!_isInstantiatable)
			{
				// TODO: log error
				return;
			}

			_isInstantiatable = false;

			Vector3 pos = position;
			Quaternion rot = rotation;
			if (relativeTo == NetworkP2PSpace.NetworkP2P) _networkP2P.Transform(ref pos, ref rot);

			var nv = _networkP2P._network.Instantiate(player, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, pos, rot, group, Utility.BytesToObjects(_initialData));

			if (nv._hasSerializeHandover)
			{
				var stream = new BitStream(_handoverData, false);
				nv._SerializeHandover(stream, new NetworkMessageInfo(NetworkPlayer.unassigned, 0, 0, 0, NetworkFlags.NoTimestamp, nv));
			}
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.Write(position.x); buffer.Write(position.y); buffer.Write(position.z);
			buffer.Write(rotation.x); buffer.Write(rotation.y); buffer.Write(rotation.z); buffer.Write(rotation.w);
			buffer.Write((byte)relativeTo);

			remoteViewID._Write(buffer);
			group._Write(buffer);

			buffer.Write((byte)authFlags);
			buffer.Write(isInstantiatedRemotely);

			buffer.Write(proxyPrefab);
			buffer.Write(ownerPrefab);
			buffer.Write(serverPrefab);
			buffer.Write(cellAuthPrefab);
			buffer.Write(cellProxyPrefab);

			if (_networkView.initialData != null)
			{
				var initialData = _networkView.initialData._ToArray();
				buffer.WriteVariableUInt32((uint)initialData.Length);
				buffer.Write(initialData);
			}
			else
			{
				buffer.WriteVariableUInt32(0);
			}

			if (_networkView._hasSerializeHandover)
			{
				var stream = new BitStream(true, false);
				_networkView._SerializeHandover(stream, new NetworkMessageInfo(NetworkPlayer.unassigned, 0, 0, 0, NetworkFlags.NoTimestamp, _networkView));

				var handoverData = stream._ToArray();
				buffer.WriteVariableUInt32((uint)handoverData.Length);
				buffer.Write(handoverData);
			}
			else
			{
				buffer.WriteVariableUInt32(0);
			}
		}

		internal void _AssertRedirectOwner()
		{
			if (_networkView.owner == NetworkPlayer.unassigned)
			{
				// TODO: throw exception
			}

			if (_networkView.owner == NetworkPlayer.server)
			{
				// TODO: throw exception
			}
		}

		public override string ToString()
		{
			return "position " + position + ", rotation" + rotation + ", relativeTo " + relativeTo;
		}

		public override bool Equals(object other)
		{
			return Equals(other as NetworkP2PHandoverInstance);
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public bool Equals(NetworkP2PHandoverInstance other)
		{
			return other != null &&
				position == other.position &&
				rotation == other.rotation &&
				relativeTo == other.relativeTo &&
				remoteViewID == other.remoteViewID &&
				group == other.group &&
				proxyPrefab == other.proxyPrefab &&
				ownerPrefab == other.ownerPrefab &&
				serverPrefab == other.serverPrefab &&
				Utility.IsArrayEqual(_initialData, other._initialData) &&
				Utility.IsArrayEqual(_handoverData, other._handoverData);
		}
	}
}