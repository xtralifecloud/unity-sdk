
namespace CotcSdk {

	/// @ingroup gamer_classes
	/// <summary>
	/// Represents a key/value system, also known as virtual file system, to be used for game properties.
	/// This class is scoped by domain, meaning that you can call .Domain("yourdomain") and perform
	/// additional calls that are scoped.
	/// </summary>
	public sealed class GameVfs {

		/// <summary>
		/// Sets the domain affected by this object.
		/// You should typically use it this way: `gamer.GamerVfs.Domain("private").SetKey(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the VFS. Defaults to `private` if not specified.</param>
		/// <returns>This object, so you can chain operations</returns>
		public GameVfs Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>Retrieves all keys from the key/value system for the current domain.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached bundle contains the keys
		///     along with their values. If you would like to fetch the value of a given key and you expect
		///     it to be a string, you may simply do `string value = result.Value["key"];`.</returns>
		public Promise<Bundle> GetAll() {
			UrlBuilder url = new UrlBuilder("/v1/vfs").Path(domain);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson);
			});
		}

		/// <summary>Retrieves an individual key from the key/value system.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached bundle contains the fetched property.
		///     As usual with bundles, it can be casted to the proper type you are expecting.
		///     If the property doesn't exist, the call is marked as failed with a 404 status.</returns>
		/// <param name="key">The name of the key to be fetched.</param>
		public Promise<Bundle> GetKey(string key) {
			UrlBuilder url = new UrlBuilder("/v1/vfs").Path(domain).Path(key);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson);
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
