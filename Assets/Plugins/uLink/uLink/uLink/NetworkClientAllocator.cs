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
	internal struct NetworkClientAllocator
	{
		private struct Recyclable
		{
			public int id;
			public double timeToRecycle;
		}

		private readonly List<Recyclable> _recyclables;

		private int _nextNewID;

		public NetworkClientAllocator(bool dummy)
		{
			_recyclables = new List<Recyclable>();
			_nextNewID = NetworkPlayer.minClient.id;
		}

		public NetworkPlayer Allocate()
		{
			Log.Debug(NetworkLogFlags.Allocator, "Allocating a new client PlayerID");

			if (_recyclables.Count != 0)
			{
				var recyclable = _recyclables[0];
				if (recyclable.timeToRecycle <= NetworkTime.localTime)
				{
					_recyclables.RemoveAt(0);

					var player = new NetworkPlayer(recyclable.id);

					Log.Debug(NetworkLogFlags.Allocator, "Allocated ", player, " from recyclable list of client PlayerIDs");
					return player;
				}
			}

			if (NetworkPlayer.minClient.id <= _nextNewID && _nextNewID <= NetworkPlayer.maxClient.id) // TODO: fix magic direction
			{
				var player = new NetworkPlayer(_nextNewID);
				_nextNewID++; // TODO: fix magic direction

				Log.Debug(NetworkLogFlags.Allocator, "Allocated ", player, " from remaining unused range of client PlayerIDs");
				return player;
			}

			Log.Error(NetworkLogFlags.Allocator, "Can't allocate a new client PlayerID because they entire client range from ", NetworkPlayer.minClient.id, " to ", NetworkPlayer.maxClient.id, " is already allocated");
			return NetworkPlayer.unassigned;
		}

		public bool TryAllocate(NetworkPlayer player)
		{
			Log.Debug(NetworkLogFlags.Allocator, "Try allocating a manually assigned client PlayerID: ", player);

			if (_nextNewID == player.id)
			{
				_nextNewID++;
				return true;
			}
			
			if (_nextNewID > player.id)
			{
				for (int i = 0; i < _recyclables.Count; i++)
				{
					var recyclable = _recyclables[i];

					if (recyclable.id == player.id)
					{
						if (recyclable.timeToRecycle <= NetworkTime.localTime)
						{
							_recyclables.RemoveAt(i);
							return true;
						}

						return false;
					}
				}

				return false;
			}

			// TODO: figure out a way to avoid filling the _recyclables pool with lots of unused IDs.

			for (int id = _nextNewID; id < player.id; id++)
			{
				_recyclables.Add(new Recyclable { id = id, timeToRecycle = 0 });
			}

			_nextNewID = player.id + 1; 
			return true;
		}

		public void Deallocate(NetworkPlayer player, double timeToRecycle)
		{
			Log.Debug(NetworkLogFlags.Allocator, "Deallocating ", player, " back to recyclable list of client PlayerIDs");

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
			_nextNewID = NetworkPlayer.minClient.id;
		}
	}
}
