using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class AchievementTests : MonoBehaviour {

	[InstanceMethod(typeof(AchievementTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("Runs a transaction and checks the balance. Tests both the transaction and the balance calls.")]
	public void ShouldRunTransaction(Clan clan) {
		clan.LoginAnonymously(loginResult => {
			Gamer gamer = loginResult.Value;
			Bundle tx = Bundle.CreateObject("gold", 10, "silver", 100);
			// Set property, then get all and check it
			gamer.Transaction(
				transaction: tx,
				description: "Transaction run by integration test.",
				done: txResult => {
					if (!txResult.IsSuccessful) IntegrationTest.Fail("Error when running transaction");
					if (txResult.Value["balance"]["gold"] != 10) IntegrationTest.Fail("Gold is not set properly");
					if (txResult.Value["balance"]["silver"] != 100) IntegrationTest.Fail("Silver is not set properly");

					gamer.Balance(balance => {
						if (!balance.IsSuccessful) IntegrationTest.Fail("Error when running balance");
						IntegrationTest.Assert(balance.Value["gold"] == 10);
						IntegrationTest.Assert(balance.Value["silver"] == 100);
					});
				}
			);
		});
	}

	[Test("Runs a transaction that resets the balance. Tests the transaction syntax and read balance.")]
	public void ShouldResetBalance(Clan clan) {
		clan.LoginAnonymously(loginResult => {
			Gamer gamer = loginResult.Value;
			// Set property, then get all and check it
			gamer.Transaction(txResult => {
				if (!txResult.IsSuccessful) IntegrationTest.Fail("Error when running transaction");
				if (txResult.Value["balance"]["gold"] != 10) IntegrationTest.Fail("Balance not affected properly");

				gamer.Transaction(txResult2 => {
					IntegrationTest.Assert(txResult2.IsSuccessful);
					IntegrationTest.Assert(txResult2.Value["gold"] == 0);
				}, Bundle.CreateObject("gold", "-auto"), "Run from integration test");
			}, Bundle.CreateObject("gold", 10), "Transaction run by integration test.");
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
