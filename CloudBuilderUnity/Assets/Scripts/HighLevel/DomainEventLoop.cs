using System;
using System.Collections.Generic;
using System.Threading;

namespace CloudBuilderLibrary {

	public delegate void EventLoopHandler(DomainEventLoop sender, EventLoopArgs e);

	public class EventLoopArgs {
		Bundle Message;

		internal EventLoopArgs(Bundle message) {
			Message = message;
		}
	}

	/**
	 * This class is responsible for polling the server waiting for new events.
	 * You should instantiate one and manage its lifecycle as the state of the application changes.
	 */
	public sealed class DomainEventLoop {
		/**
		 * You need valid credentials in order to instantiate this class. Use Clan.Login* methods for that purpose.
		 * Once the object is created, you need to start the thread, please look at the other methods available.
		 * @param gamer the gamer object received from a login or similar function.
		 * @param domain the domain on which to listen for events. Note that you may create multiple event loops,
		 * especially if you are using multiple domains. The default domain, that you should use unless you are
		 * explicitly using multiple domains, is the private domain.
		 */
		public DomainEventLoop(Gamer gamer, String domain = Common.PrivateDomain) {
			Domain = domain;
			Gamer = gamer;
		}

		/**
		 * The domain on which this loop is listening.
		 */
		public String Domain {
			get; private set;
		}

		public event EventLoopHandler ReceivedEvent;

		/**
		 * Starts the thread. Call this upon initialization.
		 */
		public DomainEventLoop Start() {
			if (AlreadyStarted) return this;
			AlreadyStarted = true;
			// Allow for automatic housekeeping
			CloudBuilder.RunningEventLoops.Add(this);
			new Thread(new ThreadStart(this.Run)).Start();
			return this;
		}

		/**
		 * Will stop the event thread. Might take some time until the current request finishes.
		 * You should not use this object for other purposes later on. In particular, do not start it again.
		 */
		public DomainEventLoop Stop() {
			Stopped = true;
			return this;
		}

		/**
		 * Suspends the event thread.
		 */
		public DomainEventLoop Suspend() {
			// TODO
			return this;
		}

		/**
		 * Resumes a suspended event thread.
		 */
		public DomainEventLoop Resume() {
			// TODO
			return this;
		}

		#region Private
		private void Run() {
			Clan clan = Gamer.Clan;
			int delay = clan.PopEventDelay;
			int CorrelationId = Random.Next();
			string messageToAcknowledge = null;
			bool lastResultPositive = true;

			while (!Stopped) {
				if (!lastResultPositive) {
					// Last time failed, wait a bit to avoid bombing the Internet.
					Thread.Sleep(PopEventDelayThreadHold);
					// And try with a smaller delay so that we can notify success (connection back) quickly.
					delay = PopEventDelayAfterFailure;
				}

				UrlBuilder url = new UrlBuilder("/v1/gamer/event");
				url.Subpath(Domain).QueryParam("timeout", delay).QueryParam("correlationId", CorrelationId);
				if (messageToAcknowledge != null) {
					url.QueryParam("ack", messageToAcknowledge);
				}

				HttpRequest req = Gamer.MakeHttpRequest(url);
				req.RetryPolicy = HttpRequest.Policy.NonpermanentErrors;
				req.TimeoutMillisec = delay + 30000;
				HttpResponse res = Directory.HttpClient.RunSynchronously(req);
				lastResultPositive = true;

				if (res.StatusCode == 200) {
					messageToAcknowledge = res.BodyJson["id"];
					if (ReceivedEvent != null) ReceivedEvent(this, new EventLoopArgs(res.BodyJson));
				}
				else if (res.StatusCode != 204) {
					lastResultPositive = false;
					// Non retriable error -> kill ourselves
					if (res.StatusCode >= 400 && res.StatusCode < 500) {
						Stopped = true;
					}
				}
			}
			CloudBuilder.Log("Finished pop event thread " + CorrelationId);
		}

		private Random Random = new Random();
		private bool Stopped = false, AlreadyStarted = false;
		private const int PopEventDelayAfterFailure = 2000, PopEventDelayThreadHold = 20000;
		private Gamer Gamer;
		#endregion
	}
}

