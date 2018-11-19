#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion
using System;
using Lidgren.Network;

namespace uLink
{
	internal struct NetworkTypeHandle : IEquatable<NetworkTypeHandle>
	{
		//by WuNan @2016/08/27 14:54:43 编译不过，没有引用，暂时注释掉
		//public static readonly NetworkUtility.EqualityComparer<NetworkTypeHandle> comparer = NetworkUtility.EqualityComparer<NetworkTypeHandle>.comparer;

		public static readonly NetworkTypeHandle unassigned = new NetworkTypeHandle();

		public readonly RuntimeTypeHandle typeHandle;

		public Type type { get { return Type.GetTypeFromHandle(typeHandle); } }
		
		public NetworkTypeHandle(RuntimeTypeHandle typeHandle)
		{
			this.typeHandle = typeHandle;
		}

		public NetworkTypeHandle(Type type)
			: this(type.TypeHandle)
		{}

		/*
		internal NetworkTypeHandle(NetBuffer buffer)
		{
			//TODO
		}

		internal void _Write(NetBuffer buffer)
		{
			//TODO
		}
		*/

		public static implicit operator RuntimeTypeHandle(NetworkTypeHandle self) { return self.typeHandle; }
		public static implicit operator Type(NetworkTypeHandle self) { return self.type; }

		public static implicit operator NetworkTypeHandle(RuntimeTypeHandle typeHandle) { return new NetworkTypeHandle(typeHandle); }
		public static implicit operator NetworkTypeHandle(Type type) { return new NetworkTypeHandle(type); }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkTypeHandle"/>s are identical
		/// </summary>
		public static bool operator ==(NetworkTypeHandle lhs, NetworkTypeHandle rhs) { return lhs.Equals(rhs); }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkTypeHandle"/>s are not identical
		/// </summary>
		public static bool operator !=(NetworkTypeHandle lhs, NetworkTypeHandle rhs) { return !lhs.Equals(rhs); }

		/// <summary>
		/// Returns the hash code for this <see cref="uLink.NetworkTypeHandle"/>.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this <see cref="uLink.NetworkTypeHandle"/>.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode() { return typeHandle.GetHashCode(); }

		/// <summary>
		/// Indicates whether this <see cref="uLink.NetworkTypeHandle"/> and a specified object are equal.
		/// </summary>
		/// <returns>
		/// <c>true</c> if <paramref name="other"/> and this <see cref="uLink.NetworkTypeHandle"/> are the same type and represent the same value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="other">Another object to compare to. </param><filterpriority>2</filterpriority>
		public override bool Equals(object other)
		{
			return (other is NetworkTypeHandle) && Equals((NetworkTypeHandle)other);
		}

		public bool Equals(NetworkTypeHandle other)
		{
			return typeHandle.Equals(other.typeHandle);
		}

		/// <summary>
		/// Returns a formatted string with details on this <see cref="uLink.NetworkTypeHandle"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> containing a fully qualified type name.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			var t = type;
			return t != null ? t.ToString() : "Null";
		}
	}
}
