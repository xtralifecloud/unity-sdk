using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/**
 * Represents an asynchronous operation. Basically it allows to start an asynchronous process and wait for a callback
 * from the calling thread. Let's consider this: @code
	AsyncOp<int> op = new AsyncOp<int>();
	new Thread(new ThreadStart(() => {
		// Simulate lengthy operation
		Thread.Sleep(1000);
		op.Return(123);
	});
	op.Then(result => {
		Debug.Log("Operation completed with result: " + result); // 123
	}); @endcode
 * 
 * Basically, you subscribe to the operation and your callback is called when it returns a value. This class is
 * "race-condition-safe". You can even subscribe to an operation after it has already returned. In this case your
 * callback is simply called immediately with the result that was posted.
 * 
 * You may post only one result for a given operation. However you may chain multiple operations by requesting an
 * additional `AsyncOp` argument in your `Then` callback. This will block the event chain until the operation in
 * question has been completed, as shown below: @code
	AsyncOp<int> operation = new AsyncOp<int>();
	operation.Then((int result, AsyncOp<int> op) => {
		new Thread(new ThreadStart(() => {
			// Simulate lengthy operation
			Thread.Sleep(1000);
			op.Return(123);
		});
	})
	.Then((int result) => {
		// Will be exected when the "op" of the previous block returns
		// Do not request an additional operation, the chain will stop here
		Debug.Log("Result: " + result); // 123
	})
	.Then((int result) => {
		// This block will be executed at the same time as the previous one since
		// the previous one doesn't have any pending operation (no "op" arg).
	})
	.Return(0); // Start the chain (the first Then executes) @endcode
 */
public class AsyncOp<T> {
	private List<Func<T, AsyncOp<T>>> Pending = new List<Func<T, AsyncOp<T>>>();
	private bool AlreadyReturned;
	private T Result;

	[MethodImpl(MethodImplOptions.Synchronized)]
	private void HandlePending(T result) {
		for (int i = 0; i < Pending.Count; ) {
			AsyncOp<T> after = Pending[i](result);
			Pending.RemoveAt(i);
			if (after != null) {
				after.Then(r => { HandlePending(r); return null; });
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public void Return(T result) {
		if (AlreadyReturned) throw new ArgumentException("Returned multiple times to AsyncOp");
		HandlePending(result);
		AlreadyReturned = true;
		Result = result;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public AsyncOp<T> Then(Func<T, AsyncOp<T>> action) {
		if (AlreadyReturned) {
			var next = action(Result);
			return next ?? this;
		}
		else {
			Pending.Add(action);
		}
		return this;
	}

	public AsyncOp<T> Then(Action<T, AsyncOp<T>> action) {
		AsyncOp<T> p = new AsyncOp<T>();
		return Then(result => {
			action(result, p);
			return p;
		});
	}

	public AsyncOp<T> Then(Action<T> action) {
		return Then(result => {
			action(result);
			return null;
		});
	}
}

/**
 * Same, without argument (result).
 */
public class AsyncOp {
	private List<Func<AsyncOp>> Pending = new List<Func<AsyncOp>>();
	private bool AlreadyReturned;

	[MethodImpl(MethodImplOptions.Synchronized)]
	private void HandlePending() {
		for (int i = 0; i < Pending.Count; ) {
			AsyncOp after = Pending[i]();
			Pending.RemoveAt(i);
			if (after != null) {
				after.Then(() => { HandlePending(); return null; });
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public void Return() {
		if (AlreadyReturned) throw new ArgumentException("Returned multiple times to AsyncOp");
		HandlePending();
		AlreadyReturned = true;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public AsyncOp Then(Func<AsyncOp> action) {
		if (AlreadyReturned) {
			var next = action();
			return next ?? this;
		}
		else {
			Pending.Add(action);
		}
		return this;
	}

	public AsyncOp Then(Action<AsyncOp> action) {
		AsyncOp p = new AsyncOp();
		return Then(() => {
			action(p);
			return p;
		});
	}

	public AsyncOp Then(Action action) {
		return Then(() => {
			action();
			return null;
		});
	}
}

