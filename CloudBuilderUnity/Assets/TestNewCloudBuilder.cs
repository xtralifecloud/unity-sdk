using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class TestNewCloudBuilder : MonoBehaviour {

	// Use this for initialization
	void Start() {
		Bundle config = Bundle.CreateObject();
		config["key"] = "cloudbuilder-key";
		config["secret"] = "azerty";
//		config["env"] = "http://195.154.227.44:8000";
//		config["env"] = "http://localhost:8000";
		config["env"] = "https://sandbox-api[id].clanofthecloud.mobi";
		config["httpVerbose"] = true;
//		config["httpTimeout"] = 2000;
		CloudBuilder.Clan.Setup(delegate(CloudResult result) {
			Debug.Log("Done: " + result.ToString());
		}, config);
	}
	
	// Update is called once per frame
	void Update() {
	
	}

	public void DoLogin() {
		CloudBuilder.Clan.LoginAnonymous(delegate(CloudResult result) {
			Debug.Log("Login done: " + result.ToString());
        }, Bundle.Empty);
    }

	public void DoGetProfile() {
		CloudBuilder.Clan.TEMP_GetUserProfile(delegate(CloudResult result) {
			Debug.Log("Get profile done: " + result.ToString());
		}, Bundle.Empty);
	}
}
