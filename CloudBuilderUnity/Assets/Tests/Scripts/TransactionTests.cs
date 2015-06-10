using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class TransactionTests : TestBase {

	[InstanceMethod(typeof(TransactionTests))]
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
			Assert(loginResult.IsSuccessful, "Failed to log in");

			Gamer gamer = loginResult.Value;
			Bundle tx = Bundle.CreateObject("gold", 10, "silver", 100);
			// Set property, then get all and check it
			gamer.Transactions.Post(
				transaction: tx,
				description: "Transaction run by integration test.",
				done: txResult => {
					Assert(txResult.IsSuccessful, "Error when running transaction");
					Assert(txResult.Value.Balance["gold"] == 10, "Gold is not set properly");
					Assert(txResult.Value.Balance["silver"] == 100, "Silver is not set properly");

					gamer.Transactions.Balance(balance => {
						Assert(balance.IsSuccessful, "Failed to fetch balance");
						Assert(balance.Value["gold"] == 10, "Expected gold: 10 in balance");
						Assert(balance.Value["silver"] == 100, "Expected silver: 100 in balance");
						CompleteTest();
					});
				}
			);
		});
	}

	[Test("Runs a transaction that resets the balance. Tests the transaction syntax and read balance.")]
	public void ShouldResetBalance(Clan clan) {
		clan.LoginAnonymously(gamer => {
			Assert(gamer.IsSuccessful, "Failed to log in");
			// Set property, then get all and check it
			gamer.Value.Transactions.Post(txResult => {
				Assert(txResult.IsSuccessful, "Error when running transaction");
				Assert(txResult.Value.Balance["gold"] == 10, "Balance not affected properly");

				gamer.Value.Transactions.Post(txResult2 => {
					Assert(txResult2.IsSuccessful, "Failed to post transaction");
					Assert(txResult2.Value.Balance["gold"] == 0, "Expected gold: 0 balance");
					CompleteTest();
				}, Bundle.CreateObject("gold", "-auto"), "Run from integration test");
			}, Bundle.CreateObject("gold", 10), "Transaction run by integration test.");
		});
	}

	[Test("Runs a transaction that should trigger an achievement.", requisite: "Please import {\"testAch\":{\"type\":\"limit\",\"config\":{\"unit\":\"gold\",\"maxValue\":\"100\"}}} into the current game achievements.")]
	public void ShouldTriggerAchievement(Clan clan) {
		clan.LoginAnonymously(gamer => {
			Assert(gamer.IsSuccessful, "Failed to log in");
			gamer.Value.Transactions.Post(txResult => {
				Assert(txResult.IsSuccessful, "Failed transaction");
				Assert(txResult.Value.TriggeredAchievements.Count == 1, "Expected one achievement triggered");
				Assert(txResult.Value.TriggeredAchievements["testAch"].Name == "testAch", "Expected testAch to be triggered");
				Assert(txResult.Value.TriggeredAchievements["testAch"].Type == AchievementType.Limit, "Expected testAch.type: limit");
				Assert(txResult.Value.TriggeredAchievements["testAch"].Config["maxValue"] == 100, "Expected testAch.maxValue: 100");
				CompleteTest();
			}, Bundle.CreateObject("gold", 100), "Transaction run by integration test.");
		});
	}

	[Test("Fetches the transaction history.")]
	public void ShouldFetchTransactionHistory(Clan clan) {
		clan.LoginAnonymously(gamer => {
			Assert(gamer.IsSuccessful, "Failed to log in");
			gamer.Value.Transactions.Post(txResult => {
				Assert(txResult.IsSuccessful, "Failed to run transaction");
				gamer.Value.Transactions.History(histResult => {
					Assert(histResult.IsSuccessful, "Failed to fetch history");
					Assert(histResult.Value.Count == 1, "Expected one history entry");
					Assert(histResult.Value[0].Description == "Transaction run by integration test.", "Wrong description");
					Assert(histResult.Value[0].TxData["gold"] == 10, "Wrong tx data");
					CompleteTest();
				});
			}, Bundle.CreateObject("gold", 10), "Transaction run by integration test.");
		});
	}

	[Test("Tests the pagination feature of the transaction history by creating one user and three transactions.")]
	public void ShouldHavePaginationInTxHistory(Clan clan) {
		clan.LoginAnonymously(gamer => {
			Assert(gamer.IsSuccessful, "Failed to log in");
			// Run 3 transactions serially
			Bundle[] transactions = { Bundle.CreateObject("gold", 1), Bundle.CreateObject("gold", 2, "silver", 10), Bundle.CreateObject("gold", 3) };
			bool alreadyWentBack = false;
			ExecuteTransactions(gamer.Value, transactions, () => {
				// All transactions have been executed. Default to page 1.
				gamer.Value.Transactions.History(histResult => {
					PagedList<Transaction> tx = histResult.Value;
					Assert(histResult.IsSuccessful, "Failed to fetch history");
					if (tx.Offset == 0) {
						// Even though there are three, only two results should be returned
						Assert(tx.Count == 2, "Expected two entries in history");
						// Then fetch the next page
						Assert(!tx.HasPrevious, "Should not have previous page");
						Assert(tx.HasNext, "Should have next page");
						// Coming back from the second page
						if (alreadyWentBack) {
							CompleteTest();
							return;
						}
						tx.FetchNext();
					}
					else {
						// Last result
						Assert(tx.Offset == 2, "Expected offset: 2");
						Assert(tx.Count == 1, "Expected one value at page 2");
						Assert(tx.HasPrevious, "Should have previous page");
						Assert(!tx.HasNext, "Should not have next page");
						tx.FetchPrevious();
						alreadyWentBack = true;
					}
				}, limit: 2);

			})(null);
		});
	}

	[Test("Tests the filter by unit feature of the transaction history")]
	public void ShouldFilterTransactionsByUnit(Clan clan) {
		clan.LoginAnonymously(gamer => {
			Assert(gamer.IsSuccessful, "Failed to log in");
			// Run 3 transactions serially
			Bundle[] transactions = { Bundle.CreateObject("gold", 1), Bundle.CreateObject("gold", 2, "silver", 10), Bundle.CreateObject("gold", 3) };
			ExecuteTransactions(gamer.Value, transactions, () => {
				// All transactions have been executed. Default to page 1.
				gamer.Value.Transactions.History(histResult => {
					Assert(histResult.IsSuccessful, "Failed to fetch history");
					Assert(histResult.Value.Count == 1, "Expected one value in history");
					Assert(histResult.Value[0].TxData["gold"] == 2, "Expected tx: {gold: 2}");
					CompleteTest();
				}, unit: "silver");
			})(null);
		});
	}

	// Makes a handler that allows to execute several sample transactions and a handler when done
	private ResultHandler<TransactionResult> ExecuteTransactions(Gamer gamer, Bundle[] transactionList, Action done, int txCounter = 0) {
		return txResult => {
			if (txCounter < transactionList.Length) {
				gamer.Transactions.Post(
					ExecuteTransactions(gamer, transactionList, done, txCounter + 1),
					transactionList[txCounter]
				);
			}
			else {
				done();
			}
		};
	}
}
