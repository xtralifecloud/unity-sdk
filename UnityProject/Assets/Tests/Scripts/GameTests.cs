using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class GameTests : TestBase {

	[InstanceMethod(typeof(GameTests))]
	public string TestMethodName;

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("Fetches all keys for the current game.")]
	public void ShouldFetchAllKeys(Cloud cloud) {
		cloud.Game.GameVfs.GetAll()
			.CompleteTestIfSuccessful();
	}

	[Test("Fetches a key on the game and checks that it worked properly.", requisite: "Please import [{\"fskey\":\"testkey\",\"fsvalue\":{\"test\": 2}}] in the current game key/value storage.")]
	public void ShouldFetchGameKey(Cloud cloud) {
		cloud.Game.GameVfs.GetKey("testkey")
		.ExpectSuccess(getResult => {
			Assert(getResult["test"] == 2, "Expected test: 2 key");
			CompleteTest();
		});
	}

	[Test("Runs a batch on the server and checks the return value.", requisite: "The current game must be set-up with a batch of name 'test' which does return {value: params.request.value * 2};")]
	public void ShouldRunGameBatch(Cloud cloud) {
		cloud.Game.Batches.Run("test", Bundle.CreateObject("value", 3))
		.ExpectSuccess(batchResult => {
			Assert(batchResult["value"] == 6, "Result invalid (expected 3 x 2 = 6)");
			CompleteTest();
		});
	}
}