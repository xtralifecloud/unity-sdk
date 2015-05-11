using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudBuilderLibrary;
using UnityEngine;

namespace CLI {
	public class Commands : MonoBehaviour {
		public List<CommandDefinition> Definitions() {
			return new List<CommandDefinition>() {
				new CommandDefinition("loginanonymous", "loginanonymous", LoginAnonymous),
				new CommandDefinition("match", "match commands", MatchDefinitions()),
			};
		}

		private List<CommandDefinition> MatchDefinitions() {
			return new List<CommandDefinition>() {
				new CommandDefinition("create", "creates a match", CreateMatch),
			};
		}

		public void LoginAnonymous(Arguments args) {
			Clan.LoginAnonymously(SuccessHandler<Gamer>(result => {
				Gamer = result.Value;
			}));
		}

		public void CreateMatch(Arguments args) {
			args.ExpectingArgs(1, ArgumentType.String, ArgumentType.Double);
			Log("Here: " + args.StringArg(0) + ", " + args.DoubleArg(1));
		}

		#region Not exposed
		private Clan Clan;
		private Gamer Gamer;
		public CloudBuilderGameObject CloudBuilderGameObject;
		public CLI Cli;

		void Start() {
			CloudBuilderGameObject.GetClan(result => {
				Clan = result;
				Debug.Log("Setup done");
			});
		}

		private void Log(string text) {
			Cli.AppendText(text);
		}

		private ResultHandler<T> SuccessHandler<T>(ResultHandler<T> subHandler) {
			return result => {
				if (result.IsSuccessful) {
					subHandler(result);
					Debug.Log("Done! " + result.ToString());
				}
				else {
					Debug.Log("Failed! " + result.ToString());
				}
			};
		}
		#endregion
	}
}
