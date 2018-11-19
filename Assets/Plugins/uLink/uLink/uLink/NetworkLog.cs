#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 9002 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-09-02 16:46:35 +0200 (Fri, 02 Sep 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
// TODO: make sure all log messages have context


using System;

namespace uLink
{
	/// <summary>
	/// Class for configuring uLink logging.
	/// </summary>
	/// <remarks>
	/// The best way to get started with logging in uLink is to use the Editor menu: uLink - Edit Settings. 
	/// There it is possible to configure logging level per category and use the generated output to debug most problems.
	/// By default the log messages are written to the Unity console window in the editor or the log file output_log.txt when 
	/// running a build outside the editor.
	/// <para>
	/// Use this class to go one step further and control the log settings in your own code. Also use it to 
	/// replace the 4 public Writers with your own faster implementations: 
	/// <see cref="errorWriter"/>, <see cref="warningWriter"/>, <see cref="infoWriter"/>, <see cref="debugWriter"/> 
	/// with your own implementations it is possible to log events to 
	/// screen or file(s) or database or whatever you like. 
	/// </para>
	/// <para>
	/// Finally, it is possible to add your own logging categories like “Trading”, “Cheating attempts” or “Level loding” 
	/// and set the log level individually for these categories.
	/// </para>
	/// </remarks>
	/// <example> 
	/// This example shows how to create and use your own log categories:
	/// <code>
	/// using UnityEngine;
	/// using System.Collections;
	/// public class CustomLog : MonoBehaviour 
	/// {
	///    //Define your own log categories like this
	///    public const uLink.NetworkLogFlags CHEAT_ATTEMPTS = uLink.NetworkLogFlags.UserDefined1;
	///    public const uLink.NetworkLogFlags TRADE_EVENT = uLink.NetworkLogFlags.UserDefined2;
	///    public const uLink.NetworkLogFlags LEVEL_LOAD_EVENT = uLink.NetworkLogFlags.UserDefined3;
	///
	///    void Start()
	///    {
	///        //Set the log level for cheat attempts to the highest = Debug    
	///        uLink.NetworkLog.SetLevel(CHEAT_ATTEMPTS, uLink.NetworkLogLevel.Debug);
	///
	///        //Do some debug logging
	///        for (int i = 1; i != 5; i++)
	///        {
	///            uLink.NetworkLog.Debug(CHEAT_ATTEMPTS, "Detected cheat attempt nr ", i, " in the demo code.");
	///        }
	///    }
	/// }
	/// </code>
	/// </example>
	public partial class NetworkLog
	{
		public const bool isDebugBuild =
#if DEBUG
			true;
#else
			false;
#endif

