// (c)2011 Unity Park. All Rights Reserved.

using UnityEditor;

namespace uLinkEditor
{
	[InitializeOnLoad]
	public static class AutoIconSelector
	{
		static AutoIconSelector()
		{
			EditorApplication.update += SetIcons;
		}

		private static void SetIcons()
		{
			// ReSharper disable DelegateSubtraction
			EditorApplication.update -= SetIcons;
			// ReSharper restore DelegateSubtraction

			try
			{
				if (Utility.SetIconForObject == null) return;

				var icon = Utility.GetLogoTexture();
				if (icon == null) return;

				var scripts = AssetDatabase.LoadAllAssetsAtPath(Utility.GetuLinkAssemblyPath());
				foreach (var script in scripts)
				{
					try
					{
						Utility.SetIconForObject(script, icon);
					}
					catch
					{
						// ignore exception
					}
				}
			}
			catch
			{
				// ignore exception
			}
		}
	}
}
