using System;
using System.Collections;

namespace CotcSdk {
	public class ExceptionEventArgs : EventArgs {
		internal ExceptionEventArgs(Exception Exception) {
			this.Exception = Exception;
		}

		public Exception Exception {
			get;
			private set;
		}
	}

	internal enum PromiseState {
		Pending,
		Fulfilled,
		Rejected
	}

	internal class PromiseHandler {
		public Action Callback;
		public Action<Exception> OnFailure;
	}

	public class Promise {
		private ArrayList ResolvedHandlers = new ArrayList(), RejectedHandlers = new ArrayList();
		private Exception RejectedValue;
		private PromiseState State = PromiseState.Pending;

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

		public void Done(Action onResolved, Action<Exception> onRejected) {
			var resultPromise = new Promise();
			ActionHandlers(onResolved, onRejected, ex => resultPromise.Reject(ex));
		}

		public void Done(Action onResolved) {
			var resultPromise = new Promise();
			ActionHandlers(onResolved, ex => Promise.PropagateUnhandledException(this, ex), ex => resultPromise.Reject(ex));
		}

		public void Done() {
			var resultPromise = new Promise();
			ActionHandlers(() => { }, ex => Promise.PropagateUnhandledException(this, ex), ex => resultPromise.Reject(ex));
		}

		public void Reject(Exception ex) {
			if (State != PromiseState.Pending) throw new InvalidOperationException("Illegal promise state transition");
			RejectedValue = ex;
			State = PromiseState.Rejected;
			foreach (PromiseHandler<Exception> handler in RejectedHandlers) {
				InvokeHandler(handler.Callback, handler.OnFailure, ex);
			}
			RejectedHandlers.Clear();
		}

		public void Resolve() {
			if (State != PromiseState.Pending) throw new InvalidOperationException("Illegal promise state transition");
			State = PromiseState.Fulfilled;
			foreach (PromiseHandler handler in ResolvedHandlers) {
				InvokeHandler(handler.Callback, handler.OnFailure);
			}
			ResolvedHandlers.Clear();
		}

		public Promise<ConvertedT> Then<ConvertedT>(Func<Promise<ConvertedT>> onResolved) {
			return Then(onResolved, null);
		}

		public Promise Then(Action onResolved) {
			return Then(onResolved, null);
		}

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

		public Promise Then(Func<Promise> onResolved) {
			return Then(onResolved, null);
		}

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
				onFailure(ex);
			}
		}

		private void InvokeHandler<T>(Action<T> callback, Action<Exception> onFailure, T value) {
			try {
				callback(value);
			}
			catch (Exception ex) {
				onFailure(ex);
			}
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

		internal static void PropagateUnhandledException(object sender, Exception ex) {
			if (unhandledException != null) {
				unhandledException(sender, new ExceptionEventArgs(ex));
			}
		}
	}
}
