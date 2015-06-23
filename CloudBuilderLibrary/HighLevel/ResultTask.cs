using System;

namespace CotcSdk {
	// TODO remove
	public class ResultTask<T> : Promise<T> {
		public ResultTask<T> ForwardTo(ResultTask<T> otherTask) {
			return (ResultTask<T>)
				Then(result => otherTask.Resolve(result))
				.Catch(ex => otherTask.Reject(ex));
		}

		public ResultTask<T> PostResult(ErrorCode code, string reason) {
			Reject(new CotcException(code, reason));
			return this;
		}
		public ResultTask<T> PostResult(T value, Bundle serverData) {
			Resolve(value);
			return this;
		}
		internal ResultTask<T> PostResult(HttpResponse response, string reason) {
			CotcException result = new CotcException(response);
			result.ErrorInformation = reason;
			Reject(result);
			return this;
		}
		internal ResultTask<T> PostResult(T value, Bundle serverData, HttpResponse response) {
			Resolve(value);
			return this;
		}
	}
}
