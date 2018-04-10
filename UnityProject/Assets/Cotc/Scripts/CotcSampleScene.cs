using UnityEngine;
using System.Collections;
using CotcSdk;
using UnityEngine.UI;
using System;

#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif

public class CotcSampleScene : MonoBehaviour {
	// The cloud allows to make generic operations (non user related)
	private Cloud Cloud;
	// The gamer is the base to perform most operations. A gamer object is obtained after successfully signing in.
	private Gamer Gamer;
	// The friend, when fetched, can be used to send messages and such
	private string FriendId;
	// When a gamer is logged in, the loop is launched for domain private. Only one is run at once.
	private DomainEventLoop Loop;
	// Input field
	public InputField EmailInput;
	// Default parameters
	private const string DefaultEmailAddress = "me@localhost.localdomain";
	private const string DefaultPassword = "Pass1234";

	// Use this for initialization
	void Start() {
		// Link with the CotC Game Object
		var cb = FindObjectOfType<CotcGameObject>();
		if (cb == null) {
			Debug.LogError("Please put a Clan of the Cloud prefab in your scene!");
			return;
		}
		// Log unhandled exceptions (.Done block without .Catch -- not called if there is any .Then)
		Promise.UnhandledException += (object sender, ExceptionEventArgs e) => {
			Debug.LogError("Unhandled exception: " + e.Exception.ToString());
		};
		// Initiate getting the main Cloud object
		cb.GetCloud().Done(cloud => {
			Cloud = cloud;
			// Retry failed HTTP requests once
			Cloud.HttpRequestFailedHandler = (HttpRequestFailedEventArgs e) => {
				if (e.UserData == null) {
					e.UserData = new object();
					e.RetryIn(1000);
				}
				else
					e.Abort();
			};
			Debug.Log("Setup done");
		});
		// Use a default text in the e-mail address
		EmailInput.text = DefaultEmailAddress;
	}
	
	// Signs in with an anonymous account
	public void DoLogin() {
		// Call the API method which returns an Promise<Gamer> (promising a Gamer result).
		// It may fail, in which case the .Then or .Done handlers are not called, so you
		// should provide a .Catch handler.
		Cloud.LoginAnonymously()
			.Then(gamer => DidLogin(gamer))
			.Catch(ex => {
				// The exception should always be CotcException
				CotcException error = (CotcException)ex;
				Debug.LogError("Failed to login: " + error.ErrorCode + " (" + error.HttpStatusCode + ")");
			});
	}

	// Log in by e-mail
	public void DoLoginEmail() {
		// You may also not provide a .Catch handler and use .Done instead of .Then. In that
		// case the Promise.UnhandledException handler will be called instead of the .Done
		// block if the call fails.
		Cloud.Login(
			network: LoginNetwork.Email.Describe(),
			networkId: EmailInput.text,
			networkSecret: DefaultPassword)
		.Done(this.DidLogin);
	}

	public void DoLoginGameCenter() {
		Social.localUser.Authenticate(success => {
			if (success) {
				Debug.Log("Authentication successful:\nUsername: " + Social.localUser.userName + 
					"\nUser ID: " + Social.localUser.id + 
					"\nIsUnderage: " + Social.localUser.underage);
				// Game Center accounts do not have a password
				Cloud.Login(LoginNetwork.GameCenter.Describe(), Social.localUser.id, "n/a").Done(this.DidLogin);
			}
			else {
				Debug.LogError("Failed to authenticate on Game Center");
			}
		});
	}

	// Converts the account to e-mail
	public void DoConvertToEmail() {
		if (!RequireGamer()) return;
		Gamer.Account.Convert(
			network: LoginNetwork.Email.ToString().ToLower(),
			networkId: EmailInput.text,
			networkSecret: DefaultPassword)
		.Done(dummy => {
			Debug.Log("Successfully converted account");
		});
	}

	// Fetches a friend with the given e-mail address
	public void DoFetchFriend() {
		string address = EmailInput.text;
		Cloud.ListUsers(filter: address)
		.Done(friends => {
			if (friends.Total != 1) {
				Debug.LogWarning("Failed to find account with e-mail " + address + ": " + friends.ToString());
			}
			else {
				FriendId = friends[0].UserId;
				Debug.Log(string.Format("Found friend {0} ({1} on {2})", FriendId, friends[0].NetworkId, friends[0].Network));
			}
		});
	}

	// Sends a message to the current friend
	public void DoSendMessage() {
		if (!RequireGamer() || !RequireFriend()) return;

		Gamer.Community.SendEvent(
			gamerId: FriendId,
			eventData: Bundle.CreateObject("hello", "world"),
			notification: new PushNotification().Message("en", "Please open the app"))
		.Done(dummy => Debug.Log("Sent event to gamer " + FriendId));
	}

	// Posts a sample transaction
	public void DoPostTransaction() {
		if (!RequireGamer()) return;

		Gamer.Transactions.Post(Bundle.CreateObject("gold", 50))
		.Done(result => {
			Debug.Log("TX result: " + result.ToString());
		});
	}

	// Invoked when any sign in operation has completed
	private void DidLogin(Gamer newGamer) {
		if (Gamer != null) {
			Debug.LogWarning("Current gamer " + Gamer.GamerId + " has been dismissed");
			Loop.Stop();
		}
		Gamer = newGamer;
		Loop = Gamer.StartEventLoop();
		Loop.ReceivedEvent += Loop_ReceivedEvent;
		Debug.Log("Signed in successfully (ID = " + Gamer.GamerId + ")");
	}

	private void Loop_ReceivedEvent(DomainEventLoop sender, EventLoopArgs e) {
		Debug.Log("Received event of type " + e.Message.Type + ": " + e.Message.ToJson());
	}

	private bool RequireGamer() {
		if (Gamer == null)
			Debug.LogError("You need to login first. Click on a login button.");
		return Gamer != null;
	}

	private bool RequireFriend() {
		if (FriendId == null)
			Debug.LogError("You need to fetch a friend first. Fill the e-mail address field and click Fetch Friend.");
		return FriendId != null;
	}
}
