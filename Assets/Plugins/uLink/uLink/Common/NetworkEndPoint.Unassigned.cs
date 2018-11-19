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
		public const int unassignedPort = -1;

		public static readonly NetworkEndPoint unassigned = new NetworkEndPoint(Unassigned.singleton);

		private sealed class Unassigned : EndPoint
		{
			public static readonly Unassigned singleton = new Unassigned();

			private const AddressFamily _family = AddressFamily.Unspecified;

			private static readonly SocketAddress _dummyAddress = new SocketAddress(_family, 2);

			private Unassigned()
			{
			}

			public override AddressFamily AddressFamily
			{
				get { return _family; }
			}

			public override SocketAddress Serialize()
			{
				return _dummyAddress;
			}

			public override EndPoint Create(SocketAddress socketAddress)
			{
				return this;
			}

			public override bool Equals(object other)
			{
				return other is Unassigned;
			}

			public override int GetHashCode()
			{
				return -1;
			}

			public override string ToString()
			{
				return "Unassigned";
			}
		}
	}
}
