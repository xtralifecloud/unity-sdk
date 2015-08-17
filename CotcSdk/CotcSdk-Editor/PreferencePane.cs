using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CotcSdk
{
	[CustomEditor(typeof(CotcGameObject))]
	public class PreferencePane : Editor {
		private class EnvironmentInfo {
			public string Url;
			public int LbCount;
			public EnvironmentInfo(string url, int lbCount) {
				Url = url;
				LbCount = lbCount;
			}
			public override bool Equals(object obj) { return Url == ((EnvironmentInfo)obj).Url; }
			public override int GetHashCode() { return Url.GetHashCode(); }
		}
		private Dictionary<string, EnvironmentInfo> PredefinedEnvironments = new Dictionary<string,EnvironmentInfo>() {
			{"Custom...", new EnvironmentInfo("", 0)},
			{"Sandbox", new EnvironmentInfo("https://sandbox-api[id].clanofthecloud.mobi", 2)},
			{"Production", new EnvironmentInfo("https://prod-api[id].clanofthecloud.mobi", 16)},
#if DEBUG
			{"Dev Server", new EnvironmentInfo("http://195.154.227.44:8000", 0)},
			{"Local Server", new EnvironmentInfo("http://127.0.0.1:3000", 0)},
			{"Parallels VM", new EnvironmentInfo("http://10.211.55.2:2000", 0)},
#endif
		};
		private bool HttpGroupEnabled = true;

		public override void OnInspectorGUI() {
			// Auto-create the asset on the first time
			CotcSettings s = CotcSettings.Instance;
			if (s == null) {
				string properPath = Path.Combine(Application.dataPath, Path.GetDirectoryName(CotcSettings.AssetPath));
                if (!Directory.Exists(properPath)) {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
				s = CreateInstance<CotcSettings>();
				AssetDatabase.CreateAsset(s, CotcSettings.AssetPath);
				s = CotcSettings.Instance;
			}

			GUILayout.Label("CotC SDK Settings", EditorStyles.boldLabel);
			GUILayout.Label("These are global to all scenes in your project (stored under Assets/Resources/).");
			GUI.skin.label.wordWrap = true;
			GUILayout.Space(5);

			s.ApiKey = EditorGUILayout.TextField("API Key", s.ApiKey);
			s.ApiSecret = EditorGUILayout.TextField("API Secret", s.ApiSecret);

			// Provide default sandbox environment
			if (string.IsNullOrEmpty(s.Environment)) {
				s.Environment = PredefinedEnvironments["Sandbox"].Url;
				s.LbCount = PredefinedEnvironments["Sandbox"].LbCount;
			}

			var keys = new string[PredefinedEnvironments.Keys.Count];
			var comparison = new EnvironmentInfo(s.Environment, s.LbCount);
			PredefinedEnvironments.Keys.CopyTo(keys, 0);
			int currentIndex = IndexInDict(comparison, PredefinedEnvironments);
			int newIndex = EditorGUILayout.Popup("Environment", currentIndex, keys);
			// Custom env
			if (newIndex == 0) {
				if (currentIndex != 0) s.Environment = "http://";
				s.Environment = EditorGUILayout.TextField("Env. URL", s.Environment);
				s.LbCount = 0;
			}
			else {
				// Predefined env.
				var env = PredefinedEnvironments[keys[newIndex]];
				s.Environment = env.Url;
				s.LbCount = env.LbCount;
			}

			EditorGUILayout.GetControlRect(true, 16f, EditorStyles.foldout);
			HttpGroupEnabled = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(), HttpGroupEnabled, "Network Connection Settings");
			if (HttpGroupEnabled) {
				int tmpInt;
				EditorGUI.indentLevel++;
				s.HttpVerbose = EditorGUILayout.Toggle("Verbose logging", s.HttpVerbose);
				if (int.TryParse(EditorGUILayout.TextField("Request timeout (sec)", s.HttpTimeout.ToString()), out tmpInt)) {
					s.HttpTimeout = tmpInt;
				}
				EditorGUI.indentLevel--;
			}

			// So that the asset will be saved eventually
			EditorUtility.SetDirty(s);
		}

		private int IndexInDict<T>(T value, Dictionary<string, T> choices, int defaultChoice = 0) {
			int index = 0;
			foreach (T v in choices.Values) {
				if (v.Equals(value)) {
					return index;
				}
				index++;
			}
			return defaultChoice;
		}
	}
}


