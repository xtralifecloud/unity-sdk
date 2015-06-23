using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

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
			// Add gamer1 as a friend of gamer2
			gamer2.Community.AddFriend(gamer1.GamerId)
			.ExpectSuccess(addResult => {
				// Then list the friends of gamer1, gamer2 should be in it
				gamer1.Community.ListFriends()
				.ExpectSuccess(friends => {
					Assert(friends.Count == 1, "Expects one friend");
					Assert(friends[0].GamerId == gamer2.GamerId, "Wrong friend ID");
					CompleteTest();
				});
			});
		});
	}

	[Test("Creates 2 users, and sends a message from one to the other and verifies that all happens as expected.")]
	public void ShouldSendEvent(Cloud cloud) {
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			// Wait event for P1
			Promise finishedSendEvent = new Promise();
			DomainEventLoop loop = new DomainEventLoop(gamer1).Start();
			loop.ReceivedEvent += (sender, e) => {
				Assert(sender == loop, "Event should come from the loop");
				Assert(e.Message["hello"] == "world", "Message invalid");
				loop.Stop();
				// Wait the results of SendEvent as well
				finishedSendEvent.Then(() => CompleteTest());
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
		new AsyncOp().Then(next => {
			// Create first user
			cloud.Login(LoginNetwork.Email, "user1@localhost.localdomain", "123")
			.ExpectSuccess(result1 => {
				gamers[0] = result1;
				// Second user
				cloud.Login(LoginNetwork.Email, "user2@localhost.localdomain", "123")
				.ExpectSuccess(result2 => {
					gamers[1] = result2;
					next.Return();
				});
			});
		})
		.Then(next => {
			// Query for a specific user by e-mail
			cloud.ListUsers("user2@localhost.localdomain")
			.ExpectSuccess(result => {
				Assert(result.Count == 1, "Expected one result only");
				Assert(result[0].UserId == gamers[1].GamerId, "Expected to return user 2");
				Assert(result[0].Network == LoginNetwork.Email, "Network is e-mail");
				Assert(result[0].NetworkId == "user2@localhost.localdomain", "Invalid network ID");
				Assert(result[0]["profile"]["displayName"] == "user2", "Invalid profile display name");
				next.Return();
			});
		})
		.Then(next => {
			// Query for all users in a paginated way
			cloud.ListUsers("@", 1)
			.ExpectSuccess(result => {
				Assert(result.Count == 1, "Expected one result per page");
				Assert(result.Total >= 2, "Expected at least two results total");
				Assert(result.HasNext, "Should have next page");
				Assert(!result.HasPrevious, "Should not have previous page");
				result.FetchNext()
				.ExpectSuccess(nextPage => {
					Assert(nextPage.HasPrevious, "Should have previous page");
					next.Return();
				});
			});
		})
		.Then(() => CompleteTest()).Return();
	}
}
