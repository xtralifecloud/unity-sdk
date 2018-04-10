using UnityEngine;
using System.Collections;
using CotcSdk;
using UnityEngine.UI;
using CotcSdk.InappPurchase;
using System;

public class CotcInappPurchaseSampleScene : MonoBehaviour {
	// The cloud allows to make generic operations (non user related)
	private Cloud Cloud;
	// The gamer is the base to perform most operations. A gamer object is obtained after successfully signing in.
	private Gamer Gamer;
	// The friend, when fetched, can be used to send messages and such
	private string FriendId;
	// When a gamer is logged in, the loop is launched for domain private. Only one is run at once.
	private DomainEventLoop Loop;
	// Input field
	public InputField EmailInput;
	// Default parameters
	private const string DefaultEmailAddress = "me@localhost.localdomain";
	private const string DefaultPassword = "Pass1234";

	// Use this for initialization
	void Start() {
		// Link with the CotC Game Object
		var cb = FindObjectOfType<CotcGameObject>();
		if (cb == null) {
			Debug.LogError("Please put a Clan of the Cloud prefab in your scene!");
			return;
		}
		// Log unhandled exceptions (.Done block without .Catch -- not called if there is any .Then)
		Promise.UnhandledException += (object sender, ExceptionEventArgs e) => {
			Debug.LogError("Unhandled exception: " + e.Exception.ToString());
		};
		// Initiate getting the main Cloud object
		cb.GetCloud().Done(cloud => {
			Cloud = cloud;
			// Retry failed HTTP requests once
			Cloud.HttpRequestFailedHandler = (HttpRequestFailedEventArgs e) => {
				if (e.UserData == null) {
					e.UserData = new object();
					e.RetryIn(1000);
				}
				else
					e.Abort();
			};
			Debug.Log("Setup done");
		});
		// Use a default text in the e-mail address
		EmailInput.text = DefaultEmailAddress;
	}
	
	// Signs in with an anonymous account
	public void DoLogin() {
		// Call the API method which returns an Promise<Gamer> (promising a Gamer result).
		// It may fail, in which case the .Then or .Done handlers are not called, so you
		// should provide a .Catch handler.
		Cloud.LoginAnonymously()
			.Then(gamer => DidLogin(gamer))
			.Catch(ex => {
				// The exception should always be CotcException
				CotcException error = (CotcException)ex;
				Debug.LogError("Failed to login: " + error.ErrorCode + " (" + error.HttpStatusCode + ")");
			});
	}

	// Log in by e-mail
	public void DoLoginEmail() {
		// You may also not provide a .Catch handler and use .Done instead of .Then. In that
		// case the Promise.UnhandledException handler will be called instead of the .Done
		// block if the call fails.
		Cloud.Login(
			network: LoginNetwork.Email.Describe(),
			networkId: EmailInput.text,
			networkSecret: DefaultPassword)
		.Done(this.DidLogin);
	}

	// Converts the account to e-mail
	public void DoConvertToEmail() {
		if (!RequireGamer()) return;
		Gamer.Account.Convert(
			network: LoginNetwork.Email.ToString().ToLower(),
			networkId: EmailInput.text,
			networkSecret: DefaultPassword)
		.Done(dummy => {
			Debug.Log("Successfully converted account");
		});
	}

	// Fetches a friend with the given e-mail address
	public void DoFetchFriend() {
		string address = EmailInput.text;
		Cloud.ListUsers(filter: address)
		.Done(friends => {
			if (friends.Total != 1) {
				Debug.LogWarning("Failed to find account with e-mail " + address + ": " + friends.ToString());
			}
			else {
				FriendId = friends[0].UserId;
				Debug.Log(string.Format("Found friend {0} ({1} on {2})", FriendId, friends[0].NetworkId, friends[0].Network));
			}
		});
	}

	// Sends a message to the current friend
	public void DoSendMessage() {
		if (!RequireGamer() || !RequireFriend()) return;

		Gamer.Community.SendEvent(
			gamerId: FriendId,
			eventData: Bundle.CreateObject("hello", "world"),
			notification: new PushNotification().Message("en", "Please open the app"))
		.Done(dummy => Debug.Log("Sent event to gamer " + FriendId));
	}

