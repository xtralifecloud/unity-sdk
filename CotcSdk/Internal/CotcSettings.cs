using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CotcSdk {

	/** @cond private */
	[Serializable]
	public class CotcSettings : ScriptableObject {
		public const string AssetPath = "Assets/Resources/CotcSettings.asset";

		public static CotcSettings Instance {
			get {
				if (instance == null) {
					instance = Resources.Load(Path.GetFileNameWithoutExtension(AssetPath)) as CotcSettings;
				}
				return instance;
			}
		}

		private static CotcSettings instance;

		[Serializable]
		public class Environment {
			[SerializeField]
			public string Name;
			[SerializeField]
			public string ApiKey;
			[SerializeField]
			public string ApiSecret;
			[SerializeField]
			public string ServerUrl;
			[SerializeField]
			public int LbCount = 1;
			[SerializeField]
			public bool HttpVerbose = true;
			[SerializeField]
			public int HttpTimeout = 60;
			[SerializeField]
			public int HttpClientType = 0;
		}

		[SerializeField]
		public int FileVersion = 2;
		[SerializeField]
		public List<Environment> Environments = new List<Environment>();
		[SerializeField]
		public int SelectedEnvironment = 0;
	}
	/** @endcond */
}
