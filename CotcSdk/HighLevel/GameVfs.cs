using System;
using System.Collections.Generic;

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
        /// <remarks>This method is obsolete, use GetValue instead.</remarks>
        [Obsolete("Will be removed soon. Use GetValue with a null key instead.")]
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
        /// <remarks>This method is obsolete, use GetValue instead.</remarks>
        [Obsolete("Will be removed soon. Use GetValue instead.")]
        public Promise<Bundle> GetKey(string key)
        {
            UrlBuilder url = new UrlBuilder("/v1/vfs").Path(domain).Path(key);
            HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
            return Common.RunInTask<Bundle>(req, (response, task) => {
                task.PostResult(response.BodyJson);
            });
        }
        /// <summary>Retrieves an individual key or all keys from the key/value system.</summary>
        /// <returns>Promise resolved when the operation has completed. The attached bundle contains the fetched property(ies).
        ///     As usual with bundles, it can be casted to the proper type you are expecting.
        ///     If the property doesn't exist, the call is marked as failed with a 404 status.</returns>
        /// <param name="key">The name of the key to be fetched.</param>
        public Promise<Bundle> GetValue(string key = null)
        {
            UrlBuilder url = new UrlBuilder("/v3.0/vfs").Path(domain);
            if (key != null && key != "")
                url = url.Path(key);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson);
			});
		}

        /// <summary>Retrieves the binary data of game key from the key/value system.</summary>
        /// <returns>Promise resolved when the operation has completed. The binary data is attached as the value
        ///     of the result. Please ensure that the key was set with binary data before, or this call will
        ///     fail with a network error.</returns>
        /// <param name="key">The name of the key to be fetched.</param>
        public Promise<byte[]> GetBinary(string key)
        {
            UrlBuilder url = new UrlBuilder("/v3.0/vfs").Path(domain).Path(key).QueryParam("binary");
            HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
            return Common.RunInTask<byte[]>(req, (response, task) => {
                // We must then download the received URL
                Bundle bundleRes = Bundle.FromJson(response.BodyString);
                Dictionary<string, Bundle> dict = bundleRes["result"].AsDictionary();
                string downloadUrl = "";
                foreach (var obj in dict)
                {
                    downloadUrl = obj.Value.AsString().Trim('"');
                    break;
                }
                HttpRequest binaryRequest = new HttpRequest();
                binaryRequest.Url = downloadUrl;
                binaryRequest.FailedHandler = Cloud.HttpRequestFailedHandler;
                binaryRequest.Method = "GET";
                binaryRequest.TimeoutMillisec = Cloud.HttpTimeoutMillis;
                binaryRequest.UserAgent = Cloud.UserAgent;
                Common.RunRequest(binaryRequest, task, binaryResponse => {
                    task.Resolve(binaryResponse.Body);
                }, forceClient: Managers.UnityHttpClient);
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
