#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8656 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-22 05:36:17 +0200 (Mon, 22 Aug 2011) $
#endregion

using System;

namespace uLink
{
	/// <summary>
	/// Provides the RLE codec.
	/// </summary>
	internal static class RunLengthEncoding
	{
		private const byte rleMarker = Byte.MaxValue;

		private const byte maxLength = Byte.MaxValue - 1;

		/// <summary>
		/// RLE-Encodes a data set.
		/// </summary>
		public static int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputCapacity)
		{
			if (input == null | inputLength <= 3) return 0;

			int runLength = 1;
			var runValue = input[inputOffset];
			int outputLength = 0;

			int iLen = inputOffset + inputLength;
			for (int i = inputOffset + 1; i < iLen; i++)
			{
				var currentValue = input[i];

				// if the current value is the value of the current run, don't yield anything, 
				// just extend the run
				if (currentValue == runValue & runLength != maxLength)
				{
					runLength++;
				}
				else
				{
					// the current value is different from the current run
					// yield what we have so far
					int encodeCount = _EncodeRun(runLength, runValue, output, outputOffset + outputLength, outputCapacity - outputLength);
					if (encodeCount == 0) return 0;
					outputLength += encodeCount;

					// and reset the run
					runValue = currentValue;
					runLength = 1;
				}
			}

			int lastEncodeCount = _EncodeRun(runLength, runValue, output, outputOffset + outputLength, outputCapacity - outputLength);
			return lastEncodeCount != 0 ? outputLength + lastEncodeCount : 0;
		}

		private static int _EncodeRun(int runLength, byte runValue, byte[] output, int outputCurrentOffset, int outputCapacityLeft)
		{
			if ((runLength <= 3 & runValue != rleMarker) | runLength == 1)
			{
				if (runValue == rleMarker)
				{
					runLength++;
				}

				if (outputCapacityLeft < runLength)
				{
					return 0;
				}

				//don't compress this run, it is just too small
				int jLen = outputCurrentOffset + runLength;
				for (int j = outputCurrentOffset; j < jLen; j++)
				{
					output[j] = runValue;
				}

				return runLength;
			}

			if (outputCapacityLeft < 3)
			{
				return 0;
			}

			//compressed run
			output[outputCurrentOffset] = rleMarker;
			output[outputCurrentOffset + 1] = (byte)runLength;
			output[outputCurrentOffset + 2] = runValue;
			return 3;
		}

		/// <summary>
		/// Decodes RLE-encoded data
		/// </summary>
		public static int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputCapacity)
		{
			int outputLength = 0;

			int iLen = inputOffset + inputLength;
			for (int i = inputOffset; i < iLen; i++)
			{
				var currentValue = input[i];
				if (currentValue == rleMarker)
				{
					i++;
					if (i >= iLen)
					{
						return 0;
					}

					int runLength = input[i];
					byte runValue;

					if (runLength != rleMarker)
					{
						i++;
						if (i >= iLen)
						{
							return 0;
						}

						runValue = input[i];
					}
					else
					{
						runLength = 1;
						runValue = rleMarker;
					}

					if (outputCapacity < outputLength + runLength)
					{
						return 0;
					}

					int jLen = outputOffset + outputLength + runLength;
					for (int j = outputOffset + outputLength; j < jLen; j++)
					{
						output[j] = runValue;
					}

					outputLength += runLength;
				}
				else
				{
					if (outputCapacity < outputLength + 1)
					{
						return 0;
					}

					output[outputLength] = currentValue;
					outputLength++;
				}
			}

			return outputLength;
		}
	}

}
