#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10139 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:11:15 +0100 (Tue, 29 Nov 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// Base class to inherit from when you do C# scripts that need to access
	/// the neworkView for a GameObject. 
	/// </summary>
	/// <remarks>It is very convenient to write C# scripts that have access to the property with
	/// the name networkView. To use this functionality, make your script class extend 
	/// uLink.MonoBehavior instead of using the MonoBehaviour included in Unity. In addition,
	/// the two properties networkP2P and role will be accessible. Also transform and gameObject properties will cache their results and will be faster than Unity's ones.</remarks>
	[AddComponentMenu("")]
	public class MonoBehaviour : UnityEngine.MonoBehaviour
	{
		[NonSerialized]
		private GameObject _gameObject = null;

		[NonSerialized]
		private Transform _transform = null;

		[NonSerialized]
		private NetworkView _networkView = null;

		[NonSerialized]
		private NetworkP2P _networkP2P = null;

		/// <summary>
		/// Gets the GameObject fast without calling native code.
		/// </summary>
		public new GameObject gameObject
		{
			get
			{
				if (_gameObject.IsNullOrDestroyed())
				{
					_gameObject = base.gameObject;
				}

				return _gameObject;
			}
		}

		/// <summary>
		/// Gets the transform fast without a lookup and without calling native code.
		/// </summary>
		public new Transform transform
		{
			get
			{
				if (_transform.IsNullOrDestroyed())
				{
					_transform = base.transform;
				}

				return _transform;
			}
		}
		
		/// <summary>
		/// Gets the network view fast without a lookup and without calling native code.
		/// </summary>
		public new NetworkView networkView
		{
			get
			{
				if (_networkView.IsNullOrDestroyed())
				{
					_networkView = NetworkView.Get(this);
				}

				return _networkView;
			}
		}

		/// <summary>
		/// Gets the networkP2P fast without a lookup and without calling native code.
		/// </summary>
		public NetworkP2P networkP2P
		{
			get
			{
				if (_networkP2P.IsNullOrDestroyed())
				{
					_networkP2P = NetworkP2P.Get(this);
				}

				return _networkP2P;
			}
		}

		/// <summary>
		/// Gets the role this peer/host has for this GameObject.
		/// </summary>
		[Obsolete("MonoBehaviour.role is deprecated, please use NetworkView.isOwner or NetworkView.isProxy instead.", true)]
		public NetworkRole role { get { return 0; } }

		public string ToHierarchyString()
		{
			return Utility.ToHierarchyString(this);
		}


	}
}

#endif
