using System.Text;
using UnityPark.VersionFormat.Exceptions;

namespace UnityPark.VersionFormat
{
	/// <summary>
	/// Contains a representation of version information, encoded in the format specific to assembly attributes.
	/// </summary>
	public struct VersionAttributes : IStringBuildable
	{
		/// <summary>
		/// The content of an AssemblyProductAttribute.
		/// </summary>
		public string ProductName;

		/// <summary>
		/// The content of an AssemblyDescriptionAttribute.
		/// </summary>
		public string Description;

		/// <summary>
		/// The content of an AssemblyCompanyAttribute.
		/// </summary>
		public string Company;

		/// <summary>
		/// The content of an AssemblyCopyrightAttribute.
		/// </summary>
		public string Copyright;

		/// <summary>
		/// The content of an AssemblyConfigAttribute.
		/// </summary>
		public string Config;

		/// <summary>
		/// The content of an AssemblyInformationalVersionAttribute.
		/// </summary>
		public string InformationalVersion;

		/// <summary>
		/// The content of an AssemblyFileVersionAttribute.
		/// </summary>
		public string FileVersion;

		/// <summary>
		/// Converts the assembly attribute representation into a general VersionParameters representation.
		/// </summary>
		/// <returns>A VersionParameters instance containing the encoded information.</returns>
		public VersionParameters ToParameters()
		{
			var parameters = new VersionParameters
			{
				ProductName = ProductName,
				Description = Description,
				Company = Company,
				Copyright = Copyright,
				Config = Config
			};

			var buffer = new VersionParserBuffer(InformationalVersion, "InformationalVersion");

			buffer.ReadVersionMajor(out parameters.Major);
			buffer.ReadVersionMinor(out parameters.Minor);
			buffer.ReadVersionUpdate(out parameters.Update);
			buffer.TryReadVersionHotfix(out parameters.Hotfix);
			buffer.TryReadBuildType(out parameters.BuildType, out parameters.BuildNumber);
			buffer.TryReadSourceFlag(out parameters.IsSource);
			buffer.TryReadFancyName(out parameters.FancyName, ' ');
			buffer.ReadDate(out parameters.Date, ' ');
			buffer.ReadRevision(out parameters.Revision);
			buffer.AssertEnd();

			if (FileVersion != null)
			{
				VerifyMainVersion(FileVersion, parameters);
			}

			return parameters;
		}

		private static void VerifyMainVersion(string fileVersion, VersionParameters parameters)
		{
			var buffer = new VersionParserBuffer(fileVersion, "FileVersion");

			int major;
			buffer.ReadVersionMajor(out major);
			AssertEqual(major, parameters.Major, "Major", buffer.Name);

			int minor;
			buffer.ReadVersionMinor(out minor);
			AssertEqual(minor, parameters.Minor, "Minor", buffer.Name);

			int update;
			buffer.ReadVersionUpdate(out update);
			AssertEqual(update, parameters.Update, "Update", buffer.Name);

			int hotfix;
			buffer.TryReadVersionHotfix(out hotfix);
			AssertEqual(hotfix, parameters.Hotfix, "Hotfix", buffer.Name);

			VersionBuildType buildType;
			int buildNumber;
			buffer.TryReadBuildType(out buildType, out buildNumber);
			AssertEqual(buildType, parameters.BuildType, "BuildType", buffer.Name);
			AssertEqual(buildNumber, parameters.BuildNumber, "BuildNumber", buffer.Name);

			bool isSource;
			buffer.TryReadSourceFlag(out isSource);
			AssertEqual(isSource, parameters.IsSource, "IsSource", buffer.Name);

			buffer.AssertEnd();
		}

		private static void AssertEqual<TValue>(TValue value, TValue expected, string name, string buffer)
		{
			if (!Equals(value, expected))
			{
				throw new VersionFormatException(buffer + " mismatch: " + name + " is '" + value + "' but expected '" + expected + "'");
			}
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
			StringBuildableUtility.BuildFromMember(builder, ProductName, "ProductName");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Description, "Description");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Company, "Company");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Copyright, "Copyright");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Config, "Config");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, InformationalVersion, "InformationalVersion");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, FileVersion, "FileVersion");
			builder.Append('}');
		}
	}
}
