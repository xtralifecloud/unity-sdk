using System;
using System.Collections.Generic;
using CotcSdk;
using UnityEngine;

public class TestUtilities : MonoBehaviour {
	private Dictionary<string, string> GeneratedIds = new Dictionary<string,string>();

	void Start() {
	}

	/**
	 * Generates an ID that is unique for this test run AND prefix.
	 * In other words, you may call this method several times with the same prefix to get the
	 * same ID. It is useful in order to reuse identifiers among tests and avoid surcharging
	 * the database.
	 * The prefix is prepended to the generated unique ID, which makes around ten characters.
	 */
	public string GetAllTestScopedId(string prefix) {
		if (GeneratedIds.ContainsKey(prefix)) {
			return GeneratedIds[prefix];
		}
		return GeneratedIds[prefix] = prefix + Guid.NewGuid().ToString();
	}
}
