// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using log4net;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// We don't reference ICSharpCode.Core but still need the logging interface.
	/// </summary>
	internal static class LoggingService
	{
		static ILog log = LogManager.GetLogger(typeof(LoggingService));
		
		public static void Debug(object message)
		{
			log.Debug(message);
		}
		
		public static void Info(object message)
		{
			log.Info(message);
		}
		
		public static void Warn(object message)
		{
			log.Warn(message);
		}
		
		public static void Warn(object message, Exception exception)
		{
			log.Warn(message, exception);
		}
		
		public static void Error(object message)
		{
			log.Error(message);
		}
		
		public static void Error(object message, Exception exception)
		{
			log.Error(message, exception);
		}
		
		public static bool IsDebugEnabled {
			get {
				return log.IsDebugEnabled;
			}
		}
	}
}
