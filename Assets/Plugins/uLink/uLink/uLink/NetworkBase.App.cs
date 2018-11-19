#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision$
// $LastChangedBy$
// $LastChangedDate$
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Collections.Generic;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	internal abstract class NetworkBaseApp
	{
		protected class GroupData
		{
			public readonly Dictionary<NetworkPlayer, HashSet<NetworkViewID>> users = new Dictionary<NetworkPlayer, HashSet<NetworkViewID>>();
			public readonly HashSet<NetworkViewID> views = new HashSet<NetworkViewID>();

			public NetworkGroupFlags flags;

			public GroupData(NetworkGroup group)
			{
				if (!NetworkGroup._flags.TryGetValue(group, out flags))
				{
					flags = NetworkGroupFlags.None;
				}
			}
		}

		protected readonly Dictionary<NetworkGroup, GroupData> _groups = new Dictionary<NetworkGroup, GroupData>();

		internal readonly Dictionary<NetworkViewID, NetworkViewBase> _enabledViews = new Dictionary<NetworkViewID, NetworkViewBase>();
		internal readonly Dictionary<NetworkPlayer, List<NetworkViewBase>> _userViews = new Dictionary<NetworkPlayer, List<NetworkViewBase>>();

		protected abstract void OnStart();

		protected abstract NetworkViewBase OnCreate(string prefabName, NetworkInstantiateArgs args, NetworkMessageInfo info);
		protected abstract void OnDestroy(NetworkViewBase networkView);

		protected abstract void OnEvent(string eventName, object value);

		public int networkViewCount
		{
			get
			{
				return _enabledViews.Count;
			}
		}

		public Dictionary<NetworkViewID, NetworkViewBase>.ValueCollection networkViews
		{
			get
			{
				return _enabledViews.Values;
			}
		}

		/* TODO: come up with a good name for:
		
		public NetworkPlayer[] allOwners
		{
			get
			{
				return Utility.ToArray(_userViews.Keys);
			}
		}
		*/

		internal void _PreStart(NetworkStartEvent nsEvent)
		{
			_Notify("OnPreStartNetwork", nsEvent);
		}

		internal void _Start()
		{
			OnStart();
		}

		internal void _Notify(string eventName, object value)
		{
			Log.Debug(NetworkLogFlags.Event, "Triggering event ", eventName, " with value ", value);
			OnEvent(eventName, value);
		}

		internal NetworkViewBase _FindNetworkView(NetworkViewID viewID)
		{
			NetworkViewBase nv;
			return _enabledViews.TryGetValue(viewID, out nv) ? nv : null;
		}

		internal NetworkPlayer _FindNetworkViewOwner(NetworkViewID viewID)
		{
			if (viewID == NetworkViewID.unassigned)
				return NetworkPlayer.unassigned;

			if (viewID.isManual)
				return NetworkPlayer.server;

			var nv = _FindNetworkView(viewID);
			return nv.IsNotNull() ? nv.owner : NetworkPlayer.unassigned;
		}

		internal NetworkViewBase[] _FindNetworkViewsByOwner(NetworkPlayer player)
		{
			List<NetworkViewBase> nvs;
			return _userViews.TryGetValue(player, out nvs) ? nvs.ToArray() : new NetworkViewBase[0];
		}

		internal bool _DoesOwnerHaveAnyNetworkViews(NetworkPlayer player)
		{
			return _userViews.ContainsKey(player);
		}

		internal bool _DoesGroupHaveAnyNetworkViews(NetworkGroup group)
		{
			return _groups.ContainsKey(group);
		}

		// TODO: export find by group w/ owner etc functions to official API!

		internal NetworkViewBase[] _FindNetworkViewsInGroup(NetworkGroup group)
		{
			GroupData data;
			if (_groups.TryGetValue(group, out data))
			{
				var viewIDs = data.views;
				var views = new List<NetworkViewBase>(viewIDs.Count);

				foreach (var viewID in viewIDs)
				{
					NetworkViewBase nv;
					if (_enabledViews.TryGetValue(viewID, out nv))
					{
						views.Add(nv);
					}
				}

				return views.ToArray();
			}

			return new NetworkViewBase[0];
		}

		internal NetworkViewID[] _FindNetworkViewIDsInGroup(NetworkGroup group)
		{
			GroupData data;
			if (_groups.TryGetValue(group, out data))
			{
				var viewIDs = data.views;
				var array = new NetworkViewID[viewIDs.Count];

				viewIDs.CopyTo(array);
				return array;
			}

			return new NetworkViewID[0];
		}

		internal NetworkPlayer[] _FindPlayersInGroup(NetworkGroup group)
		{
			GroupData data;
			if (_groups.TryGetValue(group, out data))
			{
				var users = data.users;
				var array = new NetworkPlayer[users.Count];

				users.Keys.CopyTo(array, 0);
				return array;
			}

			return new NetworkPlayer[0];
		}

		internal NetworkGroup[] _FindGroupsWithPlayer(NetworkPlayer player)
		{
			var groups = new List<NetworkGroup>(_groups.Count);

			foreach (var pair in _groups)
			{
				if (pair.Value.users.ContainsKey(player))
				{
					groups.Add(pair.Key);
				}
			}

			return groups.ToArray();
		}

		public bool AddNetworkView(NetworkViewBase nv)
		{
			var viewID = nv.viewID;
			if (viewID == NetworkViewID.unassigned) return false;

			NetworkViewBase other;
			if (_enabledViews.TryGetValue(viewID, out other))
			{
				if (!other.ReferenceEquals(nv))
				{
					Log.Error(NetworkLogFlags.NetworkView, "Can't subscribe ", nv, " since there already exists one with ", viewID, " at ", other);
					return false;
				}
			}
			else
			{
				Log.Debug(NetworkLogFlags.NetworkView, "Subscribing ", nv);

				_enabledViews[viewID] = nv;

				var owner = nv.owner;

				_userViews.GetOrAdd(owner).Add(nv);

				nv._ClearStateSyncData();

				var group = nv.group;
				if (group != NetworkGroup.unassigned)
				{
					_AddPlayerToGroup(owner, group, viewID);
				}
			}

			return true;
		}

		public bool RemoveNetworkView(NetworkViewBase nv)
		{
			var viewID = nv.viewID;
			if (viewID == NetworkViewID.unassigned) return false;

			NetworkViewBase other;
			if (!_enabledViews.TryGetValue(viewID, out other) || !other.ReferenceEquals(nv)) return false;

			Log.Debug(NetworkLogFlags.NetworkView, "Unsubscribing ", nv);

			_enabledViews.Remove(viewID);

			var owner = nv.owner;

			List<NetworkViewBase> views;
			if (_userViews.TryGetValue(owner, out views))
			{
				views.Remove(nv);
			}

			var group = nv.group;
			if (group != NetworkGroup.unassigned)
			{
				_RemovePlayerFromGroup(owner, group, viewID);
			}

			return true;
		}

		public void RemoveAllNetworkViews()
		{
			_enabledViews.Clear();
			_userViews.Clear();

			_groups.Clear();
		}

		internal void _DestroyNetworkView(NetworkViewBase nv)
		{
			OnDestroy(nv);

			nv.SetUnassignedViewID();
		}

		internal NetworkViewBase _Create(string localPrefab, NetworkInstantiateArgs args, NetworkMessage msg)
		{
#if UNITY_BUILD
			Profiler.BeginSample("Instantiate prefab: " + localPrefab);
#endif

			// TODO: if server then validate viewID is owned by player!

			if (String.IsNullOrEmpty(localPrefab))
			{
				Log.Debug(NetworkLogFlags.Instantiate, "Skipping instantiate of empty prefab with ", args.viewID, " (in ", args.group, "), owner ", args.owner);

#if UNITY_BUILD
				Profiler.EndSample();
#endif
				return null;
			}

			Log.Debug(NetworkLogFlags.Instantiate, "Instantiate prefab '", localPrefab, "' with viewID ", args.viewID, " (in ", args.group, "), owner ", args.owner, ", position ", args.position, ", rotation ", args.rotation);

			// TODO: why do we do this? We aren't removing the old instantiate from the RPC buffer so is this even safe?
			NetworkViewBase nv = _FindNetworkView(args.viewID);
			if (nv.IsNotNull())
			{
				_DestroyNetworkView(nv);
			}

			var info = new NetworkMessageInfo(msg, null);
			nv = OnCreate(localPrefab, args, info);

#if UNITY_BUILD
			Profiler.EndSample();
#endif
			return nv;
		}

		protected GroupData _AddPlayerToGroup(NetworkPlayer player, NetworkGroup group, NetworkViewID viewID)
		{
			var data = _groups.GetOrAdd(group, () => new GroupData(group));

			var userSet = data.users.GetOrAdd(player);
			if (!userSet.Add(viewID)) return data;

			if (viewID != NetworkViewID.unassigned) data.views.Add(viewID);

			_ServerAddPlayerToGroup(player, group, viewID, data, userSet.Count);
			return data;
		}

		protected GroupData _RemovePlayerFromGroup(NetworkPlayer player, NetworkGroup group, NetworkViewID viewID)
		{
			GroupData data;
			if (_groups.TryGetValue(group, out data))
			{
				HashSet<NetworkViewID> userSet;
				if (data.users.TryGetValue(player, out userSet))
				{
					if (!userSet.Remove(viewID)) return data;

					if (userSet.Count == 0) data.users.Remove(player);
					if (viewID != NetworkViewID.unassigned) data.views.Remove(viewID);

					_ServerRemovePlayerFromGroup(player, group, viewID, data, userSet.Count);
				}

				if (data.users.Count == 0) _groups.Remove(group);
				return data;
			}

			return null;
		}

		protected abstract void _ServerAddPlayerToGroup(NetworkPlayer player, NetworkGroup group, NetworkViewID viewID, GroupData data, int userSetCount);
		protected abstract void _ServerRemovePlayerFromGroup(NetworkPlayer player, NetworkGroup group, NetworkViewID viewID, GroupData data, int userSetCount);
	}
}
