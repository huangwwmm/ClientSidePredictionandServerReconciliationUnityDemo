#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8678 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-22 22:31:15 +0200 (Mon, 22 Aug 2011) $
#endregion

using System;
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// Enables configuration of low-level network parameters, like connection timeouts, inactivity timeouts, ping frequency
	/// and MTU (Maximum Transmission Unit). See <see cref="uLink.Network.config"/>. 
	/// </summary>
	public class NetworkConfig
	{
		public delegate double ResendDelayFunction(int numSent, double avgRTT);

		private ResendDelayFunction _resendFunction = NetConnection.DefaultResendFunction;

		private int _sendBufferSize = Constants.DEFAULT_RECV_BUFFER_SIZE;
		private int _receiveBufferSize = Constants.DEFAULT_SEND_BUFFER_SIZE;

		private int _maximumTransmissionUnit = 1400;
		private float _timeBetweenPings = 3.0f;
		private float _timeoutDelay = 30.0f;
		private int _handshakeRetriesMaxCount = 5;
		private float _handshakeRetryDelay = 2.5f;

		private string _localIP = String.Empty;

		private bool _allowInternalUnconnectedMessages = true;
		private bool _allowConnectionToSelf = false;

		private Type _socketType = typeof(NetworkSocket.UDPIPv4Only);

		private readonly NetworkBaseLocal _network;

		[Obsolete("NetworkConfig.serverTimeOffsetInMillis is deprecated, please use NetworkTime.serverTimeOffset instead")]
		public long serverTimeOffsetInMillis
		{
			get { return NetTime.ToMillis(_network.rawServerTimeOffset); }
			set { throw new InvalidOperationException("Can't change server time offset any more"); }
		}

		[Obsolete("NetworkConfig.serverTimeOffset is deprecated, please use NetworkTime.serverTimeOffset instead")]
		public double serverTimeOffset
		{
			get { return _network.rawServerTimeOffset; }
			set { throw new InvalidOperationException("Can't change server time offset any more"); }
		}

		public ResendDelayFunction resendDelayFunction
		{
			get { return _resendFunction; }
			set { _resendFunction = value; _Apply(); }
		}

		[Obsolete("NetworkConfig.timeMeasurementFunction is deprecated, please use NetworkTime.timeMeasurementFunction instead")]
		public NetworkTimeMeasurementFunction timeMeasurementFunction
		{
			get { return NetworkTime.timeMeasurementFunction; }
			set { NetworkTime.timeMeasurementFunction = value; }
		}

		public string localIP
		{
			get { return _localIP; }
			set { _localIP = value; _Apply(); }
		}

		public int sendBufferSize { get { return _sendBufferSize; } set { _sendBufferSize = value; _Apply(); } }
		public int receiveBufferSize { get { return _receiveBufferSize; } set { _receiveBufferSize = value; _Apply(); } }

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
		/// If you set this to false, broadcast messages inside the network will not be allowed and features like discovery
		/// of local peers or servers in the local network will not work.
		/// If your game doesn't use these features, you might turn these off to prevent security issues in DOS/D-DOS attacks.
		/// Default value is true.
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
			get { return _network.batchSendAtEndOfFrame; }
			set { _network.batchSendAtEndOfFrame = value; }
		}

		public double recyclingDelayForViewID
		{
			get { return _network._recyclingDelayForViewID; }
			set { _network._recyclingDelayForViewID = value; }
		}

		public double recyclingDelayForPlayerID
		{
			get { return _network._recyclingDelayForPlayerID; }
			set { _network._recyclingDelayForPlayerID = value; }
		}

		public Type socketType
		{
			get { return _socketType; }
			set { _socketType = value; _Apply(); }
		}

		internal NetworkConfig(NetworkBaseLocal network)
		{
			_network = network;
		}

		private void _Apply()
		{
			_Apply(_network._GetNetBase());
			_Apply(_network._GetNetBaseMaster());
		}

		internal void _Apply(NetBase net)
		{
			if (net == null) return;

			net.Configuration.ResendFunction = new NetConnection.ResendFunction(_resendFunction);

			if (net.Configuration.ReceiveBufferSize != _receiveBufferSize)
			{
				net.Configuration.ReceiveBufferSize = _receiveBufferSize;

				if (net.m_socket != null)
				{
					net.m_socket.receiveBufferSize = _receiveBufferSize;

					// make sure we have the actual buffer size
					net.Configuration.ReceiveBufferSize = net.m_socket.receiveBufferSize;
				}
			}

			if (net.Configuration.SendBufferSize != _sendBufferSize)
			{
				net.Configuration.SendBufferSize = _sendBufferSize;

				if (net.m_socket != null)
				{
					net.m_socket.sendBufferSize = _sendBufferSize;

					// make sure we have the actual buffer size
					net.Configuration.SendBufferSize = net.m_socket.sendBufferSize;
				}
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

		/// <summary>
		/// Gets the NetworkConfig properties from <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		public void GetPrefs()
		{
			_localIP = NetworkPrefs.Get("Network.config.localIP", _localIP);
			_sendBufferSize = NetworkPrefs.Get("Network.config.sendBufferSize", _sendBufferSize);
			_receiveBufferSize = NetworkPrefs.Get("Network.config.receiveBufferSize", _receiveBufferSize);
			_maximumTransmissionUnit = NetworkPrefs.Get("Network.config.maximumTransmissionUnit", _maximumTransmissionUnit);
			_timeBetweenPings = NetworkPrefs.Get("Network.config.timeBetweenPings", _timeBetweenPings);
			_timeoutDelay = NetworkPrefs.Get("Network.config.timeoutDelay", _timeoutDelay);
			_handshakeRetriesMaxCount = NetworkPrefs.Get("Network.config.handshakeRetriesMaxCount", _handshakeRetriesMaxCount);
			_handshakeRetryDelay = NetworkPrefs.Get("Network.config.handshakeRetryDelay", _handshakeRetryDelay);
			batchSendAtEndOfFrame = NetworkPrefs.Get("Network.config.batchSendAtEndOfFrame", batchSendAtEndOfFrame);
			_allowInternalUnconnectedMessages = NetworkPrefs.Get("Network.config.allowInternalUnconnectedMessages", allowInternalUnconnectedMessages);
			_allowConnectionToSelf = NetworkPrefs.Get("Network.config.allowConnectionToSelf", allowConnectionToSelf);
			_Apply();
		}

		/// <summary>
		/// Sets the NetworkConfig properties in <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		/// <remarks>
		/// The method can't update the saved values in the persistent <see cref="uLink.NetworkPrefs.resourcePath"/> file,
		/// because that assumes the file is editable (i.e. the running project isn't built) and would require file I/O permission.
		/// Calling this will only update the values in memory.
		/// </remarks>
		public void SetPrefs()
		{
			NetworkPrefs.Set("Network.config.localIP", _localIP);
			NetworkPrefs.Set("Network.config.sendBufferSize", _sendBufferSize);
			NetworkPrefs.Set("Network.config.receiveBufferSize", _receiveBufferSize);
			NetworkPrefs.Set("Network.config.maximumTransmissionUnit", _maximumTransmissionUnit);
			NetworkPrefs.Set("Network.config.timeBetweenPings", _timeBetweenPings);
			NetworkPrefs.Set("Network.config.timeoutDelay", _timeoutDelay);
			NetworkPrefs.Set("Network.config.handshakeRetriesMaxCount", _handshakeRetriesMaxCount);
			NetworkPrefs.Set("Network.config.handshakeRetryDelay", _handshakeRetryDelay);
			NetworkPrefs.Set("Network.config.batchSendAtEndOfFrame", batchSendAtEndOfFrame);
			NetworkPrefs.Set("Network.config.allowInternalUnconnectedMessages", allowInternalUnconnectedMessages);
		}
	}
}
