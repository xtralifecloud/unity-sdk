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
			CloudBuilderFacebookIntegration inst = target as CloudBuilderFacebookIntegration;
			EditorGUILayout.BeginVertical();
			inst.AppId = EditorGUILayout.TextField("Facebook App ID", inst.AppId);
			EditorGUILayout.EndVertical();
		}

	}
}
