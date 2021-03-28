using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class VisualizationSceneScript : MonoBehaviour {
	private const string	TAG = "VisualizationSceneScript; ";
	private const bool		isLogging = false;
	private float angularCorrection = 1/(4*Mathf.PI);// make it synchronous unity

	private class VisualizationDevice {
		public GameObject Clone;
		public bool HasSubscription;
		public Transform MovesenseSensorTransform;
		public Transform CylinderUpDown;
		public Transform ConeUpDown;
		public Transform CylinderForthBack;
		public Transform ConeForthBack;
		public Transform CylinderLeftRight;
		public Transform ConeLeftRight;

		public VisualizationDevice(GameObject clone, bool hasSubscription, Transform movesenseSensorTransform, Transform cylinderUpDown, Transform coneUpDown, Transform cylinderForthBack, Transform coneForthBack, Transform cylinderLeftRight, Transform coneLeftRight) {
			Clone = clone;
			HasSubscription = hasSubscription;
			MovesenseSensorTransform = movesenseSensorTransform;
			CylinderUpDown = cylinderUpDown;
			ConeUpDown = coneUpDown;
			CylinderForthBack = cylinderForthBack;
			ConeForthBack = coneForthBack;
			CylinderLeftRight = cylinderLeftRight;
			ConeLeftRight = coneLeftRight;
		}
	}
	private List<VisualizationDevice> visualizationDevices = new List<VisualizationDevice>();
	private List<Vector3[]> devicePositions = new List<Vector3[]>();
	[SerializeField]
	private GameObject MovesenseSensor;

	[SerializeField]
	private Slider gyroscopeSlider;

	[SerializeField]
	private TMP_Text textThreshold;
	private float threshold;
	

	void Start () {
		// Matrix of positions depending on how many sensors are subscribed
		CreateDevicePositions();

		RefreshVisualizationDevices();
		
		// attach event
		MovesenseController.Event += OnMovesenseControllerCallbackEvent;

		if (PlayerPrefs.HasKey("gyrothreshold")) {
			threshold = PlayerPrefs.GetFloat("gyrothreshold");
			gyroscopeSlider.value = threshold;
		} else {
			threshold = gyroscopeSlider.value;
		}
		textThreshold.text = "Gyroscope threshold: " + threshold.ToString("F2");
	}

	public void OnClickButtonBack() {
		// detach event
		MovesenseController.Event -= OnMovesenseControllerCallbackEvent;
		ChangeSceneController.GoSceneBack();
	}

	public void OnClickButtonReset() {
		foreach (var item in visualizationDevices) {
			if(item.MovesenseSensorTransform != null) {
				item.MovesenseSensorTransform.localRotation = Quaternion.Euler(0, 0, 0);
			}
		}
	}

	public void OnSliderChanged() {
		threshold = gyroscopeSlider.value;
		textThreshold.text = "Gyroscope threshold: " + gyroscopeSlider.value.ToString("F2");
		PlayerPrefs.SetFloat("gyrothreshold", threshold);
	}

	void OnMovesenseControllerCallbackEvent(object sender, MovesenseController.EventArgs e) {
		switch (e.Type) {
		case MovesenseController.EventType.NOTIFICATION:
			for(int i = 0; i < e.OriginalEventArgs.Count; i++) {
				var ne = (NotificationCallback.EventArgs) e.OriginalEventArgs[i];
				UpdateTransform(MovesenseDevice.ContainsSerialAt(ne.Serial), ne);
			}
			break;
		case MovesenseController.EventType.DISCONNECTED:
			RefreshVisualizationDevices();
			break;
		}
	}
	
	private void UpdateTransform(int serialListPosition, NotificationCallback.EventArgs e) {
		// ignore magnetic field, heratrate and temperature
		if (e.Subscriptionpath == SubscriptionPath.MagneticField || e.Subscriptionpath == SubscriptionPath.HeartRate || e.Subscriptionpath == SubscriptionPath.Temperature) {
			return;
		}

		var notificationFieldArgs = (NotificationCallback.FieldArgs) e;

		Transform transform = visualizationDevices[serialListPosition].MovesenseSensorTransform;
		
		if (e.Subscriptionpath == SubscriptionPath.AngularVelocity) {
			float z = (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z*angularCorrection;
			if (z < -threshold || z > threshold) {
				transform.RotateAround(transform.position, -transform.up, z); // spin around Z-axis
			}
			float y = (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y*angularCorrection;
			if (y < -threshold || y > threshold) {
				transform.RotateAround(transform.position, -transform.forward, y);
			}
			float x = (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x*angularCorrection;
			if (x < -threshold || x > threshold) {
				transform.RotateAround(transform.position, -transform.right, x);// spin around Movesense-axis
			}
		} else if (e.Subscriptionpath == SubscriptionPath.LinearAcceleration) {
			Transform CylinderUpDown = visualizationDevices[serialListPosition].CylinderUpDown;
			Transform ConeUpDown = visualizationDevices[serialListPosition].ConeUpDown;
			Transform CylinderForthBack = visualizationDevices[serialListPosition].CylinderForthBack;
			Transform ConeForthBack = visualizationDevices[serialListPosition].ConeForthBack;
			Transform CylinderLeftRight = visualizationDevices[serialListPosition].CylinderLeftRight;
			Transform ConeLeftRight = visualizationDevices[serialListPosition].ConeLeftRight;
			
			// Up/Down
			float cylUpDown = 0;
			float coneUpDownDegree = 0;
			float coneUpDown = 0;
			if (notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z < 0) { // up
				cylUpDown = 26.5F*transform.localScale.y - (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z;
				coneUpDownDegree = 270;
				coneUpDown = 51.6F*transform.localScale.y - 2*(float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z;
			} else {
				cylUpDown = -51.3F*transform.localScale.y - (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z;
				coneUpDownDegree = 90;
				coneUpDown = -76.4F*transform.localScale.y - 2*(float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z;
			} 
			CylinderUpDown.localPosition = new Vector3(CylinderUpDown.localPosition.x, cylUpDown, CylinderUpDown.localPosition.z);
			CylinderUpDown.localScale = new Vector3(CylinderUpDown.localScale.x, (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z, CylinderUpDown.localScale.z);
			ConeUpDown.localRotation = Quaternion.Euler(coneUpDownDegree, 0, 0);
			ConeUpDown.localPosition = new Vector3(ConeUpDown.localPosition.x, coneUpDown, ConeUpDown.localPosition.z);	
			
			// Forth/Back
			float cylForthBack = 0;
			float coneForthBackDegree = 0;
			float coneForthBack = 0;
			if (notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y < 0) { // forth
				cylForthBack = 178.1F*transform.localScale.z - (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y;
				coneForthBackDegree = 0;
				coneForthBack = 203.2F*transform.localScale.y - 2*(float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y;
			} else {
				cylForthBack = -178.1F*transform.localScale.z - (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y;
				coneForthBackDegree = 180;
				coneForthBack = -203.2F*transform.localScale.y - 2*(float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y;
			}
			CylinderForthBack.localPosition = new Vector3(CylinderForthBack.localPosition.x, CylinderForthBack.localPosition.y, cylForthBack);
			CylinderForthBack.localScale = new Vector3(CylinderForthBack.localScale.x, (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y, CylinderForthBack.localScale.z);
			ConeForthBack.localRotation = Quaternion.Euler(coneForthBackDegree, 0, 0);
			ConeForthBack.localPosition = new Vector3(ConeForthBack.localPosition.x, ConeForthBack.localPosition.y, coneForthBack);
			
			// Left/Right
			float cylLeftRight = 0;
			float coneLeftRightDegree = 0;
			float coneLeftRight = 0;
			if (notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x < 0) { // right
				cylLeftRight = 178.1F*transform.localScale.x - (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x;
				coneLeftRightDegree = 90;
				coneLeftRight = 203.2F*transform.localScale.x - 2*(float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x;
			} else {
				cylLeftRight = -178.1F*transform.localScale.x - (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x;
				coneLeftRightDegree = 270;
				coneLeftRight = -203.2F*transform.localScale.x - 2*(float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x;
			}
			CylinderLeftRight.localPosition = new Vector3(cylLeftRight, CylinderLeftRight.localPosition.y, CylinderLeftRight.localPosition.z);
			CylinderLeftRight.localScale = new Vector3(CylinderLeftRight.localScale.x, (float)notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x, CylinderLeftRight.localScale.z);
			ConeLeftRight.localRotation = Quaternion.Euler(0, coneLeftRightDegree, 0);
			ConeLeftRight.localPosition = new Vector3(coneLeftRight, ConeLeftRight.localPosition.y, ConeLeftRight.localPosition.z);
		}
	}

	private void CreateDevicePositions () {
		devicePositions.Add(new [] {new Vector3(0, -2.31F, 0)}); // 1 Device
		devicePositions.Add(new [] {new Vector3(0, 3.48F, 0), new Vector3(0, -2.31F, 0)}); // 2 Devices
		devicePositions.Add(new [] {new Vector3(0, 3.48F, 0), new Vector3(0, -2.31F, 0), new Vector3(0, -8.7F, 0)}); // etc.
		devicePositions.Add(new [] {new Vector3(-3F, 3.48F, 0),	new Vector3(3F, 3.48F, 0), new Vector3(-3.7F, -4.86F, 0), new Vector3(3.7F, -4.86F, 0)});
		devicePositions.Add(new [] {new Vector3(-4.35F, 3.48F, 0), new Vector3(4.35F, 3.48F, 0), new Vector3(0, -2.31F, 0), new Vector3(-5.35F, -8.7F, 0), new Vector3(5.35F, -8.7F, 0)});
		devicePositions.Add(new [] {new Vector3(-3F, 3.48F, 0),	new Vector3(3F, 3.48F, 0), new Vector3(-3.5F, -2.31F, 0), new Vector3(3.5F, -2.31F, 0), new Vector3(-4F, -8.7F, 0),	new Vector3(4F, -8.7F, 0)});
		devicePositions.Add(new [] {new Vector3(0, 3.48F, 0), new Vector3(-4, 0.46F, 0), new Vector3(4, 0.46F, 0), new Vector3(0, -2.31F, 0), new Vector3(-4.55F, -5.54F, 0), new Vector3(4.55F, -5.54F, 0), new Vector3(0, -8.7F, 0)});
		devicePositions.Add(new [] {new Vector3(-2.6F, 8.42F, 0), new Vector3(2.6F, 8.42F, 0), new Vector3(-3F, 3.48F, 0), new Vector3(3F, 3.48F, 0), new Vector3(-3.5F, -2.31F, 0), new Vector3(3.5F, -2.31F, 0), new Vector3(-4F, -8.7F, 0), new Vector3(4F, -8.7F, 0)});
		devicePositions.Add(new [] {new Vector3(-4.35F, 3.48F, 0), new Vector3(0, 3.48F, 0), new Vector3(4.35F, 3.48F, 0), new Vector3(-4.76F, -2.31F, 0), new Vector3(0, -2.31F, 0), new Vector3(4.76F, -2.31F, 0), new Vector3(-5.35F, -8.7F, 0), new Vector3(0, -8.7F, 0), new Vector3(5.35F, -8.7F, 0)});
		devicePositions.Add(new [] {new Vector3(0, 8.42F, 0), new Vector3(-3.5F, 5.92F, 0), new Vector3(3.5F, 5.92F, 0), new Vector3(0, 3.48F, 0), new Vector3(-4, 0.46F, 0), new Vector3(4, 0.46F, 0), new Vector3(0, -2.31F, 0), new Vector3(-4.55F, -5.54F, 0), new Vector3(4.55F, -5.54F, 0), new Vector3(0, -8.7F, 0)});	
		devicePositions.Add(new [] {new Vector3(-4.07F, 8.42F, 0), new Vector3(4.07F, 8.42F, 0), new Vector3(0, 6.06F, 0), new Vector3(-4.35F, 3.48F, 0), new Vector3(4.35F, 3.48F, 0), new Vector3(0, 0.67F, 0), new Vector3(-4.76F, -2.31F, 0), new Vector3(4.76F, -2.31F, 0), new Vector3(0, -5.37F, 0), new Vector3(-5.35F, -8.7F, 0), new Vector3(5.35F, -8.7F, 0),});
		devicePositions.Add(new [] {new Vector3(-4.07F, 8.42F, 0), new Vector3(0, 8.42F, 0), new Vector3(4.07F, 8.42F, 0), new Vector3(-4.35F, 3.48F, 0), new Vector3(0, 3.48F, 0), new Vector3(4.35F, 3.48F, 0), new Vector3(-4.76F, -2.31F, 0), new Vector3(0, -2.31F, 0), new Vector3(4.76F, -2.31F, 0), new Vector3(-5.35F, -8.7F, 0), new Vector3(0, -8.7F, 0), new Vector3(5.35F, -8.7F, 0)});
	}

	private void RefreshVisualizationDevices() {
		// get subcribed sensors
		int subscriptedDevices = 0;
		foreach (var item in MovesenseDevice.Devices) {
			if (MovesenseDevice.GetAllSubscriptionPaths(item.Serial) != null) {
				subscriptedDevices ++;
			}
		}
		int visualizationDevicesCount = visualizationDevices.Count;

		if (visualizationDevicesCount < subscriptedDevices) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "RefreshVisualizationDevices, add clones");
			#pragma warning restore CS0162
			for (int i = visualizationDevicesCount; i < MovesenseDevice.Devices.Count; i++) {
				// needed to keep index consistent with connected Devices (unconnected have been removed)
				if (MovesenseDevice.GetAllSubscriptionPaths(MovesenseDevice.Devices[i].Serial) == null) {
					visualizationDevices.Add(new VisualizationDevice(null, false, null, null, null, null, null, null, null));
					Debug.LogWarning(TAG + "RefreshVisualizationDevices, no Subscriptions available for " + MovesenseDevice.Devices[i].Serial);
					continue;
				}
				GameObject movesenseSensorClone = Instantiate(MovesenseSensor) as GameObject;

				// DeviceSerial
				TMP_Text serial = movesenseSensorClone.GetComponentInChildren<TMP_Text>();
				serial.text = MovesenseDevice.Devices[i].Serial;

				Transform cylinderUpDown = null;
				Transform coneUpDown = null;
				Transform cylinderForthBack = null;
				Transform coneForthBack = null;
				Transform cylinderLeftRight = null;
				Transform coneLeftRight = null;
				for(int j = 0; j < movesenseSensorClone.transform.childCount; j++) {
					if (movesenseSensorClone.transform.GetChild(j).name == "CylinderUpDown") {
						cylinderUpDown = movesenseSensorClone.transform.GetChild(j);
					} else if (movesenseSensorClone.transform.GetChild(j).name == "ConeUpDown") {
						coneUpDown = movesenseSensorClone.transform.GetChild(j);
					} else if (movesenseSensorClone.transform.GetChild(j).name == "CylinderForthBack") {
						cylinderForthBack = movesenseSensorClone.transform.GetChild(j);
					} else if (movesenseSensorClone.transform.GetChild(j).name == "ConeForthBack") {
						coneForthBack = movesenseSensorClone.transform.GetChild(j);
					} else if (movesenseSensorClone.transform.GetChild(j).name == "CylinderLeftRight") {
						cylinderLeftRight = movesenseSensorClone.transform.GetChild(j);
					} else if (movesenseSensorClone.transform.GetChild(j).name == "ConeLeftRight") {
						coneLeftRight = movesenseSensorClone.transform.GetChild(j);
					}
				}

				Transform movesenseSensorTransform = movesenseSensorClone.GetComponent<Transform>();

				visualizationDevices.Add(new VisualizationDevice(movesenseSensorClone, true, movesenseSensorTransform, cylinderUpDown, coneUpDown, cylinderForthBack, coneForthBack, cylinderLeftRight, coneLeftRight));
			}
		} else if (visualizationDevicesCount > subscriptedDevices) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "RefreshVisualizationDevices, destroy clones");
			#pragma warning restore CS0162
			for (int i = visualizationDevicesCount-1; i > subscriptedDevices-1; i--) {
				Destroy(visualizationDevices[i].Clone);
				visualizationDevices.RemoveAt(i);
			}
		}

		// Positioning
		int devicesWithSubscription = 0;
		for (int i = 0; i < MovesenseDevice.Devices.Count; i++) {
			if (MovesenseDevice.GetAllSubscriptionPaths(MovesenseDevice.Devices[i].Serial) != null) {
				visualizationDevices[i].MovesenseSensorTransform.localPosition = devicePositions[subscriptedDevices-1][devicesWithSubscription];
				devicesWithSubscription++;
			}
		}
	}
}
