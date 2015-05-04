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

	void OnApplicationFocus(bool focused) {
		if (EventLoop != null) {
			if (focused)	EventLoop.Resume();
			else			EventLoop.Suspend();
		}
		CloudBuilder.OnApplicationFocus(focused);
	}

	void OnApplicationQuit() {
		if (EventLoop != null)
			EventLoop.Stop();
		CloudBuilder.OnApplicationQuit();
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
			gamerId: "5547438f454c74f11ca98177",
			gamerSecret: "e785694c0607c38f5674aa1621da9016c8b52262"
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
