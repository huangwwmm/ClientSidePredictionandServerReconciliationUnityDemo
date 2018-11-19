#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12058 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-14 09:02:21 +0200 (Mon, 14 May 2012) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.IO;
using System.Net;
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

// TODO: shallow copy/clone function

// TODO: shallow copy between OnPlayerApproval and OnPlayerConnected etc ...

// TODO: add reset function to set seek begining of user data, after ulink header and stuff

// TODO: implement delta compression with huffman encoding or zlib compression?

// TODO: add BIT-size just like NetBuffer instead of padding everything to byte-size

// TODO: add read & write for optimized types like unit float, ranged float, ranged integer. don't forget to add assert for codecOptions: numberofbits, min/max etc.

namespace uLink
{
	/// <summary>
	/// Represents serialized variables, packed into a stream.
	/// </summary>
	/// <remarks>
	/// Data can be serialized, transmitted, and then received by remote clients by
	/// using this class. Read about state synchronization and automatic serializations
	/// of data types in the uLink manual to get the big picture. The code for
	/// serializing and deserializing objects are usually placed in the callback method
	/// <see cref="uLink.Network.uLink_OnSerializeNetworkView"/>.
	/// Check out the C# and javascript code examples for that callback method. 
	/// <para> 
	/// Also see the two other callbacks that are used to serialize and
	/// deserialize objects in some special situations: 
	/// <see cref="uLink.Network.uLink_OnSerializeNetworkViewOwner"/>
	/// and <see cref="uLink.NetworkP2P.uLink_OnHandoverNetworkView"/>.
	/// </para>
	/// <para> 
	/// We recommend that C# code and javascript code (also called Unity script)
	/// uses the methods <see cref="M:uLink.BitStream.Read``1(System.Object[])"/> and 
	/// <see cref="M:uLink.BitStream.Write``1"/>. Yes, it is possible to
	/// use generic types in javascript in Unity. It is not possible to use generic
	/// types in javascript outside Unity, this is a feature that has been added by
	/// Unity since v3.0 of the Editor. 
	/// </para>
	/// <para>
	/// Arrays (only one-dimentional) are handled by uLink. Arrays are no problem for 
	/// methods like <see cref="M:uLink.BitStream.Read``1(System.Object[])"/> and 
	/// <see cref="M:uLink.BitStream.Write``1"/>
	/// </para>
	/// <para> If you are using an Unity version older than 3.0, or for some other
	/// reason can't use generics, you can use the <see
	/// cref="O:uLink.BitStream.ReadObject"/> and <see
	/// cref="O:uLink.BitStream.WriteObject"/>
	/// methods.
	/// </para>
	/// <para> There are several methods for reading and writing just one specific
	/// datatype. These can be used to gain some performance, but the code you have to
	/// write will usually be a bit longer and not as elegant. Two examples of these
	/// method pairs are 
	/// <see cref="uLink.BitStream.ReadInt16"/>/<see cref="uLink.BitStream.WriteInt16"/> and 
	/// <see cref="uLink.BitStream.ReadString"/>/<see cref="uLink.BitStream.WriteString"/>. 
	/// </para>
	/// <para> 
	/// Because uLink is backward compatible with the Unity built-in network the
	/// method <see cref="O:uLink.BitStream.Serialize"/>
	/// is included in this class and can still be used. It supports a limited set of
	/// basic datatypes (the same as Unity built-in network). The new method <see
	/// cref="M:uLink.BitStream.Serialize``1"/> can be
	/// used to handle more data types (all data types supported by uLink), but we
	/// recommend that you use the generic read/write methods whenever possible. </para>
	/// </remarks>
	public class BitStream
	{
		internal NetBuffer _buffer;

		// 开洞 [10/24/2016 zhangdi]
		public void ResetBuffer()
		{
			_buffer.Reset();
		}
		// 开洞 [10/25/2016 zhangdi]
		public void CopyMyBufferTo(BitStream target)
		{
			if (this._buffer.LengthBytes > target._buffer.LengthBytes)
			{
				target._buffer.Data = new byte[this._buffer.LengthBytes];
			}
			for (int i = 0; i < this._buffer.LengthBytes; i++)
			{
				target._buffer.Data[i] = this._buffer.Data[i];
			}
			target._buffer.LengthBytes = this._buffer.LengthBytes;
		}

