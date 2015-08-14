﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ExportReleasePackage {
	private const string ReleaseDirectory = "Release";
	private static readonly Dictionary<string, string[]> PackagesToBeExported = new Dictionary<string,string[]>()
	{
		{
			"CotcSdk.Core.unitypackage",
			new string[] {
				"Assets/Cotc",
				"Assets/Plugins/CotcSdk.dll",
				"Assets/Plugins/Editor/CotcSdk-Editor.dll",
				"Assets/Scenes/SampleSimpleScene.unity",
				"Assets/Scripts/SampleScript.cs",
			}
		},
		{
			"CotcSdk.FacebookIntegration.unitypackage",
			new string[] {
				"Assets/Cotc.FacebookIntegration",
				"Assets/Scenes/SampleSceneWithFacebook.unity",
				"Assets/Scripts/SampleScriptWithFacebook.cs",
			}
		},
		{
			"CotcSdk.PushNotifications.unitypackage",
			new string[] {
				"Assets/Cotc.PushNotifications",
				"Assets/Plugins/Android/AndroidManifest.xml",
				"Assets/Plugins/Android/appcompat-v7-22.1.1.aar",
				"Assets/Plugins/Android/Cotc.PushNotifications.aar",
				"Assets/Plugins/Android/play-services-base-7.5.0.aar",
				"Assets/Plugins/Android/play-services-gcm-7.5.0.aar",
				"Assets/Plugins/Android/support-v4-22.1.1.aar",
				"Assets/Plugins/Android/res/drawable-hdpi/ic_stat_ic_notification.png",
				"Assets/Plugins/Android/res/drawable-hdpi-v11/ic_stat_ic_notification.png",
				"Assets/Plugins/Android/res/drawable-mdpi/ic_stat_ic_notification.png",
				"Assets/Plugins/Android/res/drawable-mdpi-v11/ic_stat_ic_notification.png",
				"Assets/Plugins/Android/res/drawable-xhdpi/ic_stat_ic_notification.png",
				"Assets/Plugins/Android/res/drawable-xhdpi-v11/ic_stat_ic_notification.png",
				"Assets/Plugins/Android/res/drawable-xxhdpi/ic_stat_ic_notification.png",
				"Assets/Plugins/Android/res/drawable-xxhdpi-v11/ic_stat_ic_notification.png",
			}
		},
	};

	[MenuItem("CotC/Build release packages")]
	public static void ReleasePackages() {
		var releaseDirectory = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, ReleaseDirectory);

		// First, build the DLLs
		var buildScriptDir = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).Parent.FullName, "CloudBuilderLibrary");
		var process = new System.Diagnostics.Process();
		var succeeded = false;
		process.StartInfo.FileName = Path.Combine(buildScriptDir, "Build.bat");
		process.StartInfo.WorkingDirectory = buildScriptDir;
		if (process.Start()) {
			process.WaitForExit();
			succeeded = process.ExitCode == 0;
		}
		if (!succeeded) {
			Debug.LogError("Failed to build the libraries, might be packaging an old version of the library");
			if (!EditorUtility.DisplayDialog("Error", "Failed to build the libraries. This only works on Windows out of the box. You can continue but you may be packaging a wrong version of the library. Click cancel to stop this process.", "Continue", "Cancel")) {
				return;
			}
		}

		Debug.Log("Building packages in " + releaseDirectory);
		System.IO.Directory.CreateDirectory(releaseDirectory);

		foreach (var pair in PackagesToBeExported) {
			Debug.Log("Creating package " + pair.Key + "...");
			AssetDatabase.ExportPackage(pair.Value, ReleaseDirectory + "/" + pair.Key, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
			foreach (string s in pair.Value) {
				Debug.Log("Included file " + s);
			}
		}

		// Reveal in file explorer
		OpenInFileBrowser.Open(releaseDirectory);
	}
}