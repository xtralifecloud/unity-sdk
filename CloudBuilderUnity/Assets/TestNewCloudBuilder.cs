using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using LitJson;

public class TestNewCloudBuilder : MonoBehaviour {

	// Use this for initialization
	void Start() {
		JsonData config = new JsonData();
		config["key"] = "cloudbuilder-key";
		config["secret"] = "azerty";
		config["env"] = "http://195.154.227.44:8000";
		config["httpVerbose"] = true;
		CloudBuilder.Clan.Setup(config);
	}
	
	// Update is called once per frame
	void Update() {
	
	}

	public void DoLogin() {
		CloudBuilder.Clan.LoginAnonymous();
    }
}
