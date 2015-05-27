using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace IntegrationTests {

	[CustomPropertyDrawer(typeof(InstanceMethod))]
	public class InstanceMethodDrawer : PropertyDrawer {
		private const string SelectMethodMessage = "Please choose a method!";
		private Dictionary<string, Test> methods;

		public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label) {
			Dictionary<string, Test> methods = GetMethodList();
			string[] keys = new string[methods.Keys.Count];
			methods.Keys.CopyTo(keys, 0);

			EditorGUI.BeginChangeCheck();
			string value = keys[
				EditorGUI.Popup(position, "Method to call", IndexInArray(prop.stringValue, keys), keys)
			];

			// Method info
			bool hasChosenMethod = methods.ContainsKey(prop.stringValue);
			string helpMessage = hasChosenMethod ? HelpMessage(methods[value]) : SelectMethodMessage;
			position.height = new GUIStyle(GUI.skin.GetStyle("HelpBox")).CalcHeight(new GUIContent(helpMessage), position.width - 30);
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			if (!hasChosenMethod)
				EditorGUI.HelpBox(position, helpMessage, MessageType.Error);
			else
				EditorGUI.HelpBox(position, helpMessage, methods[value].Requisite == null ? MessageType.Info : MessageType.Warning);
			if (EditorGUI.EndChangeCheck())
				prop.stringValue = value;
		}

		// Called by Unity
		public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) {
			var methods = GetMethodList();
			string helpMessage = methods.ContainsKey(prop.stringValue) ? HelpMessage(methods[prop.stringValue]) : SelectMethodMessage;
			// Popup + help box
			return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
				+ new GUIStyle(GUI.skin.GetStyle("HelpBox")).CalcHeight(new GUIContent(helpMessage), EditorGUIUtility.currentViewWidth - 19 - 30);
		}

		private Dictionary<string, Test> GetMethodList() {
			if (methods != null) return methods;
			return methods = ListTestMethods(((InstanceMethod)attribute).CallerType);
		}

		private string HelpMessage(Test test) {
			if (test == null) return "(no method)";
			if (test.Requisite != null) return test.Description + "\nRequisite: " + test.Requisite;
			return test.Description;
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
		private Dictionary<string, Test> ListTestMethods(Type type) {
			var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			Dictionary<string, Test> matching = new Dictionary<string, Test>();
			foreach (var method in allMethods) {
				var attrs = method.GetCustomAttributes(typeof(Test), false);
				if (attrs == null || attrs.Length == 0) continue;
				matching[method.Name] = (Test)attrs[0];
			}
			return matching;
		}
	}
}
