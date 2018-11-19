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
	public struct PortRange
	{
		public readonly int start;
		public readonly int end;

		public PortRange(int start, int end)
		{
			this.start = start;
			this.end = end;
		}

		public PortRange(string range)
		{
			var ports = range.Split(new[] { ':', '-' }, StringSplitOptions.RemoveEmptyEntries);

			if (ports.Length == 1)
			{
				end = start = UInt16.Parse(ports[0]);
				return;
			}

			if (ports.Length != 2)
			{
				start = 0;
				end = 0;
				return;
			}

			start = UInt16.Parse(ports[0]);
			end = UInt16.Parse(ports[1]);

			if (start > end)
			{
				int swap = start;
				start = end;
				end = swap;
			}
		}

		public override string ToString()
		{
			return (start == end) ? start.ToString() : start + ":" + end;
		}
	}
}
