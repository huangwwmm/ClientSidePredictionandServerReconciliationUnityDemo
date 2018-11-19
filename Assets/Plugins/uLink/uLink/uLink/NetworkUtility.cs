#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11844 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:05:10 +0200 (Fri, 13 Apr 2012) $
#endregion
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

// TODO: add GetPublicAddress!

namespace uLink
{
	/// <summary>
	/// Utility class for handling hostnames and IP addresses.
	/// </summary>
	public static partial class NetworkUtility
	{
		public static string GetLocalHostName() { return Utility.GetHostName(); }

		public static string GetLocalIPAddress() { return ResolveAddress(GetLocalHostName()).ToString(); }

		/// <summary>
		/// Tries to resolve and creates an IP address from a string.
		/// </summary>
		/// <param name="ipOrHost">The IP address/computer name</param>
		/// <returns>The created IP address if successful, null otherwise.</returns>
		public static IPAddress ResolveAddress(string ipOrHost) { return Utility.Resolve(ipOrHost); }

		/// <summary>
		/// Tries to resolve an IP address with it's port from a string.
		/// </summary>
		/// <param name="ipOrHostInclPort">IP address with port separated by : as usual.</param>
		/// <param name="defaultPort">The default port. If the port is not resolvable, this will be used.</param>
		/// <returns>The IP address if resolved, null otherwise.</returns>
		public static NetworkEndPoint ResolveEndPoint(string ipOrHostInclPort, int defaultPort)
		{
			var splits = ipOrHostInclPort.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

			if (splits.Length == 0) return NetworkEndPoint.unassigned;

			int port;

			switch (splits.Length)
			{
				case 1: port = defaultPort; break;
				case 2: if (!Int32.TryParse(splits[1], out port)) port = defaultPort; break;
				default: return NetworkEndPoint.unassigned;
			}

			IPAddress ip = Utility.Resolve(splits[0]);
			return new NetworkEndPoint(ip, port);
		}

		/// <summary>
		/// Resolves a set of IP addresses.
		/// </summary>
		/// <param name="ipOrHosts">The array of IP addresses in string</param>
		/// <returns>The array of resolved IPAddress instances,
		/// The returned array always have the length of the provided argument, any element should be
		/// checked for validity.</returns>
		public static IPAddress[] ResolveAddress(string[] ipOrHosts)
		{
			var retval = new IPAddress[ipOrHosts.Length];

			for (int i = 0; i < ipOrHosts.Length; i++)
			{
				retval[i] = ResolveAddress(ipOrHosts[i]);
			}

			return retval;
		}

		/// <summary>
		/// Tries to resolve a set of IP addresses with ports.
		/// </summary>
		/// <param name="ipOrHostInclPort">The array of IPs and ports separated with : as usual.</param>
		/// <param name="defaultPort">The default port if the port can not be resolved.</param>
		/// <returns>An array in length of the provided argument array.
		/// Each element should be checked for validity.</returns>
		public static NetworkEndPoint[] ResolveEndPoint(string[] ipOrHostInclPort, int defaultPort)
		{
			var retval = new NetworkEndPoint[ipOrHostInclPort.Length];

			for (int i = 0; i < ipOrHostInclPort.Length; i++)
			{
				retval[i] = ResolveEndPoint(ipOrHostInclPort[i], defaultPort);
			}

			return retval;
		}

		/// <summary>
		/// Tries to resolve a set of IP addresses and ports.
		/// Works just like <see cref="ResolveEndPoint(string[],int)"/>
		/// just uses different kinds of arguments.
		/// </summary>
		/// <param name="ipOrHostInclOrDefaultPort">The pairs of string addresses and default ports for each address.</param>
		/// <returns></returns>
		public static NetworkEndPoint[] ResolveEndPoint(KeyValuePair<string, int>[] ipOrHostInclOrDefaultPort)
		{
			var retval = new NetworkEndPoint[ipOrHostInclOrDefaultPort.Length];

			for (int i = 0; i < ipOrHostInclOrDefaultPort.Length; i++)
			{
				var pair = ipOrHostInclOrDefaultPort[i];
				retval[i] = ResolveEndPoint(pair.Key, pair.Value);
			}

			return retval;
		}

