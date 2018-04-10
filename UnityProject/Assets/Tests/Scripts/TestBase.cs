using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using CotcSdk;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;

/**
 * Base for an integration test class.
 * Usage is typically as follow: @code
	public class TestCategory: TestBase {
		[InstanceMethod(typeof(TestCategory))]
		public string TestMethodName;

		// Test boostrap code (run for each test)
		void Start() {
			RunTestMethod(TestMethodName);
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
	// Set to true if you plan to instantiate tests using AddComponent (bypasses the classic integration test mechanisms)
	internal static bool DoNotRunMethodsAutomatically = false;
	// Called whenever a test finishes with a boolean value indicating success
	internal static event Action<bool> OnTestCompleted;
	// Set this to false to prevent default behaviour and test Promise.UnhandledException
	internal static bool FailOnUnhandledException = true;
	private List<string> PendingSignals = new List<string>();
	private Dictionary<string, Action> RegisteredSlots = new Dictionary<string, Action>();
    protected static Cloud cloud;
    protected static bool testIsRunning;

    [OneTimeSetUp]
    public void Init() {
        Promise.Debug_OutputAllExceptions = false;
        if (cloud == null) {
			GameObject instance = (GameObject)Instantiate(Resources.Load("Prefabs/CotcSdk-UnitTests"));
            instance.GetComponent<CotcGameObject>().GetCloud().Done(cloud_ => {
                cloud = cloud_;
            });
        }
    }

    [SetUp]
    public void Setup() {
        testIsRunning = true;
    }

    protected IEnumerator WaitForEndOfTest() {
        while (testIsRunning)
            yield return null;
    }

    protected void Assert(bool condition, string message) {
		if (!condition) {
			Debug.LogError("Test interrupted because of failed assertion: " + message);
		}
	}

    public static void CompleteTest() {
        testIsRunning = false;
    }

    public static void FailTest(string reason) {
        testIsRunning = false;
        Debug.LogError("Test failed: " + reason);
    }

    /*  ======================================
     *   Old CompleteTest and FailTest method
     *  ======================================
     *
    public static void CompleteTest() {
        if (!DoNotRunMethodsAutomatically) IntegrationTest.Pass();
        if (OnTestCompleted != null) OnTestCompleted(true);
    }

    public static void FailTest(string reason) {
        if (!DoNotRunMethodsAutomatically) {
            IntegrationTest.Fail(reason);
        }
        else {
            Common.LogError("Test failed: " + reason);
        }
        if (OnTestCompleted != null) OnTestCompleted(false);
    }
    *
    * 
    */

    // For use in test classes themselves (Start method)
    protected void RunTestMethod(string testMethodName) {
		// Fail test on unhandled exception
		Promise.UnhandledException += (sender, e) => {
			if (FailOnUnhandledException) {
				TestBase.FailTest("Unhandled exception in test: " + e.Exception);
			}
		};
		if (DoNotRunMethodsAutomatically) return;
		RunTestMethodStandalone(testMethodName);
	}

	// For use by an external test runner
	internal void RunTestMethodStandalone(string testMethodName) {
		var met = GetType().GetMethod(testMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (met == null) {
			TestBase.FailTest("Method name not configured for this test!");
			return;
		}

		// To prevent causing auto tests failure because of SDK's error logs (e.g. we may expect a "missing score" server error without test failure)
        Promise.Debug_OutputAllExceptions = false;

        var parms = met.GetParameters();
		// Test methods can either have no param, either have one "Cloud" param, in which case we do the setup here to simplify
		if (parms.Length >= 1 && parms[0].ParameterType == typeof(Cloud)) {
			FindObjectOfType<CotcGameObject>().GetCloud().Done(cloud => {
				met.Invoke(this, new object[] { cloud });
			});
		}
		else {
			met.Invoke(this, null);
		}
	}

	protected string GetAllTestScopedId(string prefix) {
		return FindObjectOfType<TestUtilities>().GetAllTestScopedId(prefix);
	}

	/**
	 * Executes a callback for each page. Return true from the callback to stop, return false to get the callback executed again with the next page when available.
	 * 
	 * The returned promise can be used to catch any error that would happen meanwhile. It returns true if a page did return true, or false if all pages have been
	 * visited and no callback ever returned true.
	 */
	protected Promise<bool> ProcessAllPages<T>(PagedList<T> initialList, Func<PagedList<T>, bool> forEachPage) {
		Promise<bool> result = new Promise<bool>();
		var thenHandlerRef = new Action<PagedList<T>>[1];
		Action<PagedList<T>> thenHandler = list => {
			bool shouldStop = forEachPage(list);
			if (shouldStop) {
				result.Resolve(true);
				return;
			}
			if (!list.HasNext) {
				result.Resolve(false);
				return;
			}
			list.FetchNext().Then(thenHandlerRef[0]);
		};
		thenHandlerRef[0] = thenHandler;
		thenHandler(initialList);
		return result;
	}

	protected void Login(Cloud cloud, Action<Gamer> done) {
        cloud.Login(
            network: LoginNetwork.Email.Describe(),
            networkId: "cloud@localhost.localdomain",
            networkSecret: "Password123")
        .ExpectSuccess(gamer => {
            done(gamer);
        });
	}

	protected void Login2Users(Cloud cloud, Action<Gamer, Gamer> done) {
		Login(cloud, gamer1 => {
            // Second user
            cloud.Login(
                network: LoginNetwork.Email.Describe(),
                networkId: "clan2@localhost.localdomain",
                networkSecret: "Password123")
            .ExpectSuccess(gamer2 => {
                done(gamer1, gamer2);
            });
		});
	}

	protected void LoginNewUser(Cloud cloud, Action<Gamer> done) {
        cloud.LoginAnonymously()
        .ExpectSuccess(gamer => { done(gamer); });
	}

	protected void Login2NewUsers(Cloud cloud, Action<Gamer, Gamer> done) {
        cloud.LoginAnonymously().ExpectSuccess(gamer1 => {
            // Second user
            return cloud.LoginAnonymously().ExpectSuccess(gamer2 => {
                done(gamer1, gamer2);
            });
        });
	}

	protected string RandomEmailAddress() {
		string randomPart = Guid.NewGuid().ToString();
		return string.Format("test{0}@localhost.localdomain", randomPart);
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

	protected Promise Wait(int millisec) {
		Promise p = new Promise();
		new Thread(new ThreadStart(() => {
			Thread.Sleep(millisec);
			Cotc.RunOnMainThread(p.Resolve);
		})).Start();
		return p;
	}

	protected Promise<T> Wait<T>(int millisec) {
		Promise<T> p = new Promise<T>();
		new Thread(new ThreadStart(() => {
			Thread.Sleep(millisec);
			Cotc.RunOnMainThread(() => p.Resolve(default(T)));
		})).Start();
		return p;
	}
}
