#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8638 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-20 04:03:00 +0200 (Sat, 20 Aug 2011) $
#endregion

using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// This enum represents the different ways which you can use to measure time.
	/// </summary>
	public enum NetworkTimeMeasurementFunction
	{
		/// <summary>
		/// This is .NET's StopWatch which uses high performance timers of the CPU most of the times. 
		/// See MSDN docs for StopWatch for more information.
		/// </summary>
		Stopwatch,
		/// <summary>
		/// Uses the lower resolution GetTickCount windows API which provides better results in some situations.
		/// </summary>
		TickCount,
		/// <summary>
		/// Uses .NET's DateTime class.
		/// See MSDN for more information on this class.
		/// </summary>
		DateTime,
	}

	public static partial class NetworkTime
	{
		/// <summary>
		/// Gets the value of a local smooth monotonic and accurate clock (in seconds).
		/// </summary>
		/// <remarks>
		/// This can for example be compared against NetworkMessageInfo.localTimestamp to determine how long ago a message was sent.
		/// The clock is monotonic (i.e. no backward leaps), smooth (i.e. no forward leaps), and has greater accuracy than UnityEngine.Time.time (or similar),
		/// so it can also be used to measure execution time of performance-critical tasks etc.
		/// </remarks>
		public static double localTime
		{
			get { return NetTime.Now; }
		}

		internal static long _GetElapsedTimeInMillis(double oldLocalTime)
		{
			return NetTime.ToMillis(NetTime.Now - oldLocalTime);
		}

		/// <summary>
		/// Gets or sets the time measurement function for uLink.
		/// </summary>
		/// <remarks>
		/// Usually StopWatch should be the best but sometimes TickCount works better despite the fact that it has a lower resolution.
		/// </remarks>
		public static NetworkTimeMeasurementFunction timeMeasurementFunction
		{
			get { return (NetworkTimeMeasurementFunction)NetTime.timeMeasurementFunction; }
			set { NetTime.timeMeasurementFunction = (NetTime.TimeMeasurementFunction)value; }
		}


#if false // TODO: add support in editor GUI to select timeMeasurementFunction and then enable the code below
		/// <summary>
		/// Gets the persistent NetworkTime properties from <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		public static void GetPrefs()
		{
			timeMeasurementFunction = (NetworkTimeMeasurementFunction)NetworkPrefs.Get("NetworkTime.timeMeasurementFunction", (int)NetworkTimeMeasurementFunction.Stopwatch);
		}

		/// <summary>
		/// Sets the persistent NetworkTime properties in <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		/// <remarks>
		/// The method can't update the saved values in the persistent <see cref="uLink.NetworkPrefs.resourcePath"/> file,
		/// because that assumes the file is editable (i.e. the running project isn't built) and would require file I/O permission.
		/// Calling this will only update the values in memory.
		/// </remarks>
		public static void SetPrefs()
		{
			NetworkPrefs.Set("NetworkTime.timeMeasurementFunction", (int)NetTime.timeMeasurementFunction);
		}
#endif
	}
}
