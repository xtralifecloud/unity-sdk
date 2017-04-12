using UnityEngine;
using CotcSdk;
using IntegrationTests;
using System.Collections.Generic;

public class CommunityTests : TestBase {

	[InstanceMethod(typeof(CommunityTests))]
	public string TestMethodName;

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("Uses two anonymous accounts. Tests that a friend can be added properly and then listed back (AddFriend + ListFriends).")]
	public void ShouldAddFriend(Cloud cloud) {
		// Use two test accounts
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			// Expects friend status change event
			Promise restOfTheTestCompleted = new Promise();
            DomainEventLoop loopP1 = gamer1.StartEventLoop();
			gamer1.Community.OnFriendStatusChange += (FriendStatusChangeEvent e) => {
				Assert(e.FriendId == gamer2.GamerId, "Should come from P2");
				Assert(e.NewStatus == FriendRelationshipStatus.Add, "Should have added me");
                loopP1.Stop();
				restOfTheTestCompleted.Done(CompleteTest);
			};

			// Add gamer1 as a friend of gamer2
			gamer2.Community.AddFriend(gamer1.GamerId)
			.ExpectSuccess(addResult => {
                // Then list the friends of gamer1, gamer2 should be in it
                return gamer1.Community.ListFriends();
            })
			.ExpectSuccess(friends => {
				Assert(friends.Count == 1, "Expects one friend");
				Assert(friends[0].GamerId == gamer2.GamerId, "Wrong friend ID");
				restOfTheTestCompleted.Resolve();
			});
		});
	}

    [Test("Uses two anonymous accounts. Tests that a friend can be forget, then blacklisted (AddFriend + ChangeRelationshipStauts:Forget/Blacklist).")]
    public void ShouldChangeRelationshipStatus(Cloud cloud) {
        // Use two test accounts
        Login2NewUsers(cloud, (gamer1, gamer2) => {
            // Make gamers friends
            gamer1.Community.AddFriend(gamer2.GamerId)
            .ExpectSuccess(done => {
                Assert(done, "Should have added friend");
                return gamer1.Community.ListFriends();
            })
            .ExpectSuccess(friends => {
                Assert(friends.Count == 1, "Expects one friend");
                Assert(friends[0].GamerId == gamer2.GamerId, "Wrong friend ID");
                return gamer2.Community.ChangeRelationshipStatus(gamer1.GamerId, FriendRelationshipStatus.Forget);
            })
            .ExpectSuccess(done => {
                Assert(done, "Should have changed relationship to forgot");
                return gamer1.Community.ListFriends();
            })
            .ExpectSuccess(emptyList => {
                Assert(emptyList.Count == 0, "Expects no friend");
                return gamer2.Community.ChangeRelationshipStatus(gamer1.GamerId, FriendRelationshipStatus.Blacklist);
            })
            .ExpectSuccess(done => {
                Assert(done, "Should have changed relationship to forgot");
                return gamer1.Community.ListFriends(true);
            })
            .ExpectSuccess(blacklist => {
                Assert(blacklist.Count == 1, "Expect one blacklisted");
                Assert(blacklist[0].GamerId == gamer2.GamerId, "Wrong friend ID");
                CompleteTest();
            });
        });
    }

    [Test("Uses two anonymous accounts. Tests the function DiscardEventHandlers.", "Broken actually because gamer.Community returns a new instance each time it is called.")]
    public void ShouldDiscardEventHandlers(Cloud cloud) {
        Login2NewUsers(cloud, (gamer1, gamer2) => {
            DomainEventLoop loopP1 = gamer1.StartEventLoop();
            gamer1.Community.OnFriendStatusChange += (FriendStatusChangeEvent e) => {
                // This one is added before the discard, so it must not be executed
                FailTest("Should not receive a OnFriendStatusChange event after DiscardEventHandlers");
			};

            gamer1.Community.DiscardEventHandlers();

            gamer1.Community.OnFriendStatusChange += (FriendStatusChangeEvent e) => {
                // This one is added after the discard, so it must be executed
                CompleteTest();
            };

            // Add gamer1 as a friend of gamer2
            gamer2.Community.AddFriend(gamer1.GamerId)
            .Catch(ex => {
                Debug.LogError(ex);
            });
        });
    }

    [Test("Creates 2 users, and sends a message from one to the other and verifies that all happens as expected.")]
	public void ShouldSendEvent(Cloud cloud) {
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			// Wait event for P1
			Promise finishedSendEvent = new Promise();
			DomainEventLoop loop = gamer1.StartEventLoop();
			loop.ReceivedEvent += (sender, e) => {
				Assert(sender == loop, "Event should come from the loop");
				Assert(e.Message["event"]["hello"] == "world", "Message invalid");
				loop.Stop();
				// Wait the results of SendEvent as well
				finishedSendEvent.Done(CompleteTest);
			};
			// Send event as P2
			gamer2.Community.SendEvent(
				gamerId: gamer1.GamerId,
				eventData: Bundle.CreateObject("hello", "world"))
			.ExpectSuccess(result => {
				Assert(result, "Expected true result");
				finishedSendEvent.Resolve();
			});
		});
	}

    [Test("Creates 2 users, each user starts listening for event, sends an event to the other user, and verifies its reception.")]
    public void ShouldNotMixEvents(Cloud cloud) {
        Login2NewUsers(cloud, (gamer1, gamer2) => {
            DomainEventLoop loopP1 = gamer1.StartEventLoop();
            DomainEventLoop loopP2 = gamer2.StartEventLoop(); 

            bool p1received = false, p2received = false;

            loopP1.ReceivedEvent += (sender, e) => {
                Assert(!p1received, "Event already received by the Player 1's loop");
                Assert(sender == loopP1, "Event should come from the correct loop");
                Assert(e.Message["from"].AsString() != gamer1.GamerId, "Player 1 received an event coming from himself instead of the other player");
                loopP1.Stop();
                // Wait the other player's event before completing the test.
                p1received = true;
                if (p2received == true)
                    CompleteTest();
            };

            loopP2.ReceivedEvent += (sender, e) => {
                Assert(!p2received, "Event already received by the Player 2's loop");
                Assert(sender == loopP2, "Event should come from the correct loop");
                Assert(e.Message["from"].AsString() != gamer2.GamerId, "Player 2 received an event coming from himself instead of the other player");
                loopP2.Stop();
                // Wait the other player's event before completing the test.
                p2received = true;
                if (p1received == true)
                    CompleteTest();
            };

            gamer1.Community.SendEvent(gamer2.GamerId, new Bundle("hi"));
            gamer2.Community.SendEvent(gamer1.GamerId, new Bundle("hi"));
        });
    }

    [Test("Creates two users and tries to list them in a paginated fashion.")]
	public void ShouldListUsers(Cloud cloud) {
		Gamer[] gamers = new Gamer[2];
		// Create first user
		cloud.Login(LoginNetwork.Email.Describe(), "user1@localhost.localdomain", "123")
		.ExpectSuccess(result1 => {
			gamers[0] = result1;

			// Second user
			return cloud.Login(LoginNetwork.Email.Describe(), "user2@localhost.localdomain", "123");
		})
		.ExpectSuccess(result2 => {
			gamers[1] = result2;

			// Query for a specific user by e-mail
			return cloud.ListUsers("user2@localhost.localdomain");
		})
		.ExpectSuccess(result => {
			Assert(result.Count == 1, "Expected one result only");
			Assert(result[0].UserId == gamers[1].GamerId, "Expected to return user 2");
			Assert(result[0].Network == LoginNetwork.Email, "Network is e-mail");
			Assert(result[0].NetworkId == "user2@localhost.localdomain", "Invalid network ID");
			Assert(result[0]["profile"]["displayName"] == "user2", "Invalid profile display name");

			// Query for all users in a paginated way
			return cloud.ListUsers("@", 1);
		})
		.ExpectSuccess(result => {
			Assert(result.Count == 1, "Expected one result per page");
			Assert(result.Total >= 2, "Expected at least two results total");
			Assert(result.HasNext, "Should have next page");
			Assert(!result.HasPrevious, "Should not have previous page");

			// Next page
			return result.FetchNext();
		})
		.ExpectSuccess(nextPage => {
			Assert(nextPage.HasPrevious, "Should have previous page");
			CompleteTest();
		});
	}

	[Test("Tests the list network users call. Uses real data obtained from real test accounts on Facebook", "The user token may have expired (Last creation 11/04/2017), get a new user token via facebook for developpers website.")]
	public void ShouldListNetworkUsers(Cloud cloud) {
        // User ID in the CotC network.
        string gamerID = "58ece8890810e5fe491a20a0";

        // User Token obtained from Facebook.
        string user_token = "EAAENyTNQMpQBAHD1SxerW3XJ0e3iK1KqE2znNHwr7wyvTQDDKzjsJ6NqOxnj3HZA0LjbgFjJUb2SZBsvZClWjGZCObAsZCXZCkpvOHY7bQZAcV5v6KwoXLEvTf3t9N0lsLbqgP2ywCPN3hdkYbVRDHw7VPuJgbq4svRh4RZATphNhpsHjtZA78R2oFl8ZBdex0jnRrtZAsdZBzvHQQZDZD";

        cloud.Login("facebook", gamerID, user_token)
            .Then(gamer => {
                // Data obtained by fetching friends from Facebook. Using real test accounts.
                Bundle data = Bundle.FromJson(@"{""data"":[{""name"":""Fr\u00e9d\u00e9ric Benois"",""id"":""107926476427271""}],""paging"":{""cursors"":{""before"":""QVFIUlY5TGkwWllQSU1tZAmN2NVlRaWlyeVpZAWk1idktkaU5GcFotRkp0RWlCVnNyR3MweUR5R3ZAfQ193ZAUhYWk84US0zVHdxdzdxMWswVTk2YUxlbVVlQXd3"",""after"":""QVFIUlY5TGkwWllQSU1tZAmN2NVlRaWlyeVpZAWk1idktkaU5GcFotRkp0RWlCVnNyR3MweUR5R3ZAfQ193ZAUhYWk84US0zVHdxdzdxMWswVTk2YUxlbVVlQXd3""}},""summary"":{""total_count"":1}}");
                
                List<SocialNetworkFriend> friends = new List<SocialNetworkFriend>();
                foreach (Bundle f in data["data"].AsArray()) {
                    friends.Add(new SocialNetworkFriend(f));
                }
                gamer.Community.ListNetworkFriends(LoginNetwork.Facebook, friends, true)
                .ExpectSuccess(response => {
                    Debug.LogWarning(response);
                    Assert(response.ByNetwork[LoginNetwork.Facebook].Count == 1, "Should have registered 1 facebook users");
                    CompleteTest();
                });
            });

        /*Login(cloud, gamer => {
            //Bundle data = Bundle.FromJson("{\"data\":[{\"name\":\"Test 1\",\"id\":\"10153057921478192\"},{\"name\":\"Test 2\",\"id\":\"10153366656847346\"},{\"name\":\"Test 3\",\"id\":\"339262546278220\"}],\"paging\":{\"next\":\"https://graph.facebook.com/v2.1/10152381145462633/friends?access_token=CAAENyTNQMpQBADfCgZCpmiZAcRifeQVmfoeVNScZCi5DQxlianZABohlOFivboYIuOb1Qqv4ATAMswCNpJWtmWUrkZAdsDUUxtQMrm0qo3QOjztl2niJ0vmmKrKccXhZAwFK5GkNe4Q58ZBPouzS5IFVsFUzDjhiAVjlzmlCgdb2Fcf9n6651wsWrEMZCKxk98QDcs0OJYEeDQZDZD&limit=5000&offset=5000&__after_id=enc_AdCP2GaZAGG8lfGebTkZAwTP6l7CbRHm15XU9mT6RRx9xa5C7PBZB35xaZAVf1IoFQTd9ZAILfmqphj3KZBrrQ3vaIuYRO\"},\"summary\":{\"total_count\":760}}");
            Bundle data = Bundle.FromJson(@"{""data"":[{""name"":""Fr\u00e9d\u00e9ric Alexandre"",""id"":""102410670314912""}],""paging"":{""cursors"":{""before"":""QVFIUjJnLUtsdWpGZAHFsNXRuOTgxaXJqUFVjb0dKR3Nka0JGdWxkZAkg5U1hMaFg1ZA19seG91WGdNSjRmY3NCNFdTVG1feGdGNG1lXzFGSmRTTTA1ODM1ME53"",""after"":""QVFIUjJnLUtsdWpGZAHFsNXRuOTgxaXJqUFVjb0dKR3Nka0JGdWxkZAkg5U1hMaFg1ZA19seG91WGdNSjRmY3NCNFdTVG1feGdGNG1lXzFGSmRTTTA1ODM1ME53""}},""summary"":{""total_count"":1}}");
            Debug.LogWarning(data);
            List<SocialNetworkFriend> friends = new List<SocialNetworkFriend>();
			foreach (Bundle f in data["data"].AsArray()) {
				friends.Add(new SocialNetworkFriend(f));
			}
            gamer.Community.ListNetworkFriends(LoginNetwork.Facebook, friends, true)
            .ExpectSuccess(response => {
                Debug.LogWarning(response);
                Assert(response.ByNetwork[LoginNetwork.Facebook].Count == 3, "Should have registered 3 facebook users");
                CompleteTest();
            });
        });*/
	}
}
