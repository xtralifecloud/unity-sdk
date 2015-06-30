using System;

namespace CotcSdk {

	public static class PromiseExtensions {
		public static IPromise<T> ForwardTo<T>(this IPromise<T> promise, Promise<T> otherTask) {
			return promise.Then(result => otherTask.Resolve(result))
				.Catch(ex => otherTask.Reject(ex));
		}

		public static IPromise<T> PostResult<T>(this Promise<T> promise, ErrorCode code, string reason) {
			promise.Reject(new CotcException(code, reason));
			return promise;
		}
		public static IPromise<T> PostResult<T>(this Promise<T> promise, T value, Bundle serverData) {
			promise.Resolve(value);
			return promise;
		}
		internal static IPromise<T> PostResult<T>(this Promise<T> promise, HttpResponse response, string reason) {
			CotcException result = new CotcException(response);
			result.ErrorInformation = reason;
			promise.Reject(result);
			return promise;
		}
		internal static IPromise<T> PostResult<T>(this Promise<T> promise, T value, Bundle serverData, HttpResponse response) {
			promise.Resolve(value);
			return promise;
		}
	}
}
