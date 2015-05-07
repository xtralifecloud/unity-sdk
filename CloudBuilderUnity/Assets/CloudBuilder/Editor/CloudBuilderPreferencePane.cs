using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CloudBuilderLibrary
{
	public class CloudBuilderPreferencePane : EditorWindow {
		private Dictionary<string, string> PredefinedEnvironments = new Dictionary<string,string>() {
			{"Sandbox", "https://sandbox-api[id].clanofthecloud.mobi"},
			{"Production", "https://prod-api[id].clanofthecloud.mobi"},
			{"Dev Server", "http://195.154.227.44:8000"},
			{"Local Server", "http://127.0.0.1:3000"},
		};
		private bool HttpGroupEnabled = false;

		[MenuItem("Window/CloudBuilder settings")]
		static void ShowWindow() {
			EditorWindow.GetWindow<CloudBuilderPreferencePane>().Show();
		}

		void OnGUI() {
			CloudBuilderSettings s = CloudBuilderSettings.Instance;
		
			GUILayout.Label("CloudBuilder Library Settings", EditorStyles.boldLabel);
			s.ApiKey = EditorGUILayout.TextField("API Key", s.ApiKey);
			s.ApiSecret = EditorGUILayout.PasswordField("API Secret", s.ApiSecret);
			string[] keys = new string[PredefinedEnvironments.Keys.Count];
			PredefinedEnvironments.Keys.CopyTo(keys, 0);
			s.Environment = PredefinedEnvironments[
				keys[
					EditorGUILayout.Popup("Environment", IndexInDict(s.Environment, PredefinedEnvironments), keys)
				]
			];

			EditorGUILayout.GetControlRect(true, 16f, EditorStyles.foldout);
			HttpGroupEnabled = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(), HttpGroupEnabled, "Network Connection Settings");
			if (HttpGroupEnabled) {
				int tmpInt;
				EditorGUI.indentLevel++;
				s.HttpVerbose = EditorGUILayout.Toggle("Verbose logging", s.HttpVerbose);
				if (int.TryParse(EditorGUILayout.TextField("Request timeout (sec)", s.HttpTimeout.ToString()), out tmpInt)) {
					s.HttpTimeout = tmpInt;
				}
				if (int.TryParse(EditorGUILayout.TextField("Event loop iteration (sec)", s.EventLoopTimeout.ToString()), out tmpInt)) {
					s.EventLoopTimeout = tmpInt;
				}
				EditorGUI.indentLevel--;
			}
		}

		private int IndexInDict(string value, Dictionary<string, string> choices, int defaultChoice = 0) {
			int index = 0;
			foreach (string v in choices.Values) {
				if (value == v) {
					return index;
				}
				index++;
			}
			return defaultChoice;
		}
	}

}


