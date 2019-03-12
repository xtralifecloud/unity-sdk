
namespace CotcSdk {

	/// <summary>Promise class extensions.</summary>
	public static class PromiseExtensions {
		/// <summary>Makes Promise's resolving/rejecting result replace another Promise's one.</summary>
		/// <param name="otherTask">The other Promise to which to pass this Promise's result.</param>
		public static Promise<T> ForwardTo<T>(this Promise<T> promise, Promise<T> otherTask) {
			return promise.Then(delegate(T result) { otherTask.Resolve(result); })
				.Catch(ex => otherTask.Reject(ex));
		}

		/// <summary>Rejects a promise as a failure.</summary>
		/// <param name="code">Internal code of the error which occured.</param>
		/// <param name="reason">Error message to describe why the Promise has been rejected.</param>
		public static Promise<T> PostResult<T>(this Promise<T> promise, ErrorCode code, string reason) {
			promise.Reject(new CotcException(code, reason));
			return promise;
		}

		/// <summary>Resolves a promise as a success.</summary>
		/// <param name="value">The obtained promised value thanks to its success.</param>
		public static Promise<T> PostResult<T>(this Promise<T> promise, T value) {
			promise.Resolve(value);
			return promise;
		}

		/// <summary>Rejects a HTTP promise as a failure.</summary>
		/// <param name="response">HttpResponse to get some info about the request.</param>
		/// <param name="reason">Error message to describe why the Promise has been rejected.</param>
		internal static Promise<T> PostResult<T>(this Promise<T> promise, HttpResponse response, string reason) {
			CotcException result = new CotcException(response);
			result.ErrorInformation = reason;
			promise.Reject(result);
			return promise;
		}

		/// <summary>Resolves a HTTP promise as a success.</summary>
		/// <param name="value">The obtained promised value thanks to its success.</param>
		/// <param name="serverData">Additional data Bundle sent by the server.</param>
		/// <param name="response">HttpResponse to get some info about the request.</param>
		internal static Promise<T> PostResult<T>(this Promise<T> promise, T value, Bundle serverData, HttpResponse response) {
			promise.Resolve(value);
			return promise;
		}
	}
}
