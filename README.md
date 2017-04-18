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

Open the `IntegrationTestScene` on desktop, or build a mobile application with the `MobileIntegrationTestScene` scene if you are going to test on iOS for instance (on Android you may manage to do it using the mechanism built in Unity with the `IntegrationTestScene`, though we've found it to be sometimes complicated to get working). Using `IntegrationTestScene`, open Unity Test Tools and Integration Test Runner from the menu, then click Run all. When using the `MobileIntegrationTestScene`, tests are run automatically upon startup. Just check the output (logs) to get the results. This scene may also be used on desktop, although we consider it less convenient.

### Writing a new integration test

In order to write an integration test, open the `IntegrationTestScene`. From the Hierarchy, select the group to which you'd like to add your test and duplicate a test inside of it (for example Gamer Tests -> ShouldSetProperty). If you want to create a new group, see further instructions. From the inspector, you'll be able to select your new method (Gamer Tests (Script) -> Method to call). You should write it. Open the corresponding script (here GamerTests) and add a new method like that:

```	
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public void ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		CompleteTest();
	});
}
```

From the Test annotation, you may describe your test. Optionally, if the test may fail because it requires additional configuration, you may also add another argument to the test annotation: `requisite: "..."`. When selecting the test in question, a warning sign will appear and the requisite text will be displayed in the inspector under the *Method to call*.

Once your test is written, return to the Unity editor, wait a few seconds for the UI to refresh and select your new test from the *Method to call* combo. You may then use the *Integration Test Runner* (from the menu under *Unity Test Tools*) and click *Run selected*. This will run only your test.

### Creating a new group

A test group is associated to a separate test class to make things more clear. Tests are placed under the directory `UnityProject/Assets/Tests/Scripts`. To get started, we suggest that you simply copy and rename an existing test class, since the inheritance and imports are important.

Then, from the `IntegrationTestScene`, duplicate an existing test group, rename it according to the name of your new test class. Place it in alphabetic order under the hierarchy and remove all tests inside except one. From this test, remove the existing script, refering to the test class that you copied (click on the little gear on the right of, e.g. *Gamer Tests (Script)*, *remove component*). Then click *Add component* and add the new test class that you just created. Select the method representing your first test through the *Method to call* combo box.

NB: this functionality is provided through the `TestBase` class. If it doesn't seem to work properly, check that your class is defined properly and everything compiles (check out the *Console*).

After creating a new group of tests, you need to update the `RunAllTests.cs` script (which is used by the `MobileIntegrationTestScene`). There is something like:

```
private static readonly Type[] TestTypes = {
		typeof(CloudTests),
```

You just need to add the new class in there. Try to respect the alphabetical ordering.

### Fail/Success of a test

Generally, a test may fail due to :

- A `LogError` in the console
- An exception not catched by the test
- A timeout of the test (by default, the maximum delay before a timeout is 30s)
- The conditionnal expression in an `Assert` valued to false.
- A call to the `FailTest` function
- A function called in an `ExpectSuccess` returning an error
- The functions called in an `ExpectFailure` not returning any error

A test can only success if it calls the function `CompleteTest()`, thus you should always and only call this function a the end of the test.

### Using unit tests with asynchronous functions

This SDK contains mostly asyncrhonous functions wich consist in asking the server to send us data or to perform an action ; thus, in a unit test, you have to ask the server, and wait for it's response to be able to test it. This is done by the `Promise` system (cf. http://xtralifecloud.github.io/unity-sdk/Docs/DoxygenGenerated/html/getting_started_ref.html#promises_ref). Wich let you call the asynchronous function, and register a delegate that will be executed when the response is received. 

For exemple :
```	
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public void ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		CompleteTest();
	});
}
```

During this test, `CompleteTest();` will be called only when the server response is received.

You can easily call one asynchronous function after another by returning the asyncrhonous functions result (wich is a promise) and using ExpectSuccess or ExpectFailure. For exemple : 

```	
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public void ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		return Logout();
	}).ExpectSuccess(done => {
		CompleteTest();
	});	
}
```

Or, witout the use of ExpectSuccess/ExpectFailure :
```	
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public void ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		Logout(done => {
			CompleteTest();
		};
	});	
}
```

### Test on Android

First, you will need to be able to build on Android (cf. https://unity3d.com/fr/learn/tutorials/topics/mobile-touch/building-your-unity-game-android-device-testing).

Link your android device via USB on your computer and enable USB debugging. In Unity, open `Unity Test Tools > Plateform Runner > Run on Plateform`. In the third column named `Available Scenes`, select `IntegrationTestScene` (and/or any other scene you want) and click on `Add to Build`, on `Build tests for` select Android, and finally click on `Build and run tests`.

Now, your tests are running on your device. When they are all completed, a text will appear, indicating if the tests succeded or failed. To be able to see the logs produced on your device, you will need a tool. For exemple, you can use `monitor`. Go to Android SDK folder > tools > monitor.bat (Run as administrator for windows users).

In monitor, on the upper left (Devices), select your device. On the bottom, in the filter input field, add `tag:Unity`. Now, you can go back to Unity, and `Build and run tests` again. The logs will now be displayed inside `monitor`. 




