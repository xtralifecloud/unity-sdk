using System;
using CloudBuilderLibrary;

namespace CLI
{
    public partial class Commands {

		[CommandInfo("Logs in anonymously and starts the event loop.")]
		void loginanonymous(Arguments args) {
			Clan.LoginAnonymously(SuccessHandler<Gamer>(DidLogin));
		}

		[CommandInfo("Resumes an existing session by gamer ID/secret.", "gamer_id, gamer_secret")]
		void resumesession(Arguments args) {
			args.Expecting(2, ArgumentType.String, ArgumentType.String);
			Clan.ResumeSession(
				done: SuccessHandler<Gamer>(DidLogin),
				gamerId: args.StringArg(0),
				gamerSecret: args.StringArg(1));
		}

		/**
		 * This handler handles any login done message.
		 */
		private void DidLogin(Result<Gamer> result) {
			Gamer = result.Value;
			// Only one loop at a time
			if (PrivateEventLoop != null) PrivateEventLoop.Stop();
			PrivateEventLoop = new DomainEventLoop(Gamer);
		}
    }
}
