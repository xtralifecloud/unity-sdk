using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class PagedList<DataType> : List<DataType> {
		/**
		 * Fetches the next results and calls the same handler again.
		 */
		public IPromise<PagedList<DataType>> FetchNext() {
			return Next();
		}
		/**
		 * Fetches the previous results and calls the same handler again.
		 */
		public IPromise<PagedList<DataType>> FetchPrevious() {
			return Previous();
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
		public Bundle ServerData { get; private set; }
		/**
		 * @return the total number of items (possibly greater than the page size).
		 */
		public int Total;

		public override string ToString() {
			return ServerData.ToString();
		}

		internal PagedList(int currentOffset, int totalResults) {
			Offset = currentOffset;
			Total = totalResults;
		}
		internal Func<IPromise<PagedList<DataType>>> Next, Previous;
	}
}
