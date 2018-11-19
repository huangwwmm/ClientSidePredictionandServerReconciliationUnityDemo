// (c)2011 Unity Park. All Rights Reserved.
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// This component can be used for entering the uLink license key in your game server.
	/// The key should not be known in clients and should not be in any of the executables which clients have access to, 
	/// no matter if it's a windows exe, android apk or a web player .unity3d file.
	/// </summary>
	/// <remarks>
	/// If you are using a trial version, you need a license key for releasing your game and you should buy the license from MuchDifferent.
	/// </remarks>
	[AddComponentMenu("uLink Basics/Server License Key")]
	public class EnterLicenseKey : MonoBehaviour
	{
		/// <summary>
		/// The License key that you want to use for your game server.
		/// </summary>
		[SerializeField]
		public string licenseKey = String.Empty;

		protected void Awake()
		{
			if (!String.IsNullOrEmpty(licenseKey)) Network.licenseKey = licenseKey;
		}
	}
}

#endif
