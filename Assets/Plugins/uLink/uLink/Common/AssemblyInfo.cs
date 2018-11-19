#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 12119 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-05-17 17:45:33 +0200 (Thu, 17 May 2012) $
#endregion
using System;
using System.Reflection;

namespace uLink
{
	internal static class AssemblyInfo
	{
		public const bool isDebug =
#if DEBUG
			true;
#else
			false;
#endif

		public static Assembly GetAssembly()
		{
			return typeof(AssemblyInfo).Assembly;
		}

		public static AssemblyName GetAssemblyName()
		{
			return GetAssembly().GetName();
		}

		public static string GetName()
		{
			return GetAssemblyName().Name;
		}

		public static Version GetVersion()
		{
			return GetAssemblyName().Version;
		}

		public static string GetFullVersion()
		{
			var info = GetInformationalVersion();
			var version = GetVersion();

			if (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0) return info;

			var name = info;
#pragma warning disable 0162
			if (isDebug) name += " (Debug)";
#pragma warning restore 0162
			if (name.Length > 0 && name[0] != ' ') name = ' ' + name;

			var buildDate = GetBuildDate(version);

			return String.Format("{0}.{1}{2} Build {3:yyyy-MM-dd}", version.Major, version.Minor, name, buildDate);
		}

		public static string GetFullTitle()
		{
			return GetTitle() + " [" + GetFullVersion() + "]";
		}

		public static T GetCustomAttribute<T>() where T : Attribute
		{
			return Attribute.GetCustomAttribute(GetAssembly(), typeof(T)) as T;
		}

		public static string GetTitle()
		{
			var attrib = GetCustomAttribute<AssemblyTitleAttribute>();
			return (attrib != null) ? attrib.Title : String.Empty;
		}

		public static string GetConfiguration()
		{
			var attrib = GetCustomAttribute<AssemblyConfigurationAttribute>();
			return (attrib != null) ? attrib.Configuration : String.Empty;
		}

		public static string GetCopyright()
		{
			var attrib = GetCustomAttribute<AssemblyCopyrightAttribute>();
			return (attrib != null) ? attrib.Copyright : String.Empty;
		}

		public static string GetDescription()
		{
			var attrib = GetCustomAttribute<AssemblyDescriptionAttribute>();
			return (attrib != null) ? attrib.Description : String.Empty;
		}

		public static string GetInformationalVersion()
		{
			var attrib = GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			return (attrib != null) ? attrib.InformationalVersion : String.Empty;
		}

		public static Version GetFileVersion()
		{
			var attrib = GetCustomAttribute<AssemblyFileVersionAttribute>();
			return (attrib != null) ? new Version(attrib.Version) : new Version(0, 0);
		}

		public static DateTime GetBuildDate(Version version)
		{
			var startDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);

			var buildDate = startDate.AddDays(version.Build).AddSeconds(version.Revision * 2);
			//if (TimeZone.IsDaylightSavingTime(buildDate, TimeZone.CurrentTimeZone.GetDaylightChanges(buildDate.Year))) buildDate = buildDate.AddHours(1);

			return buildDate;
		}
	}
}
