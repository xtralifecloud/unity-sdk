using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

	/**
	 * Allows to manipulate the gamer properties.
	 */
	public sealed class GamerProperties {

		internal GamerProperties(Gamer parent) {
			Gamer = parent;
		}

		/**
		 * Sets the domain affected by this object.
		 * You should typically use it this way: `gamer.Properties.Domain("private").Post(...);`
		 * @param domain optional domain on which to scope the properties. Default to `private` if unmodified.
		 */
		public void Domain(string domain) {
			this.domain = domain;
		}

		public void GetKey(ResultHandler<Bundle> done, string key) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["properties"], response.BodyJson);
			});
		}

		public void GetAll(ResultHandler<Bundle> done) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["properties"], response.BodyJson);
			});
		}

		public void SetKey(ResultHandler<int> done, string key, Bundle value) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("value", value);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		public void SetAll(ResultHandler<int> done, Bundle properties) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = properties;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		public void RemoveKey(ResultHandler<bool> done, string key) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		public void RemoveAll(ResultHandler<bool> done) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
	}
}
