// (c)2011 Unity Park. All Rights Reserved.

using System;
using System.Threading;
using UnityEngine;
using UnityEditor;
using uLink;

namespace uLinkEditor
{
	public class ServerAuthenticationGenerator : EditorWindow
	{
		private const int WINDOW_WIDTH = 618;
		private const int WINDOW_HEIGHT = 678;

		[SerializeField]
		private ServerAuthentication target;

		[SerializeField]
		private Vector2 privateScrollPos;

		[SerializeField]
		private Vector2 publicScrollPos;

		[NonSerialized]
		private Thread generatorThread;

		[NonSerialized]
		private volatile string privateKey = String.Empty;
		[NonSerialized]
		private volatile string publicKey = String.Empty;

		internal static void Open(ServerAuthentication target)
		{
			var window = GetWindow<ServerAuthenticationGenerator>(true, "Set public/private key for uLink server authentication");
			window.Init(target);
			window.Repaint();
		}

		public ServerAuthenticationGenerator()
		{
			position = new Rect(100f, 100f, WINDOW_WIDTH, WINDOW_HEIGHT);
			minSize = maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
		}

		private void Init(ServerAuthentication target)
		{
			this.target = target;
			minSize = maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
		}

		protected void OnGUI()
		{
			if (target == null)
			{
				Close();
				return;
			}

			var textAreaStyle = new GUIStyle(EditorStyles.textField);
			textAreaStyle.padding.right = 1;
			textAreaStyle.contentOffset = new Vector2(0, 0);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(12);
			GUILayout.Label("To guarantee that the client is communicating with the correct (trusted) server, uLink allows server authentication. Begin by generating a RSA private/public key pair (please be patient) and follow these steps:", EditorStyles.wordWrappedLabel);

			if (generatorThread != null && generatorThread.IsAlive)
			{
				var style = new GUIStyle(EditorStyles.label);
				style.alignment = TextAnchor.MiddleCenter;

				GUILayout.Label(new GUIContent(" Takes a minute...", Utility.GetWaitSpinIcon().image), style, GUILayout.Width(140), GUILayout.Height(30));
			}
			else
			{
				if (generatorThread != null && !generatorThread.IsAlive)
				{
					generatorThread = null;

					target.privateKey = privateKey;
					target.publicKey = publicKey;

					GUI.FocusControl("Close");
				}

				if (GUILayout.Button("Generate Key Pair", GUILayout.Width(140), GUILayout.Height(30)))
				{
					Generate();
				}
			}

			GUILayout.Space(12);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space(12);
				EditorGUILayout.BeginVertical();
				{
					GUILayout.Space(25);
					GUILayout.Label("Step 1. Server's Private Key", EditorStyles.boldLabel);

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Space(53);
						EditorGUILayout.BeginVertical();
						{
							EditorGUILayout.BeginHorizontal();
							{
								GUILayout.Label("Make sure this instance of the \"Server Authentication\" component (containing the private key) only exists in the server scene (where the server is initialized). The private key must never be included in the client build; otherwise it's compromised.", EditorStyles.wordWrappedLabel);
								GUILayout.Space(12);
							}
							EditorGUILayout.EndHorizontal();
							GUILayout.Space(12);
			
							EditorGUILayout.BeginHorizontal();
							{
								GUI.enabled = (generatorThread == null);
								EditorGUILayout.BeginHorizontal(textAreaStyle);
								{
									privateScrollPos = EditorGUILayout.BeginScrollView(privateScrollPos, GUILayout.Height(254));
									target.privateKey = EditorGUILayout.TextArea(target.privateKey, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
									EditorGUILayout.EndScrollView();
								}
								EditorGUILayout.EndHorizontal();
								GUI.enabled = true;

								GUILayout.Space(18);
							}
							EditorGUILayout.EndHorizontal();
						}
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndHorizontal();

					GUILayout.Space(25);
					GUILayout.Label("Step 2. Client's Public Key", EditorStyles.boldLabel);

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Space(53);
						EditorGUILayout.BeginVertical();
						{
							EditorGUILayout.BeginHorizontal();
							{
								GUILayout.Label("After key generation: click \"Cut to Clipboard\", close window, open the client scene (where the client connects) and add component \"Server Authentication\" into it. Open its window (\"Set Key\") and click \"Paste from Clipboard\".", EditorStyles.wordWrappedLabel);
								GUILayout.Space(12);
							}
							EditorGUILayout.EndHorizontal();
							GUILayout.Space(12);

							EditorGUILayout.BeginHorizontal();
							{
								GUI.enabled = (generatorThread == null);
								EditorGUILayout.BeginHorizontal(textAreaStyle);
								{
									publicScrollPos = EditorGUILayout.BeginScrollView(publicScrollPos, GUILayout.Height(78));
									target.publicKey = EditorGUILayout.TextArea(target.publicKey, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
									EditorGUILayout.EndScrollView();
								}
								EditorGUILayout.EndHorizontal();
								GUI.enabled = true;

								GUILayout.Space(18);
							}
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginHorizontal();
							{
								GUI.enabled = (generatorThread == null);
								if (GUILayout.Button("Cut to Clipboard", GUILayout.Width(150)))
								{
									EditorGUIUtility.systemCopyBuffer = target.publicKey;
									target.publicKey = String.Empty;
									GUI.FocusControl("Close");
								}
								if (GUILayout.Button("Paste from Clipboard", GUILayout.Width(150)))
								{
									target.publicKey = EditorGUIUtility.systemCopyBuffer;
									GUI.FocusControl("Close");
								}
								GUI.enabled = true;

								GUILayout.FlexibleSpace();
							}
							EditorGUILayout.EndHorizontal();
						}
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();

			GUI.enabled = (generatorThread == null);
			GUI.SetNextControlName("Close");
			if (GUI.Button(new Rect(WINDOW_WIDTH - 12 - 70, WINDOW_HEIGHT - 11 - 22, 70, 22), "Close"))
			{
				Close();
			}
			GUI.enabled = true;

			if (Utility.HelpButton(new Rect(8, WINDOW_HEIGHT - 7 - 22, 200, 22), "What does it do?"))
			{
				Application.OpenURL("http://developer.muchdifferent.com/unitypark/uLink/SecurityandEncryption");
			}
		}

		protected void OnInspectorUpdate()
		{
			if (generatorThread != null)
			{
				Repaint();
			}
		}

		private void Generate()
		{
			privateKey = String.Empty;
			publicKey = String.Empty;

			generatorThread = new Thread(GeneratorThread);
			generatorThread.Start();
		}

		private void GeneratorThread()
		{
			var key = PrivateKey.Generate(2048);

			privateKey = key.ToXmlString();
			publicKey = key.GetPublicKey().ToXmlString();
		}
	}
}
