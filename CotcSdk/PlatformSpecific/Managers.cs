
using System;
namespace CotcSdk
{
	internal static class Managers
	{
		static Managers() {
			MonoHttpClient = new MonoHttpClient();
			UnityHttpClient = new UnityHttpClientV2();
			Logger = UnityLogger.Instance;
			SystemFunctions = new UnitySystemFunctions();
			SetHttpClientType(0);
		}
		
		internal static ILogger Logger {
			get;
			private set;
		}

		internal static HttpClient HttpClient {
			get;
			private set;
		}

		internal static ISystemFunctions SystemFunctions {
			get;
			private set;
		}

		public static void SetHttpClientType(int type) {
			switch (type) {
				case 0: HttpClient = MonoHttpClient; break;
				case 1: HttpClient = UnityHttpClient; break;
				default: throw new ArgumentException("Invalid HTTP client type. Must be between 0 and 1.");
			}
		}

		private static HttpClient MonoHttpClient, UnityHttpClient;
	}
}

