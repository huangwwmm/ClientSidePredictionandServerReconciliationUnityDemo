#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12061 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-14 21:25:28 +0200 (Mon, 14 May 2012) $
#endregion

using System;
using Lidgren.Network;

namespace uLink
{
	internal partial class NetworkBaseLocal
	{
		private const double _unassignedServerTime = 0;

		private double _rawServerTimeOffset;

		private double _lastMonotonicServerTime;

		private double _smoothLastUpdateLocalTime;
		private double _smoothLastUpdateServerTime;
		private double _smoothLastUpdateServerTimeSkew;
		private double _smoothAverageServerTimeSkew;

		private double _lastMonotonicTimestamp;

		public bool isServerTimeAutoSynchronized = true;

		public double serverTime
		{
			get { return _GetMonotonicServerTime(NetworkTime.localTime); }
		}

		public double smoothServerTime
		{
			get { return _GetSmoothServerTime(NetworkTime.localTime); }
		}

		public double rawServerTime
		{
			get { return _GetRawServerTime(NetworkTime.localTime); }
		}

		public double rawServerTimeOffset
		{
			get
			{
				return _rawServerTimeOffset;
			}

			set
			{
				if (isServerTimeAutoSynchronized) throw new InvalidOperationException("Can't set rawServerTimeOffset if isServerTimeAutoSynchronized is enabled");

				_rawServerTimeOffset = value;
			}
		}

		public bool isServerTimeAvailable
		{
			get { return _status == NetworkStatus.Connected || _status == NetworkStatus.Disconnecting; }
		}

		internal void _UnsynchronizeServerTime()
		{
			_lastMonotonicServerTime = 0;

			_smoothLastUpdateLocalTime = 0;
			_smoothLastUpdateServerTime = 0;
			_smoothLastUpdateServerTimeSkew = 0;
			_smoothAverageServerTimeSkew = 0;

			_lastMonotonicTimestamp = 0;
		}

		internal void _SynchronizeInitialServerTime(NetConnection serverConnection)
		{
			_SynchronizeInitialServerTime(serverConnection.LastRemoteTimeOffset, serverConnection.LastRoundtripTime, serverConnection.AverageRemoteTimeOffset, serverConnection.AverageRoundtripTime);
		}

		internal void _SynchronizeInitialServerTime(double lastServerTimeOffset, double lastServerRTT, double avgServerTimeOffset, double avgServerRTT)
		{
			_rawServerTimeOffset = 0;
			_ResynchronizeServerTime(lastServerTimeOffset, lastServerRTT, avgServerTimeOffset, avgServerRTT);

			double localTime = NetworkTime.localTime;

			_lastMonotonicServerTime = localTime + _rawServerTimeOffset;

			_smoothLastUpdateLocalTime = localTime;
			_smoothLastUpdateServerTime = localTime + _rawServerTimeOffset;
			_smoothLastUpdateServerTimeSkew = 1;
			_smoothAverageServerTimeSkew = 0;

			_lastMonotonicTimestamp = 0;
		}

		internal void _ResynchronizeServerTime(NetConnection serverConnection)
		{
			_ResynchronizeServerTime(serverConnection.LastRemoteTimeOffset, serverConnection.LastRoundtripTime, serverConnection.AverageRemoteTimeOffset, serverConnection.AverageRoundtripTime);
		}

		internal void _ResynchronizeServerTime(double lastServerTimeOffset, double lastServerRTT, double avgServerTimeOffset, double avgServerRTT)
		{
			Log.Debug(NetworkLogFlags.ClockSync, NetworkTime.localTime, ": Latest estimation of server time offset = ", lastServerTimeOffset, "s (latest RTT = ", lastServerRTT, "s), avg time offset = ", avgServerTimeOffset, "s (avg RTT = ", avgServerRTT, "s)");

			if (isServerTimeAutoSynchronized)
			{
				_UpdateServerTimeOffset(avgServerTimeOffset);
			}
		}

		private void _UpdateServerTimeOffset(double serverTimeOffset)
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			if (_rawServerTimeOffset == serverTimeOffset) return; // offset hasn't changed
			// ReSharper restore CompareOfFloatsByEqualityOperator

			Log.Info(NetworkLogFlags.ClockSync, "NetworkTime.rawServerTimeOffset has changed from ", _rawServerTimeOffset, "s to ", serverTimeOffset, "s");

			_rawServerTimeOffset = serverTimeOffset;
		}

		internal void _UpdateSmoothServerTime(double localTime, double deltaTime)
		{
			// TODO: allow the below constant values to be configured.
			// However they must satisfy the following conditions:
			//   1.   0 < p < 2
			//   2.   0 < k1 - k2 < (2*k1)/(3*p)
			//
			// For more details, see paper "Skewless Network Clock Synchronization".

			const double p = 0.99;
			const double k1 = 1.1;
			const double k2 = 1.0;

			_smoothLastUpdateServerTime += _smoothLastUpdateServerTimeSkew * deltaTime;
			double smoothServerTimeOffset = rawServerTime - _smoothLastUpdateServerTime;

			_smoothLastUpdateServerTimeSkew += k1 * smoothServerTimeOffset - k2 * _smoothAverageServerTimeSkew;
			if (_smoothLastUpdateServerTimeSkew < 0) _smoothLastUpdateServerTimeSkew = 0; // never go backwards

			_smoothAverageServerTimeSkew = p * smoothServerTimeOffset + (1 - p) * _smoothAverageServerTimeSkew;

			_smoothLastUpdateLocalTime = localTime;
		}

		internal double _GetMonotonicServerTime(double localTime)
		{
			if (isServerTimeAvailable)
			{
				double monotonicServerTime = localTime + _rawServerTimeOffset;
				if (monotonicServerTime <= _lastMonotonicServerTime) return _lastMonotonicServerTime;

				_lastMonotonicServerTime = monotonicServerTime;
				return monotonicServerTime;
			}

			return _unassignedServerTime;
		}

		internal double _GetSmoothServerTime(double localTime)
		{
			if (isServerTimeAvailable)
			{
				double timeSinceLastUpdate = (localTime - _smoothLastUpdateLocalTime);
				return _smoothLastUpdateServerTime + _smoothLastUpdateServerTimeSkew*timeSinceLastUpdate;
			}

			return _unassignedServerTime;
		}

		internal double _GetRawServerTime(double localTime)
		{
			return isServerTimeAvailable ?
				localTime + _rawServerTimeOffset : _unassignedServerTime;
		}

		// TODO: remove this ugly hack please! It should ideally not be necessary IMHO! /Aidin
		internal double _EnsureAndUpdateMonotonicTimestamp(double timestamp)
		{
			if (timestamp >= _lastMonotonicTimestamp)
			{
				_lastMonotonicTimestamp = timestamp;
				return timestamp;
			}

			return _lastMonotonicTimestamp;
		}
	}
}
