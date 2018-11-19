using System;
using UnityPark.VersionFormat.Exceptions;

namespace UnityPark.VersionFormat
{
	internal class VersionParserBuffer
	{
		readonly public string Name;
		readonly public string Contents;

		private int _index;

		public VersionParserBuffer(string contents, string name)
		{
			Name = name;
			Contents = contents;
		}

		public override string ToString()
		{
			return Name;
		}

		public void ReadProductName(out string productName)
		{
			var offset = VersionParserUtility.ReadWord(Contents, _index, out productName);
			_index += offset;
		}

		public bool TryReadProductName(out string productName)
		{
			try
			{
				ReadProductName(out productName);
				return true;
			}
			catch (VersionFormatException)
			{
				productName = default(string);
				return false;
			}
		}

		public void ReadVersionMajor(out int major)
		{
			var offset = VersionParserUtility.ReadPositiveInteger(Contents, _index, out major);
			_index += offset;
		}

		public bool TryReadVersionMajor(out int major)
		{
			try
			{
				ReadVersionMajor(out major);
				return true;
			}
			catch (VersionFormatException)
			{
				major = default(int);
				return false;
			}
		}

		public void ReadVersionMinor(out int minor)
		{
			var offset = 0;
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '.');
			offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out minor);
			_index += offset;
		}

		public bool TryReadVersionMinor(out int minor)
		{
			try
			{
				ReadVersionMinor(out minor);
				return true;
			}
			catch (VersionFormatException)
			{
				minor = default(int);
				return false;
			}
		}

		public void ReadVersionUpdate(out int update)
		{
			var offset = 0;
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '.');
			offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out update);
			_index += offset;
		}

		public bool TryReadVersionUpdate(out int update)
		{
			try
			{
				ReadVersionUpdate(out update);
				return true;
			}
			catch (VersionFormatException)
			{
				update = default(int);
				return false;
			}
		}

		public void ReadVersionHotfix(out int hotfix)
		{
			var offset = 0;
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '.');
			offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out hotfix);
			_index += offset;
		}

		public bool TryReadVersionHotfix(out int hotfix)
		{
			try
			{
				ReadVersionHotfix(out hotfix);
				return true;
			}
			catch (VersionFormatException)
			{
				hotfix = default(int);
				return false;
			}
		}

		public void ReadBuildType(out VersionBuildType buildType, out int buildNumber)
		{
			buildType = default(VersionBuildType);
			buildNumber = default(int);
			var offset = 0;

			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '-');

			string buildTypeString;
			offset += VersionParserUtility.ReadWord(Contents, _index + offset, out buildTypeString);

			switch (buildTypeString)
			{
				case "beta":
					buildType = VersionBuildType.Beta;
					break;
				case "custom":
					buildType = VersionBuildType.Custom;
					break;
				default:
					throw new VersionFormatException("Unknown build type: " + buildTypeString);
			}

			offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out buildNumber);

			_index += offset;
		}

		public bool TryReadBuildType(out VersionBuildType buildType, out int buildNumber)
		{
			try
			{
				ReadBuildType(out buildType, out buildNumber);
				return true;
			}
			catch (VersionFormatException)
			{
				buildType = default(VersionBuildType);
				buildNumber = default(int);
				return false;
			}
		}

		public void ReadSourceFlag(out bool isSource)
		{
			isSource = default(bool);
			var offset = 0;

			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '-');

			string sourceFlag;
			offset += VersionParserUtility.ReadWord(Contents, _index + offset, out sourceFlag);

			if (sourceFlag != "source")
			{
				throw new VersionFormatException("Unknown flag: " + sourceFlag);
			}

			isSource = true;
			_index += offset;
		}

		public bool TryReadSourceFlag(out bool isSource)
		{
			try
			{
				ReadSourceFlag(out isSource);
				return true;
			}
			catch (VersionFormatException)
			{
				isSource = default(bool);
				return false;
			}
		}

		public void ReadFancyName(out string fancyName, char separator)
		{
			fancyName = default(string);
			var offset = 0;

			while (VersionParserUtility.TryAssertChar(Contents, _index + offset, separator) > 0)
			{
				var partOffset = 1;
				string fancyNameWord;

				partOffset += VersionParserUtility.TryReadWord(Contents, _index + offset + partOffset, out fancyNameWord);

				if (partOffset == 1)
				{
					_index += offset;
					return;
				}
				offset += partOffset;
				if (fancyName != null)
				{
					fancyName += ' ';
				}
				fancyName += fancyNameWord;
			}
			_index += offset;				
		}

		public bool TryReadFancyName(out string fancyName, char separator)
		{
			try
			{
				ReadFancyName(out fancyName, separator);
				return true;
			}
			catch (VersionFormatException)
			{
				fancyName = default(string);
				return false;
			}
		}

		public void ReadDate(out DateTime date, char separator)
		{
			var offset = 0;
			int year, month, day;

			offset += VersionParserUtility.AssertChar(Contents, _index + offset, separator);
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '(');
			offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out year);
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '-');
			offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out month);
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, '-');
			offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out day);
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, ')');

			date = new DateTime(year, month, day);
			_index += offset;
		}

		public bool TryReadDate(out DateTime date, char separator)
		{
			try
			{
				ReadDate(out date, separator);
				return true;
			}
			catch (VersionFormatException)
			{
				date = default(DateTime);
				return false;
			}
		}

		public void ReadRevision(out string revision)
		{
			revision = default(string);
			var offset = 0;

			offset += VersionParserUtility.AssertChar(Contents, _index + offset, ' ');
			offset += VersionParserUtility.AssertChar(Contents, _index + offset, 'r');

			var offsetParenthesis = VersionParserUtility.TryAssertChar(Contents, _index + offset, '(');
			if (offsetParenthesis > 0)
			{
				offset += offsetParenthesis;

				string revisionFlag;
				offset += VersionParserUtility.ReadWord(Contents, _index + offset, out revisionFlag);

				if (revisionFlag != "Unknown")
				{
					throw new VersionFormatException("Expected flag for unknown revision but found '" + revisionFlag + "'");
				}

				offset += VersionParserUtility.AssertChar(Contents, _index + offset, ')');
			}
			else
			{
				int revisionNumber;
				offset += VersionParserUtility.ReadPositiveInteger(Contents, _index + offset, out revisionNumber);
				revision = "r" + revisionNumber;
				//string revisionData;
				//offset += VersionParserUtility.ReadWord(Contents, _index + offset, out revisionData);
				//revision = "r" + revisionData;
			}
			_index += offset;
		}

		public bool TryReadRevision(out string revision)
		{
			try
			{
				ReadRevision(out revision);
				return true;
			}
			catch (VersionFormatException)
			{
				revision = default(string);
				return false;
			}
		}

		public void ReadPositiveInteger(out int major)
		{
			var offset = VersionParserUtility.ReadPositiveInteger(Contents, _index, out major);
			_index += offset;
		}

		public bool TryReadPositiveInteger(out int value)
		{
			try
			{
				ReadPositiveInteger(out value);
				return true;
			}
			catch (VersionFormatException)
			{
				value = default(int);
				return false;
			}
		}

		public void ReadWord(out string value)
		{
			var offset = VersionParserUtility.ReadWord(Contents, _index, out value);
			_index += offset;
		}

		public bool TryReadWord(out string value)
		{
			try
			{
				ReadWord(out value);
				return true;
			}
			catch (VersionFormatException)
			{
				value = default(string);
				return false;
			}
		}

		public void AssertEnd()
		{
			VersionParserUtility.AssertEndOfBuffer(Contents, _index);
		}

		public bool TryAssertEnd()
		{
			try
			{
				AssertEnd();
				return true;
			}
			catch (VersionFormatException)
			{
				return false;
			}
		}

		public void AssertChar(char expected)
		{
			var offset = VersionParserUtility.AssertChar(Contents, _index, expected);
			_index += offset;
		}

		public bool TryAssertChar(char expected)
		{
			try
			{
				AssertChar(expected);
				return true;
			}
			catch (VersionFormatException)
			{
				return false;
			}
		}
	}
}
