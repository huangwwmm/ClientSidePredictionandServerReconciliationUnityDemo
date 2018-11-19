#if !NO_POOLING

namespace uLink
{
	/// <summary>
	/// The purpose of this class is to cache BitStreams for unreliable outgoing traffic.
	/// This avoid ugly GC spikes in the Unity profiler for InternalHelper.LateUpdate()
	/// Unreliable traffic is very short lived, it only lives during one Unite frame because there is no 
	/// resend and no RPC buffering going on.
	/// The BitStreams in the buffer can and should be be reused every frame.
	/// See also: The class NetBufferPool is used for buffering incoming traffic's binaries.
	/// </summary>
	internal class BitStreamPool
	{
		private const float OVERFLOW_LOG_INTERVAL = 5;

		private double nextOverflowLogTime = 0;

		private int nextFree = 0;
		private readonly bool isTypesafe;
		private readonly BitStream[] pool;

		//TODO: Make this number configurable for customers
		private const int defaultByteArraySize = 64;

		public BitStreamPool(int capacity, bool isTypesafe)
		{
			this.isTypesafe = isTypesafe;
			pool = new BitStream[capacity];
			for (int i = 0; i < capacity; i++)
			{
				pool[i] = new BitStream(defaultByteArraySize, isTypesafe);
			}
		}

		public BitStream GetNext()
		{
			if (nextFree >= pool.Length)
			{
				//Pool overflow during one Unity Frame - will not use pool
				nextFree++;
				return new BitStream(defaultByteArraySize, isTypesafe);
			}

			BitStream next = pool[nextFree];
			next._buffer.Reset();
			next._isWriting = true; // we must make sure the BitStream is in write-mode, because it can be changed by uLink i certain cases.
			nextFree++;

			return next;
		}

		public void ReportFrameFinished()
		{
			if (nextFree >= pool.Length && nextOverflowLogTime < NetworkTime.localTime)
			{
				NetworkLog.Warning(NetworkLogFlags.Server, "uLink detected risk for major GC spikes. Unreliable outgoing BitStreamPool (isTypesafe: ", isTypesafe, ") overflow, pool capacity ", pool.Length, ", usage was ", nextFree);
				nextOverflowLogTime = NetworkTime.localTime + OVERFLOW_LOG_INTERVAL;
			}

			nextFree = 0;
		}
	}

}
#endif

