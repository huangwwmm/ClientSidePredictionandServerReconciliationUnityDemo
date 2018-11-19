#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10143 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:29:20 +0100 (Tue, 29 Nov 2011) $
#endregion
using System.Collections.Generic;

namespace uLink
{
	internal struct ParameterWriterCache
	{
		private readonly System.Collections.Generic.Dictionary<string, ParameterWriter> _writers;

		public ParameterWriterCache(bool dummy)
		{
			_writers = new System.Collections.Generic.Dictionary<string, ParameterWriter>();
		}

		public void Write(BitStream stream, string name, object[] parameters)
		{
			if (ParameterWriter.CanPrepare(parameters))
			{
				ParameterWriter writer;
				if (_writers.TryGetValue(name, out writer) && writer.IsPreparedFor(parameters))
				{
					writer.WritePrepared(stream, parameters);
					return;
				}

				writer = new ParameterWriter(parameters);
				writer.WritePrepared(stream, parameters);
				_writers[name] = writer;
			}
			else
			{
				ParameterWriter.WriteUnprepared(stream, parameters);
			}
		}

		public void Clear()
		{
			_writers.Clear();
		}
	}
}