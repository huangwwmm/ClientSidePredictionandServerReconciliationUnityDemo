#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 9074 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-09-06 15:42:59 +0200 (Tue, 06 Sep 2011) $
#endregion
using System.Net;

namespace uLink
{
	internal struct HandoverSession
	{
		public NetworkPlayer player;
		public double localTimeout;
		public NetworkEndPoint clientDebugInfo;

		public NetworkP2PHandoverInstance[] instances;
		public BitStream data;

		public HandoverSession(NetworkPlayer player, double localTimeout, NetworkP2PHandoverInstance[] instances, BitStream data, NetworkEndPoint clientDebugInfo)
		{
			this.player = player;
			this.localTimeout = localTimeout;
			this.instances = instances;
			this.data = data;
			this.clientDebugInfo = clientDebugInfo;
		}
	}
}
