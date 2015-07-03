using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		public static event EventHandler<LoggedInEventArgs> LoggedIn;

		public static void NotifyLoggedIn(object sender, Gamer gamer) {
			if (LoggedIn != null) LoggedIn(sender, new LoggedInEventArgs(gamer));
		}
	}
}
