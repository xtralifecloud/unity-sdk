using System;

namespace CotcSdk {
	
	/**
	 * Allows to run batches authenticated as a user.
	 */
	public class GamerBatches {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Batches.Domain("private").Run(...);`
		 * @param domain domain on which to scope the next operations.
		 */
		public GamerBatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Runs a batch on the server, authenticated as a gamer (gamer-scoped).
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     is the JSON data returned by the match.
		 * @param batchName name of the batch to run, as configured on the server.
		 * @param batchParams parameters to be passed to the batch.
		 */
		public IPromise<Bundle> Run(string batchName, Bundle batchParams = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/batch").Path(domain).Path(batchName);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = batchParams ?? Bundle.Empty;
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson, response.BodyJson);
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
