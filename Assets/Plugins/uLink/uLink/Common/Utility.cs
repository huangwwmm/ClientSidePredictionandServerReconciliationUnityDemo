#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11844 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:05:10 +0200 (Fri, 13 Apr 2012) $
#endregion
#define ULINK //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Security;
using System.Text;
using Lidgren.Network;
using Random = System.Random;

#if UNITY_BUILD
using UnityEngine;
#endif

#if ULINK
using Log = uLink.NetworkLog;
using LogFlags = uLink.NetworkLogFlags;
#elif ULOBBY
using Log = uLobby.Log;
using LogFlags = uLobby.LogFlags;
#endif

namespace uLink
{
	internal static class Utility
	{
		public static readonly Random random = new Random(); 

		public static void Assert(bool condition, params object[] args)
		{
			if (!condition) Exception(args);
		}

		public static void Exception(params object[] args)
		{
#if PIKKO_BUILD || DRAGONSCALE || NO_CRAP_DEPENDENCIES
			//TODO: discuss with Aidin if we should add NetworkException to PikkoServer
			throw new System.Exception(NetworkLogUtility.ObjectsToString(args));
#else
			throw new NetworkException(NetworkLogUtility.ObjectsToString(args));
#endif
		}

		public static IPAddress Resolve(string host)
		{
			IPAddress ip = null;

			try
			{
				ip = NetUtility.Resolve(host);
			}
			catch (Exception ex)
			{
				NetworkLog.Debug(NetworkLogFlags.Utility, "Failed to resolve host '", host, "':", ex);
			}

			Assert(ip != null, "Unable to resolve host");
			return ip;
		}

		public static string GetHostName()
		{
#if UNITY_BUILD
			if (UnityEngine.Application.webSecurityEnabled)
			{
				NetworkLog.Debug(NetworkLogFlags.Utility, "Can't get local host name when running as a webplayer");
				return "localhost";
			}
#endif
			return Dns.GetHostName();
		}

		public static IPAddress TryGetLocalIP()
		{
			IPAddress ip = null;

			try
			{
				ip = NetUtility.Resolve(GetHostName());
			}
			catch (Exception ex)
			{
				NetworkLog.Debug(NetworkLogFlags.Utility, "Failed to get local ip address:", ex);
			}

			return ip ?? IPAddress.Loopback;
		}

		public static NetworkEndPoint Resolve(int port)
		{
			IPAddress ip;

			try
			{
				string hostname = GetHostName();
				ip = Resolve(hostname);
			}
			catch
			{
				ip = IPAddress.Loopback;
			}

			return new NetworkEndPoint(ip, port);
		}

		public static NetworkEndPoint Resolve(string host, int port)
		{
			IPAddress ip = Resolve(host);
			return new NetworkEndPoint(ip, port);
		}

		public static NetworkEndPoint[] Resolve(string[] hosts, int port)
		{
			var endpoints = new NetworkEndPoint[hosts.Length];

			for (int i = 0; i < hosts.Length; i++)
			{
				endpoints[i] = Resolve(hosts[i], port);
			}

			return endpoints;
		}

		public static string BytesToString(byte[] bytes)
		{
			string str = String.Empty;

			for (int n = 0; n < bytes.Length; ++n)
				str += bytes[n] + ", ";

			return str;
		}

		public static string BytesToHex(byte[] bytes)
		{
			StringBuilder sb = new StringBuilder("0x", bytes.Length * 2);

			foreach (byte b in bytes)
			{
				sb.AppendFormat("{0:x2}", b);
			}

			return sb.ToString();
		}

		public static object[] BytesToObjects(byte[] bytes)
		{
			object[] objects = new object[bytes.Length];

			Array.Copy(bytes, 0, objects, 0, bytes.Length);

			return objects;
		}

		public static bool AreArraySegmentsEqual(byte[] one, byte[] two, int length, int offsetOne = 0, int offsetTwo = 0)
		{
			if (one.Length - offsetOne < length || two.Length - offsetTwo < length) return false;
			for (int i = 0; i < length; i++)
			{
				if (one[i + offsetOne] != two[i + offsetTwo]) return false;
			}
			return true;
		}

		public static bool AreAllElementsZero(byte[] a)
		{
			if (a == null) return false;

			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != 0) return false;
			}

