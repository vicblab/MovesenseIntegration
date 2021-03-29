using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using AOT;
using System.Runtime.InteropServices;
#endif


public class MovesenseController : MonoBehaviour {
	private const string	TAG = "MovesenseController; ";
	public const bool		isLogging = false;

	private const string URI_EVENTLISTENER = "suunto://MDS/EventListener";

	public enum EventType {
        CONNECTING,
        CONNECTED,
        DISCONNECTED,
        NOTIFICATION,
        RESPONSE
    }

	#region Plugin import
		#if UNITY_ANDROID && !UNITY_EDITOR
			private static AndroidJavaObject movesensePlugin;
		#elif UNITY_IOS && !UNITY_EDITOR	
    		private delegate void CallbackConnect(string macID);
    		private delegate void CallbackConnectionComplete(string macID, string serial);
    		private delegate void CallbackConnectError(string exception);
    		private delegate void CallbackDisconnect(string macID);
			[DllImport ("__Internal")]
			private static extern void InitMDS(CallbackConnect onConnect, CallbackConnectionComplete onConnectionComplete, CallbackConnectError onError, CallbackDisconnect onDisconnect);
			[DllImport ("__Internal")]
			private static extern void ConnectMDS(string macID);
			[DllImport ("__Internal")]
			private static extern bool DisConnectMDS(string macID);
			private static ConnectCallback callbackObject;

    		private delegate void CallbackNotification(string data, string serial, string subscriptionPath);
    		private delegate void CallbackNotificationError(string exception);
			[DllImport ("__Internal")]
			private static extern void SubscribeMDS(string serial, string subscriptionPath, string sampleRate, string jsonParameters, CallbackNotification onNotification, CallbackNotificationError onError);
			[DllImport ("__Internal")]
			private static extern void UnSubscribeMDS(string serial, string path);
			private static NotificationCallback notificationObject = new NotificationCallback();

			private delegate void CallbackResponse(string data, string method, string uri);
    		private delegate void CallbackResponseError(string exception);
			[DllImport ("__Internal")]
			private static extern void GetMDS(string serial, string path, string jsonParameters, CallbackResponse onSuccess, CallbackResponseError onError);
			[DllImport ("__Internal")]
			private static extern void PutMDS(string serial, string path, string jsonParameters, CallbackResponse onSuccess, CallbackResponseError onError);
			[DllImport ("__Internal")]
			private static extern void PostMDS(string serial, string path, string jsonParameters, CallbackResponse onSuccess, CallbackResponseError onError);
			[DllImport ("__Internal")]
			private static extern void DeleteMDS(string serial, string path, string jsonParameters, CallbackResponse onSuccess, CallbackResponseError onError);
			private static ResponseCallback responseObject = new ResponseCallback();
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	#endregion

	#region Variables
		public static bool				isInitialized;		
		private List<System.EventArgs>	notificationCallbackEventArgs = new List<System.EventArgs>();
		private ReaderWriterLockSlim	notificationLock = new ReaderWriterLockSlim();
		
		private List<System.EventArgs>	connectEventArgs = new List<System.EventArgs>();
		private ReaderWriterLockSlim	connectLock = new ReaderWriterLockSlim();
		private List<System.EventArgs>	disConnectEventArgs = new List<System.EventArgs>();
		private ReaderWriterLockSlim	disConnectLock = new ReaderWriterLockSlim();
		
		private List<System.EventArgs>	responseEventArgs = new List<System.EventArgs>();
		private ReaderWriterLockSlim	responseLock = new ReaderWriterLockSlim();

		
	#endregion

	#region iOS-Callback methods
		#if UNITY_IOS && !UNITY_EDITOR
		[MonoPInvokeCallback(typeof(CallbackConnect))]
		private static void onConnect(string macID) {
			callbackObject.onConnect(macID);
		}

		[MonoPInvokeCallback(typeof(CallbackConnectionComplete))]
		private static void onConnectionComplete(string macID, string serial) {
			callbackObject.onConnectionComplete(macID, serial);
		}

