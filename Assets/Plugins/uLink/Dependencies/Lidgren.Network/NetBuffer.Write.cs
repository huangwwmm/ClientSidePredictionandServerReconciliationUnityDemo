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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using uLink;

namespace Lidgren.Network
{
	internal sealed partial class NetBuffer
	{
		//
		// 8 bit
		//
		public void Write(bool value)
		{
			EnsureBufferSizeInBits(m_bitLength + 8);
			Data[LengthBytes] = value ? (byte)1 : (byte)0;
			m_bitLength += 8;
		}

		public void Write(byte source)
		{
			EnsureBufferSizeInBits(m_bitLength + 8);
			Data[LengthBytes] = source;
			m_bitLength += 8;
		}

		//[CLSCompliant(false)]
		public void Write(sbyte source)
		{
			Write((byte)source);
		}

		public void Write(byte[] source)
		{
			Write(source, 0, source.Length);
		}

		public void WriteAndInclVarLen(byte[] source)
		{
			WriteAndInclVarLen(source, 0, source.Length);
		}

		public void WriteAndInclVarLen(byte[] source, int offsetInBytes, int numberOfBytes)
		{
			WriteVariableUInt32((uint)numberOfBytes);
			Write(source, offsetInBytes, numberOfBytes);
		}

		public void Write(byte[] source, int offsetInBytes, int numberOfBytes)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			int bits = numberOfBytes * 8;
			EnsureBufferSizeInBits(m_bitLength + bits);

			Buffer.BlockCopy(source, offsetInBytes, Data, LengthBytes, numberOfBytes);

			m_bitLength += bits;
		}

		//
		// 16 bit
		//
		//[CLSCompliant(false)]
		public void Write(UInt16 source)
		{
			EnsureBufferSizeInBits(m_bitLength + 16);
			Data[LengthBytes] = (byte)(source);
			Data[LengthBytes + 1] = (byte)(source >> 8);
			m_bitLength += 16;
		}

		//[CLSCompliant(false)]
		public void Write(Int16 source)
		{
			EnsureBufferSizeInBits(m_bitLength + 16);
			Data[LengthBytes] = (byte)(source);
			Data[LengthBytes + 1] = (byte)(source >> 8);
			m_bitLength += 16;
		}

		//
		// 32 bit
		//

		public void Write(Int32 source)
		{
			EnsureBufferSizeInBits(m_bitLength + 32);
			Data[LengthBytes] = (byte)(source);
			Data[LengthBytes + 1] = (byte)(source >> 8);
			Data[LengthBytes + 2] = (byte)(source >> 16);
			Data[LengthBytes + 3] = (byte)(source >> 24);
			m_bitLength += 32;
		}

		//[CLSCompliant(false)]
		public void Write(UInt32 source)
		{
			EnsureBufferSizeInBits(m_bitLength + 32);
			Data[LengthBytes] = (byte)(source);
			Data[LengthBytes + 1] = (byte)(source >> 8);
			Data[LengthBytes + 2] = (byte)(source >> 16);
			Data[LengthBytes + 3] = (byte)(source >> 24);
			m_bitLength += 32;
		}

		//
		// 64 bit
		//
		//[CLSCompliant(false)]
		public void Write(UInt64 source)
		{
			EnsureBufferSizeInBits(m_bitLength + 64);
			for (int i = 0; i < 8; i++)
			{
				Data[LengthBytes + i] = (byte)(source >> (i * 8));
			}

			m_bitLength += 64;
		}

		//[CLSCompliant(false)]
		public void Write(UInt64 source, int numberOfBits)
		{
			EnsureBufferSizeInBits(m_bitLength + (((numberOfBits + 7) / 8) * 8));
			int byteLen = LengthBytes;

			for (int bits = 0; bits < numberOfBits; bits += 8)
			{
				Data[byteLen++] = (byte)(source >> bits);
			}

			LengthBytes = byteLen;
		}

