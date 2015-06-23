using System;

namespace CotcSdk {
	// TODO remove
	public class ResultTask<T> : Promise<T> {

		public ResultTask<T> PostResult(ErrorCode code, string reason) {
			Reject(new CotcException(code, reason));
			return this;
		}
		internal ResultTask<T> PostResult(HttpResponse response, string reason) {
			CotcException result = new CotcException(response);
			result.ErrorInformation = reason;
			Reject(result);
			return this;
		}
		internal ResultTask<T> PostResult(T value, Bundle serverData, HttpResponse response = null) {
			Resolve(value);
			return this;
		}
	}
}
