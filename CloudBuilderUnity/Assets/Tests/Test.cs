using System;
using UnityEditor;
using UnityEngine;

namespace IntegrationTests {
	public class Test : Attribute {
		public string Description;
		public Test(string description) { Description = description; }
	}
}
