#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8678 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-22 22:31:15 +0200 (Mon, 22 Aug 2011) $
#endregion

using System;
using System.Net.Sockets;
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// Enables configuration of low-level network parameters, like connection timeouts, inactivity timeouts, ping frequency
	/// and MTU (Maximum Transmission Unit). See <see cref="uLink.Network.config"/>. 
	/// </summary>
	public class NetworkP2PConfig
	{
		public delegate double ResendDelayFunction(int numSent, double avgRTT);

		private ResendDelayFunction _resendFunction = NetConnection.DefaultResendFunction;

		private int _receiveBufferSize = Constants.DEFAULT_SEND_BUFFER_SIZE;
		private int _sendBufferSize = Constants.DEFAULT_RECV_BUFFER_SIZE;

		private int _maximumTransmissionUnit = 1400;
		private float _timeBetweenPings = 3.0f;
		private float _timeoutDelay = 30.0f;
		private int _handshakeRetriesMaxCount = 5;
		private float _handshakeRetryDelay = 2.5f;

		private string _localIP = String.Empty;

		private bool _allowInternalUnconnectedMessages = true;
		private bool _allowConnectionToSelf = false;

		private Type _socketType = typeof(NetworkSocket.UDPIPv4Only);

		private readonly NetworkP2PBase _network;

		public ResendDelayFunction resendDelayFunction
		{
			get { return _resendFunction; }
			set { _resendFunction = value; _Apply(); }
		}

		public int receiveBufferSize { get { return _receiveBufferSize; } set { _receiveBufferSize = value; _Apply(); } }
		public int sendBufferSize { get { return _sendBufferSize; } set { _sendBufferSize = value; _Apply(); } }

		public string localIP
		{
			get { return _localIP; }
			set { _localIP = value; _Apply(); }
		}

		/// <summary>
		/// Gets or sets how many data bytes that can maximally be sent using a single UDP packet.
		/// This can be set independently for the client and the server.
		/// Only modify this if you know what you are doing. 
		/// The default value 1400 has been carefully chosen for optimal speed and 
		/// throughput in most link layers in IP networks. 
		/// The value 1400 will ensure that UDP packets with uLink traffic (data and all headders) fit into one single 
		/// ethernet frame (1500 max) and thus the packet loss risk will be minimal for every UDP packet.
		/// Ethernet is a very common link layer in modern IP networks.
		/// </summary>
		public int maximumTransmissionUnit
		{
			get { return _maximumTransmissionUnit; }
			set { _maximumTransmissionUnit = value; _Apply(); }
		}

		/// <summary>
		/// Gets or sets the number of seconds between pings.
		/// This can be set independently for the client and the server.
		/// Default value is 3 seconds.
		/// </summary>
		public float timeBetweenPings
		{
			get { return _timeBetweenPings; }
			set { _timeBetweenPings = value; _Apply(); }
		}

		/// <summary>
		/// Gets or sets the time in seconds before a connection times out when no network packets are received from remote host.
		/// This can be set independently for the client and the server. Default value is 30 seconds.
		/// </summary>
		public float timeoutDelay
		{
			get { return _timeoutDelay; }
			set { _timeoutDelay = value; _Apply(); }
		}

		/// <summary>
		/// Gets or sets the maximum number of re-attempts to connect to the remote host before giving up.
		/// This only has meaning for the client, or the connecting peer. Default value is 5 attempts.
		/// </summary>
		public int handshakeRetriesMaxCount
		{
			get { return _handshakeRetriesMaxCount; }
			set { _handshakeRetriesMaxCount = value; _Apply(); }
		}

		/// <summary>
		/// Gets or sets the number of seconds between handshake attempts.
		/// This only has meaning for the client, or the connecting peer.
		/// Default value is 2.5 seconds.
		/// </summary>
		public float handshakeRetryDelay
		{
			get { return _handshakeRetryDelay; }
			set { _handshakeRetryDelay = value; _Apply(); }
		}

		/// <summary>
		/// If set to true, you can use the discovery feature to broadcast messages on LAN to find
		/// other peers, If set to false, discovery will not work but DOS/D-DOS attacks can not use this capability
		/// to make a lot of traffic on the local network.
		/// </summary>
		public bool allowInternalUnconnectedMessages
		{
			get { return _allowInternalUnconnectedMessages; }
			set { _allowInternalUnconnectedMessages = value; _Apply(); }
		}

		public bool allowConnectionToSelf
		{
			get { return _allowConnectionToSelf; }
			set { _allowConnectionToSelf = value; _Apply(); }
		}

		/// <summary>
		/// Determines whether uLink should queue all outgoing messages until the end of frame.
		/// </summary>
		/// <remarks>
		/// Batching lets uLink do more optimizations such as fitting multiple messages in a single packet,
		/// if they are not too big. There will be a slight CPU and bandwidth cost for turning off batching,
		/// how much depends on the amount and type of messages.
		/// But one benefit of disabling it, is that you then know the message has been sent after <see cref="uLink.NetworkView.RPC"/>
		/// has returned and CPU profiling of RPCs is more accurate, in Unity's Profiler. 
		/// </remarks>
		public bool batchSendAtEndOfFrame
		{
			get { return _network._batchSendAtEndOfFrame; }
			set { _network._batchSendAtEndOfFrame = value; }
		}

		public Type socketType
		{
			get { return _socketType; }
			set { _socketType = value; _Apply(); }
		}

		internal NetworkP2PConfig(NetworkP2PBase network)
		{
			_network = network;
		}

		private void _Apply()
		{
			_Apply(_network._peer);
		}

		internal void _Apply(NetBase net)
		{
			if (net == null) return;

			net.Configuration.ResendFunction = new NetConnection.ResendFunction(_resendFunction);

			if (net.Configuration.ReceiveBufferSize != _receiveBufferSize)
			{
				net.Configuration.ReceiveBufferSize = _receiveBufferSize;
				net.m_socket.receiveBufferSize = _receiveBufferSize;

				// make sure we have the actual buffer size
				net.Configuration.ReceiveBufferSize = net.m_socket.receiveBufferSize;
			}

			if (net.Configuration.SendBufferSize != _sendBufferSize)
			{
				net.Configuration.SendBufferSize = _sendBufferSize;
				net.m_socket.sendBufferSize = _sendBufferSize;

				// make sure we have the actual buffer size
				net.Configuration.SendBufferSize = net.m_socket.sendBufferSize;
			}

			net.Configuration.MaximumTransmissionUnit = _maximumTransmissionUnit;
			// Note that Lidgren uses "frequency" but means "time".
			net.Configuration.PingFrequency = _timeBetweenPings;
			net.Configuration.TimeoutDelay = _timeoutDelay;
			// Note that Lidgren uses the word "attempts" when it actually means "retries", i.e always one attempt + N.
			net.Configuration.HandshakeAttemptsMaxCount = _handshakeRetriesMaxCount;
			net.Configuration.HandshakeAttemptRepeatDelay = _handshakeRetryDelay;
			net.Configuration.AddressStr = _localIP;
			net.Configuration.SocketType = _socketType;

			net.SetMessageTypeEnabled(NetMessageType.OutOfBandData, _allowInternalUnconnectedMessages);
			net.Configuration.AllowConnectionToSelf = _allowConnectionToSelf;
		}
	}
}
