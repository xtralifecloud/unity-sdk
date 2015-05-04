using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class TestNewCloudBuilder : MonoBehaviour {
	private Clan Clan;
	private User User;

	// Use this for initialization
	void Start() {
		CloudBuilder.Setup(
			apiKey: "cloudbuilder-key",
			apiSecret: "azerty",
			environment: CloudBuilder.SandboxEnvironment,
			httpVerbose: true,
			eventLoopTimeout: 10,
			done: (CloudResult result, Clan clan) => {
				Clan = clan;
				Debug.Log("Setup done");
			}
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

		Clan.LoginAnonymously((CloudResult result, User user) => {
			if (result.ErrorCode == ErrorCode.enNoErr) {
				User = user;
				Debug.Log("Login done! Welcome " + user.GamerId + "!");
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

		User.Profile.Get((CloudResult result, UserProfile profile) => {
			Debug.Log("Get profile done: " + profile.Properties.ToJson());
		});
	}

	public void DoTerminate() {
		CloudBuilder.Terminate();
	}
}
