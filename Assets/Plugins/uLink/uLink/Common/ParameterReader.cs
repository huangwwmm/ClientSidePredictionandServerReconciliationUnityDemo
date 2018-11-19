#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11266 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-02-02 21:26:58 +0100 (Thu, 02 Feb 2012) $
#endregion
using System;
using Lidgren.Network;

namespace uLink
{
	internal struct ParameterReader
	{
		private readonly BitStreamCodec[] _codecs;
		private readonly int _count;
		private readonly bool _appendStream;
		private readonly bool _appendMessageInfo;

		public ParameterReader(RPCMethod method, RuntimeTypeHandle messageInfoHandle)
		{
			_appendStream = false;
			_appendMessageInfo = false;

			var infos = method.info.GetParameters();
			_count = infos.Length;
			var handles = new RuntimeTypeHandle[_count];

			for (int i = 0; i < infos.Length; i++)
			{
				handles[i] = infos[i].ParameterType.TypeHandle;
			}

			int last = _count - 1;
			if (last >= 0)
			{
				var parameterHandle = handles[last];
				if (typeof(BitStream).TypeHandle.Equals(parameterHandle))
				{
					_appendStream = true;
					last--;
				}
				else if (messageInfoHandle.Equals(parameterHandle))
				{
					_appendMessageInfo = true;
					last--;

					if (last >= 0 && typeof(BitStream).TypeHandle.Equals(handles[last]))
					{
						_appendStream = true;
						last--;
					}
				}
			}

			_codecs = new BitStreamCodec[last + 1];

			for (int i = 0; i <= last; i++)
			{
				var handle = handles[i];
				var codec = BitStreamCodec.Find(handle);

				if(!(codec.deserializer != null)){Utility.Exception( "Missing Deserializer for parameter ", i, ", ", handle, ", in ", method);}

				_codecs[i] = codec;
			}
		}

		public object[] ReadParameters(BitStream stream, object messageInfo, RPCMethod method)
		{
			var parameters = new object[_count];
			int i = 0;

			// TODO: use a uLink specific exception instead.
			try
			{
				for (; i < _codecs.Length; i++)
				{
					parameters[i] = stream._ReadObject(_codecs[i]);
				}
			}
			catch (Exception e)
			{
				if (e.Message != NetBuffer.c_readOverflowError) throw;

				Log.Warning(NetworkLogFlags.RPC, "Trying to read past the buffer size when calling RPC ", method, " - likely caused by mismatching send parameters, different size or order.");
				return null;
			}

			if (_appendStream) parameters[i++] = stream;
			if (_appendMessageInfo) parameters[i] = messageInfo;

			int bytesRemaining = stream.bytesRemaining;
			if (!_appendStream && bytesRemaining > 0)
			{
				// TODO: does this happen all the time?
#if PIKKO_BUILD
				Log.Warning("All RPC parameters ({0} remaining bytes) were not read by the {1} declaration", bytesRemaining, method);
#else
				Log.Warning(NetworkLogFlags.RPC, "All RPC parameters (", bytesRemaining, " remaining bytes) were not read by the ", method, " declaration");
#endif
			}

			return parameters;
		}
	}
}
