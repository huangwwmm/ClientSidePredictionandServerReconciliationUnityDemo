#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision$
// $LastChangedBy$
// $LastChangedDate$
#endregion

namespace uLink
{
	/// <summary>
	/// The whole data which a <see cref="uLink.NetworkView"/> needs to fully initialize itself can be stored in
	/// an instance of this class.
	/// This is mostly used in Instantiator methods which should initialize and create the network aware object.
	/// </summary>
	public struct NetworkViewData
	{
		/// <summary>
		/// ViewID of the NetworkView.
		/// </summary>
		public NetworkViewID viewID;
		/// <summary>
		/// The <see cref="uLink.NetworkPlayer"/> which owns the NetworkView in question.
		/// </summary>
		public NetworkPlayer owner;
		/// <summary>
		/// The <see cref="uLink.NetworkGroup"/> which the NetworkView belongs to.
		/// You usually use ints instead of NetworkGroups and an implicit casts exists between the types.
		/// </summary>
		public NetworkGroup group;
		/// <summary>
		/// The Network related settings which is used by Pikko Server.
		/// </summary>
		public NetworkAuthFlags authFlags;
		/// <summary>
		/// Is the NetworkView instantiated on a remote machine or locally.
		/// </summary>
		public bool isInstantiatedRemotely;
		/// <summary>
		/// The prefab used for proxy role in the NetworkView.
		/// </summary>
		public string proxyPrefab;
		/// <summary>
		/// The prefab used for the owner role in this NetworkView.
		/// </summary>
		public string ownerPrefab;
		/// <summary>
		/// The prefab used for server role in this NetworkView.
		/// </summary>
		public string serverPrefab;

		/// <summary>
		/// The prefab on the cell server which has authority on the object.
		/// </summary>
		public string cellAuthPrefab;
		/// <summary>
		/// The prefab used for cell servers which have a proxy of the object and don't have authority over it.
		/// </summary>
		public string cellProxyPrefab;
		/// <summary>
		/// The initial data sent in the <see cref="uLink.Network.Instantiate"/> call which resulted in creation of this NetworkView.
		/// </summary>
		public BitStream initialData;

		/// <summary>
		/// Creates a NetworkViewData object with the provided arguments.
		/// </summary>
		/// <param name="viewID"></param>
		/// <param name="owner"></param>
		/// <param name="group"></param>
		/// <param name="authFlags"></param>
		/// <param name="isInstantiatedRemotely"></param>
		/// <param name="proxyPrefab"></param>
		/// <param name="ownerPrefab"></param>
		/// <param name="serverPrefab"></param>
		/// <param name="cellAuthPrefab"></param>
		/// <param name="cellProxyPrefab"></param>
		/// <param name="initialData"></param>
		public NetworkViewData(NetworkViewID viewID, NetworkPlayer owner, NetworkGroup group, NetworkAuthFlags authFlags, bool isInstantiatedRemotely, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, BitStream initialData)
		{
			this.viewID = viewID;
			this.owner = owner;
			this.group = group;
			this.authFlags = authFlags;
			this.isInstantiatedRemotely = isInstantiatedRemotely;
			this.proxyPrefab = proxyPrefab;
			this.ownerPrefab = ownerPrefab;
			this.serverPrefab = serverPrefab;
			this.cellAuthPrefab = cellAuthPrefab;
			this.cellProxyPrefab = cellProxyPrefab;
			this.initialData = initialData;
		}

		/// <summary>
		/// Sets a NetworkView up with this data.
		/// </summary>
		/// <param name="nv"></param>
		public void SetupNetworkView(NetworkViewBase nv)
		{
			nv.SetViewID(viewID, owner, group, isInstantiatedRemotely);
			nv._data.authFlags = authFlags; // TODO: ugly hack to void authority permission check

			nv.proxyPrefab = proxyPrefab;
			nv.ownerPrefab = ownerPrefab;
			nv.serverPrefab = serverPrefab;
			nv.cellAuthPrefab = cellAuthPrefab;
			nv.cellProxyPrefab = cellProxyPrefab;

			nv.initialData = initialData;
		}
	}
}
