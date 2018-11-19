#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using UnityEngine;


/// <summary>
/// Special class to make some uLink features available in all GameObjects in Unity. Do not use this class directly.
/// </summary>
/// <remarks>
/// This class uses new .Net features to add static methods to other classes. Therefore this feature is only available
/// in Unity 3.0 and newer versions of Unity. The static methods this class adds are uLinkNetworkView() and 
/// uLinkNetworkP2P() that can be accessed from 
/// ALL GameObjects and Components in your code. The property will be <c>null</c> if no component of that type is
/// attached to the GameObject. Do not use this class directly, see example below instead.
/// </remarks>
/// <example>
/// This code works in C# only, in a script component.
/// <code>
/// //Using the game object
/// Debug.Log("View ID = " + gameObject.uLinkNetworkView().viewID);
/// //Using the component via 'this'
/// Debug.Log("View ID = " + this.uLinkNetworkView().viewID);
/// </code></example>
/// 
public static class uLinkExtensions
{
	/// <summary>
	/// Returns the <see cref="uLink.NetworkView"/> component attached to the Game Object.
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns>The NetworkView component if attached, null otherwise.</returns>
	public static uLink.NetworkView uLinkNetworkView(this GameObject gameObject)
	{
		return uLink.NetworkView.Get(gameObject);
	}

	/// <summary>
	/// Returns the <see cref="uLink.NetworkView"/> component attached to the Game Object which this component is attached to.
	/// </summary>
	/// <param name="component"></param>
	/// <returns>The NetworkView component attached to the GameObject which we are attached to (if exists), otherwise null</returns>
	public static uLink.NetworkView uLinkNetworkView(this Component component)
	{
		return uLink.NetworkView.Get(component);
	}

	/// <summary>
	/// Returns the <see cref="uLink.NetworkP2P"/> component attached to the Game Object.
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns>The NetworkP2P component if attached, null otherwise.</returns>
	public static uLink.NetworkP2P uLinkNetworkP2P(this GameObject gameObject)
	{
		return uLink.NetworkP2P.Get(gameObject);
	}

	/// <summary>
	/// Returns the <see cref="uLink.NetworkP2P"/> component attached to the Game Object which this component is attached to.
	/// </summary>
	/// <param name="component"></param>
	/// <returns>The NetworkP2P component attached to the GameObject which we are attached to (if exists), otherwise null</returns>
	public static uLink.NetworkP2P uLinkNetworkP2P(this Component component)
	{
		return uLink.NetworkP2P.Get(component);
	}
}

#endif
