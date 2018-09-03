# **C# XtraLife SDK for Unity**

## **Downloading & Installing**

Get the latest [Unity SDK Release Packages](https://github.com/xtralifecloud/unity-sdk/releases) you're interested in and import them into your Unity project. Only the **core package** will be necessary if you don't need Facebook integration, in app purchases, and push notifications features.

Read the [CotC Unity SDK](http://xtralifecloud.github.io/unity-sdk) and [Backend APIs](http://doc.xtralife.cloud/backend/?cs#) documentations to get started.

Want to go further? Do not hesitate to check for our [Game Template](https://github.com/xtralifecloud/unity-gametemplate) to inspire yourself with that extra-clean, simple, and fully commented sample of how to handle a persistent login logic and event messages. Plus, it gives you a quick way to test most of SDK's APIs via its sample scene. Last but no the least, you may even import this entire project into yours to plug your workflow on it and even reuse its default UI elements, it's originally designed for this!

## **Support**

The Unity SDK is developed for **Unity 5** as the main test channel but is compatible with **Unity 2017** and **2018** too.

## **Building the library**

If you wish to use the SDK on a not provided platform or even experiment with your own code modifications, you may want to clone the present repository and build the library by yourself instead of importing repository's release packages.

### **Required components**

The build system is currently made for [Visual Studio 2015/2017](https://visualstudio.microsoft.com/en/downloads) on **Windows**. You may use **Monodevelop** too (to be able to build on **Mac** for example), but you'll be unable to build the **CotcSdk-UniversalWindows** part.

> About the **Universal Windows** platform specific library: You'll have to use **Visual Studio 2015/2017** on **Windows 10** in order to build the **CotcSdk-UniversalWindows** solution's project.

### **Building the CotcSdk solution**

A few steps are involved to build the CotcSdk solution:

1. `Open the solution:` Just open solution's file (`CotcSdk\CotcSdk.sln`) with **Visual Studio**.

2. `Set Unity engine/editor libraries references:` In the **Solution Explorer** and for each of the 3 projects, unfold the project and then the **References** sub-menus, then check cautiously all **UnityEngine** and **UnityEditor** libraries references point to the correct Unity editor's version you want to build for (e.g. `C:\Program Files\Unity 5.6\Editor\Data\Managed\UnityEngine.dll`) and match the following:

   - **CotcSdk:** UnityEngine
   - **CotcSdk-Editor:** UnityEngine, UnityEditor
   - **CotcSdk-UniversalWindows:** UnityEngine

3. `Select the target build configuration:` Select the active solution configuration to build that matches the Unity editor's version you want to build for (e.g. `Release-Unit-5`).

   > **Release** configurations are lightweight optimized libraries designed to be used on production, while **Debug** configurations allow for more in-depth debugging.

4. `Build the solution:` Simply hit the `Build > Rebuild Solution` menu to generate the library; Each time you do so, the generated files can be found in the `bin` folders of their respective projects and are automatically copied/replaced in repository's corresponding Unity project `Assets\Plugins`, `Assets\Plugins\Editor`, and `Assets\Plugins\WSA` folders.

### **Additional Unity plugins**

Some additional plugins may be necessary if you wish to open repository's Unity project in order to make it compile and work well:

- Download and install **Visual Studio Tools for Unity** (if not already installed thanks to the **Unity editor installer**).

- Download and import the [Facebook SDK for Unity](https://developers.facebook.com/docs/unity).

### **Packaging the library**

In the Unity editor, click the `CotC > Build Release Packages` menu to generate Unity packages which can be found in the `Release` folder of the project and are ready to be imported into any other Unity project.

## **Using the library on the Universal Windows Platform (UWP)**

In order to ensure the compatibility between **Unity UWP projects** and the CotcSdk library, a few steps are needed...

### **Manage Unity plugins**

Basicly, because **Windows Store Apps** make use of specific runtime APIs, you'll need 2 different CotcSdk libraries:

- The `standard library (Assets\Plugins\CotcSdk.dll):` It will act as a **placeholder for the UWP library** to be able to compile the Unity project directly in the editor (there is no such concerns about the **editor** library part).

- The `UWP compatible library (Assets\Plugins\WSA\CotcSdk.dll):` To be able to build a **Windows Store App** for the **Universal Windows Platform**.

For this to work, a few steps are involved:

- Make sure the **standard** and the **UWP compatible** libraries are put in the correct folders (respectively `Assets\Plugins` and `Assets\Plugins\WSA`).

  > It is crucial that both the `standard` and the `UWP compatible` libraries are identically named and share the same assembly version for the placeholder to work (e.g. `CotcSdk.dll`).

- In Unity editor, select the `Assets\Plugins\CotcSdk.dll` library file and make sure all platforms **but** `WSAPlayer` are ticked.

- Select the `Assets\Plugins\WSA\CotcSdk.dll` file and make sure the `WSAPlayer` platform is the **only one** ticked, then set `Assets\Plugins\CotcSdk.dll` as its placeholder.

For further informations, please check out official Unity's manual about [Windows Store Apps plugins integration](https://docs.unity3d.com/Manual/windowsstore-plugins.html).

### **Enabling the Internet Client app capability**

Don't forget to allow your app to access the Internet:

In Unity editor, click the `Edit > Project Settings > Player` menu and hit the `Windows Store Apps settings` tab, then in the `Publishing Settings` section search for `Capabilities` and tick the `InternetClient` capability. This will allow Unity to automatically add this capability in the `Package.appxmanifest` file generated with your Unity project's build for UWP.

## **About integration tests (aka unit tests)**

Integration tests are a very useful tool to quickly test new features and ensure that no regressions are made whenever something is modified throughout the developement of this SDK. Each time you add a feature by yourself, you should add one or several integration test as well. Also, when modifying the library, please run all integration tests and check if anything has been broken.

### **Running integration tests**

In Unity editor, open the **Test Runner** by clicking on `Window -> Test Runner`. In the `PlayMode` tab, you should be able to see all written tests after you've hit the `Enable playmode tests` button for the first time. You can run tests quickly **in the editor** by clicking the `Run All` or `Run Selected` buttons.

> Before you run the tests, don't forget to set your game's `API Key` and `API Secret` credentials in order to be able to connect to the server and to read/write backend data.

> Note that the `CommunityTests > ShouldListNetworkUsers` test needs a **valid Facebook access token** in order to successfully complete, thus if you want to run this test you'll need to replace the hardcoded one in the corresponding script (`user_token` **string** in `Assets\Scripts\UnitTests\Tests\CommunityTests.cs`). You can get and generate new access tokens via the [Facebook API Graph explorer](https://developers.facebook.com/tools/explorer).

> Note that some tests may fail if the related **Game VFS** data keys are missing from the base, as it's an expected behavior. Please refer to the corresponding tests code comments.

You can also run those tests **on specific platforms devices** via the **PlayMode**. To do this, click the `Run all in player` button instead; The targeted platform will be the current platform selected in project's **Build Settings**.

#### **Testing on Android**

If you want to run tests on an Android device, then complete the following steps:

1. First, you'll need to be able to [build on Android](https://unity3d.com/fr/learn/tutorials/topics/mobile-touch/building-your-unity-game-android-device-testing).

   > If not already done, you may need to get and install the specific **ADB drivers** for your device model.

2. On your Android device, enable the **USB debugging option** then plug it on an USB port of your computer.

   > On some devices, you may have to switch to the `Files Transfer mode` in order to make the device visible to the computer.

3. Switch Unity project's target platform to `Android` in **Build Settings**.

4. On the **Test Runner** window, click the `Run all in player (Android)` button; All tests should be running on your device. When all of them are completed, a text indicating if the tests succeeded or failed will appear.

### **Writing new integration tests**

In order to **edit integration tests**, check for the `Assets\Scripts\UnitTests\Tests` folder; Each script represents a separate group of tests. If you want to create a new group see further instructions, else just select the script you'd like to add your new test in and add a new method like the following:

```CSharp
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public IEnumerator ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		CompleteTest();
	});
	return WaitForEndOfTest(); // This line waits for either CompleteTest or FailTest to be called
}
```

> The **Test** annotation allows you to describe your test's goal. Optionally, if the test may fail because it requires additional configuration, you may also add another argument to the test annotation: `requisite: "..."`.

Once your test is written, just return to the Unity editor, wait for a few seconds for the scripts to be compiled again, and then select your new test. Then, click the `Run Selected` button to only run the new test.

### **Creating a new group**

A **test group** is associated to a separate **test class** to make things clearer. To get started, we suggest that you simply duplicate, rename, then edit an existing test class since the inheritance structure and imports are all important. Don't forget to add your new test class in the `TestTypes` **Type array** in the `Assets\Scripts\UnitTests\Tests\RunAllTests.cs` script.

### **Fail/Success of a test**

In most cases, a test can fail due to:

- An **error log** in the console (`Debug.LogError(...);`)
- An **uncatched** code **Exception**
- A test **timeout**, classically while waiting for a request answer (the maximum timeout delay is 30 seconds by default)
- The conditionnal expression of an **Assert** valued to `false`
- A call to the `FailTest(...)` function
- **Any function returning an error** called in an `ExpectSuccess(...)` **promise**
- **Not a single function returning an error** called in an `ExpectFailure(...)` **promise**

A test can only succeed if it calls the `CompleteTest()` function, thus you should always and only call this function at the (successful) end of the test and you'll be unable to declare it as failed thereafter.

Be careful about the `return WaitForEndOfTest();` instruction (which should always be at the end of your test) as it waits for either a `CompleteTest()` or a `FailTest(...)` function to be called. If none of them is called, your test will wait untill it fails due to a timeout.

### **Using unit tests with asynchronous functions**

The CotcSdk contains mostly **asynchronous functions** wich consist in asking the server to send back data (and/or to perform a some actions before that). Testing features in such a way is done via the [Promise functions](http://xtralifecloud.github.io/unity-sdk/Docs/DoxygenGenerated/html/getting_started_ref.html#promises_ref), which let you call asynchronous functions and register **callbacks** (aka **delegates**) to be executed when the response is received.

Here is a sample:

```CSharp
[Test("Tests the outline functionality", "You must have setup the SDK properly")]
public IEnumerator ShouldReturnProperOutline(Cloud cloud) {
	Login(cloud, gamer => {
		CompleteTest();
	});
	return WaitForEndOfTest();
}
```

During this test, the `Login(...)` asynchronous function is called and an **anonymous delegate** is defined, which will call the `CompleteTest()` function only once server's response has been received. The `WaitForEndOfTest()` function call is here to ensure the test result isn't evaluated before we actually received server's response (right after the asynchronous function call).

You can easily call multiple asynchronous functions one after another by returning their results (which are always **promises**) and using `ExpectSuccess(...)` or `ExpectFailure(...)`, like the following code snippet:

```CSharp
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

Or, witout the use of `ExpectSuccess(...)` / `ExpectFailure(...)` functions:

```CSharp
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
