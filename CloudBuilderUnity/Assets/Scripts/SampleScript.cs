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

	public void DoListUsers() {
		Cloud.Index("testpromise").IndexObject("p1", Bundle.CreateObject("hello", "world"), Bundle.CreateObject("itsme", "mario"))
			.Then(done => Cloud.Index("testpromise").IndexObject("p2", Bundle.CreateObject("hello", "world"), Bundle.CreateObject("itsme", "mario")))
			.Then(done => Cloud.Index("testpromise").IndexObject("p3", Bundle.CreateObject("hello", "world"), Bundle.CreateObject("itsme", "mario")))
			.Then(done => {
				Debug.Log("Indexed 3 objects");
				Cloud.Index("testpromise").Search("hello:world", null, 2, 0)
					.Then(result => { GotUsers(result.Hits); });
			});
	}

	private void GotUsers(PagedList<IndexResult> hits) {
		Debug.Log("Showing " + hits.Count + " of " + hits.Total + ", with first being " + hits[0].ToJson());
		if (hits.HasNext) {
			hits.FetchNext().Then(nextHits => {
				GotUsers(nextHits);
			});
		}
	}
	
	// Signs in with an anonymous account
	public void DoLogin() {
		Cloud.LoginAnonymously().Done(this.DidLogin);
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
