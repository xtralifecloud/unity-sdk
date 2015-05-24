
using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;

public class TestNewCloudBuilder : MonoBehaviour {
	private Clan Clan;
	private DomainEventLoop EventLoop;
	private Gamer Gamer;

	// Inherited
	void Start() {
		CloudBuilderGameObject cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(result => {
			Clan = result;
			Clan.HttpRequestFailedHandler = e => {
				int[] delays = { 1, 100, 1000 };
				int counter = (int) (e.UserData ?? 0);
				e.UserData = counter + 1;
				if (counter >= delays.Length) e.Abort();
				else e.RetryIn(delays[counter]);
			};
			Debug.Log("Setup done");
		});
	}

	// Responding to events
	public void DoLogin() {
		Clan.LoginAnonymously(done: this.DidLogin);
	}

	public void DoTest() {
	}

	// Private
	private void DidLogin(Result<Gamer> result) {
		if (result.IsSuccessful) {
			Gamer = result.Value;
			// Run an event loop
			if (EventLoop != null) EventLoop.Stop();
			EventLoop = new DomainEventLoop(Gamer).Start();
			Debug.Log("Login done! Welcome " + Gamer.GamerId + "!");
		}
		else
			Debug.Log("Login failed :( " + result.ToString());
	}
}
