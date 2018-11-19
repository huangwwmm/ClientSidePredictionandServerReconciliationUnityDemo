#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11845 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:15:43 +0200 (Fri, 13 Apr 2012) $
#endregion
using System;
using System.Security.Cryptography;
using Lidgren.Network;

namespace uLink
{
	internal class AsymmetricEncryption
	{
		private const int DEFAULT_KEY_SIZE = 1024;
		private readonly RSACryptoServiceProvider _rsa;

		public AsymmetricEncryption(PublicKey publicKey)
		{
			var prm = new RSAParameters();

			prm.Modulus = publicKey.modulus;
			prm.Exponent = publicKey.exponent;

			_rsa = new RSACryptoServiceProvider();
			_rsa.ImportParameters(prm);
		}

		public AsymmetricEncryption(PrivateKey privateKey)
		{
			var prm = new RSAParameters();

			prm.Modulus = privateKey.modulus;
			prm.Exponent = privateKey.exponent;
			prm.P = privateKey.p;
			prm.Q = privateKey.q;
			prm.DP = privateKey.dp;
			prm.DQ = privateKey.dq;
			prm.InverseQ = privateKey.inverseQ;
			prm.D = privateKey.d;

			_rsa = new RSACryptoServiceProvider();
			_rsa.ImportParameters(prm);
		}

		public NetBuffer Encrypt(NetBuffer inBuf)
		{
			NetworkLog.Debug(NetworkLogFlags.Encryption, "Encrypting message using RSA");
			//Log.Debug(NetworkLogFlags.Encryption, "PlainData: ", Convert.ToBase64String(inBuf.Data, 0, inBuf.LengthBytes));

			byte[] unencrypted = inBuf.Data.SubArray(0, inBuf.LengthBytes);
			byte[] encrypted;

			try
			{
				encrypted = _rsa.Encrypt(unencrypted, false);
			}
			catch (Exception ex)
			{
				NetworkLog.Debug(NetworkLogFlags.Encryption, "Failed to encrypt message using RSA: ", ex);
				return null;
			}

			ArrayUtility.Push(ref encrypted, NetworkMessage.ENCRYPTED_SIGNATURE);

			var outBuf = new NetBuffer();
			outBuf.Data = encrypted;
			outBuf.LengthBytes = encrypted.Length;

			//Log.Debug(NetworkLogFlags.Encryption, "Encrypted: ", Convert.ToBase64String(outBuf.Data, 0, outBuf.LengthBytes));
			return outBuf;
		}

		public NetBuffer Decrypt(NetBuffer inBuf)
		{
			NetworkLog.Debug(NetworkLogFlags.Encryption, "Decrypting message using RSA");
			//Log.Debug(NetworkLogFlags.Encryption, "Encrypted: ", Convert.ToBase64String(inBuf.Data, 0, inBuf.LengthBytes));

			byte[] encrypted = inBuf.Data.SubArray(1, inBuf.LengthBytes - 1);
			byte[] decrypted;

			try
			{
				decrypted = _rsa.Decrypt(encrypted, false);
			}
			catch (Exception ex)
			{
				NetworkLog.Debug(NetworkLogFlags.Encryption, "Failed to decrypt message using RSA: ", ex);
				return null;
			}

			var outBuf = new NetBuffer();
			outBuf.Data = decrypted;
			outBuf.LengthBytes = decrypted.Length;

			//Log.Debug(NetworkLogFlags.Encryption, "Decrypted: ", Convert.ToBase64String(outBuf.Data, 0, outBuf.LengthBytes));
			return outBuf;
		}

		public PublicKey GetPublicKey()
		{
			var prm = _rsa.ExportParameters(false);
			return new PublicKey(prm.Modulus, prm.Exponent);
		}
	}
}
