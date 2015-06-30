using System;

namespace CotcSdk {

	/**
	 * Represents a key/value system, also known as virtual file system, to be used for game properties.
	 * This class is scoped by domain, meaning that you can call .Domain("yourdomain") and perform
	 * additional calls that are scoped.
	 */
	public sealed class GameVfs {

		/**
		 * Sets the domain affected by this object.
		 * You should typically use it this way: `gamer.GamerVfs.Domain("private").SetKey(...);`
		 * @param domain domain on which to scope the VFS. Defaults to `private` if not specified.
		 */
		public void Domain(string domain) {
			this.domain = domain;
		}

		/**
		 * Retrieves all keys from the key/value system for the current domain.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     contains the keys along with their values. If you would like to fetch the value of a given key and you
		 *     expect it to be a string, you may simply do `string value = result.Value["key"];`.
		 * @param key the name of the key to be fetched.
		 */
		public IPromise<Bundle> GetAll() {
			UrlBuilder url = new UrlBuilder("/v1/vfs").Path(domain);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson, response.BodyJson);
			});
		}

		/**
		 * Retrieves an individual key from the key/value system.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     contains the fetched property. As usual with bundles, it can be casted to the proper type you are expecting.
		 *     If the property doesn't exist, the call is marked as failed with a 404 status.
		 * @param key the name of the key to be fetched.
		 */
		public IPromise<Bundle> GetKey(string key) {
			UrlBuilder url = new UrlBuilder("/v1/vfs").Path(domain).Path(key);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson, response.BodyJson);
			});
		}

		#region Private
		internal GameVfs(Cloud cloud) {
			Cloud = cloud;
		}

		private string domain = Common.PrivateDomain;
		private Cloud Cloud;
		#endregion
	}
}
