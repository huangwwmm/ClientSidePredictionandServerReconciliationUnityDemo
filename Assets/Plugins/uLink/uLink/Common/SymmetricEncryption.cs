#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11845 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:15:43 +0200 (Fri, 13 Apr 2012) $
#endregion
using System;
using System.IO;
using System.Security.Cryptography;
using Lidgren.Network;

namespace uLink
{
	internal class SymmetricEncryption
	{
		private const int KEY_SIZE = 16;
		private const int IV_SIZE = 16;

		private readonly RijndaelManaged _aes;

		public SymmetricEncryption(SymmetricKey symKey)
		{
			_aes = new RijndaelManaged();
			_aes.Key = symKey.key;
			_aes.IV = symKey.iv; // NOTE: this part is really ignored, since we generate a unique one for each message!
		}

		public SymmetricKey GetSymmetricKey()
		{
			return new SymmetricKey(_aes.Key, _aes.IV);
		}

		public NetBuffer Encrypt(NetBuffer inBuf)
		{
			NetworkLog.Debug(NetworkLogFlags.Encryption, "Encrypting message using AES");
			//Log.Debug(NetworkLogFlags.Encryption, "PlainData: ", Convert.ToBase64String(inBuf.Data, 0, inBuf.LengthBytes));

			MemoryStream ms = null;
			CryptoStream cs = null;

			try
			{
				ms = new MemoryStream();
				ms.WriteByte(NetworkMessage.ENCRYPTED_SIGNATURE);

				var iv = new byte[IV_SIZE];
				NetRandom.Instance.NextBytes(iv);
				ms.Write(iv, 0, IV_SIZE);

				var encryptor = _aes.CreateEncryptor(_aes.Key, iv);
				cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

				cs.Write(inBuf.Data, 0, inBuf.LengthBytes);
				cs.FlushFinalBlock();
			}
			catch (Exception ex)
			{
				NetworkLog.Debug(NetworkLogFlags.Encryption, "Failed to encrypt message using AES: ", ex);
				return null;
			}
			finally
			{
				if (cs != null) cs.Close();
				if (ms != null) ms.Close();
			}

			var encrypted = ms.ToArray();
			var outBuf = new NetBuffer(encrypted, 0, encrypted.Length * 8);

			//Log.Debug(NetworkLogFlags.Encryption, "Encrypted: ", Convert.ToBase64String(outBuf.Data, 0, outBuf.LengthBytes));
			return outBuf;

		}

		public NetBuffer Decrypt(NetBuffer inBuf)
		{
			NetworkLog.Debug(NetworkLogFlags.Encryption, "Decrypting message using AES");
			//Log.Debug(NetworkLogFlags.Encryption, "Encrypted: ", Convert.ToBase64String(inBuf.Data, 0, inBuf.LengthBytes));

			var ms = new MemoryStream(inBuf.Data, 1, inBuf.LengthBytes - 1);
			CryptoStream cs = null;

			byte[] decrypted;
			int decryptedCount;

			try
			{
				var iv = new byte[IV_SIZE];
				ms.Read(iv, 0, IV_SIZE);

				var decryptor = _aes.CreateDecryptor(_aes.Key, iv);
				cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

				decrypted = new byte[inBuf.LengthBytes];
				decryptedCount = cs.Read(decrypted, 0, decrypted.Length);
			}
			catch (Exception ex)
			{
				NetworkLog.Debug(NetworkLogFlags.Encryption, "Failed to decrypt message using AES: ", ex);
				return null;
			}
			finally
			{
				if (cs != null) cs.Close();
				ms.Close();
			}

			var outBuf = new NetBuffer(decrypted, 0, decryptedCount * 8);

			//Log.Debug(NetworkLogFlags.Encryption, "Decrypted: ", Convert.ToBase64String(outBuf.Data, 0, outBuf.LengthBytes));
			return outBuf;
		}

		public static byte[] GenerateRandomKey()
		{
			var newKey = new byte[KEY_SIZE + IV_SIZE];
			NetRandom.Instance.NextBytes(newKey);
			return newKey;
		}
	}
}
