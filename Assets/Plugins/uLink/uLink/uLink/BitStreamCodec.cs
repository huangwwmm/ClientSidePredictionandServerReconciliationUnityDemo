#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10139 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:11:15 +0100 (Tue, 29 Nov 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Net;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if UNITY_BUILD
using UnityEngine;
#endif

// TODO: optimize quartanion codec to 3 ranged float axis or a like.

namespace uLink
{
	/// <summary>
	/// Used to build custom handling for serializing and deserializing of your game objects.
	/// </summary>
	/// <remarks>
	/// It is possible to build custom serialization and deserialization for any data type. Replace the uLink default 
	/// serializer and deserializer for any data type, or make a new data type and register it in uLink.
	/// </remarks>
	public class BitStreamCodec
	{
		/// <summary>
		/// Signature for deserializing (reader) methods
		/// </summary>
		public delegate object Deserializer(BitStream stream, params object[] codecOptions);

		/// <summary>
		/// Signature for serializing (writer) methods
		/// </summary>
		public delegate void Serializer(BitStream stream, object value, params object[] codecOptions);

		public Deserializer deserializer;
		public Serializer serializer;
		public readonly BitStreamTypeCode typeCode;

		private static readonly Dictionary<NetworkTypeHandle, BitStreamCodec> _codecs = new Dictionary<NetworkTypeHandle, BitStreamCodec>();

		private BitStreamCodec(Deserializer deserializer, Serializer serializer, BitStreamTypeCode typeCode)
		{
			this.deserializer = deserializer;
			this.serializer = serializer;
			this.typeCode = typeCode;
		}

