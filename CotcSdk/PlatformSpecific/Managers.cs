
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
			SetHttpClientParams(0, false);
		}

		internal static ILogger Logger {
			get;
			private set;
		}

		/// <summary>
		/// Designates the HTTP default client, as selected by the user (by calling SetHttpClientType, or
		/// publicly via the CotC game object settings or Cloud Setup).
		/// </summary>
		internal static HttpClient HttpClient {
			get;
			private set;
		}

		internal static ISystemFunctions SystemFunctions {
			get;
			private set;
		}

		/// <summary>Allows to specifically direct a request to the Unity HTTP Client, while using the
		/// default client for other requests. (The two can run in parallel).</summary>
		internal static UnityHttpClientV2 UnityHttpClient {
			get;
			private set;
		}

		/// <summary>Defines the type of HTTP client./// </summary>
		/// <param name="type">0 = HttpWebRequest, 1 = UnityWebRequest.</param>
		/// <param name="verboseMode">true to enable verbose logging of every request, false otherwise.</param>
		public static void SetHttpClientParams(int type, bool verboseMode) {
			switch (type) {
				case 0: HttpClient = MonoHttpClient; break;
				case 1: HttpClient = UnityHttpClient; break;
				default: throw new ArgumentException("Invalid HTTP client type. Must be between 0 and 1.");
			}
			HttpClient.VerboseMode = verboseMode;
		}

		private static HttpClient MonoHttpClient;
	}
}

