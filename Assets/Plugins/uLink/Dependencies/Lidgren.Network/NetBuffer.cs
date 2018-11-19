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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.IO;

namespace Lidgren.Network
{
	/// <summary>
	/// Wrapper around a byte array with methods for reading/writing at bit level
	/// </summary>
	internal sealed partial class NetBuffer
	{
		// how many NetMessages are using this buffer?
		internal int m_refCount; // TODO: can we remove this for uLink & MasterServer?

		internal int m_bitLength;
		internal int m_readPosition;

		[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
		public byte[] Data;

		/// <summary>
		/// Creates a new NetBuffer
		/// </summary>
		public NetBuffer()
		{
			Data = new byte[64];
		}

		/// <summary>
		/// Creates a new NetBuffer initially capable of holding 'capacity' bytes
		/// </summary>
		public NetBuffer(int capacityBytes)
		{
			Data = new byte[Math.Max(capacityBytes, 64)];
		}

		public NetBuffer(byte[] Data)
		{
			this.Data = Data;
		}

		public NetBuffer(NetBuffer buffer)
		{
			Data = buffer.Data;
			m_readPosition = buffer.m_readPosition;
			m_bitLength = buffer.m_readPosition;
		}

		public NetBuffer(NetBuffer buffer, int lengthBits)
		{
			Data = buffer.Data;
			m_readPosition = buffer.m_readPosition;
			m_bitLength = m_readPosition + lengthBits;
		}

		public NetBuffer(byte[] data, int posBits, int lengthBits)
		{
			Data = data;
			m_readPosition = posBits;
			m_bitLength = lengthBits;
		}

		public NetBuffer(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				Data = new byte[1];
				WriteVariableUInt32(0);
				return;
			}

			byte[] strData = Encoding.UTF8.GetBytes(str);
			Data = new byte[1 + strData.Length];
			WriteVariableUInt32((uint)strData.Length);
			Write(strData);
		}

		/// <summary>
		/// Gets a stream to the data held by this netbuffer
		/// </summary>e
		public MemoryStream GetStream()
		{
			return new MemoryStream(Data, 0, LengthBytes, false, true);
		}

		/// <summary>
		/// Data is NOT copied; but now owned by the NetBuffer
		/// </summary>
		public static NetBuffer FromData(byte[] data)
		{
			NetBuffer retval = new NetBuffer(false);
			retval.Data = data;
			retval.LengthBytes = data.Length;
			return retval;
		}

		internal NetBuffer(bool createDataStorage)
		{
			if (createDataStorage)
				Data = new byte[8];
		}

		/// <summary>
		/// Gets or sets the length of the buffer in bytes
		/// </summary>
		public int LengthBytes
		{
			get { return ((m_bitLength + 7) >> 3); }
			set
			{
				m_bitLength = value * 8;
				EnsureBufferSizeInBits(m_bitLength);
			}
		}

		/// <summary>
		/// Gets or sets the length of the buffer in bits
		/// </summary>
		public int LengthBits
		{
			get { return m_bitLength; }
			set
			{
				m_bitLength = value;
				EnsureBufferSizeInBits(m_bitLength);
			}
		}

		/// <summary>
		/// Gets or sets the read position in the buffer, in bits (not bytes)
		/// </summary>
		public int PositionBits
		{
			get { return m_readPosition; }
			set { m_readPosition = value; }
		}

		public int PositionBytes
		{
			get { return (m_readPosition >> 3); }
			set { m_readPosition = value * 8; }
		}

		public int BitsRemaining { get { return LengthBits - PositionBits; } }

		public int BytesRemaining { get { return LengthBytes - PositionBytes; } }

		/// <summary>
		/// Resets read and write pointers
		/// </summary>
		public void Reset()
		{
			m_readPosition = 0;
			m_bitLength = 0;
			m_refCount = 0;
		}

		/// <summary>
		/// Resets read and write pointers
		/// </summary>
		internal void Reset(int readPos, int writePos)
		{
			m_readPosition = readPos;
			m_bitLength = writePos;
			m_refCount = 0;
		}

		/// <summary>
		/// Ensures this buffer can hold the specified number of bits prior to a write operation
		/// </summary>
		public void EnsureBufferSizeInBits(int numberOfBits)
		{
			EnsureBufferSizeInBytes((numberOfBits + 7) >> 3);
		}

		/// <summary>
		/// Ensures this buffer can hold the specified number of bits prior to a write operation
		/// </summary>
		public void EnsureBufferSizeInBytes(int numberOfBytes)
		{
			if (Data == null)
			{
				Data = new byte[numberOfBytes];
				return;
			}

			if (Data.Length < numberOfBytes)
			{
				var newData = new byte[numberOfBytes * 2];
				Buffer.BlockCopy(Data, 0, newData, 0, LengthBytes);
				Data = newData;
			}
		}

		/// <summary>
		/// Copies the content of the buffer to a new byte array
		/// </summary>
		public byte[] ToArray()
		{
			int len = LengthBytes;
			byte[] copy = new byte[len];
			Array.Copy(Data, copy, copy.Length);
			return copy;
		}

		public bool Equals(NetBuffer other)
		{
			if (m_bitLength != other.m_bitLength) return false;

			int lenBytes = LengthBytes;
			int posBytes = PositionBytes;
			int otherPosBytes = other.PositionBytes;

			for (int i = 0; i < lenBytes; i++)
			{
				if (Data[posBytes] != other.Data[otherPosBytes]) return false;

				posBytes++;
				otherPosBytes++;
			}

			return true;
		}

		public override string ToString()
		{
			return "[NetBuffer " + m_bitLength + " bits, " + m_readPosition + " read]";
		}
	}
}
