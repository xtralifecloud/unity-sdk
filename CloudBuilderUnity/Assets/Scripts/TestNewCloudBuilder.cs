using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class TestNewCloudBuilder : MonoBehaviour {
	private Clan Clan;
	private DomainEventLoop EventLoop;
	private Gamer Gamer;

	// Inherited
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

	void Update() {
	}

	void OnApplicationQuit() {
		DoTerminate();
	}

	// Responding to events
	public void DoLogin() {
		if (Clan == null) {
			Debug.Log("Please wait for setup to finish first");
			return;
		}

		Clan.LoginAnonymously(this.DidLogin);
	}

	public void DoRestoreSession() {
		if (Clan == null) {
			Debug.Log("Please wait for setup to finish first");
			return;
		}

		Clan.ResumeSession(
			done: this.DidLogin,
			gamerId: "541429b6dc346f01314edef3",
			gamerSecret: "6809888abc46b67105aea4c3583cd1e8e035a353"
		);
	}

	public void DoGetProfile() {
		if (Gamer == null) {
			Debug.Log("Please log in first");
			return;
		}

		Gamer.Profile.Get((Result<GamerProfile> result) => {
			if (result.IsSuccessful)
				Debug.Log("Get profile done: " + result.Value.Properties.ToJson());
			else
				Debug.Log("Get profile failed " + result.ToString());
		});
	}

	public void DoTerminate() {
		if (EventLoop != null)
			EventLoop.Stop();
		CloudBuilder.Terminate();
	}

	// Private
	private void DidLogin(Result<Gamer> result) {
		if (result.IsSuccessful) {
			Gamer = result.Value;
			EventLoop = new DomainEventLoop(Gamer).Start();
			Debug.Log("Login done! Welcome " + Gamer.GamerId + "!");
		}
		else
			Debug.Log("Login failed :( " + result.ToString());
	}
}
