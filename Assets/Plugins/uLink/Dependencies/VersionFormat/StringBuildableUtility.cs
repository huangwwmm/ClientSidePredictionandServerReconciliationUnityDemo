using System.Text;

namespace UnityPark.VersionFormat
{
	///<summary>
	/// Utility class for building strings.
	///</summary>
	public static class StringBuildableUtility
	{
		/// <summary>
		/// Helps append a string containing information about a member to a StringBuilder.
		/// </summary>
		/// <param name="builder">A StringBuilder to receive the data.</param>
		/// <param name="value">The value of the member.</param>
		/// <param name="name">The name of the member.</param>
		public static void BuildFromMember(StringBuilder builder, object value, string name)
		{
			builder.Append(name);
			builder.Append(':');
			builder.Append(' ');
			if (value == null)
			{
				builder.Append("null");
			}
			else
			{
				var isString = value.GetType() == typeof(string);
				if (isString) builder.Append('"');
				builder.Append(value);
				if (isString) builder.Append('"');
			}
		}		
	}
}
