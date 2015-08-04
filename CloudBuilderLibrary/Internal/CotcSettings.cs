using System;
using System.IO;
using UnityEngine;

namespace CotcSdk {

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

		[SerializeField]
		public string ApiKey;
		[SerializeField]
		public string ApiSecret;
		[SerializeField]
		public string Environment;
		[SerializeField]
		public int LbCount = 1;
		[SerializeField]
		public bool HttpVerbose = true;
		[SerializeField]
		public int HttpTimeout = 60;
	}
}
