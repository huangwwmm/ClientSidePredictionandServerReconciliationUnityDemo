#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12062 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2012-05-16 10:43:38 +0200 (Wed, 16 May 2012) $
#endregion

namespace uLink
{
	public partial struct NetworkEndPoint
	{
		/// <summary>
		/// Returns the hash code for this <see cref="NetworkEndPoint"/>.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this <see cref="NetworkEndPoint"/>.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		/// <summary>
		/// Returns a formatted string with details on this <see cref="NetworkEndPoint"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> containing a fully qualified type name.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			return "EndPoint " + ToRawString();
		}

		public string ToRawString()
		{
			return value.ToString();
		}
	}
}
