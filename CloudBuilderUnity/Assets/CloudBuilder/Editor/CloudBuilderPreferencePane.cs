using System;
using UnityEditor;
using UnityEngine;

namespace CloudBuilderLibrary
{
	[CustomEditor(typeof(CloudBuilderSettings))]
	public class CloudBuilderPreferencePane : Editor {
		private bool FacebookGroupEnabled = true;
		private bool HttpGroupEnabled = false;

		public override void OnInspectorGUI() {
			CloudBuilderSettings s = (CloudBuilderSettings)target;
		
			GUILayout.Label("Base Settings", EditorStyles.boldLabel);
			s.ApiKey = EditorGUILayout.TextField("API Key", s.ApiKey);
			s.ApiSecret = EditorGUILayout.TextField("API Secret", s.ApiSecret);

			EditorGUILayout.GetControlRect(true, 16f, EditorStyles.foldout);
			FacebookGroupEnabled = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(), FacebookGroupEnabled, "Facebook Settings");
			if (FacebookGroupEnabled) {
				EditorGUI.indentLevel++;
				s.FacebookAppId = EditorGUILayout.TextField("App ID", s.FacebookAppId);
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.GetControlRect(true, 16f, EditorStyles.foldout);
			HttpGroupEnabled = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(), FacebookGroupEnabled, "Network Connection Settings");
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
	}

}


