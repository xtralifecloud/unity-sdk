using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class MatchTests : TestBase {

	[InstanceMethod(typeof(MatchTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("Creates a match with the minimum number of arguments and checks that it is created properly (might highlight problems with the usage of the Bundle class).")]
	public void ShouldCreateMatchWithMinimumArgs(Clan clan) {
		Login(clan, gamer => {
			gamer.Matches.Create(
				maxPlayers: 2,
				done: (Result<Match> result) => {
					Assert(result.IsSuccessful, "Creating match failed");
					CompleteTest();
				}
			);
		});
	}

	[Test("Creates a match, and verifies that the match object seems to be configured appropriately.")]
	public void ShouldCreateMatch(Clan clan) {
		string matchDesc = "Test match";
		Login(clan, gamer => {
			gamer.Matches.Create(
				description: matchDesc,
				maxPlayers: 2,
				customProperties: Bundle.CreateObject("test", "value"),
				shoe: Bundle.CreateArray(1, 2, 3),
				done: (Result<Match> result) => {
					Match match = result.Value;
					Assert(result.IsSuccessful, "Creating match failed");
					Assert(match.Creator.GamerId == gamer.GamerId, "Match creator not set properly");
					Assert(match.CustomProperties["test"] == "value", "Missing custom property");
					Assert(match.Description == matchDesc, "Invalid match description");
					Assert(match.Events.Count == 0, "Should not have any event at first");
					Assert(match.GlobalState.AsDictionary().Count == 0, "Global state should be empty initially");
					Assert(match.LastEventId == null, "Last event should be null");
					Assert(match.MatchId != null, "Match ID shouldn't be null");
					Assert(match.MaxPlayers == 2, "Should have two players");
					Assert(match.Players.Count == 1, "Should contain only one player");
					Assert(match.Players[0].GamerId == gamer.GamerId, "Should contain me as player");
					Assert(match.Seed != 0, "A 31-bit seed should be provided");
					Assert(match.Shoe.AsArray().Count == 0, "The shoe shouldn't be available until the match is finished");
					Assert(match.Status == MatchStatus.Running, "The match status is invalid");
					CompleteTest();
				}
			);
		});
	}

	[Test("Creates a match, and fetches it then, verifying that the match can be continued properly.")]
	public void ShouldContinueMatch(Clan clan) {
		Login(clan, gamer => {
			gamer.Matches.Create(createResult => {
				Assert(createResult.IsSuccessful, "Creating match failed");
				string matchId = createResult.Value.MatchId;
				gamer.Matches.Fetch(fetchResult => {
					Assert(fetchResult.IsSuccessful, "Fetching match failed");
					Assert(fetchResult.Value.MatchId == createResult.Value.MatchId, "The fetched match doesn't correspond to the created one");
					Assert(fetchResult.Value.Players[0].GamerId == gamer.GamerId, "Should contain me as player");
					CompleteTest();
				}, matchId);
			}, 2);
		});
	}

	[Test("Creates a match as one user, and joins with another. Also tries to join again with the same user and expects an error.")]
	public void ShouldJoinMatch(Clan clan) {
		Login2Users(clan, (Gamer creator, Gamer joiner) => {
			creator.Matches.Create(createdMatch => {
				Assert(createdMatch.IsSuccessful, "Match creation failed");
				// Creator should not be able to join again
				creator.Matches.Join(triedJoin => {
					Assert(!triedJoin.IsSuccessful, "Should not succeed to join match already part of");
					// But the second player should
					joiner.Matches.Join(joined => {
						// Check that the match looks usable
						Assert(joined.IsSuccessful, "Failed to join match");
						Assert(joined.Value.MatchId == createdMatch.Value.MatchId, "The fetched match doesn't correspond to the created one");
						Assert(joined.Value.Players[0].GamerId == creator.GamerId, "Should contain creator as player 1");
						Assert(joined.Value.Players[1].GamerId == joiner.GamerId, "Should contain joiner as player 2");
						CompleteTest();
					}, createdMatch.Value.MatchId);
				}, createdMatch.Value.MatchId);
			}, 2);
		});
	}

	[Test("Creates a match and attempts to delete it, and expects it to fail.")]
	public void ShouldFailToDeleteMatch(Clan clan) {
		Login(clan, gamer => {
			gamer.Matches.Create(createResult => {
				Assert(createResult.IsSuccessful, "Failed to create match");
				gamer.Matches.DeleteMatch(deleteResult => {
					Assert(!deleteResult.IsSuccessful, "Failed to delete match");
					Assert(deleteResult.ServerData["name"] == "MatchNotFinished", "Should not be able to delete match");
					CompleteTest();
				}, createResult.Value.MatchId);
			}, 2);
		});
	}

	#region Private
	private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
