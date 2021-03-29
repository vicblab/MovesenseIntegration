using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;


public enum BleHardwareState {
	UNKNOWN			= 0,
	RESETTING		= 1,
	UNSUPPORTED		= 2,
	UNAUTHORIZED	= 3,
	POWERED_OFF		= 4,
	POWERED_ON		= 5,
	LOCATION_OFF	= 6,
	LOCATION_ON		= 7,
	SCAN_READY		= 8
}

public class ScanController : MonoBehaviour {
	private const string	TAG = "ScanController; ";
	private const bool		isLogging = false;

	public enum EventType {
		SYSTEM_NOT_SCANNING,
		SYSTEM_SCANNING,
        NEW_DEVICE,
        RSSI,
        REFRESH, // every ScanController.deviceRefreshTime seconds the list is updated. Devices, which are no more available are removed from the Devicelist
        REMOVE_UNCONNECTED,
    }

	#region Plugin import
		#if UNITY_ANDROID && !UNITY
			private static AndroidJavaObject scanPlugin;
		#elif UNITY_IOS && !UNITY_EDITOR	
			[DllImport ("__Internal")]
			private static extern void InitScanPluginiOS(bool shouldLog);

			[DllImport ("__Internal")]
			private static extern void Dispose();
			
			[DllImport ("__Internal")]
			private static extern int GetBLEStatus(); // see iOS-Ble-Hardware-States

			[DllImport ("__Internal")]
			private static extern void Scan_iOS(string device);
			
			[DllImport ("__Internal")]
			private static extern void Stop_iOS();
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	#endregion

	#region Variables
		private static ScanController			instance = null;
		private bool						    isInitialized = false;
		private BleHardwareState				bleState;
		private const string 					uuidString = "61353090-8231-49cc-b57a-886370740041";
		private bool							isScanning = false;
		private List<String>					refresherList = new List<string>();
		private const float						deviceRefreshTime = 2.0F;
		private const float						rssiBlockTime = 1.0F;
		private bool							isIgnoringScanReport = false;
		private bool							isRefreshing = false;
		private bool							isRefreshingRssiBlocked = false;
		private bool							isStartRefresh = false;

		public static bool IsInitialized {
			get {
				if (instance == null) {
					return false;
				}
				return instance.isInitialized;
			}
			private set {
				instance.isInitialized = value;
			}
		}
		public static BleHardwareState BleState {
			get {
				return instance.bleState;
			}
			private set {
				instance.bleState = value;
			}
		}
		public static bool IsScanning {
			get {
				if (instance == null) {
					return false;
				}
				return instance.isScanning;
			}
			private set {
				instance.isScanning = value;
			}
		}
		public static List<String> RefresherList {
			get {
				return instance.refresherList;
			}
			private set {
				instance.refresherList = value;
			}
		}
		public static bool IsIgnoringScanReport {
			get {
				return instance.isIgnoringScanReport;
			}
			private set {
				instance.isIgnoringScanReport = value;
			}
		}
		public static bool IsRefreshing {
			get {
				return instance.isRefreshing;
			}
			private set {
				instance.isRefreshing = value;
			}
		}
		public static bool IsRefreshingRssiBlocked {
			get {
				return instance.isRefreshingRssiBlocked;
			}
			private set {
				instance.isRefreshingRssiBlocked = value;
			}
		}
		public static bool IsStartRefresh {
			get {
				return instance.isStartRefresh;
			}
			private set {
				instance.isStartRefresh = value;
			}
		}
	#endregion

	#region Event
		[Serializable]
		public sealed class EventArgs : System.EventArgs {
			public EventType Type { get; private set; }
			public string InvokeMethod { get; private set; }
			public string MacID { get; private set; }
			public EventArgs (EventType type, string invokeMethod, string macID) {
				Type = type;
				InvokeMethod = invokeMethod;
				MacID = macID;
			}
		}
		//provide Events
		public static event	EventHandler<EventArgs> Event;

	#endregion


