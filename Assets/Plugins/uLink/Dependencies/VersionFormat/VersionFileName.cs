using System;
using System.Text;

namespace UnityPark.VersionFormat
{
	/// <summary>
	/// Contains a representation of version information, encoded in the format specific for file names.
	/// </summary>
	public struct VersionFileName : IStringBuildable
	{
		/// <summary>
		/// The version information encoded into a string on file name form.
		/// </summary>
		public string FileName;

		/// <summary>
		/// Converts the file name representation into a general VersionParameters representation.
		/// </summary>
		/// <returns>A VersionParameters instance containing the encoded information.</returns>
		public VersionParameters ToParameters()
		{
			var parameters = new VersionParameters();

			var buffer = new VersionParserBuffer(FileName, "FileName");

			buffer.ReadProductName(out parameters.ProductName);
			buffer.AssertChar('_');
			buffer.ReadVersionMajor(out parameters.Major);
			buffer.ReadVersionMinor(out parameters.Minor);
			buffer.ReadVersionUpdate(out parameters.Update);
			buffer.TryReadVersionHotfix(out parameters.Hotfix);
			buffer.TryReadBuildType(out parameters.BuildType, out parameters.BuildNumber);
			buffer.TryReadSourceFlag(out parameters.IsSource);
			buffer.TryReadFancyName(out parameters.FancyName, '_');
			buffer.ReadDate(out parameters.Date, '_');
			buffer.AssertEnd();

			return parameters;
		}

		/// <summary>
		/// Returns the fully qualified type name of this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> containing a fully qualified type name.
		/// </returns>
		public override string ToString()
		{
			var builder = new StringBuilder();
			BuildString(builder);
			return builder.ToString();
		}

		/// <summary>
		/// Gets a StringBuilder from the caller and uses it to append string information about the instance.
		/// </summary>
		/// <param name="builder">A StringBuilder instance to append data to.</param>
		public void BuildString(StringBuilder builder)
		{
			builder.Append(GetType().Name + " {");
			StringBuildableUtility.BuildFromMember(builder, FileName, "FileName");
			builder.Append('}');
		}
	}
}