		/// <summary>
		/// Implement one or several new delegates if there is a need to replace the default Writers in this class. 
		/// </summary>
		/// <example>
		/// In your own implementation you can choose another destination for log messages. 
		/// This example code shows how to send log messages to a file.
		/// <code>
		/// using UnityEngine;
		/// using System.Collections;
		/// using System.IO;
		/// 
		/// public class CustomFileLog : MonoBehaviour 
		/// {
		///     public string logFileName = @"c:\temp\MyLog.txt";
		/// 
		///     void Start()
		///     {
		///         //This code replaces the default debugWriter with a new delegate
		///         uLink.NetworkLog.debugWriter = delegate(uLink.NetworkLogFlags flags, object[] args) 
		///         {
		///             string line = System.DateTime.Now + ", " + uLink.NetworkLogUtility.ToString(args) + "\r\n";
		///             File.AppendAllText(logFileName, line); 
		///         };
		/// 
		///         //Set the level to Debug for the category uLink.NetworkLogFlags.Server
		///         uLink.NetworkLog.SetLevel(uLink.NetworkLogFlags.Server, uLink.NetworkLogLevel.Debug);
		/// 
		///         //Do some debug logging to the file
		///         for (int i = 1; i != 5; i++)
		///         {
		///             uLink.NetworkLog.Debug(uLink.NetworkLogFlags.Server, "Hello ", i, " from demo code.");
		///         }
		///     }
		/// }
		/// </code></example>
		public delegate void Writer(NetworkLogFlags flags, params object[] args);

#if UNITY_BUILD
		/// <summary>
		/// The default method which used for error logs.
		/// </summary>
		public readonly static Writer defaultErrorWriter = delegate(NetworkLogFlags flags, object[] args) { UnityEngine.Debug.LogError(NetworkLogUtility.ObjectsToString(args), NetworkLogUtility.FindObjectOfType<UnityEngine.Object>(args)); };
		/// <summary>
		/// The default method for writing warning logss.
		/// </summary>
		public readonly static Writer defaultWarningWriter = delegate(NetworkLogFlags flags, object[] args) { UnityEngine.Debug.LogWarning(NetworkLogUtility.ObjectsToString(args), NetworkLogUtility.FindObjectOfType<UnityEngine.Object>(args)); };
		/// <summary>
		/// The default method for writing info logs.
		/// </summary>
		public readonly static Writer defaultInfoWriter = delegate(NetworkLogFlags flags, object[] args) { UnityEngine.Debug.Log(NetworkLogUtility.ObjectsToString(args), NetworkLogUtility.FindObjectOfType<UnityEngine.Object>(args)); };
		/// <summary>
		/// The default method for writing debug logs.
		/// </summary>
		public readonly static Writer defaultDebugWriter = delegate(NetworkLogFlags flags, object[] args) { UnityEngine.Debug.Log(NetworkLogUtility.ObjectsToString(args), NetworkLogUtility.FindObjectOfType<UnityEngine.Object>(args)); };
#else
		public readonly static Writer defaultErrorWriter = delegate(NetworkLogFlags flags, object[] args) { System.Console.Error.WriteLine(NetworkLogUtility.ObjectsToString(args)); };
		public readonly static Writer defaultWarningWriter = delegate(NetworkLogFlags flags, object[] args) { System.Console.WriteLine(NetworkLogUtility.ObjectsToString(args)); };
		public readonly static Writer defaultInfoWriter = delegate(NetworkLogFlags flags, object[] args) { System.Console.WriteLine(NetworkLogUtility.ObjectsToString(args)); };
		public readonly static Writer defaultDebugWriter = delegate(NetworkLogFlags flags, object[] args) { System.Console.WriteLine(NetworkLogUtility.ObjectsToString(args)); };
#endif

		/// <summary>
		/// The delegate writing error messages. Default implementation send output to the Editor console / output_log.txt file. 
		/// </summary>
		/// <remarks>Look at <see cref="uLink.NetworkLog.Writer"/> for code example replacing the default Writer.</remarks>
		public static Writer errorWriter = defaultErrorWriter;

		/// <summary>
		/// The delegate writing warning messages. Default implementation send output to the Editor console / output_log.txt file. 
		/// </summary>
		/// <remarks>Look at <see cref="uLink.NetworkLog.Writer"/> for code example replacing the default Writer.</remarks>
		public static Writer warningWriter = defaultWarningWriter;

		/// <summary>
		/// The delegate writing info messages. Default implementation send output to the Editor console / output_log.txt file. 
		/// </summary>
		/// <remarks>Look at <see cref="uLink.NetworkLog.Writer"/> for code example replacing the default Writer.</remarks>
		public static Writer infoWriter = defaultInfoWriter;

		/// <summary>
		/// The delegate writing debug messages. Default implementation send output to the Editor console / output_log.txt file. 
		/// </summary>
		/// <remarks>Look at <see cref="uLink.NetworkLog.Writer"/> for code example replacing the default Writer.</remarks>
		public static Writer debugWriter = defaultDebugWriter;

		/// <summary>
		/// The minimum level for all logging categories. This can be overruled by setting a detailed log level for an individual category to a higher log level via <see cref="SetLevel"/>.
		/// </summary>
		/// <value>Default value is <see cref="uLink.NetworkLogLevel.Warning"/></value>
		public static NetworkLogLevel minLevel = NetworkLogLevel.Warning;

		private static NetworkLogFlags[] _levelFlags = new NetworkLogFlags[LEVELS] { 0, 0, 0, 0 };

		public const int LEVELS = 4;

		static NetworkLog()
		{
			GetPrefs();

			Log.Debug(NetworkLogFlags.Utility, "Initialized logging for uLink ", NetworkVersion.current);
		}

