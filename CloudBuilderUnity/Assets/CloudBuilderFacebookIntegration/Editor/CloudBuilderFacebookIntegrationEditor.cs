using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace CloudBuilderLibrary
{
	[CustomEditor(typeof(CloudBuilderFacebookIntegration))]
	public class CloudBuilderFacebookIntegrationEditor : Editor {

		public override void OnInspectorGUI() {
			// Auto-create the asset on the first time
			var s = CloudBuilderFacebookIntegrationSettings.Instance;
			if (s == null) {
				s = CreateInstance<CloudBuilderFacebookIntegrationSettings>();
				AssetDatabase.CreateAsset(s, CloudBuilderFacebookIntegrationSettings.AssetPath);
				s = CloudBuilderFacebookIntegrationSettings.Instance;
			}

			CloudBuilderFacebookIntegration inst = target as CloudBuilderFacebookIntegration;
			EditorGUILayout.BeginVertical();
			inst.AppId = EditorGUILayout.TextField("Facebook App ID", inst.AppId);
			EditorGUILayout.EndVertical();

			// So that the asset will be saved eventually
			EditorUtility.SetDirty(s);
		}

	}
}
