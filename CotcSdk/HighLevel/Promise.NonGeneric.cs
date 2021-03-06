using System;
using System.Collections;

namespace CotcSdk {
	/// <summary>An event arguments class holding an exception. Used with unhandled exceptions.</summary>
	public class ExceptionEventArgs : EventArgs {
		internal ExceptionEventArgs(Exception Exception) { this.Exception = Exception; }

		/// <summary>Gets event arguments' related exception. Used with unhandled exceptions.</summary>
		public Exception Exception {
			get;
			private set;
		}
	}

	/// <summary>Current state of the Promise. Should only be used for internal purposes.</summary>
	internal enum PromiseState {
		Pending,
		Fulfilled,
		Rejected
	}

	/// <summary>Holds two callback delegates to call in case of resolved (success) or rejected (failure) Promise.
	/// Should only be used for internal SDK conveniency.</summary>
	/// <param name="Callback">Delegate to call in case of resolved (success) Promise.</param>
	/// <param name="OnFailure">Delegate to call in case of rejected (failure) Promise.</param>
	internal class PromiseHandler {
		/// <summary>Delegate to call in case of resolved (success) Promise.</summary>
		public Action Callback;
		/// <summary>Delegate to call in case of rejected (failure) Promise.</summary>
		public Action<Exception> OnFailure;
	}

	public class Promise {
		private ArrayList ResolvedHandlers = new ArrayList(), RejectedHandlers = new ArrayList();
		private Exception RejectedValue;
		private PromiseState State = PromiseState.Pending;

		/// <summary>Catches a failure at that point in the chain.</summary>
		/// <param name="onRejected">Block handling the exception.</param>
		/// <returns>Another promise which is rejected under any circumstances: either for the same reason as this one (if
		/// the promise is caught further in the chain and this block executes well) or with another exception (if the
		/// onRejected block throws an exception). As such, it is highly recommended to provide a .Done() block after your
		/// Catch block, so that any exception in the catch body can be propagated to the unhandled exception handler.</returns>
		public Promise Catch(Action<Exception> onRejected) {
			var resultPromise = new Promise();
			Action resolveHandler = () => resultPromise.Resolve();
			Action<Exception> rejectHandler = ex => {
				onRejected(ex);
				resultPromise.Reject(ex);
			};
			ActionHandlers(resolveHandler, rejectHandler, ex => resultPromise.Reject(ex));
			return resultPromise;
		}

		/// <summary>Put this at the end of a promise chain. It ensures that unhandled exceptions can be delivered properly.</summary>
		/// <param name="onResolved">Execute upon success of all the chain steps.</param>
		/// <param name="onRejected">Execute upon rejection of the promise chain (any failure within the chain).</param>
		public void Done(Action onResolved, Action<Exception> onRejected) {
			var resultPromise = new Promise();
			ActionHandlers(onResolved, onRejected, ex => resultPromise.Reject(ex));
		}

		/// <summary>Put this at the end of a promise chain.</summary>
		/// <param name="onResolved">Execute upon success (as is, this is nearly equivalent to providing a simple Then block, except
		/// that you can not do further chain the promise. Therefore, it ensures that an exception not handled at that point will never
		/// be and allows unhandled exceptions to be delivered properly.</param>
		public void Done(Action onResolved) {
			var resultPromise = new Promise();
			ActionHandlers(onResolved, ex => Promise.PropagateUnhandledException(this, ex), ex => resultPromise.Reject(ex));
		}

		/// <summary>Put this at the end of a promise chain. It ensures that unhandled exceptions can be delivered properly.</summary>
		public void Done() {
			var resultPromise = new Promise();
			ActionHandlers(() => { }, ex => Promise.PropagateUnhandledException(this, ex), ex => resultPromise.Reject(ex));
		}

		/// <summary>Reject this promise (indicate that the process failed for some reason).</summary>
		/// <param name="ex">Exception to return as the failure result.</param>
		public void Reject(Exception ex) {
			if (State != PromiseState.Pending) throw new InvalidOperationException("Illegal promise state transition (" + State + " to Rejected)");
			RejectedValue = ex;
			State = PromiseState.Rejected;
			if (Debug_OutputAllExceptions) {
				Common.LogError("Rejected promise because of exception " + ex.ToString());
			}
			foreach (PromiseHandler<Exception> handler in RejectedHandlers) {
				InvokeHandler(handler.Callback, handler.OnFailure, ex);
			}
			RejectedHandlers.Clear();
		}