		public int GetBufferLength()
		{
			return _buffer.LengthBytes;
		}

		[Obsolete("Please note that BitStream._isWriting is an undocumented internal API, and is susceptible to change in the future. Only use it if you know what you are doing")]
		public bool _isWriting;

		[Obsolete("Please note that BitStream._data is an undocumented internal API, and is susceptible to change in the future. Only use it if you know what you are doing")]
		public byte[] _data { get { return _buffer.Data; } }

		[Obsolete("Please note that BitStream._bitCount is an undocumented internal API, and is susceptible to change in the future. Only use it if you know what you are doing")]
		public int _bitCount
		{
			get { return _buffer.LengthBits; }
			set { _buffer.LengthBits = value; }
		}

		[Obsolete("Please note that BitStream._bitIndex is an undocumented internal API, and is susceptible to change in the future. Only use it if you know what you are doing")]
		public int _bitIndex
		{
			get { return _buffer.PositionBits; }
			set { _buffer.PositionBits = value; }
		}

		[Obsolete("Please note that BitStream._bitCapacity is an undocumented internal API, and is susceptible to change in the future. Only use it if you know what you are doing")]
		public int _bitCapacity
		{
			get { return _buffer.Data.Length * 8; }
			set { _buffer.EnsureBufferSizeInBits(value); }
		}

		/// <summary>
		/// Gets a value indicating whether the BitStream is currently being written to.
		/// </summary>
		public bool isWriting
		{
			get { return _isWriting; }
		}

		/// <summary>
		/// Gets a value indicating whether the BitStream is currently being read.
		/// </summary>
		public bool isReading
		{
			get { return !_isWriting; }
		}

		/// <summary>
		/// Gets a value indicating whether there are any more bits to be read in the BitStream.
		/// </summary>
		public bool isEOF { get { return _buffer.PositionBits == _buffer.LengthBits; } }

		public int bitsRemaining { get { return _buffer.BitsRemaining; } }

		public int bytesRemaining { get { return _buffer.BytesRemaining; } }

#if DRAGONSCALE
		public static PoolT MakePool<PoolT>() where PoolT : AbstractPool<BitStream>, new()
		{
			return AbstractPool<BitStream>.MakePool<BitStream, PoolT>((pool) => new BitStream((NetBuffer)null, false));
		}
#endif

		public BitStream(bool isTypeSafe)
		{
			_buffer = new NetBuffer();
			_isWriting = true;
		}

		public BitStream(int capacityInBytes, bool isTypeSafe)
		{
			_buffer = new NetBuffer(capacityInBytes);
			_isWriting = true;
		}

		public BitStream(byte[] data, bool isTypeSafe)
			: this(data, false, isTypeSafe)
		{
		}

		public BitStream(byte[] data, int bitIndex, int bitCount, bool isWriting, bool isTypeSafe)
		{
			_buffer = new NetBuffer(data, bitIndex, bitCount);
			_isWriting = isWriting;
		}

		internal BitStream(NetBuffer buffer, bool isTypeSafe)
		{
			_buffer = buffer;
			_isWriting = false;
		}

		internal BitStream(bool isWriting, bool isTypeSafe)
		{
			_buffer = new NetBuffer();
			_isWriting = isWriting;
		}

		private BitStream(byte[] data, bool isWriting, bool isTypeSafe)
		{
			_buffer = new NetBuffer(data);
			_buffer.LengthBytes = data.Length;

			_isWriting = isWriting;
		}

		public void ExpandCapacity(int additionalBytes)
		{
			_buffer.EnsureBufferSizeInBytes(_buffer.LengthBytes + additionalBytes);
		}

