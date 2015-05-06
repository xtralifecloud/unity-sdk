using System;
using UnityEngine;

namespace CloudBuilderLibrary
{
	class CloudBuilderGameObject : MonoBehaviour {

		public string ApiKey;
		public string ApiSecret;
		public string Environment = CloudBuilder.SandboxEnvironment;
		public string FacebookAppId = null;
		public bool HttpVerbose = false;
		public int HttpTimeout = 60;
		public int EventLoopTimeout = 590;
		private static Clan clan = null;

		public Clan Clan {
			get { return clan; }
		}

		void Start() {
			CloudBuilder.TEMP("Init'ed cloud builder game object");

			// No need to initialize it once more
			if (clan != null) {
				return;
			}

			if (string.IsNullOrEmpty(FacebookAppId)) {
				CloudBuilder.Log(LogLevel.Info, "No facebook credential. Facebook will be unavailable.");
			}
			else {
				// TODO make the setup to wait on this operation (a chain of initialization callbacks for example)
				FB.Init(() => Debug.Log("FB inited"), FacebookAppId);
			}

			CloudBuilder.Setup((Result<Clan> result) => {
				clan = result.Value;
				CloudBuilder.Log("CloudBuilder inited");
			}, ApiKey, ApiSecret, Environment, HttpVerbose, HttpTimeout, EventLoopTimeout);
		}

	}

}
