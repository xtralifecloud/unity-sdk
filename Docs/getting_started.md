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
		cb.GetCloud(cloud => {
			Cloud = cloud;
		});
	}
~~~~

This code will fetch a cloud object. This operation is asynchronous and may take a bit of time or operate instantly, depending on various conditions. You should do it at startup as shown above and keep it as a member of your class.

Another very important object is the #CotcSdk.Gamer object. This represents a signed in user, and provides all functionality that requires to be logged in. You will obtain it by logging in through the cloud object.

~~~~{.cs}
	Cloud.LoginAnonymously((Result<Gamer> result) => {
		if (!result.IsSuccessful)
			Debug.LogError("Login failed: " + result.ToString());
		else {
			Gamer = gamer.Value;
			Debug.Log("Signed in successfully (ID = " + Gamer.GamerId + ")");	});
		}
	}
~~~~

# Result handlers

All functions are asynchronous. They use a simple pattern with a delegate as an action to execute when finished, and get passed a `Result<Entity>` as a parameter. The result indicates success or failure, encloses an entity (whose type depends on the call) and may contain additional information reported by the server.

A delegate for a given Entity is called `ResultHandler<Entity>` and is basically simply a function with one argument of type `Result<Entity>`. The entity is obtained through the `Value` member of the `Result<Entity>`.

# Error handlers

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
