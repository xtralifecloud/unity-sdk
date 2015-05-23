using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

	/**
	 * Represents a key/value system, also known as virtual file system.
	 * This class is scoped by domain, meaning that you can call .Domain("yourdomain") and perform
	 * additional calls that are scoped.
	 */
	public sealed class GamerVfs {

		/**
		 * Sets the domain affected by this object.
		 * You should typically use it this way: `gamer.GamerVfs.Domain("private").SetKey(...);`
		 * @param domain domain on which to scope the VFS. Defaults to `private` if not specified.
		 */
		public void Domain(string domain) {
			this.domain = domain;
		}

		/**
		 * Retrieves an individual key from the key/value system.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     contains the fetched property. As usual with bundles, it can be casted to the proper type you are expecting.
		 *     If the property doesn't exist, the call is marked as failed with a 404 status.
		 * @param key the name of the key to be fetched.
		 */
		public void GetKey(ResultHandler<Bundle> done, string key) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["value"], response.BodyJson);
			});
		}

		/**
		 * Retrieves the binary data of an individual key from the key/value system.
		 * @param done callback invoked when the operation has finished, either successfully or not. The binary data
		 *     is attached as the value of the result. Please ensure that the key was set with binary data before,
		 *     or this call will fail with a network error.
		 * @param key the name of the key to be fetched.
		 */
		public void GetKeyBinary(ResultHandler<byte[]> done, string key) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key).QueryParam("binary");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				// We must then download the received URL
				string downloadUrl = response.BodyString.Trim('"');
				HttpRequest binaryRequest = Gamer.MakeHttpRequest(downloadUrl);
				Common.RunHandledRequest(binaryRequest, done, binaryResponse => {
					Common.InvokeHandler(done, response.Body, response.BodyJson);
				});
			});
		}

		/**
		 * Sets the value of a key in the key/value system.
		 * @param done callback invoked when the operation has finished, either successfully or not. The enclosed value
		 *     indicates success.
		 * @param key the name of the key to set the value for.
		 * @param value the value to set. As usual with bundles, casting is implicitly done, so you may as well
		 *     call this method passing an integer or string as value for instance.
		 */
		public void SetKey(ResultHandler<bool> done, string key, Bundle value) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("value", value);
			req.Method = "PUT";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Sets the value of a key in the key/value system as binary data.
		 * @param done callback invoked when the operation has finished, either successfully or not. The enclosed value
		 *     indicates success.
		 * @param key the name of the key to set the value for.
		 * @param binaryData the value to set as binary data.
		 */
		public void SetKeyBinary(ResultHandler<bool> done, string key, byte[] binaryData) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key).QueryParam("binary");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "PUT";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				// Now we have an URL to upload the data to
				HttpRequest binaryRequest = new HttpRequest();
				binaryRequest.Url = response.BodyJson["putURL"];
				binaryRequest.Body = binaryData;
				binaryRequest.Method = "PUT";
				binaryRequest.TimeoutMillisec = Gamer.Clan.HttpTimeoutMillis;
				binaryRequest.UserAgent = Gamer.Clan.UserAgent;
				Common.RunHandledRequest(binaryRequest, done, binaryResponse => {
					Common.InvokeHandler(done, true);
				});
			});
		}

		/**
		 * Removes a single key from the key/value system.
		 * @param done callback invoked when the operation has finished, either successfully or not. The enclosed boolean
		 *     value indicates success.
		 * @param key the name of the key to remove.
		 */
		public void RemoveKey(ResultHandler<bool> done, string key) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/vfs").Path(domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
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
