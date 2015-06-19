using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CotcSdk {

	public class CotcTask<T> {

		private List<Func<T, CotcTask<T>>> Pending = new List<Func<T, CotcTask<T>>>();
		private bool AlreadyReturned;
		private T Result;

		public CotcTask<T> ForwardTo(CotcTask<T> otherTask) {
			return Then(r => otherTask.PostResult(r));
		}
		public CotcTask<T> ForwardTo(Action<T> handler) {
			return Then(r => handler(r));
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private void HandlePending(T result) {
			for (int i = 0; i < Pending.Count; ) {
				CotcTask<T> after = Pending[i](result);
				Pending.RemoveAt(i);
				if (after != null) {
					after.Then(r => { HandlePending(r); return null; });
					break;
				}
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void PostResult(T result) {
			if (AlreadyReturned) throw new ArgumentException("Returned multiple times to CotcTask");
			HandlePending(result);
			AlreadyReturned = true;
			Result = result;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public CotcTask<T> Then(Func<T, CotcTask<T>> action) {
			if (AlreadyReturned) {
				var next = action(Result);
				return next ?? this;
			}
			else {
				Pending.Add(action);
			}
			return this;
		}

		public CotcTask<T> Then(Action<T, CotcTask<T>> action) {
			CotcTask<T> p = new CotcTask<T>();
			return Then(result => {
				action(result, p);
				return p;
			});
		}

		public CotcTask<T> Then(Action<T> action) {
			return Then(result => {
				action(result);
				return null;
			});
		}
	}

	public class ResultTask<T> : CotcTask<Result<T>> {
		public void PostResult(ErrorCode code, string reason = null) {
			PostResult(new Result<T>(code, reason));
		}
		internal void PostResult(HttpResponse response, string reason = null) {
			Result<T> result = new Result<T>(response);
			result.ErrorInformation = reason;
			PostResult(result);
		}
		public void PostResult(T value, Bundle serverData) {
			PostResult(new Result<T>(value, serverData));
		}
	}
}
