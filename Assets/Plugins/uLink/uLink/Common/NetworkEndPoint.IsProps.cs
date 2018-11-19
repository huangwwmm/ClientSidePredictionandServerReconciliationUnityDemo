#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion

using System.Net;
using System.Net.Sockets;

namespace uLink
{
	public partial struct NetworkEndPoint
	{
		public bool isUnassigned { get { return ReferenceEquals(value, unassigned._value); } }
		public bool isIPEndPoint { get { return value is IPEndPoint; } }

		public bool isLoopback { get { return isIPEndPoint && _IsLoopbackAddress(ipAddress); } }
		public bool isBroadcast { get { return isIPEndPoint && _IsBroadcastAddress(ipAddress); } }
		public bool isPublic { get { return isIPEndPoint && _IsPublicAddress(ipAddress); } }
		public bool isPrivate { get { return isIPEndPoint && _IsPrivateAddress(ipAddress); } }
		public bool isAny { get { return isIPEndPoint && _IsAnyAddress(ipAddress); } }
		public bool isNone { get { return isIPEndPoint && _IsNoneAddress(ipAddress); } }

		private static bool _IsPublicIPv4Address(IPAddress ipAddress)
		{
			// Use .Address because it's faster than GetAddressBytes().
			#pragma warning disable 612,618 // we know what we're doing.
			var ipv4 = (uint)ipAddress.Address;
			#pragma warning restore 612,618

			return (ipv4 & 0x0000000F) != 0x0000000A  // not 10.0.0.0    - 10.255.255.255
				 & (ipv4 & 0x000000FF) != 0x0000007F  // not 127.0.0.0   - 127.255.255.255
				 & (ipv4 & 0x0000FFFF) != 0x0000FEA9  // not 169.254.0.0 - 169.254.255.255
				 & (ipv4 & 0x0000F0FF) != 0x000010AC  // not 172.16.0.0  - 172.31.255.255
				 & (ipv4 & 0x0000FFFF) != 0x0000C0A8; // not 192.168.0.0 - 192.168.255.255
		}

		private static bool _IsPublicAddress(IPAddress ipAddress)
		{
			switch (ipAddress.AddressFamily)
			{
				case AddressFamily.InterNetwork: return _IsPublicIPv4Address(ipAddress);
				case AddressFamily.InterNetworkV6: return true;
				default: return false;
			}
		}

		private static bool _IsPrivateAddress(IPAddress ipAddress)
		{
			switch (ipAddress.AddressFamily)
			{
				case AddressFamily.InterNetwork: return !_IsPublicIPv4Address(ipAddress);
				case AddressFamily.InterNetworkV6: return false;
				default: return false;
			}
		}

		private static bool _IsLoopbackAddress(IPAddress ipAddress)
		{
			return IPAddress.IsLoopback(ipAddress);
		}

		private static bool _IsBroadcastAddress(IPAddress ipAddress)
		{
			return ipAddress.Equals(IPAddress.Broadcast);
		}

		private static bool _IsAnyAddress(IPAddress ipAddress)
		{
			return _EqualsAddress(ipAddress, IPAddress.Any, IPAddress.IPv6Any);
		}

		private static bool _IsNoneAddress(IPAddress ipAddress)
		{
			return _EqualsAddress(ipAddress, IPAddress.None, IPAddress.IPv6None);
		}

		private static bool _EqualsAddress(IPAddress ipAddress, IPAddress IPv4, IPAddress IPv6)
		{
			switch (ipAddress.AddressFamily)
			{
				// Use .Address because it's faster.
				#pragma warning disable 612,618 // we know what we're doing.
				case AddressFamily.InterNetwork: return ipAddress.Address == IPv4.Address;
				#pragma warning restore 612,618

				case AddressFamily.InterNetworkV6: return ipAddress.Equals(IPv6);
				default: return false;
			}
		}
	}
}
