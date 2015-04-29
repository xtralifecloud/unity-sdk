using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	/**
	 * This class is responsible for polling the server.
	 */
	internal class SystemPopEventLoopThread {
		protected String Domain;
		private bool Stop = false;

		public SystemPopEventLoopThread(String domain) {
			Domain = domain;
		}

		public void Run() {
			while (!Stop) {
				UrlBuilder url = new UrlBuilder("/v1/gamer/event");
				url.Subpath(Domain).QueryParam("timeout", delay * 1000).QueryParam("correlationId", correlationId);

				HttpRequest req = CloudBuilder.Clan.MakeHttpRequest("/v1/gamer/event");




			}
		}
	}

}

