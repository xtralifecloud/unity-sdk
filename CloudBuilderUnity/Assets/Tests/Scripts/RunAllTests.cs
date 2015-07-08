using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using IntegrationTests;
using System.Collections.Generic;
using System.Threading;
using CotcSdk;

public class RunAllTests : MonoBehaviour {

	private static readonly Type[] TestTypes = {
		typeof(ClanTests),
		typeof(CommunityTests),
		typeof(GamerTests),
		typeof(GameTests),
		typeof(GodfatherTests),
		typeof(IndexTests),
		typeof(MatchTests),
		typeof(ScoreTests),
		typeof(TransactionTests),
		typeof(VfsTests),
	};
	private Promise<bool> NextTestPromise;
	private int CurrentTestClassNo = 0, PassedTests = 0, FailedTests = 0;
	private ManualResetEvent TestDone = new ManualResetEvent(false);
	private const int TestTimeoutMillisec = 30000;

#if UNITY_IPHONE || UNITY_ANDROID
	// Use this for initialization
	void Start() {
		// Prepare to run integration tests in detached mode (uses static classes so a little bit dirty)
		TestBase.DoNotRunMethodsAutomatically = true;
		TestBase.OnTestCompleted += OnTestCompleted;

		// Run class by class, all test methods
		ProcessNextTestClass();
	}
#else
	void Start() {
		Debug.LogWarning("Not running tests (RunAllTests) on this platform");
	}
#endif
	
	private Dictionary<string, MethodInfo> ListTestMethods(Type type) {
		var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		var matching = new Dictionary<string, MethodInfo>();
		foreach (var method in allMethods) {
			var attrs = method.GetCustomAttributes(typeof(Test), false);
			if (attrs == null || attrs.Length == 0) continue;
			matching[method.Name] = method;
		}
		return matching;
	}

	// Called when a test completes
	private void OnTestCompleted(bool successful) {
		if (successful) PassedTests += 1;
		else FailedTests += 1;
		TestDone.Set();
		var p = NextTestPromise;
		NextTestPromise = null;
		if (p != null) p.Resolve(successful);
	}

	private void ProcessNextTestClass() {
		if (CurrentTestClassNo >= TestTypes.Length) {
			Debug.Log("Tests completed. Passed: " + PassedTests + ", failed: " + FailedTests);
			return;
		}

		Type t = TestTypes[CurrentTestClassNo++];
		TestBase test = (TestBase)gameObject.AddComponent(t);
		var methods = ListTestMethods(t);
		Promise<bool> initialPromise = new Promise<bool>();
		Promise<bool> allTestPromise = initialPromise;
		// And run test methods
		foreach (var pair in methods) {
			string methodName = pair.Key;
			allTestPromise = allTestPromise.Then(success => {
				Debug.Log("Running method " + t.Name + "::" + methodName);
				// Will be resolved in OnTestCompleted
				NextTestPromise = new Promise<bool>();
				// Handle possible timeout
				TestDone.Reset();
				ThreadPool.RegisterWaitForSingleObject(TestDone, new WaitOrTimerCallback(TestTimedOut), null, TestTimeoutMillisec, true); 
				// Run the actual method
				try {
					test.RunTestMethod(methodName, true);
				}
				catch (Exception ex) {
					TestBase.FailTest ("Exception inside test: " + ex.ToString());
				}
				return NextTestPromise;
			});
		}
		// Start the deferred chain
		initialPromise.Resolve(false);
		allTestPromise.Then(success => {
			ProcessNextTestClass();
		});
		return;
	}

	// Called upon timeout.
	private void TestTimedOut(object state, bool timedOut) {
		if (timedOut) {
			Debug.LogError("Test timed out!");
			OnTestCompleted(false);
		}
	}
}