		/// <summary>
		/// Gets the NetworkLog properties from <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		public static void GetPrefs()
		{
			minLevel = (NetworkLogLevel)NetworkPrefs.Get("NetworkLog.minLevel", (int)NetworkLogLevel.Warning);
			errorFlags = (NetworkLogFlags)NetworkPrefs.Get("NetworkLog.errorFlags", (int)NetworkLogFlags.None);
			warningFlags = (NetworkLogFlags)NetworkPrefs.Get("NetworkLog.warningFlags", (int)NetworkLogFlags.None);
			infoFlags = (NetworkLogFlags)NetworkPrefs.Get("NetworkLog.infoFlags", (int)NetworkLogFlags.None);
			debugFlags = (NetworkLogFlags)NetworkPrefs.Get("NetworkLog.debugFlags", (int)NetworkLogFlags.None);
		}

		/// <summary>
		/// Sets the NetworkLog properties in <see cref="uLink.NetworkPrefs"/>.
		/// </summary>
		/// <remarks>
		/// The method can't update the saved values in the persistent <see cref="uLink.NetworkPrefs.resourcePath"/> file,
		/// because that assumes the file is editable (i.e. the running project isn't built) and would require file I/O permission.
		/// Calling this will only update the values in memory.
		/// </remarks>
		public static void SetPrefs()
		{
			NetworkPrefs.Set("NetworkLog.minLevel", (int)minLevel);
			NetworkPrefs.Set("NetworkLog.errorFlags", (int)errorFlags);
			NetworkPrefs.Set("NetworkLog.warningFlags", (int)warningFlags);
			NetworkPrefs.Set("NetworkLog.infoFlags", (int)infoFlags);
			NetworkPrefs.Set("NetworkLog.debugFlags", (int)debugFlags);
		}

		/// <summary>
		/// Gets or sets the log categories for the <see cref="errorWriter"/>.
		/// </summary>
		public static NetworkLogFlags errorFlags { get { return _levelFlags[(int)NetworkLogLevel.Error - 1]; } set { _levelFlags[(int)NetworkLogLevel.Error - 1] = value; } }

		/// <summary>
		/// Gets or sets the log categories for the <see cref="warningWriter"/>.
		/// </summary>
		public static NetworkLogFlags warningFlags { get { return _levelFlags[(int)NetworkLogLevel.Warning - 1]; } set { _levelFlags[(int)NetworkLogLevel.Warning - 1] = value; } }

		/// <summary>
		/// Gets or sets the log categories for the <see cref="infoWriter"/>.
		/// </summary>
		public static NetworkLogFlags infoFlags { get { return _levelFlags[(int)NetworkLogLevel.Info - 1]; } set { _levelFlags[(int)NetworkLogLevel.Info - 1] = value; } }

		/// <summary>
		/// Gets or sets the log categories for the <see cref="debugWriter"/>.
		/// </summary>
		public static NetworkLogFlags debugFlags { get { return _levelFlags[(int)NetworkLogLevel.Debug - 1]; } set { _levelFlags[(int)NetworkLogLevel.Debug - 1] = value; } }

		/// <summary>
		/// Sends a log message to the delegate <see cref="debugWriter"/> if the <see cref="uLink.NetworkLogLevel.Debug"/> log level for the specifed categories (flags) are set by either <see cref="SetLevel"/> or <see cref="minLevel"/>.
		/// </summary>
		/// <param name="flags">The categories this log message belongs to</param>
		/// <param name="args">The objects that are to be concatenated to a log message if the debug log level for the flags is set</param>
		/// <example>Example code for writing a message like "The server has now received 88 Fire RPCs from player Paul55".
		/// <code>
		/// uLink.NetworkLog.Debug(uLink.NetworkLogFlags.Server, "The server has now received ", numberOfFireRPCs, " Fire RPCs from player ", playerName);
		/// </code>
		/// </example>
		/// <remarks>Please note that it is important to NOT use string concatenation with + to compose the log message. 
		/// Instead send the objects individually just like the example code provided here. 
		/// Otherwise you will lose performance. The concatenation would be executed even if the log level for the flags are 
		/// turned off, and that is a waste of CPU resources.</remarks>
		public static void Debug(NetworkLogFlags flags, params object[] args)
		{
			if (IsDebugLevel(flags))
			{
				try
				{
					debugWriter(flags, args);
				}
				catch (Exception ex)
				{
					_DebugWriterFailed(ex, flags, args);
				}
			}
		}

