using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace CotcSdk
{
	internal class PromiseHandler<T> {
		public Action<T> Callback;
		public Action<Exception> OnFailure;
	}

	public class Promise<PromisedT> {
		private ArrayList ResolvedHandlers = new ArrayList(), RejectedHandlers = new ArrayList();
		private PromisedT ResolvedValue;
		private Exception RejectedValue;
		private PromiseState State = PromiseState.Pending;

		public Promise<PromisedT> Catch(Action<Exception> onRejected) {
			var resultPromise = new Promise<PromisedT>();
			Action<PromisedT> resolveHandler = v => {
				resultPromise.Resolve(v);
			};
			Action<Exception> rejectHandler = ex => {
				onRejected(ex);
				resultPromise.Reject(ex);
			};
			ActionHandlers(resolveHandler, rejectHandler, ex => resultPromise.Reject(ex));
			return resultPromise;
		}

		public void Done(Action<PromisedT> onResolved, Action<Exception> onRejected) {
			var resultPromise = new Promise<PromisedT>();
			ActionHandlers(onResolved, onRejected, ex => resultPromise.Reject(ex));
		}

		public void Done(Action<PromisedT> onResolved) {
			var resultPromise = new Promise<PromisedT>();
			ActionHandlers(onResolved, ex => Promise.PropagateUnhandledException(this, ex), ex => resultPromise.Reject(ex));
		}

		public void Done() {
			var resultPromise = new Promise<PromisedT>();
			ActionHandlers(value => { }, ex => Promise.PropagateUnhandledException(this, ex), ex => resultPromise.Reject(ex));
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

		public void Resolve(PromisedT value) {
			if (State != PromiseState.Pending) throw new InvalidOperationException("Illegal promise state transition");
			ResolvedValue = value;
			State = PromiseState.Fulfilled;
			foreach (PromiseHandler<PromisedT> handler in ResolvedHandlers) {
				InvokeHandler(handler.Callback, handler.OnFailure, value);
			}
			ResolvedHandlers.Clear();
		}

		public Promise<ConvertedT> Then<ConvertedT>(Func<PromisedT, Promise<ConvertedT>> onResolved) {
			return Then(onResolved, null);
		}

		public Promise<PromisedT> Then(Action<PromisedT> onResolved) {
			return Then(onResolved, null);
		}

		public Promise<PromisedT> Then(Action<PromisedT> onResolved, Action<Exception> onRejected) {
			var resultPromise = new Promise<PromisedT>();
			Action<PromisedT> resolveHandler = v => {
				if (onResolved != null) {
					onResolved(v);
				}
				resultPromise.Resolve(v);
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

		public Promise<ConvertedT> Then<ConvertedT>(Func<PromisedT, Promise<ConvertedT>> onResolved, Action<Exception> onRejected) {
			// This version of the function must supply an onResolved.
			// Otherwise there is now way to get the converted value to pass to the resulting promise.
			var resultPromise = new Promise<ConvertedT>();
			Action<PromisedT> resolveHandler = v => {
				onResolved(v)
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

		private void ActionHandlers(Action<PromisedT> onResolved, Action<Exception> onRejected, Action<Exception> onRejectResolved) {
			if (State == PromiseState.Pending) {
				if (onResolved != null) {
					ResolvedHandlers.Add(new PromiseHandler<PromisedT>() {
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
				InvokeHandler(onResolved, onRejectResolved, ResolvedValue);
			}
			else if (State == PromiseState.Rejected) {
				InvokeHandler(onRejected, onRejectResolved, RejectedValue);
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
	}
}
