using System;
using System.Collections.Generic;
using CotcSdk;

namespace CLI
{
	public partial class Commands {

		[CommandInfo("Resets the library up with different parameters.")]
		void setup(Arguments args) {
			args.Return();
		}

		[CommandInfo("Logs in anonymously and starts the event loop.")]
		void loginanonymous(Arguments args) {
			Cloud.LoginAnonymously()
				.WrapForSuccess(this, args, gamer => DidLogin(gamer, args));
		}

		[CommandInfo("Logs on using any supported network.", "network, id, secret")]
		void login(Arguments args) {
			args.Expecting(3, ArgumentType.String, ArgumentType.String, ArgumentType.String);
			Cloud.Login(
				network: ParseEnum<LoginNetwork>(args.StringArg(0)),
				networkId: args.StringArg(1),
				networkSecret: args.StringArg(2))
			.WrapForSuccess(this, args, result => DidLogin(result, args));
		}

		[CommandInfo("Resumes an existing session by gamer ID/secret.", "gamer_id, gamer_secret")]
		void resumesession(Arguments args) {
			args.Expecting(2, ArgumentType.String, ArgumentType.String);
			Cloud.ResumeSession(
				gamerId: args.StringArg(0),
				gamerSecret: args.StringArg(1))
			.WrapForSuccess(this, args, result => DidLogin(result, args));
		}

		[CommandInfo("Logs in with a facebook profile")]
		void loginfacebook(Arguments args) {
			CotcFacebookIntegration.LoginWithFacebook(Cloud)
				.WrapForSuccess(this, args, result => DidLogin(result, args));
		}

		[CommandInfo("Lists facebook friends and sends it to CotC servers")]
		void fbfriends(Arguments args) {
			CotcFacebookIntegration.FetchFriends(Gamer)
				.WrapForSuccess(this, args, result => args.Return(result.AsBundle()));
		}

		[CommandInfo("Lists friends")]
		void listfriends(Arguments args) {
			Gamer.Community.ListFriends(filterBlacklisted: false)
			.WrapForSuccess(this, args, result => args.Return());
		}

		/**
		 * This handler handles any login done message.
		 */
		private void DidLogin(Gamer result, Arguments args) {
			Gamer = result;
			// Only one loop at a time
			if (PrivateEventLoop != null) PrivateEventLoop.Stop();
			PrivateEventLoop = new DomainEventLoop(Gamer);
			// Return info about the gamer
			args.Return(result.AsBundle());
		}

		private T ParseEnum<T>(string value, T defaultValue = default(T)) {
			try {
				return (T)Enum.Parse(typeof(T), value, true);
			}
			catch (Exception) {
				return defaultValue;
			}
		}
    }
}
