using System;
using UnityEngine;

namespace IntegrationTests {
	public class InstanceMethod : PropertyAttribute {
		public Type CallerType;
		public InstanceMethod(Type type) { CallerType = type; }
	}
}