			return true;
		}

		public static bool IsArrayEqual(byte[] a, byte[] b)
		{
			if (a == b)
			{
				return true;
			}
			 
			if (a == null)
			{
				if(b == null || b.Length == 0)
					return true;
				else
					return false;
			}
			if(b == null)
			{
				if(a == null || a.Length == 0)
					return true;
				else
					return false;
			}
			if (a.Length != b.Length)
			{
				return false;
			}

			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i]) 
					return false;
			}

			return true;
		}

		public static bool IsArrayRefEqual<T>(T[] a, T[] b)
		{
			if (a.Length != b.Length)
			{
				return false;
			}

			for (int i = 0; i < a.Length; i++)
			{
				if (!ReferenceEquals(a[i], b[i])) return false;
			}

			return true;
		}

		public static byte[] PushToArray(byte[] buf, int pushSize)
		{
			var retval = new byte[pushSize + buf.Length];
			Buffer.BlockCopy(buf, 0, retval, pushSize, buf.Length);
			return retval;
		}

		// TODO: some calls to this func are need only because NetBuffer ensures a bigger buffer and should be optimized away.
		public static byte[] SubArray(byte[] buf, int start, int len)
		{
			var retval = new byte[len];
			Buffer.BlockCopy(buf, start, retval, 0, len);
			return retval;
		}

		public static T[] SubArray<T>(T[] buf, int start, int len)
		{
			var retval = new T[len];
			Array.Copy(buf, start, retval, 0, len);
			return retval;
		}

		public static T[] ToArray<T>(ICollection<T> collection)
		{
			var array = new T[collection.Count];
			int i = 0;

			foreach (var element in collection)
			{
				array[i++] = element;
			}

			return array;
		}

		public static string Join<T>(string seperator, T[] values)
		{
			return values.Length != 0 ? Join(seperator, values, 0, values.Length) : String.Empty;
		}

		public static string Join<T>(string seperator, T[] values, int startIndex, int count)
		{
			StringBuilder sb = new StringBuilder(values[startIndex].ToString(), count * 2 - 1);

			for (int i = startIndex + 1; i < startIndex + count; i++)
			{
				sb.Append(seperator);
				sb.Append(values[i]);
			}

			return sb.ToString();
		}

		public static void InsertSorted<T>(List<T> sortedList, T newItem) where T : IComparable<T>
		{
			if (sortedList.Count == 0 || sortedList[sortedList.Count - 1].CompareTo(newItem) <= 0)
			{
				sortedList.Add(newItem);
			}
			else if (sortedList[0].CompareTo(newItem) >= 0)
			{
				sortedList.Insert(0, newItem);
			}
			else
			{
				int index = sortedList.BinarySearch(newItem);
				if (index < 0) index = ~index;

				sortedList.Insert(index, newItem);
			}
		}

		public static bool HasAttributeWithPrefix(MethodInfo method, string prefix)
		{
			try
			{
				var attribs = method.GetCustomAttributes(true);
				foreach (var attrib in attribs)
				{
					try
					{
						if (attrib.GetType().Name.StartsWith(prefix)) return true;
					}
					catch
					{
					}
				}
			}
			catch
			{
			}

			return false;
		}

#if !PIKKO_BUILD && !DRAGONSCALE && !NO_CRAP_DEPENDENCIES
		public static NetBuffer ToBuffer(BitStream stream)
		{
			return new NetBuffer(stream._data, stream._bitIndex, stream._bitCount);
		}

		public static BitStream ToStream(NetBuffer buffer, bool isWriting, bool isTypeSafe)
		{
			return new BitStream(buffer.Data, buffer.PositionBits, buffer.LengthBits, isWriting, isTypeSafe);
		}

		public static string EscapeURL(string str)
		{
			return Uri.EscapeDataString(str);
		}
#endif

#if UNITY_BUILD
		public static string ToHierarchyString(MonoBehaviour component)
		{
			return ToHierarchyString(component, component.transform);
		}

		public static string ToHierarchyString(Component component)
		{
			return ToHierarchyString(component, component.transform);
		}

		public static string ToHierarchyString(Component component, Transform transform)
		{
			var sb = new StringBuilder(component.GetType().Name);
			sb.Append(" \"");
			sb.Append(transform.GetName());

			var cur = transform.parent;
			while (!cur.IsNullOrDestroyed())
			{
				sb.Insert(0, '/');

				cur = cur.parent;
				sb.Insert(0, cur.GetName());
			}

			sb.Append('\"');
			return sb.ToString();
		}
#endif

#if UNITY_BUILD
		public static Type GetUnityEngineNetworkType()
		{
			try
			{
				return typeof(UnityEngine.Application).Assembly.GetType("UnityEngine.Network", false);
			}
			catch (Exception)
			{
				return null;
			}
		}
#else
		public static Type GetUnityEngineNetworkType()
		{
			return null;
		}
#endif
	}
}
