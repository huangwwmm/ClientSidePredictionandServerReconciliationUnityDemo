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
    /// <summary>
    /// Thrown by uLink in error situations.
    /// </summary>
	public class NetworkException : Exception
	{
		public NetworkException(string message)
			: base(message)
		{
		}
	}
}