		/// <summary>
		/// Sends a log message to the delegate <see cref="infoWriter"/> if the <see cref="uLink.NetworkLogLevel.Info"/> log level for the specifed categories (flags) are set by either <see cref="SetLevel"/> or <see cref="minLevel"/>.
		/// </summary>
		/// <param name="flags">The categories this log message belongs to</param>
		/// <param name="args">The objects that are to be concatenated to a log message if the info log level for the flags is set</param>
		/// <example>Example code for writing a message like "The server has now received 88 Fire RPCs from player Paul55".
		/// <code>
		/// uLink.NetworkLog.Info(uLink.NetworkLogFlags.Server, "The server has now received ", numberOfFireRPCs, " Fire RPCs from player ", playerName);
		/// </code>
		/// </example>
		/// <remarks>Please note that it is important NOT to use string concatenation with + to compose a nice log message. 
		/// Instead send the arguments individually just like the example code provided here. 
		/// Otherwise you will lose performance. The concatenation would be executed even if the logging 
		/// is turned off, and that is a waste of CPU resources.</remarks>
		public static void Info(NetworkLogFlags flags, params object[] args)
		{
			if (IsInfoLevel(flags))
			{
				try
				{
					infoWriter(flags, args);
				}
				catch (Exception ex)
				{
					_InfoWriterFailed(ex, flags, args);
				}
			}
		}

		/// <summary>
		/// Sends a log message to the delegate <see cref="warningWriter"/> if the <see cref="uLink.NetworkLogLevel.Warning"/> log level for the specifed categories (flags) are set by either <see cref="SetLevel"/> or <see cref="minLevel"/>.
		/// </summary>
		/// <param name="flags">The categories this log message belongs to</param>
		/// <param name="args">The objects that are to be concatenated to a log message if the info log level for the flags is set</param>
		/// <example>Example code for writing a message like "The server has now received 88 Fire RPCs from player Paul55".
		/// <code>
		/// uLink.NetworkLog.Warning(uLink.NetworkLogFlags.Server, "The server has now received ", numberOfFireRPCs, " Fire RPCs from player ", playerName);
		/// </code>
		/// </example>
		/// <remarks>Please note that it is important NOT to use string concatenation with + to compose a nice log message. 
		/// Instead send the arguments individually just like the example code provided here. 
		/// Otherwise you will lose performance. The concatenation would be executed even if the logging 
		/// is turned off, and that is a waste of CPU resources.</remarks>
		public static void Warning(NetworkLogFlags flags, params object[] args)
		{
			if (IsWarningLevel(flags))
			{
				try
				{
					warningWriter(flags, args);
				}
				catch (Exception ex)
				{
					_WarningWriterFailed(ex, flags, args);
				}
			}
		}

		/// <summary>
		/// Sends a log message to the delegate <see cref="errorWriter"/> if the <see cref="uLink.NetworkLogLevel.Error"/> log level for the specifed categories (flags) are set by either <see cref="SetLevel"/> or <see cref="minLevel"/>.
		/// </summary>
		/// <param name="flags">The categories this log message belongs to</param>
		/// <param name="args">The objects that are to be concatenated to a log message if the info log level for the flags is set</param>
		/// <example>Example code for writing a message like "The server received an illegal Fire RPCs from player Paul55".
		/// <code>
		/// uLink.NetworkLog.Error(uLink.NetworkLogFlags.Server, "The server received an illegal Fire RPCs from player ", playerName);
		/// </code>
		/// </example>
		/// <remarks>Please note that it is important NOT to use string concatenation with + to compose a nice log message. 
		/// Instead send the arguments individually just like the example code provided here. 
		/// Otherwise you will lose performance. The concatenation would be executed even if the logging 
		/// is turned off, and that is a waste of CPU resources.</remarks>
		public static void Error(NetworkLogFlags flags, params object[] args)
		{
			if (IsErrorLevel(flags))
			{
				try
				{
					errorWriter(flags, args);
				}
				catch (Exception ex)
				{
					_ErrorWriterFailed(ex, flags, args);
				}
			}
		}

		/// <summary>
		/// Returns true if the the categories (flags) are set by either <see cref="SetLevel"/> or <see cref="minLevel"/>.
		/// </summary>
		public static bool HasLevel(NetworkLogFlags flags, NetworkLogLevel level)
		{
			return minLevel >= level || (_levelFlags[(int)level - 1] & flags) != 0;
		}

