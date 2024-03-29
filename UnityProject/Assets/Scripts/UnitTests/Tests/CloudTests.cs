using System;
using UnityEngine;
using CotcSdk;
using IntegrationTests;
using System.Collections.Generic;
using System.Collections;

public class CloudTests : TestBase {

	private const string BoBatchConfiguration = "Needs a set up batch to work (please check for the related test function's comment in code).";

    [Test("Tests a simple setup.")]
	public IEnumerator ShouldSetupProperly() {
		var cb = FindObjectOfType<CotcGameObject>();
        cb.GetCloud().ExpectSuccess(cloud => {
            Assert(cloud != null, "Failed to fetch a cloud object");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

    [Test("Sets up and does a ping")]
	public IEnumerator ShouldPing() {
        cloud.Ping().CompleteTestIfSuccessful();
        return WaitForEndOfTest();
    }

    [Test("Logs in anonymously.")]
	public IEnumerator ShouldLoginAnonymously() {
        cloud.LoginAnonymously()
        .ExpectSuccess(result => {
            Assert(result != null, "Failed to fetch a gamer object");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

    [Test("Log in anonymously. Then Log out.")]
    public IEnumerator ShouldLogout() {
        cloud.LoginAnonymously()
        .ExpectSuccess(result => cloud.Logout(result))
        .ExpectSuccess(result => CompleteTest());
        return WaitForEndOfTest();
    }

    [Test("First logs in anonymously, then tries to restore the session with the received credentials.")]
	public IEnumerator ShouldRestoreSession() {
        cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", "cloud@localhost.localdomain", "secret", "Password123"))
        // Resume the session with the credentials just received
        .ExpectSuccess(gamer => cloud.ResumeSession(
            gamerId: gamer.GamerId,
            gamerSecret: gamer.GamerSecret))
        .ExpectSuccess(resumeResult => {
            Assert(resumeResult != null, "Resume failed");
            CompleteTest();
        });
        return WaitForEndOfTest();
	}

	[Test("Tries to restore another session but tries to execute a batch that doesn't exist, then tries to execute an existing batch.", requisite: BoBatchConfiguration)]
	/*
		Backend prerequisites >> Add the following "unitTest_thenBatch" batch:
		
		function __unitTest_thenBatch(params, customData, mod) {
			"use strict";
			// don't edit above this line // must be on line 3
			// Used for unit test.
		  	return {thenBatchRun:true};
		} // must be on last line, no CR
	*/
    public IEnumerator ShouldLoginAndRunBatch() {
        Bundle batchNode = Bundle.CreateObject(
            "name", "nonexistingBatch",
            "domain", "private",
            "params", Bundle.CreateObject());
        cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", "cloud@localhost.localdomain", "secret", "Password123"),
            options: Bundle.CreateObject("thenBatch", batchNode, "preventRegistration", false))
        .ExpectFailure(ex => {
            Assert(ex.ServerData["name"] == "HookError", "Message should be HookError");
            Assert(ex.ServerData["message"].AsString().EndsWith("does not exist"), "Should indicate nonexisting batch");

            Bundle batchNode2 = Bundle.CreateObject(
            "name", "unitTest_thenBatch",
            "domain", "private",
            "params", Bundle.CreateObject());
            cloud.Login(
                network: LoginNetwork.Email.Describe(),
                credentials: Bundle.CreateObject("id", "cloud@localhost.localdomain", "secret", "Password123"),
                options: Bundle.CreateObject("thenBatch", batchNode2, "preventRegistration", false))
            .ExpectSuccess(gamer => {
                Assert(gamer["customData"]["thenBatchRun"].AsBool(), "Expected customData to contain 'thenBatchRun:true'");
                CompleteTest();
            });
        });
        return WaitForEndOfTest();
    }

    [Test("Tests that a non-existing session fails to resume (account not created).")]
	public IEnumerator ShouldNotRestoreInexistingSession() {
        // Resume the session with the credentials just received
        cloud.ResumeSession(
            gamerId: "15555f06c7b852423cb9074a",
            gamerSecret: "1f89a1efa49a3cf59d00f8badb03227d1b56840b")
        .ExpectFailure(resumeResult => {
            Assert(resumeResult.HttpStatusCode == 401, "401 status code expected");
            Assert(resumeResult.ServerData["name"] == "LoginError", "LoginError expected");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

    [Test("Tests that the prevent registration flag is taken in account correctly.")]
	public IEnumerator ShouldPreventRegistration() {
        // Resume the session with the credentials just received
        cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", RandomEmailAddress(), "secret", "Password123"),
            options: Bundle.CreateObject("preventRegistration", true))
        .ExpectFailure(resumeResult => {
            Assert(resumeResult.HttpStatusCode == 400, "400 expected");
            Assert(resumeResult.ServerData["name"] == "PreventRegistration", "PreventRegistration error expected");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

    [Test("Tests that an anonymous account can be converted to an e-mail account.")]
    public IEnumerator ShouldConvertAccount() {
        // Create an anonymous account
        cloud.LoginAnonymously()
        // Then convert it to e-mail
        .ExpectSuccess(gamer => gamer.Account.Convert(
            network: LoginNetwork.Email.ToString().ToLower(),
            credentials: Bundle.CreateObject("id", RandomEmailAddress(), "secret", "Password123")))
        .ExpectSuccess(conversionResult => {
            Assert(conversionResult, "Convert account failed");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

    [Test("Ensures that an account cannot be converted to a credential that already exists.")]
	public IEnumerator ShouldFailToConvertToExistingAccount() {
		// Ensures that a fake account has been created
		cloud.Login(
			network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", "cloud@localhost.localdomain", "secret", "Password123"))
		.ExpectSuccess(dummyGamer => {
			// Create an anonymous account
			return cloud.LoginAnonymously();
		})
		.ExpectSuccess(gamer => {
        // Then try to convert it to the same e-mail as the fake account created at first
        gamer.Account.Convert(
            network: LoginNetwork.Email.ToString().ToLower(),
            credentials: Bundle.CreateObject("id", "cloud@localhost.localdomain", "secret", "Anotherp4ss"))
			.ExpectFailure(conversionResult => {
				Assert(conversionResult.HttpStatusCode == 400, "400 expected");
				Assert(conversionResult.ServerData["message"] == "UserExists", "UserExists error expected");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

    [Test("Tests the auto-modification of the gamer object when converting the account")]
	public IEnumerator ShouldModifyGamerAfterConvertingAccount() {
		Gamer gamer = null;
        // Create an anonymous account
        cloud.LoginAnonymously()
        // Then convert it to e-mail
        .ExpectSuccess(g => {
            gamer = g;
            return g.Account.Convert(
                network: LoginNetwork.Email.ToString().ToLower(),
                credentials: Bundle.CreateObject("id", RandomEmailAddress(),"secret", "Password123"));
        })
        .ExpectSuccess(done => {
			Assert(gamer.Network == LoginNetwork.Email.Describe(), "The gamer object failed to change");
            CompleteTest();
        });
        return WaitForEndOfTest();
    }

    [Test("Checks the 'find user' functionality.")]
    public IEnumerator ShouldCheckIfUserExists() {
        // Ensures that a fake account has been created
        cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", "cloud@localhost.localdomain", "secret", "Password123"))
        .ExpectSuccess(loginResult => cloud.UserExists(
            network: LoginNetwork.Email.ToString().ToLower(),
            networkId: "cloud@localhost.localdomain"))
        .ExpectSuccess(checkResult => {
            Assert(checkResult, "UserExists failed");
            cloud.UserExists(LoginNetwork.Email.ToString().ToLower(), "inexisting@localhost.localdomain")
            .ExpectFailure(dummy => CompleteTest());
        });
        return WaitForEndOfTest();
    }

    [Test("Checks the send reset link functionality.", "Known to timeout sometimes (server side issue).")]
	public IEnumerator ShouldSendAccountResetLink() {
        cloud.SendResetPasswordEmail(
            userEmail: "cloud@localhost.localdomain",
            mailSender: "admin@localhost.localdomain",
            mailTitle: "Reset link",
            mailBody: "Here is your link: [[SHORTCODE]]")
        .ExpectSuccess(result => {
            Assert(result, "Should succeed to send reset password mail");
            CompleteTest();
        });
        return WaitForEndOfTest();
	}

    [Test("Changes the password of an e-mail account.")]
	public IEnumerator ShouldChangePassword() {
        Gamer gamer = null;
        cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", RandomEmailAddress(), "secret", "Password123"))
        .ExpectSuccess(g => {
            gamer = g;
            return gamer.Account.ChangePassword("Password124");
        })
        .ExpectSuccess(pswResult => {
            Assert(pswResult, "Change password failed");
            return cloud.Logout(gamer);
        })
         // Try to login with the new password
        .ExpectSuccess(result => cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", gamer.NetworkId, "secret", "Password124")))
        .ExpectSuccess(result => CompleteTest());
        return WaitForEndOfTest();
	}

    [Test("Changes the e-mail address associated to an e-mail account.")]
	public IEnumerator ShouldChangeEmailAddress() {
        Gamer gamer = null;
        String newEmail = RandomEmailAddress();
        cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", RandomEmailAddress(), "secret", "Password123"))
        .ExpectSuccess(g => {
            gamer = g;
            return gamer.Account.ChangeEmailAddress(newEmail);
        })
        .ExpectSuccess(pswResult => {
            Assert(pswResult, "Change email failed");
            return cloud.Logout(gamer);
        })
        // Try to login with the new email
        .ExpectSuccess(result => cloud.Login(
            network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", newEmail, "secret", "Password123")))
        .ExpectSuccess(result => CompleteTest());
        return WaitForEndOfTest();
	}

    [Test("Changes the e-mail address associated to an e-mail account.")]
	public IEnumerator ShouldFailToChangeEmailAddressToExistingOne() {
		cloud.Login(
			network: LoginNetwork.Email.Describe(),
            credentials: Bundle.CreateObject("id", RandomEmailAddress(), "secret", "Password123"))
		.ExpectSuccess(gamer => {
			gamer.Account.ChangeEmailAddress("clan@localhost.localdomain")
			.ExpectFailure(pswResult => {
				Assert(pswResult.HttpStatusCode == 400, "400 expected");
				Assert(pswResult.ServerData["message"] == "UserExists", "UserExists error expected");
				CompleteTest();
			});
		});
        return WaitForEndOfTest();
	}

    [Test("Tests the DidLogin notification.")]
	public IEnumerator ShouldSendLoggedInNotification() {
		Cotc.LoggedIn += GotLoggedInNotification;
		Login(cloud, gamer => { });
        return WaitForEndOfTest();
	}

    [Test("Tests various functionality related to promises.")]
	public IEnumerator PromisesShouldWorkProperly() {
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
        return WaitForEndOfTest();
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
		expectingException[0] = new Promise();
		expectingException[0].Done(() => {
			Promise.UnhandledException -= promiseExHandler;
			FailOnUnhandledException = true;
			PromisesShouldWorkProperlyPart3();
		});
        prom.Done();
    }

	private void PromisesShouldWorkProperlyPart3() {
		// 3) This is normally prevented by the IPromise interface, but we removed it because of iOS AOT issues
		Promise<bool> p1 = new Promise<bool>();
		Promise<bool> p2 = p1.Then(dummy => { FailTest("Should not be called"); });
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

    [Test("Tests advanced JSON-related functions.")]
	public IEnumerator ShouldInterpretAdvancedJsonProperly() {
		string json = "{\"maxLongOverflow\":9223372036854776000, \"minLongOverflow\":-9223372036854776000}";
		bool triggeredException = false;

		#pragma warning disable 0168
		#pragma warning disable 0219
		try {
			Bundle data = Bundle.FromJson(json);
		}
		catch (InvalidCastException ex) {
			triggeredException = true;
		}
		#pragma warning restore 0219
		#pragma warning restore 0168

		if (triggeredException)
			CompleteTest();
		else
			FailTest("Shouldn't be able to parse long-type json value exceeding possible max/min long-type variable values");
		
        return WaitForEndOfTest();
	}

    [Test("Tests JSON-related functions.")]
	public IEnumerator ShouldInterpretJsonProperly() {
		string json = "{\"products\":[{\"internalProductId\":\"android.test.purchased\",\"price\":0.965951,\"currency\":\"CHF\",\"productId\":\"CotcProduct3\"}]}";
		Bundle data = Bundle.FromJson(json);
		Assert(data["products"][0]["price"] == 0.965951, "Double values should be decoded properly");
		CompleteTest();
        return WaitForEndOfTest();
	}

    [Test("Fails to log in by short code (that's the best we can test without access to an actual e-mail address.")]
	public IEnumerator ShouldLoginByShortcode() {
		cloud.LoginWithShortcode("lzX84KYj").ExpectFailure(ex => {
			Assert(ex.HttpStatusCode == 400, "Should return 400 HTTP code");
			Assert(ex.ServerData["name"] == "BadToken", "Bad token expected");
			CompleteTest();
		});
        return WaitForEndOfTest();
	}

    [Test("Tests that an anonymous fails to link to an invalid facebook token (we cannot do much with automated testing).")]
    public IEnumerator ShouldLinkAndUnlinkAccount() {
        /**
         * This test was previously annoted with : "This test should fail from my understanding since the token is invalid, but for some reason it succeeds so we'll make it this way."
         * It seems to work as intended now (fail to link to an invalid facebook token), so we'll not make it this way anymore.
         */

        FailOnUnhandledException = false;

        // Create an anonymous account
        cloud.LoginAnonymously()
        // Then convert it to e-mail
        .ExpectSuccess(gamer => {
            gamer.Account.Link(
                network: LoginNetwork.Facebook.ToString().ToLower(),
                networkId: "100016379375516",
                networkSecret: "EAAENyTNQMpQBAJ8HvBZCh05WZCJXP9q4k6g5pXAdkMhyIzaNt7k57Jdqil57PKlO8HDtR5qeDzs1Sfy24aZAePLCtIi99LyWIqWFQQjraGOEj8aYW59aewZAZArOZBUBDHBahemWh2ZCulR4LIGUpkYVAfHWZCj58Kke9aQYRNorCQZDZD")
            .ExpectFailure(result => CompleteTest());
        });
    //        .ExpectSuccess(done => {
    //             // Data obtained by fetching friends from Facebook. Using real test accounts.
    //             Bundle data = Bundle.FromJson(@"{""data"":[{""name"":""Fr\u00e9d\u00e9ric Benois"",""id"":""107926476427271""}],""paging"":{""cursors"":{""before"":""QVFIUlY5TGkwWllQSU1tZAmN2NVlRaWlyeVpZAWk1idktkaU5GcFotRkp0RWlCVnNyR3MweUR5R3ZAfQ193ZAUhYWk84US0zVHdxdzdxMWswVTk2YUxlbVVlQXd3"",""after"":""QVFIUlY5TGkwWllQSU1tZAmN2NVlRaWlyeVpZAWk1idktkaU5GcFotRkp0RWlCVnNyR3MweUR5R3ZAfQ193ZAUhYWk84US0zVHdxdzdxMWswVTk2YUxlbVVlQXd3""}},""summary"":{""total_count"":1}}");

    //             List<SocialNetworkFriend> friends = new List<SocialNetworkFriend>();
    //             foreach (Bundle f in data["data"].AsArray()) {
    //                 friends.Add(new SocialNetworkFriend(f));
    //             }
    //             gamer.Community.ListNetworkFriends(LoginNetwork.Facebook, friends, true)
    //             .ExpectSuccess(response => {
    //                 Assert(response.ByNetwork[LoginNetwork.Facebook].Count == 1, "Should have registered 1 facebook users");
    //                 gamer.Account.Unlink(LoginNetwork.Facebook.ToString().ToLower())
    //                 .ExpectSuccess(done2 => {
    //                     CompleteTest();
    //                 });
    //             });

    //         });
    //});
        return WaitForEndOfTest();
	}

    [Test("Tests the floating point bundle functionality")]
	public IEnumerator ShouldStoreFloatsInBundles() {
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
        return WaitForEndOfTest();
	}

    [Test("Tests the root and parent functionality from bundles, including when they are cloned.")]
	public IEnumerator ShouldHandleParentInBundles() {
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
        return WaitForEndOfTest();
	}

    [Test("Scratchpad type test, to be used to test temporary pieces of code or experimenting with the SDK.", "This test failing is not a problem, but avoid commiting it.")]
	public IEnumerator FiddlingWithSdk() {
        CompleteTest();
        return WaitForEndOfTest();
    }

	#region Private
	private void GotLoggedInNotification(object sender, Cotc.LoggedInEventArgs e) {
		CompleteTest();
		Cotc.LoggedIn -= GotLoggedInNotification;
	}
	#endregion
}
