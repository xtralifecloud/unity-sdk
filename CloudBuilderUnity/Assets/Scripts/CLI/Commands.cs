using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudBuilderLibrary;
using UnityEngine;

namespace CLI {
	public class Commands: MonoBehaviour {
		public class Match {
			public void create(Arguments parser) {
				parser.ExpectingArgs(1, ArgumentType.String, ArgumentType.Double);
				Debug.Log("ICI " + parser.StringArg(0) + ", " + parser.DoubleArg(1));
			}
		}

		public void loginanonymous(Arguments args) {
			Clan.LoginAnonymously(StandardHandler<Gamer>(result => {
				if (result.IsSuccessful) {
					Gamer = result.Value;
				}
			}));
		}

		public Match match = new Match();

		#region Not exposed
		private Clan Clan;
		private Gamer Gamer;
		public CloudBuilderGameObject CloudBuilderGameObject;

		void Start() {
			CloudBuilderGameObject.GetClan(result => {
				Clan = result;
				Debug.Log("Setup done");
			});
		}

		private ResultHandler<T> StandardHandler<T>(ResultHandler<T> subHandler) {
			return result => {
				subHandler(result);
				if (result.IsSuccessful) {
					Debug.Log("Done! Result: " + result.ToString());
				}
				else {
					Debug.Log("Failed! Result: " + result.ToString());
				}
			};
		}
		#endregion
	}
}
