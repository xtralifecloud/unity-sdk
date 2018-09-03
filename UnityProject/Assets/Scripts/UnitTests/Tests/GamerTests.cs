using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;
using System.Collections;

public class GamerTests : TestBase {

	private const string BoBatchConfiguration = "Needs a set up batch to work (please check for the related test function's comment in code).";
	private const string BoHookConfiguration = "Needs a set up hook to work (please check for the related test function's comment in code).";

	[Test("Sets a property and checks that it worked properly (tests read all & write single).")]
	public IEnumerator ShouldSetProperty() {
		Login(cloud, gamer => {
			// Set property, then get all and check it
			gamer.Properties.SetKey(
				key: "testkey",
				value: "value")
			.ExpectSuccess(setResult => {
				Assert(setResult["done"] == 1, "Expected done = 1");

				return gamer.Properties.GetAll();
			})
			.ExpectSuccess(getResult => {
                Assert(getResult.Has("testkey"), "Previously set key is missing");
                Assert(getResult["testkey"] == "value", "Previously set key contains a wrong value");

                CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Sets properties and checks them (tests read all & write all).")]
	public IEnumerator ShouldSetMultipleProperties() {
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
					Assert(getResult["array"].AsArray()[0] == 1
					    && getResult["array"].AsArray()[1] == 2
                        && getResult["array"].AsArray()[2] == 3, "Content of array invalid");
					CompleteTest();
				});
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Tests removal of a single property (tests remove single & read single).")]
	public IEnumerator ShouldRemoveProperty() {
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
        return WaitForEndOfTest();
	}

	[Test("Tests removal of all properties.")]
	public IEnumerator ShouldRemoveAllProperties() {
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
        return WaitForEndOfTest();
	}

	[Test("Fetches and updates profile information about a gamer, testing that the GamerProfile methods work as expected.")]
	public IEnumerator ShouldUpdateProfile() {
		Login(cloud, gamer => {
			gamer.Profile.Get()
			.ExpectSuccess(profile => {
				Assert(profile["email"] == "cloud@localhost.localdomain", "Invalid e-mail address (verify Login method in TestBase)");
				Assert(profile["lang"] == "en", "Default language should be english");

                gamer.Profile.Set(Bundle.CreateObject("displayName", "QA", "firstName", "Tester"))
                .ExpectSuccess(setProfile => {
                    Assert(setProfile == true, "Update profile expected to return true");

                    return gamer.Profile.Get();
                })
                // Ensure that the profile have been updated
                .ExpectSuccess(profile2 => {
                    Assert(profile2["displayName"] == "QA", "Display name has not been updated");
                    Assert(profile2["firstName"] == "Tester", "First name has not been updated");
                    CompleteTest();
                });
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Runs a batch on the server and checks the return value.", requisite: BoBatchConfiguration)]
	/*
		Backend prerequisites >> Add the following "unitTest_testGamer" batch:
		
		function __unitTest_testGamer(params, customData, mod) {
			"use strict";
			// don't edit above this line // must be on line 3
			// Used for unit test.
			return this.user.profile.read(params.user_id).then(function (result) {
		    	return {message: params.request.prefix + result.profile.email};
			});
		} // must be on last line, no CR
	*/
	public IEnumerator ShouldRunGamerBatch() {
		Login(cloud, gamer => {
			gamer.Batches.Run("unitTest_testGamer", Bundle.CreateObject("prefix", "Hello "))
			.ExpectSuccess(batchResult => {
				Assert(batchResult["message"] == "Hello cloud@localhost.localdomain", "Returned value invalid (" + batchResult["message"] + ", check hook on server");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Creates a match, and checks that the request has been hooked by looking for a new field in the data returned", requisite: BoHookConfiguration)]
	/*
		Backend prerequisites >> Add the following "after-match-create" hook:
		
		function after-match-create(params, customData, mod) {
			"use strict";
			// don't edit above this line // must be on line 3
			// Used for unit test.
		    params.match.globalState = {hooked:true};
		} // must be on last line, no CR
	*/
    public IEnumerator ShouldBeHooked() {
        LoginNewUser(cloud, gamer => {
            gamer.Matches.Create(1)
            .ExpectSuccess(match => {
                Assert(match.GlobalState["hooked"].AsBool(), "Expected a field 'hooked:true' in the globalState");
                CompleteTest();
            });
        });
        return WaitForEndOfTest();
    }

	[Test("Tests the outline functionality")]
	public IEnumerator ShouldReturnProperOutline() {
		Login(cloud, gamer => {
			gamer.Profile.Outline()
			.ExpectSuccess(outline => {
				Assert(outline.Network == gamer.Network, "Expected same network");
				Assert(outline.NetworkId == gamer.NetworkId, "Expected same network ID");
				Assert(outline["games"].Type == Bundle.DataType.Array, "Expected games array");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}
}
