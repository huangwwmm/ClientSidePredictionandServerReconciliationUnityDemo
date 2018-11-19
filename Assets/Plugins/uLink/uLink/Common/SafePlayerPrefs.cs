#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11844 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:05:10 +0200 (Fri, 13 Apr 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
namespace uLink
{
	internal static partial class SafePlayerPrefs
	{
		public static bool TryHasKey(string key)
		{
#if UNITY_BUILD
			try
			{
				return UnityEngine.PlayerPrefs.HasKey(key);
			}
			catch
			{
			}
#endif

			return false;
		}
	}
}
