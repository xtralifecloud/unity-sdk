﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

	/**
	 * Represents a key/value system, also known as virtual file system.
	 * Subclasses implement this functionality: Gamer.Vfs and .
	 */
	public class KeyValueSystem {
		internal KeyValueSystem(Gamer gamer, string domain, string baseUrl) {
			Gamer = gamer;
			Domain = domain;
			BaseUrl = baseUrl;
		}

		public void GetKey(ResultHandler<Bundle> done, string key) {
			UrlBuilder url = new UrlBuilder(BaseUrl).Path(Domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["value"], response.BodyJson);
			});
		}

		public void GetKeyBinary(ResultHandler<byte[]> done, string key) {
			UrlBuilder url = new UrlBuilder(BaseUrl).Path(Domain).Path(key).QueryParam("binary");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				// We must then download the received URL
				string downloadUrl = response.BodyString.Trim('"');
				HttpRequest binaryRequest = Gamer.MakeHttpRequest(downloadUrl);
				Managers.HttpClient.Run(binaryRequest, binaryResponse => {
					Common.InvokeHandler(done, response.Body, response.BodyJson);
				});
			});
		}

		public void SetKey(ResultHandler<bool> done, string key, Bundle value) {
			UrlBuilder url = new UrlBuilder(BaseUrl).Path(Domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("value", value);
			req.Method = "PUT";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		public void SetKeyBinary(ResultHandler<bool> done, string key, byte[] binaryData) {
			UrlBuilder url = new UrlBuilder(BaseUrl).Path(Domain).Path(key).QueryParam("binary");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Body = binaryData;
			req.Method = "PUT";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		public void RemoveKey(ResultHandler<bool> done, string key) {
			UrlBuilder url = new UrlBuilder(BaseUrl).Path(Domain).Path(key);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		private string BaseUrl, Domain;
		private Gamer Gamer;
	}
}
