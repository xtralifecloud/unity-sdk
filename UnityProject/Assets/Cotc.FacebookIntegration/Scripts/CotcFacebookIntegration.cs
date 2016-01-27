using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Facebook.Unity;

namespace CotcSdk.FacebookIntegration
{
	/// <summary>Facebook integration utility root entry point.</summary>
	[Serializable]
	public class CotcFacebookIntegration : MonoBehaviour {

		// Use this for initialization
		void Start() {
			// Do not init twice.
			if (FB.IsInitialized) {
				Inited();
			}
			else {
				FB.Init(() => Inited());
			}
		}

		void OnGUI() {
			if (!FbIsLoaded) return;
			lock (DoOnGUI) {
				// Run pending actions and resolve promises
				foreach (var a in DoOnGUI) {
					a();
				}
				DoOnGUI.Clear();
			}
		}

		/// <summary>
		/// Logs in to CotC through facebook. This will bring an user interface allowing to sign in
		/// to facebook.
		/// </summary>
		/// <returns>task returning when the login has finished. The resulting Gamer object can then
		/// be used for many purposes related to the signed in account.</returns>
		/// <param name="cloud">needed to perform various tasks. Ensure that the SDK is initialized properly and fetch a
		/// cloud object.</param>
		public Promise<Gamer> LoginWithFacebook(Cloud cloud) {
			var task = new Promise<Gamer>();

			LoginToFacebook(new List<string>() { "public_profile","email","user_friends" }).Then(aToken => {
				string userId = aToken.UserId, token = aToken.TokenString;
				Common.Log("Logged in through facebook");
				cloud.Login(LoginNetwork.Facebook, userId, token)
					.ForwardTo(task);
			}).Catch(ex => {
				task.Reject(ex);
			});
			return task;
		}

		/// <summary>
		/// Logs in to Facebook and returns an access token (FB API) that can be used to log in with CotC.
		/// </summary>
		/// <returns>task returning a facebook access token in case of success.</returns>
		/// <param name="permissions">List of permissions to request for.</param>
		public Promise<AccessToken> LoginToFacebook(List<string> permissions) {
			var task = new Promise<AccessToken>();
			DoOnGUI.Add(() => {
				FB.LogInWithReadPermissions(permissions, (ILoginResult result) => {
					if (result.Error != null) {
						task.PostResult(ErrorCode.SocialNetworkError, "Facebook/ " + result.Error);
					}
					else if (!FB.IsLoggedIn) {
						task.PostResult(ErrorCode.LoginCanceled, "Login canceled");
					}
					else {
						task.PostResult(AccessToken.CurrentAccessToken);
					}
				});
			});
			return task;
		}

		/// <summary>
		/// Fetches the list of friends on facebook and sends them to CotC so that they automatically become friend with you.
		/// Note that this can only fetch the friends who are actually playing the game, so the list may be empty especially
		/// when in development.
		/// </summary>
		/// <returns>Promise resolved when the request has finished. The value is as returned by
		/// #CotcSdk.GamerCommunity.ListNetworkFriends.</returns>
		/// <param name="gamer">Gamer object used to link the data to the account.</param>
		/// <param name="automatching">If true, synchronizes the CotC friends with your facebook friends. They will be
		/// reported by ListFriends and such).</param>
		public Promise<SocialNetworkFriendResponse> FetchFriends(Gamer gamer, bool automatching = true) {
			var task = new Promise<SocialNetworkFriendResponse>();
			EnsureFacebookLoaded(() => {
				DoFacebookRequestWithPagination("/me/friends", HttpMethod.GET)
				.Then(result => {
					gamer.Community.ListNetworkFriends(LoginNetwork.Facebook, result, automatching)
						.ForwardTo(task);
				})
				.Catch(ex => {
					task.PostResult(ErrorCode.SocialNetworkError, "Facebook request failed");
				});
			});
			return task;
		}

		/// <summary>Shares a short story consisting of a link and some information on facebook.</summary>
		/// <returns>A promise with a bundle optionally containing {postId: "<the post ID on facebook>"}.</returns>
		/// <param name="url">The URL to which this post should link.</param>
		/// <param name="title">The desired title of the content in the link.</param>
		/// <param name="description">A short description, rendered below title in the story.</param>
		/// <param name="photoUrl">A URL for the thumbnail image that appears on the post.</param>
		public Promise<Bundle> ShareLink(string url = null, string title = "", string description = "", string photoUrl = null) {
			var task = new Promise<Bundle>();
			DoOnGUI.Add(() => {
				Uri uri = url != null ? new Uri(url) : null;
				Uri photoUri = photoUrl != null ? new Uri(photoUrl) : null;
				FB.ShareLink(uri, title, description, photoUri, shareResult => {
					if (shareResult.Cancelled || !String.IsNullOrEmpty(shareResult.Error)) {
						task.PostResult(ErrorCode.Canceled, "Facebook share canceled");
					}
					else if (!String.IsNullOrEmpty(shareResult.PostId)) {
						// Print post identifier of the shared content
						task.PostResult(Bundle.CreateObject("postId", shareResult.PostId));
					}
					else {
						// Share succeeded without postID
						task.PostResult(Bundle.Empty);
					}
				});
			});
			return task;
		}

		#region Private
		// Starting point
		private Promise<List<SocialNetworkFriend>> DoFacebookRequestWithPagination(string query, HttpMethod method) {
			var task = new Promise<List<SocialNetworkFriend>>();
			FB.API(query, method, (IGraphResult result) => {
				DoFacebookRequestWithPagination(task, result, new List<SocialNetworkFriend>());
			});
			return task;
		}

		// Recursive
		private void DoFacebookRequestWithPagination(Promise<List<SocialNetworkFriend>> task, IGraphResult result, List<SocialNetworkFriend> addDataTo) {
			if (result.Error != null) {
				Common.LogWarning("Error in facebook request: " + result.Error.ToString());
				task.PostResult(ErrorCode.SocialNetworkError, "Facebook/ Network #1");
				return;
			}

			// Gather the result from the last request
			try {
				Common.Log("FB response: " + result.RawResult);
				Bundle fbResult = Bundle.FromJson(result.RawResult);
				List<Bundle> data = fbResult["data"].AsArray();
				foreach (Bundle element in data) {
					addDataTo.Add(new SocialNetworkFriend(element["id"], element["first_name"], element["last_name"], element["name"]));
				}
				string nextUrl = fbResult["paging"]["next"];
				// Finished
				if (data.Count == 0 || nextUrl == null) {
					task.PostResult(addDataTo);
					return;
				}

				FB.API(nextUrl, HttpMethod.GET, (IGraphResult res) => {
					DoFacebookRequestWithPagination(task, res, addDataTo);
				});
			}
			catch (Exception e) {
				Common.LogError("Error decoding FB data: " + e.ToString());
				task.PostResult(ErrorCode.SocialNetworkError, "Decoding facebook data: " + e.Message);
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

		private void Inited() {
			Common.Log("FB initialized properly.");
			lock (DoWhenFbLoaded) {
				foreach (Action a in DoWhenFbLoaded) {
					a();
				}
				DoWhenFbLoaded.Clear();
				FbIsLoaded = true;
			}
		}

		private List<Action> DoWhenFbLoaded = new List<Action>();
		// Required because of facebook 7.3.0's bugs on Unity 5.
		// Executing methods here also mean that they wait for facebook initialization.
		private List<Action> DoOnGUI = new List<Action>();
		private bool FbIsLoaded = false;
		#endregion
	}
}
