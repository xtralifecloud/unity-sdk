## Downloading & Installing

Open [Releases](https://github.com/clanofthecloud/unity-sdk/releases) and download the latest Unity package.

Read the [Documentation](http://clanofthecloud.github.io/unity-sdk/) to get started.

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

## Running integration tests

Integration tests are a very useful feature used throughout the developement of this SDK in order to quickly test new features and ensure that no regressions are made whenever features are modified.

Each time you add a feature, you should add one or several integration test as well. Also, when modifying the library, please run all integration tests and check if anything has been broken.

Open the `IntegrationTestScene` on desktop, or build a mobile application with the `MobileIntegrationTestScene` scene if you are going to test on iOS for instance (on Android you may manage to do it using the mechanism built in Unity with the `IntegrationTestScene`, though we've found it to be sometimes complicated to get working). Using `IntegrationTestScene`, open Unity Test Tools and Integration Test Runner from the menu, then click Run all. When using the `MobileIntegrationTestScene`, tests are run automatically upon startup. Just check the output (logs) to get the results. This scene may also be used on desktop, although we consider it less convenient.

### Writing a new integration test

In order to write an integration test, open the `IntegrationTestScene`. From the Hierarchy, select the group to which you'd like to add your test and duplicate a test inside of it (for example Gamer Tests -> ShouldSetProperty). If you want to create a new group, see further instructions. From the inspector, you'll be able to select your new method (Gamer Tests (Script) -> Method to call). You should write it. Open the corresponding script (here GamerTests) and add a new method like that:

```	[Test("Tests the outline functionality")]
	public void ShouldReturnProperOutline(Cloud cloud) {
		Login(cloud, gamer => {
			CompleteTest();
		});
	}```

From the Test annotation, you may describe your test. Optionally, if the test may fail because it requires additional configuration, you may also add another argument to the test annotation: `requisite: "..."`. When selecting the test in question, a warning sign will appear and the requisite text will be displayed in the inspector under the *Method to call*.

Once your test is written, return to the Unity editor, wait a few seconds for the UI to refresh and select your new test from the *Method to call* combo. You may then use the *Integration Test Runner* (from the menu under *Unity Test Tools*) and click *Run selected*. This will run only your test.

#### Creating a new group

A test group is associated to a separate test class to make things more clear. Tests are placed under the directory `UnityProject/Assets/Tests/Scripts`. To get started, we suggest that you simply copy and rename an existing test class, since the inheritance and imports are important.

Then, from the `IntegrationTestScene`, duplicate an existing test group, rename it according to the name of your new test class. Place it in alphabetic order under the hierarchy and remove all tests inside except one. From this test, remove the existing script, refering to the test class that you copied (click on the little gear on the right of, e.g. *Gamer Tests (Script)*, *remove component*). Then click *Add component* and add the new test class that you just created. Select the method representing your first test through the *Method to call* combo box.

NB: this functionality is provided through the `TestBase` class. If it doesn't seem to work properly, check that your class is defined properly and everything compiles (check out the *Console*).

After creating a new group of tests, you need to update the `RunAllTests.cs` script (which is used by the `MobileIntegrationTestScene`). There is something like:

```	private static readonly Type[] TestTypes = {
		typeof(CloudTests),```

You just need to add the new class in there. Try to respect the alphabetical ordering.