		static BitStreamCodec()
		{
			AddAndMakeNullableAndArray<char>(_ReadChar, _WriteChar, BitStreamTypeCode.Char, true);
			AddAndMakeNullableAndArray<bool>(_ReadBoolean, _WriteBoolean, BitStreamTypeCode.Boolean, true);
			AddAndMakeNullableAndArray<sbyte>(_ReadSByte, _WriteSByte, BitStreamTypeCode.SByte, true);
			AddAndMakeNullableAndArray<byte>(_ReadByte, _WriteByte, BitStreamTypeCode.Byte, true);
			AddAndMakeNullableAndArray<short>(_ReadInt16, _WriteInt16, BitStreamTypeCode.Int16, true);
			AddAndMakeNullableAndArray<ushort>(_ReadUInt16, _WriteUInt16, BitStreamTypeCode.UInt16, true);
			AddAndMakeNullableAndArray<int>(_ReadInt32, _WriteInt32, BitStreamTypeCode.Int32, true);
			AddAndMakeNullableAndArray<uint>(_ReadUInt32, _WriteUInt32, BitStreamTypeCode.UInt32, true);
			AddAndMakeNullableAndArray<long>(_ReadInt64, _WriteInt64, BitStreamTypeCode.Int64, true);
			AddAndMakeNullableAndArray<ulong>(_ReadUInt64, _WriteUInt64, BitStreamTypeCode.UInt64, true);
			AddAndMakeNullableAndArray<float>(_ReadSingle, _WriteSingle, BitStreamTypeCode.Single, true);
			AddAndMakeNullableAndArray<double>(_ReadDouble, _WriteDouble, BitStreamTypeCode.Double, true);
			AddAndMakeNullableAndArray<DateTime>(_ReadDateTime, _WriteDateTime, BitStreamTypeCode.DateTime, true);
			AddAndMakeNullableAndArray<decimal>(_ReadDecimal, _WriteDecimal, BitStreamTypeCode.Decimal, true);
			AddAndMakeArray<string>(_ReadString, _WriteString, BitStreamTypeCode.String, true);
			AddAndMakeArray<IPEndPoint>(_ReadIPEndPoint, _WriteIPEndPoint, BitStreamTypeCode.IPEndPoint, true);
			AddAndMakeArray<EndPoint>(_ReadEndPoint, _WriteEndPoint, BitStreamTypeCode.EndPoint, true);
			AddAndMakeNullableAndArray<NetworkEndPoint>(_ReadNetworkEndPoint, _WriteNetworkEndPoint, BitStreamTypeCode.NetworkEndPoint, true);
			AddAndMakeNullableAndArray<NetworkPlayer>(_ReadNetworkPlayer, _WriteNetworkPlayer, BitStreamTypeCode.NetworkPlayer, true);
			AddAndMakeNullableAndArray<NetworkViewID>(_ReadNetworkViewID, _WriteNetworkViewID, BitStreamTypeCode.NetworkViewID, true);
			AddAndMakeNullableAndArray<NetworkGroup>(_ReadNetworkGroup, _WriteNetworkGroup, BitStreamTypeCode.NetworkGroup, true);
			AddAndMakeNullableAndArray<Vector2>(_ReadVector2, _WriteVector2, BitStreamTypeCode.Vector2, true);
			AddAndMakeNullableAndArray<Vector3>(_ReadVector3, _WriteVector3, BitStreamTypeCode.Vector3, true);
			AddAndMakeNullableAndArray<Vector4>(_ReadVector4, _WriteVector4, BitStreamTypeCode.Vector4, true);
			AddAndMakeNullableAndArray<Quaternion>(_ReadQuaternion, _WriteQuaternion, BitStreamTypeCode.Quaternion, true);
			AddAndMakeNullableAndArray<Color>(_ReadColor, _WriteColor, BitStreamTypeCode.Color, true);
			AddAndMakeArray<PublicKey>(_ReadPublicKey, _WritePublicKey, BitStreamTypeCode.PublicKey, true);
			AddAndMakeArray<PrivateKey>(_ReadPrivateKey, _WritePrivateKey, BitStreamTypeCode.PrivateKey, true);
			AddAndMakeArray<BitStream>(_ReadBitStream, _WriteBitStream, BitStreamTypeCode.BitStream, true);

#if !PIKKO_BUILD
			AddAndMakeNullableAndArray<NetworkPeer>(_ReadNetworkPeer, _WriteNetworkPeer, BitStreamTypeCode.NetworkPeer, true);
			AddAndMakeArray<LocalHostData>(_ReadLocalHostData, _WriteLocalHostData, BitStreamTypeCode.LocalHostData, true);
			AddAndMakeArray<HostData>(_ReadHostData, _WriteHostData, BitStreamTypeCode.HostData, true);
			AddAndMakeArray<HostDataFilter>(_ReadHostDataFilter, _WriteHostDataFilter, BitStreamTypeCode.HostDataFilter, true);
			AddAndMakeArray<LocalPeerData>(_ReadLocalPeerData, _WriteLocalPeerData, BitStreamTypeCode.LocalPeerData, true);
			AddAndMakeArray<PeerData>(_ReadPeerData, _WritePeerData, BitStreamTypeCode.PeerData, true);
			AddAndMakeArray<PeerDataFilter>(_ReadPeerDataFilter, _WritePeerDataFilter, BitStreamTypeCode.PeerDataFilter, true);
			AddAndMakeArray<SerializedAssetBundle>(_ReadSerializedAssetBundle, _WriteSerializedAssetBundle, BitStreamTypeCode.SerializedAssetBundle, true);
			AddAndMakeArray<NetworkP2PHandoverInstance>(_ReadNetworkP2PHandoverInstance, _WriteNetworkP2PHandoverInstance, BitStreamTypeCode.NetworkP2PHandoverInstance, true);
#endif
		}

		/// <summary>
		/// Add codec for specified type, including support for null values and arrays
		/// </summary>
		public static void AddAndMakeNullableAndArray<T>(Deserializer deserializer, Serializer serializer) where T : struct
		{
			AddAndMakeNullableAndArray<T>(deserializer, serializer, BitStreamTypeCode.Undefined, false);
		}

