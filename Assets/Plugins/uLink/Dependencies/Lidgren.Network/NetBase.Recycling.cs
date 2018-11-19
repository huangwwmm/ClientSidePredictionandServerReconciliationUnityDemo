#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD || TEST_BUILD || PIKKO_BUILD
#define NO_LIDGREN_THREADS
#endif

using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	internal partial class NetBase
	{
#if !NO_LIDGREN_THREADS
		private const int c_defaultBufferCapacity = 8;

		private const int c_smallBufferSize = 24;
		private const int c_maxSmallItems = 32;
		private const int c_maxLargeItems = 16;

		private Stack<NetBuffer> m_smallBufferPool = new Stack<NetBuffer>(c_maxSmallItems);
		private Stack<NetBuffer> m_largeBufferPool = new Stack<NetBuffer>(c_maxLargeItems);
#endif

		internal void RecycleBuffer(NetBuffer item)
		{
#if !NO_LIDGREN_THREADS
			if (item.Data.Length <= c_smallBufferSize)
			{
				if (m_smallBufferPool.Count >= c_maxSmallItems) return; // drop, we're full
				m_smallBufferPool.Push(item);
				return;
			}

			if (m_largeBufferPool.Count >= c_maxLargeItems) return; // drop, we're full
			m_largeBufferPool.Push(item);
#endif
		}

		public NetBuffer CreateBuffer(int initialCapacity)
		{
#if NO_LIDGREN_THREADS
			return new NetBuffer(initialCapacity);
#else
			NetBuffer retval;
			if (initialCapacity <= c_smallBufferSize)
			{
				if (m_smallBufferPool.Count == 0)
					return new NetBuffer(initialCapacity);
				retval = m_smallBufferPool.Pop();

				retval.Reset();
				return retval;
			}

			if (m_largeBufferPool.Count == 0)
				return new NetBuffer(initialCapacity);
			retval = m_largeBufferPool.Pop();
			retval.Reset();
			return retval;
#endif
		}

		public NetBuffer CreateBuffer(string str)
		{
#if NO_LIDGREN_THREADS
			var retval = new NetBuffer();
#else
			// TODO: optimize
			NetBuffer retval = CreateBuffer(Encoding.UTF8.GetByteCount(str) + 1);
#endif
			retval.Write(str);
			return retval;
		}

		public NetBuffer CreateBuffer()
		{
#if NO_LIDGREN_THREADS
			return new NetBuffer();
#else
			return CreateBuffer(c_defaultBufferCapacity);
#endif
		}

	}
}
