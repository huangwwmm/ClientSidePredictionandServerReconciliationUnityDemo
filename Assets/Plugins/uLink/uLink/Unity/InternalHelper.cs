#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 10139 $
// $LastChangedBy: david.almroth $
// $LastChangedDate: 2011-11-29 12:11:15 +0100 (Tue, 29 Nov 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using System;
using System.Text;
using UnityEngine;

namespace uLink
{
	/// <summary>
	/// Do not use this class. For internal use only.
	/// </summary>
	[AddComponentMenu("")]
	public class InternalHelper : MonoBehaviour
	{
		private const string GAMEOBJECT_NAME = "uLinkInternalHelper";
		private const HideFlags GAMEOBJEC_FLAGS = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable | HideFlags.DontSave;

		[NonSerialized]
		private static InternalHelper _singleton;

		protected void Awake()
		{
			Log.Debug(NetworkLogFlags.InternalHelper, "Awake was called on ", this);

			if (!_singleton.IsNullOrDestroyed())
			{
				Log.Debug(NetworkLogFlags.InternalHelper, "Destroying duplicate ", this, " because singleton ", _singleton, " already exists");
				_singleton = null;
				DestroyImmediate(gameObject);
				return;
			}

			_singleton = this;

			Network._singleton._UpdateTypeSafe();

			Log.Debug(NetworkLogFlags.InternalHelper, "Called DontDestroyOnLoad on ", this);
			DontDestroyOnLoad(this);

			Log.Debug(NetworkLogFlags.InternalHelper, "Application.runInBackground is enabled");
			Application.runInBackground = true;
		}

		protected void OnLevelWasLoaded()
		{
			Log.Debug(NetworkLogFlags.InternalHelper, "OnLevelWasLoaded was called on ", this);

			Network._singleton._AddAllNetworkViews();
		}

		protected void LateUpdate()
		{
			// TODO: do something with this, so that it doesn't log to often.
			//Log.Debug(NetworkLogFlags.InternalHelper, "LateUpdate was called on ", this);

			Network._singleton.Update();
		}

		protected void OnApplicationQuit()
		{
			Log.Debug(NetworkLogFlags.InternalHelper, "OnApplicationQuit was called on ", this);

			Network.Disconnect();
			Network.DisconnectImmediate(); // TODO: is this ok?
		}

		internal static void _AddOneHelper()
		{
			if (_singleton.IsNullOrDestroyed())
			{
				Log.Debug(NetworkLogFlags.InternalHelper, "Creating hidden GameObject called '", GAMEOBJECT_NAME, "' with component ", typeof(InternalHelper), " and flags ", GAMEOBJEC_FLAGS);

				var go = new GameObject(GAMEOBJECT_NAME, typeof(InternalHelper));
				go.hideFlags = GAMEOBJEC_FLAGS;
			}
		}

		/* TODO: use this late for trail version:
		
		private const string REMINDER = "uLink trial version";
		private const float MARGIN_X = 4;
		private const float MARGIN_Y = 0;

		private static GUIContent content = null;

		protected void OnGUI()
		{
			var oldSkin = GUI.skin;
			var oldDepth = GUI.depth;

			GUI.skin = null;
			GUI.depth = 0;

			if (content == null) content = new GUIContent(REMINDER);

			var size = GUI.skin.label.CalcSize(content);
			var pos = new Vector2(Screen.width - size.x - MARGIN_X, MARGIN_Y);
			var area = new Rect(0, 0, size.x, size.y);

			GUI.color = new Color(0, 0, 0, 0.25f);

			area.x = pos.x;
			area.y = pos.y - 1; GUI.Label(area, content);
			area.y = pos.y + 1; GUI.Label(area, content);
			area.y = pos.y + 2; GUI.Label(area, content);

			area.x = pos.x + 1;
			area.y = pos.y + 1; GUI.Label(area, content);
			area.y = pos.y + 2; GUI.Label(area, content);
			area.y = pos.y - 1; GUI.Label(area, content);

			area.y = pos.y;
			area.x = pos.x + 2; GUI.Label(area, content);
			area.x = pos.x - 1; GUI.Label(area, content);

			GUI.color = new Color(1, 1, 1, 1f);

			area.x = pos.x + 1; GUI.Label(area, content);
			area.x = pos.x; GUI.Label(area, content);

			GUI.skin = oldSkin;
			GUI.depth = oldDepth;
		}
		*/

		public override string ToString()
		{
			return Utility.ToHierarchyString(this);
		}
	}
}

#endif
