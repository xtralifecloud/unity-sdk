using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	internal class UrlBuilder {
		public UrlBuilder(string path, string server = null) {
			Url = server ?? "";
		}

		public UrlBuilder Subpath(string path) {
			if (Url.Length > 0 && Url[Url.Length - 1] != '/') {
				Url += '/';
			}
			// Skip leading slash
			if (path[0] == '/') Url += path.Substring(1);
			else Url += path;
			return this;
		}

		public UrlBuilder QueryParam(string name, string value = null) {
			Url += Url.Contains("?") ? '&' : '?';
			Url += name;
			if (value != null) {
				Url += '=' + value;
			}
			return this;
		}

		public UrlBuilder QueryParam(string name, int value) {
			return QueryParam(name, value.ToString());
		}

		public static implicit operator string(UrlBuilder b) {
			return b.Url;
		}

		private string Url;
	}
}
