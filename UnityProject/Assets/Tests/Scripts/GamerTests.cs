using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class GamerTests : TestBase {

	[InstanceMethod(typeof(GamerTests))]
	public string TestMethodName;

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("Sets a property and checks that it worked properly (tests read all & write single).")]
	public void ShouldSetProperty(Cloud cloud) {
		Login(cloud, gamer => {
			// Set property, then get all and check it
			gamer.Properties.SetKey(
				key: "testkey",
				value: "value")
			.ExpectSuccess(setResult => {
				Assert(setResult["done"] == 1, "Expected done = 1");

				gamer.Properties.GetAll()
				.ExpectSuccess(getResult => {
					Assert(getResult.Has("testkey"), "Previously set key is missing");
					CompleteTest();
				});
			});
		});
	}

	[Test("Sets properties and checks them (tests read all & write all).")]
	public void ShouldSetMultipleProperties(Cloud cloud) {
		Login(cloud, gamer => {
			Bundle props = Bundle.CreateObject();
			props["hello"] = "world";
			props["array"] = Bundle.CreateArray(1, 2, 3);

			// Set property, then get all and check it
			gamer.Properties.SetAll(props)
			.ExpectSuccess(setResult => {
				Assert(setResult["done"] == 1, "Expected done = 1");

				gamer.Properties.GetAll()
				.ExpectSuccess(getResult => {
					Assert(getResult["hello"] == "world", "Should contain hello: world key");
					Assert(getResult["array"].AsArray().Count == 3, "Should have a 3-item array");
					Assert(getResult["array"].AsArray()[1] == 2, "Item 2 of array invalid");
					CompleteTest();
				});
			});
		});
	}

	[Test("Tests removal of a single property (tests remove single & read single).")]
	public void ShouldRemoveProperty(Cloud cloud) {
		Login(cloud, gamer => {
			Bundle props = Bundle.CreateObject();
			props["hello"] = "world";
			props["prop2"] = 123;
			// Set properties, remove one, then get it and check
			gamer.Properties.SetAll(props)
			.ExpectSuccess(setResult => {
				gamer.Properties.RemoveKey("hello")
				.ExpectSuccess(removeResult => {
					// Should not find anymore
					gamer.Properties.GetKey("hello")
					.ExpectSuccess(getResult => {
						Assert(getResult.IsEmpty, "The key should be empty");
						CompleteTest();
					});
				});
			});
		});
	}

	[Test("Tests removal of all properties.")]
	public void ShouldRemoveAllProperties(Cloud cloud) {
		Login(cloud, gamer => {
			Bundle props = Bundle.CreateObject();
			props["hello"] = "world";
			props["prop2"] = 123;
			// Set properties, remove them, then get all and check it
			gamer.Properties.SetAll(props)
			.ExpectSuccess(setResult => {

				gamer.Properties.RemoveAll()
				.ExpectSuccess(removeResult => {
					
					gamer.Properties.GetAll()
					.ExpectSuccess(getResult => {
						Assert(getResult.IsEmpty, "Expected no properties");
						CompleteTest();
					});
				});
			});
		});
	}

	[Test("Fetches and updates profile information about a gamer, testing that the GamerProfile methods work as expected.")]
	public void ShouldUpdateProfile(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.Profile.Get()
			.ExpectSuccess(profile => {
				Assert(profile["email"] == "cloud@localhost.localdomain", "Invalid e-mail address (verify Login method in TestBase)");
				Assert(profile["lang"] == "en", "Default language should be english");

				gamer.Profile.Set(Bundle.CreateObject("displayName", "QA", "firstName", "Tester"))
				.ExpectSuccess(setProfile => {
					Assert(setProfile == true, "Update profile expected to return true");
					CompleteTest();
				});
			});
		});
	}

	[Test("Runs a batch on the server and checks the return value.", requisite: "The current game must be set-up with {\"__test\":\"\treturn {value: params.request.value * 2};\",\"__testGamer\":\"    return this.user.profile.read(params.user_id).then(function (result) {\n      return {message: params.request.prefix + result.profile.email};\n    });\"}.")]
	public void ShouldRunGamerBatch(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.Batches.Run("testGamer", Bundle.CreateObject("prefix", "Hello "))
			.ExpectSuccess(batchResult => {
				Assert(batchResult["message"] == "Hello cloud@localhost.localdomain", "Returned value invalid (" + batchResult["message"] + ", check hook on server");
				CompleteTest();
			});
		});
	}

	[Test("Tests the outline functionality")]
	public void ShouldReturnProperOutline(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.Profile.Outline()
			.ExpectSuccess(outline => {
				Assert(outline.Network == gamer.Network, "Expected same network");
				Assert(outline.NetworkId == gamer.NetworkId, "Expected same network ID");
				Assert(outline["games"].Type == Bundle.DataType.Array, "Expected games array");
				CompleteTest();
			});
		});
	}

}
