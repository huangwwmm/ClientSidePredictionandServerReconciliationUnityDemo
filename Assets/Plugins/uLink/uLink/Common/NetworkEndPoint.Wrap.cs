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
		public AddressFamily addressFamily
		{
			get { return value.AddressFamily; }
		}

		public SocketAddress Serialize()
		{
			return value.Serialize();
		}

		public NetworkEndPoint Create(SocketAddress socketAddress)
		{
			return new NetworkEndPoint(value.Create(socketAddress));
		}
	}
}
