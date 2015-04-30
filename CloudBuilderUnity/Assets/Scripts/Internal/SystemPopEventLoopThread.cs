using System;
using System.Collections.Generic;
using System.Threading;

namespace CloudBuilderLibrary {

	/**
	 * This class is responsible for polling the server.
	 */
	internal class SystemPopEventLoopThread {
		protected String Domain;
		private Random Random = new Random();
		private bool Stop = false;
		private const int PopEventDelayAfterFailure = 2000, PopEventDelayThreadHold = 20000;

		public SystemPopEventLoopThread(String domain) {
			Domain = domain;
		}

		public bool IsMainDomain {
			get { return Domain == Common.PrivateDomain; }
		}

		public void Run() {
			int delay = CloudBuilder.Clan.PopEventDelay;
			int correlationId = Random.Next();
			string messageToAcknowledge = null;
			bool lastResultPositive = true;

			while (!Stop) {
				if (!lastResultPositive) {
					// Last time failed, wait a bit to avoid bombing the Internet.
					Thread.Sleep(PopEventDelayThreadHold);
					// And try with a smaller delay so that we can notify success (connection back) quickly.
                    if (IsMainDomain) {
						delay = PopEventDelayAfterFailure;
					}
				}

				UrlBuilder url = new UrlBuilder("/v1/gamer/event");
				url.Subpath(Domain).QueryParam("timeout", delay).QueryParam("correlationId", correlationId);
				if (messageToAcknowledge != null) {
					url.QueryParam("ack", messageToAcknowledge);
				}

				HttpRequest req = CloudBuilder.Clan.MakeHttpRequest(url);
				req.RetryPolicy = HttpRequest.Policy.NonpermanentErrors;
				req.TimeoutMillisec = delay + 30000;
				HttpResponse res = CloudBuilder.HttpClient.RunSynchronously(req);
				lastResultPositive = true;
                
                if (res.StatusCode == 200) {
					messageToAcknowledge = res.BodyJson["id"];
				}
				else if (res.StatusCode != 204) {
					lastResultPositive = false;
					// Non retriable error -> kill ourselves
					if (res.StatusCode >= 400 && res.StatusCode < 500) {
						Stop = true;
					}
				}

				// Notify connection status to main thread
				if (IsMainDomain) {
					CloudBuilder.Clan.NetworkIsOnline = lastResultPositive;
                }
            }
		}
	}

}

