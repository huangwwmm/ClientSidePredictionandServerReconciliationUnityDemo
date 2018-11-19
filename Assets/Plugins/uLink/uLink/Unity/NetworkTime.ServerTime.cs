#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8638 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-20 04:03:00 +0200 (Sat, 20 Aug 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

namespace uLink
{
	public static partial class NetworkTime
	{
		/// <summary>
		/// Gets the value of a server-synchronized monotonic clock (in seconds).
		/// </summary>
		/// <returns>
		/// If called on a client, the clock will return a monotonic approximate value of the server抯 NetworkTime.localTime.
		/// If called on a server, the clock will return the same exact value as the server抯 NetworkTime.localTime.
		/// If called on a disconnected uLink instance, the clock will return zero.
		/// </returns>
		/// <remarks>
		/// The clock is monotonic (i.e. no backward leaps), but makes no effort to be smooth (i.e. it may leap forward).
		/// It is deemed to give a more up-to-date estimated value of the remote server clock than NetworkTime.smoothServerTime, but less up-to-date than NetworkTime.rawServerTime.
		/// The clock can for example be compared against NetworkMessageInfo.serverTimestamp to determine how long ago a message was sent.
		/// </remarks>
		public static double serverTime
		{
			get { return Network._singleton.serverTime; }
		}

		/// <summary>
		/// Gets the value of a server-synchronized smooth monotonic clock (in seconds).
		/// </summary>
		/// <returns>
		/// If called on a client, the clock will return a continues approximate value of the server抯 NetworkTime.localTime.
		/// If called on a server, the clock will return the same exact value as the server抯 NetworkTime.localTime.
		/// If called on a disconnected uLink instance, the clock will return zero.
		/// </returns>
		/// <remarks>
		/// The clock is monotonic (i.e. no backward leaps) and smooth (i.e. no forward leaps).
		/// However, it is deemed to give the least up-to-date estimated value of the remote server clock than NetworkTime.serverTime or NetworkTime.rawServerTime.
		/// </remarks>
		public static double smoothServerTime
		{
			get { return Network._singleton.smoothServerTime; }
		}

		/// <summary>
		/// Gets the value of a server-synchronized up-to-date clock (in seconds).
		/// </summary>
		/// <returns>
		/// If called on a client, the clock will return a latest approximate value of the server抯 NetworkTime.localTime.
		/// If called on a server, the clock will return the same exact value as the server抯 NetworkTime.localTime.
		/// If called on a disconnected uLink instance, the clock will return zero.
		/// </returns>
		/// <remarks>
		/// The clock makes no effort to be monotonic (i.e. it may leap backwards), or smooth (i.e. it may leap forwards).
		/// However, it is deemed to give the most up-to-date estimated value of the remote server clock,
		/// compared to both NetworkTime.serverTime and NetworkTime.smoothServerTime.
		/// The clock can for example be compared against NetworkMessageInfo.rawServerTimestamp to determine how long ago a message was sent.
		/// </remarks>
		public static double rawServerTime
		{
			get { return Network._singleton.rawServerTime; }
		}

		public static double rawServerTimeOffset
		{
			get { return Network._singleton.rawServerTimeOffset; }
			set { Network._singleton.rawServerTimeOffset = value; }
		}

		/// <summary>
		/// Indicates whether the server clock is available or not.
		/// </summary>
		/// <returns>
		/// Returns true if called on a connected (or disconnecting) client or on a initialized server, otherwise returns false.
		/// </returns>
		/// <remarks>
		/// If the server clock is not available, then all server time values will return zero.
		/// </remarks>
		public static bool isServerTimeAvailable
		{
			get { return Network._singleton.isServerTimeAvailable; }
		}

		public static bool isServerTimeAutoSynchronized
		{
			get { return Network._singleton.isServerTimeAutoSynchronized; }
			set { Network._singleton.isServerTimeAutoSynchronized = value; }
		}
	}
}

#endif
