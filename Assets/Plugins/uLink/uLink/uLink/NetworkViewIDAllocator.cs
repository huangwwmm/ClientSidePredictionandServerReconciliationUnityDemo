#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion

using System.Collections.Generic;

namespace uLink
{
	internal struct NetworkViewIDAllocator
	{
		private struct Recyclable
		{
			public int subID;
			public double timeToRecycle;
		}

		private readonly List<Recyclable> _recyclables;

		private int _nextNewSubID;

		public NetworkViewIDAllocator(bool dummy)
		{
			_recyclables = new List<Recyclable>();
			_nextNewSubID = NetworkViewID.minManual.subID;
		}

		public NetworkViewID Allocate(NetworkPlayer allocator)
		{
			Log.Debug(NetworkLogFlags.Allocator, "Allocating a new viewID for ", allocator);

			if (_recyclables.Count != 0)
			{
				// TODO: find the lowest id that can be recycled in the list.

				var recyclable = _recyclables[0];

				if (recyclable.timeToRecycle <= NetworkTime.localTime)
				{
					_recyclables.RemoveAt(0);

					var viewID = new NetworkViewID(recyclable.subID, allocator);

					Log.Debug(NetworkLogFlags.Allocator, "Allocated ", viewID, " from recyclable list of viewIDs");
					return viewID;
				}
			}

			if (_nextNewSubID <= NetworkViewID.maxManual.subID)
			{
				var viewID = new NetworkViewID(_nextNewSubID, allocator);
				_nextNewSubID++; // TODO: fix magic direction

				Log.Debug(NetworkLogFlags.Allocator, "Allocated ", viewID, " from remaining unused range of viewIDs");
				return viewID;
			}

			Log.Error(NetworkLogFlags.Allocator, "Can't allocate viewID for ", allocator, " because they are all already allocated");
			return NetworkViewID.unassigned;
		}

		public NetworkViewID[] Allocate(NetworkPlayer allocator, int count)
		{
			Log.Debug(NetworkLogFlags.Allocator, "Allocating ", count, " new viewID(s) for ", allocator);

			var viewIDs = new NetworkViewID[count];
			int recycledCount = 0;

			for (; recycledCount < _recyclables.Count; recycledCount++)
			{
				var recyclable = _recyclables[recycledCount];

				if (recyclable.timeToRecycle > NetworkTime.localTime)
				{
					break;
				}

				viewIDs[recycledCount] = new NetworkViewID(recyclable.subID, allocator);
			}

			if (count - recycledCount <= (NetworkViewID.maxManual.subID - NetworkViewID.minManual.subID) - _nextNewSubID - 1) // TODO: fix magic number
			{
				_recyclables.RemoveRange(0, recycledCount);

				for (int i = recycledCount; i < count; i++)
				{
					viewIDs[i] = new NetworkViewID(_nextNewSubID, allocator);
					_nextNewSubID++; // TODO: fix magic direction
				}

				return viewIDs;
			}

			Log.Error(NetworkLogFlags.Allocator, "Can't allocate ", count, " viewID(s) for ", allocator, " because they are all already allocated");
			return new NetworkViewID[0];
		}

		public void Deallocate(NetworkViewID viewID, double timeToRecycle)
		{
			Log.Debug(NetworkLogFlags.Allocator, "Deallocating ", viewID, " back to recyclable list of viewIDs");

			var recyclable = new Recyclable
			{
				subID = viewID.subID,
				timeToRecycle = timeToRecycle,
			};

			_recyclables.Add(recyclable);
		}

		public void Deallocate(NetworkViewID[] viewIDs, double timeToRecycle)
		{
			foreach (var viewID in viewIDs)
			{
				Deallocate(viewID, timeToRecycle);
			}
		}

		public void Clear()
		{
			_recyclables.Clear();
			_nextNewSubID = NetworkViewID.minManual.subID;
		}
	}
}
