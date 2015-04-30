using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class TestNewCloudBuilder : MonoBehaviour {
	private Clan Clan;
	private User User;

	// Use this for initialization
	void Start() {
		CloudBuilder.Setup(
            done: (CloudResult result, Clan clan) => {
				Clan = clan;
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

		User.TEMP_GetProfile((CloudResult result, string unused) => {
			Debug.Log("Get profile done: " + result.ToString());
		});
	}
}
