using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class GamerTests : MonoBehaviour {

	[InstanceMethod(typeof(GamerTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("Sets a property and check that it worked properly (tests read all & write single).")]
	public void ShouldSetProperty(Clan clan) {
		Login(clan, gamer => {
			// Set property, then get all and check it
			gamer.Properties().Set(
				key: "testkey",
				value: "value",
				done: setResult => {
					if (!setResult.IsSuccessful) IntegrationTest.Fail("Error when setting property");

					gamer.Properties().GetAll(getResult => {
						IntegrationTest.Assert(getResult.IsSuccessful);
						IntegrationTest.Assert(getResult.Value.Has("testkey"));
					});
				}
			);
		});
	}

	#region Private helpers
	private void Login(Clan clan, Action<Gamer> done) {
		clan.Login(
			network: LoginNetwork.Email,
			networkId: "clan@localhost.localdomain",
			networkSecret: "Password123",
			done: result => {
				if (!result.IsSuccessful) IntegrationTest.Fail("Failed to log in");
				done(result.Value);
			}
		);
	}
	#endregion
}
