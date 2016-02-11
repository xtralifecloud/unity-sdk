using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CotcSdk {

	/// <summary>
	/// If an object of this type is returned, then it means that the thing was uploaded successfully.
	/// You can get the URL using the Url member.
	/// </summary>
	public class UploadResult: PropertiesObject {
		/// <summary>The URL to which the content was uploaded.</summary>
		public string Url { get; private set; }

		public UploadResult(Bundle serverData, string url) : base(serverData) {
			Url = url;
		}
	}
}
