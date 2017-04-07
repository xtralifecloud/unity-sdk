using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
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
			gamer1.StartEventLoop();
			gamer1.Community.OnFriendStatusChange += (FriendStatusChangeEvent e) => {
				Assert(e.FriendId == gamer2.GamerId, "Should come from P2");
				Assert(e.NewStatus == FriendRelationshipStatus.Add, "Should have added me");
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

	[Test("Tests the list network users call. Does little about this one since we would need real data…")]
	public void ShouldListNetworkUsers(Cloud cloud) {
		Login(cloud, gamer => {
			Bundle data = Bundle.FromJson("{\"data\":[{\"name\":\"Test 1\",\"id\":\"10153057921478192\"},{\"name\":\"Test 2\",\"id\":\"10153366656847346\"},{\"name\":\"Test 3\",\"id\":\"339262546278220\"}],\"paging\":{\"next\":\"https://graph.facebook.com/v2.1/10152381145462633/friends?access_token=CAAENyTNQMpQBADfCgZCpmiZAcRifeQVmfoeVNScZCi5DQxlianZABohlOFivboYIuOb1Qqv4ATAMswCNpJWtmWUrkZAdsDUUxtQMrm0qo3QOjztl2niJ0vmmKrKccXhZAwFK5GkNe4Q58ZBPouzS5IFVsFUzDjhiAVjlzmlCgdb2Fcf9n6651wsWrEMZCKxk98QDcs0OJYEeDQZDZD&limit=5000&offset=5000&__after_id=enc_AdCP2GaZAGG8lfGebTkZAwTP6l7CbRHm15XU9mT6RRx9xa5C7PBZB35xaZAVf1IoFQTd9ZAILfmqphj3KZBrrQ3vaIuYRO\"},\"summary\":{\"total_count\":760}}");
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
        });
	}
}
