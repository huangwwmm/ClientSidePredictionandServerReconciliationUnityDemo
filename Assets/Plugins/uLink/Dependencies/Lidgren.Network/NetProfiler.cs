// (c)2012 MuchDifferent. All Rights Reserved.
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System.Diagnostics;

#if UNITY_BUILD
using UnityEngine;
#endif

internal static class NetProfiler
{
	[Conditional("NET_PROFILER")]
	public static void BeginSample(string name)
	{
#if UNITY_BUILD
		Profiler.BeginSample(name);
#endif
	}

	[Conditional("NET_PROFILER")]
	public static void EndSample()
	{
#if UNITY_BUILD
		Profiler.EndSample();
#endif
	}
}