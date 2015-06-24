using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;
using System.Threading;
using System.Collections.Generic;
using System.Net;

public class MatchTests : TestBase {

	[InstanceMethod(typeof(MatchTests))]
	public string TestMethodName;

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("Creates a match with the minimum number of arguments and checks that it is created properly (might highlight problems with the usage of the Bundle class).")]
	public void ShouldCreateMatchWithMinimumArgs(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.Matches.Create(maxPlayers: 2)
			.CompleteTestIfSuccessful();
		});
	}

	[Test("Creates a match, and verifies that the match object seems to be configured appropriately.")]
	public void ShouldCreateMatch(Cloud cloud) {
		string matchDesc = "Test match";
		Login(cloud, gamer => {
			gamer.Matches.Create(
				description: matchDesc,
				maxPlayers: 2,
				customProperties: Bundle.CreateObject("test", "value"),
				shoe: Bundle.CreateArray(1, 2, 3))
			.ExpectSuccess(match => {
				Assert(match.Creator.GamerId == gamer.GamerId, "Match creator not set properly");
				Assert(match.CustomProperties["test"] == "value", "Missing custom property");
				Assert(match.Description == matchDesc, "Invalid match description");
				Assert(match.Moves.Count == 0, "Should not have any move at first");
				Assert(match.GlobalState.AsDictionary().Count == 0, "Global state should be empty initially");
				Assert(match.LastEventId != null, "Last event should not be null");
				Assert(match.MatchId != null, "Match ID shouldn't be null");
				Assert(match.MaxPlayers == 2, "Should have two players");
				Assert(match.Players.Count == 1, "Should contain only one player");
				Assert(match.Players[0].GamerId == gamer.GamerId, "Should contain me as player");
				Assert(match.Seed != 0, "A 31-bit seed should be provided");
				Assert(match.Shoe.AsArray().Count == 0, "The shoe shouldn't be available until the match is finished");
				Assert(match.Status == MatchStatus.Running, "The match status is invalid");
				CompleteTest();
			});
		});
	}

	[Test("Creates a match, and fetches it then, verifying that the match can be continued properly.")]
	public void ShouldContinueMatch(Cloud cloud) {
		Login(cloud, gamer => {
			gamer.Matches.Create(2)
			.ExpectSuccess(createdMatch => {
				string matchId = createdMatch.MatchId;
				gamer.Matches.Fetch(matchId)
				.ExpectSuccess(fetchedMatch => {
					Assert(fetchedMatch.MatchId == createdMatch.MatchId, "The fetched match doesn't correspond to the created one");
					Assert(fetchedMatch.Players[0].GamerId == gamer.GamerId, "Should contain me as player");
					CompleteTest();
				});
			});
		});
	}

	[Test("Creates a match as one user, and joins with another. Also tries to join again with the same user and expects an error.")]
	public void ShouldJoinMatch(Cloud cloud) {
		Login2Users(cloud, (Gamer creator, Gamer joiner) => {
			creator.Matches.Create(2)
			.ExpectSuccess(createdMatch => {

				// Creator should not be able to join again
				creator.Matches.Join(createdMatch.MatchId)
				.ExpectFailure(triedJoin => {
	
					// But the second player should
					joiner.Matches.Join(createdMatch.MatchId)
					.ExpectSuccess(joined => {
						// Check that the match looks usable
						Assert(joined.MatchId == createdMatch.MatchId, "The fetched match doesn't correspond to the created one");
						Assert(joined.Players[0].GamerId == creator.GamerId, "Should contain creator as player 1");
						Assert(joined.Players[1].GamerId == joiner.GamerId, "Should contain joiner as player 2");
						CompleteTest();
					});
				});
			});
		});
	}

	[Test("Creates a match and attempts to delete it, and expects it to fail.")]
	public void ShouldFailToDeleteMatch(Cloud cloud) {
		Login(cloud, gamer => {

			gamer.Matches.Create(2)
			.ExpectSuccess(createResult => {
	
				gamer.Matches.Delete(createResult.MatchId)
				.ExpectFailure(deleteResult => {
					Assert(deleteResult.ServerData["name"] == "MatchNotFinished", "Should not be able to delete match");
					CompleteTest();
				});
			});
		});
	}

	[Test("Big test that creates a match and simulates playing it with two players. Tries a bit of everything in the API.")]
	public void ShouldPlayMatch(Cloud cloud) {
		Login2Users(cloud, (Gamer gamer1, Gamer gamer2) => {
			// Create a match
			gamer1.Matches.Create(maxPlayers: 2).ExpectSuccess(matchP1 => {
				// Join with P2
				gamer2.Matches.Join(matchP1.MatchId).ExpectSuccess(matchP2 => {
					
					// Post a move
					matchP2.PostMove(Bundle.CreateObject("x", 1))
					.ExpectSuccess(movePosted => {
						Assert(matchP2.Moves.Count == 1, "Should have a move event");
						Assert(matchP2.LastEventId != null, "Last event ID shouldn't be null");
						Assert(matchP2.Players.Count == 2, "Should contain two players");
						
						// Post another move with global state
						matchP2.PostMove(Bundle.CreateObject("x", 2), Bundle.CreateObject("key", "value"))
						.ExpectSuccess(matchPostedGlobalState => {
							Assert(matchP2.Moves.Count == 1, "Posting a global state should clear events");
							Assert(matchP2.GlobalState["key"] == "value", "The global state should have been updated");

							// Now make P2 leave
							matchP2.Leave().ExpectSuccess(leftMatch => {
								// Then update P1's match, and check that it reflects changes made by P2.
								// Normally these changes should be fetched automatically via events, but we don't handle them in this test.
								gamer1.Matches.Fetch(matchP1.MatchId)
								.ExpectSuccess(matchRefreshed => {
									matchP1 = matchRefreshed;
									Assert(matchP1.Moves.Count == 1, "Should have a move event (after refresh)");
									Assert(matchP1.LastEventId == matchP2.LastEventId, "Last event ID should match");
									Assert(matchP1.Players.Count == 1, "Should only contain one player after P2 has left");

									// Then finish the match & delete for good
									matchP1.Finish(true).ExpectSuccess(matchFinished => {
										// The match should have been deleted
										gamer1.Matches.Fetch(matchP1.MatchId)
										.ExpectFailure(deletedMatch => {
											Assert(deletedMatch.ServerData["name"] == "BadMatchID", "Expected bad match ID");
											CompleteTest();
										});
									});
								});
							});
						});
					});
				});
			});
		});
	}

	[Test("Creates a match and plays it as two users. Checks that events are broadcasted appropriately.")]
	public void ShouldReceiveEvents(Cloud cloud) {
		Login2NewUsers(cloud, (Gamer gamer1, Gamer gamer2) => {
			DomainEventLoop loopP1 = new DomainEventLoop(gamer1).Start();
			DomainEventLoop loopP2 = new DomainEventLoop(gamer2).Start();
			gamer1.Matches.Create(maxPlayers: 2)
			.ExpectSuccess(createdMatch => {
				// P1 will receive the join event
				createdMatch.OnPlayerJoined += (Match sender, MatchJoinEvent e) => {
					// P2 has joined; wait that he subscribes to the movePosted event.
					RunOnSignal("p2subscribed", () => {
						// Ok so now P2 has joined and is ready, we can go forward and post a move
						createdMatch.PostMove(Bundle.CreateObject("x", 3)).ExpectSuccess();
					});
				};
				// Join as P2
				gamer2.Matches.Join(createdMatch.MatchId)
				.ExpectSuccess(joinedMatch => {
					// P2 will receive the move event
					joinedMatch.OnMovePosted += (Match sender, MatchMoveEvent e) => {
						Assert(e.MoveData["x"] == 3, "Invalid move data");
						Assert(e.PlayerId == gamer1.GamerId, "Expected P1 to make move");
						loopP1.Stop();
						loopP2.Stop();
						CompleteTest();
					};
					Signal("p2subscribed");
				});
			});
		});
	}

	[Test("Tests the reception of an invitation between two players")]
	public void ShouldReceiveInvitation(Cloud cloud) {
		Login2NewUsers(cloud, (Gamer gamer1, Gamer gamer2) => {
			// P1 will be invited
			DomainEventLoop loopP1 = new DomainEventLoop(gamer1).Start();
			gamer1.Matches.OnMatchInvitation += (MatchInviteEvent e) => {
				Assert(e.Inviter.GamerId == gamer2.GamerId, "Invitation should come from P2");
				// Test dismiss functionality (not needed in reality since we are stopping the loop it is registered to)
				gamer1.Matches.DiscardEventHandlers();
				loopP1.Stop();
				CompleteTest();
			};
			// P2 will create a match and invite P1
			gamer2.Matches.Create(maxPlayers: 2)
			.ExpectSuccess(createResult => {
				createResult.InvitePlayer(gamer1.GamerId).ExpectSuccess();
			});
		});
	}

	[Test("Creates a variety of matches and tests the various features of the match listing functionality.")]
	public void ShouldListMatches(Cloud cloud) {
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			int[] totalMatches = new int[1];
			Match[] matches = new Match[4];
			// Allows to better indent the code
			new AsyncOp().Then(next => {
				// 1) Create a match to which only P1 is participating
				gamer1.Matches.Create(2)
				.ExpectSuccess(m1 => {
					matches[0] = m1;
					next.Return();
				});
			})
			.Then(next => {
				// 2) Create a finished match
				gamer1.Matches.Create(2)
				.ExpectSuccess(m2 => {
					m2.Finish()
					.ExpectSuccess(finishedM2 => {
						matches[1] = m2;
						next.Return();
					});
				});
			})
			.Then(next => {
				// 3) Create a match to which we invite P1 (he should see himself)
				gamer2.Matches.Create(2)
				.ExpectSuccess(m3 => {
					// Invite P1 to match 3
					m3.InvitePlayer(gamer1.GamerId)
					.ExpectSuccess(invitedP1 => {
						matches[2] = m3;
						next.Return();
					});
				});
			})
			.Then(next => {
				// 4) Create a full match
				gamer1.Matches.Create(1)
				.ExpectSuccess(m4 => {
					matches[3] = m4;
					next.Return();
				});
			})
			.Then(next => {
				// List all matches; by default, not all matches should be returned (i.e. m1 and m3)
				gamer1.Matches.List()
				.ExpectSuccess(list => {
					Assert(list.Count >= 4, "Should have many results");
					totalMatches[0] = list.Total;
					next.Return();
				});
			})
			.Then(next => {
				// List matches to which P1 is participating (i.e. not m1 as m2 is full, m3 created by P2 and m4 full)
				gamer1.Matches.List(participating: true)
				.ExpectSuccess(list => {
					Assert(list.Count == 1, "Should have 1 match");
					Assert(list[0].MatchId == matches[0].MatchId, "M1 expected");
					next.Return();
				});
			})
			.Then(next => {
				// List matches to which P1 is invited (i.e. m3)
				gamer1.Matches.List(invited: true)
				.ExpectSuccess(list => {
					Assert(list.Count == 1, "Should have 1 match");
					Assert(list[0].MatchId == matches[2].MatchId, "M3 expected");
					next.Return();
				});
			})
			.Then(next => {
				// List all matches, including finished ones (i.e. m2)
				gamer1.Matches.List(finished: true)
				.ExpectSuccess(list => {
					Assert(list.Total > totalMatches[0], "Should list more matches");
					next.Return();
				});
			})
			.Then(next => {
				// List full matches (i.e. m4)
				gamer1.Matches.List(full: true)
				.ExpectSuccess(list => {
					Assert(list.Total > totalMatches[0], "Should list more matches");
					next.Return();
				});
			})
			.Then(() => CompleteTest())
			.Return(); // Start the deferred chain
		});
	}

	#region Private
	private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
