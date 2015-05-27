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
			gamer.Matches.Create(result => {
				Assert(result.IsSuccessful, "Creating match failed");
				CompleteTest();
			}, 2);
		});
	}

	[Test("Creates a match, and verifies that the match object seems to be configured appropriately.")]
	public void ShouldCreateMatch(Clan clan) {
		string matchDesc = "Test match";
		Login(clan, gamer => {
			gamer.Matches.Create(result => {
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
			}, 2, matchDesc, Bundle.CreateObject("test", "value"), Bundle.CreateArray(1, 2, 3));
		});
	}

	#region Private
	private string RandomBoardName() {
		return "board-" + Guid.NewGuid().ToString();
	}
	#endregion
}
