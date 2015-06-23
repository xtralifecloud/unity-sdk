using System;
using CotcSdk;

public static class TestPromiseExtensions {
	/**
	 * Call this on a promise to close a test. If you do not do it, you will need to call CompleteTest() on
	 * your current test class.
	 */
	public static void CompleteTestPromise<T>(this IPromise<T> p) {
		p.Catch(ex => IntegrationTest.Fail("Test failed: " + ex.ToString()))
		.Done(result => IntegrationTest.Pass());
	}

	public static IPromise<T> ExpectSuccess<T>(this IPromise<T> p, Action<T> action = null) {
		return p.Catch(ex => IntegrationTest.Fail("Test failed: " + ex.ToString()))
		.Then(result => {
			if (action != null) action(result);
		});
	}

	public static IPromise<T> ExpectFailure<T>(this IPromise<T> p, Action<Exception> action = null) {
		return p.Then(value => IntegrationTest.Fail("Test failed: value should not be returned"))
		.Catch(ex => {
			if (action != null) action(ex);
		});
	}
}
