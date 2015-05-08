using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CloudBuilderLibrary {
	[Serializable]
	public class CloudBuilderSettings : ScriptableObject {
		public static CloudBuilderSettings Instance {
			get {
				if (instance == null) {
					instance = AssetDatabase.LoadAssetAtPath("Assets/Resources/CloudBuilderSettings.asset", typeof(CloudBuilderSettings)) as CloudBuilderSettings;
					if (instance == null) {
#if UNITY_EDITOR
						instance = CreateInstance<CloudBuilderSettings>();
						AssetDatabase.CreateAsset(instance, "Assets/Resources/CloudBuilderSettings.asset");
#endif
					}
				}
				return instance;
			}
		}

		public string ApiKey {
			get { return apiKey; }
			set {
				apiKey = value;
				EditorUtility.SetDirty(Instance);
			}
		}
		public string ApiSecret {
			get { return apiSecret; }
			set {
				apiSecret = value;
				EditorUtility.SetDirty(Instance);
			}
		}
		public string Environment {
			get { return environment; }
			set {
				environment = value;
				EditorUtility.SetDirty(Instance);
			}
		}

		public bool HttpVerbose {
			get { return httpVerbose; }
			set {
				httpVerbose = value;
				EditorUtility.SetDirty(Instance);
			}
		}
		public int HttpTimeout {
			get { return httpTimeout; }
			set {
				httpTimeout = value;
				EditorUtility.SetDirty(Instance);
			}
		}
		public int EventLoopTimeout {
			get { return eventLoopTimeout; }
			set {
				eventLoopTimeout = value;
				EditorUtility.SetDirty(Instance);
			}
		}

		private static CloudBuilderSettings instance;

		[SerializeField]
		private string apiKey;
		[SerializeField]
		private string apiSecret;
		[SerializeField]
		private string environment;
		[SerializeField]
		private bool httpVerbose = true;
		[SerializeField]
		private int httpTimeout = 60;
		[SerializeField]
		private int eventLoopTimeout = 590;
	}
}
