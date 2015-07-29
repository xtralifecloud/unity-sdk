using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;

public class CloudTests : TestBase {
	[InstanceMethod(typeof(CloudTests))]
	public string TestMethodName;

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("Tests a simple setup.")]
	public void ShouldSetupProperly() {
		var cb = FindObjectOfType<CotcGameObject>();
		cb.GetCloud().Then(cloud => {
			Assert(cloud != null, "Failed to fetch a cloud object");
		})
		.CompleteTestIfSuccessful();
	}

	[Test("Sets up and does a ping")]
	public void ShouldPing(Cloud cloud) {
		cloud.Ping().CompleteTestIfSuccessful();
	}

	[Test("Logs in anonymously.")]
	public void ShouldLoginAnonymously(Cloud cloud) {
		cloud.LoginAnonymously()
		.ExpectSuccess(result => {
			Assert(result != null, "Failed to fetch a gamer object");
			CompleteTest();
		});
	}

	[Test("First logs in anonymously, then tries to restore the session with the received credentials.")]
	public void ShouldRestoreSession(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain",
			networkSecret: "Password123")
		// Resume the session with the credentials just received
		.Then(gamer => cloud.ResumeSession(
			gamerId: gamer.GamerId,
			gamerSecret: gamer.GamerSecret))
		.Then(resumeResult => {
			Assert(resumeResult != null, "Resume failed");
		})
		.CompleteTestIfSuccessful();
	}

	[Test("Tests that a non-existing session fails to resume (account not created).")]
	public void ShouldNotRestoreInexistingSession(Cloud cloud) {
		// Resume the session with the credentials just received
		cloud.ResumeSession(
			gamerId: "15555f06c7b852423cb9074a",
			gamerSecret: "1f89a1efa49a3cf59d00f8badb03227d1b56840b")
		.ExpectFailure(resumeResult => {
			Assert(resumeResult.HttpStatusCode == 401, "401 status code expected");
			Assert(resumeResult.ServerData["name"] == "LoginError", "LoginError expected");
			CompleteTest();
		});
	}

	[Test("Tests that the prevent registration flag is taken in account correctly.")]
	public void ShouldPreventRegistration(Cloud cloud) {
		// Resume the session with the credentials just received
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "nonexisting@localhost.localdomain",
			networkSecret: "Password123",
			preventRegistration: true)
		.ExpectFailure(resumeResult => {
			Assert(resumeResult.HttpStatusCode == 400, "400 expected");
			Assert(resumeResult.ServerData["name"] == "PreventRegistration", "PreventRegistration error expected");
			CompleteTest();
		});
	}

	[Test("Tests that an anonymous account can be converted to an e-mail account.")]
	public void ShouldConvertAccount(Cloud cloud) {
		// Create an anonymous account
		cloud.LoginAnonymously()
		// Then convert it to e-mail
		.Then(gamer => gamer.Account.Convert(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123"))
		.Then(conversionResult => {
			Assert(conversionResult, "Convert account failed");
		})
		.CompleteTestIfSuccessful();
	}

	[Test("Ensures that an account cannot be converted to a credential that already exists.")]
	public void ShouldFailToConvertToExistingAccount(Cloud cloud) {
		// Ensures that a fake account has been created
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain",
			networkSecret: "Password123")
		.ExpectSuccess(dummyGamer => {
			// Create an anonymous account
			return cloud.LoginAnonymously();
		})
		.ExpectSuccess(gamer => {
			// Then try to convert it to the same e-mail as the fake account created at first
			gamer.Account.Convert(
				network: LoginNetwork.Email,
				networkId: "cloud@localhost.localdomain",
				networkSecret: "Anotherp4ss")
			.ExpectFailure(conversionResult => {
				Assert(conversionResult.HttpStatusCode == 400, "400 expected");
				Assert(conversionResult.ServerData["message"] == "UserExists", "UserExists error expected");
				CompleteTest();
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
		.Then(loginResult => cloud.UserExists(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain"))
		.Then(checkResult => {
			Assert(checkResult, "UserExists failed");
			cloud.UserExists(LoginNetwork.Email, "inexisting@localhost.localdomain")
			.ExpectFailure(dummy => {
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
		})
		.CompleteTestIfSuccessful();
	}

	[Test("Changes the password of an e-mail account.")]
	public void ShouldChangePassword(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123")
		.Then(gamer => gamer.Account.ChangePassword("Password124"))
		.Then(pswResult => {
			Assert(pswResult, "Change password failed");
		})
		.CompleteTestIfSuccessful();
	}

	[Test("Changes the e-mail address associated to an e-mail account.")]
	public void ShouldChangeEmailAddress(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123")
		.Then(gamer => gamer.Account.ChangeEmailAddress(RandomEmailAddress()))
		.Then(pswResult => {
			Assert(pswResult, "Change email failed");
		})
		.CompleteTestIfSuccessful();
	}

	[Test("Changes the e-mail address associated to an e-mail account.")]
	public void ShouldFailToChangeEmailAddressToExistingOne(Cloud cloud) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: RandomEmailAddress(),
			networkSecret: "Password123")
		.ExpectSuccess(gamer => {
			gamer.Account.ChangeEmailAddress("clan@localhost.localdomain")
			.ExpectFailure(pswResult => {
				Assert(pswResult.HttpStatusCode == 400, "400 expected");
				Assert(pswResult.ServerData["message"] == "UserExists", "UserExists error expected");
				CompleteTest();
			});
		});
	}

	[Test("Tests the DidLogin notification")]
	public void ShouldSendLoggedInNotification(Cloud cloud) {
		Cotc.LoggedIn += GotLoggedInNotification;
		Login(cloud, gamer => {});
	}

	#region Private
	private void GotLoggedInNotification(object sender, Cotc.LoggedInEventArgs e) {
		CompleteTest();
		Cotc.LoggedIn -= GotLoggedInNotification;
	}
	#endregion
}
