using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Paginated result. Simply a list enriched with functionality allowing to easily paginate the results, which
	 * works by calling again the handler initially passed to the method with the current page, as shown below: @code 
		clan.Indexing.Search((Result<IndexSearchResult> result) => {
			PagedList<IndexResult> list = result.Value.Results;
			Debug.Log("Showing results starting from " + list.Offset);
			foreach (var obj in list) { ... }
			nextButton.Enabled = list.HasNext;
			nextButton.Click += list.FetchNext();
		}, …); @endcode
	 * In that case, the handler will be called again, showing the next results when the "nextButton" is clicked.

	 * This object is always found in a subclass from a #Result, or at the root of a #Result.
	 */
	public class PagedList<T> : List<T> {
		/**
		 * Fetches the next results and calls the same handler again.
		 */
		public void FetchNext() {
			Next();
		}
		/**
		 * Fetches the previous results and calls the same handler again.
		 */
		public void FetchPrevious() {
			Previous();
		}
		/**
		 * @return whether there is a previous page. Call FetchPrevious to go back to it.
		 */
		public bool HasPrevious {
			get { return Previous != null; }
		}
		/**
		 * @return whether there is a next page. Call FetchNext to go back to it.
		 */
		public bool HasNext {
			get { return Next != null; }
		}
		/**
		 * @return the number of the first result in the list.
		 */
		public int Offset;
		/**
		 * @return the total number of items (possibly greater than the page size).
		 */
		public int Total;

		internal PagedList(int currentOffset, int totalResults) {
			Offset = currentOffset;
			Total = totalResults;
		}
		internal Action Next, Previous;
	}
}