		public void ExpandCapacityInBits(int additionalBits)
		{
			_buffer.EnsureBufferSizeInBits(_buffer.LengthBits + additionalBits);
		}

#if !NO_BITSTREAM_CODEC
		/// <summary>
		/// Deserializes different types of variables. Recommended for C# code and javascript code.
		/// 这个方法只给引用类型的用
		/// </summary>
		/// <param name="codecOptions">Optional parameters forwared to the deserializer</param>
		/// <remarks>
		/// Use this kind of function when reading from the stream.
		/// <para>
		/// The supported data types are documented in the uLink manual in the serialization section.
		/// </para>
		/// </remarks>
		//by WuNan @2016/09/12 10:54:58	封装无GC的Read接口
		public T Read<T>() where T : class
		{
			return Read<T>(Constants.EMPTY_OBJECT_ARRAY);
		}
		/// <summary>
		/// 反序列化 引用类型或者值类型的变量
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="codecOptions"></param>
		/// <returns></returns>
		public T Read<T>(params object[] codecOptions) where T : class
		{
			return (T)ReadObject(typeof(T).TypeHandle, codecOptions);
		}
		/// <summary>
		///  反序列化 引用类型或者值类型的变量
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="codecOptions"></param>
		/// <returns></returns>
		private T ReadStructOrClass<T>(params object[] codecOptions)
		{
			return (T)ReadObject(typeof(T).TypeHandle, codecOptions);
		}


		/// <summary>
		/// 反序列化struct类型的对象，注意:这个方法会由于装箱造成gc
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T ReadStruct<T>() where T : struct
		{
			return ReadStructOrClass<T>(Constants.EMPTY_OBJECT_ARRAY);
		}

		//[Obsolete("BitStream.Read<T>(out T, codecOptions) is deprecated, please use BitStream.Read<T>(codecOptions) instead")]
		//public void Read<T>(out T value, params object[] codecOptions) 
		//{
		//	value = ReadObject<T>(codecOptions);
		//}

		public bool TryRead<T>(out T value) 
		{
			try
			{
				value = ReadStructOrClass<T>(Constants.EMPTY_OBJECT_ARRAY);
				return true;
			}
			catch(Exception)
			{
				value = default(T);
				return false;
			}
		}


		/// <summary>
		/// Serializes different types of variables. Recommended for C# code and javascript code.
		/// </summary>
		/// <param name="value">The actual data</param>
		/// <param name="codecOptions">Optional parameters forwared to the serializer</param>
		/// <remarks>           
		/// Use this kind of function when writing the stream and the data type is known at design time.
		/// <para>All supported data types, that uLink serializes automatically, are documented in the uLink 
		/// manual in the serialization section.
		/// </para>
		/// </remarks>
		public void Write<T>(T value, params object[] codecOptions)
		{
			if (value is BitStream)
			{
				WriteBitStream(value as BitStream);
			}
			else
			{
				WriteObject(typeof(T).TypeHandle, value, codecOptions);
			}
		}

		/// <summary>
		/// Deserializes different types of variables. Recommended for Javascript code in Unity 2.6.
		/// </summary>
		/// <param name="typeHandle">The data type for this value that will be serialized</param>
		/// <param name="codecOptions">Optional parameters forwared to the deserializer</param>
		/// <remarks>
		/// Use this function when reading from the stream and the code is javascript in Unity 2.6.
		/// Use <see cref="M:uLink.BitStream.Read``1(System.Object[])"/> if you can use generics, the code will 
		/// be easier to debug and maintain.
		/// <para>
		/// The supported data types are documented in the uLink manual in the serialization section.
		/// </para>
		/// </remarks>
		public object ReadObject(RuntimeTypeHandle typeHandle, params object[] codecOptions)
		{
			//if(!(isReading)){Utility.Exception( "Can't read when BitStream is write only");}

			var codec = BitStreamCodec.Find(typeHandle);
			if(!(codec.deserializer != null)){Utility.Exception( "Missing Deserializer for type ", typeHandle);}

			return _ReadObject(codec, codecOptions);
		}

		[Obsolete("BitStream.ReadObject(typeHandle, out value, codecOptions) is deprecated, please use BitStream.ReadObject(typeHandle, codecOptions) instead")]
		public void ReadObject(RuntimeTypeHandle typeHandle, out object value, params object[] codecOptions)
		{
			value = ReadObject(typeHandle, codecOptions);
		}

