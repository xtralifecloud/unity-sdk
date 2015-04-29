using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	internal class UrlBuilder {
		public UrlBuilder(string path, string server = null) {
			url = server ?? "";
		}

		public UrlBuilder Subpath(string path) {
			if (url.Length > 0 && url[url.Length - 1] != '/') {
				url += '/';
			}
			// Skip leading slash
			if (path[0] == '/') url += path.Substring(1);
			else url += path;
			return this;
		}

		public UrlBuilder QueryParam(string name, string value = null) {
			url += url.Contains('?') ? '&' : '?';
			url += name;
			if (value != null) {
				url += '=' + value;
			}
			return this;
		}

		public string Url {
			get { return url; }
		}

		private string url;
	}
}
