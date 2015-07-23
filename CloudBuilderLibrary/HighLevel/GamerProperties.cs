using System;

namespace CotcSdk {

	/**
	 * Allows to manipulate the gamer properties.
	 */
	public sealed class GamerProperties {

		/**
		 * Sets the domain affected by this object.
		 * You should typically use it this way: `gamer.Properties.Domain("private").Post(...);`
		 * @param domain optional domain on which to scope the properties. Default to `private` if unmodified.
		 * @return this object for operation chaining.
		 */
		public GamerProperties Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Retrieves an individual key from the gamer properties.
		 * @return promise resolved when the operation has completed. The attached bundle contains the fetched property.
		 *     As usual with bundles, it can be casted to the proper type you are expecting.
		 *     In case the call fails, the bundle is not attached, the call is marked as failed with a 404 status.
		 * @param key the name of the key to be fetched.
		 */
		public Promise<Bundle> GetKey(string key) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson["properties"], response.BodyJson);
			});
		}

		/**
		 * Retrieves all the properties of the gamer.
		 * @return promise resolved when the operation has completed. The attached bundle contains the keys along with their
		 *     values. If you would like to fetch the value of a given key and you expect it to be a string, you may simply
		 *     do `string value = result.Value["key"];`. Bundle handles automatic conversions as well, so if you passed an
		 *     integer, you may as well fetch it as a string and vice versa.
		 */
		public Promise<Bundle> GetAll() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson["properties"], response.BodyJson);
			});
		}

		/**
		 * Sets a single key from the user properties.
		 * @return promise resolved when the operation has completed. The enclosed value indicates success.
		 * @param key the name of the key to set the value for.
		 * @param value the value to set. As usual with bundles, casting is implicitly done, so you may as well
		 *     call this method passing an integer or string as value for instance.
		 */
		public Promise<Done> SetKey(string key, Bundle value) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("value", value);
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Sets all keys at once.
		 * @return promise resolved when the operation has completed. The enclosed value indicates success.
		 * @param properties a bundle of key/value properties to set. An example is `Bundle.CreateObject("key", "value")`.
		 */
		public Promise<Done> SetAll(Bundle properties) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = properties;
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Removes a single key from the user properties.
		 * @return promise resolved when the operation has completed.
		 * @param key the name of the key to remove.
		 */
		public Promise<Done> RemoveKey(string key) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Remove all properties for the user.
		 * @return promise resolved when the operation has completed.
		 */
		public Promise<Done> RemoveAll() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		#region Private
		internal GamerProperties(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
