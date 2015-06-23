using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using CotcSdk;
using UnityEngine;

/**
 * Base for an integration test class.
 * Usage is typically as follow: @code
	public class TestCategory: TestBase {
		[InstanceMethod(typeof(TestCategory))]
		public string TestMethodName;

		// Test boostrap code (run for each test)
		void Start() {
			FindObjectOfType<CotcGameObject>().GetCloud(cloud => {
				GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, new object[] { cloud });
			});
		}
		
		[Test("Does something.")]
		public void ShouldDoSomething(Cloud cloud) {
			Assert(someCondition, "error message otherwise");
			cloud.SomeAsynchronousCall(result => {
				Assert(result.IsSuccessful);
				...
				CompleteTest(); // Completes the async test.
	 		});
		}
	}
 * Integration tests are then done by creating a test object to the IntegrationTestScene scene via the Integration
 * Tests runner panel, adding the `TestCategory` class above as a script, and then choosing the right method to invoke.
 * Try to give to your test the same name as the method.
 */
public class TestBase : MonoBehaviour {
	private List<string> PendingSignals = new List<string>();
	private Dictionary<string, Action> RegisteredSlots = new Dictionary<string, Action>();

	protected void Assert(bool condition, string message) {
		if (!condition) {
			throw new Exception("Test interrupted because of failed assertion: " + message);
		}
	}

	protected void CompleteTest() {
		IntegrationTest.Pass();
	}

	protected string GetAllTestScopedId(string prefix) {
		return FindObjectOfType<TestUtilities>().GetAllTestScopedId(prefix);
	}

	protected void Login(Cloud cloud, Action<Gamer> done) {
		cloud.Login(
			network: LoginNetwork.Email,
			networkId: "cloud@localhost.localdomain",
			networkSecret: "Password123")
		.Then(gamer => {
			done(gamer);
		})
		.Catch(ex => IntegrationTest.Fail("Failed to log in"));
	}

	protected void Login2Users(Cloud cloud, Action<Gamer, Gamer> done) {
		Login(cloud, gamer1 => {
			// Second user
			cloud.Login(
				network: LoginNetwork.Email,
				networkId: "clan2@localhost.localdomain",
				networkSecret: "Password123")
			.Then(gamer2 => {
				done(gamer1, gamer2);
			})
			.Catch(ex => IntegrationTest.Fail("Failed to log in"));
		});
	}

	protected void LoginNewUser(Cloud cloud, Action<Gamer> done) {
		cloud.LoginAnonymously()
		.Then(gamer => { done(gamer); })
		.Catch(ex => IntegrationTest.Fail("Failed to log in"));
	}

	protected void Login2NewUsers(Cloud cloud, Action<Gamer, Gamer> done) {
		cloud.LoginAnonymously().Then(gamer1 => {
			// Second user
			return cloud.LoginAnonymously().Then(gamer2 => {
				done(gamer1, gamer2);
			});
		})
		.Catch(ex => IntegrationTest.Fail("Failed to log in"));
	}

	protected string RandomEmailAddress() {
		string randomPart = Guid.NewGuid().ToString();
		return string.Format("test{0}@localhost.localdomain", randomPart);
	}

	protected void RunLater(int millisec, Action action) {
		new Thread(new ThreadStart(() => {
			Thread.Sleep(millisec);
			action();
		})).Start();
	}

	// Registers a method that is run once when a signal is triggered
	protected void RunOnSignal(string signalName, Action action) {
		bool runThisAction = false;
		lock (this) {
			if (RegisteredSlots.ContainsKey(signalName)) throw new ArgumentException("Already registered a method for signal " + signalName);
			// Was previously triggered (race condition)
			if (PendingSignals.Contains(signalName)) {
				runThisAction = true;
				PendingSignals.Remove(signalName);
			}
			else {
				RegisteredSlots[signalName] = action;
			}
		}
		if (runThisAction) action();
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	protected void Signal(string signalName) {
		Action action = null;
		lock (this) {
			if (RegisteredSlots.ContainsKey(signalName)) {
				action = RegisteredSlots[signalName];
				RegisteredSlots.Remove(signalName);
			}
			else if (!PendingSignals.Contains(signalName)) {
				PendingSignals.Add(signalName);
			}
		}
		if (action != null) action();
	}
}
