using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;
using System.Collections;

public class VfsTests: TestBase {

	[Test("Tries to query a non existing key.")]
	public IEnumerator ShouldNotReadInexistingKey() {
		cloud.LoginAnonymously().ExpectSuccess(gamer => {
			gamer.GamerVfs.GetValue("nonexistingkey")
			.ExpectFailure(getRes => {
				Assert(getRes.HttpStatusCode == 404, "Wrong error code (404)");
				Assert(getRes.ServerData["name"] == "KeyNotFound", "Wrong error message");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Sets a few keys, then reads them.")]
	public IEnumerator ShouldWriteKeys() {
		Login(cloud, gamer => {
			gamer.GamerVfs.SetValue("testkey", "hello world")
			.ExpectSuccess(setRes => {
                gamer.GamerVfs.GetValue("testkey")
                .ExpectSuccess(getRes => {
                    Assert(getRes.Has("result"), "Expected result field");
                    Assert(getRes["result"].Has("testkey"), "Expected testKey field");
                    Assert(getRes["result"]["testkey"].AsString() == "hello world", "Wrong key value");
					CompleteTest();
				});
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Sets a key, deletes it and then rereads it.")]
	public IEnumerator ShouldDeleteKey() {
		Login(cloud, gamer => {
			gamer.GamerVfs.SetValue("testkey", "value")
			.ExpectSuccess(setRes => {
				gamer.GamerVfs.DeleteValue("testkey")
				.ExpectSuccess(remRes => {
					gamer.GamerVfs.GetValue("testkey")
					.ExpectFailure(getRes => {
						Assert(getRes.HttpStatusCode == 404, "Wrong error code (404)");
						CompleteTest();
					});
				});
			});
		});
        return WaitForEndOfTest();
	}

    [NUnit.Framework.Ignore("Test broken because the content-type header is invalid on Unity 2017.4 when calling SetBinary")]
    [Test("Sets a binary key and rereads it.")]
	public IEnumerator ShouldWriteAndReadBinaryKey() {
		Login(cloud, gamer => {
			byte[] data = { 1, 2, 3, 4 };

            gamer.GamerVfs.SetBinary("testkey", data)
            .ExpectSuccess(setRes => {
                gamer.GamerVfs.GetBinary("testkey")
                .ExpectSuccess(getRes => {
                    Assert(getRes.Length == 4, "Wrong key length");
                    Assert(getRes[0] == 1
                        && getRes[1] == 2
                        && getRes[2] == 3
                        && getRes[3] == 4, "Wrong key value");
                    CompleteTest();
                });
            });
        });
        return WaitForEndOfTest();
	}
}
