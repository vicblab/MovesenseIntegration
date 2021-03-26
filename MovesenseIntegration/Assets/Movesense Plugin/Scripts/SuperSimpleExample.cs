using System.Collections;
using UnityEngine;

public class SuperSimpleExample : MonoBehaviour {

	private void Awake() {
		ScanController.Event += OnScanControllerCallbackEvent;
		MovesenseController.Event += OnMovesenseControllerCallbackEvent;
	}

	// Use this for initialization
	void Start () {
		StartCoroutine(StartScanning());
	}
	
	// Update is called once per frame
	void Update () {
		
	}


	void OnScanControllerCallbackEvent(object sender, ScanController.EventArgs e) {
		//Debug.Log("OnScanControllerCallbackEvent, Type: " + e.Type + ", invoked by: " + e.InvokeMethod);
		switch (e.Type) {
			case ScanController.EventType.NEW_DEVICE:
				Debug.Log("OnScanControllerCallbackEvent, NEW_DEVICE with MacID: "+e.MacID+", connecting...");
				StartCoroutine(Connect(e.MacID));
				break;
			default:
				Debug.Log("tets");
				break;
		}
	}

	void OnMovesenseControllerCallbackEvent(object sender, MovesenseController.EventArgs e) {
		//Debug.Log("OnMovesenseControllerCallbackEvent, Type: " + e.Type + ", invoked by: " + e.InvokeMethod);
		switch (e.Type) {
			case MovesenseController.EventType.CONNECTING:
				for (int i = 0; i < e.OriginalEventArgs.Count; i++) {
					var ce = (ConnectCallback.EventArgs) e.OriginalEventArgs[i];

					Debug.Log("OnMovesenseControllerCallbackEvent, CONNECTING " + ce.MacID);
				}
			break;
			case MovesenseController.EventType.CONNECTED:
				for (int i = 0; i < e.OriginalEventArgs.Count; i++) {
					var ce = (ConnectCallback.EventArgs) e.OriginalEventArgs[i];

					Debug.Log("OnMovesenseControllerCallbackEvent, CONNECTED " + ce.MacID + ", subscribing linearAcceleration");
					
					MovesenseController.Subscribe(ce.Serial, SubscriptionPath.LinearAcceleration, SampleRate.medium);

					MovesenseController.Subscribe(ce.Serial, SubscriptionPath.AngularVelocity, SampleRate.medium);//modified
				
					//MovesenseController.Subscribe(ce.Serial, SubscriptionPath.MagneticField, SampleRate.medium);//modified
				}
			break;
			case MovesenseController.EventType.NOTIFICATION:
				for (int i = 0; i < e.OriginalEventArgs.Count; i++) {
					var ne = (NotificationCallback.EventArgs) e.OriginalEventArgs[i];
					//Debug.Log("Epic debug");
					Debug.Log("OnMovesenseControllerCallbackEvent, NOTIFICATION for " + ne.Serial + ", SubscriptionPath: " + ne.Subscriptionpath + ", Data: " + ne.Data);
					
				}
			break;
		}
	}

	IEnumerator StartScanning() {
		if (ScanController.IsInitialized) {
			yield return 0;
			ScanController.StartScan();
		} else {
			yield return new WaitForSeconds(0.1F); // wait for ScanController to be initialized
			ScanController.StartScan();
		}
	}

	IEnumerator Connect(string macID) {
		if (MovesenseController.isInitialized) {
			yield return 0;
			MovesenseController.Connect(macID);
		} else {
			yield return new WaitForSeconds(0.1F); // wait for MovesenseController to be initialized
			MovesenseController.Connect(macID);
		}
	}
}
#region Edited by
// Alarik Näykki
#endregion