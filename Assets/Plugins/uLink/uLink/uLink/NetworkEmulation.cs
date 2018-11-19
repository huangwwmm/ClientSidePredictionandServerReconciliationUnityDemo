#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
using System;
using Lidgren.Network;

namespace uLink
{
	/// <summary>
	/// Emulates network problems like lost packets, duplicate packets and latency 
	/// fluctuations. See <see cref="uLink.Network.emulation"/>. 
	/// </summary>
	public class NetworkEmulation
	{
		private float _maxBandwidth = 0;
		private float _chanceOfLoss = 0;
		private float _chanceOfDuplicates = 0;
		private float _minLatency = 0;
		private float _maxLatency = 0;

		private NetworkBaseLocal _network;

		/// <summary>
		/// Gets or sets the maximum emulated bandwidth when sending from this peer.
		/// </summary>
		public float maxBandwidth { get { return _maxBandwidth; } set { _maxBandwidth = value; _Apply(); } }

		/// <summary>
		/// This is a float which simulates lost UDP packets when sending from this peer.
		/// </summary>
		/// <remarks>A value of 0 will disable this feature, 
		/// a value of 0.5 will make half of your sent packets disappear, chosen randomly. Note that 
		/// one UDP packet may contain several uLink messages (RPCs and statesyncs) - this is the amount 
		/// of UDP packets lost. 
		/// </remarks>
		public float chanceOfLoss { get { return _chanceOfLoss; } set { _chanceOfLoss = value; _Apply(); } }
		
		/// <summary>
		///	Dictates the chance that a packet will be duplicated at the destination, when sent from this peer. 
		/// </summary>
		/// <remarks>
		/// 0 means no packets will be duplicated, 0.5 means that on average, every other packet will be duplicated. 
		/// </remarks>
		public float chanceOfDuplicates { get { return _chanceOfDuplicates; } set { _chanceOfDuplicates = value; _Apply(); } }
		/// <summary>
		/// Gets or sets the minimum emulated one-way latency addition for packets in seconds (not milliseconds)
		/// </summary>
		/// <remarks>
		/// This value and <see cref="maxLatency"/> work on top of the actual network delay and the total delay will be: 
		/// Actual one way latency + <see cref="minLatency"/> + (randomly per packet 0 to <see cref="maxLatency"/> seconds) 
		/// when sending from this peer
		/// </remarks>
		public float minLatency { get { return _minLatency; } set { _minLatency = value; _Apply(); } }
		/// <summary>
		/// Gets or sets the maximum emulated one-way latency addition for packets in seconds (not milliseconds)
		/// </summary>
		/// <remarks>
		/// This value and <see cref="minLatency"/> work on top of the actual network delay and the total delay will be: 
		/// Actual one way latency + <see cref="minLatency"/> + (randomly per packet 0 to <see cref="maxLatency"/> seconds) 
		/// when sending from this peer
		/// </remarks>
		public float maxLatency { get { return _maxLatency; } set { _maxLatency = value; _Apply(); } }

		internal NetworkEmulation(NetworkBaseLocal network)
		{
			_network = network;
		}

		private void _Apply()
		{
			_Apply(_network._GetNetBase());
			_Apply(_network._GetNetBaseMaster());
		}

		internal void _Apply(NetBase net)
		{
			if (net == null) return;

			net.Simulate(_chanceOfLoss, _chanceOfDuplicates, _minLatency, Math.Max(0, _maxLatency - _minLatency));
			net.Configuration.ThrottleBytesPerSecond = _maxBandwidth * 1024;
		}

		/// <summary>
		/// Gets the emulation properties from <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		public void GetPrefs()
		{
			_maxBandwidth = NetworkPrefs.Get("Network.emulation.maxBandwidth", _maxBandwidth);
			_chanceOfLoss = NetworkPrefs.Get("Network.emulation.chanceOfLoss", _chanceOfLoss);
			_chanceOfDuplicates = NetworkPrefs.Get("Network.emulation.chanceOfDuplicates", _chanceOfDuplicates);
			_minLatency = NetworkPrefs.Get("Network.emulation.minLatency", _minLatency);
			_maxLatency = NetworkPrefs.Get("Network.emulation.maxLatency", _maxLatency);
			_Apply();
		}

		/// <summary>
		/// Sets the emulation properties in <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		/// <remarks>
		/// The method can't update the saved values in the persistent <see cref="uLink.NetworkPrefs.resourcePath"/> file,
		/// because that assumes the file is editable (i.e. the running project isn't built) and would require file I/O permission.
		/// Calling this will only update the values in memory.
		/// </remarks>
		public void SetPrefs()
		{
			NetworkPrefs.Set("Network.emulation.maxBandwidth", _maxBandwidth);
			NetworkPrefs.Set("Network.emulation.chanceOfLoss", _chanceOfLoss);
			NetworkPrefs.Set("Network.emulation.chanceOfDuplicates", _chanceOfDuplicates);
			NetworkPrefs.Set("Network.emulation.minLatency", _minLatency);
			NetworkPrefs.Set("Network.emulation.maxLatency", _maxLatency);
		}
	}
}
