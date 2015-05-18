using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace IntegrationTests {

	[CustomPropertyDrawer(typeof(InstanceMethod))]
	public class InstanceMethodDrawer : PropertyDrawer {
		private const string SelectMethodMessage = "(Please choose a method)";
		private Dictionary<string, string> methods;

		public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label) {
			Dictionary<string, string> methods = GetMethodList();
			string[] keys = new string[methods.Keys.Count];
			methods.Keys.CopyTo(keys, 0);

			EditorGUI.BeginChangeCheck();
			string value = keys[
				EditorGUI.Popup(position, "Method to call", IndexInArray(prop.stringValue, keys), keys)
			];

			// Method info
			string helpMessage = methods.ContainsKey(value) ? methods[value] : SelectMethodMessage;
			position.height = new GUIStyle(GUI.skin.GetStyle("HelpBox")).CalcHeight(new GUIContent(helpMessage), position.width - 20);
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.HelpBox(position, helpMessage, MessageType.Info);
			if (EditorGUI.EndChangeCheck())
				prop.stringValue = value;
		}

		// Called by Unity
		public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) {
			var methods = GetMethodList();
			string helpMessage = methods.ContainsKey(prop.stringValue) ? methods[prop.stringValue] : SelectMethodMessage;
			// Popup + help box
			return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
				+ new GUIStyle(GUI.skin.GetStyle("HelpBox")).CalcHeight(new GUIContent(helpMessage), EditorGUIUtility.currentViewWidth - 19 - 20);
		}

		private Dictionary<string, string> GetMethodList() {
			if (methods != null) return methods;
			return methods = ListTestMethods(((InstanceMethod)attribute).CallerType);
		}

		private int IndexInArray(string value, string[] choices, int defaultChoice = 0) {
			for (int i = 0; i < choices.Length; i++) {
				if (value == choices[i]) {
					return i;
				}
			}
			return defaultChoice;
		}

		// Method name -> description
		private Dictionary<string, string> ListTestMethods(Type type) {
			var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			Dictionary<string, string> matching = new Dictionary<string, string>();
			foreach (var method in allMethods) {
				var attrs = method.GetCustomAttributes(typeof(Test), false);
				if (attrs == null || attrs.Length == 0) continue;
				matching[method.Name] = ((Test)attrs[0]).Description;
			}
			return matching;
		}
	}
}
