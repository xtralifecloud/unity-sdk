using System;

namespace CotcSdk {

	public class UrlBuilder {
		public UrlBuilder(string path, string server = null) {
			Url = server ?? "";
			Path(path);
		}

		public UrlBuilder Path(string path) {
			if (Url.Length == 0 || Url[Url.Length - 1] != '/') {
				Url += '/';
			}
			// Skip leading slash
			if (path[0] == '/') Url += path.Substring(1);
			else Url += path;
			return this;
		}

		public UrlBuilder QueryParamEscaped(string name, string value = null) {
			if (value != null) {
				value = Uri.EscapeDataString(value);
			}
			return QueryParam(name, value);
		}

		public UrlBuilder QueryParam(string name, string value = null) {
			Url += Url.Contains("?") ? '&' : '?';
			Url += name;
			if (value != null) {
				Url += '=' + value;
			}
			return this;
		}

		public UrlBuilder QueryParam(string name, bool value) {
			return QueryParam(name, value.ToString());
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
