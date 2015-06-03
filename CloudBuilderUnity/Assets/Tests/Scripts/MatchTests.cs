using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;
using System.Threading;
using System.Collections.Generic;
using System.Net;

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
					Assert(match.Moves.Count == 0, "Should not have any move at first");
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
					
					// Post a move
					matchP2.PostMove(matchPosted => {
						Assert(matchP2.Moves.Count == 1, "Should have a move event");
						Assert(matchP2.LastEventId != null, "Last event ID shouldn't be null");
						Assert(matchP2.Players.Count == 2, "Should contain two players");
						
						// Post another move with global state
						matchP2.PostMove(matchPostedGlobalState => {
							Assert(matchPostedGlobalState.IsSuccessful, "Failed to post global move");
							Assert(matchP2.Moves.Count == 1, "Posting a global state should clear events");
							Assert(matchP2.GlobalState["key"] == "value", "The global state should have been updated");

							// Now make P2 leave
							matchP2.Leave(leftMatch => {
								// Then update P1's match, and check that it reflects changes made by P2.
								// Normally these changes should be fetched automatically via events, but we don't handle them in this test.
								gamer1.Matches.Fetch(matchRefreshed => {
									Assert(matchRefreshed.IsSuccessful, "Failed to refresh match");
									matchP1 = matchRefreshed.Value;
									Assert(matchP1.Moves.Count == 1, "Should have a move event (after refresh)");
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

	[Test("Creates a match and plays it as two users. Checks that events are broadcasted appropriately.")]
	public void ShouldReceiveEvents(Clan clan) {
		Login2NewUsers(clan, (Gamer gamer1, Gamer gamer2) => {
			DomainEventLoop loopP1 = new DomainEventLoop(gamer1).Start();
			DomainEventLoop loopP2 = new DomainEventLoop(gamer2).Start();
			gamer1.Matches.Create(createdMatch => {
				Assert(createdMatch.IsSuccessful, "Failed to create match");
				// P1 will receive the join event
				createdMatch.Value.OnPlayerJoined += (Match sender, MatchJoinEvent e) => {
					// Ok so now P2 has joined and is ready, we can go forward and post a move
					createdMatch.Value.PostMove(postedMove => {
						Assert(postedMove.IsSuccessful, "Failed to post move");
					}, Bundle.CreateObject("x", 3));
				};
				// Join as P2
				gamer2.Matches.Join(joinedMatch => {
					CompleteTest();
					Assert(joinedMatch.IsSuccessful, "Failed to join match");
					// P2 will receive the move event
					joinedMatch.Value.OnMovePosted += (Match sender, MatchMoveEvent e) => {
						CompleteTest();
					};
				}, createdMatch.Value.MatchId);
			}, 2);
		});
	}

	#region Please remove
	[Test("Tests buggy HTTP client")]
	public void TEMP_TestBuggyHttpClient(Clan clan) {
		HttpWebRequest req1 = TEMP_CreateReq("http://10.211.55.2:2000/v1/gamer/event/private?timeout=5000");
		req1.BeginGetResponse(result => {
			Debug.LogWarning("Got response for req 1");
		}, null);

		HttpWebRequest req2 = TEMP_CreateReq("http://10.211.55.2:2000/v1/gamer/event/private?timeout=8000");
		req2.Headers["Authorization"] = "Basic NTU2YmZhYTUxNDYxODg2NDNkNTZhNzRiOmZlNWZjZjdiYzcwYTBjY2QwOGJjM2QyMTRjZmVhYzY4NzQxNDM0OTA=";
		req2.BeginGetResponse(result2 => {
			Debug.LogWarning("Got response for req 2");
		}, null);

		HttpWebRequest req3 = TEMP_CreateReq("http://10.211.55.2:2000/v1/vfs/private/key1");
		req3.BeginGetResponse(result3 => {
			Debug.LogWarning("Got response for key1");

			HttpWebRequest req4 = TEMP_CreateReq("http://10.211.55.2:2000/v1/vfs/private/key2");
			req4.BeginGetResponse(result4 => {
				Debug.LogWarning("Got response for key2");
			}, null);
		}, null);
	}

	[Test("Tests buggy HTTP client with threads")]
	public void TEMP_TestBuggyHttpClientThread(Clan clan) {
		ThreadPool.SetMinThreads(100, 4);
		ServicePointManager.DefaultConnectionLimit = 100;
		new Thread(new ThreadStart(() => {
			Debug.LogWarning("Req #1 on " + Thread.CurrentThread.ManagedThreadId);
			try {
				HttpWebRequest req1 = TEMP_CreateReq("http://10.211.55.2:2000/v1/gamer/event/private?timeout=5000");
				req1.GetResponse();
				Debug.LogWarning("Got response for req 1");
			}
			catch (Exception e) {
				Debug.LogWarning("EX: " + e.ToString());
			}
		})).Start();

		new Thread(new ThreadStart(() => {
			Debug.LogWarning("Req 2 on " + Thread.CurrentThread.ManagedThreadId);
			HttpWebRequest req2 = TEMP_CreateReq("http://10.211.55.2:2000/v1/gamer/event/private?timeout=7000");
//			HttpWebRequest req2 = TEMP_CreateReq("https://sandbox-api01.clanofthecloud.mobi/v1/gamer/event/private?timeout=10000");
//			req2.Headers["Authorization"] = "Basic NTU2YmZmNDk2YzM0MGU4YjNmZGEyN2QyOmJlMDM3OWU2YTlmZGMxNmEwYTE4NmY5ZTYxN2FjYTM4Y2YxOWU4Njg=";
//			req2.Headers["Authorization"] = "Basic NTU2YmZiZTcyOWFkMTQ0YTFiNzkwZjgzOmFmYjA4NjM0MDBkMzIyNmU1N2QzMDllNmYxNWQzNTBkZTY5ZTRkYzY=";
			req2.GetResponse();
			Debug.LogWarning("Got response for req 2");
		})).Start();

		new Thread(new ThreadStart(() => {
			Debug.LogWarning("Req 3 on " + Thread.CurrentThread.ManagedThreadId);
			HttpWebRequest req2 = TEMP_CreateReq("http://10.211.55.2:2000/v1/gamer/event/private?timeout=4000");
//			HttpWebRequest req2 = TEMP_CreateReq("https://sandbox-api01.clanofthecloud.mobi/v1/gamer/event/private?timeout=8000");
			//			req2.Headers["Authorization"] = "Basic NTU2YmZmNDk2YzM0MGU4YjNmZGEyN2QyOmJlMDM3OWU2YTlmZGMxNmEwYTE4NmY5ZTYxN2FjYTM4Y2YxOWU4Njg=";
//			req2.Headers["Authorization"] = "Basic NTU2YmZiZTcyOWFkMTQ0YTFiNzkwZjgzOmFmYjA4NjM0MDBkMzIyNmU1N2QzMDllNmYxNWQzNTBkZTY5ZTRkYzY=";
			req2.GetResponse();
			Debug.LogWarning("Got response for req 3");
		})).Start();

		new Thread(new ThreadStart(() => {
			Debug.LogWarning("Req 4 on " + Thread.CurrentThread.ManagedThreadId);
			HttpWebRequest req2 = TEMP_CreateReq("http://10.211.55.2:2000/v1/gamer/event/private?timeout=6000");
//			HttpWebRequest req2 = TEMP_CreateReq("https://sandbox-api01.clanofthecloud.mobi/v1/gamer/event/private?timeout=9000");
			//			req2.Headers["Authorization"] = "Basic NTU2YmZmNDk2YzM0MGU4YjNmZGEyN2QyOmJlMDM3OWU2YTlmZGMxNmEwYTE4NmY5ZTYxN2FjYTM4Y2YxOWU4Njg=";
//			req2.Headers["Authorization"] = "Basic NTU2YmZiZTcyOWFkMTQ0YTFiNzkwZjgzOmFmYjA4NjM0MDBkMzIyNmU1N2QzMDllNmYxNWQzNTBkZTY5ZTRkYzY=";
			req2.GetResponse();
			Debug.LogWarning("Got response for req 4");
		})).Start();

		new Thread(new ThreadStart(() => {
			Debug.LogWarning("Req 5 on " + Thread.CurrentThread.ManagedThreadId);
			HttpWebRequest req2 = TEMP_CreateReq("http://10.211.55.2:2000/v1/gamer/event/private?timeout=5000");
//			HttpWebRequest req2 = TEMP_CreateReq("https://sandbox-api01.clanofthecloud.mobi/v1/gamer/event/private?timeout=12000");
			// req2.Headers["Authorization"] = "Basic NTU2YmZmNDk2YzM0MGU4YjNmZGEyN2QyOmJlMDM3OWU2YTlmZGMxNmEwYTE4NmY5ZTYxN2FjYTM4Y2YxOWU4Njg=";
			// req2.Headers["Authorization"] = "Basic NTU2YmZiZTcyOWFkMTQ0YTFiNzkwZjgzOmFmYjA4NjM0MDBkMzIyNmU1N2QzMDllNmYxNWQzNTBkZTY5ZTRkYzY=";
			req2.GetResponse();
			Debug.LogWarning("Got response for req 5");
		})).Start();

		new Thread(new ThreadStart(() => {
			try {
				Debug.LogWarning("Req A on " + Thread.CurrentThread.ManagedThreadId);
				HttpWebRequest req3 = TEMP_CreateReq("http://10.211.55.2:2000/v1/vfs/private/key1");
				req3.GetResponse();
				Debug.LogWarning("Got response for key1");
			} 
			catch (Exception e) {
				Debug.LogWarning("EX: " + e.ToString());
			}

			try {
				Debug.LogWarning("Req B on " + Thread.CurrentThread.ManagedThreadId);
				HttpWebRequest req4 = TEMP_CreateReq("http://10.211.55.2:2000/v1/vfs/private/key2");
				req4.GetResponse();
				Debug.LogWarning("Got response for key2");
			} catch (Exception e) {
				Debug.LogWarning("EX: " + e.ToString());
			}
		})).Start();
	}

#if false
	[Test("Tests another HTTP client")]
	public void TEMP_TestOtherHttpClient(Clan clan) {
		string serv = "http://10.211.55.2:2000";
//		string serv = ""https://sandbox-api01.clanofthecloud.mobi";
		HTTP.Request req = TEMP_CreateReq2(serv + "/v1/gamer/event/private?timeout=5000");
		req.whenFinished = response => {
			Debug.LogWarning("Response from req1 " + response.message);
		};
		req.Send();

		req = TEMP_CreateReq2(serv + "/v1/gamer/event/private?timeout=8000");
		req.whenFinished = response => {
			Debug.LogWarning("Response from req2 " + response.message);
		};
		req.Send();

		req = TEMP_CreateReq2(serv + "/v1/vfs/private/key1");
		req.whenFinished = response1 => {
			Debug.LogWarning("Response for key1");

			req = TEMP_CreateReq2(serv + "/v1/vfs/private/key2");
			req.whenFinished = response2 => {
				Debug.LogWarning("Response for key2");
			};
			req.Send();
		};
		req.Send();
	}
#endif

	private HttpWebRequest TEMP_CreateReq(string url) {
		HttpWebRequest req1 = HttpWebRequest.Create(url) as HttpWebRequest;
		req1.UserAgent = "cloudbuilder-unity-WindowsEditor-2.11";
		req1.Headers["x-apikey"] = "testgame-key";
		req1.Headers["x-sdkversion"] = "1";
		req1.Headers["x-apisecret"] = "testgame-secret";
		req1.KeepAlive = false;
		req1.Headers["Authorization"] = "Basic NTU2YmZhYTUxNDYxODg2NDNkNTZhNzRjOjkxZWU2YzA2NTVhZjMwZWMzZjViZjI3ZDAxY2Y4MmQ3ODYxODY0OGE=";
		req1.ServicePoint.ConnectionLimit = 10;
//		req1.Headers["Authorization"] = "Basic NTU2YzA0NmIyOWFkMTQ0YTFiNzkwZjg3OmEwMmRiZTU1MTUzMDYwN2VmZTBhNDg3Njg2OTAxYjE5M2Q3ZTJiZjM=";
		return req1;
	}

#if false
	private HTTP.Request TEMP_CreateReq2(string url) {
		HTTP.Request req1 = new HTTP.Request("GET", url);
		req1.SetHeader("User-Agent", "cloudbuilder-unity-WindowsEditor-2.11");
		req1.SetHeader("x-apikey", "testgame-key");
		req1.SetHeader("x-sdkversion", "1");
		req1.SetHeader("x-apisecret", "testgame-secret");
		req1.SetHeader("Authorization", "Basic NTU2YmNiMjY4YjhmZGNlZDFjNGFlNWFmOjRmMWJhYWRhYjAxNmJhZGRjYWJlZDM5MWY5NzQ4ZDI3MmM2YjJhN2M=");
		return req1;
	}
#endif
	#endregion

	#region Private
	private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
