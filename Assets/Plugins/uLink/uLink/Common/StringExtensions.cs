#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11844 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:05:10 +0200 (Fri, 13 Apr 2012) $
#endregion
using System;
using System.Text;

internal static class StringExtensions
{
	internal static int CountOccurrencesOfChar(this string str, char target)
	{
		int count = 0;

		for (int i = 0; i < str.Length; i++)
		{
			char c = str[i];
			if (c == target) count++;
		}

		return count;
	}

	internal static string RemoveAll(this string str, char[] targets)
	{
		var sb = new StringBuilder(str.Length);
		for (int i = 0; i < str.Length; i++)
		{
			char c = str[i];
			if (targets.Contains(c)) sb.Append(c);
		}

		return sb.ToString();
	}

	internal static string RemoveAll(this string str, char target)
	{
		var sb = new StringBuilder(str.Length);
		for (int i = 0; i < str.Length; i++)
		{
			char c = str[i];
			if (c != target) sb.Append(c);
		}

		return sb.ToString();
	}

	internal static string Remove(this string str, string target)
	{
		return Remove(str, target, 0, str.Length);
	}

	internal static string Remove(this string str, string target, int startIndex, int count)
	{
		var index = str.IndexOf(target, startIndex, count, StringComparison.Ordinal);
		return (index != -1) ? str.Remove(index, target.Length) : str;
	}

	internal static int LastIndexOf(this string str, char target)
	{
		for (int i = str.Length - 1; i >= 0; i--)
		{
			if (str[i] == target) return i;
		}

		return -1;
	}

	internal static bool Contains(this string str, char target)
	{
		for (int i = 0; i < str.Length; i++)
		{
			if (str[i] == target) return true;
		}

		return false;
	}

	internal static string RemoveIfEndsWith(this string str, char target, StringComparison comparison)
	{
		if (str.Length > 0 && str[str.Length - 1] == target)
		{
			str = str.Remove(str.Length - 1);
		}

		return str;
	}

	internal static string RemoveIfEndsWith(this string str, string target, StringComparison comparison)
	{
		if (str.EndsWith(target, comparison))
		{
			str = str.Remove(str.Length - target.Length);
		}

		return str;
	}

	internal static string RemoveUpToLastIndexOf(this string str, char target)
	{
		int i = LastIndexOf(str, target);
		if (i != -1) str = str.Remove(0, i + 1);
		return str;
	}
}