		/// <summary>
		/// Add codec for specified type, including support for null values and arrays
		/// </summary>
		/// <parameter name="typeCode">Type code, used to provide type-safe serialization as described in the "Serialization and data types" chapter in the manual</parameter>
		public static void AddAndMakeNullableAndArray<T>(Deserializer deserializer, Serializer serializer, BitStreamTypeCode typeCode, bool replaceIfExists) where T : struct
		{
			AddAndMakeArray<T>(deserializer, serializer, typeCode, replaceIfExists);

			BitStreamTypeCode nullableTypeCode = (typeCode == BitStreamTypeCode.Undefined || typeCode == BitStreamTypeCode.MaxValue) ? BitStreamTypeCode.Undefined : typeCode + 1;
			Add(typeof(T?).TypeHandle,
				delegate(BitStream stream, object[] args) { return _ReadNullable<T>(deserializer, stream, args); },
				delegate(BitStream stream, object value, object[] args) { _WriteNullable(serializer, stream, (T?)value, args); },
				nullableTypeCode, replaceIfExists);

			if (!typeof(T?).IsArray)
			{
				BitStreamTypeCode arrayTypeCode = (typeCode == BitStreamTypeCode.Undefined || nullableTypeCode >= BitStreamTypeCode.ArrayType1) ? BitStreamTypeCode.Undefined : nullableTypeCode + (int)BitStreamTypeCode.ArrayType1;
				Add(typeof(T?).MakeArrayType().TypeHandle,
					delegate(BitStream stream, object[] args) { return _ReadArray<T?>(deserializer, stream, args); },
					delegate(BitStream stream, object value, object[] args) { _WriteArray(serializer, stream, (T?[])value, args); },
					arrayTypeCode, replaceIfExists);
			}
		}

		/// <summary>
		/// Add codec for specified type, including support for null values
		/// </summary>
		public static void AddAndMakeNullable<T>(Deserializer deserializer, Serializer serializer) where T : struct
		{
			AddAndMakeNullable<T>(deserializer, serializer, BitStreamTypeCode.Undefined, false);
		}

		/// <summary>
		/// Add codec for specified type, including support for null values
		/// </summary>
		/// <parameter name="typeCode">Type code, used to provide type-safe serialization as described in the "Serialization and data types" chapter in the manual</parameter>
		public static void AddAndMakeNullable<T>(Deserializer deserializer, Serializer serializer, BitStreamTypeCode typeCode, bool replaceIfExists) where T : struct
		{
			Add<T>(deserializer, serializer, typeCode, replaceIfExists);

			BitStreamTypeCode nullableTypeCode = (typeCode == BitStreamTypeCode.Undefined || typeCode == BitStreamTypeCode.MaxValue) ? BitStreamTypeCode.Undefined : typeCode + 1;
			Add(typeof(T?).TypeHandle,
				delegate(BitStream stream, object[] args) { return _ReadNullable<T>(deserializer, stream, args); },
				delegate(BitStream stream, object value, object[] args) { _WriteNullable(serializer, stream, (T?)value, args); },
				nullableTypeCode, replaceIfExists);
		}

		/// <summary>
		/// Add codec for specified type, including support for arrays.
		/// </summary>
		public static void AddAndMakeArray<T>(Deserializer deserializer, Serializer serializer)
		{
			AddAndMakeArray<T>(deserializer, serializer, BitStreamTypeCode.Undefined, false);
		}

		/// <summary>
		/// Add codec for specified type, including support for arrays.
		/// </summary>
		/// <parameter name="typeCode">Type code, used to provide type-safe serialization as described in the "Serialization and data types" chapter in the manual</parameter>
		public static void AddAndMakeArray<T>(Deserializer deserializer, Serializer serializer, BitStreamTypeCode typeCode, bool replaceIfExists)
		{
			Add<T>(deserializer, serializer, typeCode, replaceIfExists);

			if (!typeof(T).IsArray)
			{
				BitStreamTypeCode arrayTypeCode = (typeCode == BitStreamTypeCode.Undefined || typeCode >= BitStreamTypeCode.ArrayType1) ? BitStreamTypeCode.Undefined : typeCode + (int)BitStreamTypeCode.ArrayType1;
				Add(typeof(T).MakeArrayType().TypeHandle,
					delegate(BitStream stream, object[] args) { return _ReadArray<T>(deserializer, stream, args); },
					delegate(BitStream stream, object value, object[] args) { _WriteArray(serializer, stream, (T[])value, args); },
					arrayTypeCode, replaceIfExists);
			}
		}

