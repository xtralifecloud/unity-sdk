using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace CloudBuilderLibrary
{
	[Serializable]
	public class CloudBuilderFacebookIntegration : MonoBehaviour {
		[SerializeField]
		public string AppId;

		// Use this for initialization
		void Start() {
			if (string.IsNullOrEmpty(AppId)) {
				Debug.LogError("Facebook Credentials missing, facebook integration will NOT work.");
				return;
			}

			FB.Init(() => {
				Debug.Log("FB initialized properly.");
				lock (DoWhenFbLoaded) {
					foreach (Action a in DoWhenFbLoaded) {
						a();
					}
					DoWhenFbLoaded.Clear();
					FbIsLoaded = true;
				}
			}, AppId);
		}

		/**
		 * Logs in to CotC through facebook. This will bring an user interface allowing to sign in
		 * to facebook.
		 * @param done callback invoked when the login has finished, either successfully or not. The resulting Gamer
		 *     object can then be used for many purposes related to the signed in account.
		 * @param clan needed to perform various tasks. Ensure that CloudBuilder is initialized properly and fetch a
		 *     clan object.
		 */
		public void LoginWithFacebook(ResultHandler<Gamer> done, Clan clan) {
			EnsureFacebookLoaded(() => {
				FB.Login("public_profile,email,user_friends", (FBResult result) => {
					if (result.Error != null) {
						Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Facebook/ " + result.Error);
					}
					else if (!FB.IsLoggedIn) {
						Common.InvokeHandler(done, ErrorCode.LoginCanceled);
					}
					else {
						string userId = FB.UserId, token = FB.AccessToken;
						Debug.Log("Logged in through facebook");
						clan.Login(done, LoginNetwork.Facebook, userId, token);
					}
				});
			});
		}

		/**
		 * Fetches the list of friends on facebook and sends them to CotC so that they automatically become friend with you.
		 * Note that this can only fetch the friends who are actually playing the game, so the list may be empty especially
		 * when in development.
		 * @param done callback invoked when the request has finished. The boolean value returned indicates success if true.
		 * @param gamer gamer object used to link the data to the account.
		 */
		public void FetchFriends(ResultHandler<bool> done, Gamer gamer) {
			EnsureFacebookLoaded(() => {
				DoFacebookRequestWithPagination((Result<List<SocialNetworkFriend>> result) => {
					gamer.PostSocialNetworkFriends(done, LoginNetwork.Facebook, result.Value);
				}, "/me/friends", Facebook.HttpMethod.GET);
			});
		}

		#region Private
		// Starting point
		private void DoFacebookRequestWithPagination(ResultHandler<List<SocialNetworkFriend>> done, string query, Facebook.HttpMethod method) {
			FB.API(query, method, (FBResult result) => {
				DoFacebookRequestWithPagination(done, result, new List<SocialNetworkFriend>());
			});
		}

		// Recursive
		private void DoFacebookRequestWithPagination(ResultHandler<List<SocialNetworkFriend>> done, FBResult result, List<SocialNetworkFriend> addDataTo) {
			if (result.Error != null) {
				Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Facebook/ Network #1");
				return;
			}

			// Gather the result from the last request
			try {
				Bundle fbResult = Bundle.FromJson(result.Text);
				List<Bundle> data = fbResult["data"].AsArray();
				foreach (Bundle element in data) {
					addDataTo.Add(new SocialNetworkFriend(element["id"], element["first_name"], element["last_name"], element["name"]));
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
				Debug.LogError("Error decoding FB data: " + e.ToString());
				Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Decoding facebook data: " + e.Message);
				return;
			}
		}

		private void EnsureFacebookLoaded(Action a) {
			lock (DoWhenFbLoaded) {
				// Can do immediately
				if (FbIsLoaded) {
					a();
					return;
				}
				// Need to enqueue for later
				DoWhenFbLoaded.Add(a);
			}
		}

		private List<Action> DoWhenFbLoaded = new List<Action>();
		private bool FbIsLoaded = false;
		#endregion
	}
}
