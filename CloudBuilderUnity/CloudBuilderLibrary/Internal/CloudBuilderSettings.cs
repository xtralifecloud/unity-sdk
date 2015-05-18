using System;
using System.IO;
using UnityEngine;

namespace CloudBuilderLibrary {
	[Serializable]
	public class CloudBuilderSettings : ScriptableObject {
		public const string AssetPath = "Assets/Resources/CloudBuilderSettings.asset";

		public static CloudBuilderSettings Instance {
			get {
				if (instance == null) {
					instance = Resources.Load(Path.GetFileNameWithoutExtension(AssetPath)) as CloudBuilderSettings;
				}
				return instance;
			}
		}

		private static CloudBuilderSettings instance;

		[SerializeField]
		public string ApiKey;
		[SerializeField]
		public string ApiSecret;
		[SerializeField]
		public string Environment;
		[SerializeField]
		public bool HttpVerbose = true;
		[SerializeField]
		public int HttpTimeout = 60;
		[SerializeField]
		public int EventLoopTimeout = 590;
	}
}
