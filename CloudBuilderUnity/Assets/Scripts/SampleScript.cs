#define USE_FACEBOOK

using UnityEngine;
using System.Collections;
using CotcSdk;
using UnityEngine.UI;

public class SampleScript : MonoBehaviour {
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
		var cb = FindObjectOfType<CotcGameObject>();
		if (cb == null) {
			Debug.LogError("Please put a Clan of the Cloud prefab in your scene!");
			return;
		}
		// Initiate getting the main Cloud object
		cb.GetCloud().Done(cloud => {
			Cloud = cloud;
			Promise.UnhandledException += (object sender, ExceptionEventArgs e) => {
				Debug.LogError("Unhandled exception: " + e.Exception.ToString());
			};
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
		Cloud.LoginAnonymously().Done(this.DidLogin);
	}

	// Signs in with facebook
	public void DoLoginWithFacebook() {
#if USE_FACEBOOK
		var fb = FindObjectOfType<CotcFacebookIntegration>();
		if (fb == null) {
			Debug.LogError("Please put the CotcFacebookIntegration prefab in your scene!");
			return;
		}
		fb.LoginWithFacebook(Cloud).Done(this.DidLogin);
#else
		Debug.LogError("Facebook not included (uncomment #define USE_FACEBOOK).");
#endif
	}

	// Log in by e-mail
	public void DoLoginEmail() {
		Cloud.Login(
			network: LoginNetwork.Email,
			networkId: EmailInput.text,
			networkSecret: DefaultPassword)
		.Done(this.DidLogin);
	}

	// Converts the account to e-mail
	public void DoConvertToEmail() {
		if (!RequireGamer()) return;
		Gamer.Account.Convert(
			network: LoginNetwork.Email,
			networkId: EmailInput.text,
			networkSecret: DefaultPassword)
		.Then(dummy => {
			Debug.Log("Successfully converted account");
		});
	}

	// Fetches a friend with the given e-mail address
	public void DoFetchFriend() {
		string address = EmailInput.text;
		Cloud.ListUsers(filter: address)
		.Then(friends => {
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

	public void DoAddFacebookFriends() {
#if USE_FACEBOOK
		var fb = FindObjectOfType<CotcFacebookIntegration>();
		if (fb == null) {
			Debug.LogError("Please put the CotcFacebookIntegration prefab in your scene!");
			return;
		}
		// List facebook friends
		fb.FetchFriends(Gamer).Then(result => {
			foreach (SocialNetworkFriend f in result.ByNetwork[LoginNetwork.Facebook]) {
				// Those who have a CotC account, add them as friend
				if (f.ClanInfo != null) {
					Gamer.Community.AddFriend(f.ClanInfo.GamerId)
					.Then(addFriendResult => {
						if (!addFriendResult) {
							Debug.LogError("Failed to add friend " + f.ClanInfo.GamerId);
						}
					});
				}
			}
		});
#else
		Debug.LogError("Facebook not included (uncomment #define USE_FACEBOOK).");
#endif
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
		Loop = new DomainEventLoop(Gamer).Start();
		Loop.ReceivedEvent += Loop_ReceivedEvent;
		Debug.Log("Signed in successfully (ID = " + Gamer.GamerId + ")");
	}

	private void Loop_ReceivedEvent(DomainEventLoop sender, EventLoopArgs e) {
		Debug.Log("Received event of type " + e.Message.Type + ": " + e.Message.ToJson());
	}

	private bool RequireGamer() {
		if (FriendId == null)
			Debug.LogError("You need to fetch a friend first. Fill the e-mail address field and click Fetch Friend.");
		return FriendId != null;
	}

	private bool RequireFriend() {
		if (FriendId == null)
			Debug.LogError("You need to fetch a friend first. Fill the e-mail address field and click Fetch Friend.");
		return FriendId != null;
	}
}
