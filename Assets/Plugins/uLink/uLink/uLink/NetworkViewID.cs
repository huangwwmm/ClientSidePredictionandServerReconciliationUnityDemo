#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Globalization;
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// The NetworkViewID is a unique identifier for a network view instance in a multiplayer game.
	/// </summary>
	/// <remarks>
	/// It is important that this is unique for every network aware object across all connected peers, or 
	/// else RPCs and statesyncs can be sent to the wrong object. 
	/// </remarks>
	public struct NetworkViewID : IEquatable<NetworkViewID>, IComparable<NetworkViewID>, IComparable
	{
		//by WuNan @2016/08/27 14:54:43 编译不过，没有引用，暂时注释掉
		//public static readonly NetworkUtility.Comparer<NetworkViewID> comparer = NetworkUtility.Comparer<NetworkViewID>.comparer;

		/// <summary>
		/// Represents an invalid network view ID.
		/// </summary>
		public static readonly NetworkViewID unassigned = new NetworkViewID(0);

		/// <summary>
		/// The minimum allowed ID which can be set manually for an object.
		/// </summary>
		public static readonly NetworkViewID minManual = new NetworkViewID(1);


		/// <summary>
		/// Maximum allowd ID which can be assigned manually to an object.
		/// </summary>
		public static readonly NetworkViewID maxManual = new NetworkViewID(Int32.MaxValue);

		public readonly int subID;

		/// <summary>
		/// The <see cref="uLink.NetworkPlayer"/> who allocated the <see cref="uLink.NetworkView"/>.
		/// </summary>
		/// <remarks>This can be someone other than owner. As an example, server might allocate the NetworkView for a client.</remarks>
		public readonly NetworkPlayer allocator;

		/// <summary>
		/// This is the unique ID number for this networkView.
		/// </summary>
#if UNITY_BUILD
		[UnityEngine.HideInInspector] /* TODO: remove hacky attempt to avoid a weird bug in Unity:
		
		* MissingMethodException: Method not found: 'uLink.NetworkViewID.get_id'.
		* UnityEditor.InspectorWindow.DrawEditors (Boolean isRepaintEvent, UnityEditor.Editor[] editors, Boolean eyeDropperDirty)
		* UnityEditor.DockArea:OnGUI()
		*/
#endif
		// TODO: also try to change back to long, after we've found the cause for the weird bug in Unity mentioned above.
		public ulong id { get { return (ulong)subID | ((ulong)allocator.id << 32); } }

		/// <summary>
		/// Gets a value indicating whether this instance is server.
		/// </summary>
		public bool isUnassigned { get { return (subID == unassigned.subID); } }

#if UNITY_BUILD
		/// <summary>
		/// The <see cref="uLink.NetworkPlayer"/> who owns the <see cref="uLink.NetworkView"/>.
		/// </summary>
		public NetworkPlayer owner { get { return Network._singleton._FindNetworkViewOwner(this); } }

		/// <summary>
		/// Gets a value indicating whether the <see cref="uLink.NetworkView"/> was instantiated by me.
		/// </summary>
		/// <value><c>true</c> if instantiated by me/owned by me; otherwise, <c>false</c>.</value>
		public bool isMine { get { return (owner == Network._singleton._localPlayer); } }

		/// <summary>
		/// Am i the cell server that has authority over this object.
		/// </summary>
		/// <value><c>true</c> if i am the cell server which has authority over the object which this ViewID is for, <c>false</c> otherwise</value>
		/// <remarks>
		/// This is used for Pikko server.
		/// Read the Pikko server manual for more information.
		/// </remarks>
		public bool isCellAuthority
		{
			get
			{
				var nv = Network._singleton._FindNetworkView(this);
				return nv.IsNotNull() && nv.isCellAuthority;
			}
		}
#else
		public bool isCellAuthority
		{
			get
			{
				return false; // TODO
			}
		}
#endif
		/// <summary>
		/// Gets a value indicating whether this <see cref="uLink.NetworkView"/> was set in the Unity editor and (and the value is lower than than Network.maxManualViewIDs)
		/// </summary>
		public bool isManual { get { return (!isUnassigned & allocator.isUnassigned); } }

		/// <summary>
		/// Gets a value indicating whether this <see cref="uLink.NetworkView"/> was allocated at runtime (and the value is higher than Network.maxManualViewIDs)
		/// </summary>
		public bool isAllocated { get { return (!isUnassigned & !allocator.isUnassigned); } }

		/// <summary>
		/// Initializes a new instance of the <see cref="uLink.NetworkViewID"/> struct.
		/// </summary>
		/// <remarks>Never use this unless you know what you are doing. New instances should be created automatically by uLink.
		/// For example when calling the uLink.Network.
		/// <see cref="uLink.Network.Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>
		/// function</remarks>
		public NetworkViewID(int manualViewID)
		{
			if(!(manualViewID >= 0)){Utility.Exception( "manualViewID can't be negative");}

			subID = manualViewID;
			allocator = NetworkPlayer.unassigned;
		}

		internal NetworkViewID(ulong id)
		{
			//reverse transformation of (long)subID | ((long)allocator.id << 32)
			subID = (int)id;
			allocator = new NetworkPlayer((int)(id >> 32));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="uLink.NetworkViewID"/> struct.
		/// </summary>
		/// <remarks>Never use this unless you know what you are doing. New instances should be created automatically by uLink.
		/// For example when calling the uLink.Network.
		/// <see cref="uLink.Network.Instantiate(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion,uLink.NetworkGroup,System.Object[])"/>
		/// function</remarks>
		public NetworkViewID(int subID, NetworkPlayer allocator)
		{
			if(!(subID >= 0)){Utility.Exception( "subID can't be negative");}
			if(!(subID > 0 || allocator.isUnassigned)){Utility.Exception( "if subID is zero, then allocator must be unassigned");}

			this.subID = subID;
			this.allocator = allocator;
		}

		internal NetworkViewID(NetBuffer buffer)
		{
			subID = buffer.ReadVariableInt32();

			if (subID == 0) // special case if unassigned
			{
				allocator = NetworkPlayer.unassigned;
			}
			else if (subID < 0) // optimized case for allocator == server
			{
				subID = -subID;
				allocator = NetworkPlayer.server;
			}
			else // general case
			{
				allocator = new NetworkPlayer(buffer);
			}
		}

		internal void _Write(NetBuffer buffer)
		{
			if (subID == 0) // special case if unassigned
			{
				buffer.Write((byte)0);
			}
			else if (allocator == NetworkPlayer.server) // optimized case for allocator == server
			{
				buffer.WriteVariableInt32(-subID);
			}
			else // general case
			{
				buffer.WriteVariableInt32(subID);
				allocator._Write(buffer);
			}
		}

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkViewID"/>s are identical
		/// </summary>
		public static bool operator ==(NetworkViewID lhs, NetworkViewID rhs) { return lhs.id == rhs.id; }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="uLink.NetworkViewID"/>s are not identical
		/// </summary>
		public static bool operator !=(NetworkViewID lhs, NetworkViewID rhs) { return lhs.id != rhs.id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkViewID"/> is greater than or equal to the right <see cref="uLink.NetworkViewID"/>.
		/// </summary>
		public static bool operator >=(NetworkViewID lhs, NetworkViewID rhs) { return lhs.id >= rhs.id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkViewID"/> is less than or equal to the right <see cref="uLink.NetworkViewID"/>.
		/// </summary>
		public static bool operator <=(NetworkViewID lhs, NetworkViewID rhs) { return lhs.id <= rhs.id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkViewID"/> is greater than the right <see cref="uLink.NetworkViewID"/>.
		/// </summary>
		public static bool operator >(NetworkViewID lhs, NetworkViewID rhs) { return lhs.id > rhs.id; }

		/// <summary>
		/// Returns <c>true</c> if the left <see cref="uLink.NetworkViewID"/> is less than the right <see cref="uLink.NetworkViewID"/>.
		/// </summary>
		public static bool operator <(NetworkViewID lhs, NetworkViewID rhs) { return lhs.id < rhs.id; }

		/// <summary>
		/// Returns the hash code for this <see cref="uLink.NetworkViewID"/>.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this <see cref="uLink.NetworkViewID"/>.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode() { return id.GetHashCode(); }

		/// <summary>
		/// Indicates whether this <see cref="uLink.NetworkViewID"/> and a specified object are equal.
		/// </summary>
		/// <returns>
		/// <c>true</c> if <paramref name="other"/> and this <see cref="uLink.NetworkViewID"/> are the same type and represent the same value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="other">Another object to compare to. </param><filterpriority>2</filterpriority>
		public override bool Equals(object other)
		{
			return (other is NetworkViewID) && Equals((NetworkViewID)other);
		}

		public bool Equals(NetworkViewID other)
		{
			return id == other.id;
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
		public int CompareTo(NetworkViewID other)
		{
			return id.CompareTo(other.id);
		}

		public int CompareTo(object other)
		{
			return CompareTo((NetworkViewID)other);
		}

		/// <summary>
		/// Returns a formatted string with details on this <see cref="uLink.NetworkViewID"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> containing a fully qualified type name.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			if (isUnassigned) return "ViewID Unassigned";
			if (isManual) return "ViewID " + subID.ToString(CultureInfo.InvariantCulture) + " (Manual)";
			return "ViewID " + subID.ToString(CultureInfo.InvariantCulture) + " (Allocated by " + allocator + ")";
		}
	}
}
