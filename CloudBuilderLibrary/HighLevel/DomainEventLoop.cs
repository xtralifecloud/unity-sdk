using System;
using System.Collections.Generic;
using System.Threading;

namespace CotcSdk {

	/**
	 * Delegate called when receiving a message on a #DomainEventLoop.
	 * @param sender domain loop that triggered the event.
	 * @param e description of the received event.
	 */
	public delegate void EventLoopHandler(DomainEventLoop sender, EventLoopArgs e);

	/**
	 * Arguments of the EventLoopArgs.ReceivedEvent event. You can use `args.Message.ToJson()` to
	 * obtain more information.
	 */
	public class EventLoopArgs {
		/**
		 * Message received.
		 */
		public Bundle Message {
			get; private set;
		}

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
		 * You need valid credentials in order to instantiate this class. Use Cloud.Login* methods for that purpose.
		 * Once the object is created, you need to start the thread, please look at the other methods available.
		 * @param gamer the gamer object received from a login or similar function.
		 * @param domain the domain on which to listen for events. Note that you may create multiple event loops,
		 * especially if you are using multiple domains. The default domain, that you should use unless you are
		 * explicitly using multiple domains, is the private domain.
		 * @param sets a custom timeout in seconds for the long polling event loop. Should be used with care and
		 *	 set to a high value (at least 60). Defaults to 590 (~10 min).
		 */
		public DomainEventLoop(Gamer gamer, String domain = Common.PrivateDomain, int iterationDuration = 590) {
			Domain = domain;
			Gamer = gamer;
			LoopIterationDuration = iterationDuration * 1000;
			Random = new Random((int)DateTime.UtcNow.Ticks);
		}

		/**
		 * The domain on which this loop is listening.
		 */
		public String Domain {
			get; private set;
		}

		public Gamer Gamer { get; private set; }

		/**
		 * This event is raised when an event is received.
		 */
		public event EventLoopHandler ReceivedEvent {
			add { receivedEvent += value; }
			remove { receivedEvent -= value; }
		}
		private EventLoopHandler receivedEvent;

		/**
		 * Starts the thread. Call this upon initialization.
		 */
		public DomainEventLoop Start() {
			if (Stopped) throw new InvalidOperationException("Never restart a loop that was stopped");
			if (AlreadyStarted) return this;
			AlreadyStarted = true;
			// Allow for automatic housekeeping
			Cotc.RunningEventLoops.Add(this);
			new Thread(new ThreadStart(this.Run)).Start();
			return this;
		}

		/**
		 * Will stop the event thread. Might take some time until the current request finishes.
		 * You should not use this object for other purposes later on. In particular, do not start it again.
		 */
		public DomainEventLoop Stop() {
			Stopped = true;
			Resume();
			// Stop and exit cleanly
			if (CurrentRequest != null) {
				Managers.HttpClient.Abort(CurrentRequest);
				CurrentRequest = null;
			}
			Cotc.RunningEventLoops.Remove(this);
			return this;
		}

		/**
		 * Suspends the event thread.
		 */
		public DomainEventLoop Suspend() {
			Paused = true;
			if (CurrentRequest != null) {
				Managers.HttpClient.Abort(CurrentRequest);
				CurrentRequest = null;
			}
			return this;
		}

		/**
		 * Resumes a suspended event thread.
		 */
		public DomainEventLoop Resume() {
			if (Paused) {
				SynchronousRequestLock.Set();
				Paused = false;
			}
			return this;
		}

		#region Private
		private void ProcessEvent(HttpResponse res) {
			try {
				if (receivedEvent != null) receivedEvent(this, new EventLoopArgs(res.BodyJson));
			}
			catch (Exception e) {
				Common.LogError("Exception in the event chain: " + e.ToString());
			}
		}

		private void Run() {
			Cloud cloud = Gamer.Cloud;
			int delay = LoopIterationDuration;
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
				url.Path(Domain).QueryParam("timeout", delay);
				if (messageToAcknowledge != null) {
					url.QueryParam("ack", messageToAcknowledge);
				}

				CurrentRequest = Gamer.MakeHttpRequest(url);
				CurrentRequest.RetryPolicy = HttpRequest.Policy.NonpermanentErrors;
				CurrentRequest.TimeoutMillisec = delay + 30000;
				CurrentRequest.DoNotEnqueue = true;

				Managers.HttpClient.Run(CurrentRequest, (HttpResponse res) => {
					CurrentRequest = null;
					try {
						lastResultPositive = true;
						if (res.StatusCode == 200) {
							messageToAcknowledge = res.BodyJson["id"];
							ProcessEvent(res);
						}
						else if (res.StatusCode != 204) {
							lastResultPositive = false;
							// Non retriable error -> kill ourselves
							if (res.StatusCode >= 400 && res.StatusCode < 500) {
								Stopped = true;
							}
						}
					}
					catch (Exception e) {
						Common.LogError("Exception happened in pop event loop: " + e.ToString());
					}
					SynchronousRequestLock.Set();
				});

				// Wait for request (synchronous)
				SynchronousRequestLock.WaitOne();

				// Wait if suspended
				if (Paused) {
					SynchronousRequestLock.WaitOne();
					lastResultPositive = true;
				}
			}
			Common.Log("Finished pop event thread " + Thread.CurrentThread.ManagedThreadId);
		}

		private Random Random;
		private AutoResetEvent SynchronousRequestLock = new AutoResetEvent(false);
		private HttpRequest CurrentRequest;
		private bool Stopped = false, AlreadyStarted = false, Paused = false;
		private int LoopIterationDuration;
		private const int PopEventDelayAfterFailure = 2000, PopEventDelayThreadHold = 20000;
		#endregion
	}
}
