using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class MovesenseDevice {
	private const string	TAG = "MovesenseDevice; ";
	private const bool		isLogging = false;
	public static bool		ShouldSortByRssi = true;

	/// <summary>MacAdress or identifier</summary>
	public string MacID;
	/// <summary>AdvertismentName</summary>
	public string Serial;
	/// <summary>Distance from Sensor to MobileDevice</summary>
	public int? Rssi;
	public bool IsConnecting = false;
	public bool IsConnected = false;
	public class SubscriptionSection {
		public int? Samplerate;
		#if UNITY_ANDROID && !UNITY_EDITOR
		public AndroidJavaObject AndroidjavaObject;
		#endif
		
		public SubscriptionSection(int? samplerate
		#if UNITY_ANDROID && !UNITY_EDITOR
		, AndroidJavaObject androidjavaObject
		#endif
		) {
			Samplerate = samplerate;
			#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidjavaObject = androidjavaObject;
			#endif
		}
	}
	/// <summary>Key: SubscriptionPath, Value: SampleRate and Android: subscription itself, iOS: subscriptionpath</summary>
	public Dictionary<string, SubscriptionSection> Subscriptions = new Dictionary<string, SubscriptionSection>();
	
	public MovesenseDevice(string macID, string serial, int? rssi, bool isConnecting, bool isConnected, Dictionary<string, SubscriptionSection> subscriptions) {
		MacID = macID;
		Serial = serial;
		Rssi = rssi;
		IsConnecting = isConnecting;
		IsConnected = isConnected;
		if (subscriptions != null) {
			Subscriptions = subscriptions;
		}
	}
	private static List<MovesenseDevice> _movesenseDevices = new List<MovesenseDevice>();
	

    public static ReadOnlyCollection<MovesenseDevice> Devices {
        get {
			return _movesenseDevices.AsReadOnly();
		}
    }
	public static void Add(MovesenseDevice movesenseDevice) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "Add: " + movesenseDevice.MacID + " (" + movesenseDevice.Serial + ")");
		#pragma warning restore CS0162

		_movesenseDevices.Add(movesenseDevice);

		if (ShouldSortByRssi) SortByRssi();