		/// <summary>
		/// Adds a serialization codec for the specified user-defined type.
		/// </summary>
		/// <parameter name="T">The type that should be serialized using this codec.</parameter>
		/// <parameter name="deserializer">The deserializer function. Based on the uLink.BitStreamCodec.Deserializer delegate.</parameter>
		/// <parameter name="serializer">The serializer function. Based on the uLink.BitStreamCodec.Serializer delegate.</parameter>
		public static void Add<T>(Deserializer deserializer, Serializer serializer)
		{
			Add(typeof(T).TypeHandle, deserializer, serializer, BitStreamTypeCode.Undefined, false);
		}

		/// <summary>
		/// Adds a serialization codec for the specified user-defined type.
		/// </summary>
		/// <parameter name="T">The type that should be serialized using this codec.</parameter>
		/// <parameter name="deserializer">The deserializer function. Based on the uLink.BitStreamCodec.Deserializer delegate.</parameter>
		/// <parameter name="serializer">The serializer function. Based on the uLink.BitStreamCodec.Serializer delegate.</parameter>
		/// <parameter name="typeCode">Type code, used to provide type-safe serialization as described in the "Serialization and data types" chapter in the manual</parameter>
		public static void Add<T>(Deserializer deserializer, Serializer serializer, BitStreamTypeCode typeCode, bool replaceIfExists)
		{
			Add(typeof(T).TypeHandle, deserializer, serializer, typeCode, replaceIfExists);
		}

		/// <summary>
		/// Add codec for specified type
		/// </summary>
		public static void Add(RuntimeTypeHandle typeHandle, Deserializer deserializer, Serializer serializer)
		{
			Add(typeHandle, deserializer, serializer, BitStreamTypeCode.Undefined, false);
		}

		/// <summary>
		/// Add codec for specified type
		/// </summary>
		/// <parameter name="typeCode">Type code, used to provide type-safe serialization as described in the "Serialization and data types" chapter in the manual</parameter>
		public static void Add(RuntimeTypeHandle typeHandle, Deserializer deserializer, Serializer serializer, BitStreamTypeCode typeCode, bool replaceIfExists)
		{
			Add(typeHandle, new BitStreamCodec(deserializer, serializer, typeCode), replaceIfExists);
		}

		private static void Add(RuntimeTypeHandle typeHandle, BitStreamCodec codec, bool replaceIfExists)
		{
			if (!replaceIfExists && _codecs.ContainsKey(typeHandle))
			{
#if PIKKO_BUILD
				Log.Error("Can't add BitStreamCodec because it already exists for type {0}", typeHandle);
#else
				Log.Error(NetworkLogFlags.BitStreamCodec, "Can't add BitStreamCodec because it already exists for type ", typeHandle);
#endif
				return;
			}

#if PIKKO_BUILD
			Log.Debug("Adding BitStreamCodec for type {0}", typeHandle);
#else
			Log.Debug(NetworkLogFlags.BitStreamCodec, "Adding BitStreamCodec for type ", typeHandle);
#endif

			_codecs[typeHandle] = codec;
		}

		/// <summary>
		/// Removes codec for specified type
		/// </summary>
		public static void Remove(RuntimeTypeHandle typeHandle)
		{
			if (!_codecs.Remove(typeHandle)) return;

			var type = Type.GetTypeFromHandle(typeHandle);

			if (!type.IsArray)
			{
				_codecs.Remove(type.MakeArrayType().TypeHandle);
			}

			try
			{
				var nullableType = typeof(Nullable<>).MakeGenericType(new[] { type });
				_codecs.Remove(nullableType.TypeHandle);
			}
			catch (Exception) {}
		}

		public static BitStreamCodec Find(RuntimeTypeHandle typeHandle)
		{
			BitStreamCodec codec;
			return _codecs.TryGetValue(typeHandle, out codec) ? codec : _CreateAndAddDefaultCodec(typeHandle);
		}

