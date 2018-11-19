#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11266 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-02-02 21:26:58 +0100 (Thu, 02 Feb 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Reflection;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	internal struct RPCMethod
	{
		public readonly MethodInfo info;

		public RPCMethod(Type instanceType, string name, bool isRPC)
		{
			try
			{
				const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				info = instanceType.GetMethod(name, flags);
			}
			catch (AmbiguousMatchException ex)
			{
				Log.Error(NetworkLogFlags.RPC, "Method name ", name, " declared in ", instanceType, " is ambiguous due to multiple matches: ", ex);
				info = null;
			}
			catch (Exception ex)
			{
				Log.Error(NetworkLogFlags.RPC, "Couldn't find method ", name, " declared in ", instanceType, ": ", ex);
				info = null;
			}

			if (info == null) return;

			if (isRPC && !name.StartsWith("RPC") && !Utility.HasAttributeWithPrefix(info, "RPC"))
			{
				NetworkLog.Warning(NetworkLogFlags.RPC, "Ignoring matched method ", name, " declared in ", instanceType, " because it is missing the RPC attribute or prefix");
				info = null;
			}

#if NO_COROUTINE_RPC
			if (info.ReturnType == typeof(IEnumerator))
			{
				Log.Warning(NetworkLogFlags.RPC, "Ignoring matched method " + name + " declared in " + type + " because it is a coroutine");
				info = null;
			}
#endif
		}

		public bool isExecutable
		{
			get { return info != null; }
		}

		public object Execute(object instance, params object[] parameters)
		{
			// TODO: catch reflection exceptions and NetworkException, what about other exceptions? can we rethrow them without unity crashing?

			try
			{
				return info.Invoke(instance, parameters);
			}
			catch (Exception e)
			{
				string paramstr;

				if (parameters.Length != 0)
				{
					paramstr = parameters[0].ToString();

					for (int i = 1; i < parameters.Length; i++)
					{
						paramstr += ", " + parameters[i];
					}
				}
				else
				{
					paramstr = String.Empty;
				}

				NetworkLog.Error(NetworkLogFlags.RPC, "Failed to invoke ", this, " with ", parameters.Length, " parameter(s): ", paramstr);
				NetworkLog.Error(NetworkLogFlags.RPC, e);

				// swallow the exception, so that we don't actually stop the execution flow in uLink.
				return null;
			}
		}

		public override string ToString()
		{
			return "Method " + info.DeclaringType + ":" + info.Name;
		}
	}
}
