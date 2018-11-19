#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion

using System;
using System.Net;

namespace uLink
{
	public partial struct NetworkEndPoint
	{
		public Type type { get { return value.GetType(); } }

		public new Type GetType()
		{
			return type;
		}

		public bool IsType<TEndPoint>()
			where TEndPoint : EndPoint
		{
			return value is TEndPoint;
		}
	}
}