		/// <summary>
		/// Gets my local IP address (not necessarily external) and subnet mask
		/// </summary>
		public static NetworkAddressInfo[] GetLocalAddresses()
		{
			var addresses = new List<NetworkAddressInfo>();

			try
			{
				NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

				foreach (NetworkInterface adapter in nics)
				{
					if (adapter.OperationalStatus != OperationalStatus.Up)
						continue;
					if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback || adapter.NetworkInterfaceType == NetworkInterfaceType.Unknown)
						continue;
					if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
						continue;

					IPInterfaceProperties properties = adapter.GetIPProperties();
					foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
					{
						if (unicastAddress != null && unicastAddress.Address != null && unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							addresses.Add(new NetworkAddressInfo(unicastAddress.Address, unicastAddress.IPv4Mask));
						}
					}
				}
			}
			catch (Exception e)
			{
#if PIKKO_BUILD
				Log.Error("Failed to get local addresses. This may be due to webplayer security: {0}", e);
#else
				Log.Error(NetworkLogFlags.Utility, "Failed to get local addresses. This may be due to webplayer security: ", e);
#endif
			}

			return addresses.ToArray();
		}

		/// <summary>
		/// Returns true if the NetworkEndPoint supplied is a public IP address
		/// </summary>
		/// <remarks>
		/// An IP address is considered public if the IP number is valid and falls outside any of the IP address ranges reserved for private uses by Internet standards groups.
		/// Private IP addresses are typically used on local networks including home, school and business LANs including airports and hotels.
		/// </remarks>
		public static bool IsPublicAddress(NetworkEndPoint ipEndpoint)
		{
			return ipEndpoint.isIPEndPoint && IsPublicAddress(ipEndpoint.ipAddress);
		}

		/// <summary>
		/// Returns true if the IPAddress supplied is a public IP address
		/// </summary>
		/// <remarks>
		/// An IP address is considered public if the IP number is valid and falls outside any of the IP address ranges reserved for private uses by Internet standards groups.
		/// Private IP addresses are typically used on local networks including home, school and business LANs including airports and hotels.
		/// </remarks>
		public static bool IsPublicAddress(IPAddress ipAddress)
		{
			var bytes = ipAddress.GetAddressBytes();

			if (bytes.Length == 4)
			{
				uint ipv4 = (uint)(bytes[0] << 24) | (uint)(bytes[1] << 16) | (uint)(bytes[2] << 8) | bytes[3];

				return (ipv4 < 0x0A000000 || ipv4 > 0x0AFFFFFF) // not 10.0.0.0    - 10.255.255.255
					&& (ipv4 < 0xA9FE0000 || ipv4 > 0xA9FEFFFF) // not 169.254.0.0 - 169.254.255.255
					&& (ipv4 < 0xAC100000 || ipv4 > 0xAC1FFFFF) // not 172.16.0.0  - 172.31.255.255
					&& (ipv4 < 0xC0A80000 || ipv4 > 0xC0A8FFFF) // not 192.168.0.0 - 192.168.255.255
					&& ipv4 != 0x7F000001; // not 127.0.0.1
			}

			return true; // default
		}

		/// <summary>
		/// Is the address loop back address (address of the local machine).
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns><c>true</c> if the address is for the local machine, <c>false</c> otherwise.</returns>
		/// <remarks>You can connect to your machine (yourself) using the loop back address.
		/// It's denoted using 127.0.0.1 or localhost usually.</remarks>
		public static bool IsLoopbackAddress(NetworkEndPoint endpoint)
		{
			return endpoint.isIPEndPoint && IsLoopbackAddress(endpoint.ipAddress);
		}

