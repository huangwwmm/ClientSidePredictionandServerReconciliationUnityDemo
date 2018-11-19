/* Copyright (c) 2008 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Net;
using uLink;

namespace Lidgren.Network
{
	/// <summary>
	/// Configuration for a NetBase derived class
	/// </summary>
	internal sealed class NetConfiguration
	{
		internal NetConnection.ResendFunction m_resendFunction;
		internal int m_startPort;
		internal int m_endPort;
		internal string m_addressStr;
		internal string m_appIdentifier;
		internal int m_receiveBufferSize, m_sendBufferSize;
		internal int m_maxConnections;
		internal int m_maximumTransmissionUnit;
		internal float m_pingFrequency;
		internal float m_timeoutDelay;
		internal int m_handshakeAttemptsMaxCount;
		internal float m_handshakeAttemptRepeatDelay;
		internal float m_resendTimeMultiplier;
		internal float m_maxAckWithholdTime;
		internal float m_disconnectLingerMaxDelay;
		internal float m_throttleBytesPerSecond;
		internal bool m_answerDiscoveryRequests;
		internal bool m_useMessageCoalescing;
		internal bool m_allowConnectionToSelf;
		internal Type m_socketType;

		public NetConnection.ResendFunction ResendFunction { get { return m_resendFunction; } set { m_resendFunction = value; } }

		/// <summary>
		/// Gets or sets the string identifying this particular application; distinquishing it from other Lidgren based applications. Ie. this needs to be the same on client and server.
		/// </summary>
		public string ApplicationIdentifier { get { return m_appIdentifier; } set { m_appIdentifier = value; } }

		/// <summary>
		/// Gets or sets the local port to bind to
		/// </summary>
		public int StartPort
		{
			get { return m_startPort; }
			set { m_startPort = value; }
		}

		public int EndPort
		{
			get { return m_endPort; }
			set { m_endPort = value; }
		}

		public bool UseMessageCoalescing
		{
			get { return m_useMessageCoalescing; }
			set { m_useMessageCoalescing = value; }
		}

		public bool AllowConnectionToSelf
		{
			get { return m_allowConnectionToSelf; }
			set { m_allowConnectionToSelf = value; }
		}

		// TODO: ugly hack
		public IPAddress Address
		{
			get { return IPAddress.Parse(m_addressStr); }
			set { m_addressStr = value.ToString(); }
		}

		public string AddressStr
		{
			get { return m_addressStr; }
			set { m_addressStr = value; }
		}

		public int ReceiveBufferSize { get { return m_receiveBufferSize; } set { m_receiveBufferSize = value; } }
		public int SendBufferSize { get { return m_sendBufferSize; } set { m_sendBufferSize = value; } }

		/// <summary>
		/// Gets or sets how many simultaneous connections a NetServer can have
		/// </summary>
		public int MaxConnections { get { return m_maxConnections; } set { m_maxConnections = value; } }

		/// <summary>
		/// Gets or sets how many bytes can maximally be sent using a single packet
		/// </summary>
		public int MaximumTransmissionUnit { get { return m_maximumTransmissionUnit; } set { m_maximumTransmissionUnit = value; } }

		/// <summary>
		/// Gets or sets the number of seconds between pings
		/// </summary>
		public float PingFrequency { get { return m_pingFrequency; } set { m_pingFrequency = value; } }

		/// <summary>
		/// Gets or sets the time in seconds before a connection times out when no answer is received from remote host
		/// </summary>
		public float TimeoutDelay { get { return m_timeoutDelay; } set { m_timeoutDelay = value; } }

		/// <summary>
		/// Gets or sets the maximum number of attempts to connect to the remote host
		/// </summary>
		public int HandshakeAttemptsMaxCount { get { return m_handshakeAttemptsMaxCount; } set { m_handshakeAttemptsMaxCount = value; } }

		/// <summary>
		/// Gets or sets the number of seconds between handshake attempts
		/// </summary>
		public float HandshakeAttemptRepeatDelay { get { return m_handshakeAttemptRepeatDelay; } set { m_handshakeAttemptRepeatDelay = value; } }

		/// <summary>
		/// Gets or sets the multiplier for resend times; increase to resend packets less often
		/// </summary>
		public float ResendTimeMultiplier { get { return m_resendTimeMultiplier; } set { m_resendTimeMultiplier = value; } }

		/// <summary>
		/// Gets or sets the amount of time, in multiple of current average roundtrip time,
		/// that acknowledges waits for other data to piggyback on before sending them explicitly
		/// </summary>
		public float MaxAckWithholdTime { get { return m_maxAckWithholdTime; } set { m_maxAckWithholdTime = value; } }

		/// <summary>
		/// Gets or sets the number of seconds allowed for a disconnecting connection to clean up (resends, acks)
		/// </summary>
		public float DisconnectLingerMaxDelay { get { return m_disconnectLingerMaxDelay; } set { m_disconnectLingerMaxDelay = value; } }

		/// <summary>
		/// Gets or sets if a NetServer/NetPeer should answer discovery requests
		/// </summary>
		public bool AnswerDiscoveryRequests { get { return m_answerDiscoveryRequests; } set { m_answerDiscoveryRequests = value; } }

		/// <summary>
		/// Gets or sets the amount of bytes allowed to be sent per second; set to 0 for no throttling
		/// </summary>
		public float ThrottleBytesPerSecond
		{
			get { return m_throttleBytesPerSecond; }
			set
			{
				m_throttleBytesPerSecond = value;
				//if (m_throttleBytesPerSecond < m_maximumTransmissionUnit)
				//	LogWrite("Warning: Throttling lower than MTU!");
			}
		}

		public Type SocketType
		{
			get { return m_socketType; }
			set { m_socketType = value; }
		}
		public NetConfiguration(string appIdentifier)
		{
			m_resendFunction = NetConnection.DefaultResendFunction;
			m_appIdentifier = appIdentifier;
			m_startPort = 0;
			m_endPort = 0;
			m_sendBufferSize = 0; // OS-default
			m_receiveBufferSize = 0; // OS-default
			m_maxConnections = -1;
			m_maximumTransmissionUnit = 1400;
			m_pingFrequency = 3.0f;
			m_timeoutDelay = 30.0f;
			m_handshakeAttemptsMaxCount = 5;
			m_handshakeAttemptRepeatDelay = 2.5f;
			m_maxAckWithholdTime = 0.5f; // one half RT wait before sending explicit ack
			m_disconnectLingerMaxDelay = 3.0f;
			m_resendTimeMultiplier = 1.1f;
			m_answerDiscoveryRequests = true;
			m_useMessageCoalescing = true;
			m_allowConnectionToSelf = false;
			m_socketType = typeof(NetworkSocket.UDPIPv4Only);
		}
	}
}
