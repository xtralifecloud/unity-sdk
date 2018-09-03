using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;
using System.Collections;

public class GameTests : TestBase {

	private const string BoBatchConfiguration = "Needs a set up batch to work (please check for the related test function's comment in code).";
	private const string BoGameVfsConfiguration = "Needs a set up game VFS key to work (please check for the related test function's comment in code).";

    [Test("Fetches all keys for the current game.")]
	public IEnumerator ShouldFetchAllKeys() {
        cloud.Game.GameVfs.GetValue()
		.ExpectSuccess(received => {
            Assert(received.Has("result"), "No field named result");
            CompleteTest();
        });
        return WaitForEndOfTest();
	}

	[Test("Fetches a key on the game and checks that it worked properly.", requisite: BoGameVfsConfiguration)]
	/*
		Backend prerequisites >> Add the following "unitTest_testKey" game VFS key:
		
		{"int":2,"double":0.99,"string":"test","bool":true,"array":[1,2,3],"dict":{"key":"val"},"jsonStringified":"{\"key\":\"val\"}"}
	*/
	public IEnumerator ShouldFetchGameKey() {
        cloud.Game.GameVfs.GetValue("unitTest_testKey")
        .ExpectSuccess(received => {
            Assert(received.Type != Bundle.DataType.String, "Not expecting string result");
            Assert(received.Has("result"), "No field named result");

            Assert(received["result"]["unitTest_testKey"].Has("int"), "No field named int");
            Assert(received["result"]["unitTest_testKey"]["int"].AsInt() == 2, "Expected int (2), received : " + received["result"]["int"].AsInt());

            Assert(received["result"]["unitTest_testKey"].Has("double"), "No field named double");
            Assert(received["result"]["unitTest_testKey"]["double"].AsDouble() == 0.99, "Expected double (0.99), received : " + received["result"]["double"].AsDouble());

            Assert(received["result"]["unitTest_testKey"].Has("string"), "No field named string");
            Assert(received["result"]["unitTest_testKey"]["string"].AsString() == "test", "Expected string (test), received : " + received["result"]["string"].AsString());

            Assert(received["result"]["unitTest_testKey"].Has("bool"), "No field named bool");
            Assert(received["result"]["unitTest_testKey"]["bool"].AsBool() == true, "Expected bool (true), received : " + received["result"]["bool"].AsBool());

            Assert(received["result"]["unitTest_testKey"].Has("array"), "No field named array");
            Assert(received["result"]["unitTest_testKey"]["array"].AsArray()[0] == 1 
                && received["result"]["unitTest_testKey"]["array"].AsArray()[1] == 2 
                && received["result"]["unitTest_testKey"]["array"].AsArray()[2] == 3, "Expected array ([1,2,3]), received : " + received["result"]["array"].AsArray());

            Assert(received["result"]["unitTest_testKey"].Has("dict"), "No field named dict");
            Assert(received["result"]["unitTest_testKey"]["dict"].Has("key"), "No field named array");
            Assert(received["result"]["unitTest_testKey"]["dict"]["key"].AsString() == "val", "Expected string in dictionnary ({\"key\":\"val\"}, received : " + received["result"]["dict"]);

            Assert(received["result"]["unitTest_testKey"].Has("jsonStringified"), "No field named jsonStringified");
            Assert(!received["result"]["unitTest_testKey"]["jsonStringified"].Has("key"), "Expected to get a string, got a dictionnary instead");

            CompleteTest();
        });
        return WaitForEndOfTest();
	}

	[Test("Runs a batch without parameters on the server and checks the return value.", requisite: BoBatchConfiguration)]
	/*
		Backend prerequisites >> Add the following "unitTest_withoutParam" batch:
		
		function __unitTest_withoutParam(params, customData, mod) {
			"use strict";
			// don't edit above this line // must be on line 3
			// Used for unit test.
			return "Hello World !";
		} // must be on last line, no CR
	*/
	public IEnumerator ShouldRunGameBatchWithoutParameter() {
        cloud.Game.Batches.Run("unitTest_withoutParam")
        .ExpectSuccess(batchResult => {
            Assert(batchResult.AsString() == "Hello World !", "Result invalid, expected 'Hello World', got " + batchResult.AsString());
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

	[Test("Runs a batch with parameters on the server and checks the return value.", requisite: BoBatchConfiguration)]
	/*
		Backend prerequisites >> Add the following "unitTest_withParam" batch:
		
		function __unitTest_withParams(params, customData, mod) {
			"use strict";
			// don't edit above this line // must be on line 3
			// Used for unit test.
			return {value: params.request.value * 2};
		} // must be on last line, no CR
	*/
	public IEnumerator ShouldRunGameBatchWithParameters() {
		cloud.Game.Batches.Run("unitTest_withParams", Bundle.CreateObject("value", 3))
			.ExpectSuccess(batchResult => {
				Assert(batchResult["value"] == 6, "Result invalid (expected 3 x 2 = 6)");
				CompleteTest();
			});
		return WaitForEndOfTest();
	}

	[Test("Runs a batch on another domain on the server and checks the return value.", requisite: "The current game must be set-up with a batch which must return the \"Hello World\" string")]
	/*
		Backend prerequisites >> Create a new domain which includes your game, then add the following "unitTest_otherDomain" batch:
		
		function __unitTest_otherDomain(params, customData, mod) {
			"use strict";
			// don't edit above this line // must be on line 3
			// Used for unit test.
			return "Hello World";
		} // must be on last line, no CR
	*/
	public IEnumerator ShouldRunGameBatchOnAnotherDomain() {
        cloud.Game.Batches.Domain("com.clanofthecloud.cloudbuilder.test").Run("unitTest_otherDomain")
        .ExpectSuccess(batchResult => {
            Assert(batchResult.AsString() == "Other Domain", "Result invalid, expected 'Hello World', got " + batchResult.AsString());
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

	[Test("Get a binary from gameVFS", requisite: BoGameVfsConfiguration)]
	/*
		Backend prerequisites >> Add the following "unitTest_GetBinary" game VFS key:
		
		"https://s3-eu-west-1.amazonaws.com/cloudbuilder.binaries.sandbox/com.clanofthecloud.cloudbuilder.azerty/GAME/unitTest_GetBinary-65711ea4cb4a6e385e22a2fb8bda2c1ff51f7e64"
	*/
	public IEnumerator ShouldGetBinaryFromGameVFS() {
        cloud.Game.GameVfs.GetBinary("unitTest_GetBinary")
        .ExpectSuccess(data => {
            Assert(System.Text.Encoding.UTF8.GetString(data) == "This is a test for the GameGetBinary", "Invalid game binary");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

    [Test("Try to get an unexisting binary from gameVFS")]
    public IEnumerator ShouldFailToGetUnexistingBinaryFromGameVFS() {
        cloud.Game.GameVfs.GetBinary("unexisting_binary")
        .ExpectFailure(result => {
            Assert(result.ServerData["name"] == "KeyNotFound", "Name should be KeyNotFound");
            Assert(result.ServerData["message"] == "The specified key couldn't be found", "Should indicate unexisting binary");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }
}
