using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;

public class ClanTests : MonoBehaviour
{
	[InstanceMethodAttribute(typeof(ClanTests))]
	public string TestMethodName;

    public void Start() {
		typeof(ClanTests).GetMethod(TestMethodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
    }

	void ShouldSetupProperly() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			IntegrationTest.Pass();
		});
	}

	void ShouldLoginAnonymously() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		Debug.LogWarning(System.Threading.Thread.CurrentThread.ManagedThreadId);
		cb.GetClan(clan => {
			clan.LoginAnonymously(result => {
				Debug.LogWarning(System.Threading.Thread.CurrentThread.ManagedThreadId);
				if (result.IsSuccessful && result.Value != null)
					IntegrationTest.Pass();
				else
					IntegrationTest.Fail("Didn't get the gamer successfully");
			});
		});
	}

	void ShouldRestoreSession() {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			clan.ResumeSession(
				done: result => {
					if (result.IsSuccessful && result.Value != null)
						IntegrationTest.Pass();
					else
						IntegrationTest.Fail("Didn't get the gamer successfully");
				},
				gamerId: "55546a491b07bd22748cea76",
				gamerSecret: "f26b29fbf9fdb4e469c9522cd5b5de859a6f936f"
			);
		});
	}
}
