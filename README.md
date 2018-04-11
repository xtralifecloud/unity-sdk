## Downloading & Installing

Open [Releases](https://github.com/xtralifecloud/unity-sdk/releases) and download the latest Unity package.

Read the [Documentation](http://xtralifecloud.github.io/unity-sdk/) to get started.

## Support

The Unity SDK is developed for Unity 5 but is still compatible with Unity 4, although we recommend using prior releases for guarantee as Unity 5 is now the main testing channel. The Android platform specific components are not guaranteed to work on Unity 4 as libraries are built using gradle and packaged using the new AAR format.

## Building the library

### Required components

The build system is currently made for Windows, and we are using Visual Studio 2012 (with the UnityVS plugin) with Unity 5.

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

## Using the library with Universal Windows Platform (UWP) Unity projects

In order to ensure compatibility between Unity UWP generated projects and the CotcSdk library, a few steps are needed...

### Building the libraries

When you build the entire CotcSdk solution, you'll end up with 3 library files:

- `[SolutionPath]\bin`: the `standard` Sdk library DLL
- `[SolutionPath]\CotcSdk-Editor\bin`: the `standard editor` Sdk library DLL (Unity editor part)
- `[SolutionPath]\CotcSdk-UniversalWindows\bin`: the `UWP compatible` Sdk library DLL

### Using the libraries

Basicly, because Windows Store Apps use special Runtime APIs, you'll need 2 different libraries: the `UWP compatible` one to build a Windows Store App, and the `standard` one which will act as a placeholder to be able to compile Unity projects directly in the editor (there is no such considers about the `-editor` library).

For this to work, a few steps are involved:

- Put the `standard library` in the `[UnityProjectPath]\Assets\Plugins` folder
- Put the `standard editor library` in the `[UnityProjectPath]\Assets\Plugins\Editor` folder
- Put the `UWP compatible library` in the `[UnityProjectPath]\Assets\Plugins\WSA` folder
(It is crucial that both the `standard` and `UWP compatible` libraries are identically named and share the same assembly version for the placeholder to work: e.g. `CotcSdk.dll`)
- In the Unity editor, select the `Assets\Plugins\CotcSdk.dll` file and make sure all platforms BUT `WSAPlayer` are ticked
- Select the `Assets\Plugins\WSA\CotcSdk.dll` file and make sure the `WSAPlayer` platform is the ONLY ONE ticked, then select `Assets\Plugins\CotcSdk.dll` as the placeholder

For further informations, please check out this link to official Unity's manual about Windows Store Apps plugins integration: https://docs.unity3d.com/Manual/windowsstore-plugins.html

### Enabling the Internet Client app capability

Don't forget to allow your app to access the Internet :

In the Unity editor, go to `Edit >> Project Settings >> Player`; Hit the `Windows Store Apps settings` tab, then in the `Publishing Settings` section search for `Capabilities` and tick the `InternetClient` capability. This will allow Unity to automatically add this capability in the `Package.appxmanifest` file generated on your Unity project build for UWP.

## Running integration tests

Integration tests are a very useful feature used throughout the developement of this SDK in order to quickly test new features and ensure that no regressions are made whenever features are modified.

Each time you add a feature, you should add one or several integration test as well. Also, when modifying the library, please run all integration tests and check if anything has been broken.

Open the Test Runner by clicking on `Window -> Test Runner`. In the *PlayMode* tab, you should be able to see all tests. You can run tests quickly by clicking on *Run All*, *Run Selected* or *Rerun failed*.

You can also run tests on specific platforms in Play mode. To do this, click the *Run all in player* button (the target platform is the current Platform selected in build options).

### Writing a new integration test

In order to write an integration test, open the `UnityProject/Assets/Tests/Scripts` folder. Each script represent a group of tests. If you want to create a new group, see further instructions, else select the script to which you'd like to add your test and add a new method like that :

```C#
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public IEnumerator ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		CompleteTest();
	});
	return WaitForEndOfTest(); // This line wait for either CompleteTest or FailTest to be called
}
```

From the Test annotation, you may describe your test. Optionally, if the test may fail because it requires additional configuration, you may also add another argument to the test annotation: `requisite: "..."`.

Once your test is written, return to the Unity editor, wait a few seconds for the UI to refresh and select your new test. You may then click *Run selected*. This will run only your test.

### Creating a new group

A test group is associated to a separate test class to make things more clear. Tests are placed under the directory `UnityProject/Assets/Tests/Scripts`. To get started, we suggest that you simply copy and rename an existing test class, since the inheritance and imports are important.

### Fail/Success of a test

Generally, a test may fail due to:

- A `LogError` in the console
- An exception not catched by the test
- A timeout of the test (by default, the maximum delay before a timeout is 30s)
- The conditionnal expression in an `Assert` valued to false.
- A call to the `FailTest` function
- A function called in an `ExpectSuccess` returning an error
- The functions called in an `ExpectFailure` not returning any error

A test can only success if it calls the function `CompleteTest()`, thus you should always and only call this function at the end of the test.

Be careful, the `return WaitForEndOfTest();` line (which should be at the end of your test) wait for either CompleteTest or FailTest to be called. If none of them is called, your test will run for ever. Well, actually, until timeout.

### Using unit tests with asynchronous functions

This SDK contains mostly asynchronous functions wich consist in asking the server to send us data or to perform an action ; thus, in a unit test, you have to ask the server, and wait for its response to be able to test it. This is done by the [Promise system](http://xtralifecloud.github.io/unity-sdk/Docs/DoxygenGenerated/html/getting_started_ref.html#promises_ref), which let you to call asynchronous functions and register callbacks that will be executed when the responses are received.

Here is a sample:

```C#
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public IEnumerator ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		CompleteTest();
	});
	return WaitForEndOfTest();
}
```

During this test, `CompleteTest();` will be called only when the server response is received.

You can easily call any asynchronous function after another one by returning their results (which are promises) and using `ExpectSuccess` or `ExpectFailure`, like the following code snippet:

```C#
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public IEnumerator ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		return Logout();
	}).ExpectSuccess(done => {
		CompleteTest();
	});
	return WaitForEndOfTest();
}
```

Or, witout the use of ExpectSuccess/ExpectFailure:

```C#
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public IEnumerator ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		Logout(done => {
			CompleteTest();
		};
	});
	return WaitForEndOfTest();
}
```

### Testing on Android

First, you will need to be able to [build on Android](https://unity3d.com/fr/learn/tutorials/topics/mobile-touch/building-your-unity-game-android-device-testing).

Link your Android device via an USB port on your computer and enable USB debugging, and switch target platform of the project to "Android"

If you click on *Run all in player*, your tests should be running on your device. When all of them are completed, a text indicating if the tests succeded or failed will appear.