		public bool TryReadObject(RuntimeTypeHandle typeHandle, out object value, params object[] codecOptions)
		{
			try
			{
				value = ReadObject(typeHandle, codecOptions);
				return true;
			}
			catch (Exception)
			{
				value = null;
				return false;
			}
		}

		public object _ReadObject(BitStreamCodec codec, params object[] codecOptions)
		{
			return codec.deserializer(this, codecOptions);
		}

		/// <summary>
		/// Serializes different types of variables. Recommended for Javascript code in Unity 2.6.
		/// </summary>
		/// <param name="typeHandle">The data type for the value that will be serialized</param>
		/// <param name="value">The actual data</param>
		/// <param name="codecOptions">Optional parameters forwared to the serializer</param>
		/// <remarks>
		/// Use this function when writing to the stream and the code is javascript in Unity 2.6..
		/// Use <see cref="M:uLink.BitStream.Write``1"/> if you can use generics, the code will 
		/// be easier to debug and maintain.
		/// <para>All supported data types are documented in the uLink manual in the serialization section.
		/// </para>
		/// </remarks>
		public void WriteObject(RuntimeTypeHandle typeHandle, object value, params object[] codecOptions)
		{
			//if(!(isWriting)){Utility.Exception( "Can't write when BitStream is read only");}

			BitStreamCodec codec = BitStreamCodec.Find(typeHandle);
			if(!(codec.serializer != null)){Utility.Exception( "Missing Serializer for type ", typeHandle);}

			_WriteObject(codec, value, codecOptions);
		}

		public void WriteObject(object value, params object[] codecOptions)
		{
			WriteObject(Type.GetTypeHandle(value), value, codecOptions);
		}

		public void _WriteObject(BitStreamCodec codec, object value, params object[] codecOptions)
		{
			codec.serializer(this, value, codecOptions);
		}

		// --------------------------------------------------------------------

		/// <summary>
		/// Serializes if <see cref="uLink.BitStream.isWriting"/>, otherwise deserializes.
		/// 注意：调用这个方法时，如果T类型是值类型，会有GC
		/// </summary>
		/// <param name="value">Be aware this function can only handle a
		/// reference to a value. It can not be used for getting or setting properties
		/// therefore.</param>
		/// <param name="codecOptions">Optional parameters forwared to the serializer/deserializer</param>
		/// <remarks>
		/// <para>The supported data types are documented in the uLink manual in the serialization section.
		/// </para>
		/// </remarks>
		public void Serialize<T>(ref T value, params object[] codecOptions) 
		{
			if (isWriting)
				Write(value, codecOptions);
			else
				//value = Read<T>(codecOptions);
				value = ReadStructOrClass<T>(codecOptions);
		}

		/// <summary>
		/// Serializes if <see cref="uLink.BitStream.isWriting"/>, otherwise deserializes. 
		/// </summary>
		/// <param name="value">Be aware this function can only handle a
		/// reference to a value. Therefore it can not be used for getting or setting 
		/// properties.</param>
		/// <param name="codecOptions">Optional parameters forwarded to the serializer/deserializer</param>
		/// <remarks>
		/// This method for serializiation is included in uLink only because uLink is backward 
		/// compatible with Unity bilt-in network. The recommended uLink alternative is 
		/// <see cref="M:uLink.BitStream.Read``1(System.Object[])"/> and <see cref="M:uLink.BitStream.Write``1"/>. 
		/// </remarks>

