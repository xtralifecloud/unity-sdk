using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class CommunityTests : TestBase {

	[InstanceMethod(typeof(CommunityTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("Uses two anonymous accounts. Tests that a friend can be added properly and then listed back (AddFriend + ListFriends).")]
	public void ShouldAddFriend(Clan clan) {
		// Use two test accounts
		Login2NewUsers(clan, (gamer1, gamer2) => {
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
}
