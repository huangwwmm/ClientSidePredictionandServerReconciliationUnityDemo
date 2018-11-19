#region COPYRIGHT
// (c)2012 MuchDifferent. All Rights Reserved.
// 
// $Revision: 11529 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-02-22 02:34:38 +0100 (Wed, 22 Feb 2012) $
#endregion
using System;
using System.Text;

namespace uLink
{
	public static partial class NetworkPrefs
	{
		private const string FILE_HEADER = "// Don't remove this auto generated uLink config unless you wish to reset all your network settings. The file may be overwritten at any time.\n\n";

		private static void _ParseConfig(string config)
		{
			if (String.IsNullOrEmpty(config)) return;
			if (config[config.Length - 1] != '\n') config += '\n';

			int i = 0;
			int length = config.Length;

			while (i < length)
			{
				switch (config[i])
				{
					case '\0':
						return;

					case ' ':
					case '\t':
					case '\r':
					case '\n':
						i++;
						break;

					case '/':
						i = _ParseCommentBlock(config, i);
						break;

					case '#':
					case ';':
						i = _ParseCommentLine(config, i);
						break;

					default:
						i = _ParseKeyLine(config, i);
						break;
				}
			}
		}

		private static int _ParseKeyLine(string config, int i)
		{
			int end = config.IndexOfAny(new[] { '=', '\n', '\r' }, i);

			if (config[end] == '=')
			{
				string key = config.Substring(i, end - i).TrimEnd();

				i = end + 1;
				end = config.IndexOfAny(new[] { '\n', '\r' }, end + 1);

				string value = config.Substring(i, end - i).Trim();

				_keys[key] = value;
				return end + 1;

			}

			return end + 1;
		}

		private static int _ParseCommentLine(string config, int i)
		{
			return config.IndexOfAny(new[] { '\n', '\r' }, i + 1) + 1;
		}

		private static int _ParseCommentBlock(string config, int i)
		{
			if (config[i + 1] != '*')
			{
				return _ParseCommentLine(config, i);
			}

			i = config.IndexOf("*/", i + 2);
			return (i != -1) ? i + 2 : config.Length;
		}

		/// <summary>
		/// Returns a network configuration file as a string based on the current configuration.
		/// </summary>
		/// <returns></returns>
		public static string ToConfigString()
		{
			var config = new StringBuilder(FILE_HEADER, _keys.Count * 4 + 1);

			foreach (var pair in _keys)
			{
				config.Append(pair.Key);
				config.Append('=');
				config.Append(pair.Value);
				config.Append('\n');
			}

			return config.ToString();
		}
	}
}