//		logMovesenseDevices();
	}
	public static string GetSerial(string MacID) {
		foreach (var item in _movesenseDevices) {
			if (item.MacID == MacID) {
				return item.Serial;
			}
		}

		Debug.LogWarning(TAG + "GetSerial: " + MacID + " is not available");

		return null;
	}

	public static string GetMacID(string Serial) {
		foreach (var item in _movesenseDevices) {
			if (item.Serial == Serial) {
				return item.MacID;
			}
		}

		Debug.LogWarning(TAG + "GetMacID: " + Serial + " is not available");

		return null;
	}

	public static bool ContainsMacID(string MacID) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].MacID == MacID) {
				return true;
			}
		}

		Debug.LogWarning(TAG + "ConatainsMacID: " + MacID + " is not available");

		return false;
	}

	public static int ContainsSerialAt(string Serial) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].Serial == Serial) {
				return i;
			}
		}

		Debug.LogWarning(TAG + "ContainsSerialAt: " + Serial + " is not available");

		return -1;
	}
	
	public static void RemoveAllExcept(List<string> RefresherList) {
		for (int i = _movesenseDevices.Count - 1; i >= 0 ; i--) {
			if (!RefresherList.Contains(_movesenseDevices[i].MacID) && !_movesenseDevices[i].IsConnected && !_movesenseDevices[i].IsConnecting) {
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "RemoveAllExcept: " + _movesenseDevices[i].MacID + " (" + _movesenseDevices[i].Serial + ")");
				#pragma warning restore CS0162

				_movesenseDevices.RemoveAt(i);
			}
		}
	}

	public static void RemoveUnconnected() {
		// removes devices, which are not connected AND not connecting
		// keeps devices, which are connected OR connecting
		for (int i = _movesenseDevices.Count - 1; i >= 0 ; i--) {
			if (!_movesenseDevices[i].IsConnected && !_movesenseDevices[i].IsConnecting) {
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "RemoveUnconnected: " + _movesenseDevices[i].MacID + " (" + _movesenseDevices[i].Serial + ")");
				#pragma warning restore CS0162

				_movesenseDevices.RemoveAt(i);
			}
		}
	}

	public static int? GetRssi(string MacID) {
		foreach (var item in _movesenseDevices) {
			if (item.MacID == MacID) {
				return item.Rssi;
			}
		}

		Debug.LogWarning(TAG + "GetRssi: " + MacID + " is not available");

		return 1;
	}

	public static void RefreshRssi(string MacID, int ReplaceRssi) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].MacID == MacID) {
				int? oldRssi = _movesenseDevices[i].Rssi;
				
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "RefreshRssi: " + MacID + " (" + _movesenseDevices[i].Serial + "): " + oldRssi + " => " + ReplaceRssi);
				#pragma warning restore CS0162

				_movesenseDevices[i].Rssi = ReplaceRssi;

				if (ShouldSortByRssi) SortByRssi();
				
				return;
			}
		}

		Debug.LogWarning(TAG + "RefreshRssi: " + MacID + " is not available");
	}

	public static void SortByRssi() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "SortByRssi: _movesenseDevices sorted");
		#pragma warning restore CS0162

		_movesenseDevices.Sort((d1,d2)=>d2.Rssi.GetValueOrDefault(int.MinValue).CompareTo(d1.Rssi.GetValueOrDefault(int.MinValue)));
	}
	
	public static bool GetConnectingState(string MacID) {
		foreach (var item in _movesenseDevices) {
			if (item.MacID == MacID) {
				return item.IsConnecting;
			}
		}

		Debug.LogWarning(TAG + "GetConnectingState: " + MacID + " is not available");

		return false;
	}

	public static bool GetConnectedState(string MacID) {
		foreach (var item in _movesenseDevices) {
			if (item.MacID == MacID) {
				return item.IsConnected;
			}
		}

		Debug.LogWarning(TAG + "GetConnectedState: " + MacID + " is not available");

		return false;
	}

	public static bool IsAnyConnectedOrConnecting() {
		foreach (var item in _movesenseDevices) {
			if (item.IsConnected || item.IsConnecting) {
				return true;
			}
		}

		return false;
	}

	public static int NumberOfConnectedDevices() {
		int counter = 0;
		foreach (var item in _movesenseDevices) {
			if (item.IsConnected) {
				counter++;
			}
		}

		return counter;
	}

	public static int NumberOfConnectDevices() {
		int counter = 0;
		foreach (var item in _movesenseDevices) {
			if (item.IsConnected || item.IsConnecting) {
				counter++;
			}
		}

		return counter;
	}

	public static void SetConnecting(string MacID) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].MacID == MacID) {
				string serial = _movesenseDevices[i].Serial;
				
				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "SetConnecting: " + MacID + " (" + serial + ")");
				#pragma warning restore CS0162

				_movesenseDevices[i].IsConnecting = true;

				if (ShouldSortByRssi) {
					_movesenseDevices[i].Rssi -= 500;
					SortByRssi();
				}

				return;
			}
		}

		Debug.LogWarning(TAG + "SetConnecting: " + MacID + " is not available");
	}

	/// <summary>Set parameter IsConnected of Device with MacID to true or false. IsConnecting is set to false </summary>
	public static bool SetConnectionState(string MacID, bool IsConnected) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].MacID == MacID) {
				string serial = _movesenseDevices[i].Serial;

				_movesenseDevices[i].IsConnected = IsConnected;
				
				_movesenseDevices[i].IsConnecting = false;

				if (IsConnected) {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "SetConnectionState: " + MacID + " (" + serial + "): connected");
					#pragma warning restore CS0162
				} else {
					Debug.LogWarning(TAG + "SetConnectionState: " + MacID + " (" + serial + "): disconnected, removing device");

					_movesenseDevices.RemoveAt(i);// Some Android smartphones(e.g. Samsung) don't detect device after disconnect
				}
				
				return true;
			}
		}

		Debug.LogWarning(TAG + "SetConnectionState: " + MacID + " is not available");

		return false;
	}

	public static void AddSubscription(string Serial, string SubscriptionPath, SubscriptionSection subscriptionSection) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "AddSubscription, serial: " + Serial + ", SubscriptionPath: " + SubscriptionPath + ", Samplerate: " + subscriptionSection.Samplerate);
		#pragma warning restore CS0162

		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].Serial == Serial) {
				// check, if key already exist
				if (_movesenseDevices[i].Subscriptions.ContainsKey(SubscriptionPath)) {
					Debug.LogWarning(TAG + "AddSubscription: SubscriptionPath "+ SubscriptionPath + " is already subscribed");
				} else {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "AddSubscription: " + _movesenseDevices[i].MacID + " (" + _movesenseDevices[i].Serial + "), Type: " + SubscriptionPath);
					#pragma warning restore CS0162

					_movesenseDevices[i].Subscriptions.Add(SubscriptionPath, subscriptionSection);
				}

				return;
			}
		}

		Debug.LogWarning(TAG + "AddSubscription: " + Serial + " is not available");
	}
	
