#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion

using System;

namespace uLink
{
	public abstract partial class NetworkSocket
	{
		/* TODO:
		public readonly bool isBlocking;
		public readonly bool isReliable;
		public readonly bool isOrdered;
		public readonly bool isConnectionOriented;
		public readonly bool supportsBroadcast;

		protected NetSocketBase(bool isBlocking, bool isReliable, bool isOrdered, bool isConnectionOriented, bool supportsBroadcast)
		{
			this.isBlocking = isBlocking;
			this.isReliable = isReliable;
			this.isOrdered = isOrdered;
			this.isConnectionOriented = isConnectionOriented;
			this.supportsBroadcast = supportsBroadcast;
		}
		*/

		public virtual int receiveBufferSize
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public virtual int sendBufferSize
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public virtual int maxPacketSize
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public virtual int availableData
		{
			get { return 1; }
		}

		/* TODO:
		public virtual int maxConnections
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
		*/

		public virtual NetworkEndPoint listenEndPoint
		{
			get { throw new NotImplementedException(); }
		}

		public abstract void Bind(NetworkEndPoint listenEndPoint);

		// TODO: should probably be moved to a new Reliability Abstraction Layer.
		public virtual void Listen(int maxConnections)
		{
		}

		/* TODO: should probably be moved to a new Reliability Abstraction Layer.
		public virtual bool Accept(out NetworkEndPoint sourceEndPoint)
		{
			return false;
		}
		*/

		// TODO: should probably be moved to a new Reliability Abstraction Layer.
		public virtual void Connect(NetworkEndPoint targetEndPoint)
		{
		}

		// TODO: should probably be moved to a new Reliability Abstraction Layer.
		public virtual void Disconnect(NetworkEndPoint targetEndPoint)
		{
		}
		public abstract int ReceivePacket(byte[] buffer, int offset, int size, out NetworkEndPoint sourceEndPoint, out double localTimeRecv);
		public abstract int SendPacket(byte[] buffer, int offset, int size, NetworkEndPoint targetEndPoint);

		public abstract void Close(int timeout);

		// TODO: this should be moved to its own partial file.
		public static NetworkSocket Create(Type type)
		{
			try
			{
				return (NetworkSocket)Activator.CreateInstance(type);
			}
			catch (Exception ex)
			{
				Lidgren.Log.Error(Lidgren.LogFlags.Socket, "Failed to create socket of type " + type + " falling back to default socket type " + typeof(UDPIPv4Only).Name, ex);
			}

			return new UDPIPv4Only();
		}
	}
}
