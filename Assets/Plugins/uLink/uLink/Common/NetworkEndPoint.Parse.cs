#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion

using System;
using System.Net;
using System.Net.Sockets;

namespace uLink
{
	public partial struct NetworkEndPoint
	{
		public static NetworkEndPoint Resolve(string ipPortString)
		{
			return Resolve(ipPortString, unassignedPort);
		}

		public static bool TryResolve(string ipPortString, out NetworkEndPoint endpoint)
		{
			return TryResolve(ipPortString, unassignedPort, out endpoint);
		}

		public static NetworkEndPoint Resolve(string ipPortString, int defaultPort)
		{
			return new NetworkEndPoint(_Resolve(ipPortString, defaultPort));
		}

		public static bool TryResolve(string ipPortString, int defaultPort, out NetworkEndPoint endpoint)
		{
			try
			{
				endpoint = Resolve(ipPortString, defaultPort);
				return true;
			}
			catch
			{
				endpoint = unassigned;
				return false;
			}
		}

		private static IPEndPoint _Resolve(string ipPortString)
		{
			return _Resolve(ipPortString, unassignedPort);
		}

		private static IPEndPoint _Resolve(string ipPortString, int defaultPort)
		{
			//Assert.IsNotNullOrEmpty(ipPortString, "ipPortString");

			string[] splits = ipPortString.Split(_portSeparatorChars, 2, StringSplitOptions.RemoveEmptyEntries);

			//if (splits.Length == 0) Throw.Arg("ipPortString");
			string ip = splits[0];

			int port;
			if (splits.Length == 1 || !Int32.TryParse(splits[1].Trim(), out port)) port = defaultPort;

			return new IPEndPoint(_ResolveAddress(ip), port);
		}

		/// <summary>
		/// Get IP address from notation (xxx.xxx.xxx.xxx) or hostname
		/// </summary>
		private static IPAddress _ResolveAddress(string ipOrHost)
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
				if (ip.AddressFamily == AddressFamily.InterNetwork)
					ipAddress = ip;
			}

			return ipAddress;
		}

		private static readonly char[] _portSeparatorChars = { ':' };
	}
}
