/*
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

internal class SPSCBlockWaitQueue<T>
	: IDisposable
{
	private readonly T[] _buffer;
	private readonly Semaphore _semaphore;
	private readonly int _mask;

	private SPSCQueueUtility.PaddedInteger _producerIndex;
	private SPSCQueueUtility.PaddedInteger _consumerIndex;

#if DEBUG
	private SPSCQueueUtility.DebugThreadSafety _debug;
#endif

	public WaitHandle waitHandle
	{
		get { return _semaphore; }
	}

	public SPSCBlockWaitQueue(int capacity)
	{
		capacity = SPSCQueueUtility.RoundUpToPowerOfTwo(capacity);

		_semaphore = new Semaphore(0, capacity - 1);
		_mask = capacity - 1;
		_buffer = new T[capacity];
	}

	public bool TryEnqueue(T value)
	{
#if DEBUG
		_debug.AssertProducerThread();
#endif

		_buffer[_producerIndex.integer] = value;
		Thread.MemoryBarrier();

		try
		{
			_semaphore.Release();
		}
		catch (SemaphoreFullException)
		{
			return false;
		}

		_producerIndex.integer = (_producerIndex.integer + 1) & _mask;

		return true;
	}

	public void Enqueue(T value)
	{
		while (!TryEnqueue(value))
		{
			Thread.Sleep(0); // TODO: this isn't exactly blocking 
		}
	}

	public bool TryDequeue(out T value)
	{
		return TryDequeue(out value, 0);
	}

	public bool TryDequeue(out T value, int millisecondsTimeout)
	{
#if DEBUG
		_debug.AssertConsumerThread();
#endif

		if (_semaphore.WaitOne(millisecondsTimeout))
		{
			Thread.MemoryBarrier();
			value = _buffer[_consumerIndex.integer];

			_consumerIndex.integer = (_consumerIndex.integer + 1) & _mask;

			return true;
		}

		value = default(T);
		return false;
	}

	public T Dequeue()
	{
#if DEBUG
		_debug.AssertConsumerThread();
#endif

		_semaphore.WaitOne();

		Thread.MemoryBarrier();
		var value = _buffer[_consumerIndex.integer];

		_consumerIndex.integer = (_consumerIndex.integer + 1) & _mask;

		return value;
	}

	public void Dispose()
	{
		_semaphore.Close();
	}
}

internal class SPSCSpinWaitQueue<T>
{
	private struct VolatileItem
	{
		private bool _hasValue;
		private T _value;

		public bool hasValue
		{
			get
			{
				Thread.MemoryBarrier();
				return _hasValue;
			}
		}

		public T value
		{
			get
			{
				Thread.MemoryBarrier();
				return _value;
			}
			set
			{
				_value = value;
				Thread.MemoryBarrier();
				_hasValue = true;
			}
		}

		public void Clear()
		{
			_hasValue = false;
		}
	}

	private readonly VolatileItem[] _buffer;
	private readonly int _mask;

	private SPSCQueueUtility.PaddedInteger _consumerIndex;
	private SPSCQueueUtility.PaddedInteger _producerIndex;

#if DEBUG
	private SPSCQueueUtility.DebugThreadSafety _debug;
#endif

	public SPSCSpinWaitQueue(int capacity)
	{
		capacity = SPSCQueueUtility.RoundUpToPowerOfTwo(capacity);

		_mask = capacity - 1;
		_buffer = new VolatileItem[capacity];
	}

	public void Enqueue(T value)
	{
#if DEBUG
		_debug.AssertProducerThread();
#endif

		if (!_buffer[_producerIndex.integer].hasValue)
		{
			_WaitWhileFull();
		}

		_buffer[_producerIndex.integer].value = value;
		_producerIndex.integer = (_producerIndex.integer + 1) & _mask;
	}

	public bool TryEnqueue(T value)
	{
#if DEBUG
		_debug.AssertProducerThread();
#endif

		if (!_buffer[_producerIndex.integer].hasValue)
		{
			return false;
		}

		_buffer[_producerIndex.integer].value = value;
		_producerIndex.integer = (_producerIndex.integer + 1) & _mask;

		return true;
	}

	public T Dequeue()
	{
#if DEBUG
		_debug.AssertConsumerThread();
#endif

		var item = _buffer[_consumerIndex.integer];
		var value = item.hasValue ? item.value : _WaitWhileEmpty();

		_buffer[_consumerIndex.integer].Clear();
		_consumerIndex.integer = (_consumerIndex.integer + 1) & _mask;

		return value;
	}

	public bool TryDequeue(out T value)
	{
#if DEBUG
		_debug.AssertConsumerThread();
#endif

		var item = _buffer[_consumerIndex.integer];
		if (item.hasValue)
		{
			_buffer[_consumerIndex.integer].Clear();
			_consumerIndex.integer = (_consumerIndex.integer + 1) & _mask;

			value = item.value;
			return true;
		}

		value = default(T);
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void _WaitWhileFull()
	{
		int count = 0;

		while (_buffer[_producerIndex.integer].hasValue)
		{
			count = SPSCQueueUtility.SpinOnce(count);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private T _WaitWhileEmpty()
	{
		int count = 0;

		while (!_buffer[_consumerIndex.integer].hasValue)
		{
			count = SPSCQueueUtility.SpinOnce(count);
		}

		return _buffer[_consumerIndex.integer].value;
	}
}

internal static class SPSCQueueUtility
{
	internal static int RoundUpToPowerOfTwo(int value)
	{
		if ((value & (value - 1)) == 0)
		{
			return value;
		}

		value |= (value >> 1);
		value |= (value >> 2);
		value |= (value >> 4);
		value |= (value >> 8);
		value |= (value >> 16);
		return (value + 1);
	}

	internal static int SpinOnce(int count)
	{
		if (count > 10 || Environment.ProcessorCount == 1)
		{
			Thread.Sleep(0);
		}
		else
		{
			Thread.SpinWait(4 << count);
		}

		return (count == Int32.MaxValue) ? 10 : (count + 1);
	}

	///<summary>
	/// Size of a cache line in bytes
	///</summary>
	private const int CACHE_LINE_SIZE = 64;

	/// <summary>
	/// An integer value that may be updated atomically and is guaranteed to live on its own cache line (to prevent false sharing)
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = CACHE_LINE_SIZE * 2)]
	internal struct PaddedInteger
	{
		[FieldOffset(CACHE_LINE_SIZE)]
		public int integer;
	}

#if DEBUG
	internal struct DebugThreadSafety
	{
		private PaddedInteger _producerThreadId;
		private PaddedInteger _consumerThreadId;

		public void AssertProducerThread()
		{
			var currentId = Thread.CurrentThread.ManagedThreadId;

			if (_producerThreadId.integer != 0)
			{
				Debug.Assert(_producerThreadId.integer == currentId, "Must be producer thread");
			}
			else
			{
				Thread.MemoryBarrier();
				Debug.Assert(_consumerThreadId.integer != currentId, "Producer thread must be different from consumer thread");
				_producerThreadId.integer = currentId;
			}
		}

		public void AssertConsumerThread()
		{
			var currentId = Thread.CurrentThread.ManagedThreadId;

			if (_consumerThreadId.integer != 0)
			{
				Debug.Assert(_consumerThreadId.integer == currentId, "Must be consumer thread");
			}
			else
			{
				_consumerThreadId.integer = currentId;
			}
		}
	}
#endif
}
*/
