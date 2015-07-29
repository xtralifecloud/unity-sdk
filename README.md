## Downloading & Installing

Open [Releases](https://github.com/clanofthecloud/unity-sdk/releases) and download the latest Unity package.

Read the [Documentation](http://clanofthecloud.github.io/unity-sdk/) to get started.

## Building the library

### Required components

The build system is currently made for Windows, and we are using Visual Studio 2012 (with the UnityVS plugin) as well as Unity 4 for backward compatibility (this will most likely be lifted in the future, as it concerns only the CLI sample and Integration Testing projects).

### Building the library

The steps involved are:
- Build the CloudBuilderLibrary solution,
- Use the CLI sample project to run the integration tests
- Build a unity package from the same project


#### Build the CloudBuilderLibrary solution

This should be very simple. Just open the sln file under the CloudBuilderLibrary directory.


### Additional plugins

- Install the Facebook Unity SDK
- Download and install Visual Studio Tools for Unity
- From the Unity project, Assets -> Import package -> Visual Studio 2012 Tools
- Then Visual Studio Tools -> Open in Visual Studio
- Click on Attach to Unity

### Distributing the library

Making a package for the core SDK only:
- From Unity, Assets -> Export Package…
- Deselect all
- Select only Assets/CotC, Assets/Cotc.FacebookIntegration, Assets/Plugins/CotcSdk.dll, Assets/Plugins/Editor, Assets/SampleScene.unity and Assets/Script/SampleScript.cs.

Making a package for the CotC Facebook Integration plugin


## Writing a new integration test