		/// <summary>
		/// Is the provided IP address, the loop back address (address of the local machine)
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <returns></returns>
		/// <remarks>You can connect to your machine (yourself) using the loop back address.
		/// It's denoted using 127.0.0.1 or localhost usually.</remarks>
		public static bool IsLoopbackAddress(IPAddress ipAddress)
		{
			return IPAddress.IsLoopback(ipAddress);
		}

		[Obsolete("NetworkUtility.IsAddressLocal is deprecated, please use NetworkUtility.IsPublicAddress instead")]
		public static bool IsAddressLocal(NetworkEndPoint endpoint)
		{
			return !IsPublicAddress(endpoint);
		}

		[Obsolete("NetworkUtility.IsAddressLocal is deprecated, please use NetworkUtility.IsPublicAddress instead")]
		public static bool IsAddressLocal(IPAddress ipAddress)
		{
			return !IsPublicAddress(ipAddress);
		}

		/// <summary>
		/// Returns wether the port is available for binding to/opening.
		/// </summary>
		/// <param name="port"></param>
		/// <returns><c>true</c> if no one else opend the port and you can listen to it, <c>false</c> otherwise.</returns>
		public static bool IsPortAvailable(int port)
		{
			Socket sock = null;
			bool available = false;

			try
			{
				sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				sock.Bind(new NetworkEndPoint(IPAddress.Any, port));
				available = sock.IsBound;
			}
			catch (SocketException)
			{
				// ignore error
			}
			finally
			{
				if (sock != null) sock.Close();
			}

			return available;
		}

		/// <summary>
		/// Finds an available port for openning and returns it.
		/// </summary>
		/// <returns>A port which is not used by any one and can be used.</returns>
		public static int FindAvailablePort()
		{
			return FindAvailablePort(0, 0);
		}

		/// <summary>
		/// Finds an available port between the range provided.
		/// </summary>
		/// <param name="startPort">The starting port range</param>
		/// <param name="endPort">The ending port range</param>
		/// <returns>The port number if found, -1 otherwise.</returns>
		public static int FindAvailablePort(int startPort, int endPort)
		{
			Socket sock = null;
			int availablePort = -1;

			try
			{
				sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

				for (int port = startPort; port <= endPort; port++)
				{
					try
					{
						sock.Bind(new IPEndPoint(IPAddress.Any, port));

						if (sock.IsBound)
						{
							var endpoint = sock.LocalEndPoint as IPEndPoint;
							if (endpoint != null) availablePort = (sock.LocalEndPoint as IPEndPoint).Port;
							break;
						}
					}
					catch (SocketException)
					{
						// ignore error
					}
				}
			}
			finally
			{
				if (sock != null) sock.Close();
			}

			return availablePort;
		}

		/// <summary>
		/// Tries to find an available port between the ports in the array.
		/// </summary>
		/// <param name="ports">The array of ports which we hope to find an available port between them.</param>
		/// <returns>The available port number, -1 if no available port found.</returns>
		public static int FindAvailablePort(int[] ports)
		{
			Socket sock = null;
			int availablePort = -1;

			try
			{
				sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

				foreach (var port in ports)
				{
					try
					{
						sock.Bind(new IPEndPoint(IPAddress.Any, port));

						if (sock.IsBound)
						{
							var endpoint = sock.LocalEndPoint as IPEndPoint;
							if (endpoint != null) availablePort = (sock.LocalEndPoint as IPEndPoint).Port;
							break;
						}
					}
					catch (SocketException)
					{
						// ignore error
					}
				}
			}
			finally
			{
				if (sock != null) sock.Close();
			}

			return availablePort;
		}

