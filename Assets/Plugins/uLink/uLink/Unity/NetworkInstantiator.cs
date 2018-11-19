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
	/// A registry for instantiators for prefabs. 
	/// </summary>
	/// <remarks>
	/// Most common usage of this class is to use the methods <see cref="AddPrefab(UnityEngine.GameObject)"/>
	/// to register prefabs downloaded in an asset bundle before they can be instantiated by calling
	/// <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>.
	/// <para>
	/// The methods here are also used by the utility script uLinkInstantiatePool.cs to make
	/// it possible to configure a pool of network-instantiated game objects without having to change 
	/// any code at all.
	/// </para>
	/// <para>
	/// The <see cref="uLink.NetworkView.instantiator"/> also references the instantiator of the prefab which the network view is attached to.
	/// It can be used to customize the instantiation and destruction of gameObjects
	/// when you call <see cref="o:uLink.Network.Instantiate"/> and <see cref="uLink.Network.Destroy"/>.
	/// For example if you want to first run an animation and then destroy the object
	/// you can create a custom destroyer.
	/// The utility script, OverrideNetworkDestroy can be used also to easily do custom stuff when an object is destroyed
	/// on the network.
	/// </para>
	/// </remarks>
	public struct NetworkInstantiator
	{
		/// <summary>
		/// Signature for creator methods, Creators are used to Instantiate objects when you receive an instantiation RPC
		/// on the network.
		/// </summary>
		public delegate NetworkView Creator(string prefabName, NetworkInstantiateArgs args, NetworkMessageInfo info);

		/// <summary>
		/// Signature for destroyer methods, Destroyer methods are used to destroy
		/// objects when you receive a destruction RPC on the network.
		/// </summary>
		public delegate void Destroyer(NetworkView networkView);

		/// <summary>
		/// This is the method which is called when a client receives an RPC sent by <see cref="o:uLink.Network.Instantiate"/> 
		/// to instantiate a prefab as a network aware object. You can change this to your own method to customize the process.
		/// </summary>
		/// <remarks>
		/// This field is usually accessed for each network aware object using <see cref="uLink.NetworkView.instantiator"/>.
		/// </remarks>
		/// <example>See the utility script OverrideNetworkDestroy for an example.
		/// </example>
		public Creator creator;
		
		/// <summary>
		/// This is the method which is called when a client receives an RPC sent by <see cref="Network.Destroy"/> 
		/// to destroy a prefab as a network aware object. You can change this to your own method to customize the process.
		/// </summary>
		/// <remarks>
		/// This field is usually accessed for each network aware object using <see cref="uLink.NetworkView.instantiator"/>.
		/// </remarks>
		/// <example>See the utility script OverrideNetworkDestroy for an example.
		/// You can call the default behaviour <see cref="defaultDestroyer"/> at the end of your custom method as well.
		/// </example>
		public Destroyer destroyer;

		/// <summary>
		/// The default method for destroying network aware objects. This is the method which is called by default if you
		/// don't change the <see cref="destroyer"/> delegate.
		/// This simply destroys the object.
		/// </summary>
		public static readonly Destroyer defaultDestroyer = delegate(NetworkView networkView)
		{
			Profiler.BeginSample("Destroy: " + networkView.ToPrefabString());
			NetworkInstantiatorUtility.Destroy(networkView);
			Profiler.EndSample();
		};

		private static readonly System.Collections.Generic.Dictionary<string, NetworkInstantiator> _instantiators = new System.Collections.Generic.Dictionary<string, NetworkInstantiator>();

		internal NetworkInstantiator(Creator creator, Destroyer destroyer)
		{
			this.creator = creator;
			this.destroyer = destroyer;
		}


		/// <summary>
		/// Registers all prefabs in the asset bundle that does have a NetworkView component.   
		/// This makes sure they can be instantiated later with 
		/// <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>
		/// </summary>
		/// <param name="assetBundle">The asset bundle that you want to register its prefabs.</param>
		/// <remarks>
		/// This is a convenience method that calls <see cref="O:uLink.NetworkInstantiator.AddPrefab"/>
		/// for all prefabs in the asset bundle that has a network view component.
		/// <para>
		/// It is common for developers to minimize the download time for the Unity game at startup by putting
		/// some prefabs in asset bundles and then download the asset bundles when needed in the game. 
		/// We recommend writing code that downloads the asset bundle, loads the prefabs, registers them with this method, 
		/// and finally makes the calls to <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>. 
		/// If it is an authoritative server the call to <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>
		/// has to be done on the server.
		/// </para>
		/// </remarks>
		public static void AddAssetBundle(AssetBundle assetBundle)
		{
			AddAssetBundle(assetBundle, false);
		}

		/// <summary>
		/// Registers all prefabs in the asset bundle that does have a NetworkView component.   
		/// This makes sure they can be instantiated later with 
		/// <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>
		/// </summary>
		/// <param name="assetBundle">The asset bundle that you want to register its prefabs.</param>
		/// <param name="replaceIfExists">if <c>true</c> then prefabs with the same name will be replaced by the new ones in the asset bundle.</param>
		/// <remarks>
		/// This is a convenience method that calls <see cref="O:uLink.NetworkInstantiator.AddPrefab"/>
		/// for all prefabs in the asset bundle that has a network view component.
		/// <para>
		/// It is common for developers to minimize the download time for the Unity game at startup by putting
		/// some prefabs in asset bundles and then download the asset bundles when needed in the game. 
		/// We recommend writing code that downloads the asset bundle, loads the prefabs, registers them with this method, 
		/// and finally makes the calls to <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>. 
		/// If it is an authoritative server the call to <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>
		/// has to be done on the server.
		/// </para>
		/// </remarks>
		public static void AddAssetBundle(AssetBundle assetBundle, bool replaceIfExists)
		{
#if UNITY_4
			var prefabs = assetBundle.LoadAll(typeof(GameObject)) as GameObject[];
#else
			var prefabs = assetBundle.LoadAllAssets<GameObject>();
#endif

			foreach (var prefab in prefabs)
			{
				var views = NetworkInstantiatorUtility.GetComponentsInChildren<NetworkView>(prefab.transform);
				if (views.Count != 0)
				{
					Add(prefab.GetName(), _CreateDefault(prefab, views), replaceIfExists);
				}
			}
		}

		/// <summary>
		/// Registers a prefab to make sure it can be instantiated 
		/// later with <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>
		/// by creating and adding a default instantiator for the specified gameobject. 
		/// </summary>
		/// <remarks>
		/// With this uLink feature it is possible to register prefabs 
		/// that are located anywhere in the project. 
		/// The old uLink restriction that all prefabs must reside in the Resources folder is overruled.
		/// Place the prefab in any folder in the project, but remember to call this method on all 
		/// clients and the server before calling <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>.
		/// <para>
		/// When the server is authoritative, registration on the clients usually includes the prefab for 
		/// proxies and the owner. Registration on the server includes only the prefab for the creator.
		/// Keep in mind that registering prefabs using this method means you have a reference
		/// to the object so the object will be built with your scene.
		/// So don't waste memory by referencing all prefabs in both clients and servers (at production time).
		/// </para>
		/// <para>
		/// There is also a convenient utility script called uLinkRegisterPrefabs that calls this method for you.
		/// Read more about that script in the uLink manual chapter for instantiating objects.
		/// </para>
		/// <para>
		/// It is common for developers to minimize the download time for the Unity game at startup by putting
		/// some prefabs in assert bundles and then download the asset bundles when needed in the game. 
		/// We recommend writing code that downloads the asset bundle, loads the prefabs, registers them with 
		/// <see cref="O:uLink.NetworkInstantiator.AddAssetBundle"/>, 
		/// and finally makes the calls to <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>.
		/// If it is an authoritative server the call to <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>
		/// has to be done on the server.
		/// </para>
		/// </remarks>
		public static void AddPrefab(GameObject prefab)
		{
			AddPrefab(prefab, false);
		}

		/// <summary>
		/// Registers a prefab to make sure it can be instantiated 
		/// later with <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>
		/// by creating and adding a default instantiator for the specified gameobject.  
		/// Read remarks <see cref="AddPrefab(UnityEngine.GameObject)">here</see>
		/// </summary>
		public static void AddPrefab(GameObject prefab, bool replaceIfExists)
		{
			Add(prefab.GetName(), _CreateDefault(prefab), replaceIfExists);
		}

		/// <summary>
		/// Creates a prefab instantiator with custom methods for instantiation and destruction.
		/// </summary>
		/// <param name="prefabName">Name of the prefab to instantiate</param>
		/// <param name="creator">The method which should be called for prefab creation</param>
		/// <param name="destroyer">The method which should be called for prefab destruction</param>
		/// <remarks>
		/// The reference to the prefab itself is not sent to you at creation time so either you need to load it from
		/// resources folder or find it by some other way.
		/// This method can be used in creative ways but it's very advanced and you should only use it when you really need it.
		/// You can for example find the player's pet based on database values and instantiate the correct pet when an
		/// instantiate call with pet as prefab is cone. Keep in mind that you should not do slow operations here.
		/// Read the value from database in some other time (e.g. login time) and store it somewhere.
		/// </remarks>
		public static void Add(string prefabName, Creator creator, Destroyer destroyer)
		{
			Add(prefabName, creator, destroyer, false);
		}

		/// <summary>
		/// Creates a prefab instantiator with custom methods for instantiation and destruction.
		/// </summary>
		/// <param name="prefabName">Name of the prefab to instantiate</param>
		/// <param name="creator">The method which should be called for prefab creation</param>
		/// <param name="destroyer">The method which should be called for prefab destruction</param>
		/// <param name="replaceIfExists">If set to <c>true</c> then if a prefab with the same name exists, it will be replaced, otherwise an error log will be printed.</param>
		/// <remarks>
		/// The reference to the prefab itself is not sent to you at creation time so either you need to load it from
		/// resources folder or find it by some other way.
		/// This method can be used in creative ways but it's very advanced and you should only use it when you really need it.
		/// You can for example find the player's pet based on database values and instantiate the correct pet when an
		/// instantiate call with pet as prefab is cone. Keep in mind that you should not do slow operations here.
		/// Read the value from database in some other time (e.g. login time) and store it somewhere.
		/// </remarks>
		public static void Add(string prefabName, Creator creator, Destroyer destroyer, bool replaceIfExists)
		{
			Add(prefabName, new NetworkInstantiator(creator, destroyer), replaceIfExists);
		}

		private static void Add(string prefabName, NetworkInstantiator instantiator, bool replaceIfExists)
		{
			if (!replaceIfExists && _instantiators.ContainsKey(prefabName))
			{
				Log.Error(NetworkLogFlags.Instantiate, "Instantiator for prefab '", prefabName, "' already exists");
				return;
			}

			_instantiators[prefabName] = instantiator;

			Log.Debug(NetworkLogFlags.Instantiate, "Added Instantiator for prefab '", prefabName, "'");
		}

		/// <summary>
		/// Removes a instantiator. If this instantiator was added by <see cref="AddPrefab(UnityEngine.GameObject)"/>
		/// and the specific gameobject is not placed in a Resources-folder then that gameobject can no longer be used
		/// with <see cref="O:uLink.Network.Instantiate">uLink.Network.Instantiate</see>.
		/// </summary>
		/// <remarks>
		/// It is nice to be able to remove all references to a prefab to make it possible 
		/// to unload it and reduce memory footprint in the Unity player. This can for example
		/// be used in a web player when switching from one scene to another or when switching from 
		/// one game mode to another. Read more in the Unity manual about garbage collection and 
		/// unloading prefabs. 
		/// </remarks>
		public static void Remove(string prefabName)
		{
			_instantiators.Remove(prefabName);
		}

		/// <summary>
		/// Removes all instantiators. See also <see cref="Remove"/>.
		/// </summary>
		public static void RemoveAll()
		{
			_instantiators.Clear();
		}

		/// <summary>
		/// Used internally by uLink when executing Network.Instantiate, but it is also public to make it easier to debug problems with NetworkInstantiator.
		/// It finds and returns the Instantiator for the prefab with the name provided,
		/// </summary>
		public static NetworkInstantiator Find(string prefabName)
		{
			NetworkInstantiator instantiator;

			if (_instantiators.TryGetValue(prefabName, out instantiator))
				return instantiator;

			instantiator = _CreateDefault(prefabName);
			_instantiators.Add(prefabName, instantiator);
			return instantiator;
		}

		/// <summary>
		/// Creates the default <see cref="Creator"/> for the prefab and returns it.
		/// </summary>
		/// <param name="prefabName">Name of the prefab that you want to create a creator for.</param>
		/// <returns>The creator method.</returns>
		public static Creator CreateDefaultCreator(string prefabName)
		{
			var prefab = Resources.Load(prefabName, typeof(GameObject)) as GameObject;
			if (prefab.IsNullOrDestroyed())
			{
				Log.Error(NetworkLogFlags.Instantiate, "Prefab '", prefabName, "' is not registered or placed in the Resources folder");
				return null;
			}

			return CreateDefaultCreator(prefab);
		}

		/// <summary>
		/// Creates the default <see cref="Creator"/> for the prefab and returns it.
		/// </summary>
		/// <param name="prefab">The prefab that you want to create a creator for.</param>
		/// <returns>The creator method.</returns>
		public static Creator CreateDefaultCreator(GameObject prefab)
		{
			var views = NetworkInstantiatorUtility.GetComponentsInChildren<NetworkView>(prefab.transform);
			if (views.Count == 0)
			{
				Log.Error(NetworkLogFlags.Instantiate, "Prefab '", prefab.GetName(), "' must at least have one NetworkView");
				return null;
			}

			return _CreateDefaultCreator(prefab, views);
		}


		private static Creator _CreateDefaultCreator(GameObject prefab, List<NetworkView> views)
		{
			var parent = views[0]; // the first NetworkView is the parent.

			if (views.Count > 1)
			{
				var children = new NetworkView[views.Count - 1];
				views.CopyTo(1, children, 0, children.Length);

				return _CreateDefaultCreator(prefab, parent, children);
			}

			return _CreateDefaultCreator(prefab, parent, null);
		}

		private static Creator _CreateDefaultCreator(GameObject prefab, NetworkView parent, NetworkView[] children)
		{
			if (children != null && children.Length != 0)
			{
				Log.Info(NetworkLogFlags.Instantiate, "Setting up parent-child relationships in prefab '", prefab.GetName(), "' because it has multiple NetworkViews");

				parent.SetChildren(children);
			}
			else
			{
				parent._parent = null; // ensure it is null, because Unity 5.x (or later) might assign a "null object" which is not technically null.
			}

			// we assume the prefab isn't in the scene, otherwise it could be a child and strange things could happen.
			parent.prefabRoot = prefab.transform.root.gameObject;

			return delegate(string prefabName, NetworkInstantiateArgs args, NetworkMessageInfo info)
				{
					Profiler.BeginSample("Instantiate: " + prefabName);
					var networkView = NetworkInstantiatorUtility.Instantiate(parent, args);
					Profiler.EndSample();

					Profiler.BeginSample(NetworkUnity.EVENT_INSTANTIATE);
					NetworkInstantiatorUtility.BroadcastOnNetworkInstantiate(networkView, info);
					Profiler.EndSample();

					return networkView;
				};
		}

		private static NetworkInstantiator _CreateDefault(string prefabName)
		{
			return new NetworkInstantiator(CreateDefaultCreator(prefabName), defaultDestroyer);
		}

		private static NetworkInstantiator _CreateDefault(GameObject prefab)
		{
			return new NetworkInstantiator(CreateDefaultCreator(prefab), defaultDestroyer);
		}

		private static NetworkInstantiator _CreateDefault(GameObject prefab, List<NetworkView> views)
		{
			return new NetworkInstantiator(_CreateDefaultCreator(prefab, views), defaultDestroyer);
		}
	}
}

#endif