		internal static object _ReadChar(BitStream stream, object[] codecOptions) { return stream.ReadChar(); }
		internal static object _ReadBoolean(BitStream stream, object[] codecOptions) { return (stream.ReadByte() != 0); }
		internal static object _ReadSByte(BitStream stream, object[] codecOptions) { return stream.ReadSByte(); }
		internal static object _ReadByte(BitStream stream, object[] codecOptions) { return stream.ReadByte(); }
		internal static object _ReadInt16(BitStream stream, object[] codecOptions) { return stream.ReadInt16(); }
		internal static object _ReadUInt16(BitStream stream, object[] codecOptions) { return stream.ReadUInt16(); }
		internal static object _ReadInt32(BitStream stream, object[] codecOptions) { return stream.ReadInt32(); }
		internal static object _ReadUInt32(BitStream stream, object[] codecOptions) { return stream.ReadUInt32(); }
		internal static object _ReadInt64(BitStream stream, object[] codecOptions) { return stream.ReadInt64(); }
		internal static object _ReadUInt64(BitStream stream, object[] codecOptions) { return stream.ReadUInt64(); }
		internal static object _ReadSingle(BitStream stream, object[] codecOptions) { return stream.ReadSingle(); }
		internal static object _ReadDouble(BitStream stream, object[] codecOptions) { return stream.ReadDouble(); }
		internal static object _ReadString(BitStream stream, object[] codecOptions) { return stream.ReadString(); }
		internal static object _ReadIPEndPoint(BitStream stream, object[] codecOptions) { return stream.ReadIPEndPoint(); }
		internal static object _ReadEndPoint(BitStream stream, object[] codecOptions) { return (EndPoint)stream.ReadEndPoint(); }
		internal static object _ReadNetworkEndPoint(BitStream stream, object[] codecOptions) { return stream.ReadEndPoint(); }
		internal static object _ReadNetworkPlayer(BitStream stream, object[] codecOptions) { return stream.ReadNetworkPlayer(); }
		internal static object _ReadNetworkViewID(BitStream stream, object[] codecOptions) { return stream.ReadNetworkViewID(); }
		internal static object _ReadNetworkGroup(BitStream stream, object[] codecOptions) { return stream.ReadNetworkGroup(); }
		internal static object _ReadVector2(BitStream stream, object[] codecOptions) { return stream.ReadVector2(); }
		internal static object _ReadVector3(BitStream stream, object[] codecOptions) { return stream.ReadVector3(); }
		internal static object _ReadVector4(BitStream stream, object[] codecOptions) { return stream.ReadVector4(); }
		internal static object _ReadQuaternion(BitStream stream, object[] codecOptions) { return stream.ReadQuaternion(); }
		internal static object _ReadColor(BitStream stream, object[] codecOptions) { return stream.ReadColor(); }
		internal static object _ReadPublicKey(BitStream stream, object[] codecOptions) { return stream.ReadPublicKey(); }
		internal static object _ReadPrivateKey(BitStream stream, object[] codecOptions) { return stream.ReadPrivateKey(); }
		internal static object _ReadDateTime(BitStream stream, object[] codecOptions) { return stream.ReadDateTime(); }
		internal static object _ReadDecimal(BitStream stream, object[] codecOptions) { return stream.ReadDecimal(); }
		internal static object _ReadBitStream(BitStream stream, object[] codecOptions) { return stream.ReadBitStream(); }

#if !PIKKO_BUILD
		internal static object _ReadNetworkPeer(BitStream stream, object[] codecOptions) { return stream.ReadNetworkPeer(); }
		internal static object _ReadLocalHostData(BitStream stream, object[] codecOptions) { return stream.ReadLocalHostData(); }
		internal static object _ReadHostData(BitStream stream, object[] codecOptions) { return stream.ReadHostData(); }
		internal static object _ReadHostDataFilter(BitStream stream, object[] codecOptions) { return stream.ReadHostDataFilter(); }
		internal static object _ReadLocalPeerData(BitStream stream, object[] codecOptions) { return stream.ReadLocalPeerData(); }
		internal static object _ReadPeerData(BitStream stream, object[] codecOptions) { return stream.ReadPeerData(); }
		internal static object _ReadPeerDataFilter(BitStream stream, object[] codecOptions) { return stream.ReadPeerDataFilter(); }
		internal static object _ReadSerializedAssetBundle(BitStream stream, object[] codecOptions) { return stream.ReadSerializedAssetBundle(); }
		internal static object _ReadNetworkP2PHandoverInstance(BitStream stream, object[] codecOptions) { return stream.ReadNetworkP2PHandoverInstance(); }
#endif

