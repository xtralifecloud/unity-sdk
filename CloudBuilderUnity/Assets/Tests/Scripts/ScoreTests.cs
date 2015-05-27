using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class ScoreTests : TestBase {

	[InstanceMethod(typeof(ScoreTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("This test posts a score four times in a different fashion, and checks that the behaviour is as intended. No read of the rankings is done.")]
	public void ShouldPostScore(Clan clan) {
		Login(clan, gamer => {
			string board = RandomBoardName();
			// Should post the first score (rank 1)
			gamer.Scores.Post(
				score: 1234,
				board: board,
				order: ScoreOrder.HighToLow,
				scoreInfo: "test",
				forceSave: false,
				done: result1 => {
					Assert(result1.IsSuccessful, "Operation failed");
					Assert(result1.Value.Rank == 1 && result1.Value.HasBeenSaved, "Didn't save the score");

					// Higher score replaces the previous one
					gamer.Scores.Post(
						score: 1235,
						board: board,
						order: ScoreOrder.HighToLow,
						scoreInfo: "test",
						forceSave: false,
						done: result2 => {
							Assert(result2.IsSuccessful, "Operation failed");
							Assert(result2.Value.Rank == 1 && result2.Value.HasBeenSaved, "Didn't save higher score");

							// The score should not be taken in account (same)
							gamer.Scores.Post(
								score: 1234,
								board: board,
								order: ScoreOrder.HighToLow,
								scoreInfo: "test",
								forceSave: false,
								done: result3 => {
									Assert(result3.IsSuccessful, "Operation failed");
									// Note that we are not really second, we would be second if the score was taken in account
									Assert(result3.Value.Rank == 2 && !result3.Value.HasBeenSaved, "Shouldn't take in account lower score");

									// Forces taking in account the score although lower
									gamer.Scores.Post(
										score: 1234,
										board: board,
										order: ScoreOrder.HighToLow,
										scoreInfo: "test",
										forceSave: true,
										done: result4 => {
											Assert(result4.IsSuccessful, "Operation failed");
											Assert(result4.Value.Rank == 1 && result4.Value.HasBeenSaved, "Should forcedly take in account lower score");
											CompleteTest();
										}
									);
								}
							);
						}
					);
				}
			);
		});
	}

	[Test("Tests that the order of scores posted by two users (not friends) match, and checks the format of scores as returned by the API.")]
	public void ShouldFetchScores(Clan clan) {
		// Use two players, P1 makes a score of 1000, P2 of 1500
		Login2Users(clan, (Gamer gamer1, Gamer gamer2) => {
			string board = RandomBoardName();
			gamer1.Scores.Post(postResult1 => {
				Assert(postResult1.IsSuccessful, "Post P1 failed");

				gamer2.Scores.Post(postResult2 => {
					Assert(postResult2.IsSuccessful, "Post P2 failed");

					gamer1.Scores.List(scores => {
						Assert(scores.IsSuccessful, "Fetch scores failed");
						Assert(scores.Total == 2, "Should have two scores");
						Assert(scores.Values[0].Value == 1500, "First score not as expected");
						Assert(scores.Values[0].Info == "TestGamer2", "First score info not as expected");
						Assert(scores.Values[1].Value == 1000, "2nd score not as expected");
						Assert(scores.Values[1].GamerInfo.GamerId == gamer1.GamerId, "2nd score not as expected");
						CompleteTest();
					}, board);
				}, 1500, board, ScoreOrder.HighToLow, "TestGamer2", false);
			}, 1000, board, ScoreOrder.HighToLow, "TestGamer1", false);
		});
	}

	[Test("Tests the ranking functionality (allowing to know how a gamer would rank if the score was posted).")]
	public void ShouldProvideRank(Clan clan) {
		Login(clan, gamer => {
			string board = RandomBoardName();
			gamer.Scores.Post(postResult => {
				Assert(postResult.IsSuccessful, "Post score failed");

				gamer.Scores.GetRank(result => {
					Assert(result.IsSuccessful, "Get rank failed");
					Assert(result.Value == 2);
					CompleteTest();
				}, 800, board);
			}, 1000, board, ScoreOrder.HighToLow);
		});
	}

	[Test("Tests fetching a leaderboard amongst friends.")]
	public void ShouldListScoreOfFriends(Clan clan) {
		// Create 2 users
		clan.LoginAnonymously(gamer1 => {
			Assert(gamer1.IsSuccessful, "Login P1 failed");
			clan.LoginAnonymously(gamer2 => {
				Assert(gamer2.IsSuccessful, "Login P2 failed");
				// Post 1 score each
				string board = RandomBoardName();
				gamer1.Value.Scores.Post(postResult1 => {
					Assert(postResult1.IsSuccessful, "Post P1 failed");
					gamer2.Value.Scores.Post(postResult2 => {
						Assert(postResult2.IsSuccessful, "Post P2 failed");
						// Ok so now the two friends are not friends, so the scores returned should not include the other
						gamer1.Value.Scores.ListFriendScores(scores => {
							Assert(scores.IsSuccessful, "Fetch scores failed");
							Assert(scores.Value.Count == 1, "Should have one score only");
							Assert(scores.Value[0].GamerInfo.GamerId == gamer1.Value.GamerId, "Should contain my score");
							// So let's become friends!
							gamer2.Value.Community.AddFriend(friendResult => {
								Assert(friendResult.IsSuccessful, "Become friends failed");
								// And try again fetching scores
								gamer1.Value.Scores.ListFriendScores(scoresWhenFriend => {
									Assert(scoresWhenFriend.IsSuccessful, "Fetch scores failed");
									Assert(scoresWhenFriend.Value.Count == 2, "Should have two scores only");
									CompleteTest();
								}, board);
							}, gamer1.Value.GamerId);
						}, board);
					}, 1500, board, ScoreOrder.HighToLow, "TestGamer2", false);
				}, 1000, board, ScoreOrder.HighToLow, "TestGamer1", false);
			});
		});
	}

	#region Private
	private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
