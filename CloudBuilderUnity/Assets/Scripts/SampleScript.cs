//#define USE_FACEBOOK

using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class SampleScript : MonoBehaviour {
	// The clan allows to make generic operations (non user related)
	private Clan Clan;
	// The gamer is the base to perform most operations. A gamer object is obtained after successfully signing in.
	private Gamer Gamer;

	// Use this for initialization
	void Start() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		if (cb == null) {
			Debug.LogError("Please put a CloudBuilder prefab in your scene!");
			return;
		}
		cb.GetClan(clan => {
			Clan = clan;
			Debug.Log("Setup done");
		});
	}
	
	// Update is called once per frame
	void Update() {}

	// Signs in with an anonymous account
	public void DoLogin() {
		Clan.LoginAnonymously(this.DidLogin);
	}

	// Signs in with facebook
	public void DoLoginWithFacebook() {
#if USE_FACEBOOK
		var fb = FindObjectOfType<CloudBuilderFacebookIntegration>();
		if (fb == null) {
			Debug.LogError("Please put the CloudBuilderFacebookIntegration prefab in your scene!");
			return;
		}
		fb.LoginWithFacebook(this.DidLogin, Clan);
#else
		Debug.LogError("Facebook not included (uncomment #define USE_FACEBOOK).");
#endif
	}

	// Posts a sample transaction
	public void DoPostTransaction() {
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
		Gamer = gamer.Value;
		Debug.Log("Signed in successfully (ID = " + Gamer.GamerId + ")");
	}
}
