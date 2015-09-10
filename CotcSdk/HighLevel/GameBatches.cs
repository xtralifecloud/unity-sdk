
namespace CotcSdk {

	/// @ingroup game_classes
	/// <summary>
	/// Allows to run batches authenticated as a game (that is, unauthenticated).</summary>
	public class GameBatches {

		/// <summary>
		/// Changes the domain affected by the next operations.
		/// You should typically use it this way: `gamer.Batches.Domain("private").Run(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the next operations.</param>
		/// <returns>This object for operation chaining.</returns>
		public GameBatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>Runs a batch on the server, unauthenticated (game-scoped).</summary>
		/// <returns>Promise resolved when the request has finished. The attached bundle is the JSON data returned by the match.</returns>
		/// <param name="batchName">Name of the batch to run, as configured on the server.</param>
		/// <param name="batchParams">Parameters to be passed to the batch.</param>
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
