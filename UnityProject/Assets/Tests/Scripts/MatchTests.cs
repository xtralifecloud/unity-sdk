using UnityEngine;
using System;
using CotcSdk;
using IntegrationTests;

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
			Match[] matches = new Match[2];
			// Create a match
			gamer1.Matches.Create(maxPlayers: 2)
			.ExpectSuccess(matchP1 => {
				matches[0] = matchP1;
				// Join with P2
				return gamer2.Matches.Join(matchP1.MatchId);
			})
			.ExpectSuccess(matchP2 => {
				matches[1] = matchP2;
				// Post a move
				return matchP2.PostMove(Bundle.CreateObject("x", 1));
			})
			.ExpectSuccess(movePosted => {
				Assert(matches[1].Moves.Count == 1, "Should have a move event");
				Assert(matches[1].LastEventId != null, "Last event ID shouldn't be null");
				Assert(matches[1].Players.Count == 2, "Should contain two players");

				// Post another move with global state
				return matches[1].PostMove(Bundle.CreateObject("x", 2), Bundle.CreateObject("key", "value"));
			})
			.ExpectSuccess(matchPostedGlobalState => {
				Assert(matches[1].Moves.Count == 1, "Posting a global state should clear events");
				Assert(matches[1].GlobalState["key"] == "value", "The global state should have been updated");

				// Now make P2 leave
				return matches[1].Leave();
			})
			.ExpectSuccess(leftMatch => {
				// Then update P1's match, and check that it reflects changes made by P2.
				// Normally these changes should be fetched automatically via events, but we don't handle them in this test.
				return gamer1.Matches.Fetch(matches[0].MatchId);
			})
			.ExpectSuccess(matchRefreshed => {
				matches[0] = matchRefreshed;
				Assert(matches[0].Moves.Count == 1, "Should have a move event (after refresh)");
				Assert(matches[0].LastEventId == matches[1].LastEventId, "Last event ID should match");
				Assert(matches[0].Players.Count == 1, "Should only contain one player after P2 has left");

				// Then finish the match & delete for good
				return matches[0].Finish(true);
			})
			.ExpectSuccess(matchFinished => {
				// The match should have been deleted
				gamer1.Matches.Fetch(matches[0].MatchId)
				.ExpectFailure(deletedMatch => {
					Assert(deletedMatch.ServerData["name"] == "BadMatchID", "Expected bad match ID");
					CompleteTest();
				});
			});
		});
	}

	[Test("Creates a match and plays it as two users. Checks that events are broadcasted appropriately.")]
	public void ShouldReceiveEvents(Cloud cloud) {
        
		Login2NewUsers(cloud, (Gamer gamer1, Gamer gamer2) => {
			DomainEventLoop loopP1 = gamer1.StartEventLoop();
            DomainEventLoop loopP2 = gamer2.StartEventLoop();
			gamer1.Matches.Create(maxPlayers: 2)
            .Catch(ex => {
                Debug.LogError(ex);
            })
			.ExpectSuccess(createdMatch => {
                // 1) P1 subscribe to OnPlayerJoined
                createdMatch.OnPlayerJoined += (Match sender, MatchJoinEvent e) => {
                    // 4) P2 has joined; wait that he subscribes to the movePosted event.
                    RunOnSignal("p2subscribed", () => {
						// 5) Now that P2 has joined and is ready, P1 can post a move
						createdMatch.PostMove(Bundle.CreateObject("x", 3)).ExpectSuccess();
					});
				};
				// Join as P2
				gamer2.Matches.Join(createdMatch.MatchId)
                .Catch(ex => {
                    Debug.LogError(ex);
                })
				.ExpectSuccess(joinedMatch => {
                    // 2) P2 Subscribe to OnMovePosted
                    joinedMatch.OnMovePosted += (Match sender, MatchMoveEvent e) => {
                        // 6) P1 sent his move, P2 verify its validity. Test Complete.
                        Assert(e.MoveData["x"] == 3, "Invalid move data");
						Assert(e.PlayerId == gamer1.GamerId, "Expected P1 to make move");
						loopP1.Stop();
						loopP2.Stop();
						CompleteTest();
					};
                    // 3) Send a signal to let P1 know that P2 has done its job
					Signal("p2subscribed");
				});
			});
		});
	}

	[Test("Tests the reception of an invitation between two players")]
	public void ShouldReceiveInvitation(Cloud cloud) {
		Login2NewUsers(cloud, (Gamer gamer1, Gamer gamer2) => {
			// P2 will create a match and invite P1
			gamer2.Matches.Create(maxPlayers: 2)
			.ExpectSuccess(createMatch => {
				// P1 will be invited
				DomainEventLoop loopP1 = gamer1.StartEventLoop();
				gamer1.Matches.OnMatchInvitation += (MatchInviteEvent e) => {
					Assert(e.Inviter.GamerId == gamer2.GamerId, "Invitation should come from P2");
					Assert(e.Match.MatchId == createMatch.MatchId, "Should indicate the match ID");
					// Test discard functionality (not needed in reality since we are stopping the loop it is registered to)
					gamer1.Matches.DiscardEventHandlers();
					loopP1.Stop();
					CompleteTest();
				};
				// Invite P1
				createMatch.InvitePlayer(gamer1.GamerId).ExpectSuccess();
			});
		});
	}

    [Test("Tests that the DiscardEventHandlers works properly")]
    public void ShouldDiscardEventHandlers(Cloud cloud) {
        Login2NewUsers(cloud, (Gamer gamer1, Gamer gamer2) => {
            // P2 will create a match and invite P1
            gamer2.Matches.Create(maxPlayers: 2)
            .ExpectSuccess(createMatch => {
                // P1 will be invited
                DomainEventLoop loopP1 = gamer1.StartEventLoop();
                gamer1.Matches.OnMatchInvitation += (MatchInviteEvent e) => {
                    FailTest("Should be discarded");
                };

                gamer1.Matches.DiscardEventHandlers();

                gamer1.Matches.OnMatchInvitation += (MatchInviteEvent e) => {
                    loopP1.Stop();
                    CompleteTest();
                };
                // Invite P1
                createMatch.InvitePlayer(gamer1.GamerId).ExpectSuccess();
            });
        });
    }

    [Test("Creates a variety of matches and tests the various features of the match listing functionality.")]
	public void ShouldListMatches(Cloud cloud) {
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			int[] totalMatches = new int[1];
			Match[] matches = new Match[4];
			// 1) Create a match to which only P1 is participating
			gamer1.Matches.Create(2)
			.ExpectSuccess(m1 => {
				matches[0] = m1;
				// 2) Create a finished match
				return gamer1.Matches.Create(2);
			})
			.ExpectSuccess(m2 => {
				matches[1] = m2;
				return m2.Finish();
			})
			.ExpectSuccess(finishedM2 => {
				// 3) Create a match to which we invite P1 (he should see himself)
				return gamer2.Matches.Create(2);
			})
			.ExpectSuccess(m3 => {
				matches[2] = m3;
				// Invite P1 to match 3
				return m3.InvitePlayer(gamer1.GamerId);
			})
			.ExpectSuccess(invitedP1 => {
				// 4) Create a full match
				return gamer1.Matches.Create(1);
			})
			.ExpectSuccess(m4 => {
				matches[3] = m4;

				// List all matches; by default, not all matches should be returned (i.e. m1 and m3)
				return gamer1.Matches.List();
			})
			.ExpectSuccess(list => {
				Assert(list.Count >= 4, "Should have many results");
				totalMatches[0] = list.Total;

				// List matches to which P1 is participating (i.e. not m1 as m2 is full, m3 created by P2 and m4 full)
				return gamer1.Matches.List(participating: true);
			})
			.ExpectSuccess(list => {
				Assert(list.Count == 1, "Should have 1 match");
				Assert(list[0].MatchId == matches[0].MatchId, "M1 expected");
				
				// List matches to which P1 is invited (i.e. m3)
				return gamer1.Matches.List(invited: true);
			})
			.ExpectSuccess(list => {
				Assert(list.Count == 1, "Should have 1 match");
				Assert(list[0].MatchId == matches[2].MatchId, "M3 expected");
				
				// List all matches, including finished ones (i.e. m2)
				return gamer1.Matches.List(finished: true);
			})
			.ExpectSuccess(list => {
				Assert(list.Total > totalMatches[0], "Should list more matches");

				// List full matches (i.e. m4)
				return gamer1.Matches.List(full: true);
			})
			.ExpectSuccess(list => {
				Assert(list.Total > totalMatches[0], "Should list more matches");
				CompleteTest();
			});
		});
	}

	[Test("Tests race conditions between players.")]
	public void ShouldHandleRaceConditions(Cloud cloud) {
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			Match[] matches = new Match[2];
			// Create a match, and make P2 join it but start no event loop
			gamer1.Matches.Create(maxPlayers: 2)
			.ExpectSuccess(m => {
				matches[0] = m;
				return gamer2.Matches.Join(m.MatchId);
			})
			.ExpectSuccess(m => {
				matches[1] = m;
				// Then make P2 play into the match created by P1
				return m.PostMove(Bundle.CreateObject("x", 2));
			})
			.ExpectSuccess(dummy => {
				// P1 will now be desynchronized, try to post a move, which should fail
				matches[0].PostMove(Bundle.CreateObject("x", 3))
				.ExpectFailure(ex => {
					Assert(ex.ServerData["name"] == "InvalidLastEventId", "Move should be refused");
					// Try again after refreshing the match
					gamer1.Matches.Fetch(matches[0].MatchId)
					.ExpectSuccess(m => m.PostMove(Bundle.CreateObject("x", 3)))
					.CompleteTestIfSuccessful();
				});
			});
		});
	}

    [Test("Test the dismiss invitation functionnality")]
    public void ShouldDismissInvitation(Cloud cloud) {
        Login2NewUsers(cloud, (gamer1, gamer2) => {
            gamer2.Matches.Create(maxPlayers: 2)
            .ExpectSuccess(createMatch => {
                // Invite P1
                DomainEventLoop loopP1 = gamer1.StartEventLoop();
                gamer1.Matches.OnMatchInvitation += (MatchInviteEvent e) => {
                    Assert(e.Inviter.GamerId == gamer2.GamerId, "Invitation should come from P2");
                    Assert(e.Match.MatchId == createMatch.MatchId, "Should indicate the match ID");
                    // Test dismiss functionality (not needed in reality since we are stopping the loop it is registered to)
                    gamer1.Matches.DismissInvitation(createMatch.MatchId).ExpectSuccess(done => {
                        CompleteTest();
                    });
                    loopP1.Stop();                    
                };
                createMatch.InvitePlayer(gamer1.GamerId).ExpectSuccess();                
            });                
        });
    }


    [Test("Test if the Draw From Shoe functionnality works properly")]
    public void ShouldDrawFromShoe(Cloud cloud) {
        Login2NewUsers(cloud, (gamer1, gamer2) => {
            DomainEventLoop l1 = gamer1.StartEventLoop();
            DomainEventLoop l2 = gamer2.StartEventLoop();
            gamer1.Matches.Create(2, shoe: Bundle.CreateArray(new Bundle(1), new Bundle(2)))
            .ExpectSuccess(createMatch => {
                int drawn1 = 0, drawn2 = 0;
                createMatch.OnShoeDrawn += (match, shoeEvent) => {
                    match.DrawFromShoe()
                    .ExpectSuccess(result => {
                        drawn2 = result["drawnItems"].AsArray()[0];
                        if (drawn1 == 1)
                            Assert(drawn2 == 2, "Expected to get 2 from shoe, got " + drawn2);
                        else
                            Assert(drawn2 == 1, "Expected to get 1 from shoe, got " + drawn2);
                        return match.DrawFromShoe(2);
                    })
                    .ExpectSuccess(result => {
                        drawn1 = result["drawnItems"].AsArray()[0];
                        drawn2 = result["drawnItems"].AsArray()[1];
                        Assert((drawn1 == 1 && drawn2 == 2) || (drawn1 == 2 && drawn2 == 1), "Expected to get 1/2 or 2/1, got : " + drawn1 + "/" + drawn2);
                        l1.Stop();
                        l2.Stop();
                        CompleteTest();
                    });
                };

                gamer2.Matches.Join(createMatch.MatchId)
                .ExpectSuccess(matchJoined => {
                    matchJoined.DrawFromShoe()
                    .ExpectSuccess(result => {
                        drawn1 = result["drawnItems"].AsArray()[0];
                        Assert(drawn1 == 1 || drawn1 == 2, "Expected drawn 1 or 2 from shoe, got : " + drawn1);

                        createMatch.DrawFromShoe();
                    });          
                });
            });            
        });
    }

    #region Private
    private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
