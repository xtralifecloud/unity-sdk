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
		 */
		public void Domain(string domain) {
			this.domain = domain;
		}

		/**
		 * Retrieves an individual key from the gamer properties.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     contains the fetched property. As usual with bundles, it can be casted to the proper type you are expecting.
		 *     In case the call fails, the bundle is not attached, the call is marked as failed with a 404 status.
		 * @param key the name of the key to be fetched.
		 */
		public IPromise<Bundle> GetKey(string key) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson["properties"], response.BodyJson);
			});
		}

		/**
		 * Retrieves all the properties of the gamer.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     contains the keys along with their values. If you would like to fetch the value of a given key and you
		 *     expect it to be a string, you may simply do `string value = result.Value["key"];`. Bundle handles automatic
		 *     conversions as well, so if you passed an integer, you may as well fetch it as a string and vice versa.
		 */
		public IPromise<Bundle> GetAll() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson["properties"], response.BodyJson);
			});
		}

		/**
		 * Sets a single key from the user properties.
		 * @param done callback invoked when the operation has finished, either successfully or not. The enclosed value
		 *     indicates the number of set keys.
		 * @param key the name of the key to set the value for.
		 * @param value the value to set. As usual with bundles, casting is implicitly done, so you may as well
		 *     call this method passing an integer or string as value for instance.
		 */
		public IPromise<int> SetKey(string key, Bundle value) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("value", value);
			return Common.RunInTask<int>(req, (response, task) => {
				task.PostResult(response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Sets all keys at once.
		 * @param done callback invoked when the operation has finished, either successfully or not. The enclosed value
		 *     indicates the number of set keys.
		 * @param properties a bundle of key/value properties to set. An example is `Bundle.CreateObject("key", "value")`.
		 */
		public IPromise<int> SetAll(Bundle properties) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = properties;
			return Common.RunInTask<int>(req, (response, task) => {
				task.PostResult(response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Removes a single key from the user properties.
		 * @param done callback invoked when the operation has finished, either successfully or not. The enclosed boolean
		 *     value indicates success.
		 * @param key the name of the key to remove.
		 */
		public IPromise<bool> RemoveKey(string key) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<bool>(req, (response, task) => {
				task.PostResult(response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Remove all properties for the user.
		 * @param done callback invoked when the operation has finished, either successfully or not. The enclosed boolean
		 *     value indicates success.
		 */
		public IPromise<bool> RemoveAll() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/property").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<bool>(req, (response, task) => {
				task.PostResult(response.BodyJson["done"], response.BodyJson);
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
