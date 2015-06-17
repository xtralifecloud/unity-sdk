using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class GameTests : TestBase {

	[InstanceMethod(typeof(GameTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Cloud param (and we do the setup here)
		FindObjectOfType<CotcGameObject>().GetCloud(cloud => {
			met.Invoke(this, new object[] { cloud });
		});
	}

	[Test("Fetches all keys for the current game.")]
	public void ShouldFetchAllKeys(Cloud cloud) {
		cloud.Game.GameVfs.GetAll(result => {
			Assert(result.IsSuccessful, "Failed to fetch keys");
			CompleteTest();
		});
	}

	[Test("Fetches a key on the game and checks that it worked properly.", requisite: "Please import [{\"fskey\":\"testkey\",\"fsvalue\":{\"test\": 2}}] in the current game key/value storage.")]
	public void ShouldFetchGameKey(Cloud cloud) {
		cloud.Game.GameVfs.GetKey(getResult => {
			Assert(getResult.IsSuccessful, "Failed to fetch key");
			Assert(getResult.Value["test"] == 2, "Expected test: 2 key");
			CompleteTest();
		}, "testkey");
	}

	[Test("Runs a batch on the server and checks the return value.", requisite: "The current game must be set-up with a batch of name 'test' which does return {value: params.request.value * 2};")]
	public void ShouldRunGameBatch(Cloud cloud) {
		cloud.Game.Batches.Run(batchResult => {
			Assert(batchResult.IsSuccessful, "Failed to run batch");
			Assert(batchResult.Value["value"] == 6, "Result invalid (expected 3 x 2 = 6)");
			CompleteTest();
		}, "test", Bundle.CreateObject("value", 3));
	}
}
