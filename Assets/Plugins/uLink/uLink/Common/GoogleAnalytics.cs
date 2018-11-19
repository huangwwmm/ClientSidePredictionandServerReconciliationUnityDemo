// (c)2012 Unity Park. All Rights Reserved.
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;
using System.Text;
using Random = System.Random;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	internal static class GoogleAnalytics
	{
#if UNITY_BUILD
		private const string _TRACKING_ID = "UA-3642534-15";

		public static WWW TrackEvent(string category, string action)
		{
			return TrackEvent(category, action, null, -1);
		}

		public static WWW TrackEvent(string category, string action, string label)
		{
			return TrackEvent(category, action, label, -1);
		}

		public static WWW TrackEvent(string category, string action, string label, int value)
		{
			return NoEscapeTrackEvent(WWW.EscapeURL(category), WWW.EscapeURL(action), WWW.EscapeURL(label), value);
		}

		public static WWW NoEscapeTrackEvent(string category, string action)
		{
			return NoEscapeTrackEvent(category, action, null, -1);
		}

		public static WWW NoEscapeTrackEvent(string category, string action, string label)
		{
			return NoEscapeTrackEvent(category, action, action, -1);
		}

		public static WWW NoEscapeTrackEvent(string category, string action, string label, int value)
		{
			string url = _GetRequestURL(category, action, label, value);

			try
			{
				return new WWW(url);
			}
			catch
			{
				return null;
			}
		}

		private static string _GetRequestURL(string category, string action, string label, int value)
		{
			string baseURL = UnityEngine.Application.webSecurityEnabled ?
				"https://download-muchdifferent-com.loopiasecure.com/google_analytics/forward.php" :
				"http://www.google-analytics.com/__utm.gif";

			var url = new StringBuilder(baseURL, 22); // inital capacity for string concatenation

			url.Append(
				"?utmwv=4.8.1ma" + // analytics version
				"&utmt=event" + // type of request
				"&utmcs=UTF-8" + // document encoding
				"&utmac=" + _TRACKING_ID // account code
			);

			// random request number
			url.Append("&utmn=");
			url.Append(_GetRandomNumber());

			// user language
			url.Append("&utmul=");
			url.Append(_GetLanguageCode());

			// cookie string (escape encoded)
			long timestamp = _GetUnixTimestamp();
			url.Append("&utmcc=__utma%3D1.");
			url.Append(_GetUniqueIdentifier());
			url.Append(".");
			url.Append(timestamp);
			url.Append(".");
			url.Append(timestamp);
			url.Append(".");
			url.Append(timestamp);
			url.Append(".2%3B");

			// event parameters
			url.Append("&utme=5(");
			url.Append(category);
			url.Append("*");
			url.Append(action);
			if (!String.IsNullOrEmpty(label))
			{
				url.Append("*");
				url.Append(label);
			}
			if (value > -1)
			{
				url.Append(")(");
				url.Append(value);
			}
			url.Append(")");

			return url.ToString();
		}

		private static int _GetRandomNumber()
		{
			return _random.Next(0x7fffffff);
		}

		private static string _GetLanguageCode()
		{
			if (_languageCode == null)
			{
				int code;

				try
				{
					code = (int)UnityEngine.Application.systemLanguage;
				}
				catch
				{
					_languageCode = "-";
					return _languageCode;
				}

				if (!_languageCodes.TryGetValue(code, out _languageCode))
				{
					_languageCode = "-";
				}
			}

			return _languageCode;
		}

		private static string _GetUniqueIdentifier()
		{
			if (_uniqueIdentifier == null)
			{
				const string key = "uLink.UniqueIdentifier";

				_uniqueIdentifier = SafePlayerPrefs.TryGetString(key, null);
				if (String.IsNullOrEmpty(_uniqueIdentifier))
				{
					_uniqueIdentifier = _GetRandomNumber().ToString();
					SafePlayerPrefs.TrySetString(key, _uniqueIdentifier);
				}
			}

			return _uniqueIdentifier;
		}

		private static long _GetUnixTimestamp()
		{
			TimeSpan span = DateTime.Now - _epochLocalTime;
			return span.Ticks / 10000000L;
		}

		private static string _uniqueIdentifier;
		private static string _languageCode;

		private static readonly Random _random = new Random();
		private static readonly DateTime _epochLocalTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
		private static readonly Dictionary<int, string> _languageCodes = new Dictionary<int, string>
		{
			{ (int)SystemLanguage.Afrikaans,		"af" },
			{ (int)SystemLanguage.Arabic,			"ar" },
			{ (int)SystemLanguage.Basque,			"eu" },
			{ (int)SystemLanguage.Belarusian,		"be" },
			{ (int)SystemLanguage.Bulgarian,		"bg" },
			{ (int)SystemLanguage.Catalan,			"ca" },
			{ (int)SystemLanguage.Chinese,			"zh" },
			{ (int)SystemLanguage.Czech,			"cs" },
			{ (int)SystemLanguage.Danish,			"da" },
			{ (int)SystemLanguage.Dutch,			"nl" },
			{ (int)SystemLanguage.English,			"en" },
			{ (int)SystemLanguage.Estonian,			"et" },
			{ (int)SystemLanguage.Faroese,			"fo" },
			{ (int)SystemLanguage.Finnish,			"fi" },
			{ (int)SystemLanguage.French,			"fr" },
			{ (int)SystemLanguage.German,			"de" },
			{ (int)SystemLanguage.Greek,			"el" },
			{ (int)SystemLanguage.Hebrew,			"he" },
			{ (int)SystemLanguage.Hungarian,		"hu" },
			{ (int)SystemLanguage.Icelandic,		"is" },
			{ (int)SystemLanguage.Indonesian,		"id" },
			{ (int)SystemLanguage.Italian,			"it" },
			{ (int)SystemLanguage.Japanese,			"ja" },
			{ (int)SystemLanguage.Korean,			"ko" },
			{ (int)SystemLanguage.Latvian,			"lv" },
			{ (int)SystemLanguage.Lithuanian,		"lt" },
			{ (int)SystemLanguage.Norwegian,		"no" },
			{ (int)SystemLanguage.Polish,			"pl" },
			{ (int)SystemLanguage.Portuguese,		"pt" },
			{ (int)SystemLanguage.Romanian,			"ro" },
			{ (int)SystemLanguage.Russian,			"ru" },
			{ (int)SystemLanguage.SerboCroatian,	"sr" },
			{ (int)SystemLanguage.Slovak,			"sk" },
			{ (int)SystemLanguage.Slovenian,		"sl" },
			{ (int)SystemLanguage.Spanish,			"es" },
			{ (int)SystemLanguage.Swedish,			"sv" },
			{ (int)SystemLanguage.Thai,				"th" },
			{ (int)SystemLanguage.Turkish,			"tr" },
			{ (int)SystemLanguage.Ukrainian,		"uk" },
			{ (int)SystemLanguage.Vietnamese,		"vi" }
		};
#else
		public static void TrackEvent(string category, string action) {}
		public static void TrackEvent(string category, string action, string label) {}
		public static void TrackEvent(string category, string action, string label, int value) {}
		public static void NoEscapeTrackEvent(string category, string action) {}
		public static void NoEscapeTrackEvent(string category, string action, string label) {}
		public static void NoEscapeTrackEvent(string category, string action, string label, int value) {}
#endif
	}
}
