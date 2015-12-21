## Downloading & Installing

Open [Releases](https://github.com/clanofthecloud/unity-sdk/releases) and download the latest Unity package.

Read the [Documentation](http://clanofthecloud.github.io/unity-sdk/) to get started.

## Support

The Unity SDK is developed on Unity 4 and tested on Unity 5 prior to release. The Android platform specific components are not guaranteed to work on Unity 4 as libraries are built using gradle and packaged using the new AAR format.

## Building the library

### Required components

The build system is currently made for Windows, and we are using Visual Studio 2012 (with the UnityVS plugin) as well as Unity 4 for backward compatibility (this will most likely be lifted in the future, as it concerns only the CLI sample and Integration Testing projects).

### Building the library

The steps involved are:
- Build the CotcSdk solution,
- Use the CLI sample project to run the integration tests
- Build a unity package from the same project


#### Build the CotcSdk solution

This should be very simple. Just open the sln file under the CotcSdk directory.


### Additional plugins

- Install and import the Facebook Unity SDK.
- From the Asset Store, download and import the Unity Test Tools.
- Download and install Visual Studio Tools for Unity
- From the Unity project, Assets -> Import package -> Visual Studio 2012 Tools
- Then Visual Studio Tools -> Open in Visual Studio
- Click on Attach to Unity

### Distributing the library

Use the editor menu, click `CotC` and then `Build Release Packages`.

## Running integration tests

Integration tests are a very useful feature used throughout the developement of this SDK in order to quickly test new features and ensure that no regressions are made whenever features are modified.

Each time you add a feature, you should add one or several integration test as well. Also, when modifying the library, please run all integration tests and check if anything has been broken.

Open the IntegrationTestScene on desktop, or build a mobile application with the MobileIntegrationTestScene scene if you are going to test on iOS for instance. Using IntegrationTestScene, open Unity Test Tools and Integration Test Runner from the menu, then click Run all. When using the MobileIntegrationTestScene, tests are run automatically upon startup. Just check the output (logs) to get the results. This scene may also be used on desktop, although we consider it less convenient.

### Writing a new integration test




