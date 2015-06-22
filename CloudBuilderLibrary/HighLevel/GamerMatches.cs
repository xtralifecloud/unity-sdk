using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Some methods accept a PushNotification parameter. This parameter can be used to forward a push notification to the
	 * users who are not active at the moment.
	 */
	public class GamerMatches {


		#region Private
		internal GamerMatches(Gamer gamer) {
			Gamer = gamer;
		}

		private void CheckEventLoopNeeded() {
			if (onMatchInvitation != null) {
				// Register if needed
				if (RegisteredEventLoop == null) {
					RegisteredEventLoop = Cotc.GetEventLoopFor(Gamer.GamerId, domain);
					if (RegisteredEventLoop == null) {
						Common.LogWarning("No pop event loop for domain " + domain + ", match invitations will not work");
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
			if (e.Message["type"].AsString() == "match.invite") {
				if (onMatchInvitation != null) onMatchInvitation(new MatchInviteEvent(Gamer, e.Message));
			}
		}

		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		private event Action<MatchInviteEvent> onMatchInvitation;
		private DomainEventLoop RegisteredEventLoop;
		#endregion
	}

}
