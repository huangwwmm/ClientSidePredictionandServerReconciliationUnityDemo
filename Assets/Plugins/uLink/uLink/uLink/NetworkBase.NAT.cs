#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	internal abstract class NetworkBaseNAT : NetworkBaseServer
	{
		// TODO: implement these and set defaults:
		public string connectionTesterIP;
		public int connectionTesterPort;

		// TODO: implement these and set defaults:
		public string natFacilitatorIP;
		public int natFacilitatorPort;

		public ConnectionTesterStatus TestConnection(bool forceTest)
		{
			// TODO: implement
			return ConnectionTesterStatus.Error;
		}

		public ConnectionTesterStatus TestConnectionNAT()
		{
			// TODO: implement
			return ConnectionTesterStatus.Error;
		}

		public bool HavePublicAddress()
		{
			var localAddrs = NetworkUtility.GetLocalAddresses();

			foreach (var localAddr in localAddrs)
			{
				if (NetworkUtility.IsPublicAddress(localAddr.ipAddress)) return true;
			}

			return false;
		}
	}
}
