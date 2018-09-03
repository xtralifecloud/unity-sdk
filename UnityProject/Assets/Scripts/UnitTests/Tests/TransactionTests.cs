using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;
using System.Collections;

/**
 * Transactions and achievements tests.
 */
public class TransactionTests : TestBase {

	private const string BoAchievementConfiguration = "Needs a set up achievement to work: {\"unitTestAchievements\":{\"type\":\"limit\",\"config\":{\"unit\":\"gold\",\"maxValue\":\"100\"}}}";

	[Test("Runs a transaction and checks the balance. Tests both the transaction and the balance calls.")]
	public IEnumerator ShouldRunTransaction() {
		LoginNewUser(cloud, gamer => {
			Bundle tx = Bundle.CreateObject("gold", 10, "silver", 100);
			gamer.Transactions.Post(
				transaction: tx,
				description: "Transaction run by integration test.")
			.ExpectSuccess(txResult => {
				Assert(txResult.Balance["gold"] == 10, "Gold is not set properly");
				Assert(txResult.Balance["silver"] == 100, "Silver is not set properly");
				return gamer.Transactions.Balance();
			})
			.ExpectSuccess(balance => {
				Assert(balance["gold"] == 10, "Expected gold: 10 in balance");
				Assert(balance["silver"] == 100, "Expected silver: 100 in balance");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Runs a transaction that resets the balance. Tests the transaction syntax and read balance.")]
	public IEnumerator ShouldResetBalance() {
		LoginNewUser(cloud, gamer => {
			// Set property, then get all and check it
			gamer.Transactions.Post(Bundle.CreateObject("gold", 10), "Transaction run by integration test.")
			.ExpectSuccess(txResult => {
				Assert(txResult.Balance["gold"] == 10, "Balance not affected properly");
			})
			.ExpectSuccess(dummy => gamer.Transactions.Post(Bundle.CreateObject("gold", "-auto"), "Run from integration test"))
			.ExpectSuccess(txResult2 => {
				Assert(txResult2.Balance["gold"] == 0, "Expected gold: 0 balance");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Runs a transaction that should trigger an achievement.", requisite: BoAchievementConfiguration)]
	/*
		Backend prerequisites >> Add the following achievement:
		
		Name: "unitTestAchievements"
		Unit: "gold"
		Trigger value: "100"
		Reward: "{}"
	*/
	public IEnumerator ShouldTriggerAchievement() {
		LoginNewUser(cloud, gamer => {
			gamer.Transactions.Post(Bundle.CreateObject("gold", 100), "Transaction run by integration test.")
			.ExpectSuccess(txResult => {
				Assert(txResult.TriggeredAchievements.Count == 1, "Expected one achievement triggered");
				Assert(txResult.TriggeredAchievements["unitTestAchievements"].Name == "unitTestAchievements", "Expected unitTestAchievements to be triggered");
				Assert(txResult.TriggeredAchievements["unitTestAchievements"].Type == AchievementType.Limit, "Expected unitTestAchievements.type: limit");
				Assert(txResult.TriggeredAchievements["unitTestAchievements"].Config["maxValue"] == 100, "Expected unitTestAchievements.maxValue: 100");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Runs a transaction that should trigger an achievement.", requisite: BoAchievementConfiguration)]
	/*
		Backend prerequisites >> Add the following achievement:
		
		Name: "unitTestAchievements"
		Unit: "gold"
		Trigger value: "100"
		Reward: "{}"
	*/
	public IEnumerator ShouldAssociateAchievementData() {
		LoginNewUser(cloud, gamer => {
			gamer.Transactions.Post(Bundle.CreateObject("gold", 100), "Transaction run by integration test.")
			.ExpectSuccess(txResult => {
				Assert(txResult.TriggeredAchievements["unitTestAchievements"].Name == "unitTestAchievements", "Expected unitTestAchievements to be triggered");
				// Associate data
				return gamer.Achievements.AssociateData("unitTestAchievements", Bundle.CreateObject("key", "value"));
			})
			.ExpectSuccess(assocResult => {
				Assert(assocResult.Name == "unitTestAchievements", "Wrong achievement name");
				Assert(assocResult.GamerData["key"] == "value", "Wrong achievement data");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Fetches the transaction history.")]
	public IEnumerator ShouldFetchTransactionHistory() {
		LoginNewUser(cloud, gamer => {
			gamer.Transactions.Post(Bundle.CreateObject("gold", 10), "Transaction run by integration test.")
			.ExpectSuccess(dummy => gamer.Transactions.History())
			.ExpectSuccess(histResult => {
				Assert(histResult.Count == 1, "Expected one history entry");
				Assert(histResult[0].Description == "Transaction run by integration test.", "Wrong description");
				Assert(histResult[0].TxData["gold"] == 10, "Wrong tx data");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Tests the pagination feature of the transaction history by creating one user and three transactions.")]
	public IEnumerator ShouldHavePaginationInTxHistory() {
		LoginNewUser(cloud, gamer => {
			// Run 3 transactions serially
			gamer.Transactions.Post(Bundle.CreateObject("gold", 1))
			.ExpectSuccess(dummy => gamer.Transactions.Post(Bundle.CreateObject("gold", 2, "silver", 10)))
			.ExpectSuccess(dummy => gamer.Transactions.Post(Bundle.CreateObject("gold", 3)))
			// All transactions have been executed. Default to page 1.
			.ExpectSuccess(dummy => gamer.Transactions.History(limit: 2))
			.ExpectSuccess(tx => {
				// Even though there are three, only two results should be returned
				Assert(tx.Count == 2, "Expected two entries in history");
				// Then fetch the next page
				Assert(!tx.HasPrevious, "Should not have previous page");
				Assert(tx.HasNext, "Should have next page");
				return tx.FetchNext();
			})
			.ExpectSuccess(tx => {
				Assert(tx.Offset == 2, "Expected offset: 2");
				Assert(tx.Count == 1, "Expected one value at page 2");
				Assert(tx.HasPrevious, "Should have previous page");
				Assert(!tx.HasNext, "Should not have next page");
				return tx.FetchPrevious();
			})
			.ExpectSuccess(tx => {
				Assert(tx.Count == 2, "Expected two entries in history");
				Assert(!tx.HasPrevious, "Should not have previous page");
				Assert(tx.HasNext, "Should have next page");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Tests the filter by unit feature of the transaction history")]
	public IEnumerator ShouldFilterTransactionsByUnit() {
		LoginNewUser(cloud, gamer => {
			// Run 3 transactions serially
			gamer.Transactions.Post(Bundle.CreateObject("gold", 1))
			.ExpectSuccess(dummy => gamer.Transactions.Post(Bundle.CreateObject("gold", 2, "silver", 10)))
			.ExpectSuccess(dummy => gamer.Transactions.Post(Bundle.CreateObject("gold", 3)))
			// All transactions have been executed. Default to page 1.
			.ExpectSuccess(dummy => gamer.Transactions.History(unit: "silver"))
			.ExpectSuccess(histResult => {
				Assert(histResult.Count == 1, "Expected one value in history");
				Assert(histResult[0].TxData["gold"] == 2, "Expected tx: {gold: 2}");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

	[Test("Lists the state of achievements for the current user, including when a bit of progress was made.", requisite: BoAchievementConfiguration)]
	/*
		Backend prerequisites >> Add the following achievement:
		
		Name: "unitTestAchievements"
		Unit: "gold"
		Trigger value: "100"
		Reward: "{}"
	*/
	public IEnumerator ShouldListAchievements() {
        LoginNewUser(cloud, gamer => {
			gamer.Achievements.List()
			.ExpectSuccess(result => {
				Assert(result.ContainsKey("unitTestAchievements"), "'unitTestAchievements' not found, check that you have configured the required achievements on the server");
				Assert(result["unitTestAchievements"].Config["unit"] == "gold", "Expected unit to be gold");
				Assert(result["unitTestAchievements"].Config["maxValue"] == 100, "Expected maxValue to be 100");
				Assert(result["unitTestAchievements"].Progress == 0, "Expected progress to be 0");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}
}
