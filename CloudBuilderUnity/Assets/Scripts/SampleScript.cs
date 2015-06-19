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
		cb.GetCloud(cloud => {
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
		Cloud.LoginAnonymously().ForwardTo(this.DidLogin);
	}

	// Signs in with facebook
	public void DoLoginWithFacebook() {
#if USE_FACEBOOK
		var fb = FindObjectOfType<CotcFacebookIntegration>();
		if (fb == null) {
			Debug.LogError("Please put the CotcFacebookIntegration prefab in your scene!");
			return;
		}
		fb.LoginWithFacebook(Cloud).ForwardTo(this.DidLogin);
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
		.ForwardTo(this.DidLogin);
	}

	// Converts the account to e-mail
	public void DoConvertToEmail() {
		if (!RequireGamer()) return;
		Gamer.Account.Convert(
			network: LoginNetwork.Email,
			networkId: EmailInput.text,
			networkSecret: DefaultPassword,
			done: result => {
				if (result.IsSuccessful)
					Debug.Log("Successfully converted account");
				else
					Debug.LogWarning("Failed to convert account: " + result.ToString());
			});
	}

	// Fetches a friend with the given e-mail address
	public void DoFetchFriend() {
		string address = EmailInput.text;
		Cloud.ListUsers(filter: address)
		.Then(result => {
			if (!result.IsSuccessful || result.Value.Total != 1)
				Debug.LogWarning("Failed to find account with e-mail " + address + ": " + result.ToString());
			else {
				FriendId = result.Value[0].UserId;
				Debug.Log(string.Format("Found friend {0} ({1} on {2})",
					FriendId, result.Value[0].NetworkId, result.Value[0].Network));
			}
		});
	}

	// Sends a message to the current friend
	public void DoSendMessage() {
		if (!RequireGamer() || !RequireFriend()) return;

		Gamer.Community.SendEvent(
			gamerId: FriendId,
			eventData: Bundle.CreateObject("hello", "world"),
			notification: new PushNotification().Message("en", "Please open the app"),
			done: result => {
				if (!result.IsSuccessful)
					Debug.LogWarning("Failed to send event: " + result.ToString());
				else
					Debug.Log("Sent event to gamer " + FriendId);
			});
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
			if (!result.IsSuccessful) {
				Debug.LogError("Failed to fetch facebook friends: " + result.ToString());
				return;
			}
			foreach (SocialNetworkFriend f in result.Value.ByNetwork[LoginNetwork.Facebook]) {
				// Those who have a CotC account, add them as friend
				if (f.ClanInfo != null) {
					Gamer.Community.AddFriend(
						gamerId: f.ClanInfo.GamerId,
						done: addFriendResult => {
							if (!addFriendResult.IsSuccessful) {
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

		Gamer.Transactions.Post(
			transaction: Bundle.CreateObject("gold", 50),
			done: (Result<TransactionResult> result) => {
				Debug.Log("TX result: " + result.ToString());
			}
		);
	}

	// Invoked when any sign in operation has completed
	private void DidLogin(Result<Gamer> gamer) {
		if (!gamer.IsSuccessful) {
			Debug.LogError("Login failed: " + gamer.ToString());
			return;
		}
		if (Gamer != null) {
			Debug.LogWarning("Current gamer " + Gamer.GamerId + " has been dismissed");
			Loop.Stop();
		}
		Gamer = gamer.Value;
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
