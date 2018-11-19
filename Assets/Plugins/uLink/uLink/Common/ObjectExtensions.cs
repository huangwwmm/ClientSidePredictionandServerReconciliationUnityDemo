#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11844 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:05:10 +0200 (Fri, 13 Apr 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;

#if UNITY_BUILD
using UnityEngine;
using Object = UnityEngine.Object;
#else
using Object = uLink.NetworkViewBase;
#endif

namespace uLink
{
	internal static class ObjectExtensions
	{
		internal static bool EqualsOrDestroyed(this Object a, Object b)
		{
			return a == b; // this calls Unity's overloaded operator which goes into native code.
		}

		internal static bool IsNullOrDestroyed(this Object obj)
		{
			return obj == null; // this calls Unity's overloaded operator which goes into native code.
		}

		internal static bool IsNotNullOrDestroyed(this Object obj)
		{
			return obj != null; // this calls Unity's overloaded operator which goes into native code.
		}

		internal static bool IsDestroyed(this Object obj)
		{
			return IsNotNull(obj) && IsNullOrDestroyed(obj);
		}

		internal static bool IsNotDestroyed(this Object obj)
		{
			return IsNull(obj) || IsNotNullOrDestroyed(obj);
		}

		internal static bool ReferenceEquals(this Object a, Object b)
		{
			return System.Object.ReferenceEquals(a, b);
		}

		internal static bool IsNull(this Object obj)
		{
			// ReSharper disable RedundantCast.0
			return (object)obj == null;
			// ReSharper restore RedundantCast.0
		}

		internal static bool IsNotNull(this Object obj)
		{
			// ReSharper disable RedundantCast.0
			return (object)obj != null;
			// ReSharper restore RedundantCast.0
		}

		internal static bool IsUnassigned(this Object obj)
		{
#if UNITY_BUILD
			return obj.GetInstanceID() == 0;
#else
			return false;
#endif
		}

		internal static bool IsNotUnassigned(this Object obj)
		{
#if UNITY_BUILD
			return obj.GetInstanceID() != 0;
#else
			return true;
#endif
		}

		internal static bool IsNullOrUnassigned(this Object obj)
		{
#if UNITY_BUILD
			return IsNull(obj) || IsUnassigned(obj);
#else
			return IsNull(obj);
#endif
		}

		internal static bool IsNotNullOrUnassigned(this Object obj)
		{
#if UNITY_BUILD
			return IsNotNull(obj) && IsNotUnassigned(obj);
#else
			return IsNotNull(obj);
#endif
		}

		internal static string GetName(this Object obj)
		{
#if UNITY_BUILD
			// NOTE: To avoid a potential weird Unity/IL2CPP issue we wrap the property Object.name inside this function.
			return obj.name;
#else
			return obj.ToString();
#endif
		}

		internal static string GetName(this GameObject go)
		{
			// NOTE: To avoid a potential weird Unity/IL2CPP issue we wrap the property Object.name inside this function.
			return go.name;
		}

		internal static string GetName(this Component component)
		{
			// NOTE: To avoid a potential weird Unity/IL2CPP issue we wrap the property Object.name inside this function.
			return component.gameObject.name;
		}

		internal static string GetNameNullSafe(this GameObject go)
		{
			return !IsNullOrDestroyed(go) ? go.GetName() : String.Empty;
		}

		internal static string GetNameNullSafe(this Component component)
		{
			return !IsNullOrDestroyed(component) ? component.GetName() : String.Empty;
		}


		// NOTE: the reason we have these following separate overloads is because of the old internal tests

		internal static bool IsNullOrDestroyed(this GameObject go)
		{
			return go == null; // this calls Unity's overloaded operator which goes into native code.
		}

		internal static bool IsNullOrDestroyed(this Component component)
		{
			return component == null; // this calls Unity's overloaded operator which goes into native code.
		}
	}
}
