
namespace CotcSdk {
	
	/// <summary>Allows to run batches authenticated as a user.</summary>
	public class GamerBatches {

		/// <summary>
		/// Changes the domain affected by the next operations.
		/// You should typically use it this way: `gamer.Batches.Domain("private").Run(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the next operations.</param>
		/// <returns>This object for operation chaining.</returns>
		public GamerBatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>Runs a batch on the server, authenticated as a gamer (gamer-scoped).</summary>
		/// <returns>Promise resolved when the operation has completed. The attached bundle is the JSON data returned
		///     by the batch.</returns>
		/// <param name="batchName">Name of the batch to run, as configured on the server.</param>
		/// <param name="batchParams">Parameters to be passed to the batch.</param>
		public Promise<Bundle> Run(string batchName, Bundle batchParams = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/batch").Path(domain).Path(batchName);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = batchParams ?? Bundle.Empty;
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson);
			});
		}

		#region Private
		internal GamerBatches(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
