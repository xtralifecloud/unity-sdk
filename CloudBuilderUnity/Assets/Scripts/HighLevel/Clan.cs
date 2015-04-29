using System;
using System.Text;

namespace CloudBuilderLibrary
{
	public class Clan {
		/**
		 * Call this at the very beginning to start using the library.
		 * @param whenDone called when the process has finished (most likely synchronously).
		 * @param configuration may contain the following:
			The mandatory keys are:
			 - "key": string containing the community key.
			 - "secret": string containing the community secret.
			 - "env": string containing the environment.
			 - "appVersion": string containing the application version.

			The optional keys are:
			- "gamecenter": boolean to control if the application can create profiles
			  linked to GameCenter ID.
			- "facebook": boolean to control if the application can create profiles
			  linked to Facebook.
			- "googleplus": boolean to control if the application can create profiles
			  linked to GooglePlus.
			- "appFolder": used with the default CFileSystemHandler to set the folder in which
			  the data will be saved. On Windows, it would be %USERPROFILE%\AppData\Roaming\<appFolder>\.
			- "autoresume" : boolean to control if after a Setup the system has to proceed an automatic resumesession when possible.
			- "autoRegisterForNotification": by default, RegisterForNotification is called right after login.
			  By setting this key to 'false', you can manage when you want to register for notifications by
			  calling the CUserManager::RegisterForNotification at your convenience.
			- "connectTimeout": sets a custom timeout allowed when connecting to the servers. Defaults to 5.
			- "httpTimeout": sets a custom timeout for all requests. Defaults to no limit.
			- "eventLoopTimeout": sets a custom timeout for the long polling event loop. Should be used with care and set to a
			  high value (at least 60). Defaults to 590.
			- "httpVerbose": set to true to output detailed information about the requests performed to CotC servers. Can be used
			  for debugging, though it will pollute the logs very much.
		 */
		public void Setup(ResultHandler whenDone, Bundle configuration) {
			apiKey = configuration.GetString("key");
			apiSecret = configuration.GetString("secret");
			// TODO support predefined constants
			server = configuration.GetString("env");
			LoadBalancerCount = 2;
			if (apiKey == null || apiSecret == null) {
				throw new ArgumentException("key, secret required");
			}

			CloudBuilder.HttpClient.VerboseMode = configuration.GetBool("httpVerbose");
			httpTimeoutMillis = configuration.GetLong("httpTimeout", defaultTimeoutMillis);
			sdkVersion = configuration.GetString("sdkVersion");
			Common.InvokeHandler(whenDone, ErrorCode.enNoErr);
		}

		/**
		 * Logs the current user in anonymously.
		 * @param whenDone callback invoked when the login has finished, either successfully or not.
		 * TODO document the result
		 * TODO fail if not initialized properly or not logged in etc.
		 * @param configuration not used at the moment, pass Bundle.Empty.
		 */
		public void LoginAnonymous(ResultHandler whenDone, Bundle configuration) {
			Bundle config = Bundle.CreateObject();
			config["device"] = CloudBuilder.SystemFunctions.CollectDeviceInformation();

			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			req.TimeoutMillisec = httpTimeoutMillis;
			CloudBuilder.HttpClient.Run(req, (HttpResponse response) => {
				CloudResult result = new CloudResult(response);
				if (!response.HasFailed) {
					gamerId = response.BodyJson.GetString("gamer_id");
					gamerSecret = response.BodyJson.GetString("gamer_secret");
					CloudBuilder.Log("Login successful! Welcome " +  gamerId);
				}
				Common.InvokeHandler(whenDone, result);
			});
		}

		public void TEMP_GetUserProfile(ResultHandler whenDone, Bundle configuration) {
			if (!RequireLoggedIn(whenDone)) return;

			HttpRequest req = MakeHttpRequest("/v1/gamer/profile");
			CloudBuilder.HttpClient.Run(req, (HttpResponse response) => {
				Common.InvokeHandler(whenDone, response);
			});
		}

		#region Internal HTTP helpers
		internal HttpRequest MakeHttpRequest(string path) {
			HttpRequest result = MakeUnauthenticatedHttpRequest(path);
			string authInfo = gamerId + ":" + gamerSecret;
			result.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			return result;
		}

		internal HttpRequest MakeUnauthenticatedHttpRequest(string path) {
			HttpRequest result = new HttpRequest();
			result.Url = server + path;
			result.Headers["x-apikey"] = apiKey;
			result.Headers["x-sdkversion"] = sdkVersion;
			result.Headers["x-apisecret"] = apiSecret;
            return result;
        }
        #endregion

		#region Internal
		internal Clan() {}

		internal bool IsLogged {
			get { return gamerId != null && gamerSecret != null; }
		}

		internal bool IsSetup {
			get { return apiKey != null && apiSecret != null; }
		}

		/** Call this if your method requires to be logged in. Returns false and calls the delegate if not. */
		internal bool RequireLoggedIn(ResultHandler calledInCaseOfError) {
			if (!IsSetup) {
				Common.InvokeHandler(calledInCaseOfError, ErrorCode.enSetupNotCalled);
				return false;
			}
			if (!IsLogged) {
				Common.InvokeHandler(calledInCaseOfError, ErrorCode.enNotLogged);
				return false;
			}
			return true;
		}
        #endregion

		#region Members
		private const long defaultTimeoutMillis = 60 * 1000;
		private string apiKey, apiSecret, sdkVersion, server;
		private long httpTimeoutMillis;
		public int LoadBalancerCount;
		public string UserAgent = "TEMP-TODO-UA";
		// About the logged in user
		private string gamerId, gamerSecret;
        #endregion
	}
}
