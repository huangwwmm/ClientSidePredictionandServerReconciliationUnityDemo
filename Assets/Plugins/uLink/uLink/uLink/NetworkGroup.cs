#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// This class represents a network groups.
	/// Groups allow you to filter messages which a player receives or not.
	/// You can have network aware objects in specific groups and then only players which are members of the group
	/// will receive state sync and RPCs from the object. Also based on group settings, you can make network aware objects
	/// of a group to only appear in the players in the group.
	/// </summary>
	/// <remarks> You usually use int numbers to represent a group.
	/// See the manual page for groups for more information.
	/// </remarks>
	public struct NetworkGroup : IEquatable<NetworkGroup>, IComparable<NetworkGroup>, IComparable
	{
		//by WuNan @2016/08/27 14:54:43 编译不过，没有引用，暂时注释掉
		//public static readonly NetworkUtility.Comparer<NetworkGroup> comparer = NetworkUtility.Comparer<NetworkGroup>.comparer;

		internal static readonly Dictionary<NetworkGroup, NetworkGroupFlags> _flags = new Dictionary<NetworkGroup, NetworkGroupFlags>()
		{
			{ 0, NetworkGroupFlags.None }
		};

		/// <summary>
		/// Represents an special network group which all players are always part of.
		/// If you set the group of an object as 0 in <see cref="uLink.Network.Instantiate"/>, the object has no groups
		/// or it's group value is unassigned.
		/// </summary>
		public static readonly NetworkGroup unassigned = new NetworkGroup(0);

		/// <summary>
		/// Maximum <see cref="uLink.NetworkGroup.id"/> value a assigned group can have.
		/// </summary>
		public static readonly NetworkGroup max = new NetworkGroup(UInt16.MaxValue);

		/// <summary>
		/// Minimum <see cref="uLink.NetworkGroup.id"/> value a assigned group can have.
		/// </summary>
		public static readonly NetworkGroup min = new NetworkGroup(unassigned.id + 1);

		private readonly ushort _id;

		/// <summary>
		/// This is the unique ID number for this network group.
		/// </summary>
		/// <remarks>
		/// You usually use this value instead of a NetworkGroup struct. 
		/// </remarks>
		public int id { get { return _id; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="uLink.NetworkGroup"/> struct.
		/// </summary>
		public NetworkGroup(int id)
		{
			if(!(0 <= id && id <= UInt16.MaxValue)){Utility.Exception( "id can't be less than 0 or greater than UInt16.MaxValue");}

			_id = (ushort) id;
		}

		internal NetworkGroup(NetBuffer buffer)
		{
			_id = buffer.ReadUInt16();
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.Write(_id);
		}

#if !DRAGONSCALE
		public NetworkGroupFlags flags { get { return GetFlags(); } set { SetFlags(value); } }
#endif

		/// <summary>
		/// Sets the settings of the group.
		/// </summary>
		/// <param name="flags">The settings of the group as a set of flags.</param>
		public void SetFlags(NetworkGroupFlags flags)
		{
			if(!(this != unassigned)){Utility.Exception( "Can't set flags for unassigned group");}

			if (flags != NetworkGroupFlags.None)
			{
				_flags[this] = flags;
			}
			else
			{
				_flags.Remove(this);
			}

#if !TEST_BUILD && !PIKKO_BUILD &&!DRAGONSCALE
			Network._singleton._ApplyGroupFlags(this, flags);
#endif
		}

#if !DRAGONSCALE
		/// <summary>
		/// Gets a group's settings.
		/// </summary>
		/// <returns>A <see cref="uLink.NetworkGroupFlags"/> representing the group settings.</returns>
		public NetworkGroupFlags GetFlags()
		{
			return _flags.GetOrDefault(this, NetworkGroupFlags.None);
		}
#endif

		public static implicit operator int(NetworkGroup group) { return group._id; }

		public static implicit operator NetworkGroup(int id) { return new NetworkGroup(id); }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkGroup"/>s are identical
		/// </summary>
		public static bool operator ==(NetworkGroup lhs, NetworkGroup rhs) { return lhs._id == rhs._id; }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkGroup"/>s are not identical
		/// </summary>
		public static bool operator !=(NetworkGroup lhs, NetworkGroup rhs) { return lhs._id != rhs._id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkGroup"/> is greater than or equal to the right <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		public static bool operator >=(NetworkGroup lhs, NetworkGroup rhs) { return lhs._id >= rhs._id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkGroup"/> is less than or equal to the right <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		public static bool operator <=(NetworkGroup lhs, NetworkGroup rhs) { return lhs._id <= rhs._id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkGroup"/> is greater than the right <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		public static bool operator >(NetworkGroup lhs, NetworkGroup rhs) { return lhs._id > rhs._id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkGroup"/> is less than the right <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		public static bool operator <(NetworkGroup lhs, NetworkGroup rhs) { return lhs._id < rhs._id; }

		/// <summary>
		/// Returns the hash code for this <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this <see cref="uLink.NetworkGroup"/>.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode() { return _id.GetHashCode(); }

		/// <summary>
		/// Indicates whether this <see cref="uLink.NetworkViewID"/> and a specified object are equal.
		/// </summary>
		/// <returns>
		/// <c>true</c> if <paramref name="other"/> and this <see cref="uLink.NetworkGroup"/> are the same type and represent the same value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="other">Another object to compare to. </param><filterpriority>2</filterpriority>
		public override bool Equals(object other)
		{
			return (other is NetworkGroup) && Equals((NetworkGroup)other);
		}

		public bool Equals(NetworkGroup other)
		{
			return _id == other._id;
		}

		/// <summary>
		/// Compares this instance with another specified <see cref="uLink.NetworkGroup"/> object and indicates
		/// whether this instance precedes, follows, or appears in the same position
		/// in the sort order as the specified <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		/// <param name="other">The other <see cref="uLink.NetworkGroup"/>.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates whether this instance precedes, follows,
		/// or appears in the same position in the sort order as the value parameter.
		/// </returns>
		public int CompareTo(NetworkGroup other)
		{
			return _id.CompareTo(other._id);
		}

		public int CompareTo(object other)
		{
			return CompareTo((NetworkGroup)other);
		}

		/// <summary>
		/// Returns a formatted string with details on this <see cref="uLink.NetworkGroup"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> containing a fully qualified type name.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			return (this != unassigned) ? "Group " + _id.ToString(CultureInfo.InvariantCulture) : "Unassigned";
		}
	}
}
