using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed partial class Gamer {

		/**
		 * Fetches the list of friends on facebook and sends them to CotC so that they automatically become friend with you.
		 * Note that this can only fetch the friends who are actually playing the game, so the list may be empty especially
		 * when in development.
		 * @param done callback invoked when the request has finished. The number of friends fetched on the social network
		 *     is attached as value.
		 */
		public void FetchFacebookFriends(ResultHandler<int> done) {
			DoFacebookRequestWithPagination((Result<Bundle> result) => {
				PostNetworkFriends(done, "facebook", result.Value);
			}, "/me/friends", Facebook.HttpMethod.GET);
		}

		#region Private
		// Starting point
		private void DoFacebookRequestWithPagination(ResultHandler<Bundle> done, string query, Facebook.HttpMethod method) {
			FB.API(query, method, (FBResult result) => {
				DoFacebookRequestWithPagination(done, result, Bundle.CreateArray());
			});
		}

		// Recursive
		private void DoFacebookRequestWithPagination(ResultHandler<Bundle> done, FBResult result, Bundle addDataTo) {
			if (result.Error != null) {
				Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Facebook/ Network #1");
				return;
			}

			// Gather the result from the last request
			try {
				Bundle fbResult = Bundle.FromJson(result.Text);
				List<Bundle> data = fbResult["data"].AsArray();
				foreach (Bundle element in data) {
					addDataTo.Add(element);
				}
				string nextUrl = fbResult["paging"]["next"];
				// Finished
				if (data.Count == 0 || nextUrl == null) {
					Common.InvokeHandler(done, addDataTo);
					return;
				}

				FB.API(nextUrl.Replace("https://graph.facebook.com", ""), Facebook.HttpMethod.GET, (FBResult res) => {
					DoFacebookRequestWithPagination(done, res, addDataTo);
				});
			}
			catch (Exception e) {
				CloudBuilder.Log(LogLevel.Warning, "Error decoding FB data: " + e.ToString());
				Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Decoding facebook data: " + e.Message);
				return;
			}
		}
		#endregion
	}
}
