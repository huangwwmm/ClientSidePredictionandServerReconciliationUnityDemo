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
	/// Used to filter the list of peers in a list of peers when using discovery functionality of uLink.
	/// </summary>
	public class PeerDataFilter
	{
		/// <summary>
		/// Type of the peer.
		/// Can be anything which is logical for your game. combat server, Shop server or whatever.
		/// </summary>
		public string peerType;
		/// <summary>
		/// Name of the peer
		/// </summary>
		public string peerName;
		/// <summary>
		/// Is the peer password protected.
		/// </summary>
		public bool? passwordProtected;
		/// <summary>
		/// The comment of the peer.
		/// </summary>
		public string comment;
		/// <summary>
		/// The platform that the peer is running on.
		/// </summary>
		public string platform;

		public PeerDataFilter() { }
		public PeerDataFilter(string peerType) { this.peerType = peerType; }

		internal PeerDataFilter(NetBuffer buffer)
		{
			peerType = buffer.ReadString();
			peerName = buffer.ReadString();
			passwordProtected = _ReadNullableBool(buffer);
			comment = buffer.ReadString();
			platform = buffer.ReadString();
		}

		internal void _Write(NetBuffer buffer)
		{
			buffer.Write(peerType);
			buffer.Write(peerName);
			_WriteNullableBool(buffer, passwordProtected);
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

		/// <summary>
		/// Returns if the provided peer matchs this filter or not.
		/// </summary>
		/// <param name="peerData"></param>
		/// <returns></returns>
		public bool Match(LocalPeerData peerData)
		{
			if (!String.IsNullOrEmpty(peerType) && peerType != peerData.peerType) return false;
			if (!String.IsNullOrEmpty(peerName) && peerName != peerData.peerName) return false;
			if (passwordProtected != null && passwordProtected != peerData.passwordProtected) return false;
			if (!String.IsNullOrEmpty(comment) && comment != peerData.comment) return false;
			if (!String.IsNullOrEmpty(platform) && platform != peerData.platform) return false;

			return true;
		}

		/// <summary>
		/// Returns the list of peers from the provided collection, which match this filter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="unfilteredPeers"></param>
		/// <returns></returns>
		public List<T> FilterList<T>(ICollection<T> unfilteredPeers) where T : LocalPeerData
		{
			var filteredHosts = new List<T>(unfilteredPeers.Count);

			foreach (var host in unfilteredPeers)
			{
				if (Match(host)) filteredHosts.Add(host);
			}

			return filteredHosts;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as PeerDataFilter);
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public bool Equals(PeerDataFilter other)
		{
			return other != null
				&& String.IsNullOrEmpty(peerType) ? String.IsNullOrEmpty(other.peerType) : peerType == other.peerType
				&& String.IsNullOrEmpty(peerName) ? String.IsNullOrEmpty(other.peerName) : peerName == other.peerName
				&& passwordProtected == other.passwordProtected
				&& String.IsNullOrEmpty(comment) ? String.IsNullOrEmpty(other.comment) : comment == other.comment
				&& String.IsNullOrEmpty(platform) ? String.IsNullOrEmpty(other.platform) : platform == other.platform;
		}
	}
}
