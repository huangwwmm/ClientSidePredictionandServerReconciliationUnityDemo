#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10143 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:29:20 +0100 (Tue, 29 Nov 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using Lidgren.Network;

#if UNITY_BUILD
using UnityEngine;
#endif

namespace uLink
{
	/// <summary>
	/// This class represents a serialized asset bundle which you can send over the network.
	/// This is used internally in uLink but also made public because its functionality can be useful for users as well.
	/// </summary>
	public class SerializedAssetBundle
	{
		private byte[] _bytes = null;

#if UNITY_BUILD
		/// <summary>
		/// The WWW instance which is downloading our asset bundle.
		/// </summary>
		public readonly WWW download = null;

		private AssetBundleCreateRequest _request = null;

		/// <summary>
		/// The asset bundle that this class holds.
		/// </summary>
		public AssetBundle assetBundle
		{
			get
			{
				return createRequest.assetBundle;
			}
		}

		/// <summary>
		/// Tries to create a request for creating an asset bundle from the bytes stored in this class on memory.
		/// The bytes can be downloaded or sent by you based on the constructor you use to create an instance
		/// of this class.
		/// </summary>
		public AssetBundleCreateRequest createRequest
		{
			get
			{
				if(!(bytes == null)){Utility.Exception( "Can't create AssetBundle because serialized bytes are null");}

#if UNITY_4
				return _request ?? (_request = AssetBundle.CreateFromMemory(bytes));
#else
				return _request ?? (_request = AssetBundle.LoadFromMemoryAsync(bytes));
#endif
			}
		}

		/// <summary>
		/// Returns the asset bundle's raw bytes stored in this instance.
		/// </summary>
		public byte[] bytes
		{
			get
			{
				if (_bytes != null) return _bytes;
				if (download == null) return null;

				if(!(download.isDone)){Utility.Exception( "The download is not done. Please call SerializedAssetBundle.WaitForDownload()");}

				return (_bytes = download.bytes);
			}
		}

		/// <summary>
		/// Creates an instance of SerializedAssetBundle class and tries to download an asset bundle from the
		/// provided URL.
		/// </summary>
		/// <param name="url">The url to download the asset bundle from</param>
		/// <remarks>
		/// This overload of the constructor simply instantiates a WWW instance so it doesn't use cache.
		/// If you want to use caching then use <see cref="SerializedAssetBundle(UnityEngine.WWWW,System.Int32)"/>
		/// </remarks>
		public SerializedAssetBundle(string url)
			: this(new WWW(url))
		{
		}

		/// <summary>
		/// Creates an instance of SerializedAssetBundle and tries to download or get the asset bundle
		/// from the cache.
		/// </summary>
		/// <param name="url">The url which the asset bundle is stored in.</param>
		/// <param name="version">Version of the asset bundle.</param>
		/// <remarks>
		/// Read Unity's documentation on caching of asset bundles and WWW.LoadFromCacheOrDownload method.
		/// </remarks>
		public SerializedAssetBundle(string url, int version)
			: this(WWW.LoadFromCacheOrDownload(url, version))
		{
		}

		/// <summary>
		/// Creates an instance of SerializedAssetBundle and stores
		/// the provided WWW instance as the source of the asset bundle.
		/// </summary>
		/// <param name="download">The WWW instance which is downloading the asset bundle or contains it.</param>
		public SerializedAssetBundle(WWW download)
		{
			this.download = download;
		}

		/// <summary>
		/// Returns the WWW instance in the class to wait for it until it finishes its job.
		/// </summary>
		/// <returns></returns>
		public WWW WaitForDownload()
		{
			return download;
		}

		/// <summary>
		/// Returns a request object which you can wait on it until Unity creates
		/// the asset bundle from the stored bytes.
		/// </summary>
		/// <returns></returns>
		public AssetBundleCreateRequest WaitForCreateRequest()
		{
			return createRequest;
		}
#else
		public byte[] bytes
		{
			get
			{
				return _bytes;
			}
		}
#endif

		/// <summary>
		/// Creates an instance of SerializedAssetBundle from the bytes provided.
		/// </summary>
		/// <param name="bytes">The byte array containing the asset bundle.</param>
		/// M<remarks>
		/// You might want to use this in multiple situations. 
		/// You can store the bundle in a database as a byte array (BLOB object), you might download encoded bundle and decode it
		/// to a byte array and ...
		/// <para>
		/// Specially you might be interested in using Riak (uGameDB) for storing your bundles in a key value pair
		/// instead of a regular file system. It can handle a great amount of traffic because the data is mostly read only
		/// and you can use multiple nodes. However using storage services like Amazon S3 also can be interesting if you
		/// want to use the caching feature easily.
		/// </para>
		/// </remarks>
		public SerializedAssetBundle(byte[] bytes)
		{
			_bytes = bytes;
		}

		internal SerializedAssetBundle(NetBuffer buffer)
		{
			uint size = buffer.ReadVariableUInt32();
			_bytes = buffer.ReadBytes((int)size);
		}

		internal void _Write(NetBuffer buffer)
		{
			var data = bytes;
			buffer.WriteVariableUInt32((uint)data.Length);
			buffer.Write(data);
		}
	}
}
