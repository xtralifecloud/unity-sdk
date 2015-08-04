﻿using System;
using System.Collections.Generic;

namespace CotcSdk
{
	/**
	 * Godfather (code) related functions. You may also want to subscribe to events (see #CotcSdk.GamerGodfather.OnGotGodchild).
	 */
	public class GamerGodfather {

		public event Action<GotGodchildEvent> OnGotGodchild {
			add { onGotGodchild += value; CheckEventLoopNeeded(); }
			remove { onGotGodchild -= value; CheckEventLoopNeeded(); }
		}

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Godfather.Domain("private").Associate(...);`
		 * @param domain domain on which to scope the next operations.
		 * @return this object for operation chaining.
		 */
		public GamerGodfather Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Clears all event handlers subscribed, ensuring that a match object can be dismissed without causing further
		 * actions in the background.
		 */
		public void DiscardEventHandlers() {
			foreach (Action<GotGodchildEvent> e in onGotGodchild.GetInvocationList()) onGotGodchild -= e;
			CheckEventLoopNeeded();
		}

		/**
		 * Method to call in order to generate a temporary code that can be passed to another gamer so he can
		 * add us as a godfather.
		 * 
		 * The domain as specified by the #Domain method is the domain in which the godfather link should be
		 * established. "private" means it's local to this game only.
		 * 
		 * @return promise resolved when the operation has completed. The attached string is the generated code.
		 */
		public Promise<string> GenerateCode() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/godfather").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "PUT";
			return Common.RunInTask<string>(req, (response, task) => {
				task.PostResult(response.BodyJson["godfathercode"], response.BodyJson);
			});
		}

		/**
		 * This method can be used to retrieve the gamer who have added you as a godfather.
		 * @return promise resolved when the operation has completed.
		 */
		public Promise<List<GamerInfo>> GetGodchildren() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/godchildren").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<List<GamerInfo>>(req, (response, task) => {
				List<GamerInfo> result = new List<GamerInfo>();
				foreach (Bundle b in response.BodyJson["godchildren"].AsArray()) {
					result.Add(new GamerInfo(b));
				}
				task.PostResult(result, response.BodyJson);
			});
		}

		/**
		 * This method can be used to retrieve the godfather of the gamer.
		 * @return promise resolved when the operation has completed.
		 */
		public Promise<GamerInfo> GetGodfather() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/godfather").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<GamerInfo>(req, (response, task) => {
				task.PostResult(new GamerInfo(response.BodyJson["godfather"]), response.BodyJson);
			});
		}

		/**
		 * Call this to attribute a godfather to the currently logged in user.
		 * @return promise resolved when the operation has completed.
		 * @param code is a string as generated by #GenerateCode.
		 * @param rewardTx a transaction Json rewarding the godfather formed as follows:
		 *  { transaction : { "unit" : amount},
		 *    description : "reward transaction",
		 *    domain : "com.clanoftcloud.text.DOMAIN" }
		 * where description and domain are optional.
		 * @param notification optional OS notification to be sent to the godfather who generated the code.
		 *     The godfather will reveive an event of type 'godchildren' containing the id of the godchildren
		 *     and the balance/achievements field if rewarded.
		 */
		public Promise<Done> UseCode(string code, Bundle rewardTx = null, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/godfather").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Bundle config = Bundle.CreateObject();
			config["godfather"] = code;
			config["osn"] = notification != null ? notification.Data : null;
			config["reward"] = rewardTx;
			req.BodyJson = config;
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		#region Private
		internal GamerGodfather(Gamer parent) {
			Gamer = parent;
		}

		private void CheckEventLoopNeeded() {
			if (onGotGodchild != null) {
				// Register if needed
				if (RegisteredEventLoop == null) {
					RegisteredEventLoop = Cotc.GetEventLoopFor(Gamer.GamerId, domain);
					if (RegisteredEventLoop == null) {
						Common.LogWarning("No pop event loop for domain " + domain + ", community events will not work");
					}
					else {
						RegisteredEventLoop.ReceivedEvent += this.ReceivedLoopEvent;
					}
				}
			}
			else if (RegisteredEventLoop != null) {
				// Unregister from event loop
				RegisteredEventLoop.ReceivedEvent -= this.ReceivedLoopEvent;
				RegisteredEventLoop = null;
			}
		}

		private void ReceivedLoopEvent(DomainEventLoop sender, EventLoopArgs e) {
			if (e.Message["type"].AsString() == "godchildren" && onGotGodchild != null) {
				onGotGodchild(new GotGodchildEvent(e.Message));
			}
		}

		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		private Action<GotGodchildEvent> onGotGodchild;
		private DomainEventLoop RegisteredEventLoop;
		#endregion
	}

	/**
	 * Event triggered when a godfather code is used. This event is received by the one who originated the code
	 * (godfather). See #CotcSdk.GamerGodfather.GenerateCode.
	 */
	public class GotGodchildEvent: PropertiesObject {
		/**
		 * Gamer who accepted the godfather request.
		 */
		public GamerInfo Gamer;
		/**
		 * Reward transaction executed if any.
		 */
		public Bundle Reward;

		public GotGodchildEvent(Bundle serverData) : base(serverData) {
			Gamer = new GamerInfo(serverData["event"]["godchildren"]);
			Reward = serverData["reward"];
		}
	}
}
