#region COPYRIGHT
// (c)2012 MuchDifferent. All Rights Reserved.
// 
// $Revision: 11299 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-02-04 17:03:20 +0100 (Sat, 04 Feb 2012) $
#endregion

using System;

namespace uLink
{

	public partial class NetworkLog
	{
		
			public static void Debug<T1>(NetworkLogFlags flags, T1 t1)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1);
					}
				}
			}
			
			public static void Info<T1>(NetworkLogFlags flags, T1 t1)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1);
					}
				}
			}
			
			public static void Warning<T1>(NetworkLogFlags flags, T1 t1)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1);
					}
				}
			}
			
			public static void Error<T1>(NetworkLogFlags flags, T1 t1)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1);
					}
				}
			}
			
		
			public static void Debug<T1, T2>(NetworkLogFlags flags, T1 t1, T2 t2)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2);
					}
				}
			}
			
			public static void Info<T1, T2>(NetworkLogFlags flags, T1 t1, T2 t2)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2);
					}
				}
			}
			
			public static void Warning<T1, T2>(NetworkLogFlags flags, T1 t1, T2 t2)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2);
					}
				}
			}
			
			public static void Error<T1, T2>(NetworkLogFlags flags, T1 t1, T2 t2)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3);
					}
				}
			}
			
			public static void Info<T1, T2, T3>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3);
					}
				}
			}
			
			public static void Warning<T1, T2, T3>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3);
					}
				}
			}
			
			public static void Error<T1, T2, T3>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5, T6>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5, t6);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5, T6>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5, t6);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5, T6>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5, t6);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5, T6>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5, t6);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5, T6, T7>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5, t6, t7);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5, T6, T7>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5, t6, t7);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5, T6, T7>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5, t6, t7);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5, T6, T7>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5, t6, t7);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5, T6, T7, T8>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5, T6, T7, T8>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
					}
				}
			}
			
		
			public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12)
			{
				if (IsDebugLevel(flags))
				{
					try
					{
						debugWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
					catch (Exception ex)
					{
						_DebugWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
				}
			}
			
			public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12)
			{
				if (IsInfoLevel(flags))
				{
					try
					{
						infoWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
					catch (Exception ex)
					{
						_InfoWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
				}
			}
			
			public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12)
			{
				if (IsWarningLevel(flags))
				{
					try
					{
						warningWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
					catch (Exception ex)
					{
						_WarningWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
				}
			}
			
			public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(NetworkLogFlags flags, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12)
			{
				if (IsErrorLevel(flags))
				{
					try
					{
						errorWriter(flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
					catch (Exception ex)
					{
						_ErrorWriterFailed(ex, flags, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
					}
				}
			}
			
	}

}
