using System;
using UnityEditor;
using UnityEngine;

public class InstanceMethodAttribute : PropertyAttribute {
	public Type CallerType;
	public InstanceMethodAttribute(Type type) { CallerType = type; }
}
