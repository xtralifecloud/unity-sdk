Getting started {#getting_started_ref}
===========

The Unity SDK is available at the following URL:
https://github.com/clanofthecloud/unity-sdk/releases.

We decided to cut it in several parts:
- **The core library**, which contains all the core classes and communicates with our servers. It is small, has no dependencies and provides almost all functionality.
- **The facebook integration library**. It depends on the facebook SDK for Unity and provides facebook related functionality. The integration library is provided as a separate package.
- **The push notification library**, which provides that functionality, currently for Android and iOS.
- **The in-app purchase library**, which provides the corresponding functionality, currently for Android and iOS.

Packages are distributed separately, although we zipped them together due to their relatively small size. Apart from facebook, they also include all their respective dependencies.

![Import package screen](./Docs/img/importpackage01.png)

You can deselect `SampleScript.cs` and `SampleScene.unity` if you do not need the sample code. After having imported the package, you just need to configure the SDK settings. For that, open the SampleScene and select the Clan of the Cloud SDK object. In the inspector, set the appropriate values (API Key and API Secret) as configured in the backoffice.

Note: if you want to start from zero, you may simply add a new scene and drag & drop the `Clan of the Cloud SDK` prefab object from the `CotC/Prefabs` folder into your scene. This object needs to be placed on any scene where you want to use Clan of the Cloud functionality.

Note: if you include any package which provides additional functionality, you need to proceed to its configuration prior to the first compilation. For instance, if you are importing the facebook related package, you will need to import the facebook package as described in the [Facebook](#facebook_ref) section. Importing only the core package is the best way to quickly test SDK functionality in your project.

![Import package screen](./Docs/img/importpackage02.png)

# Configuring

Basic usage is provided by the Clan of the Cloud SDK prefab object. You just have to put it on your scene(s). When creating your project, you can select this object (any instance on any scene) and use the interface under the inspector to set up the CotC credentials as shown below. These credentials identify your game and are used to scope data to your game. You may also configure additional settings.

![Configuring the SDK via the CotC Game Object](./Docs/img/cotc-game-object-1.png)

The available settings are:
- **API Key:** the API key as described on the web interface when you registered to Clan of the Cloud
- **API Secret:** same as API key, but second part of the identifier.
- **Environment:** CotC comes with two environments: a "production" environment and a "sandbox" environment. During the game development and testing phase, you will use the sandbox environment, and then switch to the production environment when releasing the game to the public. It brings improved reliability and performance and allows to start out with fresh data for your customers.
- **Verbose logging:** outputs detailed information about all web requests made to the servers. Allows for finer debugging but is not recommended outside of alpha state as it may pollute the log.
- **Request timeout:** the timeout for web requests (in seconds).
- **HTTP client:** by default, CotC uses the standard HTTP client supplied with Mono (since the WWW client lacks required functionality). It works well and is supported on all platforms (except the web player), although it has a few quirks. Typically on some platforms it only supports basic security (HTTPS), and on iOS warnings will be issued in the console. On the other hand, the UnityWebRequest client is all new, doesn't have this problems but is only supported on most platforms since Unity 5.3, and doesn't support Keep-Alive yet, reducing the performance when making frequent calls to the server.

# Usage {#cotcgameobject_ref}

Functionality is provided through the Clan of the Cloud SDK prefab object, that should be present on every scene. It is not visible, therefore the position does not matter. Invoke the GetCloud method on it to fetch a #CotcSdk.Cloud object allowing to use most features. For that, you may simply use [FindObjectOfType<CotcGameObject>](http://docs.unity3d.com/ScriptReference/Object.FindObjectOfType.html).

~~~~{.cs}
using CotcSdk;

class MyGameObject: MonoBehaviour {
	private Cloud Cloud;
	
	void Startup() {
		var cb = FindObjectOfType<CotcGameObject>();
		cb.GetCloud().Done(cloud => {
			Cloud = cloud;
		});
	}
}
~~~~

This code will fetch a cloud object. This operation is asynchronous but will usually take only a very small amount of time, since it only waits for the CotcGameObject to be initialized. You should do it at startup as shown above and keep it as a member of your class.

Another very important object is the #CotcSdk.Gamer object. This represents a signed in user, and provides all functionality that requires to be logged in. You will obtain it by logging in through the cloud object.

~~~~{.cs}
	Cloud.LoginAnonymously()
	.Catch(ex => {
		Debug.LogError("Login failed: " + ex.ToString());
	})
	.Done(gamer => {
		Debug.Log("Signed in successfully (ID = " + gamer.GamerId + ")");	});
	});
