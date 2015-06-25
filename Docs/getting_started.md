Getting started {#getting_started_ref}
===========

The Unity SDK is cut in several parts:
- The core library, which contains all the core classes and communicates with our servers. It is small, has no dependencies and provides almost all functionality.
- The facebook integration library. It depends on the facebook SDK for Unity and provides facebook related functionality. The integration library is provided as a separate package.

In order to get started without third party login support, you can simply download the Unity SDK library package. This package also includes a sample scene + script that you may include or not, as shown below.

![Import package screen](./Docs/img/importpackage01.png)

You can deselect SampleScript.cs and SampleScene.unity if you do not need the sample code. After having imported the package, you just need to configure the SDK settings. For that, open the SampleScene and select the Clan of the Cloud SDK object. In the inspector, set the appropriate values (API Key and API Secret) as configured in the backoffice.

Note: if you want to start from zero, you may simply add a new scene and drag&drop the Clan of the Cloud SDK prefab object from the CotC/Prefabs folder into your scene. This object needs to be placed on any scene where you want to use Clan of the Cloud functionality.

# Usage

Basic usage is provided by the Clan of the Cloud SDK prefab object. You just have to put it on your scene and invoke the GetCloud method on it to fetch a #CotcSdk.Cloud object allowing to use most features. For that, you may simply use `FindObjectOfType<CotcGameObject>()`.

~~~~{.cs}
	private Cloud Cloud;
	
	void Setup() {
		cb.GetCloud().Done(cloud => {
			Cloud = cloud;
		});
	}
~~~~

This code will fetch a cloud object. This operation is asynchronous and may take a bit of time or operate instantly, depending on various conditions. You should do it at startup as shown above and keep it as a member of your class.

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

# Promises

All functions are asynchronous. Due to the current lack of async/await functionality in Unity, we introduced an asynchronous programming pattern borrowed from Javascript, called promises. You can get more info by [https://promisesaplus.com/](clicking here).

Getting used to promises can take a bit of time, but in a nutshell, you may simply use them as an optional callback as shown below.

~~~~{.cs}
	// Usual callback-based API
	void LoginAnonymously(Action<Gamer> onSuccess, Action<Exception> onFailure);
	LoginAnonymously(
		onSuccess: result => { ... }
		onFailure: ex => { ... }
	);
	
	// Promise based API (CotC)
	IPromise<Gamer> LoginAnonymously();
	LoginAnonymously()
		.Then(result => { ... })
		.Catch(ex => { ... });
~~~~

The basic principle means that any method of the API will return an `IPromise<Type>` object, which promises to give a result of that type in the future. The `IPromise<>` object provides a few methods which will help:

- Do something when the promise resolves (i.e. a result is given as promised),
- Do something when the promise is rejected (i.e. the result can not be provided as promised).

Let's take an example:

~~~~{.cs}
	// Example method:
	IPromise<Gamer> LoginAnonymously();
	
	// Do the API call. Will launch the login process.
	IPromise<Gamer> gamerPromise = LoginAnonymously();
	// Here, the promise may already have been resolved (login done), though unknown to us.
	// We just tell that we have something to do when the promise gets resolved.
	// The handler will either be invoked immediately or once logged in.
	gamerPromise = gamerPromise.Then((Gamer result) => {
		...
	});
	// The Done (or Then) handlers won't get called in case the promise is rejected
	// (network error…), so we can (and should) provide a Catch block to handle exceptions.
	// They will come up asynchronously as well, so the principle is exactly the same.
	gamerPromise = gamerPromise.Catch((Exception ex) => {
		...
	});
~~~~

Note that the Then and Catch block return a new promise that can itself be linked to another Then/Catch block. However you should provide a Catch block only after all Then blocks or just before a Done block. The Done method returns no promise, and just tells that it is the last thing that you will do with the result. If you provide a Then block, you may then:

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
	});
~~~~

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

# Network error handlers

In case a network error happens, the request is not retried by default. But there is a `HttpRequestFailedHandler` member on Cloud which can be set to an user defined callback. This callback tells what to do with the error (retry it, cancel it). The following code retries any failed request twice, once after 100ms, once after 5s, then aborts it.

~~~~{.cs}
	const int RetryTimes = {100 /* ms */, 5000 /* ms */};
	cloud.HttpRequestFailedHandler = (HttpRequestFailedEventArgs e) => {
		// Store retry count in UserData field (persisted among retries of a given request)
		int retriedCount = e.UserData != null ? (int)e.UserData : 0;
		e.UserData = retriedCount + 1;
		if (retriedAlready >= RetryTimes.Length)
			e.Abort();
		else
			e.RetryIn(RetryTimes[retriedAlready]);
	};
~~~~

This should be done at the very startup, after the cloud has been received. The handler is chosen when an HTTP request is built, so if you change it while an HTTP request is running, it will have no effect.

# Bundle

Some function calls use bundles. They act as a generic, typed dictionary. Read more at #CotcSdk.Bundle.
