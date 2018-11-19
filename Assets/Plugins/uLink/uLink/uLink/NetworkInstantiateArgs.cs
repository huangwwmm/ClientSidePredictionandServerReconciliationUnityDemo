#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision$
// $LastChangedBy$
// $LastChangedDate$
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	/// <summary>
	/// This struct contains the values that are sent to instantiators for instantiating a network aware object.
	/// </summary>
	/// <remarks>
	/// See <see cref="uLink.NetworkInstantiator"/> and <see cref="uLink.NetworkInstantiatorUtility"/> and <see cref="uLink.NetworkView.instantiator"/>
	/// </remarks>
	public struct NetworkInstantiateArgs
	{
		/// <summary>
		/// Position of the object to be instantiated.
		/// </summary>
		public readonly Vector3 position;
		/// <summary>
		/// Rotation of the object to be instantiated
		/// </summary>
		public readonly Quaternion rotation;
		/// <summary>
		/// All network related data like ViewID, owner, prefab name and ... which you need to instantiate the object.
		/// </summary>
		public readonly NetworkViewData data;

		/// <summary>
		/// ViewID of the object to be instantiated.
		/// </summary>
		public NetworkViewID viewID { get { return data.viewID; } }
		/// <summary>
		/// The <see cref="uLink.NetworkPlayer"/> who owns the object.
		/// </summary>
		public NetworkPlayer owner { get { return data.owner; } }
		/// <summary>
		/// The group that this object is a member of.
		/// UnAssigned/0 means the object is in no group.
		/// </summary>
		public NetworkGroup group { get { return data.group; } }

		/// <summary>
		/// The network settings of the object about Pikko Server handovers and ...
		/// </summary>
		public NetworkAuthFlags authFlags { get { return data.authFlags; } }
		/// <summary>
		/// Is this object instantiated remotely or this player is the caller of it's <see cref="uLink.Network.Instantiate"/>
		/// </summary>
		public bool isInstantiatedRemotely { get { return data.isInstantiatedRemotely; } }

		/// <summary>
		/// The prefab which is used for proxy role for this object.
		/// </summary>
		public string proxyPrefab { get { return data.proxyPrefab; } }
		/// <summary>
		/// The prefab which is used for owner role for this object.
		/// </summary>
		public string ownerPrefab { get { return data.ownerPrefab; } }
		/// <summary>
		/// The prefab which will be used on the server for this object.
		/// </summary>
		public string serverPrefab { get { return data.serverPrefab; } }
		/// <summary>
		/// The prefab which is used on the cell server which has authority on this object.
		/// Used for Pikko Server only.
		/// </summary>
		public string cellAuthPrefab { get { return data.cellAuthPrefab; } }
		/// <summary>
		/// The prefab which is used on cell servers other than the one which has authority on the object.
		/// Used for Pikko Server only.
		/// </summary>
		public string cellProxyPrefab { get { return data.cellProxyPrefab; } }
		/// <summary>
		/// The initial data sent in the Instantiate call.
		/// </summary>
		public BitStream initialData { get { return data.initialData; } }
		/// <summary>
		/// Creates and initializes a new instance of NetworkInstantiateArgs.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <param name="viewID"></param>
		/// <param name="owner"></param>
		/// <param name="group"></param>
		/// <param name="proxyPrefab"></param>
		/// <param name="ownerPrefab"></param>
		/// <param name="serverPrefab"></param>
		/// <param name="cellAuthPrefab"></param>
		/// <param name="cellProxyPrefab"></param>
		/// <param name="authFlags"></param>
		/// <param name="isInstantiatedRemotely"></param>
		/// <param name="initialData"></param>
		public NetworkInstantiateArgs(Vector3 position, Quaternion rotation, NetworkViewID viewID, NetworkPlayer owner, NetworkGroup group, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, bool isInstantiatedRemotely, BitStream initialData)
		{
			this.position = position;
			this.rotation = rotation;

			data = new NetworkViewData(viewID, owner, group, authFlags, isInstantiatedRemotely, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, initialData);
		}

		/// <summary>
		/// Sets up a NetworkView with the info in this instance of NetworkInstantiateArgs.
		/// </summary>
		/// <param name="nv">The NetworkView that we want to setup.</param>
		public void SetupNetworkView(NetworkViewBase nv)
		{
			nv.position = position;
			nv.rotation = rotation;

			data.SetupNetworkView(nv);
		}
	}
}
