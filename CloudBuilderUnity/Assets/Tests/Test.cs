using System;
using UnityEditor;
using UnityEngine;

namespace IntegrationTests {
	public class Test : Attribute {
		public string Description, Requisite;
		public Test(string description, string requisite = null) { Description = description; Requisite = requisite; }
	}
}
