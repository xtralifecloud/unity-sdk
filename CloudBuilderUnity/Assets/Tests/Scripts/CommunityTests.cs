using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class CommunityTests : TestBase {

	[InstanceMethod(typeof(CommunityTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Cloud param (and we do the setup here)
		FindObjectOfType<CotcGameObject>().GetCloud(cloud => {
			met.Invoke(this, new object[] { cloud });
		});
	}

	[Test("Uses two anonymous accounts. Tests that a friend can be added properly and then listed back (AddFriend + ListFriends).")]
	public void ShouldAddFriend(Cloud cloud) {
		// Use two test accounts
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			// Add gamer1 as a friend of gamer2
			gamer2.Community.AddFriend(
				gamerId: gamer1.GamerId,
				done: addResult => {
					Assert(addResult.IsSuccessful, "Failed to add friend");

					// Then list the friends of gamer1, gamer2 should be in it
					gamer1.Community.ListFriends(gamerInfo => {
						Assert(gamerInfo.IsSuccessful, "Failed to list friends");
						Assert(gamerInfo.Value.Count == 1, "Expects one friend");
						Assert(gamerInfo.Value[0].GamerId == gamer2.GamerId, "Wrong friend ID");
						CompleteTest();
					});
				}
			);
		});
	}

	[Test("Creates 2 users, and sends a message from one to the other and verifies that all happens as expected.")]
	public void ShouldSendEvent(Cloud cloud) {
		Login2NewUsers(cloud, (gamer1, gamer2) => {
			// Wait event for P1
			AsyncOp finishedSendEvent = new AsyncOp();
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
				eventData: Bundle.CreateObject("hello", "world"),
				done: result => {
					Assert(result.IsSuccessful, "Failed to send event");
					Assert(result.Value, "Expected true result");
					Assert(result.ServerData["hello"] == "world", "Returned message invalid");
					finishedSendEvent.Return();
				}
			);
		});
	}

	[Test("Creates two users and tries to list them in a paginated fashion.")]
	public void ShouldListUsers(Cloud cloud) {
		Gamer[] gamers = new Gamer[2];
		new AsyncOp().Then(next => {
			// Create first user
			cloud.Login(LoginNetwork.Email, "user2@localhost.localdomain", "123")
			.Then(result1 => {
				Assert(result1.IsSuccessful, "Failed to login #1");
				gamers[0] = result1.Value;
				// Second user
				cloud.Login(LoginNetwork.Email, "user1@localhost.localdomain", "123")
				.Then(result2 => {
					Assert(result2.IsSuccessful, "Failed to login #2");
					gamers[1] = result2.Value;
					next.Return();
				});
			});
		})
		.Then(next => {
			// Query for a specific user by e-mail
			cloud.ListUsers("user2@localhost.localdomain")
			.Then(result => {
				Assert(result.IsSuccessful, "Failed to list users with filter");
				Assert(result.Value.Count == 1, "Expected one result only");
				Assert(result.Value[0].UserId == gamers[1].GamerId, "Expected to return user 2");
				Assert(result.Value[0].Network == LoginNetwork.Email, "Network is e-mail");
				Assert(result.Value[0].NetworkId == "user2@localhost.localdomain", "Invalid network ID");
				Assert(result.Value[0]["profile"]["displayName"] == "user2", "Invalid profile display name");
				next.Return();
			});
		})
		.Then(next => {
			// Query for all users in a paginated way
			cloud.ListUsers("@", 1)
			.Then(result => {
				Assert(result.IsSuccessful, "Failed to list users with filter");
				Assert(result.Value.Count == 1, "Expected one result per page");
				Assert(result.Value.Total >= 2, "Expected at least two results total");
				if (result.Value.Offset == 0) {
					Assert(result.Value.HasNext, "Should have next page");
					Assert(!result.Value.HasPrevious, "Should not have previous page");
					result.Value.FetchNext();
				}
				else {
					Assert(result.Value.HasPrevious, "Should have previous page");
					next.Return();
				}
			});
		})
		.Then(() => CompleteTest()).Return();
	}
}