		[MonoPInvokeCallback(typeof(CallbackConnectError))]
		private static void onConnectError(string exception) {
			callbackObject.onError(exception);
		}

		[MonoPInvokeCallback(typeof(CallbackDisconnect))]
		private static void onDisconnect(string macID) {
			callbackObject.onDisconnect(macID);
		}

		[MonoPInvokeCallback(typeof(CallbackNotification))]
		private static void onNotification(string data, string serial, string subscriptionPath) {
			notificationObject.onNotification(data, serial, subscriptionPath);
		}

		[MonoPInvokeCallback(typeof(CallbackNotificationError))]
		private static void onNotificationError(string exception) {
			notificationObject.onError(exception);
		}

		[MonoPInvokeCallback(typeof(CallbackResponse))]
		private static void onResponse(string data, string method, string uri) {
			responseObject.onSuccess(data, method, uri);
		}

		[MonoPInvokeCallback(typeof(CallbackResponseError))]
		private static void onResponseError(string exception) {
			responseObject.onError(exception);
		}
		#endif
	#endregion

	#region Event
		[Serializable]
		public sealed class EventArgs : System.EventArgs {
			public EventType  Type { get; private set; }
			public string InvokeMethod { get; private set; }
			public List<System.EventArgs> OriginalEventArgs { get; private set; }

			public EventArgs (EventType type, string invokeMethod, List<System.EventArgs> originalEventArgs) {
				Type = type;
				InvokeMethod = invokeMethod;
				OriginalEventArgs = originalEventArgs;
			}
		}
		//provide Events
		public static event	EventHandler<EventArgs> Event;
	#endregion


