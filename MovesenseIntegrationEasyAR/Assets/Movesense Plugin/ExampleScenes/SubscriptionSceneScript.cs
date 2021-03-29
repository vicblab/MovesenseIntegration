using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SubscriptionSceneScript : MonoBehaviour {

	private const string	TAG = "SubscriptionSceneScript; ";
	private const bool		isLogging = false;

	[SerializeField]
	private Button buttonBack;
	[SerializeField]
	private Button buttonVisualize;
	[SerializeField]
	private RectTransform contentConnected;
	[SerializeField]
	private Button[] buttonsSubscription;
	[SerializeField]
	private TMP_Text[] TextLinAcc;
	[SerializeField]
	private TMP_Text[] TextGyro;
	[SerializeField]
	private TMP_Text[] TextMagnField;
	[SerializeField]
	private TMP_Text TextHeartrate;
	[SerializeField]
	private TMP_Text TextRrData;
	[SerializeField]
	private TMP_Text TextTemp;
	
	[SerializeField]
	private GameObject ConnectedElement;
	private List<GameObject>	connectedElements = new List<GameObject>();
	private int					connectedElementHighlitedIndex = 0; // selected Sensor to display
	private float				connectedElementHeight = 0;
	[SerializeField]
	private Sprite buttonOn;
	[SerializeField]
	private Sprite buttonOff;

	private Color colorDefault = new Color32(0x0A, 0x0A, 0x0A, 0xFF);//0A0A0AFF
	private Color colorHighlited = new Color32(0x4C, 0x4C, 0x4C, 0xFF);//4C4C4CFF
	
	// provides button from being pressed multiple times
	private bool isButtonVisualizePressed = false;
	

	// private void Awake() {
	// 	buttonOn = Resources.Load<Sprite>("Sprites/Button_On");
	// 	buttonOff = Resources.Load<Sprite>("Sprites/Button_Off");
	// }
	private void Start() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "Start");
		#pragma warning restore CS0162
		isButtonVisualizePressed = false;
		
		// refresh sensorlist
		RefreshScrollViewContentConnectedDevices(true);
		
		// refresh subscriptionlist
		RefreshPanelSubscription(0, null);
		
		// ButtonVisualize gets active, if any Subscription is active
		if (MovesenseDevice.isAnySubscribed(SubscriptionPath.AngularVelocity, SubscriptionPath.LinearAcceleration)) {
			buttonVisualize.gameObject.SetActive(true);
		} else {
			buttonVisualize.gameObject.SetActive(false);
		}
		
		// attach event
		MovesenseController.Event += OnMovesenseControllerCallbackEvent;
	}

	public void OnClickButtonBack() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnClickButtonBack");
		#pragma warning restore CS0162

		// detach event
		MovesenseController.Event -= OnMovesenseControllerCallbackEvent;
		
		ChangeSceneController.GoSceneBack();
	}
	public void OnClickButtonVisualize() {
		if (isButtonVisualizePressed) {
			isButtonVisualizePressed = true;
			return;
		}

		// detach event
		MovesenseController.Event -= OnMovesenseControllerCallbackEvent;

		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnClickButtonVisualize");
		#pragma warning restore CS0162
		
		ChangeSceneController.LoadSceneByName("VisualizationScene");
	}

	void OnMovesenseControllerCallbackEvent(object sender, MovesenseController.EventArgs e) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnMovesenseControllerCallbackEvent, e.Type: " + e.Type);
		#pragma warning restore CS0162
		switch (e.Type) {
		case MovesenseController.EventType.NOTIFICATION:	// got data from a sensor
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "OnMovesenseControllerCallbackEvent, case MovesenseController.EventType.NOTIFICATION");
			#pragma warning restore CS0162
			for (int i = 0; i < e.OriginalEventArgs.Count; i++) {
				var ne = (NotificationCallback.EventArgs) e.OriginalEventArgs[i];
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "OnMovesenseControllerCallbackEvent, e.OriginalEventArgs["+i+"].Data: " + ne.Data);
				#pragma warning restore CS0162
				RefreshPanelSubscription(MovesenseDevice.ContainsSerialAt(ne.Serial), ne);
			}
			break;
		case MovesenseController.EventType.CONNECTED:		// a sensor succesfully connected (in the background)
			RefreshScrollViewContentConnectedDevices(false);
			break;
		case MovesenseController.EventType.DISCONNECTED:	// a sensor disconnected
			RefreshScrollViewContentConnectedDevices(true);
			
			RefreshPanelSubscription(0, null);
			break;
		}
	}

	void RefreshScrollViewContentConnectedDevices(bool isInit) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "RefreshScrollViewContentConnectedDevices");
		#pragma warning restore CS0162
		int connectedDevices = MovesenseDevice.NumberOfConnectedDevices();
		int connectedElementsCount = connectedElements.Count;

		if (connectedElementsCount < connectedDevices) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "RefreshScrollViewContentConnectedDevices, add clones");
			#pragma warning restore CS0162
			for (int i = connectedElementsCount; i < connectedDevices; i++) {
				GameObject connectedElementClone = Instantiate(ConnectedElement, contentConnected) as GameObject;
				
				// Positioning
				RectTransform connectedElementRect = connectedElementClone.GetComponent<RectTransform>();
				if (connectedElementHeight == 0) connectedElementHeight = connectedElementRect.sizeDelta.y;
				
				// change position
				connectedElementRect.anchoredPosition = new Vector2(0, -connectedElementHeight/2 - (i * connectedElementHeight));

				connectedElements.Add(connectedElementClone);
			}
			contentConnected.sizeDelta = new Vector2(0, connectedElementHeight * connectedDevices);
			if (isInit) {
				foreach (var item in connectedElements[0].GetComponentInChildren<Button>().GetComponentsInChildren<Image>()) {
					if (item.name == "Image Background") {
						item.color = colorHighlited;
						break;
					}
				}
				connectedElementHighlitedIndex = 0;
			}
		} else if (connectedElementsCount > connectedDevices) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "RefreshScrollViewContentConnectedDevices, destroy clones");
			#pragma warning restore CS0162
			for (int i = connectedElementsCount-1; i > connectedDevices-1; i--) {
				Destroy(connectedElements[i]);
				
				connectedElements.RemoveAt(i);
			}
			contentConnected.sizeDelta = new Vector2(0, connectedElementHeight * connectedDevices);
		}

		for (int i = 0; i < connectedDevices; i++) {
			// change texts
			TMP_Text[] connectedElementTexts = connectedElements[i].GetComponentsInChildren<TMP_Text>();
			foreach (var text in connectedElementTexts) {
				if (text.name == "Text Serial") {
					text.text = MovesenseDevice.Devices[i].Serial;
				} else if (text.name == "Text MacID") {
					text.text = MovesenseDevice.Devices[i].MacID;
				}
			}
			
			// Highlight
			Button btn = connectedElements[i].GetComponentInChildren<Button>();
			
			btn.onClick.RemoveAllListeners();
			
			System.Func<int, UnityEngine.Events.UnityAction> actionBuilder = (connectedElementIndex) => () => OnClickButtonConnectElement(connectedElementIndex);
			UnityEngine.Events.UnityAction action1 = actionBuilder(i);
			btn.onClick.AddListener(action1);
		}
	}

	private void OnClickButtonConnectElement(int connectedElementIndex) {
		// connectedElementIndex: Index of which Index in List was clicked
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnClickButtonConnectElement, connectedElementIndex: " + connectedElementIndex);
		#pragma warning restore CS0162
		
		// de-highlight all buttons if another button is pressed
		if (connectedElementIndex == connectedElementHighlitedIndex) {
			return;
		}

		// reset BackgroundColors:
		foreach (var connectedElement in connectedElements) {
			foreach (var item in connectedElement.GetComponentInChildren<Button>().GetComponentsInChildren<Image>()) {
				if (item.name == "Image Background") {
					item.color = colorDefault;
					break;
				}
			}
		}
		// set BackgroundColor
		foreach (var item in connectedElements[connectedElementIndex].GetComponentInChildren<Button>().GetComponentsInChildren<Image>()) {
			if (item.name == "Image Background") {
				item.color = colorHighlited;
				break;
			}
		}
		connectedElementHighlitedIndex = connectedElementIndex;

		RefreshPanelSubscription(connectedElementIndex, null);
	}

	public void OnClickButtonSubscribe(int index) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnClickButtonSubscribe, Button: " + buttonsSubscription[index].name);
		#pragma warning restore CS0162

		// Toggle state
		bool isOn = (buttonsSubscription[index].image.sprite.name.Split('_')[1] == "On") ? true : false;
		buttonsSubscription[index].image.sprite = isOn?buttonOff:buttonOn;
		isOn = !isOn;

		string serial = MovesenseDevice.Devices[connectedElementHighlitedIndex].Serial;

		string subscriptionPath = null;
		int? sampleRate = null;
		string subscriptionChar = null; // only for logging

		switch (index) {
			case 0:
				subscriptionPath = SubscriptionPath.LinearAcceleration;
				sampleRate = SampleRate.slowest;
				subscriptionChar = "LinearAcceleration";
			break;
			case 1:
				subscriptionPath = SubscriptionPath.AngularVelocity;
				sampleRate = SampleRate.slowest;
				subscriptionChar = "AngularVelocity";
			break;
			case 2:
				subscriptionPath = SubscriptionPath.MagneticField;
				sampleRate = SampleRate.slowest;
				subscriptionChar = "MagneticField";
			break;
			case 3:
				subscriptionPath = SubscriptionPath.HeartRate;
				subscriptionChar = "HeartRate";
			break;
			case 4:
				subscriptionPath = SubscriptionPath.Temperature;
				subscriptionChar = "Temperature";
			break;
		}

		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "onClickButtonSubscribe, " + (isOn ? "" : "un") + "subscribe " + subscriptionChar + " for " + serial);
		#pragma warning restore CS0162

		if (isOn) {
			MovesenseController.Subscribe(serial, subscriptionPath, sampleRate);
		} else {
			MovesenseController.UnSubscribe(serial, subscriptionPath);
			// clear values
			Invoke("RefreshPanelSubscriptionDelayed", 0.2F);
		}

		// ButtonVisualize gets active, if any Subscription is active
		if (MovesenseDevice.isAnySubscribed(SubscriptionPath.AngularVelocity, SubscriptionPath.LinearAcceleration)) {
			buttonVisualize.gameObject.SetActive(true);
		} else {
			buttonVisualize.gameObject.SetActive(false);
		}
	}
	void RefreshPanelSubscriptionDelayed() {
		RefreshPanelSubscription(connectedElementHighlitedIndex, null);
	}

	void RefreshPanelSubscription(int connectedElementIndex, NotificationCallback.EventArgs e) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "RefreshPanelSubscription, connectedElementIndex: " + connectedElementIndex + ", e.Data: " + (e==null?"e == null!":e.Data));
		#pragma warning restore CS0162
		if (e == null) { // @Start or on DisconnectEvent or if another Sensor is selected
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "RefreshPanelSubscription, refreshing Sensorlist");
			#pragma warning restore CS0162
			// check subscriptionTypes per serial in MovesenseDevice
			if (MovesenseDevice.Devices.Count == 0) {
				Debug.LogError(TAG + "RefreshPanelSubscription, MovesenseDevice.Devices.Count == 0");
				return;
			}

			Dictionary<string, int?> subscriptionTypes = new Dictionary<string, int?>();
			if (MovesenseDevice.GetAllSubscriptionPaths(MovesenseDevice.Devices[connectedElementIndex].Serial) != null) {
				subscriptionTypes = new Dictionary<string, int?>(MovesenseDevice.GetAllSubscriptionPaths(MovesenseDevice.Devices[connectedElementIndex].Serial));
			}
			
			if (subscriptionTypes.ContainsKey(SubscriptionPath.LinearAcceleration)) {
				buttonsSubscription[0].image.sprite = buttonOn;
				// Example for getting samplerate: 
				// Debug.Log("SampleRate: " + subscriptionTypes[SubscriptionPath.LinearAcceleration]);
				TextLinAcc[0].text = "...";
				TextLinAcc[1].text = "...";
				TextLinAcc[2].text = "...";
			} else {
				buttonsSubscription[0].image.sprite = buttonOff;
				TextLinAcc[0].text = "--";
				TextLinAcc[1].text = "--";
				TextLinAcc[2].text = "--";
			}
			if (subscriptionTypes.ContainsKey(SubscriptionPath.AngularVelocity)) {
				buttonsSubscription[1].image.sprite = buttonOn;
				TextGyro[0].text = "...";
				TextGyro[1].text = "...";
				TextGyro[2].text = "...";
			} else {
				buttonsSubscription[1].image.sprite = buttonOff;
				TextGyro[0].text = "--";
				TextGyro[1].text = "--";
				TextGyro[2].text = "--";
			}
			if (subscriptionTypes.ContainsKey(SubscriptionPath.MagneticField)) {
				buttonsSubscription[2].image.sprite = buttonOn;
				TextMagnField[0].text = "...";
				TextMagnField[1].text = "...";
				TextMagnField[2].text = "...";
			} else {
				buttonsSubscription[2].image.sprite = buttonOff;
				TextMagnField[0].text = "--";
				TextMagnField[1].text = "--";
				TextMagnField[2].text = "--";
			}
			if (subscriptionTypes.ContainsKey(SubscriptionPath.HeartRate)) {
				buttonsSubscription[3].image.sprite = buttonOn;
				TextHeartrate.text = "...";
				TextRrData.text = "...";
			} else {
				buttonsSubscription[3].image.sprite = buttonOff;
				TextHeartrate.text = "--";
				TextRrData.text = "--";
			}
			Pulsing.ObjectTransform.IsPulsing = false;
			if (subscriptionTypes.ContainsKey(SubscriptionPath.Temperature)) {
				buttonsSubscription[4].image.sprite = buttonOn;
				TextTemp.text = "...";
			} else {
				buttonsSubscription[4].image.sprite = buttonOff;
				TextTemp.text = "--";
			}
		} else {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "RefreshPanelSubscription, refreshing Subscriptionlist");
			#pragma warning restore CS0162
			// only highlighted Sensordata will be updated
			if (connectedElementIndex != connectedElementHighlitedIndex) {
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "Values for "+MovesenseDevice.Devices[connectedElementIndex].Serial+" do not match displayed " + MovesenseDevice.Devices[connectedElementHighlitedIndex].Serial);
				#pragma warning restore CS0162
				return;
			}
			
			if (e.Subscriptionpath == SubscriptionPath.LinearAcceleration) {
				var notificationFieldArgs = (NotificationCallback.FieldArgs) e;
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "updating LinearAcceleration texts");
				#pragma warning restore CS0162
				TextLinAcc[0].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x.ToString("F6");
				TextLinAcc[1].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y.ToString("F6");
				TextLinAcc[2].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z.ToString("F6");
			} else if (e.Subscriptionpath == SubscriptionPath.AngularVelocity) {
				var notificationFieldArgs = (NotificationCallback.FieldArgs) e;
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "updating Gyroscope texts");
				#pragma warning restore CS0162
				TextGyro[0].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x.ToString("F6");
				TextGyro[1].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y.ToString("F6");
				TextGyro[2].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z.ToString("F6");
			} else if (e.Subscriptionpath == SubscriptionPath.MagneticField) {
				var notificationFieldArgs = (NotificationCallback.FieldArgs) e;
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "updating Magnetic texts");
				#pragma warning restore CS0162
				TextMagnField[0].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].x.ToString("F6");
				TextMagnField[1].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].y.ToString("F6");
				TextMagnField[2].text = notificationFieldArgs.Values[notificationFieldArgs.Values.Length-1].z.ToString("F6");
			} else if (e.Subscriptionpath == SubscriptionPath.HeartRate) {
				var notificationHeartRateArgs = (NotificationCallback.HeartRateArgs) e;
				TextHeartrate.text = notificationHeartRateArgs.Pulse.ToString("F0");
				TextRrData.text = notificationHeartRateArgs.RrData[notificationHeartRateArgs.RrData.Length-1].ToString("F0");
				Pulsing.ObjectTransform.BPM = (int)notificationHeartRateArgs.Pulse;
				Pulsing.ObjectTransform.IsPulsing = true;
			} else if (e.Subscriptionpath == SubscriptionPath.Temperature) {
				var notificationTemperatureArgs = (NotificationCallback.TemperatureArgs) e;
				TextTemp.text = (notificationTemperatureArgs.Temperature-273.15).ToString("F1");
			}
		}
	}
}
