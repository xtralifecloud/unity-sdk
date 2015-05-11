using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudBuilderLibrary;
using UnityEngine;

namespace CLI {
	public class Commands : MonoBehaviour {
		#region Definitions
		public List<CommandDefinition> Definitions() {
			return new List<CommandDefinition>() {
				new CommandDefinition("test", "locally tests the interpreter", Test),
				new CommandDefinition("loginanonymous", "loginanonymous (no arg)", LoginAnonymous),
				new CommandDefinition("match", "match commands", MatchDefinitions()),
			};
		}

		private List<CommandDefinition> MatchDefinitions() {
			return new List<CommandDefinition>() {
				new CommandDefinition("create", "creates a match", CreateMatch),
			};
		}
		#endregion

		#region Clan commands
		private void Test(Arguments args) {
			args.ExpectingArgs(0, ArgumentType.String);
			Log("In test method with value " + args.StringArg(0));
		}
		private void LoginAnonymous(Arguments args) {
			Clan.LoginAnonymously(SuccessHandler<Gamer>(result => {
				Gamer = result.Value;
				// Only one loop at a time
				if (PrivateEventLoop != null) PrivateEventLoop.Stop();
				PrivateEventLoop = new DomainEventLoop(Gamer);
			}));
		}
		#endregion

		#region Match commands
		private void CreateMatch(Arguments args) {
			args.ExpectingArgs(1, ArgumentType.String, ArgumentType.Double);
			Log("Here: " + args.StringArg(0) + ", " + args.DoubleArg(1));
		}
		#endregion

		#region Variables / Internal methods
		private Clan Clan;
		private Gamer Gamer;
		private DomainEventLoop PrivateEventLoop;
		public CloudBuilderGameObject CloudBuilderGameObject;
		public CLI Cli;

		void Start() {
			CloudBuilderGameObject.GetClan(result => {
				Clan = result;
				Debug.Log("Setup done");
			});
		}

		private void Log(string text) {
			Cli.AppendLine(text);
		}

		private ResultHandler<T> SuccessHandler<T>(ResultHandler<T> subHandler) {
			return result => {
				if (result.IsSuccessful) {
					subHandler(result);
				}
				Log(">> " + result.ToString());
			};
		}
		#endregion
	}
}
