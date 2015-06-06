using System;

namespace CloudBuilderLibrary {

	/**
	 * Result of an index query. Represents a single returned object.
	 * This object is a #PropertiesObject, meaning that additional properties can be queried. If you want to
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
			get { return Properties["_source"]["payload"]; }
		}
		/**
		 * Indexed properties. Passed upon indexing the object.
		 */
		public Bundle Properties {
			get { return Properties["_source"]; }
		}
		/**
		 * Score (elastic search term) of the document.
		 */
		public int ResultScore { get; private set; }

		#region Private
		internal IndexResult(Bundle serverData) : base(serverData) {
			IndexName = Properties["_index"];
			ObjectId = Properties["_id"];
			ResultScore = Properties["_score"];
		}
		#endregion
	}
}
