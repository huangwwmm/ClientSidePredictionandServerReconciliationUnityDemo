using System;
using System.Collections.Generic;

internal static class DictionaryExtensions
{
	internal static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValue)
	{
		TValue value;
		if (!dictionary.TryGetValue(key, out value))
		{
			value = newValue.Invoke();
			dictionary.Add(key, value);
		}
		return value;
	}

	internal static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
	{
		TValue value;
		if (!dictionary.TryGetValue(key, out value))
		{
			value = new TValue();
			dictionary.Add(key, value);
		}
		return value;
	}

	internal static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
	{
		if (dictionary.ContainsKey(key))
		{
			return false;
		}

		dictionary.Add(key, value);
		return true;
	}

	internal static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
	{
		TValue value;
		return dictionary.TryGetValue(key, out value) ? value : defaultValue;
	}

	internal static int RemoveWhere<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Predicate<TKey> match)
	{
		var matches = new List<TKey>(dictionary.Count);

		foreach (var key in dictionary.Keys)
		{
			if (match(key)) matches.Add(key);
		}

		for (int i = 0; i < matches.Count; i++)
		{
			dictionary.Remove(matches[i]);
		}

		return matches.Count;
	}
}