		//by WuNan @2016/09/12 10:57:55
		// 封装避免new object[0]的Serialize接口
		public void Serialize(ref bool value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadBoolean();
			}
		}
		public void Serialize(ref char value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadChar();
			}
		}
		public void Serialize(ref short value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadInt16();
			}
		}
		public void Serialize(ref int value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadInt32();
			}
		}
		public void Serialize(ref float value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadSingle();
			}
		}
		public void Serialize(ref Quaternion value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadQuaternion();
			}
		}
		public void Serialize(ref Vector3 value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadVector3();
			}
		}
		public void Serialize(ref NetworkPlayer value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadNetworkPlayer();
			}
		}
		public void Serialize(ref NetworkViewID value) {
			if (isWriting)
			{
				Write(value, Constants.EMPTY_OBJECT_ARRAY);
			}
			else
			{
				value = ReadNetworkViewID();
			}
		}


		public void Serialize(ref bool value, params object[] codecOptions) { Serialize<bool>(ref value, codecOptions); }
		public void Serialize(ref char value, params object[] codecOptions) { Serialize<char>(ref value, codecOptions); }
		public void Serialize(ref short value, params object[] codecOptions) { Serialize<short>(ref value, codecOptions); }
		public void Serialize(ref int value, params object[] codecOptions) { Serialize<int>(ref value, codecOptions); }
		public void Serialize(ref float value, params object[] codecOptions) { Serialize<float>(ref value, codecOptions); }
		public void Serialize(ref Quaternion value, params object[] codecOptions) { Serialize<Quaternion>(ref value, codecOptions); }
		public void Serialize(ref Vector3 value, params object[] codecOptions) { Serialize<Vector3>(ref value, codecOptions); }
		public void Serialize(ref NetworkPlayer value, params object[] codecOptions) { Serialize<NetworkPlayer>(ref value, codecOptions); }
		public void Serialize(ref NetworkViewID value, params object[] codecOptions) { Serialize<NetworkViewID>(ref value, codecOptions); }
