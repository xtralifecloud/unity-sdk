using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(InstanceMethodAttribute))]
public class InstanceMethodDrawer : PropertyDrawer {
    // Provide easy access to the RegexAttribute for reading information from it.
    InstanceMethodAttribute instMethod { get { return ((InstanceMethodAttribute)attribute); } }

    // Here you can define the GUI for your property drawer. Called by Unity.
    public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label) {
		InstanceMethodAttribute attrs = (InstanceMethodAttribute)attribute;
		var mbs = attrs.CallerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		string[] keys = new string[mbs.Length];
		for (int i = 0; i < mbs.Length; i++) {
			keys[i] = mbs[i].Name;
		}
		EditorGUI.BeginChangeCheck();
		string value = keys[
			EditorGUI.Popup(position, "Method to call", IndexInArray(prop.stringValue, keys), keys)
		];
		if (EditorGUI.EndChangeCheck())
			prop.stringValue = value;
	}

	private int IndexInArray(string value, string[] choices, int defaultChoice = 0) {
		for (int i = 0; i < choices.Length; i++) {
			if (value == choices[i]) {
				return i;
			}
		}
		return defaultChoice;
	}
}
