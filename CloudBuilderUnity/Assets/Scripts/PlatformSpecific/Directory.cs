using System;
using UnityEngine;

namespace CloudBuilderLibrary
{
	internal static class Directory
	{
		static Directory() {
			HttpClient = new UnityHttpClient();
			Logger = UnityLogger.Instance;
			SystemFunctions = new UnitySystemFunctions();
		}
		
		internal static ILogger Logger {
			get;
			private set;
		}

		internal static IHttpClient HttpClient {
			get;
			private set;
		}

		internal static ISystemFunctions SystemFunctions {
			get;
			private set;
		}
	}
}

