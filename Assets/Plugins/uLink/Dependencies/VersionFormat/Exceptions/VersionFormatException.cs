using System;

namespace UnityPark.VersionFormat.Exceptions
{
	/// <summary>
	/// Identifies problems specific to the parsing and generation of version information.
	/// </summary>
	public class VersionFormatException : Exception
	{
		/// <summary>
		/// Creates a new instance of VersionFormatException.
		/// </summary>
		/// <param name="message">A message describing the error.</param>
		/// <param name="innerException">An optional inner exception.</param>
		public VersionFormatException(string message, Exception innerException = null) : base(message, innerException) {}
	}
}
