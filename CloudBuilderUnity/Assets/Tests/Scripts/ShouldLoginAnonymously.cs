using System;
using UnityEngine;
using CloudBuilderLibrary;

[IntegrationTest.DynamicTestAttribute("CloudBuilderLibraryTests")]
// [IntegrationTest.Ignore]
//[IntegrationTest.ExpectExceptions(false, typeof(ArgumentException))]
[IntegrationTest.SucceedWithAssertions]
//[IntegrationTest.ExcludePlatformAttribute(RuntimePlatform.Android, RuntimePlatform.LinuxPlayer)]
public class ShouldLoginAnonymously : MonoBehaviour
{
    public void Start()
    {
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
}
