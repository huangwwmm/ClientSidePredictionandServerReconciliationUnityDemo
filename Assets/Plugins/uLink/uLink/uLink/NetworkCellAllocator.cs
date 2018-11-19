#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion

using System.Collections.Generic;

#if PIKKO_BUILD

namespace uLink
{
	// TODO: make sure the implemented NetworkCellAllocator ctor is used!!

	internal struct NetworkCellAllocator
	{
		private struct Recyclable
		{
			public int id;
			public double timeToRecycle;
		}

		private readonly List<Recyclable> _recyclables;

		private int _nextNewID;

		public NetworkCellAllocator(bool dummy)
		{
			_recyclables = new List<Recyclable>();
			_nextNewID = NetworkPlayer.maxCellServer.id;
		}

		public NetworkPlayer Allocate()
		{
			Log.Debug(NetworkLogFlags.Allocator, "Allocating a new cell PlayerID");

			if (_recyclables.Count != 0)
			{
				var recyclable = _recyclables[0];
				if (recyclable.timeToRecycle <= NetworkTime.localTime)
				{
					_recyclables.RemoveAt(0);

					var player = new NetworkPlayer(recyclable.id);

					Log.Debug(NetworkLogFlags.Allocator, "Allocated ", player, " from recyclable list of cell PlayerIDs");
					return player;
				}
			}

			if (NetworkPlayer.maxCellServer.id >= _nextNewID && _nextNewID >= NetworkPlayer.minCellServer.id) // TODO: fix magic direction
			{
				var player = new NetworkPlayer(_nextNewID);
				_nextNewID--; // TODO: fix magic direction

				Log.Debug(NetworkLogFlags.Allocator, "Allocated ", player, " from remaining unused range of cell PlayerIDs");
				return player;
			}

			Log.Error(NetworkLogFlags.Allocator, "Can't allocate a new cell PlayerID because they entire cell range from ", NetworkPlayer.minCellServer.id, " to ", NetworkPlayer.maxCellServer.id, " is already allocated");
			return NetworkPlayer.unassigned;
		}

		public void Deallocate(NetworkPlayer player, double timeToRecycle)
		{
			Log.Debug(NetworkLogFlags.Allocator, "Deallocating ", player, " back to recyclable list of cell PlayerIDs");

			var recyclable = new Recyclable
			{
				id = player.id,
				timeToRecycle = timeToRecycle,
			};

			_recyclables.Add(recyclable);
		}

		public void Clear()
		{
			_recyclables.Clear();
			_nextNewID = NetworkPlayer.maxCellServer.id;
		}
	}
}

#endif
