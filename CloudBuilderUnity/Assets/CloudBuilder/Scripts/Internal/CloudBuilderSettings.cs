using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CloudBuilderLibrary {
	public class CloudBuilderSettings : ScriptableObject {

		public static CloudBuilderSettings Instance {
			get {
				return instance = instance ?? CreateInstance<CloudBuilderSettings>();
			}
		}
		public string ApiKey {
			get { return EditorPrefs.GetString("CloudBuilder.ApiKey"); }
			set { EditorPrefs.SetString("CloudBuilder.ApiKey", value); }
		}
		public string ApiSecret {
			get { return EditorPrefs.GetString("CloudBuilder.ApiSecret"); }
			set { EditorPrefs.SetString("CloudBuilder.ApiSecret", value); }
		}
		public string Environment {
			get { return EditorPrefs.GetString("CloudBuilder.Environment", CloudBuilder.SandboxEnvironment); }
			set { EditorPrefs.SetString("CloudBuilder.Environment", value); }
		}

		public string FacebookAppId {
			get { return EditorPrefs.GetString("CloudBuilder.FacebookAppId"); }
			set { EditorPrefs.SetString("CloudBuilder.FacebookAppId", value); }
		}

		public bool HttpVerbose {
			get { return EditorPrefs.GetBool("CloudBuilder.HttpVerbose"); }
			set { EditorPrefs.SetBool("CloudBuilder.HttpVerbose", value); }
		}
		public int HttpTimeout {
			get { return EditorPrefs.GetInt("CloudBuilder.HttpTimeout", 60); }
			set { EditorPrefs.SetInt("CloudBuilder.HttpTimeout", value); }
		}
		public int EventLoopTimeout {
			get { return EditorPrefs.GetInt("CloudBuilder.EventLoopTimeout", 590); }
			set { EditorPrefs.SetInt("CloudBuilder.EventLoopTimeout", value); }
		}
		private static CloudBuilderSettings instance;
	
		[MenuItem("Window/CloudBuilder settings")]
		public static void ActivateSettings() {
			Selection.activeObject = Instance;
		}
	}
}
