#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion

using System.Net.Sockets;

namespace uLink
{
	public abstract partial class NetworkSocket
	{
		public static bool supportsIPv4 { get { return Socket.SupportsIPv4; } }
		public static bool supportsIPv6 { get { return Socket.OSSupportsIPv6; } }
	}
}
