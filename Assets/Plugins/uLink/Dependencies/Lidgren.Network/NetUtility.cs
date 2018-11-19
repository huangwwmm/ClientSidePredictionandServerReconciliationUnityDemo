/* Copyright (c) 2008 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security;

namespace Lidgren.Network
{
	/// <summary>
	/// Utility methods
	/// </summary>
	internal static class NetUtility
	{
		internal static void Assert(bool condition)
		{
			if (!condition) throw new Exception("Assertion failed!");
		}

		internal static void Assert(bool condition, string message)
		{
			if (!condition) throw new Exception(message);
		}

		/// <summary>
		/// Get IP address from notation (xxx.xxx.xxx.xxx) or hostname
		/// </summary>
		public static IPAddress Resolve(string ipOrHost)
		{
			if (string.IsNullOrEmpty(ipOrHost))
				throw new ArgumentException("Supplied string must not be empty", "ipOrHost");

			ipOrHost = ipOrHost.Trim();

			// is it an ip number string?
			IPAddress ipAddress = null;
			if (IPAddress.TryParse(ipOrHost, out ipAddress))
				return ipAddress;

			// ok must be a host name
			IPHostEntry entry;

			entry = Dns.GetHostEntry(ipOrHost);
			if (entry == null)
				return null;

			// check each entry for a valid IP address
			foreach (IPAddress ip in entry.AddressList)
			{
				//by WuNan @2016/09/28 14:27:27
#if (UNITY_IOS  || UNITY_TVOS ) && !UNITY_EDITOR
				if (ip.AddressFamily == AddressFamily.InterNetwork || ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
#else
				if (ip.AddressFamily == AddressFamily.InterNetwork)
#endif
					ipAddress = ip;
			}

			return ipAddress;
		}

		private static NetworkInterface GetNetworkInterface()
		{
			IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
			if (computerProperties == null)
				return null;

		    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			if (nics == null || nics.Length < 1)
				return null;

			foreach (NetworkInterface adapter in nics)
			{
				if (adapter.OperationalStatus != OperationalStatus.Up)
					continue;
				if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback || adapter.NetworkInterfaceType == NetworkInterfaceType.Unknown)
					continue;
				if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
					continue;

				// A computer could have several adapters (more than one network card)
				// here but just return the first one for now...
				return adapter;
			}
			return null;
		}

		/// <summary>
		/// Gets my local IP address (not necessarily external) and subnet mask
		/// </summary>
		public static IPAddress GetMyAddress(out IPAddress mask)
		{
			NetworkInterface ni = GetNetworkInterface();
			if (ni == null)
			{
				mask = null;
				return null;
			}

			IPInterfaceProperties properties = ni.GetIPProperties();
			foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
			{
				if (unicastAddress != null && unicastAddress.Address != null && unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
				{
					mask = unicastAddress.IPv4Mask;
					return unicastAddress.Address;
				}
			}

			mask = null;
			return null;
		}

		/// <summary>
		/// Returns true if the IPEndPoint supplied is on the same subnet as this host
		/// </summary>
		public static bool IsLocal(IPEndPoint endPoint)
		{
			if (endPoint == null)
				return false;
			return IsLocal(endPoint.Address);
		}

		/// <summary>
		/// Returns true if the IPAddress supplied is on the same subnet as this host
		/// </summary>
		public static bool IsLocal(IPAddress remote)
		{
			IPAddress mask;
			IPAddress local = GetMyAddress(out mask);

			if (mask == null)
				return false;

			uint maskBits = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
			uint remoteBits = BitConverter.ToUInt32(remote.GetAddressBytes(), 0);
			uint localBits = BitConverter.ToUInt32(local.GetAddressBytes(), 0);

			// compare network portions
			return ((remoteBits & maskBits) == (localBits & maskBits));
		}

		/// <summary>
		/// Returns how many bits are necessary to hold a certain number
		/// </summary>
		//[CLSCompliant(false)]
		public static int BitsToHoldUInt(uint value)
		{
			int bits = 1;
			while ((value >>= 1) != 0)
				bits++;
			return bits;
		}

		//[CLSCompliant(false)]
		public static UInt32 SwapByteOrder(UInt32 value)
		{
			return
				((value & 0xff000000) >> 24) |
				((value & 0x00ff0000) >> 8) |
				((value & 0x0000ff00) << 8) |
				((value & 0x000000ff) << 24);
		}
		
		//[CLSCompliant(false)]
		public static UInt64 SwapByteOrder(UInt64 value)
		{
			return
				((value & 0xff00000000000000L) >> 56) |
				((value & 0x00ff000000000000L) >> 40) |
				((value & 0x0000ff0000000000L) >> 24) |
				((value & 0x000000ff00000000L) >> 8) |
				((value & 0x00000000ff000000L) << 8) |
				((value & 0x0000000000ff0000L) << 24) |
				((value & 0x000000000000ff00L) << 40) |
				((value & 0x00000000000000ffL) << 56);
		}

		public static bool CompareElements(byte[] one, byte[] two)
		{
			if (one.Length != two.Length)
				return false;
			for (int i = 0; i < one.Length; i++)
				if (one[i] != two[i])
					return false;
			return true;
		}

		public static string BytesToHex(byte[] bytes)
		{
			return BytesToHex(bytes, 0, bytes.Length);
		}

		public static string BytesToHex(byte[] bytes, int startIndex, int count)
		{
			const string HEX_CHARS = "0123456789abcdef";

			if (bytes == null || count == 0) return "[]";

			var sb = new StringBuilder(2 + count * 3 - 1);
			sb.Append('[');

			byte first = bytes[startIndex];
			sb.Append(HEX_CHARS[(first & 0xf0) >> 4]);
			sb.Append(HEX_CHARS[(first & 0x0f)]);

			int length = startIndex + count;
			for (int i = startIndex + 1; i < length; i++)
			{
				byte b = bytes[i];

				sb.Append(':');
				sb.Append(HEX_CHARS[(b & 0xf0) >> 4]);
				sb.Append(HEX_CHARS[(b & 0x0f)]);
			}

			sb.Append(']');

			return sb.ToString();
		}

		private static bool isMatch(byte[] x, byte[] y, int index)
		{
			for (int j = 0; j < y.Length; ++j)
				if (x[j + index] != y[j]) return false;
			return true;
		}

		public static int IndexOf(byte[] x, byte[] y)
		{
			for (int i = 0; i < x.Length - y.Length + 1; ++i)
				if (isMatch(x, y, i)) return i;
			return -1;
		}

		private static int _GetInsertIndexByUnsafeBinarySearch<T>(T[] array, int startIndex, int stopIndex, T insert)
			where T : IComparable<T>
		{
			int low = startIndex;
			int high = stopIndex;

			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);

				int result = array[mid].CompareTo(insert);
				if (result > 0)
				{
					high = mid - 1;
				}
				else if (result < 0)
				{
					low = mid + 1;
				}
				else
				{
					return mid;
				}
			}

			return low;
		}

		public static void ReplaceSortedItem<T>(T[] array, int oldIndex, T newItem)
			where T : IComparable<T>
		{
			int compareTo = newItem.CompareTo(array[oldIndex]);
			if (compareTo < 0)
			{
				if (oldIndex == 0)
				{
					array[oldIndex] = newItem;
					return;
				}

				int newIndex = _GetInsertIndexByUnsafeBinarySearch(array, 0, oldIndex - 1, newItem);

				for (int i = oldIndex; i > newIndex; i--)
				{
					array[i] = array[i - 1];
				}

				array[newIndex] = newItem;
			}
			else if (compareTo > 0)
			{
				if (oldIndex == array.Length - 1)
				{
					array[oldIndex] = newItem;
					return;
				}

				int newIndex = _GetInsertIndexByUnsafeBinarySearch(array, oldIndex + 1, array.Length - 1, newItem) - 1;

				for (int i = oldIndex; i < newIndex; i++)
				{
					array[i] = array[i + 1];
				}

				array[newIndex] = newItem;
			}
			else
			{
				array[oldIndex] = newItem;
			}
		}

#if !PIKKO_BUILD && !DRAGONSCALE && !NO_CRAP_DEPENDENCIES
		public static bool SafeHeartbeat(NetBase net)
		{
			if (net == null) return false;

			try
			{
				net.Heartbeat();
			}
			catch (NetException ex)
			{
				if (ex.InnerException is SecurityException)
				{
					Log.Error(LogFlags.Socket, ex.Message, " because of security. Make sure your running a policy server on the other end. If this only happens once, you can ignore it or avoid it by prefetching from policy server using Security.PrefetchSocketPolicy. ", ex);
					return true; // TODO: should we really try again?
				}

				var sex = ex.InnerException as SocketException;
				if (IsSocketErrorPermanent(sex))
				{
					Log.Warning(LogFlags.Socket, "Socket error is interpreted as the network is shutdown: ", ex.InnerException);
					return false;
				}

				if (sex != null)
					Log.Error(LogFlags.Socket, ex.Message, ": [socket error = ", sex.SocketErrorCode, ", code = ", sex.ErrorCode, "] ", ex.InnerException);
				else
					Log.Error(LogFlags.Socket, ex.Message, ": ", ex.InnerException);
			}
			catch (Exception ex)
			{
				Log.Error(LogFlags.Socket, ex);
			}

			return true;
		}
#endif

		public static bool IsSocketErrorPermanent(SocketException sex)
		{
			return sex != null && (IsSocketErrorPermanent(sex.SocketErrorCode) | IsSocketErrorPermanent((SocketError)sex.ErrorCode));
		}

		public static bool IsSocketErrorPermanent(SocketError error)
		{
			// NOTE: We assume a system call failure is quite permanent
			const SocketError WSASYSCALLFAILURE = (SocketError)10107;

			return
				error == WSASYSCALLFAILURE | // 10107
				error == SocketError.HostUnreachable | // 10065
				error == SocketError.NetworkDown | // 10050
				error == SocketError.NetworkUnreachable | // 10051
				error == SocketError.Shutdown; // 10058
		}
	}
}
