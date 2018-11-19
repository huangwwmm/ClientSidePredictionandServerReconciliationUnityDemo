#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if !NO_POOLING

namespace Lidgren.Network
{
	/// <summary>
	/// The purpose of this class is to cache NetBuffers for all type of incoming uLink packets.
	/// This avoid ugly GC spikes in the Unity profiler for InternalHelper.LateUpdate()
	/// The Bitstreams in the buffer can and should be be reused every frame.
	/// See also: The class BitStreamPool is used for buffering outgoing traffic's binaries.
	/// </summary>
	internal class NetBufferPool
	{
		private const ulong OVERFLOW_LOG_INTERVAL = 5000;

		private ulong nextOverflowLogTime = 0;

		private int nextFree = 0;
		private NetBuffer[] pool;

		//TODO: Make this number configurable for customers
		private const int defaultByteArraySize = 64;

		public NetBufferPool(int capacity)
		{

			pool = new NetBuffer[capacity];
			for (int i = 0; i < capacity; i++)
			{
				pool[i] = new NetBuffer(defaultByteArraySize);
			}
		}

		public NetBuffer GetNext()
		{
			if (nextFree >= pool.Length)
			{	//Pool overflow during one Unity Frame - will not use pool
				nextFree++;
				return new NetBuffer(defaultByteArraySize);
			}
			NetBuffer next = pool[nextFree];
			next.Reset();
			nextFree++;

			return next;
		}

		public void ReportFrameFinished()
		{
			if (nextFree >= pool.Length && nextOverflowLogTime < NetTime.NowInMillis)
			{
#if UNITY_BUILD
				uLink.NetworkLog.Warning(uLink.NetworkLogFlags.Server, "uLink detected risk for major GC spikes. General incoming BufferPool overflow, pool capacity ", pool.Length, ", usage was ", nextFree);
#endif
				nextOverflowLogTime = NetTime.NowInMillis + OVERFLOW_LOG_INTERVAL;
			}

			nextFree = 0;
		}
	}

}

#endif

