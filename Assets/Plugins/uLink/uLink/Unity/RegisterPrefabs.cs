// (c)2011 Unity Park. All Rights Reserved.
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System.Collections.Generic;
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// Use this script component to register the prefabs that will be
	/// used in <see cref="O:uLink.Network.Instantiate"/>. 
	///
	/// If all your prefabs are placed in the Resources folder, there is no need to use this
	/// utility script. 
	///
	/// If you want to register prefabs at run-time, for example after downloading an asset 
	/// bundle in a client, check out <see cref="uLink.NetworkInstantiator"/> in the API doc.
	///
	/// Read more about registering prefabs in the manual chapter about Instantiating Objects.
	/// </summary>
	[AddComponentMenu("uLink Basics/Register Prefabs")]
	public class RegisterPrefabs : MonoBehaviour
	{
		[SerializeField]
		public List<GameObject> prefabs = new List<GameObject>();

		[SerializeField]
		public bool replaceIfExists = false;

		protected void Awake()
		{
			if (enabled) Register();
		}

		protected void Start()
		{
			// dummy
		}

		public void Register()
		{
			foreach (GameObject prefab in prefabs)
			{
				if (!prefab.IsNullOrDestroyed()) uLink.NetworkInstantiator.AddPrefab(prefab, replaceIfExists);
			}
		}
	}
}

#endif