~~~~

Please read the section after promises for information about anonymous login, and what to set up next once logged in.


# Promises {#promises_ref}

All functions are asynchronous. Due to the current lack of async/await functionality in Unity, we introduced an asynchronous programming pattern borrowed from Javascript, called promises. You can get more info by [clicking here](https://promisesaplus.com/).

Getting used to promises can take a bit of time, but in a nutshell, you may simply use them as an optional callback as shown below.

~~~~{.cs}
	// Usual callback-based API
	void LoginAnonymously(Action<Gamer> onSuccess, Action<Exception> onFailure);
	LoginAnonymously(
		onSuccess: result => { ... }
		onFailure: ex => { ... }
	);
	
	// Promise based API (CotC)
	Promise<Gamer> LoginAnonymously();
	LoginAnonymously()
		.Then(result => { ... })
		.Catch(ex => { ... });
~~~~

The basic principle means that any method of the API will return an @ref CotcSdk.Promise<PromisedT> "Promise<Type> object", which promises to give a result of that type in the future. The `Promise` object provides a few methods which will help:

- Do something when the promise resolves (i.e. a result is given as promised),
- Do something when the promise is rejected (i.e. the result can not be provided as promised).

Let's take an example:

~~~~{.cs}
	// Example method:
	Promise<Gamer> LoginAnonymously();
	
	// Do the API call. Will launch the login process.
	Promise<Gamer> gamerPromise = LoginAnonymously();
	// Here, the promise may already have been resolved (login done), though unknown to us.
	// We just tell that we have something to do when the promise gets resolved.
	// The handler will either be invoked immediately or once logged in.
	gamerPromise = gamerPromise.Then((Gamer result) => {
		...
	});
	// The Done (or Then) handlers won't get called in case the promise is rejected
	// (network error...), so we can (and should) provide a Catch block to handle exceptions.
	// They will come up asynchronously as well, so the principle is exactly the same.
	gamerPromise = gamerPromise.Catch((Exception ex) => {
		...
	});
~~~~

Note that the @ref CotcSdk.Promise<PromisedT>.Then "Then" and @ref CotcSdk.Promise<PromisedT>.Catch "Catch" block return a new promise that can itself be linked to another Then/Catch block. However you should provide a Catch block only after all Then blocks or just before a Done block. The @ref CotcSdk.Promise<PromisedT>.Done "Done" method returns no promise, and just tells that it is the last thing that you will do with the result. If you provide a Then block, you may then:

- Provide other Then blocks, which will receive the same result (if the lambda provided in the Then block returns an empty result).
- Handle the result returned by the next promise (in case the lambda provided in the previous Then block returned another Promise).

Let's show an example:

~~~~{.cs}
	// Chaining Then blocks
	Cloud.LoginAnonymously()
	.Then(gamer => {
		Debug.Log("Signed in successfully (ID = " + gamer.GamerId + ")");	});
	})
	.Then(gamer => {
		Debug.Log("Here again with the same gamer " + gamer.GamerId);
	})
	.Catch(ex => {
		Debug.LogError("Login failed: " + ex.ToString());
	})
	.Done();
~~~~

Providing a Done block at the end is not mandatory. Doing so will just ensure that the @ref CotcSdk.Promise.UnhandledException "Unhandled exception handler" is called in case you do not provide a Catch block. That is why we prefer the use of Done over Then in our examples: it will prevent errors from being eaten up silently. We recommend that you always provide a Catch block to handle the exceptional behaviour, or end your chain with `.Done()` as shown above.

But then there is better, let us say that we want to log in the user and then get his profile. We can return another promise from the Then block and provide a Catch block that will be invoked if either of the calls failed.

~~~~{.cs}
	// Chaining multiple operations
	Cloud.LoginAnonymously()
	.Then(gamer => {
		Debug.Log("Signed in successfully (ID = " + gamer.GamerId + ")");	});
		// Then read the profile
		return gamer.Profile.Get();
	})
	.Then(profile => {
		Debug.Log("Got user profile: " + profile["displayName"]);
		// Nothing to do next
	})
	.Catch(ex => {
		Debug.LogError("Either login or get profile failed: " + ex.ToString());
	});
~~~~

Note that the above result could technically be achieved the following way as well:

