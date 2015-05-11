using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CloudBuilderLibrary;
using UnityEngine;

namespace CLI {

	/**
	 * Base principle about commands.
	 * Commands is a partial class, with specialized methods implemented in different files.
	 * The convention is that a method belonging to a sub-object corresponds to an underscore
	 * in the name. For instance, the match.create will be match_create.
	 * There is no need to declare the methods anywhere else as they are found by reflection.
	 * Only methods fully written in lowercase letters may be called from the script. The name
	 * is case-lowered before being looked up in this class.
	 */
	public partial class Commands : MonoBehaviour {
		public CloudBuilderGameObject CloudBuilderGameObject;
		public CLI Cli;

		// Look for example in Commands.Basic.cs for method implementations
		[CommandInfo("Gives help about all or a given command.", "[command_name]")]
		void help(Arguments args) {
			// Optionally help on a given method
			string name = "";
			while (args.Lex.NextIs(TokenType.Identifier)) {
				name += args.Lex.PullNextToken().Text;
				if (args.Lex.EatTokenIf(TokenType.Dot))
					name += '.';
			}

			// List all
			StringBuilder sb = new StringBuilder();
			foreach (var info in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)) {
				// Filter by name
				if (name != "" && info.Name != name) continue;
				// Only list lowercase ones
				if (info.Name.ToLower() == info.Name) {
					foreach (var att in info.GetCustomAttributes(typeof(CommandInfo), false)) {
						CommandInfo attInfo = (CommandInfo)att;
						sb.AppendLine(info.Name.Replace('_', '.') + " " + attInfo.Usage);
						sb.AppendLine(">> " + attInfo.Description);
					}
				}
			}
			Log(sb.ToString().TrimEnd());
		}

		private Clan Clan;
		private Gamer Gamer;
		private DomainEventLoop PrivateEventLoop;

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
	}
}