	private void OnDestroy() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnDestroy");
		#pragma warning restore CS0162
		// Garbage native plugin
		#if UNITY_ANDROID && !UNITY_EDITOR
			if (scanPlugin != null) scanPlugin.Dispose();
		#elif UNITY_IOS && !UNITY_EDITOR
			Dispose();
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}
	void Awake() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "Awake");
		#pragma warning restore CS0162

		instance = this;
	}

	void Start() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "Start: Initializing Scan-Plugin");
		#pragma warning restore CS0162

		Initialize(false);
	}

	void Initialize(bool shouldSanPluginLog) {
		if (!isInitialized) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log (TAG + "Initialize");
			#pragma warning restore CS0162
			#if UNITY_ANDROID && !UNITY_EDITOR
				using (AndroidJavaClass jc = new AndroidJavaClass("com.kaasa.blescan.Ble_Scan_Android")) { // name of the class not the plugin-file
					scanPlugin = jc.CallStatic<AndroidJavaObject>("instance");
					scanPlugin.Call("InitScanPluginAndroid", shouldSanPluginLog);	
				}
			#elif UNITY_IOS && !UNITY_EDITOR
				InitScanPluginiOS(shouldSanPluginLog);	
			#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
			#endif

			isInitialized = true;
		}
	}

	#if UNITY_STANDALONE_OSX || UNITY_EDITOR
	public static void ReportActualBleState(string s_actualState) {
	#else
	public void ReportActualBleState(string s_actualState) {
	#endif
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "ReportActualBleState: state: " + s_actualState);
		#pragma warning restore CS0162
		BleState = (BleHardwareState)int.Parse(s_actualState);

		switch (BleState) {
			case BleHardwareState.RESETTING:
				Debug.Log(TAG + "Bluetooth-Hardware is resetting, waiting for turned on");
				break;
			case BleHardwareState.POWERED_OFF:
				Debug.Log(TAG + "User decided to keep Bluetooth off");
				break;
			case BleHardwareState.POWERED_ON:
				#if UNITY_ANDROID && !UNITY_EDITOR
					Debug.Log(TAG + "Bluetooth-Hardware is turned on, checking Location");
					if (!scanPlugin.Call<bool>("IsLocationTurnedOn")) {
						CheckBleStatus();
					} else {
						Scan();
					}
				#elif UNITY_IOS && !UNITY_EDITOR
					Debug.Log(TAG + "Bluetooth-Hardware is turned on");
					Scan();
				#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
				#endif
				break;
			case BleHardwareState.LOCATION_OFF:
				Debug.Log(TAG + "User decided to keep Location off");
				break;
			case BleHardwareState.LOCATION_ON:
				#if UNITY_ANDROID && !UNITY_EDITOR
					Debug.Log(TAG + "Location is on");
					if (!scanPlugin.Call<bool>("IsBluetoothTurnedOn")) {
						CheckBleStatus();
					} else {
						Scan();
					}
				#endif
				break;
		}
	}
	private static void CheckBleStatus() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "checking Ble status");
		#pragma warning restore CS0162
		#if UNITY_ANDROID && !UNITY_EDITOR
			if (!scanPlugin.Call<bool>("IsBleFeatured")) {
				Debug.Log(TAG + "Bluetooth is not featured");
			} else {
				if (!scanPlugin.Call<bool>("IsBluetoothAvailable")) {
					Debug.Log(TAG + "Bluetooth is not available");
				} else {
					if (!scanPlugin.Call<bool>("IsBluetoothTurnedOn")) {
						Debug.Log(TAG + "Bluetooth is powered_off, try to turn on");
						scanPlugin.Call("EnableBluetooth");
					} else {
						if (!scanPlugin.Call<bool>("IsLocationTurnedOn")) {
							Debug.Log(TAG + "Location is off, try to turn on");
							scanPlugin.Call("EnableLocation");
						}
					}
				}
			}
		#elif UNITY_IOS && !UNITY_EDITOR
			switch (GetBLEStatus()) {
				case 0:
					Debug.Log(TAG + "CBManagerStateUnknown");
					break;
				case 1:
					Debug.Log(TAG + "CBManagerStateResetting");
					break;
				case 2:
					Debug.Log(TAG + "CBManagerStateUnsupported");
					break;
				case 3:
					Debug.Log(TAG + "CBManagerStateUnauthorized");
					break;
				case 4:
					Debug.Log(TAG + "CBManagerStatePoweredOff");
					break;
				case 5:
					Debug.Log(TAG + "CBManagerStatePoweredOn");
					break;
			}
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}

	public static void StartScan() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "StartScan, checking Ble-status");
		#pragma warning restore CS0162

		if (instance == null || !IsInitialized) {
			Debug.LogError(TAG + "StartScan: ScanController is not initialized. Did you forget to add ScanController object in the scene?");
			return;
		}

		#if UNITY_ANDROID && !UNITY_EDITOR
			if (!scanPlugin.Call<bool>("IsBleFeatured") || !scanPlugin.Call<bool>("IsBluetoothAvailable") || !scanPlugin.Call<bool>("IsBluetoothTurnedOn") || !scanPlugin.Call<bool>("IsLocationTurnedOn")) {
				Debug.Log(TAG + "Scan not possible");
				CheckBleStatus();
			} else {
				Scan();
			}
		#elif UNITY_IOS && !UNITY_EDITOR
			if (GetBLEStatus() != 5) {
				Debug.Log(TAG + "Scan not possible");
				CheckBleStatus();
			} else {
				Scan();
			}
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}
	private static void Scan() {
		if (IsScanning) {
			return;
		}
		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "Scan");
		#pragma warning restore CS0162
		IsIgnoringScanReport = false;
		IsScanning = true;
		
		MovesenseDevice.RemoveUnconnected();
		
		if (Event != null) {
			Event(null, new EventArgs(EventType.REMOVE_UNCONNECTED, TAG + "Scan", null));
		}
		
		#if UNITY_ANDROID && !UNITY_EDITOR
			scanPlugin.Call("Scan", uuidString);
		#elif UNITY_IOS && !UNITY_EDITOR
			Scan_iOS(uuidString);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif

		if (Event != null) {
			Event(null, new EventArgs(EventType.SYSTEM_SCANNING, TAG + "Scan", null));
		}
	}

	public static void StopScan() {
		if (!IsScanning) {
			return;
		}
		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "StopScan");
		#pragma warning restore CS0162
		IsIgnoringScanReport = true;
		IsScanning = false;
		StopRefreshDeviceList();
		#if UNITY_ANDROID && !UNITY_EDITOR
			scanPlugin.Call("StopScan");
		#elif UNITY_IOS && !UNITY_EDITOR
			Stop_iOS();
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
		
		if (Event != null) {
			Event(null, new EventArgs(EventType.SYSTEM_NOT_SCANNING, TAG + "StopScan", null));
		}
	}
	

	#if UNITY_STANDALONE_OSX || UNITY_EDITOR
	public static void ReportScan(string Device) {
	#else
	public void ReportScan(string Device) {
	#endif
		if (IsIgnoringScanReport) {
			return;
		}

		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "ReportScan: " + Device);
		#pragma warning restore CS0162
		
		StartRefreshDeviceList();

		//Structure from native connect:[MacAdress or Identifier],[Name from AdvertisementData],[rssi]
		string[] splitString = Device.Split(',');
		string s_rssi = splitString[2];
		int i_rssi = int.Parse(s_rssi);
		string serial = splitString[1].Split(' ')[1];
		string macID = splitString[0];

		if (MovesenseDevice.GetConnectingState(macID)) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + macID + " is connecting, cancel further processing");
			#pragma warning restore CS0162
			return;
		}

		if (!RefresherList.Contains(macID)) {
			RefresherList.Add(macID);
		}

		if (MovesenseDevice.ContainsMacID(macID)) {
			if (MovesenseDevice.GetRssi(macID) != i_rssi && !IsRefreshingRssiBlocked) {
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + macID + " (" + serial + ") already scanned, refreshing rssi");
				#pragma warning restore CS0162
				MovesenseDevice.RefreshRssi(macID, i_rssi);

				if (Event != null) {
					Event(null, new EventArgs(EventType.RSSI, TAG + "ReportScan", macID));
				}
			} else {
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + macID + " (" + serial + ") already scanned, " + (IsRefreshingRssiBlocked ? "refreshRssi blocked" : "same rssi") + ", cancel further processing");
				#pragma warning restore CS0162
				
				return;
			}
		} else {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + macID + " (" + serial + ") is new");
			#pragma warning restore CS0162
			
			MovesenseDevice movesenseDevice = new MovesenseDevice(macID, serial, i_rssi, false, false, null);
			MovesenseDevice.Add(movesenseDevice);
			if (Event != null) {
				Event(null, new EventArgs(EventType.NEW_DEVICE, TAG + "ReportScan", macID));
			}
		}

		StartRssiRefreshBlocker();
	}

	private static void StartRssiRefreshBlocker() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "StartRssiRefreshBlocker: isRefreshingRssiBlocked = true");
		#pragma warning restore CS0162
		IsRefreshingRssiBlocked = true;

		if (IsStartRefresh) {
			return;
		}	
		IsStartRefresh = true;
		instance.InvokeRepeating("SetisRefreshingRssiBlocked", rssiBlockTime, rssiBlockTime);
	}
	private void SetisRefreshingRssiBlocked() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "SetisRefreshingRssiBlocked");
		#pragma warning restore CS0162
		isRefreshingRssiBlocked = false;
	}

	public static void StartRefreshDeviceList() {
		if (!IsRefreshing) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log(TAG + "StartRefreshDeviceList");
			#pragma warning restore CS0162
			IsRefreshing = true;

			instance.InvokeRepeating("RefreshDeviceList", deviceRefreshTime, deviceRefreshTime);

			RefresherList.Clear();
		}
	}
	public static void StopRefreshDeviceList() {
		if (!IsRefreshing) {
			return;
		}
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "StopRefreshDeviceList");
		#pragma warning restore CS0162
		
		if (RefresherList.Count == 0 || !IsScanning) {
			IsRefreshing = false;
			instance.CancelInvoke("RefreshDeviceList");
		}

		RefresherList.Clear();

		IsRefreshingRssiBlocked = false;
		instance.CancelInvoke("SetisRefreshingRssiBlocked");
	}
	private void RefreshDeviceList() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "RefreshDeviceList");
		#pragma warning restore CS0162
		isIgnoringScanReport = true;

		MovesenseDevice.RemoveAllExcept(refresherList);
		
		if (Event != null) {
			Event(null, new EventArgs(EventType.REFRESH, TAG + "RefreshDeviceList", null));
		}

		StopRefreshDeviceList();

		isIgnoringScanReport = false;
	}
}