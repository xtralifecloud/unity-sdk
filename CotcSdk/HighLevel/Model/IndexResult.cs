
namespace CotcSdk {

	/**
	 * Result of an index query. Represents a single returned object.
	 * This object is a #CotcSdk.PropertiesObject, meaning that additional properties can be queried. If you want to
	 * check what is returned as a JSON object, simply log `this.ToString()`.
	 */
	public class IndexResult : PropertiesObject {
		/**
		 * The name of the index.
		 */
		public string IndexName { get; private set; }
		/**
		 * The ID of the returned object, as passed when indexing the object.
		 */
		public string ObjectId { get; private set; }
		/**
		 * Document payload; passed upon indexing the object.
		 */
		public Bundle Payload {
			get { return Props["_source"]["payload"]; }
		}
		/**
		 * Indexed properties. Passed upon indexing the object.
		 */
		public Bundle Properties {
			get { return Props["_source"]; }
		}
		/**
		 * Score (elastic search term) of the document.
		 */
		public int ResultScore { get; private set; }

		#region Private
		internal IndexResult(Bundle serverData) : base(serverData) {
			IndexName = serverData["_type"];
			ObjectId = serverData["_id"];
			ResultScore = serverData["_score"];
		}
		#endregion
	}
}
