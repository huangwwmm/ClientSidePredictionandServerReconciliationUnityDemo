// (c)2011 Unity Park. All Rights Reserved.

using System.Collections.Generic;

namespace uLinkEditor
{
	internal static class ListExtensions
	{
		public static int RemoveAll<T>(this List<T> list, T item) where T : class
		{
			return list.RemoveAll(delegate(T other) { return other == item; });
		}
	}
}
