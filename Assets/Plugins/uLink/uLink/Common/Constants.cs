#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8656 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-22 05:36:17 +0200 (Mon, 22 Aug 2011) $
#endregion

namespace uLink
{
	internal static partial class Constants
	{
		public const string ANALYTICS_CATEGORY = "uLink1.6.0";

		//public static readonly IPEndPoint ENDPOINT_NONE = new IPEndPoint(IPAddress.None, 0);

		public const string CONFIG_NETWORK_IDENTIFIER = "uLink 1.6.0-beta23";
		public const string CONFIG_MASTER_IDENTIFIER = "uLinkMaster 1.1";
		public const string CONFIG_P2P_IDENTIFIER = "uLinkP2P 1.6";

		public const double DEFAULT_HANDSHAKE_TIMEOUT = 2.5;
		public const int DEFAULT_DICONNECT_TIMEOUT = 200;
		public const int DEFAULT_SEND_BUFFER_SIZE = 131072;
		public const int DEFAULT_RECV_BUFFER_SIZE = 131072;
		public const int DEFAULT_PIKKO_REMOTE_PORT = 7100;

		// TODO: make these strings into just a sbyte/int equal to a NetworkConnectionError value
		// TODO: add time out reason?
		public const string REASON_NORMAL_DISCONNECT = "Normal disconnect";
		public const string REASON_TOO_MANY_PLAYERS = "Server full";
		public const string REASON_CONNECTION_BANNED = "Connection banned";
		public const string REASON_INVALID_PASSWORD = "Invalid password";
		public const string REASON_LIMITED_PLAYERS = "Special server";
		public const string REASON_BAD_APP_ID = "App id bad";

		public const string NOTIFY_CONNECT_TO_SELF = "Connection to self not allowed";
		public const string NOTIFY_MAX_CONNECTIONS = "Max connections";
		public const string NOTIFY_LIMITED_PLAYERS = "I'm special";
		public const string NOTIFY_BAD_APP_ID = "Bad app id";

		public const int VALIDATION_KEY_SIZE = 1024;
		public const string VALIDATION_PUBLIC_KEY = "<RSAKeyValue><Modulus>rX2KkVYVlV4O+wMQyot18ODj+/AGgWaDiAKdFijtJBaE0OAVZESwQPcXXcavx8a/h/ExzEh7IdlpvjkEgEjzSQlLA5z7rjWhzcxfU5aMSVZKnRp9GYHJREH2CsVX6DkoeBAamyK57s2Ypjf4ekfJ/zrM2xYo+h8e/GD/EFw6X3k=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

		//by WuNan @2016/09/12 11:36:37
		// 为了避免隐式new object[0]而产生的GC,这里提前new object[0]
		public static readonly object[] EMPTY_OBJECT_ARRAY = new object[0];
	}
}
