
namespace CotcSdk {

	/// <summary>
	/// Result of an index query. Represents a single returned object.
	/// This object is a #CotcSdk.PropertiesObject, meaning that additional properties can be queried. If you want to
	/// check what is returned as a JSON object, simply log `this.ToString()`.
	/// </summary>
	public class IndexResult : PropertiesObject {
		/// <summary>The name of the index.</summary>
		public string IndexName { get; private set; }
		/// <summary>The ID of the returned object, as passed when indexing the object.</summary>
		public string ObjectId { get; private set; }
		/// <summary>Document payload; passed upon indexing the object.</summary>
		public Bundle Payload {
			get { return Props["_source"]["payload"]; }
		}
		/// <summary>Indexed properties. Passed upon indexing the object.</summary>
		public Bundle Properties {
			get { return Props["_source"]; }
		}
		/// <summary>Score (elastic search term) of the document.</summary>
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