#endif

		/// <summary>
		/// Returns the remaining bytes in a byte array. 
		/// </summary>
		public byte[] GetRemainingBytes()
		{
			int index = _buffer.PositionBytes;
			int size = _buffer.LengthBytes - index;
			byte[] remainder;

			if (size > 0)
			{
				remainder = Utility.SubArray(_buffer.Data, index, size);
			}
			else
			{
				remainder = new byte[0];
			}

			return remainder;
		}

		/// <summary>
		/// Copies the remaining bytes in a newly instantiated BitStream. 
		/// </summary>
		/// <remarks>
		/// Keeps the value if isWriting and isTypeSafe of the original Bitstream.
		/// </remarks>
		public BitStream GetRemainingBitStream()
		{
			return new BitStream(GetRemainingBytes(), isWriting, false);
		}

		public void AppendBitStream(BitStream stream)
		{
			_WriteBitStream(stream);
		}

		internal void _WriteBitStream(BitStream stream)
		{
			_buffer.Write(stream._buffer.Data, 0, stream._buffer.LengthBytes);
		}

		internal NetBuffer _ShareBuffer()
		{
			// TODO: is this still neccessary?
			// NOTE: this hacky code solves a bug in lidgren where it modifies the read position after using SendMessage

			var dummy = new NetBuffer();
			dummy.Data = _buffer.Data;
			dummy.LengthBits = _buffer.LengthBits;
			return dummy;
		}

		internal byte[] _ToArray()
		{
			return Utility.SubArray(_buffer.Data, 0, _buffer.LengthBytes);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that shows a list of all bytes in the BitStream.
		/// </summary>
		public override string ToString()
		{
			return Utility.BytesToString(_buffer.Data);
		}

		public char ReadChar() { return (char)_buffer.ReadUInt16(); }
		public bool ReadBoolean() { return (_buffer.ReadByte() != 0); }
		public sbyte ReadSByte() { return _buffer.ReadSByte(); }
		public byte ReadByte() { return _buffer.ReadByte(); }
		public short ReadInt16() { return _buffer.ReadInt16(); }
		public ushort ReadUInt16() { return _buffer.ReadUInt16(); }
		public int ReadInt32() { return _buffer.ReadInt32(); }
		public uint ReadUInt32() { return _buffer.ReadUInt32(); }
		public long ReadInt64() { return _buffer.ReadInt64(); }
		public ulong ReadUInt64() { return _buffer.ReadUInt64(); }
		public float ReadSingle() { return _buffer.ReadFloat(); }
		public double ReadDouble() { return _buffer.ReadDouble(); }
		public string ReadString() { return _buffer.ReadString(); }
		public IPEndPoint ReadIPEndPoint() { return _buffer.ReadIPEndPoint(); }
		public NetworkEndPoint ReadEndPoint() { return _buffer.ReadEndPoint(); }
		public NetworkPlayer ReadNetworkPlayer() { return new NetworkPlayer(_buffer); }
		public NetworkViewID ReadNetworkViewID() { return new NetworkViewID(_buffer); }
		public NetworkGroup ReadNetworkGroup() { return new NetworkGroup(_buffer); }
		public Vector2 ReadVector2() { return new Vector2(_buffer.ReadFloat(), _buffer.ReadFloat()); }
		public Vector3 ReadVector3() { return new Vector3(_buffer.ReadFloat(), _buffer.ReadFloat(), _buffer.ReadFloat()); }
		public Vector4 ReadVector4() { return new Vector4(_buffer.ReadFloat(), _buffer.ReadFloat(), _buffer.ReadFloat(), _buffer.ReadFloat()); }
		public Quaternion ReadQuaternion() { return new Quaternion(_buffer.ReadFloat(), _buffer.ReadFloat(), _buffer.ReadFloat(), _buffer.ReadFloat()); }
		public Color ReadColor() { return new Color(_buffer.ReadByte() / 255.0f, _buffer.ReadByte() / 255.0f, _buffer.ReadByte() / 255.0f, _buffer.ReadByte() / 255.0f); }
		
		internal Password ReadPassword() { return new Password(_buffer); }
		internal SerializedBuffer ReadSerializedBuffer() { return new SerializedBuffer(_buffer); }
		internal StateSync ReadStateSync() { return new StateSync(_buffer); }
		public PublicKey ReadPublicKey() { return new PublicKey(ReadBytes(), ReadBytes()); }
		public PrivateKey ReadPrivateKey() { return new PrivateKey(ReadBytes(), ReadBytes(), ReadBytes(), ReadBytes(), ReadBytes(), ReadBytes(), ReadBytes(), ReadBytes()); }
		internal SymmetricKey ReadSymmetricKey() { return new SymmetricKey(ReadBytes(), ReadBytes()); }
		public DateTime ReadDateTime() { return DateTime.FromBinary(_buffer.ReadInt64()); }
		public BitStream ReadBitStream() { return new BitStream(ReadBytes(), false); }

#if !PIKKO_BUILD && !DRAGONSCALE
		public NetworkPeer ReadNetworkPeer() { return new NetworkPeer(_buffer); }
		public LocalHostData ReadLocalHostData() { return new LocalHostData(_buffer); }
		public HostData ReadHostData() { return new HostData(_buffer); }
		public HostDataFilter ReadHostDataFilter() { return new HostDataFilter(_buffer); }
		public LocalPeerData ReadLocalPeerData() { return new LocalPeerData(_buffer); }
		public PeerData ReadPeerData() { return new PeerData(_buffer); }
		public PeerDataFilter ReadPeerDataFilter() { return new PeerDataFilter(_buffer); }
		public SerializedAssetBundle ReadSerializedAssetBundle() { return new SerializedAssetBundle(_buffer); }
		public NetworkP2PHandoverInstance ReadNetworkP2PHandoverInstance() { return new NetworkP2PHandoverInstance(_buffer); }
#endif

		public decimal ReadDecimal()
		{
			var bytes = _buffer.ReadBytes(16);

			using (MemoryStream memory = new MemoryStream(bytes))
			{
				using (BinaryReader reader = new BinaryReader(memory))
				{
					return reader.ReadDecimal();
				}
			}
		}

		public void WriteChar(char value) { _buffer.Write((ushort)value); }
		public void WriteBoolean(bool value) { _buffer.Write((byte)(value ? 1 : 0)); }
		public void WriteSByte(sbyte value) { _buffer.Write(value); }
		public void WriteByte(byte value) { _buffer.Write(value); }
		public void WriteInt16(short value) { _buffer.Write(value); }
		public void WriteUInt16(ushort value) { _buffer.Write(value); }
		public void WriteInt32(int value) { _buffer.Write(value); }
		public void WriteUInt32(uint value) { _buffer.Write(value); }
		public void WriteInt64(long value) { _buffer.Write(value); }
		public void WriteUInt64(ulong value) { _buffer.Write(value); }
		public void WriteSingle(float value) { _buffer.Write(value); }
		public void WriteDouble(double value) { _buffer.Write(value); }
		public void WriteString(string value) { _buffer.Write(value); }
		public void WriteIPEndPoint(IPEndPoint value) { _buffer.Write(value); }
		public void WriteEndPoint(EndPoint value) { _buffer.Write(value); }
		public void WriteEndPoint(NetworkEndPoint value) { _buffer.Write(value); }
		public void WriteNetworkPlayer(NetworkPlayer value) { value._Write(_buffer); }
		public void WriteNetworkViewID(NetworkViewID value) { value._Write(_buffer); }
		public void WriteNetworkGroup(NetworkGroup value) { value._Write(_buffer); }
		public void WriteVector2(Vector2 value) { _buffer.Write(value.x); _buffer.Write(value.y); }
		public void WriteVector3(Vector3 value) { _buffer.Write(value.x); _buffer.Write(value.y); _buffer.Write(value.z); }
		public void WriteVector4(Vector4 value) { _buffer.Write(value.x); _buffer.Write(value.y); _buffer.Write(value.z); _buffer.Write(value.w); }
		public void WriteQuaternion(Quaternion value) { _buffer.Write(value.x); _buffer.Write(value.y); _buffer.Write(value.z); _buffer.Write(value.w); }
		public void WriteColor(Color value) { _buffer.Write((byte)(value.r * 255)); _buffer.Write((byte)(value.g * 255)); _buffer.Write((byte)(value.b * 255)); _buffer.Write((byte)(value.a * 255)); }
		internal void WritePassword(Password value) { value.Write(_buffer); }
		internal void WriteSerializedBuffer(SerializedBuffer value) { value.Write(_buffer); }
		internal void WriteStateSync(StateSync value) { value.Write(_buffer); }
		public void WritePublicKey(PublicKey value) { WriteBytes(value.modulus); WriteBytes(value.exponent); }
		public void WritePrivateKey(PrivateKey value) { WriteBytes(value.modulus); WriteBytes(value.exponent); WriteBytes(value.p); WriteBytes(value.q); WriteBytes(value.dp); WriteBytes(value.dq); WriteBytes(value.inverseQ); WriteBytes(value.d); }
		internal void WriteSymmetricKey(SymmetricKey value) { WriteBytes(value.key); WriteBytes(value.iv); }
		public void WriteDateTime(DateTime value) { _buffer.Write(value.ToBinary()); }
		public void WriteBitStream(BitStream value) { _buffer.WriteAndInclVarLen(value._buffer.Data, 0, value._buffer.LengthBytes); }

#if !PIKKO_BUILD &&!DRAGONSCALE
		public void WriteNetworkPeer(NetworkPeer value) { value._Write(_buffer); }
		public void WriteSerializedAssetBundle(SerializedAssetBundle value) { value._Write(_buffer); }
		public void WriteNetworkP2PHandoverInstance(NetworkP2PHandoverInstance value) { value._Write(_buffer); }
		public void WriteLocalHostData(LocalHostData value) { value._Write(_buffer); }
		public void WriteHostData(HostData value) { value._Write(_buffer); }
		public void WriteHostDataFilter(HostDataFilter value) { value._Write(_buffer); }
		public void WriteLocalPeerData(LocalPeerData value) { value._Write(_buffer); }
		public void WritePeerData(PeerData value) { value._Write(_buffer); }
		public void WritePeerDataFilter(PeerDataFilter value) { value._Write(_buffer); }
#endif

		public void WriteDecimal(decimal value)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(memory))
				{
					writer.Write(value);
					_buffer.Write(memory.ToArray());
				}
			}
		}

		public byte[] ReadBytes()
		{
			uint size = _buffer.ReadVariableUInt32();
			return _buffer.ReadBytes((int)size);
		}

		internal void _ReadFixedBytes(byte[] intoBytes, int length, int offset = 0) //TDOO: this and all call sites will need rework after change of BitStream
		{
			//placeholder implementation until new BitStream, can't accept allocations
			for (int i = 0; i < length; ++i)
			{
				intoBytes[i + offset] = ReadByte();
			}
		}

		public void WriteBytes(byte[] bytes)
		{
			_buffer.WriteVariableUInt32((uint)bytes.Length);
			_buffer.Write(bytes);
		}

		internal void _WriteFixedBytes(byte[] bytes, int length, int offset = 0) //TDOO: this and all call sites will need rework after change of BitStream
		{
			_buffer.Write(bytes, offset, length);
		}

