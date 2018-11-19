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
		public static implicit operator NetworkEndPoint(IPEndPoint endpoint) { return new NetworkEndPoint(endpoint); }
		public static implicit operator NetworkEndPoint(EndPoint endpoint) { return new NetworkEndPoint(endpoint); }
		public static implicit operator NetworkEndPoint(string endpoint) { return new NetworkEndPoint(endpoint); }

		public static implicit operator IPEndPoint(NetworkEndPoint container) { return container.ipEndPoint; }
		public static implicit operator EndPoint(NetworkEndPoint container) { return container.value; }
		public static implicit operator string(NetworkEndPoint container) { return container.ToString(); }

		public TEndPoint Cast<TEndPoint>()
			where TEndPoint : EndPoint
		{
			return (TEndPoint)value;
		}

		public TEndPoint TryCast<TEndPoint>()
			where TEndPoint : EndPoint
		{
			return value as TEndPoint;
		}
	}
}
