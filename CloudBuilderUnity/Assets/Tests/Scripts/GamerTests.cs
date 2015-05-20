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

	[Test("Sets a property and checks that it worked properly (tests read all & write single).")]
	public void ShouldSetProperty(Clan clan) {
		Login(clan, gamer => {
			// Set property, then get all and check it
			gamer.Properties().SetKey(
				key: "testkey",
				value: "value",
				done: setResult => {
					if (!setResult.IsSuccessful) IntegrationTest.Fail("Error when setting property");
					if (setResult.Value != 1) IntegrationTest.Fail("Expected done = 1");

					gamer.Properties().GetAll(getResult => {
						IntegrationTest.Assert(getResult.IsSuccessful);
						IntegrationTest.Assert(getResult.Value.Has("testkey"));
					});
				}
			);
		});
	}

	[Test("Sets properties and checks them (tests read all & write all).")]
	public void ShouldSetMultipleProperties(Clan clan) {
		Login(clan, gamer => {
			Bundle props = Bundle.CreateObject();
			props["hello"] = "world";
			props["array"] = Bundle.CreateArray(1, 2, 3);
			// Set property, then get all and check it
			gamer.Properties().SetAll(
				properties: props,
				done: setResult => {
					if (!setResult.IsSuccessful) IntegrationTest.Fail("Error when setting property");
					if (setResult.Value != 1) IntegrationTest.Fail("Expected done = 1");

					gamer.Properties().GetAll(getResult => {
						IntegrationTest.Assert(getResult.IsSuccessful);
						IntegrationTest.Assert(getResult.Value["hello"] == "world");
						IntegrationTest.Assert(getResult.Value["array"].AsArray()[1] == 2);
						IntegrationTest.Assert(getResult.Value["array"].AsArray().Count == 3);
					});
				}
			);
		});
	}

	[Test("Tests removal of a single property (tests remove single & read single).")]
	public void ShouldRemoveProperty(Clan clan) {
		Login(clan, gamer => {
			Bundle props = Bundle.CreateObject();
			props["hello"] = "world";
			props["prop2"] = 123;
			// Set properties, remove one, then get it and check
			gamer.Properties().SetAll(
				properties: props,
				done: setResult => {
					if (!setResult.IsSuccessful) IntegrationTest.Fail("Error when setting property");

					gamer.Properties().RemoveKey(removeResult => {
						if (!removeResult.IsSuccessful) IntegrationTest.Fail("Error when removing property");

						gamer.Properties().GetKey(getResult => {
							IntegrationTest.Assert(getResult.IsSuccessful);
							IntegrationTest.Assert(getResult.Value.IsEmpty);
						}, "hello");
					}, "hello");
				}
			);
		});
	}

	[Test("Tests removal of all properties.")]
	public void ShouldRemoveAllProperties(Clan clan) {
		Login(clan, gamer => {
			Bundle props = Bundle.CreateObject();
			props["hello"] = "world";
			props["prop2"] = 123;
			// Set properties, remove them, then get all and check it
			gamer.Properties().SetAll(
				properties: props,
				done: setResult => {
					if (!setResult.IsSuccessful) IntegrationTest.Fail("Error when setting property");
					gamer.Properties().RemoveAll(removeResult => {
						if (!removeResult.IsSuccessful) IntegrationTest.Fail("Error when removing properties");
						gamer.Properties().GetAll(getResult => {
							IntegrationTest.Assert(getResult.IsSuccessful);
							IntegrationTest.Assert(getResult.Value.IsEmpty);
						});
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
