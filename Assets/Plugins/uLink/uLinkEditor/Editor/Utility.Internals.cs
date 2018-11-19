// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using uLink;
using Object = UnityEngine.Object;

namespace uLinkEditor
{
	internal static partial class Utility
	{
		public delegate GUIContent IconContentDelegate(string name);
		public static IconContentDelegate IconContent = typeof(EditorGUIUtility).CreateDelegate<IconContentDelegate>("IconContent");

		public delegate void SetIconForObjectDelegate(Object obj, Texture2D icon);
		public static SetIconForObjectDelegate SetIconForObject = typeof(EditorGUIUtility).CreateDelegate<SetIconForObjectDelegate>("SetIconForObject");

		public static TDelegate CreateDelegate<TDelegate>(this Type implementingType, string methodName)
			where TDelegate : class
		{
			try
			{
				return Delegate.CreateDelegate(typeof(TDelegate), implementingType, methodName, true, false) as TDelegate;
			}
			catch
			{
				return null;
			}
		}
	}
}
