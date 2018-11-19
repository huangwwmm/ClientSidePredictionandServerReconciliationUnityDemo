using System;
using System.Text;
using UnityPark.VersionFormat.Exceptions;

namespace UnityPark.VersionFormat
{
	///<summary>
	/// The most basic representation of version information.
	///</summary>
	public struct VersionParameters : IStringBuildable
	{
		/// <summary>
		/// The name of the product.
		/// </summary>
		public string ProductName;

		/// <summary>
		/// The product description.
		/// </summary>
		public string Description;

		/// <summary>
		/// The product company.
		/// </summary>
		public string Company;

		/// <summary>
		/// The product copyright.
		/// </summary>
		public string Copyright;

		/// <summary>
		/// The compilation configuration.
		/// </summary>
		public string Config;

		/// <summary>
		/// The product release major verison.
		/// </summary>
		public int Major;

		/// <summary>
		/// The product release minor version.
		/// </summary>
		public int Minor;

		/// <summary>
		/// The product release update version.
		/// </summary>
		public int Update;

		/// <summary>
		/// The product release hotfix version.
		/// </summary>
		public int Hotfix;

		/// <summary>
		/// The product release build type.
		/// </summary>
		public VersionBuildType BuildType;

		/// <summary>
		/// The product release build number.
		/// </summary>
		public int BuildNumber;

		/// <summary>
		/// The product release fancy name.
		/// </summary>
		public string FancyName;

		/// <summary>
		/// The product release source flag.
		/// </summary>
		public bool IsSource;

		/// <summary>
		/// The product release version control revision.
		/// </summary>
		public string Revision;

		/// <summary>
		/// The product release build date.
		/// </summary>
		public DateTime Date;

		/// <summary>
		/// Encodes the version data in assembly attribute form.
		/// </summary>
		/// <returns>The version data in assembly attribute form.</returns>
		public VersionAttributes ToAttributes()
		{
			Verify();

			var fileVersion = Major + "." + Minor + "." + Update;
			if (Hotfix != 0)
			{
				fileVersion += "." + Hotfix;
			}
			switch (BuildType)
			{
				case VersionBuildType.Beta:
					fileVersion += "-beta" + BuildNumber;
					break;
				case VersionBuildType.Custom:
					fileVersion += "-custom" + BuildNumber;
					break;
			}
			if (IsSource)
			{
				fileVersion += "-source";
			}

			var infoVersionWithoutRevision = fileVersion;
			if (!string.IsNullOrEmpty(FancyName))
			{
				infoVersionWithoutRevision += " " + FancyName;
			}
			infoVersionWithoutRevision += " " + Date.ToString("(yyyy-MM-dd)");

			var infoVersion = infoVersionWithoutRevision;
			if (string.IsNullOrEmpty(Revision))
			{
				infoVersion += " r(Unknown)";
			}
			else
			{
				infoVersion += " " + Revision;
			}

			return new VersionAttributes
			{
				ProductName = ProductName,
				Description = Description,
				Company = Company,
				Copyright = Copyright,
				Config = Config,
				FileVersion = fileVersion,
				InformationalVersion = infoVersion,
			};
		}

		/// <summary>
		/// Encodes the version data in file name form.
		/// </summary>
		/// <returns>The version data in file name form.</returns>
		public VersionFileName ToFileName()
		{
			Verify();

			var fileName = new VersionFileName();

			fileName.FileName += ProductName + "_" + Major + "." + Minor + "." + Update;
			
			if (Hotfix != 0)
			{
				fileName.FileName += "." + Hotfix;
			}

			switch (BuildType)
			{
				case VersionBuildType.Beta:
					fileName.FileName += "-beta" + BuildNumber;
					break;
				case VersionBuildType.Custom:
					fileName.FileName += "-custom" + BuildNumber;
					break;
			}

			if (IsSource)
			{
				fileName.FileName += "-source";
			}

			if (!string.IsNullOrEmpty(FancyName))
			{
				fileName.FileName += "_" + FancyName.Replace(' ', '_');
			}
			fileName.FileName += "_" + Date.ToString("(yyyy-MM-dd)");

			return fileName;
		}

		private void Verify()
		{
			if (Major < 0)
			{
				throw new VersionFormatException("The Major field of parameters must be zero or greater.");
			}

			if (Minor < 0)
			{
				throw new VersionFormatException("The Minor field of parameters must be zero or greater.");
			}

			if (Update < 0)
			{
				throw new VersionFormatException("The Update field of parameters must be zero or greater.");
			}

			if (Hotfix < 0)
			{
				throw new VersionFormatException("The Hotfix field of parameters must be zero or greater.");
			}

			if (BuildType == VersionBuildType.Beta || BuildType == VersionBuildType.Custom)
			{
				if (BuildNumber < 1)
				{
					throw new VersionFormatException(
						"The BuildNumber field of parameters must be one or greater when BuildType is Beta or Custom.");
				}
			}
			else
			{
				if (BuildNumber != 0)
				{
					throw new VersionFormatException(
						"The BuildNumber field of parameters must be zero when BuildType is neither Beta nor Custom.");
				}
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
			builder.Append("VersionAttributes {");
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
			StringBuildableUtility.BuildFromMember(builder, Major, "Major");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Minor, "Minor");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Update, "Update");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Hotfix, "Hotfix");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, BuildType, "BuildType");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, BuildNumber, "BuildNumber");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, FancyName, "FancyName");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, IsSource, "IsSource");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Revision, "Revision");
			builder.Append(", ");
			StringBuildableUtility.BuildFromMember(builder, Date, "Date");
			builder.Append('}');
		}
	}
}
