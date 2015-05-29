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

	[Test("Big test that creates a match and simulates playing it with two players. Tries a bit of everything in the API.")]
	public void ShouldPlayMatch(Clan clan) {
		Login2Users(clan, (Gamer gamer1, Gamer gamer2) => {
			// Create a match
			gamer1.Matches.Create(matchCreated => {
				var matchP1 = matchCreated.Value;
				Assert(matchCreated.IsSuccessful, "Failed to create match");

				// Join with P2
				gamer2.Matches.Join(matchJoined => {
					var matchP2 = matchJoined.Value;
					Assert(matchJoined.IsSuccessful, "Failed to join match");
					Assert(matchP2.Events.Count == 1, "Should have one join event");
					Assert(matchP2.Events[0] is MatchJoinEvent, "First event should be join");
					
					// Post a move
					matchP2.PostMove(matchPosted => {
						Assert(matchP2.Events.Count == 2, "Should have a move event");
						Assert(matchP2.LastEventId != null, "Last event ID shouldn't be null");
						Assert(matchP2.Players.Count == 2, "Should contain two players");

						MatchMoveEvent e = (MatchMoveEvent)matchP2.Events[1];
						Assert(e.Match.MatchId == matchP2.MatchId, "The match in the event doesn't match");
						
						// Post another move with global state
						matchP2.PostMove(matchPostedGlobalState => {
							Assert(matchPostedGlobalState.IsSuccessful, "Failed to post global move");
							Assert(matchP2.Events.Count == 1, "Posting a global state should clear events");
							Assert(matchP2.GlobalState["key"] == "value", "The global state should have been updated");

							// Now make P2 leave
							matchP2.Leave(leftMatch => {
								// Then update P1's match, and check that it reflects changes made by P2.
								// Normally these changes should be fetched automatically via events, but we don't handle them in this test.
								gamer1.Matches.Fetch(matchRefreshed => {
									Assert(matchRefreshed.IsSuccessful, "Failed to refresh match");
									matchP1 = matchRefreshed.Value;
									Assert(matchP1.Events.Count == 2, "Should have a move & refresh events (after refresh)");
									Assert(matchP1.LastEventId == matchP2.LastEventId, "Last event ID should match");
									Assert(matchP1.Players.Count == 1, "Should only contain one player after P2 has left");

									// Then finish the match & delete for good
									matchP1.Finish(matchFinished => {
										Assert(matchFinished.IsSuccessful, "Failed to finish the match");
										// The match should have been deleted
										gamer1.Matches.Fetch(deletedMatch => {
											Assert(!deletedMatch.IsSuccessful, "The match shouldn't exist");
											Assert(deletedMatch.ServerData["name"] == "BadMatchID", "Expected bad match ID");
											CompleteTest();
										}, matchP1.MatchId);
									}, true);
								}, matchP1.MatchId);
							});
						}, Bundle.CreateObject("x", 2), Bundle.CreateObject("key", "value"));
					}, Bundle.CreateObject("x", 1));
				}, matchCreated.Value.MatchId);
			}, 2);
		});
	}

	#region Private
	private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
