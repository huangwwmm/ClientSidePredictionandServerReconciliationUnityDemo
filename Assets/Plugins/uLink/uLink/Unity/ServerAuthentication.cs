// (c)2011 Unity Park. All Rights Reserved.
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// This component can be used for initializing security on the client and server.
	/// </summary>
	[AddComponentMenu("uLink Basics/Server Authentication")]
	public class ServerAuthentication : MonoBehaviour
	{
		/// <summary>
		/// The RSA private key for the server.
		/// </summary>
		[SerializeField]
		public string privateKey = String.Empty;
		
		/// <summary>
		/// The RSA public key for the clients.
		/// </summary>
		[SerializeField]
		public string publicKey = String.Empty;

		/// <summary>
		/// Should the security be automatically initialized by this component?
		/// </summary>
		[SerializeField]
		public bool initializeSecurity = true;

		protected void Awake()
		{
			if (!String.IsNullOrEmpty(privateKey)) Network.privateKey = new PrivateKey(privateKey);
			else if (!String.IsNullOrEmpty(publicKey)) Network.publicKey = new PublicKey(publicKey);
			else return;

			if (initializeSecurity) Network.InitializeSecurity();
		}
	}
}

#endif
