#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using UnityEngine;

namespace uLink
{
	/// <summary>
	/// The central class in uLink for PikkoServer operations.
	/// </summary>
	public static class PikkoServer
	{
		/// <summary>
		/// This option has been deprecated and will be removed in the future.
		/// </summary>
		[System.Obsolete]
		public static bool connectToPikkoServer;

		/// <summary>
		/// Gets the position of this cell server in the mast algorithm. (Updated only if PikkoServer has been configured to do so.)
		/// </summary>
		public static Vector3 cellPosition { get { return Network._singleton.cellPosition; } }

		/// <summary>
		/// Requests PikkoServer to asynchronously carry out a handover as if the mast algorithm had decided it.
		/// For this operation to have any effect, NetworkAuthFlags.DontHandoverInPikkoServer must be set.
		/// On failure PikkoServer will silently reject the request.
		/// </summary>
		/// <param name="nv">The NetworkView of the object to be handed over.</param>
		/// <param name="targetCell">The cell server the object should be handed over to.</param>
		public static void TriggerHandover(NetworkView nv, NetworkPlayer targetCell) { Network._singleton.TriggerHandover(nv, targetCell); }

		/// <summary>
		/// Requests PikkoServer to asynchronously carry out a handover as if the mast algorithm had decided it.
		/// For this operation to have any effect, NetworkAuthFlags.DontHandoverInPikkoServer must be set.
		/// On failure PikkoServer will silently reject the request.
		/// </summary>
		/// <param name="viewID">The NetworkViewID of the object to be handed over.</param>
		/// <param name="targetCell">The cell server the object should be handed over to.</param>
		public static void TriggerHandover(NetworkViewID viewID, NetworkPlayer targetCell) { Network._singleton.TriggerHandover(viewID, targetCell); }
	}
}

#endif