using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup main_classes
	/// <summary>Represents a non-paginated list. Counterpart to #PagedList, replacing simple List<> before. These lists allow to
	/// retrieve additional information that you can enrich using batches on the server.</summary>
	public class NonpagedList<DataType> : List<DataType> {
		public Bundle ServerData { get; private set; }
		public override string ToString() {
			return ServerData.ToString();
		}

		internal NonpagedList(Bundle serverData) {
			ServerData = serverData;
		}
	}
}