		/// <summary>Shorthand to create a promise that is already rejected.</summary>
		/// <param name="ex">Exception to reject the promise with.</param>
		/// <returns>A promise that is rejected right away.</returns>
		public static Promise Rejected(Exception ex) {
			Promise result = new Promise();
			result.Reject(ex);
			return result;
		}

		/// <summary>Resolves the promise, i.e. notifies a successful result of the async operation.</summary>
		public void Resolve() {
			if (State != PromiseState.Pending) throw new InvalidOperationException("Illegal promise state transition (" + State + " to Fulfilled)");
			State = PromiseState.Fulfilled;
			foreach (PromiseHandler handler in ResolvedHandlers) {
				InvokeHandler(handler.Callback, handler.OnFailure);
			}
			ResolvedHandlers.Clear();
		}

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a value promise (optionally converting to a different value type).
		/// </summary>
		/// <typeparam name="ConvertedT">Type of the expected result (it should be guessed automatically).</typeparam>
		/// <param name="onResolved">Executed upon successful result.</param>
		/// <returns>A new promise from another type.</returns>
		public Promise<ConvertedT> Then<ConvertedT>(Func<Promise<ConvertedT>> onResolved) {
			return Then(onResolved, null);
		}

		/// <summary>
		/// Registers a block of code to be executed when the promise returns a successful result.
		/// </summary>
		/// <param name="onResolved">Executed upon successful result.</param>
		/// <returns>A new promise to be used for chaining (you can Catch an exception that happened in the block for
		/// instance.</returns>
		public Promise Then(Action onResolved) {
			return Then(onResolved, null);
		}

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a non-value promise.
		/// </summary>
		/// <param name="onResolved">Executed upon successful result.</param>
		/// <param name="onRejected">Executed upon failure (promise rejected).</param>
		/// <returns>A promise that can be further chained.</returns>
		public Promise Then(Action onResolved, Action<Exception> onRejected) {
			var resultPromise = new Promise();
			Action resolveHandler = () => {
				if (onResolved != null) {
					onResolved();
				}
				resultPromise.Resolve();
			};
			Action<Exception> rejectHandler = ex => {
				if (onRejected != null) {
					onRejected(ex);
				}
				resultPromise.Reject(ex);
			};
			ActionHandlers(resolveHandler, rejectHandler, ex => resultPromise.Reject(ex));
			return resultPromise;
		}

		/// <summary>
		/// Registers a block of code to be executed when the promise returns a successful result.
		/// </summary>
		/// <param name="onResolved">Executed upon successful result.</param>
		/// <returns>A new promise to be used for chaining (you can Catch an exception that happened in the block for
		/// instance.</returns>
		public Promise Then(Func<Promise> onResolved) {
			return Then(onResolved, null);
		}

		/// <summary>
		/// Add a resolved callback and a rejected callback.
		/// The resolved callback chains a non-value promise.
		/// </summary>
		/// <param name="onResolved">Executed upon successful result.</param>
		/// <param name="onRejected">Executed upon failure (promise rejected).</param>
		/// <returns>A promise that can be further chained.</returns>
		public Promise Then(Func<Promise> onResolved, Action<Exception> onRejected) {
			var resultPromise = new Promise();
			Action resolveHandler = () => {
				if (onResolved != null) {
					onResolved()
						.Then(
							() => resultPromise.Resolve(),
							ex => resultPromise.Reject(ex)
						)
						.Done();
				}
				else {
					resultPromise.Resolve();
				}
			};
			Action<Exception> rejectHandler = ex => {
				if (onRejected != null) {
					onRejected(ex);
				}
				resultPromise.Reject(ex);
			};
			ActionHandlers(resolveHandler, rejectHandler, ex => resultPromise.Reject(ex));
			return resultPromise;
		}

