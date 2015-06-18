Facebook {#facebook_ref}
========

## Table of Content

1. [Facebook Developer](#toc1)
2. [Facebook SDK for Unity](#toc2)
3. [Using it](#toc3)


# Setup a Facebook App {#toc1}

To be able to log in Clan of the Cloud with the user's facebook account, you need to create a [Facebook App](http://developers.facebook.com/)

On Facebook Developer website, check the following settings :

![Facebook Settings](./img/FacebookDevelopers.png)

# Download the Facebook SDK for Unity {#toc2}

Download the facebook SDK for Unity [here](https://developers.facebook.com/docs/unity/) and import the package into your project.

Configure your AppID through the Facebook Settings menu in Unity. Do not forget this step as facebook will throw an error upon usage otherwise.

# Using the Facebook integration plugin {#toc3}

Import the Unity SDK Facebook Integration package into your project.

Put the CotcFacebookIntegration object on your scene, from the `CotcFacebookIntegration/Prefabs` folder.

From your code, find the object as for the Clan of the Cloud SDK and call methods on it.

~~~~{.cs}
	var fb = FindObjectOfType<CotcFacebookIntegration>();
	fb.LoginWithFacebook(gamerResult => { ... }, Cloud);
~~~~

