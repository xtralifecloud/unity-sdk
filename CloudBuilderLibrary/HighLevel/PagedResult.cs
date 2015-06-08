using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	/**
	 * A subclass of Result that handles pagination. It basically represents a result with a list of entities.
	 * It provides functionality for easy paginating the results, which works by calling again the handler initially
	 * passed to the method with the current page, as shown below: @code 
		clan.Transactions.History((PagedResult<Transaction> result) => {
			Debug.Log("Showing results starting from " + result.Offset);
			foreach (var tx in result.Values) { ... }
			nextButton.Enabled = result.HasNext;
			nextButton.Click += result.FetchNext();
		}); @endcode
	 * In that case, the handler will be called again, showing the next results when the "nextButton" is clicked.
	 */
	public class PagedResult<T> : Result<List<T>> {
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
		/**
		 * @return the fetched values for the current page, exposed as a list.
		 */
		public List<T> Values {
			get { return Value; }
		}

		internal PagedResult(HttpResponse response, string failureDescription = null) : base(response, failureDescription) {}
		internal PagedResult(ErrorCode code, string failureDescription = null) : base(code, failureDescription) { }
		internal PagedResult(List<T> values, Bundle serverData, int currentOffset) : base(values, serverData) {
			Offset = currentOffset;
			Total = serverData["count"];
		}
		internal PagedResult(List<T> values, Bundle serverData, int currentOffset, int totalResults)
			: base(values, serverData) {
			Offset = currentOffset;
			Total = totalResults;
		}
		internal Action Next, Previous;
	}
}
