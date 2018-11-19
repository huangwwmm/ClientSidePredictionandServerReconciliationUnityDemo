using UnityPark.VersionFormat.Exceptions;

namespace UnityPark.VersionFormat
{
	internal static class VersionParserUtility
	{
		public static void AssertEndOfBuffer(string buffer, int index)
		{
			if (index != buffer.Length)
			{
				throw new VersionFormatException("Expected end of buffer at position " + index);
			}
		}

		public static bool TryAssertEndOfBuffer(string buffer, int index)
		{
			try
			{
				AssertEndOfBuffer(buffer, index);
				return true;
			}
			catch (VersionFormatException)
			{
				return false;
			}
		}

		public static int ReadPositiveInteger(string buffer, int index, out int value)
		{
			value = 0;
			if (string.IsNullOrEmpty(buffer))
			{
				throw new VersionFormatException("Unable to read positive integer: Buffer is empty.");
			}
			var offset = 0;
			while (index + offset < buffer.Length && char.IsDigit(buffer, index + offset))
			{
				value = 10 * value + buffer[index + offset] - '0';
				offset++;
			}
			if (offset == 0)
			{
				throw new VersionFormatException("Unable to read positive integer: No digits at index " + index + ".");
			}
			return offset;
		}

		public static int TryReadPositiveInteger(string buffer, int index, out int value)
		{
			try
			{
				return ReadPositiveInteger(buffer, index, out value);
			}
			catch (VersionFormatException)
			{
				value = default(int);
				return 0;
			}
		}
		
		public static int ReadWord(string buffer, int index, out string value)
		{
			value = null;
			if (string.IsNullOrEmpty(buffer))
			{
				throw new VersionFormatException("Unable to read word: Buffer is empty.");
			}
			var offset = 0;
			while (index + offset < buffer.Length && char.IsLetter(buffer, index + offset))
			{
				value += buffer[index + offset];
				offset++;
			}
			if (offset == 0)
			{
				throw new VersionFormatException("Unable to read word: No letters at index " + index + ".");
			}
			return offset;
		}

		public static int TryReadWord(string buffer, int index, out string value)
		{
			try
			{
				return ReadWord(buffer, index, out value);
			}
			catch (VersionFormatException)
			{
				value = default(string);
				return 0;
			}
		}

		public static int AssertChar(string buffer, int index, char expected)
		{
			if (string.IsNullOrEmpty(buffer))
			{
				throw new VersionFormatException("Unable find char '" + expected + "': Buffer is empty.");
			}
			if (index >= buffer.Length)
			{
				throw new VersionFormatException("Unable find char '" + expected + "': At the end of buffer.");
			}
			var value = buffer[index];
			if (value != expected)
			{
				throw new VersionFormatException("Unable find char '" + expected + "': Found '" + value + "'.");
			}
			return 1;
		}

		public static int TryAssertChar(string buffer, int index, char expected)
		{
			try
			{
				return AssertChar(buffer, index, expected);
			}
			catch (VersionFormatException)
			{
				return 0;
			}
		}
	}
}
