using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class ClanTests : TestBase {
	[InstanceMethod(typeof(ClanTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		var parms = met.GetParameters();
		// Test methods can either have no param, either have one "Cloud" param, in which case we do the setup here to simplify
		if (parms.Length >= 1 && parms[0].ParameterType == typeof(Cloud)) {
			FindObjectOfType<CotcGameObject>().GetCloud().Done(cloud => {
				met.Invoke(this, new object[] { cloud });
			});
		}
		else {
			met.Invoke(this, null);
		}
	}

	[Test("Tests a simple setup.")]
	public void ShouldSetupProperly() {
		var cb = FindObjectOfType<CotcGameObject>();
		cb.GetCloud().Done(cloud => {
			IntegrationTest.Assert(cloud != null);
		});
	}

	[Test("Sets up and does a ping")]
	public void ShouldPing(Cloud cloud) {
		cloud.Ping()
			.Then(result => IntegrationTest.Pass())
			.Catch(ex => IntegrationTest.Fail("Ping failed, check that the environment is running"));
	}

	[Test("Logs in anonymously.")]
	public void ShouldLoginAnonymously(Cloud cloud) {
		cloud.LoginAnonymously().Then(result => {
			IntegrationTest.Assert(result != null);
		});
	}

	[Test("First logs in anonymously, then tries to restore the session with the received credentials.")]
	public void ShouldRestoreSession(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain",
			networkSecret: "Password123")
		.Then(gamer => {
			// Resume the session with the credentials just received
			return cloud.ResumeSession(
				gamerId: gamer.GamerId,
				gamerSecret: gamer.GamerSecret);
		})
		.Then(resumeResult => {
			Assert(resumeResult != null, "Resume failed");
			CompleteTest();
		});
	}

	[Test("Tests that a non-existing session fails to resume (account not created).")]
	public void ShouldNotRestoreInexistingSession(Cloud cloud) {
		// Resume the session with the credentials just received
		cloud.ResumeSession(
			gamerId: "15555f06c7b852423cb9074a",
			gamerSecret: "1f89a1efa49a3cf59d00f8badb03227d1b56840b")
		.Catch(ex => {
			CotcException resumeResult = (CotcException)ex;
			Assert(resumeResult.HttpStatusCode == 401, "401 status code expected");
			Assert(resumeResult.ServerData["name"] == "LoginError", "LoginError expected");
			CompleteTest();
		})
		.Done(dummy => IntegrationTest.Fail("Resume should fail"));
	}

	[Test("Tests that the prevent registration flag is taken in account correctly.")]
	public void ShouldPreventRegistration(Cloud cloud) {
		// Resume the session with the credentials just received
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "nonexisting@localhost.localdomain",
			networkSecret: "Password123",
			preventRegistration: true)
		.Catch(ex => {
			CotcException resumeResult = (CotcException)ex;
			Assert(resumeResult.HttpStatusCode == 400, "400 expected");
			Assert(resumeResult.ServerData["name"] == "PreventRegistration", "PreventRegistration error expected");
			CompleteTest();
		})
		.Done(dummy => IntegrationTest.Fail("Resume should fail"));
	}

	[Test("Tests that an anonymous account can be converted to an e-mail account.")]
	public void ShouldConvertAccount(Cloud cloud) {
		// Create an anonymous account
		cloud.LoginAnonymously().Then(gamer => {
			// Then convert it to e-mail
			gamer.Account.Convert(
				network: LoginNetwork.Email,
				networkId: RandomEmailAddress(),
				networkSecret: "Password123")
			.Then(conversionResult => {
				Assert(conversionResult, "Convert account failed");
				CompleteTest();
			});
		});
	}

	[Test("Ensures that an account cannot be converted to a credential that already exists.")]
	public void ShouldFailToConvertToExistingAccount(Cloud cloud) {
		// Ensures that a fake account has been created
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain",
			networkSecret: "Password123")
		.Then(result => {
			// Create an anonymous account
			cloud.LoginAnonymously().Then(gamer => {
				// Then try to convert it to the same e-mail as the fake account created at first
				gamer.Account.Convert(
					network: LoginNetwork.Email,
					networkId: "cloud@localhost.localdomain",
					networkSecret: "Anotherp4ss")
				.Catch(ex => {
					CotcException conversionResult = (CotcException)ex;
					Assert(conversionResult.HttpStatusCode == 400, "400 expected");
					Assert(conversionResult.ServerData["message"] == "UserExists", "UserExists error expected");
					CompleteTest();
				})
				.Done(dummy => IntegrationTest.Fail("Conversion should fail"));
			});
		});
	}

	[Test("Checks the 'find user' functionality.")]
	public void ShouldCheckIfUserExists(Cloud cloud) {
		// Ensures that a fake account has been created
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain",
			networkSecret: "Password123")
		.Then(loginResult => {
			cloud.UserExists(
				network: LoginNetwork.Email,
				networkId: "cloud@localhost.localdomain")
			.Then(checkResult => {
				Assert(checkResult, "UserExists failed");
				CompleteTest();
			});
		});
	}

	[Test("Checks the send reset link functionality.", "Known to timeout sometimes (server side issue).")]
	public void ShouldSendAccountResetLink(Cloud cloud) {
		// This method is broken because we cannot GET somewhere with a body
		// We have to fix the server or get rid of this method & test
		cloud.SendResetPasswordEmail(
			userEmail: "cloud@localhost.localdomain",
			mailSender: "admin@localhost.localdomain",
			mailTitle: "Reset link",
			mailBody: "Here is your link: [[SHORTCODE]]")
		.Then(result => {
			Assert(result, "Should succeed to send reset password mail");
			CompleteTest();
		});
	}

	[Test("Changes the password of an e-mail account.")]
	public void ShouldChangePassword(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123")
		.Then(gamer => {
			gamer.Account.ChangePassword("Password124")
			.Then(pswResult => {
				Assert(pswResult, "Change password failed");
				CompleteTest();
			});
		});
	}

	[Test("Changes the e-mail address associated to an e-mail account.")]
	public void ShouldChangeEmailAddress(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123")
		.Then(gamer => {
			gamer.Account.ChangeEmailAddress(RandomEmailAddress())
			.Then(pswResult => {
				Assert(pswResult, "Change email failed");
				CompleteTest();
			});
		});
	}

	[Test("Changes the e-mail address associated to an e-mail account.")]
	public void ShouldFailToChangeEmailAddressToExistingOne(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123")
		.Then(gamer => {
			gamer.Account.ChangeEmailAddress("clan@localhost.localdomain")
			.Catch(ex => {
				CotcException pswResult = (CotcException)ex;
				Assert(pswResult.HttpStatusCode == 400, "400 expected");
				Assert(pswResult.ServerData["message"] == "UserExists", "UserExists error expected");
				CompleteTest();
			})
			.Done(result => IntegrationTest.Fail("Email change should fail"));
		});
	}
}
