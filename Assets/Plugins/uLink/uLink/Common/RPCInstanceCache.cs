#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12057 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-05-10 16:28:27 +0200 (Thu, 10 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using System.Collections.Generic;
using UnityEngine;

namespace uLink
{
	internal struct RPCInstanceCache
	{
		private RPCReceiver _receiver;
		private Component _observed;
		private readonly System.Collections.Generic.Dictionary<string, RPCInstance> _rpcs;
		private readonly RuntimeTypeHandle _messageInfoHandle;

		public RPCInstanceCache(RuntimeTypeHandle messageInfoHandle)
		{
			_receiver = RPCReceiver.Off;
			_observed = null;
			_rpcs = new System.Collections.Generic.Dictionary<string, RPCInstance>();
			_messageInfoHandle = messageInfoHandle;
		}

		public RPCInstance Find(Component source, RPCReceiver receiver, Component observed, GameObject[] listOfGameObjects, string rpcName)
		{
			if (_receiver != receiver || !_observed.ReferenceEquals(observed) || _observed.IsDestroyed())
			{
				_receiver = receiver;
				_observed = observed.IsNotDestroyed() ? observed : null;
				_rpcs.Clear();
			}

			RPCInstance rpc;
			if (!_rpcs.TryGetValue(rpcName, out rpc) || rpc.instance.IsNullOrDestroyed())
			{
				rpc = new RPCInstance(source, receiver, observed as UnityEngine.MonoBehaviour, listOfGameObjects, rpcName, _messageInfoHandle);
				_rpcs[rpcName] = rpc;
			}

			return rpc;
		}

		public void Clear()
		{
			_rpcs.Clear();
		}
	}
}

#endif