		/// <summary>
		/// Sets a detailed logging level for the specified categories (flags). If the categories (flags) is set to a lower log level than <see cref="minLevel"/> then <see cref="minLevel"/> overrules the detailed log level when logging.
		/// </summary>
		public static void SetLevel(NetworkLogFlags flags, NetworkLogLevel level)
		{
			for (int i = 0; i < (int)level; i++)
			{
				_levelFlags[i] |= flags;
			}

			for (int i = (int)level; i < LEVELS; i++)
			{
				_levelFlags[i] &= ~flags;
			}
		}

		/// <summary>
		/// Gets the highest detailed log level configured for any of the log categories (flags), where 
		/// <see cref="uLink.NetworkLogLevel.Off"/> is the lowest and <see cref="uLink.NetworkLogLevel.Debug"/> is the highest.
		/// </summary>
		public static NetworkLogLevel GetMaxLevel(NetworkLogFlags flags)
		{
			for (int i = 0; i < LEVELS; i++)
			{
				if ((_levelFlags[i] & flags) == 0) return (NetworkLogLevel)i;
			}

			return (NetworkLogLevel)LEVELS;
		}

		/// <summary>
		/// Gets the lowest detailed log level configured for any of the log categories (flags), where 
		/// <see cref="uLink.NetworkLogLevel.Off"/> is the lowest and <see cref="uLink.NetworkLogLevel.Debug"/> is the highest.
		/// </summary>
		public static NetworkLogLevel GetMinLevel(NetworkLogFlags flags)
		{
			for (int i = 0; i < LEVELS; i++)
			{
				if ((_levelFlags[i] & flags) != flags) return (NetworkLogLevel)i;
			}

			return (NetworkLogLevel)LEVELS;
		}

		/// <summary>
		/// Is the category in debug level enabled? 
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static bool IsDebugLevel(NetworkLogFlags flags)
		{
			return minLevel >= NetworkLogLevel.Debug || (_levelFlags[(int)NetworkLogLevel.Debug - 1] & flags) != 0;
		}

		/// <summary>
		/// Is the category in info level?
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static bool IsInfoLevel(NetworkLogFlags flags)
		{
			return minLevel >= NetworkLogLevel.Info || (_levelFlags[(int)NetworkLogLevel.Info - 1] & flags) != 0;
		}

		/// <summary>
		/// Is the category in warning level?
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static bool IsWarningLevel(NetworkLogFlags flags)
		{
			return minLevel >= NetworkLogLevel.Warning || (_levelFlags[(int)NetworkLogLevel.Warning - 1] & flags) != 0;
		}

		/// <summary>
		/// Is the category in error level?
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static bool IsErrorLevel(NetworkLogFlags flags)
		{
			return minLevel >= NetworkLogLevel.Error || (_levelFlags[(int)NetworkLogLevel.Error - 1] & flags) != 0;
		}

		private static void _DebugWriterFailed(Exception ex, NetworkLogFlags flags, params object[] args)
		{
			try
			{
				defaultErrorWriter(flags, "Debug log writer failed to output: ", NetworkLogUtility.ObjectsToString(args), "\n\n", ex);
			}
			catch
			{
			}
		}

		private static void _InfoWriterFailed(Exception ex, NetworkLogFlags flags, params object[] args)
		{
			try
			{
				defaultErrorWriter(flags, "Info log writer failed to output: ", NetworkLogUtility.ObjectsToString(args), "\n\n", ex);
			}
			catch
			{
			}
		}

		private static void _WarningWriterFailed(Exception ex, NetworkLogFlags flags, params object[] args)
		{
			try
			{
				defaultErrorWriter(flags, "Warning log writer failed to output: ", NetworkLogUtility.ObjectsToString(args), "\n\n", ex);
			}
			catch
			{
			}
		}

		private static void _ErrorWriterFailed(Exception ex, NetworkLogFlags flags, params object[] args)
		{
			try
			{
				defaultErrorWriter(flags, "Error log writer failed to output: ", NetworkLogUtility.ObjectsToString(args), "\n\n", ex);
			}
			catch
			{
			}
		}
	}

	internal class Log : NetworkLog
	{
	}
}
