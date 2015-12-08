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

	[Test("Tries to restore another session but tries to execute a batch that doesn't exist.")]
	public void ShouldLoginAndRunBatch(Cloud cloud) {
		Bundle batchNode = Bundle.CreateObject(
			"name", "nonexistingBatch",
			"domain", "private",
			"params", Bundle.CreateObject());
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain",
			networkSecret: "Password123",
			preventRegistration: false,
			additionalOptions: Bundle.CreateObject("thenBatch", batchNode)
		).ExpectFailure(ex => {
			Assert(ex.ServerData["name"] == "HookError", "Message should be HookError");
			Assert(ex.ServerData["message"].AsString().EndsWith("does not exist"), "Should indicate nonexisting batch");
			CompleteTest();
		});
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

	[Test("Tests the DidLogin notification.")]
	public void ShouldSendLoggedInNotification(Cloud cloud) {
		Cotc.LoggedIn += GotLoggedInNotification;
		Login(cloud, gamer => {});
	}

	[Test("Tests various functionality related to promises.")]
	public void PromisesShouldWorkProperly() {
		// 1) Tests that multiple Then and a Done block are called as expected.
		Promise<bool> p1 = new Promise<bool>();
		CountedPromise<bool> cp = new CountedPromise<bool>(3);
		p1.Then(value => {
			cp.Resolve(value);
		})
		.Then(value => {
			cp.Resolve(value);
		})
		.Done(value => {
			cp.Resolve(value);
		});
		p1.Resolve(true);
		cp.WhenCompleted.ExpectSuccess(success => PromisesShouldWorkProperlyPart2());
	}

	private void PromisesShouldWorkProperlyPart2() {
		// 2) Test that that an unhandled exception is triggered as expected
		Promise[] expectingException = new Promise[1];
		EventHandler<ExceptionEventArgs> promiseExHandler = (sender, e) => {
			expectingException[0].Resolve();
		};
		FailOnUnhandledException = false;
		Promise.UnhandledException += promiseExHandler;

		Promise<bool> prom = new Promise<bool>();
		// With just then, the handler should not be called
		expectingException[0] = new Promise().Then(() => FailTest("Should not call UnhandledException yet"));
		prom.Reject(new InvalidOperationException());

		// But after a done, it should be invoked
		Wait(100).Then(() => {
			expectingException[0] = new Promise();
			expectingException[0].Then(() => {
				Promise.UnhandledException -= promiseExHandler;
				FailOnUnhandledException = true;
				PromisesShouldWorkProperlyPart3();
			});
			prom.Done();
		});
	}

	private void PromisesShouldWorkProperlyPart3() {
		// 3) This is normally prevented by the IPromise interface, but we removed it because of iOS AOT issues
		Promise<bool> p1 = new Promise<bool>();
		Promise<bool> p2 = p1.Then(dummy => FailTest("Should not be called"));
		p2.Then(dummy => PromisesShouldWorkProperlyPart4());
		p2.Resolve(true);
	}

	private void PromisesShouldWorkProperlyPart4() {
		// 4) An exception in a Then block should be forwarded to the catch block
		Promise<bool> p = new Promise<bool>();
		p.Then(dummy => {
			throw new InvalidOperationException();
		})
		.Catch(ex => {
			CompleteTest();
		});
		p.Resolve(true);
	}

	[Test("Tests JSON-related functions.")]
	public void ShouldInterpretJsonProperly() {
		string json = "{\"products\":[{\"internalProductId\":\"android.test.purchased\",\"price\":0.965951,\"currency\":\"CHF\",\"productId\":\"CotcProduct3\"}]}";
		Bundle data = Bundle.FromJson(json);
		Assert(data["products"][0]["price"] == 0.965951, "Double values should be decoded properly");
		CompleteTest();
	}

	[Test("Fails to log in by short code (that's the best we can test without access to an actual e-mail address.")]
	public void ShouldLoginByShortcode(Cloud cloud) {
		cloud.LoginWithShortcode("lzX84KYj").ExpectFailure(ex => {
			Assert(ex.HttpStatusCode == 400, "Should return 400 HTTP code");
			Assert(ex.ServerData["name"] == "BadToken", "Bad token expected");
			CompleteTest();
		});
	}

	[Test("Tests the floating point bundle functionality")]
	public void ShouldStoreFloatsInBundles() {
		// Use a relatively "complex" value (PI) to ensure that precision is not lost internally
		Bundle b = Bundle.CreateObject("preset", Mathf.PI);
		Assert(b["preset"] == Mathf.PI, "Constructor-passed floating point check failed");
		b["key"] = Mathf.PI / 2;
		Assert(b["key"] == Mathf.PI / 2, "Floating point equality check failed");
		Assert(b["dummy"].AsFloat(Mathf.PI / 3) == Mathf.PI / 3, "Default float value failed");

		// 2nd test case
		Bundle floatBundle = 1.99f;
		Assert(floatBundle == 1.99f, "Implicit conversion equality check failed");
		CompleteTest();
	}

	[Test("Tests the root and parent functionality from bundles, including when they are cloned.")]
	public void ShouldHandleParentInBundles() {
		Bundle response = Bundle.FromJson("{\"godfather\":{\"gamer_id\":\"5649ea7ce314c6bb0916fa6e\",\"profile\":{\"displayName\":\"Jaddream2560968877\",\"lang\":\"French\"}},\"customData\":[{\"properties\":{\"dev_profile_v2\":\"AV;,BD;,UE;,FN;,LA;French,LN;,PS;Jaddream2560968877,RB;\"},\"gamer_id\":\"5649ea7ce314c6bb0916fa6e\"}]}");
		Bundle godfather = response["godfather"]["profile"];
		Assert(godfather.Root == response, "Hierarchy should work (root)");
		Assert(godfather.Parent["profile"] == godfather, "Hierarchy should work");

		// Now clone the structure and ensure that the links are kept
		Bundle cloned = godfather.Clone();
		Assert(cloned.Root.Has("godfather"), "Hierarchy should work (root)");
		Assert(cloned.Parent["profile"] == cloned, "Hierarchy should work");

		// Now we're happy, do the same with arrays
		Bundle arrayTest = Bundle.FromJson("[{\"obj\": [1, 2, 3, 4], \"prop\": {\"sub\": \"object\"}}, {\"obj\": [5, 6, 7, 8], \"prop\": {\"sub\": \"object2\"}}]");
		Bundle subArray = arrayTest[1]["obj"];
		Bundle subArrayClone = subArray.Clone();
		Assert(subArray[3] == 8, "Simple array test failed");
		Assert(subArray.Parent.Parent[1] == subArray.Parent, "Simple array parent test failed");
		Assert(subArrayClone.Root[1]["obj"] == subArrayClone, "Simple array clone root test 1 failed");
		Assert(subArrayClone.Root == subArrayClone.Parent.Parent, "Simple array clone root test 2 failed");
		CompleteTest();
	}

	[Test("Scratchpad type test, to be used to test temporary pieces of code or experimenting with the SDK.", "This test failing is not a problem, but avoid commiting it.")]
	public void FiddlingWithSdk(Cloud cloud) {
		CompleteTest();
	}

	#region Private
	private void GotLoggedInNotification(object sender, Cotc.LoggedInEventArgs e) {
		CompleteTest();
		Cotc.LoggedIn -= GotLoggedInNotification;
	}
	#endregion
}