	private void OnDestroy() {
		ConnectCallback.Event -= OnConnectCallbackEvent;
		NotificationCallback.Event -= OnNotificationCallbackEvent;
		notificationLock.Dispose();
		responseLock.Dispose();
		ResponseCallback.Event -= OnResponseCallbackEvent;
	}
	void Awake() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "Awake");
		#pragma warning restore CS0162
		
		if (FindObjectsOfType(GetType()).Length > 1) {
             Destroy(gameObject);
        } else {
			DontDestroyOnLoad(transform.gameObject);
			ConnectCallback.Event += OnConnectCallbackEvent;
			NotificationCallback.Event += OnNotificationCallbackEvent;
			ResponseCallback.Event += OnResponseCallbackEvent;
		}
	}
	
	void Start() {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "Start: Initializing Movesense-Plugin");
		#pragma warning restore CS0162
		Initialize();
	}

	void Initialize() {
		if (!isInitialized) {
			#pragma warning disable CS0162
			if (isLogging) Debug.Log (TAG + "Initialize");
			#pragma warning restore CS0162
			#if UNITY_ANDROID && !UNITY_EDITOR
				AndroidJavaClass jcUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        		AndroidJavaObject currentActivity = jcUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");	

				AndroidJavaClass jcMds = new AndroidJavaClass("com.movesense.mds.Mds"); // name of the class not the plugin-file
				
				var builder = jcMds.CallStatic<AndroidJavaObject>("builder");

				movesensePlugin = builder.Call<AndroidJavaObject>("build", currentActivity);
			#elif UNITY_IOS && !UNITY_EDITOR
				callbackObject = new ConnectCallback();
				
				InitMDS(onConnect, onConnectionComplete, onConnectError, onDisconnect);
			#elif UNITY_STANDALONE_OSX || UNITY_EDITOR			
			#endif

			isInitialized = true;
			#pragma warning disable CS0162
			if (isLogging) Debug.Log (TAG + "Mds initialized");
			#pragma warning restore CS0162
		}
	}

	public static void Connect(string MacID) {
		// mds checks, if already connecting or connected

		if (!isInitialized) {
			Debug.LogError(TAG + "Connect: MovesenseController is not initialized. Did you forget to add MovesenseController object in the scene?");
			return;
		}

		string serial = MovesenseDevice.GetSerial(MacID);

		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "Connect: " + MacID + " (" + serial + ")");
		#pragma warning restore CS0162

		MovesenseDevice.SetConnecting(MacID);
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "Connect, raising Connecting-event");
		#pragma warning restore CS0162
		if (Event != null) {
			Event(null, new EventArgs(EventType.CONNECTING, TAG + "Connect", new List<System.EventArgs> { new ConnectCallback.EventArgs(false, MacID, serial)}));
		}
		
		#if UNITY_ANDROID && !UNITY_EDITOR
			movesensePlugin.Call("connect", MacID, new ConnectCallback());
		#elif UNITY_IOS && !UNITY_EDITOR
			ConnectMDS(MacID);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR			
		#endif
	}

	public static void Disconnect(string MacID) {
		// mds checks, if device is connected

		string serial = MovesenseDevice.GetSerial(MacID);
		
		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "Disconnect: " + MacID + " (" + serial + ")");
		#pragma warning restore CS0162
		#if UNITY_ANDROID && !UNITY_EDITOR
			movesensePlugin.Call("disconnect", MacID);
		#elif UNITY_IOS && !UNITY_EDITOR
			DisConnectMDS(MacID);
			
			if (MovesenseDevice.GetConnectingState(MacID)) {
				// connection has not been completed => there will be no callback if disconnect is called
				MovesenseDevice.SetConnectionState(MacID, false);

				#pragma warning disable CS0162
				if (isLogging) Debug.Log(TAG + "Disconnect, while connecting, raising Disconnect-event");
				#pragma warning restore CS0162
				if (Event != null) {				
					Event(null, new EventArgs(EventType.DISCONNECTED, TAG + "Disconnect",new List<System.EventArgs> { new ConnectCallback.EventArgs(false, MacID, serial) }));
				}
			}
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR		
		#endif		
	}

	public static void Subscribe(string Serial, string Subscriptionpath, int? Samplerate) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "Subscribe: " + Serial + ", Subscriptionpath: " + Subscriptionpath + ", Samplerate: " + Samplerate);
		#pragma warning restore CS0162

		// check correct format
		if ((Subscriptionpath == SubscriptionPath.LinearAcceleration || Subscriptionpath == SubscriptionPath.AngularVelocity || Subscriptionpath == SubscriptionPath.MagneticField)
			&& Samplerate == null) {
            Debug.LogError (TAG + "Subscribe, Samplerate missing");
			return;
		} else if ((Subscriptionpath == SubscriptionPath.HeartRate || Subscriptionpath == SubscriptionPath.Temperature)
			&& Samplerate != null) {
            Debug.LogWarning (TAG + "Subscribe, ignoring Samplerate");
			Samplerate = null;
			return;
		}

		#if UNITY_ANDROID && !UNITY_EDITOR
			MovesenseDevice.AddSubscription(Serial, Subscriptionpath,
											new MovesenseDevice.SubscriptionSection(Samplerate,
												movesensePlugin.Call<AndroidJavaObject>("subscribe", URI_EVENTLISTENER, BuildContract(Serial, Subscriptionpath+Samplerate.ToString()),
												new NotificationCallback(Serial, Subscriptionpath))));
		#elif UNITY_IOS && !UNITY_EDITOR
			SubscribeMDS(Serial, Subscriptionpath, Samplerate.ToString(), "{}", onNotification, onNotificationError);
			MovesenseDevice.AddSubscription(Serial, Subscriptionpath, new MovesenseDevice.SubscriptionSection(Samplerate));
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}
	private static string BuildContract(string name, string uri) {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
		string returnString = sb.Append("{\"Uri\": \"").Append(name).Append("/").Append(uri).Append("\"}").ToString();
		// Debug.Log(TAG + "BuildContract: " + returnString);
        return returnString;
	}

	public static void UnSubscribe(string Serial, string SubscriptionPath) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log (TAG + "UnSubscribe: " + Serial + ", SubscriptionPath: " + SubscriptionPath);
		#pragma warning restore CS0162

		#if UNITY_ANDROID && !UNITY_EDITOR
			MovesenseDevice.GetSubscription(Serial, SubscriptionPath).Call("unsubscribe");
			MovesenseDevice.RemoveSubscription(Serial, SubscriptionPath);
		#elif UNITY_IOS && !UNITY_EDITOR
			// Debug.Log(TAG + "GetSubscription: " + MovesenseDevice.GetSubscription(Serial, SubscriptionPath));
			UnSubscribeMDS(Serial, MovesenseDevice.GetSubscription(Serial, SubscriptionPath));
			MovesenseDevice.RemoveSubscription(Serial, SubscriptionPath);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}

	public static void ResponseGet(string Serial, string Path) {
		#if UNITY_ANDROID && !UNITY_EDITOR
			movesensePlugin.Call("get", "suunto://"+Serial+"/"+Path, null, new ResponseCallback("GET"));
		#elif UNITY_IOS && !UNITY_EDITOR
			GetMDS(Serial, Path, "{}", onResponse, onResponseError);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}

	/// <summary>
	/// Look at https://bitbucket.org/suunto/movesense-device-lib/src/3956a579c473/MovesenseCoreLib/resources/movesense-api/?at=master
	/// </summary>
	/// <param name="Serial"></param>
	/// <param name="Path"></param>
	/// <param name="JsonParameters">JSON formatted string</param>
	public static void ResponsePut(string Serial, string Path, string JsonParameters) {
		#if UNITY_ANDROID && !UNITY_EDITOR
			movesensePlugin.Call("put", "suunto://"+Serial+"/"+Path, JsonParameters, new ResponseCallback("PUT"));
		#elif UNITY_IOS && !UNITY_EDITOR
			PutMDS(Serial, Path, JsonParameters, onResponse, onResponseError);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}
	public static void ResponsePost(string Serial, string Path) {
		#if UNITY_ANDROID && !UNITY_EDITOR
			movesensePlugin.Call("post", "suunto://"+Serial+"/"+Path, null, new ResponseCallback("POST"));
		#elif UNITY_IOS && !UNITY_EDITOR
			PostMDS(Serial, Path, "{}", onResponse, onResponseError);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}
	public static void ResponseDelete(string Serial, string Path) {
		#if UNITY_ANDROID && !UNITY_EDITOR
			movesensePlugin.Call("delete", "suunto://"+Serial+"/"+Path, null, new ResponseCallback("DEL"));
		#elif UNITY_IOS && !UNITY_EDITOR
			DeleteMDS(Serial, Path, "{}", onResponse, onResponseError);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR
		#endif
	}

	void OnNotificationCallbackEvent(object sender, NotificationCallback.EventArgs e) {
		#pragma warning disable CS0162
		if (isLogging)
			Debug.Log(TAG + "OnNotificationCallbackEvent: " + e.Data);
		#pragma warning restore CS0162
		notificationLock.EnterWriteLock();
		try {
			notificationCallbackEventArgs.Add(e);
		} finally {
			notificationLock.ExitWriteLock();
		}
	}
	void OnConnectCallbackEvent(object sender, ConnectCallback.EventArgs e) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnConnectCallbackEvent, IsConnect: " + e.IsConnect + ", MacID: " + e.MacID + ", Serial: " + e.Serial);
		#pragma warning restore CS0162
		if (e.IsConnect) {
			 connectLock.EnterWriteLock();
			try {
				connectEventArgs.Add(e);
			} finally {
				connectLock.ExitWriteLock();
			}
			
			/*/ Test response
			ResponseGet(e.Serial, "Info");
				
			ResponseGet(e.Serial, "System/Energy/Level");

			ResponseGet(e.Serial, "Misc/Gear/Id");
			// End remove*/
		} else {
			disConnectLock.EnterWriteLock();
			try {
				disConnectEventArgs.Add(e);
			} finally {
				disConnectLock.ExitWriteLock();
			}
		}
	}

	void OnResponseCallbackEvent(object sender, ResponseCallback.EventArgs e) {
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "OnResponseCallbackEvent, Uri: " + e.Uri + ", Method: " + e.Method + ", Data: " + e.Data);
		#pragma warning restore CS0162
		responseLock.EnterWriteLock();
		try {
			responseEventArgs.Add(e);
		} finally {
			responseLock.ExitWriteLock();
		}
	}

	private void Update() {
		if (Event != null) { // Feature in case you forgot to subscribe to the event, no data will be lost
			connectLock.EnterUpgradeableReadLock();
			try {
				if (connectEventArgs.Count > 0) {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "Update, raising CONNECT-event");
					#pragma warning restore CS0162
					Event(null, new EventArgs(EventType.CONNECTED, TAG + "OnConnectCallbackEvent", connectEventArgs));
					connectLock.EnterWriteLock();
					try {
						connectEventArgs.Clear();
					} finally {
						connectLock.ExitWriteLock();
					}
				}
			} finally {
				connectLock.ExitUpgradeableReadLock();
			}

			disConnectLock.EnterUpgradeableReadLock();
			try {
				if (disConnectEventArgs.Count > 0) {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "Update, raising DISCONNECT-event");
					#pragma warning restore CS0162
					Event(null, new EventArgs(EventType.DISCONNECTED, TAG + "OnConnectCallbackEvent", disConnectEventArgs));
					disConnectLock.EnterWriteLock();
					try {
						disConnectEventArgs.Clear();
					} finally {
						disConnectLock.ExitWriteLock();
					}
				}
			} finally {
				disConnectLock.ExitUpgradeableReadLock();
			}

			notificationLock.EnterUpgradeableReadLock();
			try {
				if (notificationCallbackEventArgs.Count > 0) {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "Update, raising NOTIFICATION-event");
					#pragma warning restore CS0162
					Event(null, new EventArgs(EventType.NOTIFICATION, TAG + "OnNotificationCallbackEvent", notificationCallbackEventArgs));
					notificationLock.EnterWriteLock();
					try {
						notificationCallbackEventArgs.Clear();
					} finally {
						notificationLock.ExitWriteLock();
					}
				}
			} finally {
				notificationLock.ExitUpgradeableReadLock();
			}

			responseLock.EnterUpgradeableReadLock();
			try {
				if (responseEventArgs.Count > 0) {
					#pragma warning disable CS0162
					if (isLogging) Debug.Log(TAG + "Update, raising RESPONSE-event");
					#pragma warning restore CS0162
					Event(null, new EventArgs(EventType.RESPONSE, TAG + "OnResponseCallbackEvent", responseEventArgs));
					responseLock.EnterWriteLock();
					try {
						responseEventArgs.Clear();
					} finally {
						responseLock.ExitWriteLock();
					}
				}
			} finally {
				responseLock.ExitUpgradeableReadLock();
			}
		}
	}
}


// take a look at: https://bitbucket.org/suunto/movesense-device-lib/src/master/MovesenseCoreLib/resources/movesense-api/
public class SubscriptionPath {
	public const string LinearAcceleration = "Meas/Acc/";
	public const string AngularVelocity = "Meas/Gyro/";
	public const string MagneticField = "Meas/Magn/";
	public const string HeartRate = "Meas/HR";
	public const string Temperature = "Meas/Temp";
}

/// <summary>The samplerates here are the ones supported by current Movesense sensor. You can query the current supported sample rates and other info from the sensor path /Meas/[sensor]/Info </summary>
public class SampleRate {
	/// <summary>Updatefrequnzy: 13Hz</summary>
	public const int slowest = 13;
	/// <summary>Updatefrequnzy: 26Hz</summary>
	public const int slower = 26;
	/// <summary>Updatefrequnzy: 52Hz</summary>
	public const int medium = 52;
	/// <summary>Updatefrequnzy: 104Hz</summary>
	public const int fast = 104;
	/// <summary>Updatefrequnzy: 208Hz</summary>
	public const int faster = 208;
	/// <summary>Updatefrequnzy: 416Hz</summary>
	public const int fastest = 416;
}