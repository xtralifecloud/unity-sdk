using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup main_classes
	/// <summary>Represents a paginated list, which allows for easy navigation through multiple results.</summary>
	public class PagedList<DataType> : List<DataType> {
		/// <summary>Fetches the next results and calls the same handler again.</summary>
		public Promise<PagedList<DataType>> FetchNext() {
			return Next();
		}
		/// <summary>Fetches the previous results and calls the same handler again.</summary>
		public Promise<PagedList<DataType>> FetchPrevious() {
			return Previous();
		}
		/// <summary></summary>
		/// <returns>Whether there is a previous page. Call FetchPrevious to go back to it.</returns>
		public bool HasPrevious {
			get { return Previous != null; }
		}
		/// <summary></summary>
		/// <returns>Whether there is a next page. Call FetchNext to go back to it.</returns>
		public bool HasNext {
			get { return Next != null; }
		}
		/// <summary></summary>
		/// <returns>The number of the first result in the list.</returns>
		public int Offset;
		public Bundle ServerData { get; private set; }
		/// <summary></summary>
		/// <returns>The total number of items (possibly greater than the page size).</returns>
		public int Total;

		public override string ToString() {
			return ServerData.ToString();
		}

		internal PagedList(int currentOffset, int totalResults) {
			Offset = currentOffset;
			Total = totalResults;
		}
		internal Func<Promise<PagedList<DataType>>> Next, Previous;
	}
}
