using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed partial class Gamer {

		public class ProfileMethods {

			/**
			 * Method used to retrieve some optional data of the logged in profile previously set by
			 * method SetProfile.
			 * @param done callback invoked when the login has finished, either successfully or not.
			 */
			public void Get(ResultHandler<GamerProfile> done) {
				HttpRequest req = User.MakeHttpRequest("/v1/gamer/profile");
				Directory.HttpClient.Run(req, (HttpResponse response) => {
					if (Common.HasFailed(response)) {
						Common.InvokeHandler(done, response);
						return;
					}

					GamerProfile profile = new GamerProfile(response.BodyJson);
					Common.InvokeHandler(done, profile, response.BodyJson);
				});
			}

			#region Internal
			internal ProfileMethods(Gamer user) {
				User = user;
			}
			private Gamer User;
			#endregion
		}
	}
}
