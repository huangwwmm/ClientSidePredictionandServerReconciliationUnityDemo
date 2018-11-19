#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11924 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-04-22 12:47:31 +0200 (Sun, 22 Apr 2012) $
#endregion
using System;

namespace uLink
{
	/// <summary>
	/// Used for network settings in Pikko Server.
	/// </summary>
	[Flags]
	public enum NetworkAuthFlags : byte
	{
		/// <summary>
		/// None of the flags are set.
		/// </summary>
		None,
		/// <summary>
		/// Only available on cell servers; means the automatic handover by the mast algorithm should be disabled.
		/// </summary>
		DontHandoverInPikkoServer = 1 << 0,
	}
}
