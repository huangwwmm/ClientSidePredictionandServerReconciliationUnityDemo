#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion

using System;
using System.Net;

namespace uLink
{

	public partial struct NetworkEndPoint : IEquatable<NetworkEndPoint>, IEquatable<IPEndPoint>, IEquatable<EndPoint>, IEquatable<string>
	{
		/// <summary>
		/// Returns <c>true</c> if two <see cref="NetworkEndPoint"/>s are identical
		/// </summary>
		public static bool operator ==(NetworkEndPoint a, NetworkEndPoint b) { return a.Equals(b); }
		public static bool operator ==(NetworkEndPoint a, IPEndPoint b) { return !(a == b); }
		public static bool operator ==(NetworkEndPoint a, EndPoint b) { return a.Equals(b); }
		public static bool operator ==(NetworkEndPoint a, string b) { return a.Equals(b); }
		public static bool operator ==(IPEndPoint a, NetworkEndPoint b) { return b.Equals(a); }
		public static bool operator ==(EndPoint a, NetworkEndPoint b) { return b.Equals(a); }
		public static bool operator ==(string a, NetworkEndPoint b) { return b.Equals(a); }

		/// <summary>
		/// Returns <c>true</c> if two <see cref="NetworkEndPoint"/>s are not identical
		/// </summary>
		public static bool operator !=(NetworkEndPoint a, NetworkEndPoint b) { return !(a == b); }
		public static bool operator !=(NetworkEndPoint a, IPEndPoint b) { return !(a == b); }
		public static bool operator !=(NetworkEndPoint a, EndPoint b) { return !(a == b); }
		public static bool operator !=(NetworkEndPoint a, string b) { return !(a == b); }
		public static bool operator !=(IPEndPoint a, NetworkEndPoint b) { return !(a == b); }
		public static bool operator !=(EndPoint a, NetworkEndPoint b) { return !(a == b); }
		public static bool operator !=(string a, NetworkEndPoint b) { return !(a == b); }

		/// <summary>
		/// Indicates whether this <see cref="NetworkEndPoint"/> and a specified object are equal.
		/// </summary>
		/// <returns>
		/// <c>true</c> if <paramref name="other"/> and this <see cref="NetworkEndPoint"/> are the same type and represent the same value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="other">Another object to compare to. </param><filterpriority>2</filterpriority>
		public override bool Equals(object other)
		{
			return ((other is NetworkEndPoint) && Equals((NetworkEndPoint)other))
				|| ((other is IPEndPoint) && Equals((IPEndPoint)other))
				|| ((other is EndPoint) && Equals((EndPoint)other))
				|| ((other is string) && Equals((string)other));
		}

		public bool Equals(NetworkEndPoint other)
		{
			return Equals(other.value);
		}

		public bool Equals(IPEndPoint other)
		{
			return value.Equals(other);
		}

		public bool Equals(EndPoint other)
		{
			return value.Equals(other);
		}

		public bool Equals(string other)
		{
			return Equals(new NetworkEndPoint(other));
		}
	}
}
