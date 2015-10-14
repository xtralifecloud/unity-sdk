
namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>Contains the results of a search on the index.</summary>
	public class IndexSearchResult {
		/// <summary>Paginated list of results.0</summary>
		public PagedList<IndexResult> Hits { get; private set; }
		/// <summary>Maximum score in the results.</summary>
		public int MaxScore;

		internal IndexSearchResult(Bundle serverData, int currentOffset) {
			MaxScore = serverData["max_score"];
			Hits = new PagedList<IndexResult>(serverData, currentOffset, serverData["total"]);
		}
	}
}