#if UNITY_ANDROID && !UNITY_EDITOR
	public static AndroidJavaObject GetSubscription(string Serial, string SubscriptionPath) {
#else
	public static string GetSubscription(string Serial, string SubscriptionPath) {
#endif
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].Serial == Serial) {
				// check, if key exist
				if (_movesenseDevices[i].Subscriptions.ContainsKey(SubscriptionPath)) {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "GetSubscription: " + _movesenseDevices[i].MacID + " (" + _movesenseDevices[i].Serial + "), Type: " + SubscriptionPath);
					#pragma warning restore CS0162

					#if UNITY_ANDROID && !UNITY_EDITOR
						return _movesenseDevices[i].Subscriptions[SubscriptionPath].AndroidjavaObject;
					#elif UNITY_IOS && !UNITY_EDITOR
						if (_movesenseDevices[i].Subscriptions[SubscriptionPath].Samplerate == null) {
							return SubscriptionPath;
						} else {
							return SubscriptionPath + _movesenseDevices[i].Subscriptions[SubscriptionPath].Samplerate;
						}
					#endif
				} else {
					Debug.LogWarning(TAG + "GetSubscription: SubscriptionPath "+ SubscriptionPath + " is NOT subscribed");					
				}

				return null;
			}
		}

		Debug.LogWarning(TAG + "GetSubscription: " + Serial + " is not available");

		return null;
	}
	
	public static Dictionary<string, int?> GetAllSubscriptionPaths(string Serial) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].Serial == Serial) {
				
				Dictionary<string, int?> returnList = new Dictionary<string, int?>();
				
				foreach (var subscription in _movesenseDevices[i].Subscriptions) {
					if (subscription.Key.Contains(SubscriptionPath.LinearAcceleration)) {
						returnList.Add(SubscriptionPath.LinearAcceleration, subscription.Value.Samplerate);
					} else if (subscription.Key.Contains(SubscriptionPath.AngularVelocity)) {
						returnList.Add(SubscriptionPath.AngularVelocity, subscription.Value.Samplerate);
					} else if (subscription.Key.Contains(SubscriptionPath.MagneticField)) {
						returnList.Add(SubscriptionPath.MagneticField, subscription.Value.Samplerate);
					} else if (subscription.Key.Contains(SubscriptionPath.HeartRate)) {
						returnList.Add(SubscriptionPath.HeartRate, null);
					} else if (subscription.Key.Contains(SubscriptionPath.Temperature)) {
						returnList.Add(SubscriptionPath.Temperature, null);
					}
				}
				
				if (returnList.Count == 0) {
					return null;
				}

				return returnList;
			}
		}

		Debug.LogWarning(TAG + "GetAllSubscriptionPaths: " + Serial + " is not available");

		return null;
	}
	public static void RemoveSubscription(string Serial, string SubscriptionPath) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			if (_movesenseDevices[i].Serial == Serial) {
				// check, if key already exist
				if (_movesenseDevices[i].Subscriptions.ContainsKey(SubscriptionPath)) {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "RemoveSubscription: " + _movesenseDevices[i].MacID + " (" + _movesenseDevices[i].Serial + "), Type: " + SubscriptionPath);
					#pragma warning restore CS0162

					_movesenseDevices[i].Subscriptions.Remove(SubscriptionPath);
				} else {
					Debug.LogWarning(TAG + "RemoveSubscription: subscription " + SubscriptionPath + " is NOT subscribed");
				}

				return;
			}
		}

		Debug.LogWarning(TAG + "RemoveSubscription: " + Serial + " is not available");
	}
	public static bool isAnySubscribed(params string[] SubscriptionPaths) {
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			foreach (var SubscriptionPath in SubscriptionPaths) {
				foreach (var keyitem in _movesenseDevices[i].Subscriptions.Keys) {
					if (keyitem.Contains(SubscriptionPath)) {
						return true;
					}
				}
			}
		}

		return false;
	}

	public static void logMovesenseDevices() {
        string output = "movesenseDevices: ";
		for (int i = 0; i < _movesenseDevices.Count; i++) {
			output += "\n["+i+"] ";
            output += _movesenseDevices[i].MacID + ", ";
            output += _movesenseDevices[i].Serial+ ", ";
            output += _movesenseDevices[i].Rssi;
		}
        Debug.Log(output);
    }
}