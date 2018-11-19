using System;
using System.Text;

internal static class ArrayExtensions
{
	internal static string ToArrayString<T>(this T[] array)
	{
		if (array == null || array.Length == 0) return "[]";

		var sb = new StringBuilder();
		sb.Append('[');
		sb.Append(array[0]);

		for (int i = 1; i < array.Length; i++)
		{
			sb.Append(", ");
			sb.Append(array[i]);
		}

		sb.Append(']');
		return sb.ToString();
	}

	internal static bool Equals<T>(this T[] a, T[] b)
	{
		if (a == null || b == null)
		{
			return (a == b);
		}

		if (a.Length != b.Length)
		{
			return false;
		}

		for (int i = 0; i < a.Length; i++)
		{
			if (!a[i].Equals(b[i])) return false;
		}

		return true;
	}

	internal static T[] Clone<T>(this T[] array)
	{
		var result = new T[array.Length];

		Array.Copy(array, result, array.Length);
		return result;
	}

	/// <summary>
	/// Returns an array of System.Type corresponding to the input array's elements' types.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="array">The input array. May not contain null, since we cannot get the type of null.</param>
	/// <returns></returns>
	internal static Type[] GetElementTypes<T>(this T[] array)
	{
		var result = new Type[array.Length];

		for (int i = 0; i < array.Length; i++)
		{
			result[i] = array[i].GetType();
		}

		return result;
	}

	internal static T[] SubArray<T>(this T[] array, int startIndex, int length)
	{
		var result = new T[length];

		Array.Copy(array, startIndex, result, 0, length);
		return result;
	}

	internal static int IndexOf<T>(this T[] array, T item)
	{
		return Array.IndexOf(array, item);
	}

	internal static bool Contains<T>(this T[] array, T item)
	{
		return IndexOf(array, item) != -1;
	}
}