		internal static void _WriteChar(BitStream stream, object value, object[] codecOptions) { stream.WriteUInt16((char)value); }
		internal static void _WriteBoolean(BitStream stream, object value, object[] codecOptions) { stream.WriteBoolean((bool)value); }
		internal static void _WriteSByte(BitStream stream, object value, object[] codecOptions) { stream.WriteSByte((sbyte)value); }
		internal static void _WriteByte(BitStream stream, object value, object[] codecOptions) { stream.WriteByte((byte)value); }
		internal static void _WriteInt16(BitStream stream, object value, object[] codecOptions) { stream.WriteInt16((short)value); }
		internal static void _WriteUInt16(BitStream stream, object value, object[] codecOptions) { stream.WriteUInt16((ushort)value); }
		internal static void _WriteInt32(BitStream stream, object value, object[] codecOptions) { stream.WriteInt32((int)value); }
		internal static void _WriteUInt32(BitStream stream, object value, object[] codecOptions) { stream.WriteUInt32((uint)value); }
		internal static void _WriteInt64(BitStream stream, object value, object[] codecOptions) { stream.WriteInt64((long)value); }
		internal static void _WriteUInt64(BitStream stream, object value, object[] codecOptions) { stream.WriteUInt64((ulong)value); }
		internal static void _WriteSingle(BitStream stream, object value, object[] codecOptions) { stream.WriteSingle((float)value); }
		internal static void _WriteDouble(BitStream stream, object value, object[] codecOptions) { stream.WriteDouble((double)value); }
		internal static void _WriteString(BitStream stream, object value, object[] codecOptions) { stream.WriteString((string)value); }
		internal static void _WriteIPEndPoint(BitStream stream, object value, object[] codecOptions) { stream.WriteIPEndPoint((IPEndPoint)value); }
		internal static void _WriteEndPoint(BitStream stream, object value, object[] codecOptions) { stream.WriteEndPoint((EndPoint)value); }
		internal static void _WriteNetworkEndPoint(BitStream stream, object value, object[] codecOptions) { stream.WriteEndPoint((NetworkEndPoint)value); }
		internal static void _WriteNetworkPlayer(BitStream stream, object value, object[] codecOptions) { stream.WriteNetworkPlayer((NetworkPlayer)value); }
		internal static void _WriteNetworkViewID(BitStream stream, object value, object[] codecOptions) { stream.WriteNetworkViewID((NetworkViewID)value); }
		internal static void _WriteNetworkGroup(BitStream stream, object value, object[] codecOptions) { stream.WriteNetworkGroup((NetworkGroup)value); }
		internal static void _WriteVector2(BitStream stream, object value, object[] codecOptions) { stream.WriteVector2((Vector2)value); }
		internal static void _WriteVector3(BitStream stream, object value, object[] codecOptions) { stream.WriteVector3((Vector3)value); }
		internal static void _WriteVector4(BitStream stream, object value, object[] codecOptions) { stream.WriteVector4((Vector4)value); }
		internal static void _WriteQuaternion(BitStream stream, object value, object[] codecOptions) { stream.WriteQuaternion((Quaternion)value); }
		internal static void _WriteColor(BitStream stream, object value, object[] codecOptions) { stream.WriteColor((Color)value); }
		internal static void _WritePublicKey(BitStream stream, object value, object[] codecOptions) { stream.WritePublicKey((PublicKey)value); }
		internal static void _WritePrivateKey(BitStream stream, object value, object[] codecOptions) { stream.WritePrivateKey((PrivateKey)value); }
		internal static void _WriteDateTime(BitStream stream, object value, object[] codecOptions) { stream.WriteDateTime((DateTime)value); }
		internal static void _WriteDecimal(BitStream stream, object value, object[] codecOptions) { stream.WriteDecimal((decimal)value); }
		internal static void _WriteBitStream(BitStream stream, object value, object[] codecOptions) { stream.WriteBitStream((BitStream)value); }

#if !PIKKO_BUILD
		internal static void _WriteNetworkPeer(BitStream stream, object value, object[] codecOptions) { stream.WriteNetworkPeer((NetworkPeer)value); }
		internal static void _WriteLocalHostData(BitStream stream, object value, object[] codecOptions) { stream.WriteLocalHostData((LocalHostData)value); }
		internal static void _WriteHostData(BitStream stream, object value, object[] codecOptions) { stream.WriteHostData((HostData)value); }
		internal static void _WriteHostDataFilter(BitStream stream, object value, object[] codecOptions) { stream.WriteHostDataFilter((HostDataFilter)value); }
		internal static void _WriteLocalPeerData(BitStream stream, object value, object[] codecOptions) { stream.WriteLocalPeerData((LocalPeerData)value); }
		internal static void _WritePeerData(BitStream stream, object value, object[] codecOptions) { stream.WritePeerData((PeerData)value); }
		internal static void _WritePeerDataFilter(BitStream stream, object value, object[] codecOptions) { stream.WritePeerDataFilter((PeerDataFilter)value); }
		internal static void _WriteSerializedAssetBundle(BitStream stream, object value, object[] codecOptions) { stream.WriteSerializedAssetBundle((SerializedAssetBundle)value); }
		internal static void _WriteNetworkP2PHandoverInstance(BitStream stream, object value, object[] codecOptions) { stream.WriteNetworkP2PHandoverInstance((NetworkP2PHandoverInstance)value); }
#endif

