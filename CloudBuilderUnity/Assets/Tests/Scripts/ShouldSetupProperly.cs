using System;
using UnityEngine;
using CloudBuilderLibrary;

[IntegrationTest.DynamicTestAttribute("CloudBuilderLibraryTests")]
// [IntegrationTest.Ignore]
//[IntegrationTest.ExpectExceptions(false, typeof(ArgumentException))]
[IntegrationTest.SucceedWithAssertions]
[IntegrationTest.TimeoutAttribute(1)]
//[IntegrationTest.ExcludePlatformAttribute(RuntimePlatform.Android, RuntimePlatform.LinuxPlayer)]
public class ShouldSetupProperly : MonoBehaviour
{
    public void Start()
    {
		var cb = FindObjectOfType<CloudBuilderGameObject>();
		cb.GetClan(clan => {
			IntegrationTest.Pass();
		});
    }
}
