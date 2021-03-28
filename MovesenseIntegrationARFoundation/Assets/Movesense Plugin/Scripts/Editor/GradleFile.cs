using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GradleFile {
	private readonly static string GRADLE_DEST_PATH = Combine("Plugins", "Android");
	private const string GRADLE_DEST_FILE = "mainTemplate.gradle";
#if UNITY_2018_2_OR_NEWER
	private const string GRADLE_SRC_VERSION = "2018.2";
#elif UNITY_2018_1
	private const string GRADLE_SRC_VERSION = "2018.1";
#elif UNITY_2017_4
	private const string GRADLE_SRC_VERSION = "2017.4";
#elif UNITY_2017_3
	private const string GRADLE_SRC_VERSION = "2017.3";
#elif UNITY_2017_2
	private const string GRADLE_SRC_VERSION = "2017.2";
#elif UNITY_2017_1
	private const string GRADLE_SRC_VERSION = "2017.1";
#endif
	private const string GRADLE_SRC_FILE = "gradle_" + GRADLE_SRC_VERSION + ".file";
	private const string GRADLE_SRC_START = "// " + GRADLE_SRC_VERSION;

	static GradleFile() {
#if UNITY_ANDROID
		EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
	#if UNITY_2018_2_OR_NEWER
		PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel19;
	#else
		PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
	#endif
#endif
		var directory = Combine(Application.dataPath, GRADLE_DEST_PATH);
		var file = Combine(directory, GRADLE_DEST_FILE);
		if (File.Exists(file)) {
			using (var f = File.OpenText(file)) {
                string v = f.ReadLine();
                if (v == GRADLE_SRC_START) {
					return;
				}
			}
			File.Delete(file);
		}
		Directory.CreateDirectory(directory);
		File.Copy(Combine(Application.dataPath, "Movesense Plugin", "Scripts", "Editor", GRADLE_SRC_FILE), file);
	}

	private static string Combine(params string[] paths) {
		string combined = "";
		foreach (var path in paths) {
			combined = Path.Combine(combined, path);
		}
		return combined;
	}
}
