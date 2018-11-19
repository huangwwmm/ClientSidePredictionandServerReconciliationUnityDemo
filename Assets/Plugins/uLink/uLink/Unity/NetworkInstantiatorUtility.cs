#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11847 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-04-16 10:51:36 +0200 (Mon, 16 Apr 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System.Collections.Generic;
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// This class has some lower level utility methods used in <see cref="o:uLink.Network.Instantiate"/> and
	/// <see cref="o:uLink.Network.Destroy"/> and <see cref="uLink.NetworkInstantiator"/> class.
	/// </summary>
	public static class NetworkInstantiatorUtility
	{
		private static bool _autoSetupOnAwake;
		private static NetworkInstantiateArgs _autoSetupOnAwakeArgs;

		public static void AutoSetupNetworkViewOnAwake(NetworkInstantiateArgs args)
		{
			_autoSetupOnAwake = true;
			_autoSetupOnAwakeArgs = args;
		}

		public static void ClearAutoSetupNetworkViewOnAwake()
		{
			_autoSetupOnAwake = false;
		}

		internal static void _DoAutoSetupNetworkViewOnAwake(NetworkView nv)
		{
			if (_autoSetupOnAwake)
			{
				_autoSetupOnAwakeArgs.SetupNetworkView(nv);

				ClearAutoSetupNetworkViewOnAwake();
			}
		}

		/// <summary>
		/// This is the default method which is called for instantiation.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static NetworkView Instantiate(NetworkView prefab, NetworkInstantiateArgs args)
		{
			AutoSetupNetworkViewOnAwake(args);
			var networkView = Object.Instantiate(prefab, args.position, args.rotation) as NetworkView;
			ClearAutoSetupNetworkViewOnAwake();

			return networkView;
		}

		/// <summary>
		/// Broadcasts a uLink_OnNetworkInstantiate message. This is used by
		/// <see cref="o:uLink.Network.Instantiate"/>'s default instantiator to broadcast the message.
		/// </summary>
		/// <param name="networkView">The NetworkView that you want to broadcast the message for</param>
		/// <param name="info"></param>
		public static void BroadcastOnNetworkInstantiate(NetworkView networkView, NetworkMessageInfo info)
		{
			BroadcastOnNetworkInstantiate(networkView, NetworkUnity.EVENT_INSTANTIATE, info);
		}

		/// <summary>
		/// Broadcasts a uLink_OnNetworkInstantiate message. This is used by
		/// <see cref="o:uLink.Network.Instantiate"/>'s default instantiator to broadcast the message.
		/// </summary>
		/// <param name="networkView">The NetworkView that you want to broadcast the message for</param>
		/// <param name="message"></param>
		/// <param name="info"></param>
		public static void BroadcastOnNetworkInstantiate(NetworkView networkView, string message, NetworkMessageInfo info)
		{
			// We have to create a new NetworkMessageInfo which includes the NetworkView.
			var instanceInfo = new NetworkMessageInfo(info, networkView);

			// Call the event on every MonoBehaviour in this game object or any of its children.
			networkView.BroadcastMessage(NetworkUnity.EVENT_INSTANTIATE, instanceInfo, SendMessageOptions.DontRequireReceiver);
		}

		/// <summary>
		/// Destroys the prefab of a networkView. This is the default method when you call <see cref="uLink.Network.Destroy"/>
		/// and uLink wants to destroy the prefab.
		/// </summary>
		/// <param name="networkView"></param>
		public static void Destroy(NetworkView networkView)
		{
			Object.Destroy(networkView.prefabRoot);
		}

		/// <summary>
		/// This function is a replacement for Unity's regular GetComponentsInChildren, which also works on prefabs.
		/// </summary>
		public static List<T> GetComponentsInChildren<T>(Transform transform) where T : Component
		{
			List<T> result = new List<T>();

			_GetComponentsInChildren(result, transform);

			return result;
		}

		private static void _GetComponentsInChildren<T>(List<T> result, Transform transform) where T : Component
		{
			result.AddRange(transform.GetComponents<T>());

			foreach (Transform child in transform)
			{
				_GetComponentsInChildren(result, child);
			}
		}

		/// <summary>
		/// This function is a replacement for Unity's regular SetActiveRecursively, which also works on prefabs.
		/// </summary>
		public static void SetActiveRecursively(Transform transform, bool active)
		{
			transform.gameObject.active = active;

			foreach (Transform child in transform)
			{
				SetActiveRecursively(child, active);
			}
		}
	}
}

#endif
