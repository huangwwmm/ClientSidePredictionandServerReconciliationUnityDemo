#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision$
// $LastChangedBy$
// $LastChangedDate$
#endregion
using System;

namespace uLink
{
	internal struct ParameterWriter
	{
		private readonly RuntimeTypeHandle[] _handles;
		private readonly BitStreamCodec[] _codecs;
		private readonly bool _appendStream;

		public ParameterWriter(object[] parameters)
		{
			int count = parameters.Length;
			_handles = new RuntimeTypeHandle[count];

			if(!(parameters[count - 1] != null)){Utility.Exception( "Can't serialize parameter ", count - 1, " because it is null!");}
			_appendStream = typeof(BitStream).TypeHandle.Equals(Type.GetTypeHandle(parameters[count - 1]));
			if (_appendStream)
			{
				count--;
				_handles[count] = typeof(BitStream).TypeHandle;
			}

			_codecs = new BitStreamCodec[count];

			for (int i = 0; i < count; i++)
			{
				object param = parameters[i];

				if(!(param != null)){Utility.Exception( "Can't serialize parameter ", i, " because it is null!");}

				var handle = Type.GetTypeHandle(param);
				_handles[i] = handle;

				var codec = BitStreamCodec.Find(handle);
				_codecs[i] = codec;

				if(!(codec.serializer != null)){Utility.Exception( "Missing Serializer for parameter type ", handle, " with value ", param);}
			}
		}

		public bool IsPreparedFor(object[] parameters)
		{
			if (_handles == null) return false;
			if (parameters.Length != _handles.Length) return false;

			for (int i = 0; i < _handles.Length; i++)
			{
				var handle = Type.GetTypeHandle(parameters[i]);
				if (!handle.Equals(_handles[i])) return false;
			}

			return true;
		}

		public void WritePrepared(BitStream stream, object[] parameters)
		{
			int i = 0;
			for (; i < _codecs.Length; i++)
			{
				object param = parameters[i];
				stream._WriteObject(_codecs[i], param);
			}

			if (_appendStream) stream.AppendBitStream(parameters[i] as BitStream);
		}

		public static bool CanPrepare(object[] parameters)
		{
			return parameters.Length > 1;
		}

		public static void WriteUnprepared(BitStream stream, object[] parameters)
		{
			int count = parameters.Length;
			if (count == 0) return;

			if (typeof(BitStream).TypeHandle.Equals(Type.GetTypeHandle(parameters[count - 1])))
			{
				count--;
				int i = 0;

				for (; i < count; i++)
				{
					WriteParameter(stream, parameters[i]);
				}

				stream.AppendBitStream(parameters[i] as BitStream);
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					WriteParameter(stream, parameters[i]);
				}
			}
		}

		private static void WriteParameter(BitStream stream, object parameter)
		{
			var typeHandle = Type.GetTypeHandle(parameter);
			BitStreamCodec codec = BitStreamCodec.Find(typeHandle);
			if(!(codec.serializer != null)){Utility.Exception( "Missing Serializer for type ", typeHandle);}

			stream._WriteObject(codec, parameter);
		}
	}
}
