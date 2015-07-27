using System;

namespace CotcSdk
{
	/////// Events to be used for plugins ///////
	public static partial class Cotc {
		public class LoggedInEventArgs : EventArgs {
			public Gamer Gamer { get; private set; }

			internal LoggedInEventArgs(Gamer gamer) {
				this.Gamer = gamer;
			}
		}

		private static EventHandler<LoggedInEventArgs> loggedIn;
		public static event EventHandler<LoggedInEventArgs> LoggedIn {
			add { loggedIn += value; }
			remove { loggedIn -= value; }
		}

		public static void NotifyLoggedIn(object sender, Gamer gamer) {
			if (loggedIn != null) loggedIn(sender, new LoggedInEventArgs(gamer));
		}
	}
}