		/// <summary>
		/// Adds a resolved and rejected callback. Allows for chaining (i.e. return another promise, potentially of another type,
		/// indicating an operation to be waited for). Example:
		/// @code{.cs}
		/// Promise<int> longIntOperation();
		/// Promise<bool> longBoolOperation();
		/// longIntOperation()
		/// .Then((int result) => {
		///     Debug.Log("Result of longIntOperation: " + result");
		///     return longBoolOperation();
		/// })
		/// .Then((bool result) => {
		///     Debug.Log("Result of longBoolOperation: " + result");
		/// })
		/// .Catch((Exception ex) => {
		///     Debug.LogError("Any of the two operations has failed: " + ex.ToString());
		/// }).Done(); @endcode
		/// </summary>
		/// <typeparam name="ConvertedT">Type of the returned promise (should be guessed automatically).</typeparam>
		/// <param name="onResolved">Executed upon successful result.</param>
		/// <param name="onRejected">Executed upon failure (promise rejected).</param>
		/// <returns>A new promise that can be chained as shown in the summary.</returns>
		public Promise<ConvertedT> Then<ConvertedT>(Func<Promise<ConvertedT>> onResolved, Action<Exception> onRejected) {
			// This version of the function must supply an onResolved.
			// Otherwise there is now way to get the converted value to pass to the resulting promise.
			var resultPromise = new Promise<ConvertedT>();
			Action resolveHandler = () => {
				onResolved()
					.Then(
						(ConvertedT chainedValue) => resultPromise.Resolve(chainedValue),
						ex => resultPromise.Reject(ex)
					)
					.Done();
			};

			Action<Exception> rejectHandler = ex => {
				if (onRejected != null) {
					onRejected(ex);
				}
				resultPromise.Reject(ex);
			};

			ActionHandlers(resolveHandler, rejectHandler, ex => resultPromise.Reject(ex));
			return resultPromise;
		}

		private void ActionHandlers(Action onResolved, Action<Exception> onRejected, Action<Exception> onRejectResolved) {
			if (State == PromiseState.Pending) {
				if (onResolved != null) {
					ResolvedHandlers.Add(new PromiseHandler() {
						Callback = onResolved,
						OnFailure = onRejectResolved
					});
				}
				if (onRejected != null) {
					RejectedHandlers.Add(new PromiseHandler<Exception>() {
						Callback = onRejected,
						OnFailure = onRejectResolved
					});
				}
			}
			else if (State == PromiseState.Fulfilled) {
				InvokeHandler(onResolved, onRejectResolved);
			}
			else if (State == PromiseState.Rejected) {
				InvokeHandler(onRejected, onRejectResolved, RejectedValue);
			}
		}

		private void InvokeHandler(Action callback, Action<Exception> onFailure) {
			try {
				callback();
			}
			catch (Exception ex) {
				if (Debug_OutputAllExceptions) {
					Common.LogError("Exception in promise Then/Done block: " + ex.ToString());
				}
				onFailure(ex);
			}
		}

		private void InvokeHandler<T>(Action<T> callback, Action<Exception> onFailure, T value) {
			try {
				callback(value);
			}
			catch (Exception ex) {
				if (Debug_OutputAllExceptions) {
					Common.LogError("Exception in promise Catch block: " + ex.ToString());
				}
				onFailure(ex);
			}
		}

		/// <summary>Gets some Promise's stats as a human readable string, like its state and its
		/// resolved/rejected callbacks counts.</summary>
		/// <returns>Some Promise's stats as a human readable string.</returns>
		public override string ToString() {
			return base.ToString() + ", State: " + State.ToString() + ", Res: " + ResolvedHandlers.Count + ", Rej: " + RejectedHandlers.Count;
		}

		private static EventHandler<ExceptionEventArgs> unhandledException;

		/// <summary>
		/// Event raised for unhandled errors.
		/// For this to work you have to complete your promises with a call to Done().
		/// </summary>
		public static event EventHandler<ExceptionEventArgs> UnhandledException {
			add { unhandledException += value; }
			remove { unhandledException -= value; }
		}

		/// <summary>
		/// Set this to true in order to output any exception to the console, even if it is caught by a .Catch block.
		/// Just a helper, never keep it true in production.
		/// </summary>
		public static bool Debug_OutputAllExceptions = true;

		internal static void PropagateUnhandledException(object sender, Exception ex) {
			if (unhandledException != null) {
				unhandledException(sender, new ExceptionEventArgs(ex));
			}
		}
	}
}
