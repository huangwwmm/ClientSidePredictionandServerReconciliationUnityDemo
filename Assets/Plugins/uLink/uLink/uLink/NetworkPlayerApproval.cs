#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12060 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-14 21:06:59 +0200 (Mon, 14 May 2012) $
#endregion
using System;
using System.Net;

namespace uLink
{
	/// <summary>
	/// The different states which a player can have regarding being approved or not.
	/// </summary>
	public enum NetworkPlayerApprovalStatus
	{
		/// <summary>
		/// The player is being automatically approved.
		/// </summary>
		AutoApproving,
		/// <summary>
		/// The player is approvved.
		/// </summary>
		Approved,
		/// <summary>
		/// The player is waiting because server is doing a time consuming work to know if the player should
		/// be approved or not. This might be loading stuff from database or getting information from a web service or ...
		/// </summary>
		Waiting,
		/// <summary>
		/// The player is denied.
		/// </summary>
		Denied,

		[Obsolete("NetworkPlayerApprovalStatus.Pending is deprecated, please use NetworkPlayerApprovalStatus.Waiting instead")]
		Pending = Waiting,
	}

	/// <summary>
	/// The request data sent from a client wanting to connect to a server.
	/// </summary>
	/// <remarks>
	/// This is used in <see cref="uLink.Network.uLink_OnPlayerApproval"/> for approving or denying a player.
	/// </remarks>
	public class NetworkPlayerApproval
	{
		internal NetworkPlayerApprovalStatus _status = NetworkPlayerApprovalStatus.AutoApproving;

		internal NetworkPlayer _manualPlayerID = NetworkPlayer.unassigned;

		private NetworkBaseServer _network;
		private NetworkMessage _msg;

		/// <summary>
		/// The Bitstream containing the loginData sent from the client.
		/// Usually you use this to see if the client should be approved or not.
		/// </summary>
		public readonly BitStream loginData;

		/// <summary>
		/// You can see the time stamp of the sent message and see in the flags, wether the message is encrypted or not.
		/// </summary>
		public readonly NetworkMessageInfo info;

		/// <summary>
		/// Set value for <see cref="uLink.NetworkPlayer.localData"/>.
		/// </summary>
		public object localData;

		/// <summary>
		/// If the client has been handed over from a another server, then instances are the player's handover prefabs set by that server, otherwise empty.
		/// </summary>
		public NetworkP2PHandoverInstance[] handoverInstances;

		/// <summary>
		/// If the client has been handed over from a another server, then handoverData can be set by that server, otherwise null.
		/// </summary>
		public readonly BitStream handoverData;

		/// <summary>
		/// The IP address of the client.
		/// </summary>
		public NetworkEndPoint endpoint { get { return (_msg.connection != null) ? _msg.connection.RemoteEndpoint : NetworkEndPoint.unassigned; } }
		/// <summary>
		/// A string representation of the IP address of the client.
		/// </summary>
		public string ipAddress { get { return endpoint.ipAddress.ToString(); } }
		/// <summary>
		/// The port number of the client.
		/// </summary>
		public int port { get { return endpoint.port; } }

		/// <summary>
		/// Gets or sets the status of the approval process for the client.
		/// Setting this is like calling the relevant methods.
		/// </summary>
		public NetworkPlayerApprovalStatus status
		{
			get
			{
				return _status;
			}
			set
			{
				switch (value)
				{
					case NetworkPlayerApprovalStatus.Approved: Approve(); break;
					case NetworkPlayerApprovalStatus.Waiting: Wait(); break;
					case NetworkPlayerApprovalStatus.Denied: Deny(); break;
					default: throw new ArgumentException("Can't change status to " + value);
				}
			}
		}

		/// <summary>
		/// Is the player being automatically approved?
		/// </summary>
		public bool isAutoApproving { get { return _status == NetworkPlayerApprovalStatus.AutoApproving; } }
		/// <summary>
		/// Is the player approved?
		/// </summary>
		public bool isApproved { get { return _status == NetworkPlayerApprovalStatus.Approved; } }
		/// <summary>
		/// Is the player waiting?
		/// </summary>
		public bool isWaiting { get { return _status == NetworkPlayerApprovalStatus.Waiting; } }
		/// <summary>
		/// Is the player denied?
		/// </summary>
		public bool isDenied { get { return _status == NetworkPlayerApprovalStatus.Denied; } }

		[Obsolete("NetworkPlayerApproval.isPending is deprecated, please use NetworkPlayerApproval.isWaiting instead")]
		public bool isPending { get { return isWaiting; } }

