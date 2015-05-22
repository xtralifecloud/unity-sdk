using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed class GamerProfileMethods {

		/**
		 * Method used to retrieve some optional data of the logged in profile previously set by
		 * method SetProfile.
		 * @param done callback invoked when the login has finished, either successfully or not.
		 */
		public void Get(ResultHandler<GamerProfile> done) {
			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/profile");
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				GamerProfile profile = new GamerProfile(response.BodyJson);
				Common.InvokeHandler(done, profile, response.BodyJson);
			});
		}

		/**
		 * Method used to associate some optional data to the logged in profile in a JSON dictionary.
		 * You can fill fields with keys "email", "displayName", "lang", "firstName", "lastName",
		 * "addr1", "addr2", "addr3" and "avatar". Other fields will be ignored. These fields must be
		 * strings, and some are pre-populated when the account is created, using the available info
		 * from the social network used to create the account.
		 * @param done callback invoked when the login has finished, either successfully or not. The
		 *     boolean value indicates whether the operation was completed on the server.
		 * @param data is a Bundle holding the data to save for this user. The object can hold the
		 *     whole profile or just a subset of the keys.
		 */
		public void Set(ResultHandler<bool> done, Bundle data) {
			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/profile");
			req.BodyJson = data;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"]);
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
