#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using Lidgren.Network;
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// You can use this struct to hold reference to a networked object based on it's viewID. It's specially useful if the object
	/// can move between servers and/or if you use Pikko Server, so there is a chance that object goes out of access and comes back.
	/// </summary>
	/// <typeparam name="TComponent">This should be the type of the component in the object which you are interested in.
	/// If you for example work more with the object's Renderer, it should be Renderer. 
	/// This component will be stored in the <see cref="component"/> property of the object.</typeparam>
	public struct NetworkReference<TComponent> : IEquatable<NetworkReference<TComponent>>, IComparable<NetworkReference<TComponent>>, IComparable
		where TComponent : Component
	{
		//by WuNan @2016/08/27 14:54:43 编译不过，没有引用，暂时注释掉
		//public static readonly NetworkUtility.Comparer<NetworkReference<TComponent>> comparer = NetworkUtility.Comparer<NetworkReference<TComponent>>.comparer;

		/// <summary>
		/// The <see cref="uLink.NetworkViewID"/> of the object that we are referencing.
		/// </summary>
		public readonly NetworkViewID viewID;

		private NetworkView _networkView;
		private TComponent _component;

		/// <summary>
		/// The <see cref="uLink.NetworkView"/> that this reference is pointing to. The object might be destroyed or moved to another server.
		/// </summary>
		public NetworkView networkView
		{
			get
			{
				if (_networkView.IsNullOrUnassigned() || _networkView.viewID != viewID)
				{
					_networkView = NetworkView.Find(viewID);
					_component = null;
				}

				return _networkView;
			}
		}

		/// <summary>
		/// Returns wether the networked object that we are referencing to exists or not.
		/// </summary>
		public bool exists
		{
			get
			{
				return networkView.IsNotNullOrUnassigned();
			}
		}

		/// <summary>
		/// Returns the component in the referenced networked object which we are interested in and provided it's type as generic parameter.
		/// </summary>
		/// <example>
		/// For example if we have a NetworkReference&lt;Transform&gt; then the component property will return its Transform.
		/// </example>
		public TComponent component
		{
			get
			{
				if (!exists) return null;

				if (_component.IsNullOrDestroyed())
				{
					_component = _networkView.GetComponentInChildren<TComponent>();
				}

				return _component;
			}
		}

		/// <summary>
		/// Returns the Game Object that our referenced object is pointing to.
		/// </summary>
		public GameObject gameObject
		{
			get
			{
				return exists ? _networkView.gameObject : null;
			}
		}

		/// <summary>
		/// Returns the transform of the referenced object.
		/// </summary>
		public Transform transform
		{
			get
			{
				return exists ? _networkView.transform : null;
			}
		}

		/// <summary>
		/// Calls the GetComponent method of unity on the referenced object.
		/// </summary>
		/// <typeparam name="T">The type of the component which we want to get.</typeparam>
		/// <returns>The component if found, otherwise null</returns>
		public T GetComponent<T>() where T : Component
		{
			return exists ? _networkView.GetComponent<T>() : null;
		}

		/// <summary>
		/// Returns the result of a GetComponentInChildren call on the referenced object.
		/// </summary>
		/// <typeparam name="T">Type of the component that we are interested in.</typeparam>
		/// <returns>The component if found, otherwise null</returns>
		public T GetComponentInChildren<T>() where T : Component
		{
			return exists ? _networkView.GetComponentInChildren<T>() : null;
		}

		/// <summary>
		/// Creates a NetworkReference instance with the provided component's <see cref="uLink.NetworkView"/> as referenced object.
		/// </summary>
		/// <param name="component">The component that we want to reference its NetworkView and use it our <see cref="component"/> property</param>
		public NetworkReference(TComponent component)
		{
			_component = component;
			_networkView = component.GetComponent<NetworkView>();
			viewID = _networkView.viewID;
		}

		/// <summary>
		/// Creates a NetworkReference instance with the provided <see cref="uLink.NetworkView"/> as referenced object.
		/// The <see cref="component"/> property will be null.
		/// </summary>
		/// <param name="networkView">The NetworkView that we want to reference it as a networked object.</param>
		public NetworkReference(NetworkView networkView)
		{
			viewID = networkView.viewID;
			_networkView = networkView;
			_component = null;
		}

		/// <summary>
		/// Create a NetworkReference instance which points to the object owning the provided <see cref="uLink.NetworkViewID"/>
		/// The <see cref="component"/> property will be null.
		/// </summary>
		/// <param name="viewID">ViewID of the object that we want to set our reference to it.</param>
		public NetworkReference(NetworkViewID viewID)
		{
			this.viewID = viewID;
			_networkView = null;
			_component = null;
		}

		/* TODO: make it serializable
		 * 
		internal NetworkReference(NetBuffer buffer)
		{
			viewID = new NetworkViewID(buffer);
			_networkView = null;
		}

		internal void _Write(NetBuffer buffer)
		{
			viewID._Write(buffer);
		}
		*/

		public static implicit operator bool(NetworkReference<TComponent> self)
		{
			return self.exists;
		}

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkReference"/>s are identical
		/// </summary>
		public static bool operator ==(NetworkReference<TComponent> lhs, NetworkReference<TComponent> rhs) { return lhs.viewID == rhs.viewID; }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkReference"/>s are not identical
		/// </summary>
		public static bool operator !=(NetworkReference<TComponent> lhs, NetworkReference<TComponent> rhs) { return lhs.viewID != rhs.viewID; }

		public static bool operator ==(NetworkReference<TComponent> self, NetworkView other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference<TComponent> self, NetworkView other)
		{
			return self.networkView != other;
		}

		public static bool operator ==(NetworkReference<TComponent> self, TComponent other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference<TComponent> self, TComponent other)
		{
			return self.networkView != other;
		}

		public static bool operator ==(NetworkReference<TComponent> self, Transform other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference<TComponent> self, Transform other)
		{
			return self.networkView != other;
		}

		public static bool operator ==(NetworkReference<TComponent> self, GameObject other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference<TComponent> self, GameObject other)
		{
			return self.networkView != other;
		}

		public static bool operator ==(NetworkReference<TComponent> self, UnityEngine.Object other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference<TComponent> self, UnityEngine.Object other)
		{
			return self.networkView != other;
		}

		/// <summary>
		/// Returns the hash code for this <see cref="uLink.NetworkReference"/>.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this <see cref="uLink.NetworkReference"/>.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode() { return viewID.GetHashCode(); }

		/// <summary>
		/// Indicates whether this <see cref="uLink.NetworkReference"/> and a specified object are equal.
		/// </summary>
		/// <returns>
		/// <c>true</c> if <paramref name="other"/> and this <see cref="uLink.NetworkReference"/> are the same type and represent the same value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="other">Another object to compare to. </param><filterpriority>2</filterpriority>
		public override bool Equals(object other)
		{
			return ((other is NetworkReference<TComponent>) && Equals((NetworkReference<TComponent>)other))
				|| ((other is NetworkView) && Equals((NetworkView)other))
				|| ((other is TComponent) && Equals((TComponent)other))
				|| ((other is Transform) && Equals((Transform)other))
				|| ((other is GameObject) && Equals((GameObject)other))
				|| ((other is UnityEngine.Object) && Equals((UnityEngine.Object)other));
		}

		public bool Equals(NetworkReference<TComponent> other)
		{
			return viewID == other.viewID;
		}

		public bool Equals(TComponent other)
		{
			return component == other;
		}

		public bool Equals(NetworkView other)
		{
			return networkView == other;
		}

		public bool Equals(Transform other)
		{
			return transform == other;
		}

		public bool Equals(GameObject other)
		{
			return gameObject == other;
		}

		public bool Equals(UnityEngine.Object other)
		{
			return networkView == other;
		}

		/// <summary>
		/// Compares this instance with another specified <see cref="uLink.NetworkViewID"/> object and indicates
		/// whether this instance precedes, follows, or appears in the same position
		/// in the sort order as the specified <see cref="uLink.NetworkViewID"/>.
		/// </summary>
		/// <param name="other">The other <see cref="uLink.NetworkViewID"/>.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates whether this instance precedes, follows,
		/// or appears in the same position in the sort order as the value parameter.
		/// </returns>
		public int CompareTo(NetworkReference<TComponent> other)
		{
			return viewID.CompareTo(other.viewID);
		}

		public int CompareTo(object other)
		{
			return CompareTo((NetworkReference<TComponent>)other);
		}

		/// <summary>
		/// Returns a formatted string with details on this <see cref="uLink.NetworkReference"/>.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			return viewID.ToString();
		}
	}
}

#endif
