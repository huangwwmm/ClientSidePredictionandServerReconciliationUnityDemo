/* Copyright (c) 2008 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#define ULINK //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if ULINK || ULOBBY
#define NO_LIDGREN_THREADS
#endif

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

#if UNITY_BUILD
using UnityEngine;
#endif

// TODO: unify all products to use the same timer somehow!

namespace Lidgren.Network
{
	/// <summary>
	/// Time service
	/// </summary>
	internal static class NetTime
	{
		public static ulong NowInMillis
		{
			get { return (ulong)ToMillis(Now); }
		}

		public static long ToMillis(double time)
		{
			return (long)(time * 1000.0 + 0.5);
		}

#if ULOBBY // TODO: ugly hack for uLobby until we unify the time class between uLink and uLobby
		public static double Now
		{
			get
			{
				return uLink.NetworkTime.localTime;
			}
		}
#else
		public enum TimeMeasurementFunction
		{
			Stopwatch,
			TickCount,
			DateTime,
		}

		public static TimeMeasurementFunction timeMeasurementFunction = TimeMeasurementFunction.Stopwatch;

#if !NO_LIDGREN_THREADS
		private readonly static object _threadLock = new object();
#endif

		/// <summary>
		/// Get number of seconds since the application started
		/// </summary>
		public static double Now
		{
			get
			{
#if !NO_LIDGREN_THREADS
				lock (_threadLock)
#endif
				{
					switch (timeMeasurementFunction)
					{
						case TimeMeasurementFunction.Stopwatch:
							return NetworkClock_Win32.isAvailable ? NetworkClock_Win32.GetLocalTime() : NetworkClock_Stopwatch.GetLocalTime();

						case TimeMeasurementFunction.TickCount:
							return NetworkClock_TickCount.GetLocalTime();

						case TimeMeasurementFunction.DateTime:
							return NetworkClock_DateTime.GetLocalTime();

						default:
							return 0;
					}
				}
			}
		}

		private static class NetworkClock_DateTime
		{
			private static readonly long _initalTicks = DateTime.UtcNow.Ticks;

			public static double GetLocalTime()
			{
				return (DateTime.UtcNow.Ticks - _initalTicks) * 0.0000001;
			}
		}

		private static class NetworkClock_Stopwatch
		{
			private static readonly long _initalTimestamp = Stopwatch.GetTimestamp();
			private static readonly long _initalFrequency = Stopwatch.Frequency;
			private static readonly double _initalInvFreq = 1.0 / _initalFrequency;

			public static double GetLocalTime()
			{
				long currentFrequency = Stopwatch.Frequency;
				if (currentFrequency != _initalFrequency)
				{
					Log.Warning(LogFlags.Socket, "Falling back on TickCount because Stopwatch changed frequency from ", _initalFrequency, " to ", Stopwatch.Frequency);

					timeMeasurementFunction = TimeMeasurementFunction.TickCount;
					return NetworkClock_TickCount.GetLocalTime();
				}

				long currentTimestamp = Stopwatch.GetTimestamp();
				return (currentTimestamp - _initalTimestamp) * _initalInvFreq;
			}
		}

		private static class NetworkClock_TickCount
		{
			private static int _prevTickCount = Environment.TickCount;
			private static ulong _accuTickCount = 0;

			public static double GetLocalTime()
			{
				int curTickCount = Environment.TickCount;
				int deltaTickCount = unchecked(curTickCount - _prevTickCount);
				_prevTickCount = curTickCount;

				_accuTickCount += (ulong)deltaTickCount;
				return _accuTickCount * 0.001;
			}
		}

		private static class NetworkClock_Win32
		{
			// TODO: add [SuppressUnmanagedCodeSecurity] to skip security checks, if fully trusted code (i.e. most likely not webplayer or windows store/phone).
			[DllImport("Kernel32.dll")]
			private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

			// TODO: add [SuppressUnmanagedCodeSecurity] to skip security checks, if fully trusted code (i.e. most likely not webplayer or windows store/phone).
			[DllImport("Kernel32.dll")]
			private static extern bool QueryPerformanceFrequency(out long lpFrequency);

			// TODO: add [SuppressUnmanagedCodeSecurity] to skip security checks, if fully trusted code (i.e. most likely not webplayer or windows store/phone).
			[DllImport("winmm.dll")]
			private static extern uint timeGetTime();

			private static long first;
			private static long freq;
			private static double invFreq;

			private static uint prev;
			private static ulong accu;

			public static readonly bool isAvailable;

			static NetworkClock_Win32()
			{
				if (!_IsWindows()) return;

				isAvailable = _IsAvailable();
			}

			public static double GetLocalTime()
			{
				long time;
				if (freq != 0 && QueryPerformanceCounter(out time))
				{
					long newFreq = 0;
					if (QueryPerformanceFrequency(out newFreq) && newFreq == freq)
					{
						return (time - first) * invFreq;
					}
				
					Log.Warning(LogFlags.Socket, "Falling back to timeGetTime because QueryPerformanceFrequency changed frequency from ", freq, " to ", newFreq);
					freq = 0;
				}

				uint cur = timeGetTime();
				uint delta = unchecked(cur - prev);
				prev = cur;

				accu += delta;
				return accu * 0.001;
			}

			private static bool _IsAvailable()
			{
				try
				{
					if (!QueryPerformanceCounter(out first) || !QueryPerformanceFrequency(out freq) || freq == 0)
					{
						Log.Debug(LogFlags.Socket, "Falling back to timeGetTime because QueryPerformanceCounter/QueryPerformanceFrequency failed");
						freq = 0;
					}

					if (freq != 0) invFreq = 1.0 / freq;

					prev = timeGetTime();

					return true;
				}
				catch
				{
					return false;
				}
			}

			private static bool _IsWindows()
			{
#if UNITY_BUILD
				// NOTE: on some platforms Environment.OSVersion.Platform will incorrectly tell us it's Windows, that's why we use Application.platform instead.
				var platform = Application.platform;

				return
					platform == RuntimePlatform.WindowsEditor |
					platform == RuntimePlatform.WindowsPlayer |
					platform == RuntimePlatform.WindowsWebPlayer;
#else
				switch (Environment.OSVersion.Platform) 
				{
					case PlatformID.Win32NT:
					case PlatformID.Win32S:
					case PlatformID.Win32Windows:
					case PlatformID.WinCE:
						return true;
					case PlatformID.Unix:
					case PlatformID.MacOSX:
					case PlatformID.Xbox:
					default:
						return false;
				}
#endif
			}
		}
#endif
	}
}