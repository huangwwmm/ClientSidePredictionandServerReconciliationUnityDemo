#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion

using System;
using System.Text;
using System.Security.Cryptography;

namespace uLink
{
	public class PublicKey
	{
		public readonly byte[] modulus, exponent;

		public PublicKey(string xmlText)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(xmlText);

			var parameters = rsa.ExportParameters(false);
			modulus = parameters.Modulus;
			exponent = parameters.Exponent;
		}

		public PublicKey(string modulusInBase64, string exponentInBase64)
		{
			modulus = KeyUtility.GetBytes(modulusInBase64);
			exponent = KeyUtility.GetBytes(exponentInBase64);
		}

		public PublicKey(byte[] modulus, byte[] exponent)
		{
			this.modulus = modulus;
			this.exponent = exponent;
		}

		public string ToXmlString()
		{
			return "<RSAKeyValue><Modulus>"
				+ Convert.ToBase64String(modulus)
				+ "</Modulus><Exponent>"
				+ Convert.ToBase64String(exponent)
				+ "</Exponent></RSAKeyValue>";
		}

		public override string ToString()
		{
			return ToXmlString();
		}

		public override bool Equals(object other)
		{
			return Equals(other as PublicKey);
		}

		public bool Equals(PublicKey other)
		{
			if (other == null) return false;

			if (!KeyUtility.IsEqual(modulus, other.modulus)) return false;
			if (!KeyUtility.IsEqual(exponent, other.exponent)) return false;

			return true;
		}

		public override int GetHashCode()
		{
			return modulus.GetHashCode() ^ exponent.GetHashCode();
		}
	}

	public class PrivateKey : PublicKey
	{
		public readonly byte[] p, q, dp, dq, inverseQ, d;

		public PrivateKey(string xmlText)
			: base(xmlText)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(xmlText);

			var parameters = rsa.ExportParameters(true);
			p = parameters.P;
			q = parameters.Q;
			dp = parameters.DP;
			inverseQ = parameters.InverseQ;
			d = parameters.D;
		}

		public PrivateKey(
			string modulusInBase64, string exponentInBase64,
			string pInBase64, string qInBase64,
			string dPInBase64, string dQInBase64,
			string inverseQInBase64, string dInBase64)
			: base(modulusInBase64, exponentInBase64)
		{
			p = KeyUtility.GetBytes(pInBase64);
			q = KeyUtility.GetBytes(qInBase64);
			dp = KeyUtility.GetBytes(dPInBase64);
			dq = KeyUtility.GetBytes(dQInBase64);
			inverseQ = KeyUtility.GetBytes(inverseQInBase64);
			d = KeyUtility.GetBytes(dInBase64);
		}

		public PrivateKey(byte[] modulus, byte[] exponent, byte[] p, byte[] q, byte[] dp, byte[] dq, byte[] inverseQ, byte[] d)
			: base(modulus, exponent)
		{
			this.p = p;
			this.q = q;
			this.dp = dp;
			this.dq = dq;
			this.inverseQ = inverseQ;
			this.d = d;
		}

		public PublicKey GetPublicKey()
		{
			return new PublicKey(modulus, exponent);
		}

		public new string ToXmlString()
		{
			return "<RSAKeyValue><Modulus>"
				+ Convert.ToBase64String(modulus)
				+ "</Modulus><Exponent>"
				+ Convert.ToBase64String(exponent)
				+ "</Exponent><P>"
				+ Convert.ToBase64String(p)
				+ "</P><Q>"
				+ Convert.ToBase64String(q)
				+ "</Q><DP>"
				+ Convert.ToBase64String(dp)
				+ "</DP><DQ>"
				+ Convert.ToBase64String(dq)
				+ "</DQ><InverseQ>"
				+ Convert.ToBase64String(inverseQ)
				+ "</InverseQ><D>"
				+ Convert.ToBase64String(d)
				+ "</D></RSAKeyValue>";
		}

		public override string ToString()
		{
			return ToXmlString();
		}

		public override bool Equals(object other)
		{
			return Equals(other as PrivateKey);
		}

		public bool Equals(PrivateKey other)
		{
			if (!base.Equals(other)) return false;

			if (!KeyUtility.IsEqual(p, other.p)) return false;
			if (!KeyUtility.IsEqual(q, other.q)) return false;
			if (!KeyUtility.IsEqual(dp, other.dp)) return false;
			if (!KeyUtility.IsEqual(dq, other.dq)) return false;
			if (!KeyUtility.IsEqual(inverseQ, other.inverseQ)) return false;
			if (!KeyUtility.IsEqual(d, other.d)) return false;

			return true;
		}

		public override int GetHashCode()
		{
			return modulus.GetHashCode() ^ exponent.GetHashCode() ^ p.GetHashCode() ^ q.GetHashCode() ^ dp.GetHashCode() ^ dq.GetHashCode() ^ inverseQ.GetHashCode() ^ d.GetHashCode();
		}

		private const int DEFAULT_BIT_STRENGTH = 1024; // should be changed to 2048, 3072 or higher some time in the future

		public static PrivateKey Generate()
		{
			return Generate(DEFAULT_BIT_STRENGTH);
		}

		public static PrivateKey Generate(int bitStrength)
		{
			var rsa = new RSACryptoServiceProvider(bitStrength);
			var prm = rsa.ExportParameters(true);

			return new PrivateKey(prm.Modulus, prm.Exponent, prm.P, prm.Q, prm.DP, prm.DQ, prm.InverseQ, prm.D);
		}
	}

	internal class SymmetricKey
	{
		public readonly byte[] key, iv;

		public SymmetricKey(byte[] key, byte[] iv)
		{
			this.key = key;
			this.iv = iv;
		}

		public override bool Equals(object other)
		{
			return Equals(other as SymmetricKey);
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public bool Equals(SymmetricKey other)
		{
			if (other == null) return false;

			if (!KeyUtility.IsEqual(key, other.key)) return false;
			if (!KeyUtility.IsEqual(iv, other.iv)) return false;

			return true;
		}

		public const int DEFAULT_BIT_STRENGTH = 128; // should be changed to 192, 256 or higher some time in the future

		public static SymmetricKey Generate()
		{
			return Generate(DEFAULT_BIT_STRENGTH);
		}

		public static SymmetricKey Generate(int bitStrength)
		{
			var random = new RNGCryptoServiceProvider();

			var key = new byte[bitStrength / 8];
			random.GetBytes(key);

			var iv = new byte[16];
			random.GetBytes(iv);

			return new SymmetricKey(key, iv);
		}
	}

	internal static class KeyUtility
	{
		public static bool IsEqual(byte[] a, byte[] b)
		{
			if (a.Length != b.Length) return false;

			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i]) return false;
			}

			return true;
		}

		public static byte[] GetBytes(string base64)
		{
			StringBuilder sb = new StringBuilder();

			foreach (char c in base64)
			{
				if (!Char.IsWhiteSpace(c) && " \t\n\r".IndexOf(c) == -1) sb.Append(c);
			}

			return Convert.FromBase64String(sb.ToString());
		}
	}
}
