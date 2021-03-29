using UnityEngine;
using UnityEngine.UI;

public class SystemBack : MonoBehaviour {
	private void Update() {
		if (Application.platform == RuntimePlatform.Android && Input.GetKeyDown(KeyCode.Escape)) {
			var backButton = GetComponent<Button>();
			if (backButton != null) {
				backButton.onClick.Invoke();
			} else {
				new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity").Call<bool>("moveTaskToBack", true);
			}
		}
	}
}