using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;

namespace CotcSdk
{
	public sealed partial class Gamer {

		public List<string> Domains { get; private set; }
		/**
		 * Gamer credential. Use it to gain access to user related tasks.
		 */
		public string GamerId { get; private set; }
		/**
		 * Gamer credential (secret). Same purpose as GamerId, and you will need those in pair.
		 */
		public string GamerSecret { get; private set; }
		public LoginNetwork Network { get; private set; }
		public string NetworkId { get; private set; }
		public DateTime RegisterTime { get; private set; }

		/**
		 * Provides account related functions for the current gamer.
		 * @return an object allowing to manipulate the account of the current gamer.
		 */
		public GamerAccountMethods Account {
			get { return new GamerAccountMethods(this); }
		}

		/**
		 * Provides an API to manipulate achievements.
		 * @return an object that allows to manipulate achievements.
		 */
		public GamerAchievements Achievements {
			get { return new GamerAchievements(this); }
		}

		/**
		 * Provides an API to run batches.
		 * @return an object that allows to manipulate batches.
		 */
		public GamerBatches Batches {
			get { return new GamerBatches(this); }
		}

		/**
		 * Provides an API to interact with friends on CotC.
		 * @return an object that allow to manipulate friends.
		 */
		public GamerCommunity Community {
			get { return new GamerCommunity(this); }
		}

		/**
		 * Returns an object that allows to manipulate the key/value system associated with this user.
		 * @return an object allowing to manipulate key/values for this user/domain.
		 */
		public GamerVfs GamerVfs {
			get { return new GamerVfs(this); }
		}

		/**
		 * Exposes functionality related to the godfathers.
		 * @return an object that allows to add a godfather, generate a code, etc.
		 */
		public GamerGodfather Godfather {
			get { return new GamerGodfather(this); }
		}

		/**
		 * Provides an API to manipulate matches (mainly start them, since working with
		 * existing matches is provided by the Match class).
		 * @return an object that allows to perform basic operations on matches.
		 */
		public GamerMatches Matches {
			get { MatchesInstance = MatchesInstance ?? new GamerMatches(this); return MatchesInstance; }
		}

		/**
		 * Allows to manipulate information related to the gamer profile.
		 * @return an object that allows to read and set the profile.
		 */
		public GamerProfileMethods Profile {
			get { return new GamerProfileMethods(this); }
		}

		/**
		 * Allows to manipulate the properties of the current gamer.
		 * @return an object that allows to set, delete, etc. property values.
		 */
		public GamerProperties Properties {
			get { return new GamerProperties(this); }
		}

		/**
		 * Provides an API able to handle functionality related to the leaderboards and scores.
		 * @return an object that allows to manipulate scores.
		 */
		public GamerScores Scores {
			get { return new GamerScores(this); }
		}

		/**
		 * Allows to manipulate the transactions and related achievements of an user.
		 * @return an object that allows to manipulate transactions and query achievements.
		 */
		public GamerTransactions Transactions {
			get { return new GamerTransactions(this); }
		}

		#region Internal
		/**
		 * Only instantiated internally.
		 * @param gamerData Gamer data as returned by our API calls (loginanonymous, etc.).
		 */
		internal Gamer(Cloud parent, Bundle gamerData) {
			Cloud = parent;
			Network = Common.ParseEnum<LoginNetwork>(gamerData["network"]);
			NetworkId = gamerData["networkid"];
			GamerId = gamerData["gamer_id"];
			GamerSecret = gamerData["gamer_secret"];
			RegisterTime = Common.ParseHttpDate(gamerData["registerTime"]);
			Domains = new List<string>();
			foreach (Bundle domain in gamerData["domains"].AsArray()) {
				Domains.Add(domain);
			}
		}

		internal HttpRequest MakeHttpRequest(string path) {
			HttpRequest result = Cloud.MakeUnauthenticatedHttpRequest(path);
			string authInfo = GamerId + ":" + GamerSecret;
			result.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			return result;
		}

		internal Cloud Cloud;
		private GamerMatches MatchesInstance;
		#endregion
	}
}
