#define ULINK //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;

namespace Lidgren
{
	[Flags]
	internal enum LogFlags : uint
	{
		Socket = 1 << 0,
	}

	internal class Log
	{
		public static void Debug(LogFlags flags, params object[] args)
		{
#if ULINK
			uLink.Log.Debug(uLink.NetworkLogFlags.Socket, args);
#elif ULINK_TOOL
			// TODO: add support for log levels in the tools
#if DEBUG
			uLink.ConsoleMain.Log(String.Concat(args));
#endif
#elif ULOBBY
			uLobby.Log.Debug(uLobby.LogFlags.Network, args);
#endif
		}

		public static void Info(LogFlags flags, params object[] args)
		{
#if ULINK
			uLink.Log.Info(uLink.NetworkLogFlags.Socket, args);
#elif ULINK_TOOL
			// TODO: add support for log levels in the tools
#if DEBUG
			uLink.ConsoleMain.Log(String.Concat(args));
#endif
#elif ULOBBY
			uLobby.Log.Info(uLobby.LogFlags.Network, args);
#endif
		}

		public static void Warning(LogFlags flags, params object[] args)
		{
#if ULINK
			uLink.Log.Warning(uLink.NetworkLogFlags.Socket, args);
#elif ULINK_TOOL
			uLink.ConsoleMain.Log(String.Concat(args));
#elif ULOBBY
			uLobby.Log.Warning(uLobby.LogFlags.Network, args);
#endif
		}

		public static void Error(LogFlags flags, params object[] args)
		{
#if ULINK
			uLink.Log.Error(uLink.NetworkLogFlags.Socket, args);
#elif ULINK_TOOL
			uLink.ConsoleMain.Log(String.Concat(args));
#elif ULOBBY
			uLobby.Log.Error(uLobby.LogFlags.Network, args);
#endif
		}
	}
}
