#region COPYRIGHT
// (c)2012 MuchDifferent. All Rights Reserved.
// 
// $Revision: 11529 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-02-22 02:34:38 +0100 (Wed, 22 Feb 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;

namespace uLink
{
	/// <summary>
	/// Class used by the uLink to read settings (which can be edited in the uLink editor menu).
	/// </summary>
	/// <remarks>
	/// The class can't save/edit any values in the actual persistent <see cref="uLink.NetworkPrefs.resourcePath"/> file,
	/// because that assumes the file is editable (i.e. the running project isn't built) and would require file I/O permission.
	/// Calling any Set methods in the class, will only update the values in memory.
	/// </remarks>
	public static partial class NetworkPrefs
	{
		/// <summary>
		/// The resource path used by the class to load the persistent file, in which the values are stored.
		/// </summary>
		public const string resourcePath = "uLinkPrefs";

		private static readonly System.Collections.Generic.Dictionary<string, string> _keys = new System.Collections.Generic.Dictionary<string, string>();

		static NetworkPrefs()
		{
			Reload();
		}

		/// <summary>
		/// Reloads the values from the persistent <see cref="uLink.NetworkPrefs.resourcePath"/> file.
		/// </summary>
		public static void Reload()
		{
			string text = null;

#if UNITY_BUILD
			try
			{
				var asset = UnityEngine.Resources.Load(resourcePath) as UnityEngine.TextAsset;
				text = !asset.IsNullOrDestroyed() ? asset.text : null;
			}
			catch
			{
				// NOTE: Sometimes Unity load constructors in a background thread which can't call the UnityEngine API.
				UnityEngine.Debug.Log("Couldn't look for potential uLink settings file: " + resourcePath);
				return;
			}
#endif

			DeleteAll();
#if !DRAGONSCALE
			_ParseConfig(text);
#endif
		}

		/// <summary>
		/// Deletes all loaded values in NetworkPrefs.
		/// </summary>
		public static void DeleteAll()
		{
			_keys.Clear();
		}

		/// <summary>
		/// Returns if a key exists or not.
		/// </summary>
		/// <param name="key">The key which you want to see if exists or not.</param>
		/// <returns>If the key exists or not.</returns>
		public static bool HasKey(string key)
		{
			return _keys.ContainsKey(key);
		}

		/// <summary>
		/// Deletes the key if exists.
		/// </summary>
		/// <param name="key">The key which you want to delete.</param>
		/// <returns><c>true</c> if the key existed and it could delete it, <c>false</c> if the key doesn't exist.</returns>
		public static bool DeleteKey(string key)
		{
			return _keys.Remove(key);
		}

		/// <summary>
		/// Gets the value of a key as a string.
		/// </summary>
		/// <param name="key">The key to get the value of.</param>
		/// <returns>The value of the key, empty string if the key doesn't exist.</returns>
		public static string GetString(string key)
		{
			return GetString(key, String.Empty);
		}

		/// <summary>
		/// Returns the value of a key as a string.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns>The value of the key if exists, otherwise the default value.</returns>
		public static string GetString(string key, string defaultValue)
		{
			string value;
			return _keys.TryGetValue(key, out value) ? value : defaultValue;
		}

		/// <summary>
		/// Sets the value of a key as a string.
		/// </summary>
		/// <param name="key">The key to set its value.</param>
		/// <param name="value">The value to set.</param>
		public static void SetString(string key, string value)
		{
			_keys[key] = value;
		}

		/// <summary>
		/// Tries to get the value of a key as a string.
		/// </summary>
		/// <param name="key">The key to get its value.</param>
		/// <param name="value">The variable which will contain the value if the key exists.</param>
		/// <returns><c>true</c> if the key exists, <c>false</c> otherwise.</returns>
		public static bool TryGetString(string key, ref string value)
		{
			string keyValue;
			if (_keys.TryGetValue(key, out keyValue))
			{
				value = keyValue;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the value of a key as a string.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns>The value of the key if exists, otherwise the default value.</returns>
		public static string Get(string key, string defaultValue) { return GetString(key, defaultValue); }

		/// <summary>
		/// Tries to get the value of a key as a string.
		/// </summary>
		/// <param name="key">The key to get its value.</param>
		/// <param name="value">The variable which will contain the value if the key exists.</param>
		/// <returns><c>true</c> if the key exists, <c>false</c> otherwise.</returns>
		public static bool TryGet(string key, ref string value) { return TryGetString(key, ref value); }

		/// <summary>
		/// Sets the value of a key as a string.
		/// </summary>
		/// <param name="key">The key to set its value.</param>
		/// <param name="value">The value to set.</param>
		public static void Set(string key, string value) { SetString(key, value); }
	}
}
