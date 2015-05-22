using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

public class ClanTests : TestBase {
	[InstanceMethod(typeof(ClanTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		var parms = met.GetParameters();
		// Test methods can either have no param, either have one "Clan" param, in which case we do the setup here to simplify
		if (parms.Length >= 1 && parms[0].ParameterType == typeof(Clan)) {
			FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
				met.Invoke(this, new object[] { clan });
			});
		}
		else {
			met.Invoke(this, null);
		}
	}

	[Test("Tests a simple setup.")]
	public void ShouldSetupProperly() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			IntegrationTest.Assert(clan != null);
		});
	}

	[Test("Sets up and does a ping")]
	public void ShouldPing(Clan clan) {
		clan.Ping(result => {
			if (result.IsSuccessful)
				IntegrationTest.Pass();
			else
				IntegrationTest.Fail("Ping failed, check that the environment is running");
		});
	}

	[Test("Logs in anonymously.")]
	public void ShouldLoginAnonymously(Clan clan) {
		clan.LoginAnonymously(result => {
			IntegrationTest.Assert(result.IsSuccessful && result.Value != null);
		});
	}

	[Test("First logs in anonymously, then tries to restore the session with the received credentials.")]
	public void ShouldRestoreSession(Clan clan) {
		clan.Login(
			network: LoginNetwork.Email,
			networkId: "clan@localhost.localdomain",
			networkSecret: "Password123",
			done: result => {
				Assert(result.IsSuccessful, "Failed to login");

				// Resume the session with the credentials just received
				clan.ResumeSession(
					gamerId: result.Value.GamerId,
					gamerSecret: result.Value.GamerSecret,
					done: resumeResult => {
						Assert(resumeResult.IsSuccessful && resumeResult.Value != null);
						CompleteTest();
					}
				);
			}
		);
	}

	[Test("Tests that a non-existing session fails to resume (account not created).")]
	public void ShouldNotRestoreInexistingSession(Clan clan) {
		// Resume the session with the credentials just received
		clan.ResumeSession(
			gamerId: "15555f06c7b852423cb9074a",
			gamerSecret: "1f89a1efa49a3cf59d00f8badb03227d1b56840b",
			done: resumeResult => {
				Assert(!resumeResult.IsSuccessful);
				Assert(resumeResult.HttpStatusCode == 401);
				Assert(resumeResult.ServerData["name"] == "LoginError");
				CompleteTest();
			}
		);
	}

	[Test("Tests that the prevent registration flag is taken in account correctly.")]
	public void ShouldPreventRegistration(Clan clan) {
		// Resume the session with the credentials just received
		clan.Login(
			network: LoginNetwork.Email,
			networkId: "nonexisting@localhost.localdomain",
			networkSecret: "Password123",
			preventRegistration: true,
			done: resumeResult => {
				Assert(!resumeResult.IsSuccessful);
				Assert(resumeResult.HttpStatusCode == 400);
				Assert(resumeResult.ServerData["name"] == "PreventRegistration");
				CompleteTest();
			}
		);
	}

	[Test("Tests that an anonymous account can be converted to an e-mail account.")]
	public void ShouldConvertAccount(Clan clan) {
		// Create an anonymous account
		clan.LoginAnonymously(loginResult => {
			Assert(loginResult.IsSuccessful, "Anonymous login failed");

			// Then convert it to e-mail
			var gamer = loginResult.Value;
			gamer.ConvertAccount(
				network: LoginNetwork.Email,
				networkId: RandomEmailAddress(),
				networkSecret: "Password123",
				done: conversionResult => {
					Assert(conversionResult.IsSuccessful && conversionResult.Value);
					CompleteTest();
				}
			);
		});
	}

	[Test("Ensures that an account cannot be converted to a credential that already exists.")]
	public void ShouldFailToConvertToExistingAccount(Clan clan) {
		// Ensures that a fake account has been created
		clan.Login(
			network: LoginNetwork.Email,
			networkId: "clan@localhost.localdomain",
			networkSecret: "Password123",
			done: result => {
				Assert(result.IsSuccessful, "Creation of fake account failed");

				// Create an anonymous account
				clan.LoginAnonymously(loginResult => {
					Assert(result.IsSuccessful, "Anonymous login failed");
						
					// Then try to convert it to the same e-mail as the fake account created at first
					var gamer = loginResult.Value;
					gamer.ConvertAccount(
						network: LoginNetwork.Email,
						networkId: "clan@localhost.localdomain",
						networkSecret: "Anotherp4ss",
						done: conversionResult => {
							Assert(!conversionResult.IsSuccessful && !conversionResult.Value);
							Assert(conversionResult.HttpStatusCode == 400);
							Assert(conversionResult.ServerData["message"] == "UserExists");
							CompleteTest();
						}
					);
				});
			}
		);
	}

	[Test("Checks the 'find user' functionality.")]
	public void ShouldCheckIfUserExists(Clan clan) {
		// Ensures that a fake account has been created
		clan.Login(
			network: LoginNetwork.Email,
			networkId: "clan@localhost.localdomain",
			networkSecret: "Password123",
			done: loginResult => {
				Assert(loginResult.IsSuccessful, "Creation of fake account failed");
				clan.UserExists(
					network: LoginNetwork.Email,
					networkId: "clan@localhost.localdomain",
					done: checkResult => {
						Assert(checkResult.IsSuccessful && checkResult.Value);
						CompleteTest();
					}
				);
			}
		);
	}

	[Test("Checks the send reset link functionality.")]
	public void ShouldSendAccountResetLink(Clan clan) {
		// This method is broken because we cannot GET somewhere with a body
		// We have to fix the server or get rid of this method & test
		clan.SendResetPasswordEmail(
			userEmail: "clan@localhost.localdomain",
			mailSender: "admin@localhost.localdomain",
			mailTitle: "Reset link",
			mailBody: "Here is your link: [[SHORTCODE]]",
			done: result => {
				IntegrationTest.Assert(result.IsSuccessful && result.Value);
			}
		);
	}

	[Test("Changes the password of an e-mail account.")]
	public void ShouldChangePassword(Clan clan) {
		clan.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123",
			done: result => {
				Assert(result.IsSuccessful, "Creation of fake account failed");
				var gamer = result.Value;
				gamer.ChangePassword(pswResult => {
					Assert(pswResult.IsSuccessful && pswResult.Value);
					CompleteTest();
				}, "Password124");
			}
		);
	}

	[Test("Changes the e-mail address associated to an e-mail account.")]
	public void ShouldChangeEmailAddress(Clan clan) {
		clan.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123",
			done: result => {
				Assert(result.IsSuccessful, "Creation of fake account failed");
				var gamer = result.Value;
				gamer.ChangeEmailAddress(pswResult => {
					Assert(pswResult.IsSuccessful && pswResult.Value);
					CompleteTest();
				}, RandomEmailAddress());
			}
		);
	}

	[Test("Changes the e-mail address associated to an e-mail account.")]
	public void ShouldFailToChangeEmailAddressToExistingOne(Clan clan) {
		clan.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123",
			done: result => {
				Assert(result.IsSuccessful, "Creation of fake account failed");
				var gamer = result.Value;
				gamer.ChangeEmailAddress(pswResult => {
					Assert(!pswResult.IsSuccessful && !pswResult.Value);
					Assert(pswResult.HttpStatusCode == 400);
					Assert(pswResult.ServerData["message"] == "UserExists");
					CompleteTest();
				}, "clan@localhost.localdomain");
			}
		);
	}
}
