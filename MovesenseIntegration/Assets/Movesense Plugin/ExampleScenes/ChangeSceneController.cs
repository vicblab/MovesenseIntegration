using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ChangeSceneController {
	private const string	TAG = "ChangeSceneController; ";
	private const bool		isLogging = false;
	private static string	callScene;
	private static List<string> sceneBreadcrumbs = new List<string>();
	
	public static void LoadSceneByName(string targetScene) {
		
		callScene = SceneManager.GetActiveScene().name;
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "LoadSceneByName, from: " + callScene + " to: " + targetScene);
		#pragma warning restore CS0162
		sceneBreadcrumbs.Add(callScene);
		
		SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
	}

	public static void GoSceneBack() {
		SceneManager.LoadSceneAsync(sceneBreadcrumbs[sceneBreadcrumbs.Count-1], LoadSceneMode.Single);
		sceneBreadcrumbs.RemoveAt(sceneBreadcrumbs.Count-1);
	}
}