#if !PIKKO_BUILD && !DRAGONSCALE

		public HostData[] ReadHostDatas()
		{
			uint length = _buffer.ReadVariableUInt32();
			var values = new HostData[length];

			for (int i = 0; i < length; ++i)
				values[i] = ReadHostData();

			return values;
		}

		public void WriteHostDatas(HostData[] values)
		{
			_buffer.WriteVariableUInt32((uint)values.Length);

			foreach (var value in values)
				WriteHostData(value);
		}

#endif

		internal SerializedBuffer[] ReadSerializedBuffers()
		{
			uint length = _buffer.ReadVariableUInt32();
			var values = new SerializedBuffer[length];

			for (int i = 0; i < length; ++i)
				values[i] = ReadSerializedBuffer();

			return values;
		}

		internal void WriteSerializedBuffers(SerializedBuffer[] values)
		{
			_buffer.WriteVariableUInt32((uint)values.Length);

			foreach (var value in values)
				WriteSerializedBuffer(value);
		}

		internal StateSync[] ReadStateSyncs()
		{
			uint length = _buffer.ReadUInt32();
			var values = new StateSync[length];

			for (int i = 0; i < length; ++i)
				values[i] = ReadStateSync();

			return values;
		}

		/// <remarks>
		/// Only used for reference now, I ended up implementing directly in uLink multi-statesync code
		/// for simplicity (needed to interleave with message size checking) and to avoid allocations.
		/// </remarks>
		internal void WriteStateSyncs(StateSync[] values)
		{
			_buffer.Write((uint)values.Length);

			foreach (var value in values)
				WriteStateSync(value);
		}

		public NetworkViewID[] ReadNetworkViewIDs()
		{
			uint length = _buffer.ReadVariableUInt32();
			var values = new NetworkViewID[length];

			for (int i = 0; i < length; ++i)
				values[i] = ReadNetworkViewID();

			return values;
		}

		public void WriteNetworkViewIDs(NetworkViewID[] values)
		{
			_buffer.WriteVariableUInt32((uint)values.Length);

			foreach (var value in values)
				WriteNetworkViewID(value);
		}

