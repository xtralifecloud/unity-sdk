using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public partial class User {

		public class ProfileMethods {

			/**
			 * Method used to retrieve some optional data of the logged in profile previously set by
			 * method SetProfile.
			 * @param done callback invoked when the login has finished, either successfully or not.
			 */
			public void Get(ResultHandler<UserProfile> done) {
				HttpRequest req = User.MakeHttpRequest("/v1/gamer/profile");
				Directory.HttpClient.Run(req, (HttpResponse response) => {
					if (response.HasFailed) {
						Common.InvokeHandler(done, response);
						return;
					}

					UserProfile profile = new UserProfile(response.BodyJson);
					Common.InvokeHandler(done, profile, response.BodyJson);
				});
			}

			#region Internal
			internal ProfileMethods(User user) {
				User = user;
			}
			private User User;
			#endregion
		}
	}
}
