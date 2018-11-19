using System;

internal static class ArrayUtility
{
	public static void Push<T>(ref T[] array, T value)
	{
		var result = new T[array.Length + 1];

		result[0] = value;
		Array.Copy(array, 0, result, 1, array.Length);

		array = result;
	}

	public static void Pop<T>(ref T[] array)
	{
		var result = new T[array.Length - 1];

		Array.Copy(array, 1, result, 0, array.Length - 1);

		array = result;
	}

	public static T[] Concat<T>(T[] a, T[] b)
	{
		var result = new T[a.Length + b.Length];

		Array.Copy(a, 0, result, 0, a.Length);
		Array.Copy(b, 0, result, a.Length, b.Length);

		return result;
	}

	public static void Add<T>(ref T[] array, T item)
	{
		var result = new T[array.Length + 1];

		result[array.Length] = item;
		Array.Copy(array, 0, result, 0, array.Length);

		array = result;
	}

	public static void Insert<T>(ref T[] array, int index, T item)
	{
		var result = new T[array.Length + 1];

		result[index] = item;
		if (index != 0) Array.Copy(array, 0, result, 0, index);
		if (index != array.Length) Array.Copy(array, index, result, index + 1, array.Length - index);

		array = result;
	}

	public static void InsertRange<T>(ref T[] array, int index, T[] items)
	{
		var result = new T[array.Length + items.Length];

		Array.Copy(items, 0, result, index, items.Length);
		if (index != 0) Array.Copy(array, 0, result, 0, index);
		if (index != array.Length) Array.Copy(array, index, result, index + items.Length, array.Length - index);

		array = result;
	}

	public static void RemoveAt<T>(ref T[] array, int index)
	{
		var result = new T[array.Length - 1];

		if (index != 0) Array.Copy(array, 0, result, 0, index);
		if (index != array.Length - 1) Array.Copy(array, index + 1, result, index, array.Length - index - 1);
			
		array = result;
	}

	public static void Remove<T>(ref T[] array, T item)
	{
		int index = Array.IndexOf(array, item);
		if (index != -1) RemoveAt(ref array, index);
	}

	public static void RemoveAll<T>(ref T[] array, T item)
	{
		var removeIndices = new int[array.Length];
		int removeCount = 0;

		int lastMatch = Array.IndexOf(array, item);
		if (lastMatch == -1) return;

		do
		{
			removeIndices[removeCount] = lastMatch;
			removeCount++;

			if (lastMatch == array.Length - 1) break;
			lastMatch = Array.IndexOf(array, item, lastMatch + 1);

		} while (lastMatch != -1);

		var result = new T[array.Length - removeCount];
		int lastIndex = 0;
		int lastCount = 0;

		for (int i = 0; i < removeCount; i++)
		{
			int removeIndex = removeIndices[i];
			int count = removeIndex - lastIndex;

			Array.Copy(array, lastIndex, result, lastCount, count);

			lastIndex = removeIndex + 1;
			lastCount += count;
		}

		Array.Copy(array, lastIndex, result, lastCount, array.Length - lastIndex);
			
		array = result;
	}

	public static void RemoveAll<T>(ref T[] array, Predicate<T> match)
	{
		var removeIndices = new int[array.Length];
		int removeCount = 0;

		int lastMatch = Array.FindIndex(array, match);
		if (lastMatch == -1) return;

		do
		{
			removeIndices[removeCount] = lastMatch;
			removeCount++;

			if (lastMatch == array.Length - 1) break;
			lastMatch = Array.FindIndex(array, lastMatch + 1, match);

		} while (lastMatch != -1);

		var result = new T[array.Length - removeCount];
		int lastIndex = 0;
		int lastCount = 0;

		for (int i = 0; i < removeCount; i++)
		{
			int removeIndex = removeIndices[i];
			int count = removeIndex - lastIndex;

			Array.Copy(array, lastIndex, result, lastCount, count);

			lastIndex = removeIndex + 1;
			lastCount += count;
		}

		Array.Copy(array, lastIndex, result, lastCount, array.Length - lastIndex);

		array = result;
	}
}
