#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion

using System.Net;

namespace uLink
{
	public partial struct NetworkEndPoint
	{
		public NetworkEndPoint(EndPoint value)
		{
			_value = value;
		}

		public NetworkEndPoint(IPAddress ipAddress, int port)
			: this(new IPEndPoint(ipAddress, port))
		{
		}

		public NetworkEndPoint(string ipPortString)
			: this(_Resolve(ipPortString))
		{
		}

		public NetworkEndPoint(string ipPortString, int defaultPort)
			: this(_Resolve(ipPortString, defaultPort))
		{
		}
	}
}
