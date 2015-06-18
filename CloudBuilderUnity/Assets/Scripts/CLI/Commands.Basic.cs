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
			Cloud.LoginAnonymously(SuccessHandler<Gamer>(args, result => DidLogin(result, args)));
		}

		[CommandInfo("Logs on using any supported network.", "network, id, secret")]
		void login(Arguments args) {
			args.Expecting(3, ArgumentType.String, ArgumentType.String, ArgumentType.String);
			Cloud.Login(
				done: SuccessHandler<Gamer>(args, result => DidLogin(result, args)),
				network: ParseEnum<LoginNetwork>(args.StringArg(0)),
				networkId: args.StringArg(1),
				networkSecret: args.StringArg(2));
		}

		[CommandInfo("Resumes an existing session by gamer ID/secret.", "gamer_id, gamer_secret")]
		void resumesession(Arguments args) {
			args.Expecting(2, ArgumentType.String, ArgumentType.String);
			Cloud.ResumeSession(
				done: SuccessHandler<Gamer>(args, result => DidLogin(result, args)),
				gamerId: args.StringArg(0),
				gamerSecret: args.StringArg(1));
		}

		[CommandInfo("Logs in with a facebook profile")]
		void loginfacebook(Arguments args) {
			CotcFacebookIntegration.LoginWithFacebook(SuccessHandler<Gamer>(args, result => DidLogin(result, args)), Cloud);
		}

		[CommandInfo("Lists facebook friends and sends it to CotC servers")]
		void fbfriends(Arguments args) {
			CotcFacebookIntegration.FetchFriends(
				done: SuccessHandler<SocialNetworkFriendResponse>(args, result => args.Return(result.ServerData)),
				gamer: Gamer);
		}

		[CommandInfo("Lists friends")]
		void listfriends(Arguments args) {
			Gamer.Community.ListFriends(
				done: SuccessHandler<List<GamerInfo>>(args, result => args.Return()),
				filterBlacklisted: false);
		}

		/**
		 * This handler handles any login done message.
		 */
		private void DidLogin(Result<Gamer> result, Arguments args) {
			Gamer = result.Value;
			// Only one loop at a time
			if (PrivateEventLoop != null) PrivateEventLoop.Stop();
			PrivateEventLoop = new DomainEventLoop(Gamer);
			// Return info about the gamer
			args.Return(result.ServerData);
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
