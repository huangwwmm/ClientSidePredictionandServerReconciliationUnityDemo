#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11844 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:05:10 +0200 (Fri, 13 Apr 2012) $
#endregion
using System;
using System.Collections;
using System.Collections.Generic;

namespace uLink
{
	public static partial class NetworkUtility
	{
		public sealed class Comparer<T> : IEqualityComparer<T>, IComparer<T>, IComparer
			where T : struct, IEquatable<T>, IComparable<T>, IComparable
		{
			public static readonly Comparer<T> comparer = new Comparer<T>();

			private Comparer() { }

			public bool Equals(T a, T b) { return a.Equals(b); }
			public int GetHashCode(T value) { return value.GetHashCode(); }
			public int Compare(T a, T b) { return a.CompareTo(b); }
			public int Compare(object a, object b) { return Compare((T)a, (T)b); }
		}

		public sealed class EqualityComparer<T> : IEqualityComparer<T>
			where T : struct, IEquatable<T>
		{
			public static readonly EqualityComparer<T> comparer = new EqualityComparer<T>();

			private EqualityComparer() { }

			public bool Equals(T a, T b) { return a.Equals(b); }
			public int GetHashCode(T value) { return value.GetHashCode(); }
		}
	}
}
