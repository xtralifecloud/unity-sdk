using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CotcSdk
{
	/** @cond private */
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
		private bool HttpGroupEnabled = true, NeedsInitialization = true, PresetGroupEnabled = true;
		private readonly string[] SupportedHttpClients = { "Mono (System.Net.HttpWebRequest)", "Unity (UnityEngine.Experimental.Networking.UnityWebRequest)" };

		public override void OnInspectorGUI() {
			// Auto-create the asset on the first time
			CotcSettings s = CotcSettings.Instance;
			CotcSettings.Environment ce;
			if (s == null) {
				string properPath = Path.Combine(Application.dataPath, Path.GetDirectoryName(CotcSettings.AssetPath));
                if (!Directory.Exists(properPath)) {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
				s = CreateInstance<CotcSettings>();
				AssetDatabase.CreateAsset(s, CotcSettings.AssetPath);
				s = CotcSettings.Instance;
			}
			// Ensure there is at least one environment
			if (s.Environments.Count == 0) {
				var env = new CotcSettings.Environment() {
					Name = "Default"
				};
				s.Environments.Add(env);
			}
			// Initially unroll the preset group if more than one preset are configured
			if (NeedsInitialization) {
				NeedsInitialization = false;
				PresetGroupEnabled = s.Environments.Count > 1;
			}

			GUILayout.Label("CotC SDK Settings (v" + Cloud.SdkVersion + ")", EditorStyles.boldLabel);
			GUILayout.Label("These are global to all scenes in your project (stored under Assets/Resources/).");
			GUI.skin.label.wordWrap = true;
			GUILayout.Space(5);

			EditorGUILayout.GetControlRect(true, 16f, EditorStyles.foldout);
			PresetGroupEnabled = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(), PresetGroupEnabled, "Predefined parameter sets");
			if (PresetGroupEnabled) {
				EditorGUI.indentLevel++;
				// Show the predefined environment combo
				string[] presets = new string[s.Environments.Count + 1];
				int index = 0;
				foreach (var env in s.Environments) {
					presets[index] = (index + 1) + ": " + env.Name;
					index++;
				}
				presets[index] = "Add new...";

				s.SelectedEnvironment = EditorGUILayout.Popup("Preset", s.SelectedEnvironment, presets);
				// Add new entry was selected
				if (s.SelectedEnvironment == s.Environments.Count) {
					var env = new CotcSettings.Environment() {
						Name = "New preset"
					};
					s.Environments.Add(env);
				}

				ce = s.Environments[s.SelectedEnvironment];
				GUILayout.BeginHorizontal();
				ce.Name = EditorGUILayout.TextField("Preset name", ce.Name);
				GUI.enabled = s.Environments.Count > 1;
				if (GUILayout.Button("Remove", GUILayout.Width(80))) {
					s.Environments.RemoveAt(s.SelectedEnvironment);
					s.SelectedEnvironment = Math.Max(0, s.SelectedEnvironment - 1);
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
			else {
				ce = s.Environments[s.SelectedEnvironment];
			}

			// Now show standard params
			ce.ApiKey = EditorGUILayout.TextField("API Key", ce.ApiKey);
			ce.ApiSecret = EditorGUILayout.TextField("API Secret", ce.ApiSecret);

			// Provide default sandbox environment
			if (string.IsNullOrEmpty(ce.ServerUrl)) {
				ce.ServerUrl = PredefinedEnvironments["Sandbox"].Url;
				ce.LbCount = PredefinedEnvironments["Sandbox"].LbCount;
			}

			var keys = new string[PredefinedEnvironments.Keys.Count];
			var comparison = new EnvironmentInfo(ce.ServerUrl, ce.LbCount);
			PredefinedEnvironments.Keys.CopyTo(keys, 0);
			int currentIndex = IndexInDict(comparison, PredefinedEnvironments);
			int newIndex = EditorGUILayout.Popup("Environment", currentIndex, keys);
			// Custom env
			if (newIndex == 0) {
				if (currentIndex != 0) ce.ServerUrl = "http://";
				ce.ServerUrl = EditorGUILayout.TextField("Env. URL", ce.ServerUrl);
				ce.LbCount = 0;
			}
			else {
				// Predefined env.
				var env = PredefinedEnvironments[keys[newIndex]];
				ce.ServerUrl = env.Url;
				ce.LbCount = env.LbCount;
			}

			EditorGUILayout.GetControlRect(true, 16f, EditorStyles.foldout);
			HttpGroupEnabled = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(), HttpGroupEnabled, "Network Connection Settings");
			if (HttpGroupEnabled) {
				int tmpInt;
				EditorGUI.indentLevel++;
				ce.HttpVerbose = EditorGUILayout.Toggle("Verbose logging", ce.HttpVerbose);
				if (int.TryParse(EditorGUILayout.TextField("Request timeout (sec)", ce.HttpTimeout.ToString()), out tmpInt)) {
					ce.HttpTimeout = tmpInt;
				}
				ce.HttpClientType = EditorGUILayout.Popup("HTTP client", ce.HttpClientType, SupportedHttpClients);
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
	/** @endcond */
}
