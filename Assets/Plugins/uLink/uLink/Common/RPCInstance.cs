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
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

// TODO: create a delegate which parses args hardcoded

namespace uLink
{
	internal class RPCInstance
	{
		public RPCMethod method { get; private set; }
		public ParameterReader reader { get; private set; }
		public UnityEngine.MonoBehaviour instance { get; private set; }

		public RPCInstance(Component source, RPCReceiver receiver, UnityEngine.MonoBehaviour observed, GameObject[] listOfGameObjects, string rpcName, RuntimeTypeHandle messageInfoHandle)
		{
			UnityEngine.MonoBehaviour[] components;

			switch (receiver)
			{
				case RPCReceiver.OnlyObservedComponent: // look only at the observed component if it is a MonoBehaviour
					components = !observed.IsNullOrDestroyed() ? new[] { observed } : new UnityEngine.MonoBehaviour[0];
					break;
				case RPCReceiver.ThisGameObject: // look at all MonoBehaviours in this gameobject
					components = source.GetComponents<UnityEngine.MonoBehaviour>();
					break;
				case RPCReceiver.ThisGameObjectAndChildren: // look at all MonoBehaviours in this gameobject and all MonoBehaviours in any of it's children
					components = source.GetComponentsInChildren<UnityEngine.MonoBehaviour>();
					break;
				case RPCReceiver.RootGameObjectAndChildren: // look at all MonoBehaviours in the root of this gameobject and down to any of it's children
					components = source.transform.root.GetComponentsInChildren<UnityEngine.MonoBehaviour>();
					break;
				case RPCReceiver.AllActiveGameObjects: // look at all active MonoBehaviours in the scene
					components = Object.FindObjectsOfType(typeof(UnityEngine.MonoBehaviour)) as UnityEngine.MonoBehaviour[];
					break;
				case RPCReceiver.GameObjects:
					components = GetComponents<UnityEngine.MonoBehaviour>(listOfGameObjects);
					break;
				default:
					//throw new NetworkException("Dropping RPC, case clause!");
					return;
			}

			_Initialize(source, components, rpcName, messageInfoHandle);
		}

		public RPCInstance(Component source, UnityEngine.MonoBehaviour[] components, string rpcName, RuntimeTypeHandle messageInfoHandle)
		{
			_Initialize(source, components, rpcName, messageInfoHandle);
		}

		private void _Initialize(Component source, UnityEngine.MonoBehaviour[] components, string rpcName, RuntimeTypeHandle messageInfoHandle)
		{
			string type = "";

			int typeIndex = rpcName.IndexOf(':');
			if (typeIndex != -1)
			{
				type = rpcName.Substring(0, typeIndex);
				rpcName = rpcName.Substring(typeIndex + 1);
			}

			foreach (var component in components)
			{
				if (!String.IsNullOrEmpty(type) && component.GetType().Name != type) continue;

				var candidate = new RPCMethod(component.GetType(), rpcName, true);
				if (candidate.isExecutable)
				{
					if (instance.IsNotNull())
					{
						NetworkLog.Error(NetworkLogFlags.RPC, "Found two or more RPCs named '", rpcName, "' inside the receivers at ", source);

						instance = null;
						return;
					}

					instance = component;
					method = candidate;
				}
			}

			if (instance.IsNotNull())
			{
				reader = new ParameterReader(method, messageInfoHandle);
			}
			else
			{
				Log.Error(NetworkLogFlags.RPC, "No receiver found for RPC '", rpcName, "' at ", source);
			}
		}

		public bool Execute(object[] parameters)
		{

#if !NO_COROUTINE_RPC
			object retval =
#endif
				method.Execute(instance, parameters);

#if !NO_COROUTINE_RPC
			if (retval is IEnumerator)
			{
				Log.Debug(NetworkLogFlags.RPC, "RPC returned a IEnumerator so a coroutine with it is started on ", instance);
				instance.StartCoroutine(retval as IEnumerator);
			}
#endif

			return true;
		}

		public bool Execute(BitStream stream, object messageInfo)
		{
			if (instance.IsNullOrDestroyed()) return false;

			var parameters = reader.ReadParameters(stream, messageInfo, method);

			if (parameters != null) return Execute(parameters);
			return false;
		}

		private static T[] GetComponents<T>(GameObject[] gos) where T : Component
		{
			var arrays = new T[gos.Length][];
			int count = 0;

			for (int i = 0; i < gos.Length; i++)
			{
				var components = gos[i].GetComponents<T>();

				arrays[i] = components;
				count += components.Length;
			}

			var combined = new T[count];
			int j = 0;

			for (int i = 0; i < gos.Length; i++)
			{
				var array = arrays[i];
				int length = array.Length;

				Buffer.BlockCopy(array, 0, combined, j, length);
				j += length;
			}

			return combined;
		}
	}
}

#endif
