using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class ClanTests : MonoBehaviour {
	[InstanceMethod(typeof(ClanTests))]
	public string TestMethodName;

	void Start() {
		GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
	}

	[Test("Tests a simple setup.")]
	public void ShouldSetupProperly() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			IntegrationTest.Pass();
		});
	}

	[Test("Sets up and does a ping")]
	public void ShouldPing() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			clan.Ping(result => {
				if (result.IsSuccessful)
					IntegrationTest.Pass();
				else
					IntegrationTest.Fail("Ping failed, check that the environment is running");
			});
		});
	}

	[Test("Logs in anonymously.")]
	public void ShouldLoginAnonymously() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			clan.LoginAnonymously(result => {
				if (result.IsSuccessful && result.Value != null)
					IntegrationTest.Pass();
				else
					IntegrationTest.Fail("Didn't get the gamer successfully");
			});
		});
	}

	[Test("First logs in anonymously, then tries to restore the session with the received credentials.")]
	public void ShouldRestoreSession() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			clan.Login(
				network: LoginNetwork.Email,
				networkId: "clan@localhost.localdomain",
				networkSecret: "Password123",
				done: result => {
					if (!result.IsSuccessful)
						IntegrationTest.Fail("Failed to login");

					// Resume the session with the credentials just received
					clan.ResumeSession(
						gamerId: result.Value.GamerId,
						gamerSecret: result.Value.GamerSecret,
						done: resumeResult => {
							if (resumeResult.IsSuccessful && resumeResult.Value != null)
								IntegrationTest.Pass();
							else
								IntegrationTest.Fail("Didn't get the gamer successfully");
						}
					);
				}
			);
		});
	}

	[Test("Tests that a non-existing session fails to resume (account not created).")]
	public void ShouldNotRestoreInexistingSession() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			// Resume the session with the credentials just received
			clan.ResumeSession(
				gamerId: "15555f06c7b852423cb9074a",
				gamerSecret: "1f89a1efa49a3cf59d00f8badb03227d1b56840b",
				done: resumeResult => {
					IntegrationTest.Assert(!resumeResult.IsSuccessful);
					IntegrationTest.Assert(resumeResult.HttpStatusCode == 401);
					IntegrationTest.Assert(resumeResult.ServerData["name"] == "LoginError");
					IntegrationTest.Pass();
				}
			);
		});
	}

	[Test("Tests that the prevent registration flag is taken in account correctly.")]
	public void ShouldPreventRegistration() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			// Resume the session with the credentials just received
			clan.Login(
				network: LoginNetwork.Email,
				networkId: "nonexisting@localhost.localdomain",
				networkSecret: "Password123",
				preventRegistration: true,
				done: resumeResult => {
					IntegrationTest.Assert(!resumeResult.IsSuccessful);
					IntegrationTest.Assert(resumeResult.HttpStatusCode == 400);
					IntegrationTest.Assert(resumeResult.ServerData["name"] == "PreventRegistration");
					IntegrationTest.Pass();
				}
			);
		});
	}
}