		/// <summary>
		/// Returns a string representing the type of the peer and network status that the peer is in.
		/// </summary>
		/// <param name="peerType"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public static string GetStatusString(NetworkPeerType peerType, NetworkStatus status)
		{
			switch (peerType)
			{
				case NetworkPeerType.Disconnected:
					return "Disconnected";

				case NetworkPeerType.Server:
					switch (status)
					{
						case NetworkStatus.Disconnected: return "Server Disconnected";
						case NetworkStatus.Connecting: return "Server Initializing";
						case NetworkStatus.Connected: return "Server Initialized";
						case NetworkStatus.Disconnecting: return "Server Shuttingdown";
					}
					break;

				case NetworkPeerType.Client:
					switch (status)
					{
						case NetworkStatus.Disconnected: return "Client Disconnected";
						case NetworkStatus.Connecting: return "Client Connecting";
						case NetworkStatus.Connected: return "Client Connected";
						case NetworkStatus.Disconnecting: return "Client Disconnecting";
					}
					break;

				case NetworkPeerType.CellServer:
					switch (status)
					{
						case NetworkStatus.Disconnected: return "CellServer Disconnected";
						case NetworkStatus.Connecting: return "CellServer Connecting";
						case NetworkStatus.Connected: return "CellServer Connected";
						case NetworkStatus.Disconnecting: return "CellServer Disconnecting";
					}
					break;
			}

			return peerType.ToString() + status;
		}

		/// <summary>
		/// Returns a string representing a <see cref="uLink.NetworkConnectionError"/>
		/// </summary>
		/// <param name="error"></param>
		/// <returns></returns>
		public static string GetErrorString(NetworkConnectionError error)
		{
			switch (error)
			{
				case NetworkConnectionError.InternalDirectConnectFailed: return "Internal Failure";
				case NetworkConnectionError.EmptyConnectTarget: return "Empty Target";
				case NetworkConnectionError.IncorrectParameters: return "Incorrect Parameters";
				case NetworkConnectionError.CreateSocketOrThreadFailure: return "Socket Failure";
				case NetworkConnectionError.AlreadyConnectedToAnotherServer: return "Already Connected";
				case NetworkConnectionError.NoError: return "No Error";
				case NetworkConnectionError.ConnectionFailed: return "Connection Failed";
				case NetworkConnectionError.TooManyConnectedPlayers: return "Too Many Players";
				case NetworkConnectionError.LimitedPlayers: return "Limited Players";
				case NetworkConnectionError.RSAPublicKeyMismatch: return "Public Key Mismatch";
				case NetworkConnectionError.ConnectionBanned: return "Connection Banned";
				case NetworkConnectionError.InvalidPassword: return "Invalid Password";
				case NetworkConnectionError.NATTargetNotConnected: return "NAT Target Not Connected";
				case NetworkConnectionError.NATTargetConnectionLost: return "NAT Target Connection Lost";
				case NetworkConnectionError.ConnectionTimeout: return "Connection Timeout";
				case NetworkConnectionError.IsAuthoritativeServer: return "Authoritative Server";
				case NetworkConnectionError.ProxyTargetNotConnected: return "Proxy Target Not Connected";
				case NetworkConnectionError.ProxyTargetNotRegistered: return "Proxy Target Not Registered";
				case NetworkConnectionError.ProxyServerNotEnabled: return "Proxy Server Not Enabled";
				case NetworkConnectionError.ProxyServerOutOfPorts: return "Proxy Server Out Of Ports";
				case NetworkConnectionError.IncompatibleVersions: return "Incompatible uLink versions";
				default:
					if (error >= NetworkConnectionError.UserDefined1)
						return "User Defined " + (error - NetworkConnectionError.UserDefined1 + 1);
					break;
			}

			return error.ToString();
		}

		#region IPv6Utility
		private static bool _isSupportIPv6 = false;
		public static bool IsSupportIPv6()
		{
			return _isSupportIPv6;
		}

		public static void SetIPv6Supported(bool isSupport)
		{
			_isSupportIPv6 = isSupport;
		}
		#endregion
	}
}