		private static BitStreamCodec _CreateAndAddDefaultCodec(RuntimeTypeHandle typeHandle)
		{
			BitStreamCodec codec;

			// perhaps the type's static constructor might register the codec, if it hasn't already been called.
			RuntimeHelpers.RunClassConstructor(typeHandle);
			if (_codecs.TryGetValue(typeHandle, out codec))
			{
				return codec;
			}

			var type = Type.GetTypeFromHandle(typeHandle);
			if (type.IsEnum)
			{
				var underlyingTypeHandle = Enum.GetUnderlyingType(type).TypeHandle;

#if PIKKO_BUILD
				Log.Debug("Adding default BitStreamCodec for enum type {0} from underlying type {1}", type, underlyingTypeHandle);
#else
				Log.Debug(NetworkLogFlags.BitStreamCodec, "Adding default BitStreamCodec for enum type ", type, " from underlying type ", underlyingTypeHandle);
#endif

				codec = Find(underlyingTypeHandle);
			}
			else
			{
#if PIKKO_BUILD
				Log.Debug("Creating and adding default BitStreamCodec for type {0}", type);
#else
				Log.Debug(NetworkLogFlags.BitStreamCodec, "Creating and adding default BitStreamCodec for type ", type);
#endif

				codec = new BitStreamCodec(_CreateDefaultDeserializer(type), _CreateDefaultSerializer(type), BitStreamTypeCode.Undefined);
			}

			_codecs.Add(typeHandle, codec);
			return codec;
		}

		private static Deserializer _CreateDefaultDeserializer(Type type)
		{
			var fields = _GetFieldDeserializers(type);
			var properties = _GetPropertyDeserializers(type);

			return delegate(BitStream stream, object[] codecOptions)
				{
					object instance = Activator.CreateInstance(type);

					foreach (var field in fields)
					{
						object value = field.Value(stream);
						field.Key.SetValue(instance, value);
					}

					foreach (var property in properties)
					{
						object value = property.Value(stream);
						property.Key.Invoke(instance, new[] { value });
					}

					return instance;
				};
		}

		private static Serializer _CreateDefaultSerializer(Type type)
		{
			var fields = _GetFieldSerializers(type);
			var properties = _GetPropertySerializers(type);

			return delegate(BitStream stream, object instance, object[] args)
				{
					foreach (var field in fields)
					{
						object value = field.Key.GetValue(instance);
						field.Value(stream, value);
					}

					foreach (var property in properties)
					{
						object value = property.Key.Invoke(instance, null);
						property.Value(stream, value);
					}
				};
		}

		private static KeyValuePair<FieldInfo, Deserializer>[] _GetFieldDeserializers(Type type)
		{
			var infos = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_ShellSort(infos);

			var list = new List<KeyValuePair<FieldInfo, Deserializer>>();
			foreach (var info in infos)
			{
				if (info.IsInitOnly || info.IsLiteral || info.IsNotSerialized) continue;

				var deserializer = Find(info.FieldType.TypeHandle).deserializer;
				if (deserializer == null) continue;

				list.Add(new KeyValuePair<FieldInfo, Deserializer>(info, deserializer));
			}

			return list.ToArray();
		}

