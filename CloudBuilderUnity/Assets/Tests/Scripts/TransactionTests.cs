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
						Assert(balance.IsSuccessful);
						Assert(balance.Value["gold"] == 10);
						Assert(balance.Value["silver"] == 100);
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
					Assert(txResult2.IsSuccessful);
					Assert(txResult2.Value.Balance["gold"] == 0);
					CompleteTest();
				}, Bundle.CreateObject("gold", "-auto"), "Run from integration test");
			}, Bundle.CreateObject("gold", 10), "Transaction run by integration test.");
		});
	}

	[Test("Runs a transaction that should trigger an achievement.", requisite: "The corresponding achievement ('testAch' with gold reaching 100) must be configured on the server.")]
	public void ShouldTriggerAchievement(Clan clan) {
		clan.LoginAnonymously(gamer => {
			Assert(gamer.IsSuccessful, "Failed to log in");
			gamer.Value.Transactions.Post(txResult => {
				Assert(txResult.IsSuccessful, "Failed transaction");
				Assert(txResult.Value.TriggeredAchievements.Count == 1);
				Assert(txResult.Value.TriggeredAchievements["testAch"].Name == "testAch");
				Assert(txResult.Value.TriggeredAchievements["testAch"].Type == AchievementType.Limit);
				Assert(txResult.Value.TriggeredAchievements["testAch"].Config["maxValue"] == 100);
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
					Assert(histResult.IsSuccessful);
					Assert(histResult.Values.Count == 1);
					Assert(histResult.Values[0].Description == "Transaction run by integration test.");
					Assert(histResult.Values[0].TxData["gold"] == 10);
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
					Assert(histResult.IsSuccessful);
					if (histResult.Offset == 0) {
						// Even though there are three, only two results should be returned
						Assert(histResult.Values.Count == 2);
						// Then fetch the next page
						Assert(!histResult.HasPrevious);
						Assert(histResult.HasNext);
						// Coming back from the second page
						if (alreadyWentBack) {
							CompleteTest();
							return;
						}
						histResult.FetchNext();
					}
					else {
						// Last result
						Assert(histResult.Offset == 2);
						Assert(histResult.Values.Count == 1);
						Assert(histResult.HasPrevious);
						Assert(!histResult.HasNext);
						histResult.FetchPrevious();
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
					Assert(histResult.IsSuccessful);
					Assert(histResult.Values.Count == 1);
					Assert(histResult.Values[0].TxData["gold"] == 2);
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
