#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if !UNITY_BUILD && !PIKKO_BUILD
// NOTE: This is to workaround a issue (#643475) present in some versions of Mono,
// where Socket.ExclusiveAddressUse doesn't working with UDP. Thus sockets can
// accidentally be bound to the same port.
#define CHECK_ALREADYINUSE
#endif

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace uLink
{
	public partial class NetworkSocket
	{
#if CHECK_ALREADYINUSE
		private static object _checkingAlreadyInUseLock = new object();
#endif

		public sealed class UDPIPv4Only : NetworkSocket
		{
			//by WuNan @2016/09/28 14:28:49
#if (UNITY_IOS  || UNITY_TVOS ) && !UNITY_EDITOR
			private readonly IPEndPoint _anyEndPoint = new IPEndPoint(IPAddress.Any, 0);
			private readonly IPEndPoint _anyEndPointIPv6 = new IPEndPoint(IPAddress.IPv6Any, 0);
#else
			private readonly IPEndPoint _anyEndPoint = new IPEndPoint(IPAddress.Any, 0);
#endif

			private readonly Socket _socket;

			public UDPIPv4Only()
			{
				//by WuNan @2016/09/28 14:29:14
#if (UNITY_IOS  || UNITY_TVOS ) && !UNITY_EDITOR
				if(uLink.NetworkUtility.IsSupportIPv6())
				{
					_socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
				}
				else
				{
					_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				}
#else
				_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
#endif

				try
				{
					_socket.Blocking = false;
				}
				catch (Exception ex)
				{
					Lidgren.Log.Warning(Lidgren.LogFlags.Socket, "Failed to disable blocking on socket: ", ex);
				}

				try
				{
					_socket.ExclusiveAddressUse = true;
				}
				catch (Exception ex)
				{
					Lidgren.Log.Warning(Lidgren.LogFlags.Socket, "Failed to enable exclusive address usage on socket: ", ex);
				}

				try
				{
					_socket.EnableBroadcast = true;
				}
				catch (Exception ex)
				{
					Lidgren.Log.Warning(Lidgren.LogFlags.Socket, "Failed to enable broadcast on socket: ", ex);
				}

				try
				{
					_socket.Ttl = 255;
				}
				catch (Exception ex)
				{
					Lidgren.Log.Warning(Lidgren.LogFlags.Socket, "Failed to set TTL to 255 on socket: ", ex);
				}

				_ApplyWin2000Hack(_socket);
			}

			public override int receiveBufferSize
			{
				get
				{
					return _socket.ReceiveBufferSize;
				}
				set
				{
					_socket.ReceiveBufferSize = value;
					_CheckSocketOption("ReceiveBufferSize", _socket.ReceiveBufferSize, value);
				}
			}

			public override int sendBufferSize
			{
				get
				{
					return _socket.SendBufferSize;
				}
				set
				{
					_socket.SendBufferSize = value;
					_CheckSocketOption("SendBufferSize", _socket.SendBufferSize, value);
				}
			}

			public override int availableData
			{
				get
				{
					return _socket.Available;
				}
			}

			public override NetworkEndPoint listenEndPoint
			{
				get { return _socket.LocalEndPoint; }
			}

			public override void Bind(NetworkEndPoint listenEndPoint)
			{
#if CHECK_ALREADYINUSE
				var ipListenEndPoint = (IPEndPoint)listenEndPoint;

				lock (_checkingAlreadyInUseLock)
				{
					foreach (var endpoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners())
					{
						if (endpoint.Port == ipListenEndPoint.Port && (endpoint.Address.Equals(IPAddress.Any) || endpoint.Address.Equals(ipListenEndPoint.Address)))
						{
							return; // already in use
						}
					}
#endif

					_socket.Bind(listenEndPoint);

#if CHECK_ALREADYINUSE
				}
#endif

				// TODO: assert(_socket.IsBound)
			}

			public override int ReceivePacket(byte[] buffer, int offset, int size, out NetworkEndPoint sourceEndPoint, out double localTimeRecv)
			{
				EndPoint tempEndPoint = _anyEndPoint;
#if (UNITY_IOS  || UNITY_TVOS ) && !UNITY_EDITOR
				if (NetworkUtility.IsSupportIPv6())
				{
					tempEndPoint = _anyEndPointIPv6;
				}
#endif

				int read = _socket.ReceiveFrom(buffer, offset, size, SocketFlags.None, ref tempEndPoint);

				sourceEndPoint = tempEndPoint;
				localTimeRecv = NetworkTime.localTime;
				return read;
			}

			public override int SendPacket(byte[] buffer, int offset, int size, NetworkEndPoint targetEndPoint)
			{
				return _socket.SendTo(buffer, offset, size, SocketFlags.None, targetEndPoint);
			}

			public override void Close(int timeout)
			{
				_socket.Close(timeout);
			}

			// NOTE: In Windows 2000 (and likely other Windows versions as well),
			// UDP may not work correctly and may generate a WSAECONNRESET response.
			// To workaround the issue we use this fix from http://support.microsoft.com/kb/263823
			private static void _ApplyWin2000Hack(Socket socket)
			{
				/* The ioctl operation causes an exception in OS X 10.9 and also writes an error message to the console which cannot be disabled, so we omit it on this platform.
				   Since there is no good way of detecting Mac OS X in Mono we disable it for all Unix platforms, which should be OK since the operation only seems to be supported
				   in Windows. */
				if (Environment.OSVersion.Platform != PlatformID.Unix)
				{
					try
					{
						const uint IOC_OUT = 0x40000000;
						const uint IOC_IN = 0x80000000;
						const uint IOC_VENDOR = 0x18000000;
						const uint SIO_UDP_CONNRESET = (IOC_IN | IOC_OUT) | (IOC_VENDOR | 12);

						const int ioControlCode = unchecked((int)SIO_UDP_CONNRESET);
						var falseBooleanInBytes = new byte[] { 0 };

						socket.IOControl(ioControlCode, falseBooleanInBytes, null);
					}
					catch
					{
						// ignore; SIO_UDP_CONNRESET not supported on this platform
					}
				}
			}

			private static void _CheckSocketOption(string optionName, int actualValue, int desiredValue)
			{
				if (actualValue != desiredValue)
				{
					Lidgren.Log.Warning(Lidgren.LogFlags.Socket, "Socket ", optionName, " is actually set to ", actualValue, " and not the desired ", desiredValue);
				}
			}
		}
	}
}
