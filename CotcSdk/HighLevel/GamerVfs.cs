
namespace CotcSdk {

	/// @ingroup gamer_classes
	/// <summary>
	/// Represents a key/value system, also known as virtual file system.
	/// This class is scoped by domain, meaning that you can call .Domain("yourdomain") and perform
	/// additional calls that are scoped.
	/// </summary>
	public sealed class GamerVfs {

		/// <summary>
		/// Sets the domain affected by this object.
		/// You should typically use it this way: `gamer.GamerVfs.Domain("private").SetKey(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the VFS. Defaults to `private` if not specified.</param>
		/// <returns>This object for operation chaining.</returns>
		public GamerVfs Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>Retrieves an individual key from the key/value system.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached bundle contains the fetched
		///     property. As usual with bundles, it can be casted to the proper type you are expecting.
		///     If the property doesn't exist, the call is marked as failed with a 404 status.</returns>
		/// <param name="key">The name of the key to be fetched.</param>
		public Promise<Bundle> GetKey(string key) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
                // For backward compatibilty of json input in the backoffice as litterals
			    if (response.BodyJson["value"] != null)
                    task.PostResult(response.BodyJson["value"]);
                else
                    task.PostResult(response.BodyJson);
            });
		}

		/// <summary>Retrieves the binary data of an individual key from the key/value system.</summary>
		/// <returns>Promise resolved when the operation has completed. The binary data is attached as the value
		///     of the result. Please ensure that the key was set with binary data before, or this call will
		///     fail with a network error.</returns>
		/// <param name="key">The name of the key to be fetched.</param>
		public Promise<byte[]> GetKeyBinary(string key) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key).QueryParam("binary");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<byte[]>(req, (response, task) => {
				// We must then download the received URL
				string downloadUrl = response.BodyString.Trim('"');
				HttpRequest binaryRequest = new HttpRequest();
				binaryRequest.Url = downloadUrl;
				binaryRequest.FailedHandler = Gamer.Cloud.HttpRequestFailedHandler;
				binaryRequest.Method = "GET";
				binaryRequest.TimeoutMillisec = Gamer.Cloud.HttpTimeoutMillis;
				binaryRequest.UserAgent = Gamer.Cloud.UserAgent;
				Common.RunRequest(binaryRequest, task, binaryResponse => {
					task.Resolve(binaryResponse.Body);
				});
			});
		}

		/// <summary>Sets the value of a key in the key/value system.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="key">The name of the key to set the value for.</param>
		/// <param name="value">The value to set. As usual with bundles, casting is implicitly done, so you may as well
		///     call this method passing an integer or string as value for instance.</param>
		public Promise<Done> SetKey(string key, Bundle value) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("value", value);
			req.Method = "PUT";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson));
			});
		}

		/// <summary>Sets the value of a key in the key/value system as binary data.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="key">The name of the key to set the value for.</param>
		/// <param name="binaryData">The value to set as binary data.</param>
		public Promise<Done> SetKeyBinary(string key, byte[] binaryData) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key).QueryParam("binary");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "PUT";
			return Common.RunInTask<Done>(req, (response, task) => {
				// Now we have an URL to upload the data to
				HttpRequest binaryRequest = new HttpRequest();
				binaryRequest.Url = response.BodyJson["putURL"];
				binaryRequest.Body = binaryData;
				binaryRequest.FailedHandler = Gamer.Cloud.HttpRequestFailedHandler;
				binaryRequest.Method = "PUT";
				binaryRequest.TimeoutMillisec = Gamer.Cloud.HttpTimeoutMillis;
				binaryRequest.UserAgent = Gamer.Cloud.UserAgent;
				Common.RunRequest(binaryRequest, task, binaryResponse => {
					task.Resolve(new Done(true));
				});
			});
		}

		/// <summary>Removes a single key from the key/value system.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="key">The name of the key to remove.</param>
		public Promise<Done> RemoveKey(string key) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson));
			});
		}

		#region Private
		internal GamerVfs(Gamer gamer) {
			Gamer = gamer;
		}

		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
