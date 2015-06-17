using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class VfsTests: TestBase {

	[InstanceMethod(typeof(VfsTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Cloud param (and we do the setup here)
		FindObjectOfType<CotcGameObject>().GetCloud(cloud => {
			met.Invoke(this, new object[] { cloud });
		});
	}

	[Test("Tries to query a non existing key.")]
	public void ShouldNotReadInexistingKey(Cloud cloud) {
		cloud.LoginAnonymously(gamer => {
			Assert(gamer.IsSuccessful, "Failed to log in");
			gamer.Value.GamerVfs.GetKey(getRes => {
				Assert(!getRes.IsSuccessful, "Request marked as succeeded");
				Assert(getRes.HttpStatusCode == 404, "Wrong error code (404)");
				Assert(getRes.ServerData["name"] == "KeyNotFound", "Wrong error message");
				CompleteTest();
			}, "nonexistingkey");
		});
	}

	[Test("Sets a few keys, then reads them.")]
	public void ShouldWriteKeys(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.GamerVfs.SetKey(setRes => {
				gamer.GamerVfs.GetKey(getRes => {
					Assert(getRes.IsSuccessful, "Request failed");
					Assert(getRes.Value == "hello world", "Wrong key value");
					CompleteTest();
				}, "testkey");
			}, "testkey", "hello world");
		});
	}

	[Test("Sets a key, deletes it and then rereads it.")]
	public void ShouldDeleteKey(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.GamerVfs.SetKey(setRes => {
				gamer.GamerVfs.RemoveKey(remRes => {
					gamer.GamerVfs.GetKey(getRes => {
						Assert(!getRes.IsSuccessful, "Request marked as succeeded");
						Assert(getRes.HttpStatusCode == 404, "Wrong error code (404)");
						CompleteTest();
					}, "testkey");
				}, "testkey");
			}, "testkey", "value");

		});
	}

	[Test("Sets a binary key and rereads it.")]
	public void ShouldWriteBinaryKey(Cloud cloud) {
		Login(cloud, gamer => {
			byte[] data = { 1, 2, 3, 4 };
			gamer.GamerVfs.SetKeyBinary(setRes => {
				gamer.GamerVfs.GetKeyBinary(getRes => {
					Assert(getRes.IsSuccessful, "Request failed");
					Assert(getRes.Value.Length == 4, "Wrong key length");
					Assert(getRes.Value[2] == 3, "Wrong key value");
					CompleteTest();
				}, "testkey");
			}, "testkey", data);
		});
	}
}
