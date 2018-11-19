#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// Used to filter the list of servers in a MasterServer.
	/// </summary>
	public class HostDataFilter
	{
		/// <summary>
		/// To set conditions like "connected players must be more than 0 players" or "player limit less or equal to 10". 
		/// </summary>
		[Flags]
		public enum RelationFlags : byte
		{
			Any = 0,
			Equal = 1,
			Greater = 2,
			GreaterOrEqual = Greater | Equal,
			Less = 4,
			LessOrEqual = Less | Equal,
			NotEqual = Less | Greater,
		}

		/// <summary>
		/// Defines relation conditions.
		/// </summary>
		public struct Relation
		{
			public RelationFlags flags;
			public int value;

			public Relation(RelationFlags flags, int value)
			{
				this.flags = flags;
				this.value = value;
			}

			public bool Match(int other)
			{
				switch (flags)
				{
					case RelationFlags.Any: return true;
					case RelationFlags.Equal: return (value == other);
					case RelationFlags.NotEqual: return (value != other);
					case RelationFlags.Greater: return (value < other);
					case RelationFlags.GreaterOrEqual: return (value <= other);
					case RelationFlags.Less: return (value > other);
					case RelationFlags.LessOrEqual: return (value >= other);
					default: return false;
				}
			}
		}

		public string gameType;
		public string gameName;
		public string gameMode;
		public string gameLevel;
		public Relation connectedPlayers = new Relation(RelationFlags.Any, 0);
		public Relation playerLimit = new Relation(RelationFlags.Any, 0);
		public bool? passwordProtected;
		public bool? dedicatedServer;
		public bool? useNat;
		public bool? useProxy;
		public string comment;
		public string platform;

		public HostDataFilter() { }
		public HostDataFilter(string gameType) { this.gameType = gameType; }

		internal HostDataFilter(NetBuffer buffer)
		{
			gameType = buffer.ReadString();
			gameName = buffer.ReadString();
			gameMode = buffer.ReadString();
			gameLevel = buffer.ReadString();
			connectedPlayers = _ReadRelation(buffer);
			playerLimit = _ReadRelation(buffer);
			passwordProtected = _ReadNullableBool(buffer);
			dedicatedServer = _ReadNullableBool(buffer);
			useNat = _ReadNullableBool(buffer);
			useProxy = _ReadNullableBool(buffer);
			comment = buffer.ReadString();
			platform = buffer.ReadString();
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.Write(gameType);
			buffer.Write(gameName);
			buffer.Write(gameMode);
			buffer.Write(gameLevel);
			_WriteRelation(buffer, connectedPlayers);
			_WriteRelation(buffer, playerLimit);
			_WriteNullableBool(buffer, passwordProtected);
			_WriteNullableBool(buffer, dedicatedServer);
			_WriteNullableBool(buffer, useNat);
			_WriteNullableBool(buffer, useProxy);
			buffer.Write(comment);
			buffer.Write(platform);
		}

		private static bool? _ReadNullableBool(NetBuffer buffer)
		{
			byte value = buffer.ReadByte();
			if (value != 2) return (value == 1);
			return null;
		}

		private static void _WriteNullableBool(NetBuffer buffer, bool? value)
		{
			if (value != null)
				buffer.Write((byte)(value.Value ? 1 : 0));
			else
				buffer.Write((byte)2);
		}


		private static Relation _ReadRelation(NetBuffer buffer)
		{
			RelationFlags flags = (RelationFlags)buffer.ReadByte();
			int value = (flags != RelationFlags.Any) ? buffer.ReadInt32() : 0;

			return new Relation(flags, value);
		}

		private static void _WriteRelation(NetBuffer buffer, Relation relation)
		{
			buffer.Write((byte)relation.flags);
			if (relation.flags != RelationFlags.Any) buffer.Write(relation.value);
		}

		public bool Match(LocalHostData host)
		{
			if (!String.IsNullOrEmpty(gameType) && gameType != host.gameType) return false;
			if (!String.IsNullOrEmpty(gameName) && gameName != host.gameName) return false;
			if (!String.IsNullOrEmpty(gameMode) && gameMode != host.gameMode) return false;
			if (!String.IsNullOrEmpty(gameLevel) && gameLevel != host.gameLevel) return false;
			if (!connectedPlayers.Match(host.connectedPlayers)) return false;
			if (!playerLimit.Match(host.playerLimit)) return false;
			if (passwordProtected != null && passwordProtected != host.passwordProtected) return false;
			if (dedicatedServer != null && dedicatedServer != host.dedicatedServer) return false;
			if (useNat != null && useNat != host.useNat) return false;
			if (useProxy != null && useProxy != host.useProxy) return false;
			if (!String.IsNullOrEmpty(comment) && comment != host.comment) return false;
			if (!String.IsNullOrEmpty(platform) && platform != host.platform) return false;

			return true;
		}

		public List<T> FilterList<T>(ICollection<T> unfilteredHosts) where T : LocalHostData
		{
			var filteredHosts = new List<T>(unfilteredHosts.Count);

			foreach (var host in unfilteredHosts)
			{
				if (Match(host)) filteredHosts.Add(host);
			}

			return filteredHosts;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as HostDataFilter);
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public bool Equals(HostDataFilter other)
		{
			return other != null
				&& String.IsNullOrEmpty(gameType) ? String.IsNullOrEmpty(other.gameType) : gameType == other.gameType
				&& String.IsNullOrEmpty(gameName) ? String.IsNullOrEmpty(other.gameName) : gameName == other.gameName
				&& String.IsNullOrEmpty(gameMode) ? String.IsNullOrEmpty(other.gameMode) : gameMode == other.gameMode
				&& String.IsNullOrEmpty(gameLevel) ? String.IsNullOrEmpty(other.gameLevel) : gameLevel == other.gameLevel
				&& connectedPlayers.Match(other.connectedPlayers.value)
				&& playerLimit.Match(other.playerLimit.value)
				&& passwordProtected == other.passwordProtected
				&& dedicatedServer == other.dedicatedServer
				&& useNat == other.useNat
				&& useProxy == other.useProxy
				&& String.IsNullOrEmpty(comment) ? String.IsNullOrEmpty(other.comment) : comment == other.comment
				&& String.IsNullOrEmpty(platform) ? String.IsNullOrEmpty(other.platform) : platform == other.platform;
		}
	}
}
