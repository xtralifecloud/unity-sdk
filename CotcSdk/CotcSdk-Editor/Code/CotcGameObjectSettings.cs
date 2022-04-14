// To disable this CustomEditor when building to target platforms from Unity but enable it when in Unity editor or building the DLL from Visual Studio
#if UNITY_EDITOR || COTC_DLL_BUILD
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
			public EnvironmentInfo(string url) {
				Url = url;
			}
			public override bool Equals(object obj) { return Url == ((EnvironmentInfo)obj).Url; }
			public override int GetHashCode() { return Url.GetHashCode(); }
		}

		private bool HttpGroupEnabled = true, NeedsInitialization = true, PresetGroupEnabled = true;
		private readonly string[] SupportedHttpClients = { "Mono (System.Net.HttpWebRequest)", "Unity (UnityEngine.Networking.UnityWebRequest)" };

		public override void OnInspectorGUI() {
			// Auto-create the asset on the first time
			CotcSettings s = CotcSettings.Instance;
			CotcSettings.Environment ce;
			if (s == null) {
				if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(CotcSettings.AssetPath))) {
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
			ce.ServerUrl = EditorGUILayout.TextField("Env. URL", ce.ServerUrl);

			EditorGUILayout.GetControlRect(true, 16f, EditorStyles.foldout);
			HttpGroupEnabled = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(), HttpGroupEnabled, "Network Connection Settings");
			if (HttpGroupEnabled) {
				int tmpInt;
				EditorGUI.indentLevel++;
				ce.HttpVerbose = EditorGUILayout.Toggle("Verbose logging", ce.HttpVerbose);
				if (int.TryParse(EditorGUILayout.TextField("Request timeout (sec)", ce.HttpTimeout.ToString()), out tmpInt)) {
					ce.HttpTimeout = tmpInt;
				}
				ce.HttpUseCompression = EditorGUILayout.Toggle("HTTP compression", ce.HttpUseCompression);
				
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
#endif
