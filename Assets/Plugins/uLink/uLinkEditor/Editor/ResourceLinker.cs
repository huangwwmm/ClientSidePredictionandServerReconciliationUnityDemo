// (c)2011 Unity Park. All Rights Reserved.

using UnityEngine;
using UnityEditor;

namespace uLinkEditor
{
	public static class ResourceLinker
	{
		[MenuItem("uLink/Online Resources/What's New...", false, 402)]
		public static void OnMenu_News()
		{
			Application.OpenURL("http://developer.muchdifferent.com/unitypark/WhatsNew/uLink");
		}

		[MenuItem("uLink/Online Resources/Manual...", false, 403)]
		public static void OnMenu_Manual()
		{
			Application.OpenURL("http://developer.muchdifferent.com/unitypark/uLink/uLink");
		}

		[MenuItem("uLink/Online Resources/Tutorials and Examples...", false, 404)]
		public static void OnMenu_Tutorials()
		{
			Application.OpenURL("http://developer.muchdifferent.com/unitypark/Tutorials/Tutorials");
		}

		[MenuItem("uLink/Online Resources/API Reference...", false, 405)]
		public static void OnMenu_API()
		{
			Application.OpenURL("http://developer.muchdifferent.com/prevdevsite/api/ulink/");
		}

		[MenuItem("uLink/Online Resources/Community...", false, 408)]
		public static void OnMenu_Community()
		{
			Application.OpenURL("http://forum.muchdifferent.com/unitypark/");
		}

		[MenuItem("uLink/Online Resources/Buy License...", false, 450)]
		public static void OnMenu_Buy()
		{
			// TODO: we should add a better buy link.
			Application.OpenURL("http://developer.muchdifferent.com/unitypark/");
		}
	}
}
