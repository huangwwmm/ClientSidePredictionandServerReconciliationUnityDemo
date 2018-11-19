namespace Lidgren.Network
{
	//TODO: move this class to uLink tools, putting it in Lidgren to avoid Lidgren depending on uLink

	/// <summary>
	/// Provides a way of muting too frequent events depending on previous frequency and timing. Useful against log spamming...
	/// </summary>
	sealed class FrequencyMuter
	{
		private bool _muted;
		private int _numEvents;
		private double _timeOfLastEvent;
		private double _timeMuted;

		public bool isMuted
		{
			get { return _muted; }
			set
			{
				_numEvents = 0;
				if (!_muted && value) _timeMuted = NetTime.Now;
				_muted = value;
			}
		}
		public double timeBeforeResettingCount { get; set; }
		public int numEventsBeforeMuting { get; set; }
		public double minTimeBeforeUnmuting { get; set; }

		public FrequencyMuter(double minTimeBeforeUnmuting, int numEventsBeforeMuting,
			double timeBeforeResettingCount = 0)
		{
			this.timeBeforeResettingCount = timeBeforeResettingCount;
			this.minTimeBeforeUnmuting = minTimeBeforeUnmuting;
			this.numEventsBeforeMuting = numEventsBeforeMuting;
		}

		public FrequencyMuter()
		{
			numEventsBeforeMuting = int.MaxValue;
			minTimeBeforeUnmuting = double.MaxValue;
		}

		/// <summary>
		/// Queries whether the next event should be muted (and updates internal state - thus each call to this method
		/// represents precisely one event).
		/// </summary>
		/// <returns>Returns true if the event should NOT be muted.</returns>
		public bool NextEvent()
		{
			if (timeBeforeResettingCount > 0)
			{
				double time = NetTime.Now;

				if (time - _timeOfLastEvent < timeBeforeResettingCount)
					_numEvents = 0;

				_timeOfLastEvent = time;
			}

			bool wasMuted = _muted;
			if (wasMuted)
			{
				if (NetTime.Now - _timeMuted > minTimeBeforeUnmuting)
					isMuted = false;
			}
			else
			{
				if (++_numEvents >= numEventsBeforeMuting)
					isMuted = true;
			}

			return !wasMuted;
		}
	}
}
