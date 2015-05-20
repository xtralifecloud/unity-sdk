using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

	public class GamerProperties {

		internal GamerProperties(Gamer parent, string domain) {
			Domain = domain;
			Gamer = parent;
		}

		public void GetKey(ResultHandler<Bundle> done, string key) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Subpath(Domain).Subpath(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["properties"], response.BodyJson);
			});
		}

		public void GetAll(ResultHandler<Bundle> done) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Subpath(Domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["properties"], response.BodyJson);
			});
		}

		public void Set(ResultHandler<Bundle> done, string key, Bundle value) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Subpath(Domain).Subpath(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("value", value);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["properties"], response.BodyJson);
			});
		}

		public void SetAll(ResultHandler<Bundle> done, Bundle properties) {

		}

		public void RemoveKey(ResultHandler<bool> done, string key) {

		}

		public void RemoveAll(ResultHandler<bool> done) {

		}

		private string Domain;
		private Gamer Gamer;
	}
}
