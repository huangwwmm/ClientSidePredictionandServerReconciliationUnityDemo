#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// Read live network statistics for one <see cref="uLink.NetworkPlayer"/>. Available via <see cref="uLink.NetworkPlayer.statistics"/>.
	/// </summary>
	public class NetworkStatistics
	{
		private readonly NetConnectionStatistics _statistics;

		internal NetworkStatistics(NetConnection connection)
		{
			_statistics = connection.Statistics;
		}

		private double bytesSentLast;
		private double bytesReceivedLast;
		private double userBytesSentLast;
		private double userBytesReceivedLast;

		/// <summary>
		/// Gets bytesSentPerSecond.
		/// </summary>
		public double bytesSentPerSecond { get; private set; }

		/// <summary>
		/// Gets bytesReceivedPerSecond
		/// </summary>
		public double bytesReceivedPerSecond { get; private set; }

		/// <summary>
		/// Gets userBytesSentPerSecond
		/// </summary>
		public double userBytesSentPerSecond { get; private set; }

		/// <summary>
		/// Gets userBytesReceivedPerSecond
		/// </summary>
		public double userBytesReceivedPerSecond { get; private set; }

		/// <summary>
		/// Gets bytesSent
		/// </summary>
		public double bytesSent { get { return _statistics.GetBytesSent(true); } }
		/// <summary>
		/// Gets bytesReceived
		/// </summary>
		public double bytesReceived { get { return _statistics.GetBytesReceived(true); } }

		/// <summary>
		/// Gets userBytesSent
		/// </summary>
		public double userBytesSent { get { return _statistics.GetBytesSent(false); } }
		/// <summary>
		/// Gets userBytesReceived 
		/// </summary>
		public double userBytesReceived { get { return _statistics.GetBytesReceived(false); } }

		/// <summary>
		/// Gets packetsSent 
		/// </summary>
		public long packetsSent { get { return _statistics.PacketsSent; } }
		/// <summary>
		/// Gets packetsReceived 
		/// </summary>
		public long packetsReceived { get { return _statistics.PacketsReceived; } }
		/// <summary>
		/// Gets messagesSent
		/// </summary>
		public long messagesSent { get { return _statistics.GetMessagesSent(true); } }
		/// <summary>
		/// Gets messagesReceived
		/// </summary>
		public long messagesReceived { get { return _statistics.GetMessagesReceived(true); } }
		/// <summary>
		/// Gets messagesResent
		/// </summary>
		public long messagesResent { get { return _statistics.MessagesResent; } }
		/// <summary>
		/// Gets messagesStored
		/// </summary>
		public long messagesStored { get { return _statistics.CurrentlyStoredMessagesCount; } }
		/// <summary>
		/// Gets messagesUnsent
		/// </summary>
		public long messagesUnsent { get { return _statistics.CurrentlyUnsentMessagesCount; } }
		/// <summary>
		/// Gets messagesWithheld
		/// </summary>
		public long messagesWithheld { get { return _statistics.CurrentlyWithheldMessagesCount; } }
		/// <summary>
		/// Gets messageDuplicatesRejected
		/// </summary>
		public long messageDuplicatesRejected { get { return _statistics.DuplicateMessagesRejected; } }
		/// <summary>
		/// Gets messageSequencesRejected
		/// </summary>
		public long messageSequencesRejected { get { return _statistics.SequencedMessagesRejected; } }

		internal void _Update(double timeSinceLastCalc)
		{
			// Compute new values.
			var invInterval = 1.0 / timeSinceLastCalc;
			bytesSentPerSecond = (bytesSent - bytesSentLast) * invInterval;
			bytesReceivedPerSecond = (bytesReceived - bytesReceivedLast) * invInterval;
			userBytesSentPerSecond = (userBytesSent - userBytesSentLast) * invInterval;
			userBytesReceivedPerSecond = (userBytesReceived - userBytesReceivedLast) * invInterval;

			bytesSentLast = bytesSent;
			bytesReceivedLast = bytesReceived;
			userBytesSentLast = userBytesSent;
			userBytesReceivedLast = userBytesReceived;
		}

	}
}
