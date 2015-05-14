using System;
using UnityEngine;
using CloudBuilderLibrary;

[IntegrationTest.DynamicTestAttribute("CloudBuilderLibraryTests")]
// [IntegrationTest.Ignore]
//[IntegrationTest.ExpectExceptions(false, typeof(ArgumentException))]
[IntegrationTest.SucceedWithAssertions]
//[IntegrationTest.ExcludePlatformAttribute(RuntimePlatform.Android, RuntimePlatform.LinuxPlayer)]
public class ShouldRestoreSession : MonoBehaviour
{
    public void Start()
    {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		Debug.LogWarning(System.Threading.Thread.CurrentThread.ManagedThreadId);
		cb.GetClan(clan => {
			clan.ResumeSession(
				done: result => {
					Debug.LogWarning(System.Threading.Thread.CurrentThread.ManagedThreadId);
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
