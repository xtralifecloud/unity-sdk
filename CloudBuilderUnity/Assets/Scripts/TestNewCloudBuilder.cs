using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class TestNewCloudBuilder : MonoBehaviour {
	private Clan Clan;
	private User User;
	
	// Use this for initialization
	void Start() {
		CloudBuilder.Setup(
			done: (Result<Clan> result) => {
				Clan = result.Value;
				Debug.Log("Setup done");
			},
			apiKey: "cloudbuilder-key",
			apiSecret: "azerty",
			environment: CloudBuilder.SandboxEnvironment,
			httpVerbose: true,
			eventLoopTimeout: 10
		);
	}
	
	// Update is called once per frame
	void Update() {
	
	}

	void OnApplicationQuit() {
		CloudBuilder.Terminate();
	}

	public void DoLogin() {
		if (Clan == null) {
			Debug.Log("Please wait for setup to finish first");
			return;
		}

		Clan.LoginAnonymously((Result<User> result) => {
			if (result.IsSuccessful) {
				User = result.Value;
				Debug.Log("Login done! Welcome " + User.GamerId + "!");
			}
			else
				Debug.Log("Login failed :(");
		});
	}

	public void DoGetProfile() {
		if (User == null) {
			Debug.Log("Please log in first");
			return;
		}

		User.Profile.Get((Result<UserProfile> result) => {
			if (result.IsSuccessful)
				Debug.Log("Get profile done: " + result.Value.Properties.ToJson());
			else
				Debug.Log("Get profile failed");
		});
	}

	public void DoTerminate() {
		CloudBuilder.Terminate();
	}
}
