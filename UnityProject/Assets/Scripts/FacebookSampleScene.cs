using UnityEngine;
using System.Collections;
using CotcSdk;
using UnityEngine.UI;
using CotcSdk.FacebookIntegration;

public class FacebookSampleScene : MonoBehaviour {
	// The cloud allows to make generic operations (non user related)
	private Cloud Cloud;
	// The gamer is the base to perform most operations. A gamer object is obtained after successfully signing in.
	private Gamer Gamer;
	// When a gamer is logged in, the loop is launched for domain private. Only one is run at once.
	private DomainEventLoop Loop;
	private bool ShouldBringLoginDialog;

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
	}

	void OnGUI() {
		if (ShouldBringLoginDialog) {
			var fb = FindObjectOfType<CotcFacebookIntegration>();
			ShouldBringLoginDialog = false;
			if (fb == null) {
				Debug.LogError("Please put the CotcFacebookIntegration prefab in your scene!");
				return;
			}
			fb.LoginWithFacebook(Cloud).Done(this.DidLogin);
		}
	}

	// Signs in with facebook
	public void DoLoginWithFacebook() {
		ShouldBringLoginDialog = true;
	}

	public void DoAddFacebookFriends() {
		var fb = FindObjectOfType<CotcFacebookIntegration>();
		if (fb == null) {
			Debug.LogError("Please put the CotcFacebookIntegration prefab in your scene!");
			return;
		}
		// List facebook friends
		fb.FetchFriends(Gamer).Done(result => {
			foreach (SocialNetworkFriend f in result.ByNetwork[LoginNetwork.Facebook]) {
				// Those who have a CotC account, add them as friend
				if (f.ClanInfo != null) {
					Gamer.Community.AddFriend(f.ClanInfo["_id"])
					.Done(addFriendResult => {
						if (!addFriendResult) {
							Debug.LogError("Failed to add friend " + f.ClanInfo.GamerId);
						}
					});
				}
			}
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
			Debug.LogError("You need to fetch a friend first. Fill the e-mail address field and click Fetch Friend.");
		return Gamer != null;
	}
}
