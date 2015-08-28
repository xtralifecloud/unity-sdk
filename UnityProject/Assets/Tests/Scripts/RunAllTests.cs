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
		typeof(StoreTests),
		typeof(TransactionTests),
		typeof(VfsTests),
	};
	private const int TestTimeoutMillisec = 30000;
	// Only run these tests (e.g. {"ShouldAddFriend", ...})
	private static readonly string[] FilterByTestName = {"ShouldWriteBinaryKey"};
	// But ignore these (takes precedence over FilterByTestName)
	private static readonly string[] IgnoreTestNames = {};

	// Set CurrentTestClassNo to -1 when all tests have been executed
	private int CurrentTestClassNo, PassedTests;
	private List<string> FailedTests;
	private ManualResetEvent TestDone = new ManualResetEvent(false);
	// In order to run all methods on the main thread (via Update)
	private TestBase CurrentClassTestComponent;
	private string CurrentTestClassName;
	private List<MethodInfo> CurrentClassMethods;
	private int CurrentTestMethodNo = 0;
	private bool IsRunningTestMethod;

	// Use this for initialization
	void Start() {
		// Prepare to run integration tests in detached mode (uses static classes so a little bit dirty)
		TestBase.DoNotRunMethodsAutomatically = true;
		TestBase.OnTestCompleted += OnTestCompleted;
		// Fail test on unhandled exception
		Promise.UnhandledException += (sender, e) => {
			if (TestBase.FailOnUnhandledException) {
				TestBase.FailTest("Unhandled exception in test: " + e.Exception);
			}
		};

		// Run class by class, all test methods
		CurrentTestClassNo = PassedTests = 0;
		FailedTests = new List<string>();
		IsRunningTestMethod = false;
		ProcessNextTestClass();
	}

	void Update() {
		// Tests all executed?
		if (CurrentTestClassNo == -1) {
			return;
		}

		// Process next method
		if (!IsRunningTestMethod) {
			ProcessNextTestMethod();
		}
	}

	private bool ShouldExecuteTest(string testMethodName) {
		// Ignored?
		foreach (string s in IgnoreTestNames) {
			if (s == testMethodName) {
				return false;
			}
		}
		// Run all if list is empty
		if (FilterByTestName.Length == 0) {
			return true;
		}
		foreach (string s in FilterByTestName) {
			if (s == testMethodName) {
				return true;
			}
		}
		return false;
	}
	
	private List<MethodInfo> ListTestMethods(Type type) {
		var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		var matching = new List<MethodInfo>();
		foreach (var method in allMethods) {
			var attrs = method.GetCustomAttributes(typeof(Test), false);
			if (attrs == null || attrs.Length == 0) continue;
			// Method matches, just check if it's not filtered out
			if (ShouldExecuteTest(method.Name)) {
				matching.Add(method);
			}
		}
		return matching;
	}

	// Called when a test completes
	private void OnTestCompleted(bool successful) {
//		Debug.Log("Completed test " + (PassedTests + 1));
		if (CurrentTestClassNo == -1) {
			Debug.LogError("Finished test with already having theoretically completed all tests. Your tests may be calling more than once TestBase.FailTest or so.");
			return;
		}
		if (successful) PassedTests += 1;
		else FailedTests.Add(CurrentClassMethods[CurrentTestMethodNo - 1].Name);
		TestDone.Set();
		IsRunningTestMethod = false;
	}

	// Makes the next test class being the one executed
	private void ProcessNextTestClass() {
		if (CurrentTestClassNo >= TestTypes.Length) {
			CurrentTestClassNo = -1;
			Common.Log("Tests completed. Passed: " + PassedTests + ", failed: " + FailedTests.Count + ", ignored: " + IgnoreTestNames.Length);
			foreach (string name in FailedTests) {
				Debug.Log("Failed test: " + name);
			}
			return;
		}

		var t = TestTypes[CurrentTestClassNo++];
		CurrentTestClassName = t.Name;
		CurrentClassTestComponent = (TestBase)gameObject.AddComponent(t);
		CurrentClassMethods = ListTestMethods(t);
		CurrentTestMethodNo = 0;
	}

	private void ProcessNextTestMethod() {
		// All methods of this class ran
		if (CurrentTestMethodNo >= CurrentClassMethods.Count) {
			ProcessNextTestClass();
 			return;
		}

		var method = CurrentClassMethods[CurrentTestMethodNo++];
		Common.Log("Running method " + CurrentTestClassName + "::" + method.Name);
		// Handle possible timeout
		TestDone.Reset();
		ThreadPool.RegisterWaitForSingleObject(TestDone, new WaitOrTimerCallback(TestTimedOut), null, TestTimeoutMillisec, true);
		// Run the actual method
		try {
			IsRunningTestMethod = true;
			CurrentClassTestComponent.RunTestMethodStandalone(method.Name);
		}
		catch (Exception ex) {
			TestBase.FailTest("Exception inside test: " + ex.ToString());
		}
	}

	// Called upon timeout.
	private void TestTimedOut(object state, bool timedOut) {
		if (timedOut) {
			Common.LogError("Test timed out!");
			OnTestCompleted(false);
		}
	}
}
