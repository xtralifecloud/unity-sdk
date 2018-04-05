using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class ScoreTests : TestBase {

	[Test("This test posts a score four times in a different fashion, and checks that the behaviour is as intended. No read of the rankings is done.")]
	public void ShouldPostScore() {
		Login(cloud, gamer => {
			string board = RandomBoardName();
			// Should post the first score (rank 1)
			gamer.Scores.Post(
				score: 1234,
				board: board,
				order: ScoreOrder.HighToLow,
				scoreInfo: "test",
				forceSave: false)
			.ExpectSuccess(result1 => {
				Assert(result1.Rank == 1 && result1.HasBeenSaved, "Didn't save the score");

				// Higher score replaces the previous one
				return gamer.Scores.Post(
					score: 1235,
					board: board,
					order: ScoreOrder.HighToLow,
					scoreInfo: "test",
					forceSave: false);
			})
			.ExpectSuccess(result2 => {
				Assert(result2.Rank == 1 && result2.HasBeenSaved, "Didn't save higher score");

				// The score should not be taken in account (same)
				return gamer.Scores.Post(
					score: 1234,
					board: board,
					order: ScoreOrder.HighToLow,
					scoreInfo: "test",
					forceSave: false);
			})
			.ExpectSuccess(result3 => {
				// Note that we are not really second, we would be second if the score was taken in account
				Assert(result3.Rank == 2 && !result3.HasBeenSaved, "Shouldn't take in account lower score");

				// Forces taking in account the score although lower
				return gamer.Scores.Post(
					score: 1234,
					board: board,
					order: ScoreOrder.HighToLow,
					scoreInfo: "test",
					forceSave: true);
			})
			.ExpectSuccess(result4 => {
				Assert(result4.Rank == 1 && result4.HasBeenSaved, "Should forcedly take in account lower score");
				CompleteTest();
			});
		});
	}

	[Test("Tests that the order of scores posted by two users (not friends) match, and checks the format of scores as returned by the API.")]
	public void ShouldFetchScores() {
		// Use two players, P1 makes a score of 1000, P2 of 1500
		Login2Users(cloud, (Gamer gamer1, Gamer gamer2) => {
			string board = RandomBoardName();
            gamer1.Scores.Post(1000, board, ScoreOrder.HighToLow, "TestGamer1", false)
            .ExpectSuccess(postResult1 => gamer2.Scores.Post(1500, board, ScoreOrder.HighToLow, "TestGamer2", false))
            .ExpectSuccess(postResult2 => gamer1.Scores.BestHighScores(board))
            .ExpectSuccess(scores => {
                Assert(scores.Count == 2, "Should have two scores, got " + scores.Total);
                Assert(scores[0].Value == 1500, "First score not as expected");
                Assert(scores[0].Info == "TestGamer2", "First score info not as expected");
                Assert(scores[0].Rank == 1, "First score should have rank 1");
                Assert(scores[1].Value == 1000, "2nd score not as expected");
                Assert(scores[1].GamerInfo.GamerId == gamer1.GamerId, "2nd score not as expected");
                Assert(scores.ServerData[board]["rankOfFirst"] == 1, "Server data should be accessible");
                return gamer1.Scores.PagedCenteredScore(board);
            })
            .ExpectSuccess(scores => {
                Assert(scores.Count == 2, "Should have two scores, got " + scores.Total);
                Assert(scores[0].Value == 1500, "First score not as expected");
                Assert(scores[0].Info == "TestGamer2", "First score info not as expected");
                Assert(scores[0].Rank == 1, "First score should have rank 1");
                Assert(scores[1].Value == 1000, "2nd score not as expected");
                Assert(scores[1].GamerInfo.GamerId == gamer1.GamerId, "2nd score not as expected");
                Assert(scores.ServerData[board]["rankOfFirst"] == 1, "Server data should be accessible");
                CompleteTest();
            });
		});
	}

	[Test("Tests the ranking functionality (allowing to know how a gamer would rank if the score was posted).")]
	public void ShouldProvideRank() {
		Login(cloud, gamer => {
			string board = RandomBoardName();
			gamer.Scores.Post(1000, board, ScoreOrder.HighToLow)
			.ExpectSuccess(postResult => gamer.Scores.GetRank(800, board))
			.ExpectSuccess(rank => {
				Assert(rank == 2, "Expected rank: 2");
				CompleteTest();
			});
		});
	}

	[Test("Tests fetching a leaderboard amongst friends.")]
	public void ShouldListScoreOfFriends() {
		// Create 2 users
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			// Post 1 score each
			string board = RandomBoardName();
			gamer1.Scores.Post(1000, board, ScoreOrder.HighToLow, "TestGamer1", false)
			.ExpectSuccess(dummy => gamer2.Scores.Post(1500, board, ScoreOrder.HighToLow, "TestGamer2", false))
			// We need to wait a bit else the results may be misleading
			.ExpectSuccess(dummy => Wait<object>(1000))
			// Ok so now the two friends are not friends, so the scores returned should not include the other
			.ExpectSuccess(dummy => gamer1.Scores.ListFriendScores(board))
			.ExpectSuccess(scores => {
				Assert(scores.Count == 1, "Should have one score only");
				Assert(scores[0].GamerInfo.GamerId == gamer1.GamerId, "Should contain my score");
				// So let's become friends!
				return gamer2.Community.AddFriend(gamer1.GamerId);
			})
			// And try again fetching scores
			.ExpectSuccess(dummy => Wait<object>(1000))
			.ExpectSuccess(friendResult => gamer1.Scores.ListFriendScores(board))
			.ExpectSuccess(scoresWhenFriend => {
				Assert(scoresWhenFriend.Count == 2, "Should have two scores only");
				Assert(scoresWhenFriend[1].Rank == 2, "Second score should have rank 2");
				CompleteTest();
			});
		});
	}

	[Test("Creates two boards, posts scores to it and lists the best scores.")]
	public void ShouldListUserBestScores() {
		LoginNewUser(cloud, gamer => {
			string board1 = RandomBoardName(), board2 = RandomBoardName();
			gamer.Scores.Post(1000, board1, ScoreOrder.HighToLow, "Test1", false)
			.ExpectSuccess(dummy => gamer.Scores.Post(1200, board2, ScoreOrder.HighToLow, "Test2", false))
			.ExpectSuccess(dummy => gamer.Scores.ListUserBestScores())
			// Then the scores should return the two boards
			.ExpectSuccess(result => {
				Assert(result[board1].Rank == 1, "Rank is not 1");
				Assert(result[board2].Rank == 1, "Rank board #2 should be 1");
				CompleteTest();
			});
		});
	}

	#region Private
	private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
