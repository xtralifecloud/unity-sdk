using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudBuilderLibrary;
using UnityEngine;

/**
 * Base for an integration test class.
 * Usage is typically as follow: @code
	public class TestCategory: TestBase {
		[InstanceMethod(typeof(TestCategory))]
		public string TestMethodName;

		// Test boostrap code (run for each test)
		void Start() {
			FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
				GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, new object[] { clan });
			});
		}
		
		[Test("Does something.")]
		public void ShouldDoSomething(Clan clan) {
			Assert(someCondition, "error message otherwise");
			clan.SomeAsynchronousCall(result => {
				Assert(result.IsSuccessful);
				...
				CompleteTest(); // Completes the async test.
	 		});
		}
	}
 * Integration tests are then done by creating a test object to the CloudBuilderIntegrationTestScene scene via the Integration
 * Tests runner panel, adding the `TestCategory` class above as a script, and then choosing the right method to invoke.
 * Try to give to your test the same name as the method.
 */
public class TestBase : MonoBehaviour {

	protected void Assert(bool condition, string message = null) {
		if (!condition) IntegrationTest.Assert(condition, message);
	}

	protected void CompleteTest() {
		IntegrationTest.Pass();
	}

	protected void Login(Clan clan, Action<Gamer> done) {
		clan.Login(
			network: LoginNetwork.Email,
			networkId: "clan@localhost.localdomain",
			networkSecret: "Password123",
			done: result => {
				if (!result.IsSuccessful) IntegrationTest.Fail("Failed to log in");
				done(result.Value);
			}
		);
	}

	protected string RandomEmailAddress() {
		string randomPart = Guid.NewGuid().ToString();
		return string.Format("test{0}@localhost.localdomain", randomPart);
	}

}