#if !PIKKO_BUILD &&!DRAGONSCALE

		public NetworkP2PHandoverInstance[] ReadNetworkP2PHandoverInstances()
		{
			uint length = _buffer.ReadVariableUInt32();
			var values = new NetworkP2PHandoverInstance[length];

			for (int i = 0; i < length; ++i)
				values[i] = ReadNetworkP2PHandoverInstance();

			return values;
		}

		public void WriteNetworkP2PHandoverInstances(NetworkP2PHandoverInstance[] values)
		{
			_buffer.WriteVariableUInt32((uint)values.Length);

			foreach (var value in values)
				WriteNetworkP2PHandoverInstance(value);
		}

#endif

		/// <summary>
		/// 写Demo时为了方便就封装了这个函数，但是没考虑兼容性(跨平台、新老版本兼容)、GC、ErrorHandle
		/// 所以不要使用这个函数
		/// </summary>
		public void WriteObject<T>(T obj) where T : class
		{
			using (MemoryStream stream = new MemoryStream())
			{
				System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				formatter.Serialize(stream, obj);
				WriteBytes(stream.ToArray());
			}
		}

		/// <summary>
		/// 写Demo时为了方便就封装了这个函数，但是没考虑兼容性(跨平台、新老版本兼容)、GC、ErrorHandle
		/// 所以不要使用这个函数
		/// </summary>
		public T ReadObject<T>() where T : class
		{
			byte[] bytes = ReadBytes();
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				stream.Seek(0, SeekOrigin.Begin);
				return formatter.Deserialize(stream) as T;
			}
		}
	}
}
