using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

	public class PagedResult<T> : Result<List<T>> {
		public void FetchNext() {
			Next();
		}
		public void FetchPrevious() {
			Previous();
		}
		public bool HasPrevious {
			get { return Previous != null; }
		}
		public bool HasNext {
			get { return Next != null; }
		}
		public int Offset;
		public int Total;
		public List<T> Values {
			get { return Value; }
		}

		internal PagedResult(HttpResponse response, string failureDescription = null) : base(response, failureDescription) {}
		internal PagedResult(ErrorCode code, string failureDescription = null) : base(code, failureDescription) { }
		internal PagedResult(List<T> values, Bundle serverData, int currentOffset) : base(values, serverData) {
			Offset = currentOffset;
			Total = serverData["count"];
		}
		internal Action Next, Previous;
	}

}
