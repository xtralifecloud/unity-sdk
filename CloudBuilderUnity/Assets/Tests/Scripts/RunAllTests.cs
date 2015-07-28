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
		typeof(CloudTests),
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
	private Promise NextTestPromise;
	private int CurrentTestClassNo = 0, PassedTests = 0, FailedTests = 0;
	private ManualResetEvent TestDone = new ManualResetEvent(false);
	private const int TestTimeoutMillisec = 30000;
	// Only run these tests (e.g. {"ShouldAddFriend", ...})
	private static readonly string[] FilterByTestName = {};

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
		Common.LogWarning("Not running tests (RunAllTests) on this platform");
	}
#endif

	private bool IsFilteredOut(string testMethodName) {
		if (FilterByTestName.Length == 0) {
			return false;
		}
		foreach (string s in FilterByTestName) {
			if (s == testMethodName) {
				return false;
			}
		}
		return true;
	}
	
	private Dictionary<string, MethodInfo> ListTestMethods(Type type) {
		var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		var matching = new Dictionary<string, MethodInfo>();
		foreach (var method in allMethods) {
			var attrs = method.GetCustomAttributes(typeof(Test), false);
			if (attrs == null || attrs.Length == 0) continue;
			// Method matches, just check if it's not filtered out
			if (!IsFilteredOut(method.Name)) {
				matching[method.Name] = method;
			}
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
		if (p != null) p.Resolve();
	}

	private void ProcessNextTestClass() {
		if (CurrentTestClassNo >= TestTypes.Length) {
			Common.Log("Tests completed. Passed: " + PassedTests + ", failed: " + FailedTests);
			return;
		}

		Type t = TestTypes[CurrentTestClassNo++];
		TestBase test = (TestBase)gameObject.AddComponent(t);
		var methods = ListTestMethods(t);
		Promise initialPromise = new Promise(), allTestPromise = initialPromise;
		// And run test methods
		foreach (var pair in methods) {
			string methodName = pair.Key;
			allTestPromise = allTestPromise.Then(() => {
				Common.Log("Running method " + t.Name + "::" + methodName);
				// Will be resolved in OnTestCompleted
				NextTestPromise = new Promise();
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
		initialPromise.Resolve();
		allTestPromise.Then(() => {
			ProcessNextTestClass();
		});
		return;
	}

	// Called upon timeout.
	private void TestTimedOut(object state, bool timedOut) {
		if (timedOut) {
			Common.LogError("Test timed out!");
			OnTestCompleted(false);
		}
	}
}
