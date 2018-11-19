#region COPYRIGHT
// (c)2011 MuchDifferent. All Rights Reserved.
// 
// $Revision: 12128 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-05-18 14:54:19 +0200 (Fri, 18 May 2012) $
#endregion
using System;
using System.Text;

namespace uLink
{
	/// <summary>
	/// This enum shows what version of uLink build you have.
	/// </summary>
	public enum NetworkVersionBuild : byte
	{
		/// <summary>
		/// It's an Alpha build. It's not stable.
		/// </summary>
		Alpha,
		/// <summary>
		/// This is a Beta and more stable than Alpha. This is waht we use before stable release.
		/// </summary>
		Beta,
		/// <summary>
		/// This is an stable released version. Good for production.
		/// </summary>
		Stable,
		/// <summary>
		/// This is a custom build prepared for you. This might have a quick hot fix, a specific feature or ...
		/// </summary>
		Custom,
	}

	/// <summary>
	/// The version of uLink.
	/// </summary>
	public struct NetworkVersion : IComparable<NetworkVersion>
	{
		public static readonly NetworkVersion unavailable = new NetworkVersion(0, 0, -1, -1, NetworkVersionBuild.Alpha, 0, String.Empty, DateTime.MinValue);
#if DRAGONSCALE || NO_CRAP_DEPENDENCIES
		public static readonly NetworkVersion current = unavailable; //had problems getting  code to see this for some reason
#else
		public static readonly NetworkVersion current = new NetworkVersion(AssemblyInfo.GetInformationalVersion(), InputFormat.InformationalVersion);
#endif

		public readonly int major;
		public readonly int minor;
		public readonly int patch;
		public readonly int hotfix;

		public readonly NetworkVersionBuild build;
		public readonly int revision;

		public readonly string name;
		public readonly DateTime date;

		public bool isAlphaBuild { get { return build == NetworkVersionBuild.Alpha; } }
		public bool isBetaBuild { get { return build == NetworkVersionBuild.Beta; } }
		public bool isCustomBuild { get { return build == NetworkVersionBuild.Custom; } }

		public NetworkVersion(int major, int minor, int patch, int hotfix, NetworkVersionBuild build, int revision, string name, DateTime date)
		{
			this.major = major;
			this.minor = minor;
			this.patch = patch;
			this.hotfix = hotfix;
			this.build = build;
			this.revision = revision;
			this.name = name;
			this.date = date;
		}

		public enum InputFormat
		{
			InformationalVersion,
			FileName,
		}

#if !NO_CRAP_DEPENDENCIES
		public NetworkVersion(string versionString, InputFormat format)
		{
			this = unavailable;

			if (String.IsNullOrEmpty(versionString)) return;

			var parameters = ParseVersionString(versionString, format);
			var converedBuildType = ConvertBuildType(parameters.BuildType);

			major = parameters.Major;
			minor = parameters.Minor;
			patch = parameters.Update;
			hotfix = parameters.Hotfix;
			build = converedBuildType;
			revision = parameters.BuildNumber;
			name = parameters.FancyName;
			date = parameters.Date;
		}

		private static UnityPark.VersionFormat.VersionParameters ParseVersionString(string version, InputFormat format)
		{
			switch (format)
			{
				case InputFormat.InformationalVersion:
					var attributes = new UnityPark.VersionFormat.VersionAttributes();
					attributes.InformationalVersion = version;
					return attributes.ToParameters();
				case InputFormat.FileName:
					var fileName = new UnityPark.VersionFormat.VersionFileName();
					fileName.FileName = version;
					return fileName.ToParameters();
				default:
					throw new NetworkException("Unrecognized input format: " + format);
			}
		}

		private static NetworkVersionBuild ConvertBuildType(UnityPark.VersionFormat.VersionBuildType buildType)
		{
			switch (buildType)
			{
				case UnityPark.VersionFormat.VersionBuildType.Stable:
					return NetworkVersionBuild.Stable;
				case UnityPark.VersionFormat.VersionBuildType.Beta:
					return NetworkVersionBuild.Beta;
				case UnityPark.VersionFormat.VersionBuildType.Custom:
					return NetworkVersionBuild.Custom;
				default:
					return NetworkVersionBuild.Stable;
			}
		}
#endif

		public static bool operator ==(NetworkVersion lhs, NetworkVersion rhs) { return lhs.Equals(rhs); }
		public static bool operator !=(NetworkVersion lhs, NetworkVersion rhs) { return !lhs.Equals(rhs); }
		public static bool operator >=(NetworkVersion lhs, NetworkVersion rhs) { return lhs.CompareTo(rhs) >= 0; }
		public static bool operator <=(NetworkVersion lhs, NetworkVersion rhs) { return lhs.CompareTo(rhs) <= 0; }
		public static bool operator >(NetworkVersion lhs, NetworkVersion rhs) { return lhs.CompareTo(rhs) > 0; }
		public static bool operator <(NetworkVersion lhs, NetworkVersion rhs) { return lhs.CompareTo(rhs) < 0; }

		public override int GetHashCode() { return date.GetHashCode(); }

		public override bool Equals(object other)
		{
			return (other is NetworkVersion) && Equals((NetworkVersion)other);
		}

		public bool Equals(NetworkVersion other)
		{
			return major == other.major
				&& minor == other.minor
				&& patch == other.patch
				&& hotfix == other.hotfix
				&& build == other.build
				&& revision == other.revision
				&& name == other.name
				&& date.Equals(other.date);
		}

		public int CompareTo(NetworkVersion other)
		{
			int majorDiff = major.CompareTo(other.major);
			if (majorDiff != 0) return majorDiff;

			int minorDiff = minor.CompareTo(other.minor);
			if (minorDiff != 0) return minorDiff;

			int patchDiff = patch.CompareTo(other.patch);
			if (patchDiff != 0) return patchDiff;

			int hotfixDiff = patch.CompareTo(other.hotfix);
			if (hotfixDiff != 0) return hotfixDiff;

			int buildDiff = ((int)build).CompareTo((int)other.build);
			if (buildDiff != 0) return buildDiff;

			int revisionDiff = revision.CompareTo(other.revision);
			if (revisionDiff != 0) return revisionDiff;

			return date.CompareTo(other.date);
		}

		public override string ToString()
		{
			if (Equals(unavailable)) return "Unavailable";

			var sb = new StringBuilder();
			sb.Append(major);
			sb.Append('.');
			sb.Append(minor);

			if (patch >= 0)
			{
				sb.Append('.');
				sb.Append(patch);

				if (hotfix > 0)
				{
					sb.Append('.');
					sb.Append(hotfix);
				}
			}

			string buildStr = BuildToString(build);
			if (revision > 0 && !String.IsNullOrEmpty(buildStr))
			{
				sb.Append(buildStr);
				sb.Append(revision);
			}

			if (!String.IsNullOrEmpty(name))
			{
				sb.Append(' ');
				sb.Append(name);
			}

			if (!date.Equals(unavailable.date))
			{
				sb.Append(" (");
				sb.Append(date.ToString("yyyy-MM-dd"));
				sb.Append(')');
			}

			return sb.ToString();
		}

		private static string BuildToString(NetworkVersionBuild build)
		{
			switch (build)
			{
				case NetworkVersionBuild.Alpha:
					return "-alpha";

				case NetworkVersionBuild.Beta:
					return "-beta";

				case NetworkVersionBuild.Custom:
					return "-custom";

				default:
					return null;
			}
		}
	}
}
