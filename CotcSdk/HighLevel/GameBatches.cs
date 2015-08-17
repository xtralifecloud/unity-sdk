
namespace CotcSdk {
	
	/**
	 * Allows to run batches authenticated as a game (that is, unauthenticated).
	 */
	public class GameBatches {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Batches.Domain("private").Run(...);`
		 * @param domain domain on which to scope the next operations.
		 * @return this object for operation chaining.
		 */
		public GameBatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Runs a batch on the server, unauthenticated (game-scoped).
		 * @return promise resolved when the request has finished. The attached bundle is the JSON data returned by the match.
		 * @param batchName name of the batch to run, as configured on the server.
		 * @param batchParams parameters to be passed to the batch.
		 */
		public Promise<Bundle> Run(string batchName, Bundle batchParams = null) {
			UrlBuilder url = new UrlBuilder("/v1/batch").Path(domain).Path(batchName);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			req.BodyJson = batchParams ?? Bundle.Empty;
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson);
			});
		}

		#region Private
		internal GameBatches(Cloud parent) {
			Cloud = parent;
		}
		private string domain = Common.PrivateDomain;
		private Cloud Cloud;
		#endregion
	}
}
