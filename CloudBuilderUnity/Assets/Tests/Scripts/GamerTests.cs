using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class GamerTests : TestBase {

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
			gamer.Properties.SetKey(
				key: "testkey",
				value: "value",
				done: setResult => {
					Assert(setResult.IsSuccessful, "Error when setting property");
					Assert(setResult.Value == 1, "Expected done = 1");

					gamer.Properties.GetAll(getResult => {
						Assert(getResult.IsSuccessful, "Failed to fetch properties");
						Assert(getResult.Value.Has("testkey"), "Previously set key is missing");
						CompleteTest();
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
			gamer.Properties.SetAll(
				properties: props,
				done: setResult => {
					if (!setResult.IsSuccessful) IntegrationTest.Fail("Error when setting property");
					if (setResult.Value != 1) IntegrationTest.Fail("Expected done = 1");

					gamer.Properties.GetAll(getResult => {
						Assert(getResult.IsSuccessful, "Get all keys failed");
						Assert(getResult.Value["hello"] == "world", "Should contain hello: world key");
						Assert(getResult.Value["array"].AsArray().Count == 3, "Should have a 3-item array");
						Assert(getResult.Value["array"].AsArray()[1] == 2, "Item 2 of array invalid");
						CompleteTest();
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
			gamer.Properties.SetAll(
				properties: props,
				done: setResult => {
					Assert(setResult.IsSuccessful, "Error when setting property");

					gamer.Properties.RemoveKey(removeResult => {
						if (!removeResult.IsSuccessful) IntegrationTest.Fail("Error when removing property");

						gamer.Properties.GetKey(getResult => {
							Assert(getResult.IsSuccessful, "Failed to fetch key");
							Assert(getResult.Value.IsEmpty, "The key should be empty");
							CompleteTest();
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
			gamer.Properties.SetAll(
				properties: props,
				done: setResult => {
					Assert(setResult.IsSuccessful, "Error when setting property");
					gamer.Properties.RemoveAll(removeResult => {
						Assert(removeResult.IsSuccessful, "Error when removing properties");
						gamer.Properties.GetAll(getResult => {
							Assert(getResult.IsSuccessful, "Failed to get all properties");
							Assert(getResult.Value.IsEmpty, "Expected no properties");
							CompleteTest();
						});
					});
				}
			);
		});
	}
}
