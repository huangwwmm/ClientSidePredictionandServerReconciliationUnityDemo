#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10143 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:29:20 +0100 (Tue, 29 Nov 2011) $
#endregion
using Lidgren.Network;

namespace uLink
{
	public enum NetworkSecurityStatus : byte
	{
		Disabled = 0, // no encryption
		Enabling = 1, // recv RSA encrypted
		Enabled = 2, // send & recv AES encrypted
		Disabling = 3, // recv AES encrypted
	}

	internal class SecurityLayer
	{
		private NetworkSecurityStatus _status;
		private readonly AsymmetricEncryption _asymmetric;
		private SymmetricEncryption _symmetric;

		public NetworkSecurityStatus status
		{
			get { return _status; }
		}

		public SecurityLayer(PublicKey publicKey)
		{
			_status = NetworkSecurityStatus.Enabling;

			_asymmetric = new AsymmetricEncryption(publicKey);
			_symmetric = null;
		}

		public SecurityLayer(PrivateKey privateKey)
		{
			_status = NetworkSecurityStatus.Enabling;

			_asymmetric = new AsymmetricEncryption(privateKey ?? PrivateKey.Generate());
			_symmetric = null;
		}

		public void Enable(SymmetricKey symKey)
		{
			_status = NetworkSecurityStatus.Enabled;

			_symmetric = new SymmetricEncryption(symKey);
		}

		public void BeginDisabling()
		{
			_status = NetworkSecurityStatus.Disabling;
		}

		public void Disable()
		{
			_status = NetworkSecurityStatus.Disabled;

			_symmetric = null;
		}

		public PublicKey GetPublicKey()
		{
			return _asymmetric.GetPublicKey();
		}

		public SymmetricKey GetSymmetricKey()
		{
			return _symmetric.GetSymmetricKey();
		}

		public bool ClientEncrypt(ref NetBuffer buffer)
		{
			var encrypted = (_symmetric != null)? _symmetric.Encrypt(buffer) : _asymmetric.Encrypt(buffer);
			if (encrypted == null) return false;

			buffer = encrypted;
			return true;
		}

		public bool ClientDecrypt(ref NetBuffer buffer)
		{
			if (_symmetric == null) return false;

			var decrypted = _symmetric.Decrypt(buffer);
			if (decrypted == null) return false;

			buffer = decrypted;
			return true;
		}

		public bool ServerEncrypt(ref NetBuffer buffer)
		{
			if (_symmetric == null || _status != NetworkSecurityStatus.Enabled) return true;

			var encrypted = _symmetric.Encrypt(buffer);
			if (encrypted == null) return false;

			buffer = encrypted;
			return true;
		}

		public bool ServerDecrypt(ref NetBuffer buffer)
		{
			var decrypted = (_symmetric != null) ? _symmetric.Decrypt(buffer) : _asymmetric.Decrypt(buffer);
			if (decrypted == null) return false;

			buffer = decrypted;
			return true;
		}

		public static byte[] GenerateSymmetricKey()
		{
			return SymmetricEncryption.GenerateRandomKey();
		}

		public static bool IsEncrypted(NetBuffer buffer)
		{
			return (buffer.Data.Length >= 1 && buffer.Data[0] == NetworkMessage.ENCRYPTED_SIGNATURE);
		}
	}
}
