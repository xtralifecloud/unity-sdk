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
		private User User;

		public SystemPopEventLoopThread(User user, String domain) {
			Domain = domain;
			User = user;
		}

		public bool IsMainDomain {
			get { return Domain == Common.PrivateDomain; }
		}

		public void Start() {
			new Thread(new ThreadStart(this.Run)).Start();
		}

		private void Run() {
			Clan clan = User.Clan;
			int delay = clan.PopEventDelay;
			int CorrelationId = Random.Next();
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
				url.Subpath(Domain).QueryParam("timeout", delay).QueryParam("correlationId", CorrelationId);
				if (messageToAcknowledge != null) {
					url.QueryParam("ack", messageToAcknowledge);
				}

				HttpRequest req = User.MakeHttpRequest(url);
				req.RetryPolicy = HttpRequest.Policy.NonpermanentErrors;
				req.TimeoutMillisec = delay + 30000;
				HttpResponse res = Directory.HttpClient.RunSynchronously(req);
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
					User.NotifyNetworkState(lastResultPositive);
				}
			}
			CloudBuilder.Log("Finished pop event thread " + CorrelationId);
		}
	}

}

