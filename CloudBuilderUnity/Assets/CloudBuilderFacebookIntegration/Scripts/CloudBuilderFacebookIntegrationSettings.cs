using System;
using System.IO;
using UnityEngine;

namespace CloudBuilderLibrary {
	[Serializable]
	public class CloudBuilderFacebookIntegrationSettings : ScriptableObject {
		public const string AssetPath = "Assets/Resources/CloudBuilderFacebookSettings.asset";

		public static CloudBuilderFacebookIntegrationSettings Instance {
			get {
				if (instance == null) {
					instance = Resources.Load(Path.GetFileNameWithoutExtension(AssetPath)) as CloudBuilderFacebookIntegrationSettings;
				}
				return instance;
			}
		}

		private static CloudBuilderFacebookIntegrationSettings instance;

		[SerializeField]
		public string AppId;
	}
}