~~~~{.cs}
	// Log unhandled exceptions (.Done block without .Catch -- not called if there is any .Then)
	Promise.UnhandledException += (object sender, ExceptionEventArgs e) => {
		Debug.LogError("Any operation failed: " + e.Exception.ToString());
	};
	// Chaining multiple operations
	Cloud.LoginAnonymously().Done(gamer => {
		Debug.Log("Signed in successfully (ID = " + gamer.GamerId + ")");	});
		// Then read the profile
		gamer.Profile.Get().Done(profile => {
			Debug.Log("Got user profile: " + profile["displayName"]);
		});
	});
~~~~

The exceptions provided by the API in Catch blocks are always of type `CotcException`. However, note that if an exception happens in one of the Then blocks (e.g. error in your handling code), the exception will be reported to the next Catch block.

~~~~{.cs}
	Cloud.LoginAnonymously()
	.Then(gamer => {
		throw new Exception();
	});
	.Catch(ex => {
		if (ex is CotcException) {
			Debug.LogError("API call failed: " + ((CotcException)ex).ErrorCode);
		}
		else {
			Debug.LogError("Error in my code: " + ex.ToString());
		}
	});
~~~~

# Login basics and Domain Event Loops

Logging in anonymously allows the current user to get access to the CotC functionality without providing any credential, as typically happens the first time. The constructed #CotcSdk.Gamer object will contain credentials (GamerId, GamerSecret) which allow to log the gamer back using the #CotcSdk.Cloud.Login call. Thus, we recommend you to log in anonymously the first time, and then store the credentials, via `PlayerPrefs` for example.

~~~~{.cs}
	// First time
	if (!PlayerPrefs.HasKey("GamerId") || !PlayerPrefs.HasKey("GamerSecret")) {
		Cloud.LoginAnonymously()
		.Catch(ex => Debug.LogError("Login failed: " + ex.ToString()))
		.Done(gamer => {
			// Persist returned credentials for next time
			PlayerPrefs.SetString("GamerId", gamer.GamerId);
			PlayerPrefs.SetString("GamerSecret", gamer.GamerSecret);
			DidLogin(gamer);
		});
	}
	else {
		// Anonymous network type allows to log back with existing credentials
		Cloud.Login(
			network: LoginNetwork.Anonymous,
			networkId: PlayerPrefs.GetString("GamerId"),
			networkSecret: PlayerPrefs.GetString("GamerSecret"))
		.Catch(ex => Debug.LogError("Login failed: " + ex.ToString()))
		.Done(gamer => {
			// ... (logged in)
			DidLogin(gamer);
		});
	}
~~~~

Once logged in, you should also start a domain event loop, which consists of a background network thread to receive network events (messages from other gamers, match events, etc.). You will also likely attach a delegate to the #CotcSdk.DomainEventLoop.ReceivedEvent event, raised when an event is received (such as @ref CotcSdk.GamerCommunity.SendEvent "a message from another player").

~~~~
	// (Class member)
	DomainEventLoop Loop = null;

	void DidLogin(Gamer newGamer) {
		// Another loop was running; unless you want to keep multiple users active, stop the previous
		if (Loop != null)
			Loop.Stop();

		Loop = newGamer.StartEventLoop();
		Loop.ReceivedEvent += Loop_ReceivedEvent;
	}

	void Loop_ReceivedEvent(DomainEventLoop sender, EventLoopArgs e) {
		Debug.Log("Received event of type " + e.Message.Type + ": " + e.Message.ToJson());
	}
~~~~

Since you may log in as many times as you want, what really makes a gamer "active" is the fact that an event loop is running for him. If you want to dismiss (log out) a gamer, you can simply stop the loop and drop your reference to the gamer object.

# Network error handlers

In case a network error happens, the request is not retried by default. But there is a `HttpRequestFailedHandler` member on Cloud which can be set to an user defined callback. This callback tells what to do with the error (retry it, cancel it). The following code retries any failed request twice, once after 100ms, once after 5s, then aborts it.

~~~~{.cs}
	int[] RetryTimes = { 100 /* ms */, 5000 /* ms */};
	cloud.HttpRequestFailedHandler = (HttpRequestFailedEventArgs e) => {
		// Store retry count in UserData field (persisted among retries of a given request)
		int retryCount = e.UserData != null ? (int)e.UserData : 0;
		e.UserData = retryCount + 1;
		if (retryCount >= RetryTimes.Length)
			e.Abort();
		else
			e.RetryIn(RetryTimes[retryCount]);
	};
~~~~

This should be done at the very startup, after the cloud has been received. The handler is chosen when an HTTP request is built, so if you change it while an HTTP request is running, it will have no effect.

# Bundle

Some function calls use bundles. They act as a generic, typed dictionary. Read more at #CotcSdk.Bundle.

