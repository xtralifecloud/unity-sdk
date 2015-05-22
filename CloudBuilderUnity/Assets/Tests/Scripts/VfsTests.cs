using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class VfsTests: MonoBehaviour {

	[InstanceMethod(typeof(VfsTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("Tries to query a non existing key.")]
	public void ShouldNotReadInexistingKey(Clan clan) {
		clan.LoginAnonymously(gamer => {
			if (!gamer.IsSuccessful) IntegrationTest.Fail("Failed to log in");
			gamer.Value.GamerVfs().GetKey(getRes => {
				IntegrationTest.Assert(!getRes.IsSuccessful, "Request marked as succeeded");
				IntegrationTest.Assert(getRes.HttpStatusCode == 404, "Wrong error code (404)");
				IntegrationTest.Assert(getRes.ServerData["name"] == "KeyNotFound", "Wrong error message");
			}, "nonexistingkey");
		});
	}

	[Test("Sets a few keys, then reads them.")]
	public void ShouldWriteKeys(Clan clan) {
		Login(clan, gamer => {
			gamer.GamerVfs().SetKey(setRes => {
				gamer.GamerVfs().GetKey(getRes => {
					IntegrationTest.Assert(getRes.IsSuccessful, "Request failed");
					IntegrationTest.Assert(getRes.Value == "hello world", "Wrong key value");
				}, "testkey");
			}, "testkey", "hello world");
		});
	}

	[Test("Sets a key, deletes it and then rereads it.")]
	public void ShouldDeleteKey(Clan clan) {
		Login(clan, gamer => {
			gamer.GamerVfs().SetKey(setRes => {
				gamer.GamerVfs().RemoveKey(remRes => {
					gamer.GamerVfs().GetKey(getRes => {
						IntegrationTest.Assert(!getRes.IsSuccessful, "Request marked as succeeded");
						IntegrationTest.Assert(getRes.HttpStatusCode == 404, "Wrong error code (404)");
					}, "testkey");
				}, "testkey");
			}, "testkey", "value");

		});
	}

	[Test("Sets a binary key and rereads it.")]
	public void ShouldWriteBinaryKey(Clan clan) {
		Login(clan, gamer => {
			byte[] data = { 1, 2, 3, 4 };
			gamer.GamerVfs().SetKeyBinary(setRes => {
				gamer.GamerVfs().GetKeyBinary(getRes => {
					IntegrationTest.Assert(getRes.IsSuccessful, "Request failed");
					IntegrationTest.Assert(getRes.Value.Length == 4, "Wrong key length");
					IntegrationTest.Assert(getRes.Value[2] == 3, "Wrong key value");
				}, "testkey");
			}, "testkey", data);
		});
	}

	#region Private helpers
	private void Login(Clan clan, Action<Gamer> done) {
		clan.Login(
			network: LoginNetwork.Email,
			networkId: "clan@localhost.localdomain",
			networkSecret: "Password123",
			done: result => {
				if (!result.IsSuccessful) IntegrationTest.Fail("Failed to log in");
				done(result.Value);
			}
		);
	}
	#endregion
}
