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
		cloud.Game.GameVfs.GetValue()
			.ExpectSuccess(received => {
                Assert(received.Has("result"), "No field named result");
                CompleteTest();
            });
	}

    /// To import in the Game VFS under the key unitTest_DoNotTouch :
    /// {"int":2,"double":0.99,"string":"test","bool":true,"array":[1,2,3],"dict":{"key":"val"},"jsonStringified":"{\"key\":\"val\"}"}
    [Test("Fetches a key on the game and checks that it worked properly.", requisite: "Please import the value in commentary in the current game key/value storage under the key unitTest_DoNotTouch. Also assesses that string-type keys can be fetched properly.")]
    public void ShouldFetchGameKey(Cloud cloud) {
        cloud.Game.GameVfs.GetValue("unitTest_DoNotTouch")
        .ExpectSuccess(received => {
            Assert(received.Type != Bundle.DataType.String, "Not expecting string result");
            Assert(received.Has("result"), "No field named result");

            Assert(received["result"]["unitTest_DoNotTouch"].Has("int"), "No field named int");
            Assert(received["result"]["unitTest_DoNotTouch"]["int"].AsInt() == 2, "Expected int (2), received : " + received["result"]["int"].AsInt());

            Assert(received["result"]["unitTest_DoNotTouch"].Has("double"), "No field named double");
            Assert(received["result"]["unitTest_DoNotTouch"]["double"].AsDouble() == 0.99, "Expected double (0.99), received : " + received["result"]["double"].AsDouble());

            Assert(received["result"]["unitTest_DoNotTouch"].Has("string"), "No field named string");
            Assert(received["result"]["unitTest_DoNotTouch"]["string"].AsString() == "test", "Expected string (test), received : " + received["result"]["string"].AsString());

            Assert(received["result"]["unitTest_DoNotTouch"].Has("bool"), "No field named bool");
            Assert(received["result"]["unitTest_DoNotTouch"]["bool"].AsBool() == true, "Expected bool (true), received : " + received["result"]["bool"].AsBool());

            Assert(received["result"]["unitTest_DoNotTouch"].Has("array"), "No field named array");
            Assert(received["result"]["unitTest_DoNotTouch"]["array"].AsArray()[0] == 1 
                && received["result"]["unitTest_DoNotTouch"]["array"].AsArray()[1] == 2 
                && received["result"]["unitTest_DoNotTouch"]["array"].AsArray()[2] == 3, "Expected array ([1,2,3]), received : " + received["result"]["array"].AsArray());

            Assert(received["result"]["unitTest_DoNotTouch"].Has("dict"), "No field named dict");
            Assert(received["result"]["unitTest_DoNotTouch"]["dict"].Has("key"), "No field named array");
            Assert(received["result"]["unitTest_DoNotTouch"]["dict"]["key"].AsString() == "val", "Expected string in dictionnary ({\"key\":\"val\"}, received : " + received["result"]["dict"]);

            Assert(received["result"]["unitTest_DoNotTouch"].Has("jsonStringified"), "No field named jsonStringified");
            Assert(!received["result"]["unitTest_DoNotTouch"]["jsonStringified"].Has("key"), "Expected to get a string, got a dictionnary instead");

            CompleteTest();
        });
	}

    [Test("Runs a batch without parameters on the server and checks the return value.", requisite: "The current game must be set-up with a batch which must return anything")]
    public void ShouldRunGameBatchWithoutParameter(Cloud cloud) {
        cloud.Game.Batches.Run("unitTest_withoutParam")
        .ExpectSuccess(batchResult => {
            Assert(batchResult.AsString() == "Hello World !", "Result invalid, expected 'Hello World', got " + batchResult.AsString());
            CompleteTest();
        });
    }

    [Test("Runs a batch on another domain on the server and checks the return value.", requisite: "The current game must be set-up with a batch which must return anything")]
    public void ShouldRunGameBatchOnAnotherDomain(Cloud cloud) {
        cloud.Game.Batches.Domain("com.clanofthecloud.cloudbuilder.test").Run("unitTest_otherDomain")
        .ExpectSuccess(batchResult => {
            Assert(batchResult.AsString() == "Other Domain", "Result invalid, expected 'Hello World', got " + batchResult.AsString());
            CompleteTest();
        });
    }

    [Test("Runs a batch with parameters on the server and checks the return value.", requisite: "The current game must be set-up with a batch which must return {value: params.request.value * 2};")]
	public void ShouldRunGameBatchWithParameters(Cloud cloud) {
		cloud.Game.Batches.Run("unitTest_withParams", Bundle.CreateObject("value", 3))
		.ExpectSuccess(batchResult => {
			Assert(batchResult["value"] == 6, "Result invalid (expected 3 x 2 = 6)");
			CompleteTest();
		});
	}    

    [Test("Get a binary from gameVFS", "The get/set of binaries is broken at the moment.")]
    public void ShouldGetBinaryFromGameVFS(Cloud cloud) {
        cloud.Game.GameVfs.GetBinary("unitTest_GetBinary")
        .ExpectSuccess(data => {
            // TODO Complete this test when the get/set of binaries is no longer broken.
            Debug.LogError("You must test data here!");
        });        
    }
}
