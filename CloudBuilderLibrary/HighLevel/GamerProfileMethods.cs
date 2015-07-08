using System;
using System.Text;
using System.Collections.Generic;

namespace CotcSdk
{
	/**
	 * Exposes methods allowing to fetch and modify the profile of the signed in gamer.
	 */
	public sealed class GamerProfileMethods {

		/**
		 * Method used to retrieve some optional data of the logged in profile previously set by
		 * method SetProfile.
		 * @return promise resolved when the operation has completed.
		 */
		public Promise<GamerProfile> Get() {
			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/profile");
			return Common.RunInTask<GamerProfile>(req, (response, task) => {
				GamerProfile profile = new GamerProfile(response.BodyJson);
				task.PostResult(profile, response.BodyJson);
			});
		}

		/**
		 * Method used to associate some optional data to the logged in profile in a JSON dictionary.
		 * You can fill fields with keys "email", "displayName", "lang", "firstName", "lastName",
		 * "addr1", "addr2", "addr3" and "avatar". Other fields will be ignored. These fields must be
		 * strings, and some are pre-populated when the account is created, using the available info
		 * from the social network used to create the account.
		 * @return promise resolved when the operation has completed.
		 * @param data is a Bundle holding the data to save for this user. The object can hold the
		 *     whole profile or just a subset of the keys.
		 */
		public Promise<Done> Set(Bundle data) {
			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/profile");
			req.BodyJson = data;
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		#region Private
		internal GamerProfileMethods(Gamer parent) {
			Gamer = parent;
		}
		private Gamer Gamer;
		#endregion
	}
}
