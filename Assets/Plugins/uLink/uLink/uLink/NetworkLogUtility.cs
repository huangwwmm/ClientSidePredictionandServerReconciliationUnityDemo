#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10139 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:11:15 +0100 (Tue, 29 Nov 2011) $
#endregion
using System;
using System.Collections.Generic;

namespace uLink
{
	/// <summary>
	/// Utility class for composing log messages.
	/// </summary>
	public static class NetworkLogUtility
	{
		/// <summary>
		/// This is the delegate that you should conform to when writing methods to create string representation of types for logging.
		/// </summary>
		/// <param name="obj">This is the instance that you should create a representation of.</param>
		/// <returns></returns>
		public new delegate string ToString(object obj);

		private static readonly Dictionary<NetworkTypeHandle, ToString> _toStrings = new Dictionary<NetworkTypeHandle, ToString>();

		/// <summary>
		/// The static constructor of the class is called as soon as someone uses the class by any means, 
		/// before anything else executed. It justs registers a few default handlers.
		/// </summary>
		static NetworkLogUtility()
		{
			AddToString<RuntimeTypeHandle>(delegate(object obj) { return Type.GetTypeFromHandle((RuntimeTypeHandle)obj).ToString(); });
			AddToString<byte[]>(delegate(object obj) { return Utility.BytesToHex((byte[])obj); });
		}

		/// <summary>
		/// Adds a method for creating a string representation from objects of the specified type.
		/// </summary>
		/// <typeparam name="T">The type that we want to make strings from objects of it.</typeparam>
		/// <param name="toString">The method which creates the string using the provided object.</param>
		public static void AddToString<T>(ToString toString)
		{
			AddToString(typeof(T).TypeHandle, toString);
		}

		/// <summary>
		/// Adds a method for creating a string representation from objects of the specified type handle.
		/// </summary>
		/// <param name="typeHandle">Handle of the type which you want to suply the string creation method for.</param>
		/// <param name="toString">The method which can create string representations of the type.</param>
		/// <remarks>Take a look at RuntimeTypeHandle documentation on MSDN for more information.</remarks>
		public static void AddToString(RuntimeTypeHandle typeHandle, ToString toString)
		{
			_toStrings[typeHandle] = toString;
		}

		/// <summary>
		/// Removes the method for string creation for the provided type handle.
		/// </summary>
		/// <param name="typeHandle">The handle for the type that you want to remove its method.</param>
		public static void RemoveToString(RuntimeTypeHandle typeHandle)
		{
			_toStrings.Remove(typeHandle);
		}

		/// <summary>
		/// Returns the string representation of the provided object.
		/// </summary>
		/// <param name="obj">The instance that you want to get its string representation.</param>
		/// <returns>If the object is not null, string representing the object, otherwise null.</returns>
		/// <remarks>
		/// The method first tries to find a registered method for the type of the object and call it, if unsuccessful it
		/// calls Object.ToString on it. 
		/// If the provided object is null, null is returned.
		/// </remarks>
		public static string ObjectToString(object obj)
		{
			if (obj != null)
			{
				var typeHandle = Type.GetTypeHandle(obj);
				ToString toString;
				string retval;

				if (_toStrings.TryGetValue(typeHandle, out toString))
				{
					try
					{
						retval = toString(obj);
					}
					catch
					{
						retval = "<Registered ToString Failed>";
					}
				}
				else
				{
					try
					{
						retval = obj.ToString();
					}
					catch
					{
						retval = "<ToString Failed>";
					}
				}

				return retval;
			}

			return "Null";
		}

		/// <summary>
		/// Returns a single string containing all of the strings representing the objects in the array.
		/// </summary>
		/// <param name="objs">An array of objects which you want their string representation.</param>
		/// <returns>A string containing the string representation of all array elements.</returns>
		public static string ObjectsToString(object[] objs)
		{
			var strings = new string[objs.Length];

			for (int i = 0; i < objs.Length; i++)
			{
				strings[i] = ObjectToString(objs[i]);
			}

			return String.Concat(strings);
		}

		/// <summary>
		/// Returns the first object which has the type <c>T</c> or is derived from it from the whole array of provided <c>objs</c>.
		/// </summary>
		/// <typeparam name="T">The type that you want to return an object which is of its kind.</typeparam>
		/// <param name="objs">The array of objects which you want to search and only get one of its objects
		/// which have the specified type.</param>
		/// <returns>An instance from the objects which is of type <c>T</c> if any, otherwise null</returns>
		/// <remarks>The method uses the C# <c>is</c> operator to find if an object is of the type or not and does a linear search.
		/// The operation is O(N) where N is the number of elements in the array.</remarks>
		public static T FindObjectOfType<T>(object[] objs) where T : class
		{
			foreach (var obj in objs)
			{
				if (obj is T) return obj as T;
			}

			return null;
		}
	}
}
