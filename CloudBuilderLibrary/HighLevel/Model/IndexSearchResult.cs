
namespace CotcSdk {

	/**
	 * Contains the results of a search on the index.
	 */
	public class IndexSearchResult {
		/**
		 * Paginated list of results.0
		 */
		public PagedList<IndexResult> Hits { get; private set; }
		/**
		 * Maximum score in the results.
		 */
		public int MaxScore;

		internal IndexSearchResult(Bundle serverData, int currentOffset) {
			MaxScore = serverData["max_score"];
			Hits = new PagedList<IndexResult>(currentOffset, serverData["total"]);
		}
	}
}
