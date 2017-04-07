using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class VfsTests: TestBase {

	[InstanceMethod(typeof(VfsTests))]
	public string TestMethodName;

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("Tries to query a non existing key.")]
	public void ShouldNotReadInexistingKey(Cloud cloud) {
		cloud.LoginAnonymously().ExpectSuccess(gamer => {
			gamer.GamerVfs.GetValue("nonexistingkey")
			.ExpectFailure(getRes => {
				Assert(getRes.HttpStatusCode == 404, "Wrong error code (404)");
				Assert(getRes.ServerData["name"] == "KeyNotFound", "Wrong error message");
				CompleteTest();
			});
		});
	}

	[Test("Sets a few keys, then reads them.")]
	public void ShouldWriteKeys(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.GamerVfs.SetValue("testkey", "hello world")
			.ExpectSuccess(setRes => {
                gamer.GamerVfs.GetValue("testkey")
                .ExpectSuccess(getRes => {
                    Debug.Log(getRes);
                    Assert(getRes.Has("result"), "Expected result field");
                    Assert(getRes["result"].Has("testkey"), "Expected testKey field");
                    Assert(getRes["result"]["testkey"].AsString() == "hello world", "Wrong key value");
					CompleteTest();
				});
			});
		});
	}

	[Test("Sets a key, deletes it and then rereads it.")]
	public void ShouldDeleteKey(Cloud cloud) {
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
	}

	[Test("Sets a binary key and rereads it.")]
	public void ShouldWriteBinaryKey(Cloud cloud) {
		Login(cloud, gamer => {
			byte[] data = { 1, 2, 3, 4 };
            Debug.Log("0");
            gamer.GamerVfs.GetValue("testkey").Done(done => {
                Debug.Log(done);
            });

            /*gamer.GamerVfs.GetBinary("testkey").Catch(ex => {
                FailTest("Exception :" + ex);
            });*/
            gamer.GamerVfs.SetBinary("testkey", data).Catch(ex => {
                FailTest("Exception :" + ex);
            });
            /*.ExpectSuccess(setRes => {
                Debug.Log("1");
				gamer.GamerVfs.GetBinary("testkey")
				.ExpectSuccess(getRes => {
                    Debug.Log("2");
                    Assert(getRes.Length == 4, "Wrong key length");
					Assert(getRes[2] == 3, "Wrong key value");
					CompleteTest();
				});
			});*/
            Debug.Log("End");
		});
	}
}
