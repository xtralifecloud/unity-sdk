using System;

namespace CotcSdk {
	// TODO comment
	public class ResultTask<T> : CotcTask<Result<T>> {
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
	}

	// TODO comment
	public class ResultTask : CotcTask<Result<bool>> {
		public ResultTask PostResult(ErrorCode code, string reason = null) {
			PostResult(new Result<bool>(code, reason));
			return this;
		}
		internal ResultTask PostResult(HttpResponse response, string reason = null) {
			Result<bool> result = new Result<bool>(response);
			result.ErrorInformation = reason;
			PostResult(result);
			return this;
		}
		public ResultTask PostResult(Bundle serverData) {
			PostResult(new Result<bool>(true, serverData));
			return this;
		}
	}
}
