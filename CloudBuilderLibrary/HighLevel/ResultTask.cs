using System;

namespace CotcSdk {
	// TODO comment
	public class ResultTask<T> : CotcTask<Result<T>> {
		public ResultTask<T> OnFailure(Func<Result<T>, ResultTask<T>> action) {
			return (ResultTask<T>)Then(result => {
				if (!result.IsSuccessful) return action(result);
				return null;
			});
		}
		public ResultTask<T> OnSuccess(Func<Result<T>, ResultTask<T>> action) {
			return (ResultTask<T>)Then(result => {
				if (result.IsSuccessful) return action(result);
				return null;
			});
		}

		public ResultTask<T> PostResult(ErrorCode code, string reason = null) {
			PostResult(new Result<T>(code, reason));
			return this;
		}
		internal ResultTask<T> PostResult(HttpResponse response, string reason = null) {
			Result<T> result = new Result<T>(response);
			result.ErrorInformation = reason;
			PostResult(result);
			return this;
		}
		public ResultTask<T> PostResult(T value, Bundle serverData) {
			PostResult(new Result<T>(value, serverData));
			return this;
		}
		public new ResultTask<T> PostResult(Result<T> result) {
			return (ResultTask<T>) base.PostResult(result);
		}
	}
}