		public void Write(Int64 source)
		{
			EnsureBufferSizeInBits(m_bitLength + 64);
			for (int i = 0; i < 8; i++)
			{
				Data[LengthBytes + i] = (byte)(source >> (i * 8));
			}

			m_bitLength += 64;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct SingleUnion
		{
			[FieldOffset(0)]
			public float value;

			[FieldOffset(0)]
			public uint bits;
		}

		public void Write(float source)
		{
			Write(new SingleUnion { value = source }.bits);
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct DoubleUnion
		{
			[FieldOffset(0)]
			public double value;

			[FieldOffset(0)]
			public ulong bits;
		}

		public void Write(double source)
		{
			Write(new DoubleUnion { value = source }.bits);
		}

		//
		// Variable bits
		//

		public static int SizeOfVariableUInt32(uint value)
		{
			if (value < 0x80) return 1;
			if (value < 0x4000) return 2;
			if (value < 0x200000) return 3;
			if (value < 0x10000000) return 4;
			return 5;
		}

		/// <summary>
		/// Write Base128 encoded variable sized unsigned integer
		/// </summary>
		/// <returns>number of bytes written</returns>
		//[CLSCompliant(false)]
		public int WriteVariableUInt32(uint value)
		{
			int retval = 1;
			uint num1 = (uint)value;
			while (num1 >= 0x80) //TODO: could manually unroll this and in similar functions for clarity AND performance, see e.g. SizeOfVariableUInt32 above for how much clearer it looks
			{
				this.Write((byte)(num1 | 0x80));
				num1 = num1 >> 7;
				retval++;
			}
			this.Write((byte)num1);
			return retval;
		}

		/// <summary>
		/// Write Base128 encoded variable sized signed integer
		/// </summary>
		/// <returns>number of bytes written</returns>
		public int WriteVariableInt32(int value)
		{
			int retval = 1;
			uint num1 = (uint)((value << 1) ^ (value >> 31));
			while (num1 >= 0x80)
			{
				this.Write((byte)(num1 | 0x80));
				num1 = num1 >> 7;
				retval++;
			}
			this.Write((byte)num1);
			return retval;
		}

		/// <summary>
		/// Write Base128 encoded variable sized unsigned integer
		/// </summary>
		/// <returns>number of bytes written</returns>
		//[CLSCompliant(false)]
		public int WriteVariableUInt64(UInt64 value)
		{
			int retval = 1;
			UInt64 num1 = (UInt64)value;
			while (num1 >= 0x80)
			{
				this.Write((byte)(num1 | 0x80));
				num1 = num1 >> 7;
				retval++;
			}
			this.Write((byte)num1);
			return retval;
		}

		/// <summary>
		/// Write a string
		/// </summary>
		public void Write(string source)
		{
			if (string.IsNullOrEmpty(source))
			{
				WriteVariableUInt32(0);
				return;
			}

			var length = Encoding.UTF8.GetByteCount(source);
			WriteVariableUInt32((uint)length);

			EnsureBufferSizeInBits(m_bitLength + length * 8);
			Encoding.UTF8.GetBytes(source, 0, source.Length, Data, LengthBytes);
			m_bitLength += length * 8;
		}

		/// <summary>
		/// Write a string using the string table
		/// </summary>
		public void Write(NetConnection recipient, string str)
		{
			recipient.WriteStringTable(this, str);
		}

		/// <summary>
		/// Writes an IPv4 endpoint description
		/// </summary>
		/// <param name="endPoint"></param>
		public void Write(IPEndPoint endPoint)
		{
			Write((uint)endPoint.Address.Address);
			Write((ushort)endPoint.Port);
		}

		public void Write(EndPoint endpoint)
		{
			Write((NetworkEndPoint)endpoint);
		}

		public void Write(NetworkEndPoint endpoint)
		{
			var ipEndpoint = endpoint.TryCast<IPEndPoint>();
			if (ipEndpoint != null)
			{
				Write((byte)0);
				Write(ipEndpoint);
				return;
			}

			// TODO: remove ugly hack (which is there to make the UnassignedEndPoint type non-assembly-dependent).
			if (endpoint.isUnassigned)
			{
				Write((byte)1);
				Write((byte)' ');
				return;
			}

			var type = endpoint.GetType();
			string typeName = _ToShortAssemblyQualifiedNameNullSafe(type);

			Write(typeName);
			Write(endpoint.Serialize());
		}

		public void Write(SocketAddress socketAddress)
		{
			if (socketAddress == null)
			{
				Write((byte)0);
				return;
			}

			int size = socketAddress.Size;
			// TODO: assert(size >= 2)

			WriteVariableUInt32((uint)size + 1 - 2);

			WriteVariableInt32((int)socketAddress.Family);

			for (int i = 2; i < size; i++)
			{
				Write(socketAddress[i]);
			}
		}

		private static string _ToShortAssemblyQualifiedNameNullSafe(Type type)
		{
			if (type == null) return null;

			string name = type.FullName;
			if (String.IsNullOrEmpty(name)) return name;

			name = name.Replace(", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "").Replace(", mscorlib", "");

			int i = name.LastIndexOf(", Version=", StringComparison.InvariantCultureIgnoreCase);
			while (i >= 0)
			{
				int end = name.IndexOf(']', i + 10);
				if (end == -1) end = name.Length;

				name = name.Remove(i, end - i);

				i = name.LastIndexOf(", Version=", StringComparison.InvariantCultureIgnoreCase);
			}

			return ReferenceEquals(type.Assembly, typeof(int).Assembly) ?
				name : String.Concat(name, ", ", type.Assembly.GetName().Name);
		}

		public void WriteDiff(byte[] newBuffer, int newLengthBytes, byte[] oldBuffer, int oldLengthBytes)
		{
			int bits = (newLengthBytes << 3);
			EnsureBufferSizeInBits(m_bitLength + bits);

			int offset = LengthBytes;

			if (newLengthBytes <= oldLengthBytes)
			{
				for (int i = 0; i < newLengthBytes; i++)
				{
					Data[offset + i] = (byte)(newBuffer[i] ^ oldBuffer[i]);
				}
			}
			else
			{
				for (int i = 0; i < oldLengthBytes; i++)
				{
					Data[offset + i] = (byte)(newBuffer[i] ^ oldBuffer[i]);
				}

				Buffer.BlockCopy(newBuffer, oldLengthBytes, Data, offset + oldLengthBytes, newLengthBytes - oldLengthBytes);
			}

			m_bitLength += bits;
		}

		public void WriteDiff(byte[] newBuffer, int newOffsetBytes, int newLengthBytes, byte[] oldBuffer, int oldLengthBytes)
		{
			int bits = (newLengthBytes << 3);
			EnsureBufferSizeInBits(m_bitLength + bits);

			int offset = LengthBytes;

			if (newLengthBytes <= oldLengthBytes)
			{
				for (int i = 0; i < newLengthBytes; i++)
				{
					Data[offset + i] = (byte)(newBuffer[newOffsetBytes + i] ^ oldBuffer[i]);
				}
			}
			else
			{
				for (int i = 0; i < oldLengthBytes; i++)
				{
					Data[offset + i] = (byte)(newBuffer[newOffsetBytes + i] ^ oldBuffer[i]);
				}

				Buffer.BlockCopy(newBuffer, newOffsetBytes + oldLengthBytes, Data, offset + oldLengthBytes, newLengthBytes - oldLengthBytes);
			}

			m_bitLength += bits;
		}

		public static byte[] CreateDiff(byte[] newBuffer, int newLengthBytes, byte[] oldBuffer, int oldLengthBytes)
		{
			var data = new byte[newLengthBytes];

			if (newLengthBytes <= oldLengthBytes)
			{
				for (int i = 0; i < newLengthBytes; i++)
				{
					data[i] = (byte)(newBuffer[i] ^ oldBuffer[i]);
				}
			}
			else
			{
				for (int i = 0; i < oldLengthBytes; i++)
				{
					data[i] = (byte)(newBuffer[i] ^ oldBuffer[i]);
				}

				Buffer.BlockCopy(newBuffer, oldLengthBytes, data, oldLengthBytes, newLengthBytes - oldLengthBytes);
			}

			return data;
		}
	}
}
