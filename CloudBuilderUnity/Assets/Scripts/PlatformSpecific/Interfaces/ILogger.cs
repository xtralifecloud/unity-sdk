using System;

namespace CloudBuilderLibrary
{
	internal enum LogLevel {
		Verbose,
		Warning,
		Error,
	};

	internal interface ILogger
	{
		void Log(LogLevel level, string text);
	}
}
