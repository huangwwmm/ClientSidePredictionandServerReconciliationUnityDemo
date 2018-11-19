#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11825 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-04-11 22:00:40 +0200 (Wed, 11 Apr 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace uLink
{
	/// <summary>
	/// This enum contains the different values which you can set for type safety of RPCs in <see cref="uLink.Network.rpcTypeSafe"/> 
	/// </summary>
	public enum RPCTypeSafe : byte
	{
		/// <summary>
		/// Type safety is turned off and you'll not receive any errors/warnings for sending/receiving variables of different types
		/// </summary>
		Off = 0,
		/// <summary>
		/// Type safety is on in the editor and turned off in built players.
		/// </summary>
		OnlyInEditor = 1,
		/// <summary>
		/// Type safety is always on (not recommended for releases).
		/// </summary>
		Always = 2,
	}

	internal class NetworkUnity : NetworkBase
	{
		public const string EVENT_PREFIX = "uLink_";
		public const string EVENT_INSTANTIATE = EVENT_PREFIX + "OnNetworkInstantiate";

		private RPCTypeSafe _rpcTypeSafe = RPCTypeSafe.OnlyInEditor;

		public RPCTypeSafe rpcTypeSafe
		{
			get
			{
				return _rpcTypeSafe;
			}

			set
			{
				_rpcTypeSafe = value;
				//_UpdateTypeSafe();
			}
		}

		public string licenseKey
		{
			set
			{
				Log.Warning(NetworkLogFlags.None, "uLink doesn't need a license key in this version");
			}

			get
			{
				return "";
			}
		}

		internal void _UpdateTypeSafe()
		{
			if (_rpcTypeSafe == RPCTypeSafe.Always || (_rpcTypeSafe == RPCTypeSafe.OnlyInEditor && UnityEngine.Application.isEditor))
			{
				Log.Debug(NetworkLogFlags.RPC, "Enabling RPC TypeSafety");
				_rpcFlags &= ~NetworkFlags.TypeUnsafe;
			}
			else
			{
				Log.Debug(NetworkLogFlags.RPC, "Disabling RPC TypeSafety");
				_rpcFlags |= NetworkFlags.TypeUnsafe;
			}
		}

		private void _AssertPrefab(GameObject prefab, bool notNull)
		{
			if (prefab.IsNullOrDestroyed())
			{
				if (notNull) throw new ArgumentNullException("prefab");
				return;
			}

			_AssertPrefab(prefab);
		}

		private void _AssertPrefab<T>(T prefab, bool notNull) where T : Component
		{
			if (prefab.IsNullOrDestroyed())
			{
				if (notNull) throw new ArgumentNullException("prefab");
				return;
			}

			_AssertPrefab(prefab.gameObject);
		}

		private void _AssertPrefab(Object prefab, bool notNull)
		{
			if (prefab.IsNullOrDestroyed())
			{
				if (notNull) throw new ArgumentNullException("prefab");
				return;
			}

			if (prefab is GameObject) _AssertPrefab(prefab as GameObject);
			else if (prefab is Component) _AssertPrefab((prefab as Component).gameObject);
			else throw new ArgumentException("Prefab must be either a GameObject or Component", "prefab");
		}

		private void _AssertPrefab(GameObject prefab)
		{
			if (prefab.GetComponent<NetworkView>().IsNullOrDestroyed())
			{
				throw new ArgumentException("prefab '" + prefab.GetName() + "' is missing a root NetworkView component", "prefab");
			}
		}

		private static GameObject _GetGameObject(NetworkViewBase nv)
		{
			return !nv.IsNullOrDestroyed() ? nv.gameObject : null;
		}

		private static T _GetComponent<T>(NetworkViewBase nv) where T : Component
		{
			return !nv.IsNullOrDestroyed() ? nv.GetComponent<T>() : null;
		}

		private static Object _GetComponent(Type type, NetworkViewBase nv)
		{
			if (nv.IsNullOrDestroyed()) return null;
			return type == typeof(GameObject) ? nv.gameObject : nv.GetComponent(type) as Object;
		}

		public new GameObject Instantiate(string prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			if(!(!String.IsNullOrEmpty(prefab))){Utility.Exception( "prefab must be a non-empty string");}
			return _GetGameObject(base.Instantiate(prefab, position, rotation, group, initialData));
		}

		public new GameObject Instantiate(string proxyPrefab, string ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			return _GetGameObject(base.Instantiate(proxyPrefab, ownerPrefab, position, rotation, group, initialData));
		}

		public new GameObject Instantiate(NetworkPlayer owner, string prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			if(!(!String.IsNullOrEmpty(prefab))){Utility.Exception( "prefab must be a non-empty string");}
			return _GetGameObject(base.Instantiate(owner, prefab, position, rotation, group, initialData));
		}

		public new GameObject Instantiate(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			return _GetGameObject(base.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData));
		}

		public new GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			return _GetGameObject(base.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, position, rotation, group, initialData));
		}

		public new GameObject Instantiate(NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			return _GetGameObject(base.Instantiate(owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData));
		}

		public new GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, string proxyPrefab, string ownerPrefab, string serverPrefab, string cellAuthPrefab, string cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			return _GetGameObject(base.Instantiate(viewID, owner, proxyPrefab, ownerPrefab, serverPrefab, cellAuthPrefab, cellProxyPrefab, authFlags, position, rotation, group, initialData));
		}

		public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			_AssertPrefab(prefab, true);
			return _GetGameObject(base.Instantiate(prefab.GetName(), position, rotation, group, initialData));
		}

		public GameObject Instantiate(GameObject othersPrefab, GameObject ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			_AssertPrefab(othersPrefab, false);
			_AssertPrefab(ownerPrefab, false);
			return _GetGameObject(base.Instantiate(othersPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), position, rotation, group, initialData));
		}

		public GameObject Instantiate(NetworkPlayer owner, GameObject prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			_AssertPrefab(prefab, true);
			return _GetGameObject(base.Instantiate(owner, prefab.GetName(), position, rotation, group, initialData));
		}

		public GameObject Instantiate(NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			_AssertPrefab(proxyPrefab, false);
			_AssertPrefab(ownerPrefab, false);
			_AssertPrefab(serverPrefab, false);
			return _GetGameObject(base.Instantiate(owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), position, rotation, group, initialData));
		}

		public GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			_AssertPrefab(proxyPrefab, false);
			_AssertPrefab(ownerPrefab, false);
			_AssertPrefab(serverPrefab, false);
			return _GetGameObject(base.Instantiate(viewID, owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), position, rotation, group, initialData));
		}

		public GameObject Instantiate(NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, GameObject cellAuthPrefab, GameObject cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			return _GetGameObject(base.Instantiate(owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), cellAuthPrefab.GetNameNullSafe(), cellProxyPrefab.GetNameNullSafe(), authFlags, position, rotation, group, initialData));
		}

		public GameObject Instantiate(NetworkViewID viewID, NetworkPlayer owner, GameObject proxyPrefab, GameObject ownerPrefab, GameObject serverPrefab, GameObject cellAuthPrefab, GameObject cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			return _GetGameObject(base.Instantiate(viewID, owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), cellAuthPrefab.GetNameNullSafe(), cellProxyPrefab.GetNameNullSafe(), authFlags, position, rotation, group, initialData));
		}

		public TComponent Instantiate<TComponent>(TComponent prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component
		{
			_AssertPrefab(prefab, true);
			return _GetComponent<TComponent>(base.Instantiate(prefab.GetName(), position, rotation, group, initialData));
		}

		public TComponent Instantiate<TComponent>(TComponent othersPrefab, TComponent ownerPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component
		{
			_AssertPrefab(othersPrefab, false);
			_AssertPrefab(ownerPrefab, false);
			return _GetComponent<TComponent>(base.Instantiate(othersPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), position, rotation, group, initialData));
		}

		public TComponent Instantiate<TComponent>(NetworkPlayer owner, TComponent prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component
		{
			_AssertPrefab(prefab, true);
			return _GetComponent<TComponent>(base.Instantiate(owner, prefab.GetName(), position, rotation, group, initialData));
		}

		public TComponent Instantiate<TComponent>(NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component
		{
			_AssertPrefab(proxyPrefab, false);
			_AssertPrefab(ownerPrefab, false);
			_AssertPrefab(serverPrefab, false);
			return _GetComponent<TComponent>(base.Instantiate(owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), position, rotation, group, initialData));
		}

		public TComponent Instantiate<TComponent>(NetworkViewID viewID, NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component
		{
			_AssertPrefab(proxyPrefab, false);
			_AssertPrefab(ownerPrefab, false);
			_AssertPrefab(serverPrefab, false);
			return _GetComponent<TComponent>(base.Instantiate(viewID, owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), position, rotation, group, initialData));
		}

		public TComponent Instantiate<TComponent>(NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, TComponent cellAuthPrefab, TComponent cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component
		{
			return _GetComponent<TComponent>(base.Instantiate(owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), cellAuthPrefab.GetNameNullSafe(), cellProxyPrefab.GetNameNullSafe(), authFlags, position, rotation, group, initialData));
		}

		public TComponent Instantiate<TComponent>(NetworkViewID viewID, NetworkPlayer owner, TComponent proxyPrefab, TComponent ownerPrefab, TComponent serverPrefab, TComponent cellAuthPrefab, TComponent cellProxyPrefab, NetworkAuthFlags authFlags, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData) where TComponent : Component
		{
			return _GetComponent<TComponent>(base.Instantiate(viewID, owner, proxyPrefab.GetNameNullSafe(), ownerPrefab.GetNameNullSafe(), serverPrefab.GetNameNullSafe(), cellAuthPrefab.GetNameNullSafe(), cellProxyPrefab.GetNameNullSafe(), authFlags, position, rotation, group, initialData));
		}

		public Object Instantiate(Object prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			_AssertPrefab(prefab, true);
			return _GetComponent(prefab.GetType(), base.Instantiate(prefab.GetName(), position, rotation, group, initialData));
		}

		public Object Instantiate(NetworkPlayer owner, Object prefab, Vector3 position, Quaternion rotation, NetworkGroup group, params object[] initialData)
		{
			_AssertPrefab(prefab, true);
			return _GetComponent(prefab.GetType(), base.Instantiate(owner, prefab.GetName(), position, rotation, group, initialData));
		}

		public bool Destroy(GameObject gameObject)
		{
			NetworkView nv = NetworkView.Get(gameObject);
			return !nv.IsNullOrDestroyed() && Destroy(nv);
		}

		internal void _AddAllNetworkViews()
		{
			Log.Debug(NetworkLogFlags.NetworkView, "Searching for all NetworkView(s) in current scene");

			// ReSharper disable PossibleNullReferenceException
			var views = Object.FindObjectsOfType(typeof(NetworkView)) as NetworkView[];

			Log.Info(NetworkLogFlags.NetworkView, "Found ", views.Length, " NetworkView(s) in current scene");

			foreach (NetworkView nv in views)
			{
				if (nv.viewID.isManual) //this if statement is a bugfix for handovers. Found old dynamic objects from the old game server here because network instantiated objects had not yet been cleaned up by Unity when client connected to new game server.
				{
					AddNetworkView(nv);
				}
			}
			// ReSharper restore PossibleNullReferenceException
		}

		protected override void OnStart()
		{
			InternalHelper._AddOneHelper();
			_AddAllNetworkViews();
		}

		protected override NetworkViewBase OnCreate(string prefabName, NetworkInstantiateArgs args, NetworkMessageInfo info)
		{
			var instantiator = NetworkInstantiator.Find(prefabName);
			var creator = instantiator.creator;

			if (creator != null)
			{
				Log.Debug(NetworkLogFlags.Instantiate, "Calling creator for prefab '", prefabName, "' with viewID ", args.viewID, " (in ", args.group, "), owner ", args.owner, ", position ", args.position, ", rotation ", args.rotation);

				Profiler.BeginSample("Calling Creator");
				var nv = creator(prefabName, args, info);
				Profiler.EndSample();

				if (!nv.IsNullOrDestroyed())
				{
					nv.instantiator = instantiator;

					if (nv.viewID != args.viewID)
					{
						Log.Warning(NetworkLogFlags.Instantiate, "Creator failed to correctly setup the ", nv, ", which is it's responsibility. Please make sure your custom NetworkInstantiator is calling NetworkInstantiatorUtility.Instantiate or NetworkInstantiateArgs.SetupNetworkView.");
					}

					return nv;
				}

				Log.Error(NetworkLogFlags.Instantiate, "Creator for prefab '", prefabName, "' failed to return a instantiated NetworkView!");
				return null;
			}

			Log.Error(NetworkLogFlags.Instantiate, "Missing Creator for prefab '", prefabName, "'");
			return null;
		}

		protected override void OnDestroy(NetworkViewBase networkView)
		{
			if (networkView.IsNullOrDestroyed())
			{
				Log.Warning(NetworkLogFlags.Instantiate, "Object is already destroyed, the network destroyer will be skipped. Please call uLink.Network.Destroy instead of Object.Destroy.");
				return;
			}

			var nv = networkView as NetworkView;
			var destroyer = nv.instantiator.destroyer;

			if (destroyer != null)
			{
				Log.Debug(NetworkLogFlags.Instantiate, "Calling destroyer for ", nv, " with viewID ", nv.viewID, " (in ", nv.group, "), owner ", nv.owner);

				Profiler.BeginSample("Calling Destroyer");
				destroyer(nv);
				Profiler.EndSample();
			}
			else
			{
				Log.Error(NetworkLogFlags.Instantiate, "Missing Destroyer for ", nv);
			}
		}

		protected override void OnEvent(string eventName, object value)
		{
			string methodName = EVENT_PREFIX + eventName;

			Profiler.BeginSample(methodName);

			// ReSharper disable PossibleNullReferenceException
			var gos = Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];

			foreach (GameObject go in gos)
			{
				go.SendMessage(methodName, value, SendMessageOptions.DontRequireReceiver);
			}
			// ReSharper restore PossibleNullReferenceException

			Profiler.EndSample();
		}

		internal override string OnGetPlatform()
		{
			return "Unity " + UnityEngine.Application.unityVersion + " " + UnityEngine.Application.platform;
		}

		public new bool HavePublicAddress()
		{
			if (UnityEngine.Application.webSecurityEnabled)
			{
				var unityNetworkType = Utility.GetUnityEngineNetworkType();
				if (unityNetworkType == null)
				{
					Log.Error(NetworkLogFlags.Utility, "Unity Web Security is enabled, but can't find UnityEngine.Network to workaround it");
					return false;
				}

				try
				{
					const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
					var method = unityNetworkType.GetMethod("HavePublicAddress", flags, null, new Type[0], null);

					return (bool)method.Invoke(null, null);
				}
				catch (Exception ex)
				{
					Log.Error(NetworkLogFlags.Utility, "Unity Web Security is enabled, but can't call UnityEngine.Network.HavePublicAddress() to workaround it:\n", ex);
					return false;
				}
			}

			return base.HavePublicAddress();
		}
	}
}

#endif
