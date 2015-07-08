using System;
using CotcSdk;
using UnityEngine;

public static class TestPromiseExtensions {
	/**
	 * Call this on a promise to close a test. If you do not do it, you will need to call CompleteTest() on
	 * your current test class.
	 */
	public static void CompleteTestIfSuccessful<T>(this IPromise<T> p) {
		p.Catch(ex => TestBase.FailTest("Test failed: " + ex.ToString()))
		.Done(result => TestBase.CompleteTest());
	}

	public static IPromise<T> ExpectSuccess<T>(this IPromise<T> p, Action<T> action = null) {
		return p.Catch(ex => TestBase.FailTest("Test failed: " + ex.ToString()))
		.Then((T result) => {
			try {
				if (action != null) action(result);
			}
			catch (Exception ex) {
				TestBase.FailTest("Test failed because of error in ExpectSuccess body: " + ex.ToString());
			}
		});
	}

	public static IPromise<U> ExpectSuccess<T, U>(this IPromise<T> p, Func<T, IPromise<U>> action) {
		return p.Catch(ex => TestBase.FailTest("Test failed: " + ex.ToString()))
		.Then<U>((T result) => {
			try {
				return action(result);
			}
			catch (Exception ex) {
				TestBase.FailTest("Test failed because of error in ExpectSuccess body: " + ex.ToString());
				return Promise<U>.Rejected(ex);
			}
		});
	}

	public static IPromise<T> ExpectFailure<T>(this IPromise<T> p, Action<CotcException> action = null) {
		return p.Then(value => TestBase.FailTest("Test failed: value should not be returned"))
		.Catch(ex => {
			if (action != null) {
				if (ex is CotcException)
					action((CotcException)ex);
				else
					TestBase.FailTest("Exception not of type CotcException: " + ex);
			}
		});
	}
}