		private static KeyValuePair<MethodInfo, Deserializer>[] _GetPropertyDeserializers(Type type)
		{
			var infos = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_ShellSort(infos);

			var list = new List<KeyValuePair<MethodInfo, Deserializer>>();
			foreach (var info in infos)
			{
				if (!info.CanRead || !info.CanWrite) continue;

				var method = info.GetSetMethod(true);
				if (method == null) continue;

				var deserializer = Find(info.PropertyType.TypeHandle).deserializer;
				if (deserializer == null) continue;

				list.Add(new KeyValuePair<MethodInfo, Deserializer>(method, deserializer));
			}

			return list.ToArray();
		}

		private static KeyValuePair<FieldInfo, Serializer>[] _GetFieldSerializers(Type type)
		{
			var infos = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_ShellSort(infos);

			var list = new List<KeyValuePair<FieldInfo, Serializer>>();
			foreach (var info in infos)
			{
				if (info.IsInitOnly || info.IsLiteral || info.IsNotSerialized) continue;

				var serializer = Find(info.FieldType.TypeHandle).serializer;
				if (serializer == null) continue;

				list.Add(new KeyValuePair<FieldInfo, Serializer>(info, serializer));
			}

			return list.ToArray();
		}

		private static KeyValuePair<MethodInfo, Serializer>[] _GetPropertySerializers(Type type)
		{
			var infos = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_ShellSort(infos);

			var list = new List<KeyValuePair<MethodInfo, Serializer>>();
			foreach (var info in infos)
			{
				if (!info.CanRead || !info.CanWrite) continue;

				var method = info.GetGetMethod(true);
				if (method == null) continue;

				var serializer = Find(info.PropertyType.TypeHandle).serializer;
				if (serializer == null) continue;

				list.Add(new KeyValuePair<MethodInfo, Serializer>(method, serializer));
			}

			return list.ToArray();
		}

		private static T[] _ReadArray<T>(Deserializer deserializer, BitStream stream, object[] codecOptions)
		{
			uint length = stream._buffer.ReadVariableUInt32();

			var values = new T[length];

			for (int i = 0; i < length; ++i)
				values[i] = (T)deserializer(stream, typeof(T).TypeHandle, codecOptions);

			return values;
		}

		private static void _WriteArray<T>(Serializer serializer, BitStream stream, T[] values, object[] codecOptions)
		{
			if (values == null)
			{
				stream._buffer.WriteVariableUInt32(0);
				return;
			}

			stream._buffer.WriteVariableUInt32((uint)values.Length);

			foreach (T value in values)
				serializer(stream, value, codecOptions);
		}

		internal static T? _ReadNullable<T>(Deserializer deserializer, BitStream stream, object[] codecOptions) where T : struct
		{
			bool hasValue = stream.ReadBoolean();
			if (!hasValue) return null;

			return (T)deserializer(stream, typeof(T).TypeHandle, codecOptions);
		}

		private static void _WriteNullable<T>(Serializer serializer, BitStream stream, T? nullable, object[] codecOptions) where T : struct
		{
			stream.WriteBoolean(nullable.HasValue);
			if (!nullable.HasValue) return;

			serializer(stream, nullable, codecOptions);
		}

		private static void _ShellSort(MemberInfo[] list)
		{
			int h;
			int j;
			MemberInfo tmp;

			h = 1;
			while (h * 3 + 1 <= list.Length)
				h = 3 * h + 1;

			while (h > 0)
			{
				for (int i = h - 1; i < list.Length; i++)
				{
					tmp = list[i];
					j = i;
					while (true)
					{
						if (j >= h)
						{
							if (string.Compare(list[j - h].Name, tmp.Name, StringComparison.InvariantCulture) > 0)
							{
								list[j] = list[j - h];
								j -= h;
							}
							else
								break;
						}
						else
							break;
					}

					list[j] = tmp;
				}
				h /= 3;
			}
		}
	}
}
