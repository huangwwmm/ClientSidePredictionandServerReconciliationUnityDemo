#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10139 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:11:15 +0100 (Tue, 29 Nov 2011) $
#endregion

using System;
using System.Net;

namespace uLink
{
	/// <summary>
	/// Represents an IP address plus mask
	/// </summary>
	public struct NetworkAddressInfo
	{
		/// <summary>
		/// The IP address that we represent.
		/// </summary>
		public readonly IPAddress ipAddress;
		/// <summary>
		/// subnet mask of the IP address.
		/// </summary>
		public readonly IPAddress mask;

		/// <summary>
		/// Creates a NetworkAddressInfo object with the provided address.
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="mask"></param>
		public NetworkAddressInfo(IPAddress ipAddress, IPAddress mask)
		{
			this.ipAddress = ipAddress;
			this.mask = mask;
		}

		/// <summary>
		/// Returns if we are in the same subnet with the provided end point.
		/// </summary>
		/// <param name="endpoint">The end point that we want to compare ourself to</param>
		/// <returns>True if we are in the same subnet, otherwise false.</returns>
		public bool HasSameSubnet(NetworkEndPoint endpoint)
		{
			return endpoint.isIPEndPoint && HasSameSubnet(endpoint.ipAddress);
		}

		/// <summary>
		/// Returns if the other IP address has the same subnet (based on IP classes).
		/// </summary>
		/// <param name="remote">The remote IP address that we want to compare to ourself.</param>
		/// <returns>true if the other IP is in the same class that we are, otherwise false.</returns>
		public bool HasSameSubnet(IPAddress remote)
		{
			if (remote == null || ipAddress == null || mask == null) return false;

			uint remoteBits = BitConverter.ToUInt32(remote.GetAddressBytes(), 0);
			uint ipBits = BitConverter.ToUInt32(ipAddress.GetAddressBytes(), 0);
			uint maskBits = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);

			return (remoteBits & maskBits) == (ipBits & maskBits);
		}
	}
}
