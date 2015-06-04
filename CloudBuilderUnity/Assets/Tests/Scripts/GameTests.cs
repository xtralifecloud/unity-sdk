using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class GameTests : TestBase {

	[InstanceMethod(typeof(GameTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("Fetches all keys for the current game.")]
	public void ShouldFetchAllKeys(Clan clan) {
		Login(clan, gamer => {
			gamer.Game.GameVfs.GetAll(result => {
				Assert(result.IsSuccessful, "Failed to fetch keys");
				CompleteTest();
			});
		});
	}

	[Test("Fetches a key on the game and checks that it worked properly.", requisite: "The current game must be set-up with a key of name 'testkey' and value '{\"test\": 2}'.")]
	public void ShouldFetchGameKey(Clan clan) {
		Login(clan, gamer => {
			gamer.Game.GameVfs.GetKey(getResult => {
				Assert(getResult.IsSuccessful, "Failed to fetch key");
				Assert(getResult.Value["test"] == 2, "Expected test: 2 key");
				CompleteTest();
			}, "testkey");
		});
	}
}
