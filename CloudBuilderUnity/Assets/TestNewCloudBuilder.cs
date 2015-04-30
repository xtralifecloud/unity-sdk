using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using CloudBuilderLibrary.Model.Gamer;

public class TestNewCloudBuilder : MonoBehaviour {

	// Use this for initialization
	void Start() {
		CloudBuilder.Clan.Setup(
			eventLoopTimeout: 10,
			apiKey: "cloudbuilder-key",
			apiSecret: "azerty",
//			environment: "http://localhost:8000",
			environment: Clan.SandboxEnvironment,
			httpVerbose: true,
			done: () => {
				Debug.Log("Setup done");
			}
		);
	}
	
	// Update is called once per frame
	void Update() {
	
	}

	public void DoLogin() {
		CloudBuilder.UserManager.LoginAnonymous((CloudResult result, LoggedGamerData gamerData) => {
			if (result.ErrorCode == ErrorCode.enNoErr)
				Debug.Log("Login done! Welcome " + gamerData.GamerId + "!");
			else
				Debug.Log("Login failed :(");
        });
    }

	public void DoGetProfile() {
		CloudBuilder.UserManager.TEMP_GetUserProfile((CloudResult result, string unused) => {
			Debug.Log("Get profile done: " + result.ToString());
		});
	}
}
