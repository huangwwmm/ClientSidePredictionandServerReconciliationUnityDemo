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
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// You can use this struct to hold reference to a networked object based on it's viewID. It's specially useful if the object
	/// can move between servers and/or if you use Pikko Server, so there is a chance that object goes out of access and comes back.
	/// </summary>
	///<remarks>The generic version is prefered whenever possible.</remarks>
	public struct NetworkReference : IEquatable<NetworkReference>, IComparable<NetworkReference>, IComparable
	{
		//by WuNan @2016/08/27 14:54:43 编译不过，没有引用，暂时注释掉
		//public static readonly NetworkUtility.Comparer<NetworkReference> comparer = NetworkUtility.Comparer<NetworkReference>.comparer;

		/// <summary>
		/// The <see cref="uLink.NetworkViewID"/> of the object that we hold a reference to.
		/// </summary>
		public readonly NetworkViewID viewID;
		private NetworkView _networkView;

		/// <summary>
		/// The <see cref="uLink.NetworkView"/> component of the networked object which we hold reference to.
		/// </summary>
		public NetworkView networkView
		{
			get
			{
				if (_networkView.IsNullOrUnassigned() || _networkView.viewID != viewID)
				{
					_networkView = NetworkView.Find(viewID);
				}

				return _networkView;
			}
		}

		/// <summary>
		/// Returns if this networked object still exists or not. The object might be destroyed or moved to another server.
		/// </summary>
		public bool exists
		{
			get
			{
				return networkView.IsNotNullOrUnassigned();
			}
		}

		/// <summary>
		/// The GameObject of the object that we reference to.
		/// </summary>
		public GameObject gameObject
		{
			get
			{
				return exists ? _networkView.gameObject : null;
			}
		}

		/// <summary>
		/// The Transform component of the object that we are referencing to.
		/// </summary>
		public Transform transform
		{
			get
			{
				return exists ? _networkView.transform : null;
			}
		}

		/// <summary>
		/// Calls GetComponent on the object that we are referencing.
		/// </summary>
		/// <typeparam name="T">Type of the component that we want to get.</typeparam>
		/// <returns>The component if found, otherwise null</returns>
		public T GetComponent<T>() where T : Component
		{
			return exists ? _networkView.GetComponent<T>() : null;
		}

		/// <summary>
		/// Calls a GetComponentInChildren in the gameObject that we are referencing.
		/// </summary>
		/// <typeparam name="T">Type of the component that we want to get.</typeparam>
		/// <returns>The component if found, otherwise null</returns>
		public T GetComponentInChildren<T>() where T : Component
		{
			return exists ? _networkView.GetComponentInChildren<T>() : null;
		}

		/// <summary>
		/// Creates a NetworkReference object.
		/// </summary>
		/// <param name="networkView">The NetworkView that we want to reference.</param>
		public NetworkReference(NetworkView networkView)
		{
			viewID = networkView.viewID;
			_networkView = networkView;
		}

		/// <summary>
		/// Creates a NetworkReference object.
		/// </summary>
		/// <param name="viewID">ViewID of the <see cref="uLink.NetworkView"/> that we want to reference.</param>
		public NetworkReference(NetworkViewID viewID)
		{
			this.viewID = viewID;
			_networkView = null;
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

		public static implicit operator bool(NetworkReference self)
		{
			return self.exists;
		}

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkReference"/>s are identical
		/// </summary>
		public static bool operator ==(NetworkReference lhs, NetworkReference rhs) { return lhs.viewID == rhs.viewID; }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkReference"/>s are not identical
		/// </summary>
		public static bool operator !=(NetworkReference lhs, NetworkReference rhs) { return lhs.viewID != rhs.viewID; }

		public static bool operator ==(NetworkReference self, NetworkView other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference self, NetworkView other)
		{
			return self.networkView != other;
		}

		public static bool operator ==(NetworkReference self, Transform other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference self, Transform other)
		{
			return self.networkView != other;
		}

		public static bool operator ==(NetworkReference self, GameObject other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference self, GameObject other)
		{
			return self.networkView != other;
		}

		public static bool operator ==(NetworkReference self, UnityEngine.Object other)
		{
			return self.networkView == other;
		}

		public static bool operator !=(NetworkReference self, UnityEngine.Object other)
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
			return ((other is NetworkReference) && Equals((NetworkReference)other))
				|| ((other is NetworkView) && Equals((NetworkView)other))
				|| ((other is Transform) && Equals((Transform)other))
				|| ((other is GameObject) && Equals((GameObject)other))
				|| ((other is UnityEngine.Object) && Equals((UnityEngine.Object)other));
		}

		public bool Equals(NetworkReference other)
		{
			return viewID == other.viewID;
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
		public int CompareTo(NetworkReference other)
		{
			return viewID.CompareTo(other.viewID);
		}

		public int CompareTo(object other)
		{
			return CompareTo((NetworkReference)other);
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
