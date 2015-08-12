using System;
using System.Collections;
using CotcSdk;

class CountedPromise<T> {
	private bool AlreadyResolved;
	private Promise<T> Promise;
	private int RequiredCount;
	public ArrayList Results { get; private set; }

	public CountedPromise(int requiredCount) {
		RequiredCount = requiredCount;
		Promise = new Promise<T>();
		Results = new ArrayList();
	}

	public void Resolve(T result) {
		if (AlreadyResolved) return;
		if (--RequiredCount == 0) {
			Results.Add(result);
			AlreadyResolved = true;
			Promise.Resolve(result);
		}
	}

	public void Reject(Exception ex) {
		if (!AlreadyResolved) return;
		AlreadyResolved = true;
		Promise.Reject(ex);
	}

	public Promise<T> WhenCompleted {
		get { return Promise; }
	}
}
