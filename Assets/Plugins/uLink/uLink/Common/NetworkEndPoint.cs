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
		private static readonly IPEndPoint _ipEndPoint = new IPEndPoint(0, 0);
		private readonly EndPoint _value;
		public EndPoint value { get { return _value ?? unassigned._value; } }

		public IPEndPoint ipEndPoint
		{
			get
			{
				if (value is IPEndPoint) return (IPEndPoint)value;
				if (value.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
					value.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
				{
					return (IPEndPoint)_ipEndPoint.Create(value.Serialize());
				}
				throw new System.InvalidCastException();
			}
		}
		public IPAddress ipAddress { get { return ipEndPoint.Address; } }
		public int port { get { return isIPEndPoint? ipEndPoint.Port : unassignedPort; } }
	}
}
