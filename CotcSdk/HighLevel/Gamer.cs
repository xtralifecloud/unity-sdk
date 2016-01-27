using System;
using System.Collections.Generic;
using System.Text;

namespace CotcSdk
{
	/// @ingroup main_classes
	/// <summary>
	/// Important object from the SDK, allowing to perform many operations that depend on a currently logged in user.
	/// 
	/// This object is almost stateless. You may drop it without worrying about background processes that may still
	/// run. User related events are handled by a corresponding instance of DomainEventLoop, which should be started
	/// as soon as the user is logged in.
	/// </summary>
	public sealed partial class Gamer: PropertiesObject {

		public List<string> Domains { get; private set; }
		/// <summary>Gamer credential. Use it to gain access to user related tasks.</summary>
		public string GamerId { get; private set; }
		/// <summary>Gamer credential (secret). Same purpose as GamerId, and you will need those in pair.</summary>
		public string GamerSecret { get; private set; }
		public LoginNetwork Network { get; private set; }
		public string NetworkId { get; private set; }
		public DateTime RegisterTime { get; private set; }

		/// <summary>Provides account related functions for the current gamer.</summary>
		/// <returns>An object allowing to manipulate the account of the current gamer.</returns>
		public GamerAccountMethods Account {
			get { return new GamerAccountMethods(this); }
		}

		/// <summary>Provides an API to manipulate achievements.</summary>
		/// <returns>An object that allows to manipulate achievements.</returns>
		public GamerAchievements Achievements {
			get { return new GamerAchievements(this); }
		}

		/// <summary>Provides an API to run batches.</summary>
		/// <returns>An object that allows to manipulate batches.</returns>
		public GamerBatches Batches {
			get { return new GamerBatches(this); }
		}

		/// <summary>Provides an API to interact with friends on CotC.</summary>
		/// <returns>An object that allow to manipulate friends.</returns>
		public GamerCommunity Community {
			get { return new GamerCommunity(this); }
		}

		/// <summary>Returns an object that allows to manipulate the key/value system associated with this user.</summary>
		/// <returns>An object allowing to manipulate key/values for this user/domain.</returns>
		public GamerVfs GamerVfs {
			get { return new GamerVfs(this); }
		}

		/// <summary>Exposes functionality related to the godfathers.</summary>
		/// <returns>An object that allows to add a godfather, generate a code, etc.</returns>
		public GamerGodfather Godfather {
			get { return new GamerGodfather(this); }
		}

		/// <summary>
		/// Provides an API to manipulate matches (mainly start them, since working with
		/// existing matches is provided by the Match class).
		/// </summary>
		/// <returns>An object that allows to perform basic operations on matches.</returns>
		public GamerMatches Matches {
			get { MatchesInstance = MatchesInstance ?? new GamerMatches(this); return MatchesInstance; }
		}

		/// <summary>Allows to manipulate information related to the gamer profile.</summary>
		/// <returns>An object that allows to read and set the profile.</returns>
		public GamerProfileMethods Profile {
			get { return new GamerProfileMethods(this); }
		}

		/// <summary>Allows to manipulate the properties of the current gamer.</summary>
		/// <returns>An object that allows to set, delete, etc. property values.</returns>
		public GamerProperties Properties {
			get { return new GamerProperties(this); }
		}

		/// <summary>Provides an API able to handle functionality related to the leaderboards and scores.</summary>
		/// <returns>An object that allows to manipulate scores.</returns>
		public GamerScores Scores {
			get { return new GamerScores(this); }
		}

		/// <summary>
		/// Allows to list, buy products and so on. This functionality is low level and you should use the
		/// appropriate external plugin to help with the purchase process.
		/// </summary>
		/// <returns>An object that allows access to the store on a CotC point of view.</returns>
		public GamerStore Store {
			get { return new GamerStore(this); }
		}

		/// <summary>Allows to manipulate the transactions and related achievements of an user.</summary>
		/// <returns>An object that allows to manipulate transactions and query achievements.</returns>
		public GamerTransactions Transactions {
			get { return new GamerTransactions(this); }
		}

		/// <summary>
		/// Starts a DomainEventLoop in order to catch events related to this logged in gamer.
		/// 
		/// The loop will be running forever unless an error happens with this gamer (meaning that the
		/// gamer is not valid anymore, which can happen if he's not logged in). When stopping
		/// or pausing the application, you should call the corresponding methods on the loop to stop
		/// or pause it. The system will pause the loop automatically upon application pause and resume
		/// it as needed, which is done through the CotcGameObject as placed on your scene.
		///
		/// </summary>
		/// <param name="domain">Domain to listen on. The `private` domain is used to receive system notifications
		///     as well as messages sent by other players. Unless cross-game functionality is used, you
		///     should start one loop on the private domain as soon as the gamer is signed in.</param>
		/// <returns>A domain event loop that is in started state.</returns>
		public DomainEventLoop StartEventLoop(string domain = Common.PrivateDomain) {
			return new DomainEventLoop(this, domain).Start();
		}

		#region Internal
		/// <summary>Only instantiated internally.</summary>
		/// <param name="gamerData">Gamer data as returned by our API calls (loginanonymous, etc.).</param>
		internal Gamer(Cloud parent, Bundle gamerData) : base(gamerData) {
			Cloud = parent;
			Update(gamerData);
		}

		internal HttpRequest MakeHttpRequest(string path) {
			HttpRequest result = Cloud.MakeUnauthenticatedHttpRequest(path);
			string authInfo = GamerId + ":" + GamerSecret;
			result.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			return result;
		}

		internal void Update(Bundle updatedGamerData) {
			Network = Common.ParseEnum<LoginNetwork>(updatedGamerData["network"]);
			NetworkId = updatedGamerData["networkid"];
			GamerId = updatedGamerData["gamer_id"];
			GamerSecret = updatedGamerData["gamer_secret"];
			RegisterTime = Common.ParseHttpDate(updatedGamerData["registerTime"]);
			Domains = new List<string>();
			foreach (Bundle domain in updatedGamerData["domains"].AsArray()) {
				Domains.Add(domain);
			}
			Props = updatedGamerData;
		}

		internal Cloud Cloud;
		private GamerMatches MatchesInstance;
		#endregion
	}
}
