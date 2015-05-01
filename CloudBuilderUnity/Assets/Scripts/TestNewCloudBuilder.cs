using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class TestNewCloudBuilder : MonoBehaviour {
	private Clan Clan;
	private User User;

	// Use this for initialization
	void Start() {
		CloudBuilder.Setup(new CloudBuilder.SetupParams {
			apiKey = "cloudbuilder-key",
			apiSecret = "azerty",
			environment = CloudBuilder.SandboxEnvironment,
			httpVerbose = true,
			eventLoopTimeout = 10,
			done = (CloudResult result, Clan clan) => {
				Clan = clan;
				Debug.Log("Setup done");
			}
		});
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

		Clan.LoginAnonymously(new Clan.LoginAnonymouslyParams {
			done = (CloudResult result, User user) => {
				if (result.ErrorCode == ErrorCode.enNoErr) {
					User = user;
					Debug.Log("Login done! Welcome " + user.GamerId + "!");
				}
				else
					Debug.Log("Login failed :(");
			}
		});
	}

	public void DoGetProfile() {
		if (User == null) {
			Debug.Log("Please log in first");
			return;
		}

		User.Profile.Get(new User.GetProfileParams {
			done = (CloudResult result, UserProfile profile) => {
				Debug.Log("Get profile done: " + profile.Properties.ToJson());
			}
		});
	}
}
