
namespace CotcSdk
{
	/// <summary>Possible logging levels to be set.</summary>
	public enum LogLevel {
		Verbose,	// Level for verbose comments internal (internal debugging at CotC)
		Info,		// Level for verbose yet public comments, useful for a debug build only
		Warning,	// Level for potentially problematic issues that might cause an unwanted behaviour
		Error,		// Level for critical issues which prevent the library from functioning as expected
	};

	internal interface ILogger
	{
		void Log(LogLevel level, string text);
	}
}