		internal NetworkPlayerApproval(NetworkBaseServer network, NetworkMessage msg, NetworkP2PHandoverInstance[] handoverInstances, BitStream handoverData)
		{
			_network = network;
			_msg = msg;

			loginData = msg.stream;
			info = new NetworkMessageInfo(msg, null);

			this.handoverInstances = handoverInstances;
			this.handoverData = handoverData;
		}

		/// <summary>
		/// Approves the client. A connection response will be sent internally by uLink to the client.
		/// </summary>
		/// <param name="approvalData">The approval data that will be delivered to the client.</param>
		/// <exception cref="ArgumentNullException">when approvalData is null or one of the arguments is null.</exception>
		/// <remarks>You can use approval data to send level number, team info or any other information to the client.</remarks>
		public void Approve(params object[] approvalData)
		{
			if (_status == NetworkPlayerApprovalStatus.Approved || _status == NetworkPlayerApprovalStatus.Denied)
			{
				throw new ArgumentNullException("Player has already been " + _status);
			}

			if (approvalData == null) throw new ArgumentNullException("approvalData");

			for (int i = 0; i < approvalData.Length; i++)
			{
				if (approvalData[i] == null) throw new ArgumentNullException("approvalData[" + i + "]");
			}

			_status = NetworkPlayerApprovalStatus.Approved;

			_network._ApproveClient(this, _msg, approvalData);
		}

		/// <summary>
		/// Assigns a custom unique player ID to this player instead of using the
		/// unique player ID that uLink would have provided automatically.
		/// </summary>
		/// <remarks>The main purpose of using this function would be to set the
		/// player ID to the same player ID you have in a persistent storage
		/// like a database. If your player IDs are integers and they are
		/// limited to the same range  as the uLink internal protocol (which is
		/// 1 to <see cref="System.UInt16.MaxValue"/> - 1) it is very convenient to use
		/// these IDs from the database directly as playerIDs in uLink. 0 is
		/// reserved for unassigned clients and <see cref="System.UInt16.MaxValue"/> is
		/// reserved for representing the server. If you have another data type
		/// or a bigger integer range for unique player IDs in your database 
		/// this feature can not be used. Instead you have to store the database
		/// userID in one of the player's game objects on the server.
		/// </remarks>
		/// <exception cref="uLink.NetworkException">If trying to assign a manual playerID during a P2P handover.</exception>
		public void AssignManualPlayerID(int manuallyAssignedID)
		{
			Utility.Assert(NetworkPlayer.minClient.id <= manuallyAssignedID & manuallyAssignedID <= NetworkPlayer.maxClient.id,
				"Manual PlayerID ", manuallyAssignedID, " is out of range. PlayerID must be between ", NetworkPlayer.minClient.id, " and ", NetworkPlayer.maxClient.id);

			_manualPlayerID = new NetworkPlayer(manuallyAssignedID);
		}

		/// <summary>
		/// Change approval status of the player to waiting.
		/// This should be called if you want to do a time consuming operation like connecting to web services or loading
		/// data from data bases to decide on the player's approval.
		/// </summary>
		public void Wait()
		{
			_status = NetworkPlayerApprovalStatus.Waiting;
		}

		[Obsolete("NetworkPlayerApproval.Pending is deprecated, please use NetworkPlayerApproval.Wait instead")]
		public void Pending()
		{
			Wait();
		}

		/// <summary>
		/// Denies the player to connect and sends a reason code to the client.
		/// The reason sent will be <see cref="NetworkConnectionError.ApprovalDenied"/>
		/// </summary>
		/// <remarks>The player will get the reason in callback <see cref="uLink.Network.uLink_OnFailedToConnect"/></remarks>
		public void Deny()
		{
			Deny(NetworkConnectionError.ApprovalDenied);
		}

		/// <summary>
		/// Denies the player to connect and sends a reason code to the client.
		/// </summary>
		/// <param name="reason">The reason for not being granted a connection the player will get as a parameter in the 
		/// callback <see cref="uLink.Network.uLink_OnFailedToConnect"/></param>
		public void Deny(NetworkConnectionError reason)
		{
			if (_status == NetworkPlayerApprovalStatus.Approved || _status == NetworkPlayerApprovalStatus.Denied)
			{
				throw new ArgumentNullException("Player has already been " + _status);
			}

			_status = NetworkPlayerApprovalStatus.Denied;

			_network._DenyClient(this, _msg, reason);
		}
	}
}
