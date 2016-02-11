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
			gamer.GamerVfs.GetKey("nonexistingkey")
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
			gamer.GamerVfs.SetKey("testkey", "hello world")
			.ExpectSuccess(setRes => {
				gamer.GamerVfs.GetKey("testkey")
				.ExpectSuccess(getRes => {
					Assert(getRes == "hello world", "Wrong key value");
					CompleteTest();
				});
			});
		});
	}

	[Test("Sets a key, deletes it and then rereads it.")]
	public void ShouldDeleteKey(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.GamerVfs.SetKey("testkey", "value")
			.ExpectSuccess(setRes => {
				gamer.GamerVfs.RemoveKey("testkey")
				.ExpectSuccess(remRes => {
					gamer.GamerVfs.GetKey("testkey")
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
			gamer.GamerVfs.SetKeyBinary("testkey", data)
			.ExpectSuccess(setRes => {
				gamer.GamerVfs.GetKeyBinary("testkey")
				.ExpectSuccess(getRes => {
					Assert(getRes.Length == 4, "Wrong key length");
					Assert(getRes[2] == 3, "Wrong key value");
					CompleteTest();
				});
			});
		});
	}
}
