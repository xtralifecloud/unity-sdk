using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed partial class Gamer {

		/**
			* Method used to retrieve some optional data of the logged in profile previously set by
			* method SetProfile.
			* @param done callback invoked when the login has finished, either successfully or not.
			*/
		public void GetProfile(ResultHandler<GamerProfile> done) {
			HttpRequest req = MakeHttpRequest("/v1/gamer/profile");
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

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
		public void SetProfile(ResultHandler<bool> done, Bundle data) {
			HttpRequest req = MakeHttpRequest("/v1/gamer/profile");
			req.BodyJson = data;
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				Result<bool> result = new Result<bool>(response);
				if (!Common.HasFailed(response)) {
					result.Value = (response.BodyJson["done"] == 1);
				}
				Common.InvokeHandler(done, result);
			});
		}
	}
}
