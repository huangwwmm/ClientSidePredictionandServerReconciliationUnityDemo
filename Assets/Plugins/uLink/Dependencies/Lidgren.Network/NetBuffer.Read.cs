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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using uLink;

namespace Lidgren.Network
{
	internal sealed partial class NetBuffer
	{
		public const string c_readOverflowError = "Trying to read past the buffer size - likely caused by mismatching Write/Reads, different size or order.";

		/// <summary>
		/// Will overwrite any existing data
		/// </summary>
		public void CopyFrom(NetBuffer source)
		{
			int byteLength = source.LengthBytes;
			EnsureBufferSizeInBits(byteLength * 8);
			Buffer.BlockCopy(source.Data, 0, Data, 0, byteLength);
			m_bitLength = source.m_bitLength;
			m_readPosition = 0;
		}

		public byte PeekByte()
		{
			int bytePtr = m_readPosition >> 3;
			byte retval = Data[bytePtr];
			return retval;
		}

		//
		// 8 bit
		//
		public bool ReadBoolean()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 8, c_readOverflowError);
			int bytePtr = m_readPosition >> 3;
			byte retval = Data[bytePtr];
			m_readPosition += 8;
			return (retval > 0 ? true : false);
		}

		public byte ReadByte()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 8, c_readOverflowError);
			int bytePtr = m_readPosition >> 3;
			byte retval = Data[bytePtr];
			m_readPosition += 8;
			return retval;
		}

		//[CLSCompliant(false)]
		public sbyte ReadSByte()
		{
			byte retval = ReadByte();
			return (sbyte)retval;
		}

		public byte ReadByte(int numberOfBits)
		{
			return ReadByte();
		}

		public byte[] ReadBytes(int numberOfBytes)
		{
			byte[] val = new byte[numberOfBytes]; // TODO: possible DoS attack by making fooling the recipient to allocate too much memory. 
			ReadBytes(val, 0, numberOfBytes);
			return val;
		}

		public void ReadBytes(byte[] into, int offset, int numberOfBytes)
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= (numberOfBytes * 8), c_readOverflowError);
			Debug.Assert(offset + numberOfBytes <= into.Length);

			int bytePtr = m_readPosition >> 3;
			for (int i = 0; i < numberOfBytes; i++)
			{
				into[offset++] = Data[bytePtr++];
			}
			m_readPosition += (numberOfBytes * 8);
		}

		//
		// 16 bit
		//
		public Int16 ReadInt16()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 16, c_readOverflowError);
			int bytePtr = m_readPosition >> 3;

			short retval = Data[bytePtr++];
			retval |= (short) (Data[bytePtr] << 8);

			m_readPosition += 16;
			return retval;
		}

		//[CLSCompliant(false)]
		public UInt16 ReadUInt16()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 16, c_readOverflowError);
			int bytePtr = m_readPosition >> 3;

			ushort retval = Data[bytePtr++];
			retval |= (ushort)(Data[bytePtr] << 8);

			m_readPosition += 16;
			return retval;
		}

		//
		// 32 bit
		//
		public Int32 ReadInt32()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);
			int bytePtr = m_readPosition >> 3;

			Int32 retval = Data[bytePtr++];
			retval |= (Int32)(Data[bytePtr++] << 8);
			retval |= (Int32)(Data[bytePtr++] << 16);
			retval |= (Int32)(Data[bytePtr] << 24);

			m_readPosition += 32;
			return retval;
		}

		//[CLSCompliant(false)]
		public UInt32 ReadUInt32()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);
			int bytePtr = m_readPosition >> 3;

			UInt32 retval = Data[bytePtr++];
			retval |= (UInt32)(Data[bytePtr++] << 8);
			retval |= (UInt32)(Data[bytePtr++] << 16);
			retval |= (UInt32)(Data[bytePtr] << 24);

			m_readPosition += 32;
			return retval;
		}

		//
		// 64 bit
		//
		//[CLSCompliant(false)]
		public UInt64 ReadUInt64()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);
			ulong low = ReadUInt32();
			ulong high = ReadUInt32();
			ulong retval = low + (high << 32);

			return retval;
		}

		public Int64 ReadInt64()
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);
			unchecked
			{
				ulong retval = ReadUInt64();
				long longRetval = (long)retval;
				return longRetval;
			}
		}

		//[CLSCompliant(false)]
		public UInt64 ReadUInt64(int numberOfBits)
		{
			NetUtility.Assert(m_bitLength - m_readPosition >= (((numberOfBits + 7) >> 3) * 8), c_readOverflowError);

			ulong result = 0;
			int bytePtr = m_readPosition / 8;

			for (int bits = 0; bits < numberOfBits; bits += 8)
			{
				result |= ((ulong)Data[bytePtr++]) << bits;
			}

			m_readPosition += ((numberOfBits + 7) / 8) * 8;
			return result;
		}

		//
		// Floating point
		//
		public float ReadFloat()
		{
			return ReadSingle();
		}

		public float ReadSingle()
		{
			return new SingleUnion { bits = ReadUInt32() }.value;
		}

		public double ReadDouble()
		{
			return new DoubleUnion { bits = ReadUInt64() }.value;
		}

		//
		// Variable bit count
		//

		/// <summary>
		/// Reads a UInt32 written using WriteUnsignedVarInt()
		/// </summary>
		//[CLSCompliant(false)]
		public uint ReadVariableUInt32()
		{
			int num1 = 0;
			int num2 = 0;
			while (true)
			{
				if (num2 == 0x23)
					throw new FormatException("Bad 7-bit encoded integer");

				byte num3 = this.ReadByte();
				num1 |= (num3 & 0x7f) << num2;
				num2 += 7;
				if ((num3 & 0x80) == 0)
					return (uint)num1;
			}
		}

		/// <summary>
		/// Reads a Int32 written using WriteVariableInt32()
		/// </summary>
		public int ReadVariableInt32()
		{
			int num1 = 0;
			int num2 = 0;
			while (true)
			{
				if (num2 == 35)
					throw new FormatException("Bad 7-bit encoded integer");

				byte num3 = this.ReadByte();
				num1 |= (num3 & 0x7f) << (num2 & 0x1f);
				num2 += 7;
				if ((num3 & 0x80) == 0)
				{
					int sign = (num1 << 31) >> 31;
					return sign ^ (num1 >> 1);
				}
			}
		}

		/// <summary>
		/// Reads a UInt64 written using WriteVariableUInt64()
		/// </summary>
		//[CLSCompliant(false)]
		public UInt64 ReadVariableUInt64()
		{
			UInt64 num1 = 0;
			int num2 = 0;
			while (true)
			{
				if (num2 == 70)
					throw new FormatException("Bad 7-bit encoded integer");

				byte num3 = this.ReadByte();

				num1 |= ((UInt64)num3 & 127) << num2;
				num2 += 7;
				if ((num3 & 0x80) == 0)
					return num1;
			}
		}

		/// <summary>
		/// Reads a string
		/// </summary>
		public string ReadString()
		{
			int byteLen = (int)ReadVariableUInt32();

			if (byteLen == 0)
				return String.Empty;

			NetUtility.Assert(m_bitLength - m_readPosition >= (byteLen * 8), c_readOverflowError);

			if ((m_readPosition & 7) == 0)
			{
				// read directly
				string retval = System.Text.Encoding.UTF8.GetString(Data, m_readPosition >> 3, byteLen);
				m_readPosition += (8 * byteLen);
				return retval;
			}
			
			byte[] bytes = ReadBytes(byteLen);
			return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Reads a string using the string table
		/// </summary>
		public string ReadString(NetConnection sender)
		{
			return sender.ReadStringTable(this);
		}

		/// <summary>
		/// Reads a stored IPv4 endpoint description
		/// </summary>
		public IPEndPoint ReadIPEndPoint()
		{
			uint address = ReadUInt32();
			int port = (int)ReadUInt16();
			return new IPEndPoint(new IPAddress((long)address), port);
		}

		public NetworkEndPoint ReadEndPoint()
		{
			string typeName = ReadString();
			if (String.IsNullOrEmpty(typeName)) return ReadIPEndPoint();

			// TODO: remove ugly hack (which is there to make the UnassignedEndPoint type non-assembly-dependent).
			if (typeName == " ") return NetworkEndPoint.unassigned; 

			var type = Type.GetType(typeName, true);
			var temp = (EndPoint)Activator.CreateInstance(type, true);

			var socketAddress = ReadSocketAddress();
			return temp.Create(socketAddress);
		}

		public SocketAddress ReadSocketAddress()
		{
			int size = (int)ReadVariableUInt32() - 1 + 2;
			if (size == 1) return null;

			var family = (AddressFamily)ReadVariableInt32();
			var socketAddress = new SocketAddress(family, size);

			for (int i = 2; i < size; i++)
			{
				socketAddress[i] = ReadByte();
			}

			return socketAddress;
		}
	}
}