	// Posts a sample transaction
	public void DoPostTransaction() {
		if (!RequireGamer()) return;

		Gamer.Transactions.Post(Bundle.CreateObject("gold", 50))
		.Done(result => {
			Debug.Log("TX result: " + result.ToString());
		});
	}

	public void DoListProducts() {
		var inApp = FindObjectOfType<CotcInappPurchaseGameObject>();
		if (!RequireGamer()) return;

		var pp = new PurchasedProduct[1];
		var result = new ValidateReceiptResult[1];
		Gamer.Store.ListConfiguredProducts()
			.Then(products => {
				Debug.Log("Got BO products.");
				return inApp.FetchProductInfo(products);
			})
			.Then(enrichedProducts => {
				Debug.Log("Received enriched products");
				foreach (ProductInfo pi in enrichedProducts) {
					Debug.Log(pi.ToJson());
				}

				// Purchase the first one
				return inApp.LaunchPurchase(Gamer, enrichedProducts[0]);
			})
			.Then(purchasedProduct => {
				Debug.Log("Purchase ok: " + purchasedProduct.ToString());
				pp[0] = purchasedProduct;
				return Gamer.Store.ValidateReceipt(purchasedProduct.StoreType, purchasedProduct.CotcProductId, purchasedProduct.PaidPrice, purchasedProduct.PaidCurrency, purchasedProduct.Receipt);
			})
			.Then(validationResult => {
				Debug.Log("Validated receipt");
				result[0] = validationResult;
				return inApp.CloseTransaction(pp[0]);
			})
			.Then(done => {
				Debug.Log("Purchase transaction completed successfully: " + result[0].ToString());
			})
			.Catch(ex => {
				Debug.LogError("Error during purchase: " + ex.ToString());
			});
	}

	public void DoAcknowledgePendingPurchase() {
		var inApp = FindObjectOfType<CotcInappPurchaseGameObject>();
		if (!RequireGamer()) return;
		
		Gamer.Store.ListConfiguredProducts()
			.Then(products => {
				Debug.Log("Got BO products.");
				return inApp.FetchProductInfo(products);
			})
			.Then(enrichedProducts => {
				Debug.Log("Received enriched products");
				// Purchase the first one
				return inApp.LaunchPurchase(Gamer, enrichedProducts[0]);
			})
			.Then(purchasedProduct => {
				Debug.Log("Purchase ok: " + purchasedProduct.ToString() + ", closing it without sending it to CotC");
				// Important: we should be validating the purchase through CotC. The thing is, under some circumstances
				// (test orders, certificate not yet configured on our BO) it might be refused by our server.
				// This button acknowledges it locally. Never do that in production.
				return inApp.CloseTransaction(purchasedProduct);
			})
			.Then(done => {
				Debug.Log("Purchase closed successfully");
			})
			.Catch(ex => {
				Debug.LogError("Error during purchase: " + ex.ToString());
			});
	}

	// Invoked when any sign in operation has completed
	private void DidLogin(Gamer newGamer) {
		if (Gamer != null) {
			Debug.LogWarning("Current gamer " + Gamer.GamerId + " has been dismissed");
			Loop.Stop();
		}
		Gamer = newGamer;
		Loop = Gamer.StartEventLoop();
		Loop.ReceivedEvent += Loop_ReceivedEvent;
		Debug.Log("Signed in successfully (ID = " + Gamer.GamerId + ")");
	}

	private void Loop_ReceivedEvent(DomainEventLoop sender, EventLoopArgs e) {
		Debug.Log("Received event of type " + e.Message.Type + ": " + e.Message.ToJson());
	}

	private bool RequireGamer() {
		if (Gamer == null)
			Debug.LogError("You need to login first. Click on a login button.");
		return Gamer != null;
	}

	private bool RequireFriend() {
		if (FriendId == null)
			Debug.LogError("You need to fetch a friend first. Fill the e-mail address field and click Fetch Friend.");
		return FriendId != null;
	}
}
