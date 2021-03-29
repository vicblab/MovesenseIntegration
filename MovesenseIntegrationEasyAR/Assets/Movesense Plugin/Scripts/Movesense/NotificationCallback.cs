using System;
using UnityEngine;

public class NotificationCallback
		#if UNITY_ANDROID && !UNITY_EDITOR
			: AndroidJavaProxy
		#endif
{
	private const string	TAG = "NotificationCallback; ";
	private const bool		isLogging = false;

	private string serial;
	private string subscriptionPath; // without samplerate

	[Serializable]
	public class EventArgs : System.EventArgs {
		public string Serial { get; private set; }
		public string Subscriptionpath { get; private set; }
		public string Data { get; private set; }
		public EventArgs (string serial, string subscriptionPath, string data) {
			Serial = serial;
			Subscriptionpath = subscriptionPath;
			Data = data;
		}
	}
	public static event EventHandler<EventArgs> Event;

	[Serializable]
	public sealed class FieldArgs : EventArgs {
		public MeasurementValues[] Values { get; private set; }
		public FieldArgs (string serial, string type, string data, MeasurementValues[] values) : base(serial, type, data) {
			Values = values;
		}
	}
	[Serializable]
	public sealed class HeartRateArgs : EventArgs {
		public double Pulse { get; private set; }
		public int[] RrData { get; private set; }
		public HeartRateArgs (string serial, string data, double pulse, int[] rrData)  : base(serial, SubscriptionPath.HeartRate, data) {
			Pulse = pulse;
			RrData = rrData;
		}
	}
	public sealed class TemperatureArgs : EventArgs {
		public double Temperature { get; private set; }
		public TemperatureArgs (string serial, string data, double temperature) : base(serial, SubscriptionPath.Temperature, data) {
			Temperature = temperature;
		}
	}

	
	public NotificationCallback(
		#if UNITY_ANDROID && !UNITY_EDITOR
			string serial, string subscriptionPath) : base("com.movesense.mds.MdsNotificationListener"
		#endif
		)
	{
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "assigned");
		#pragma warning restore CS0162
		#if UNITY_ANDROID && !UNITY_EDITOR
		this.serial = serial;
		this.subscriptionPath = subscriptionPath;
		#endif
	}
	
	#if UNITY_IOS && !UNITY_EDITOR
	public void onNotification(string data, string serial, string subscriptionPath) {
		#pragma warning disable CS0162
		if (isLogging) {
			Debug.Log(TAG + "onNotification(iOS), data: " + data);
			Debug.Log(TAG + "onNotification(iOS), serial:" + serial);
			Debug.Log(TAG + "onNotification(iOS), subscriptionPath: "+ subscriptionPath);
		}
		#pragma warning restore CS0162
		this.serial = serial;
		this.subscriptionPath = subscriptionPath;
		onNotification(data);
	}
	#endif
	
	/// <summary>Called when data(JSON formatted string) arrives</summary>
	public void onNotification(string data) {
		Notification notification = JsonUtility.FromJson<Notification>(data);
		#pragma warning disable CS0162
		if (isLogging) {
			Debug.Log(TAG + "onNotification, data: " + data);
			Debug.Log(TAG + "onNotification, notification.Uri:" + notification.Uri);
			Debug.Log(TAG + "onNotification, serial: "+ serial);
			Debug.Log(TAG + "onNotification, subscriptionPath: "+ subscriptionPath);
		}
		#pragma warning restore CS0162

		EventArgs eventArgs;

		if (subscriptionPath == SubscriptionPath.LinearAcceleration) {
			eventArgs = new FieldArgs(serial, subscriptionPath, data, notification.Body.ArrayAcc);
		} else if (subscriptionPath == SubscriptionPath.AngularVelocity) {
			eventArgs = new FieldArgs(serial, subscriptionPath, data, notification.Body.ArrayGyro);
		} else if (subscriptionPath == SubscriptionPath.MagneticField) {
			eventArgs = new FieldArgs(serial, subscriptionPath, data, notification.Body.ArrayMagn);
		} else if (subscriptionPath == SubscriptionPath.HeartRate) {
			double pulse = notification.Body.average;
			eventArgs = new HeartRateArgs(serial, data, pulse, notification.Body.rrData);
		} else if (subscriptionPath == SubscriptionPath.Temperature) {
			double temperature = notification.Body.Measurement;
			eventArgs = new TemperatureArgs(serial, data, temperature);
		} else {
			// for custom paths
			eventArgs = new EventArgs(serial, subscriptionPath, data);
		}
		
		if (Event != null) {
			Event(null, eventArgs);
		}
	}

	/// <summary>Called when an error occurs</summary>
    public void onError(
	#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJavaObject
	#else
		string
	#endif
		error
	){
		Debug.LogError(TAG + "onError, error: " + error
		#if UNITY_ANDROID && !UNITY_EDITOR
			.Call<string>("getMessage")
		#endif
		);
	}


	[Serializable]
	private class Notification {
		public Body Body = null;
		public string Uri = null;
		public string Method = null;
	}
	[Serializable]
	private class Body {
		public int Timestamp = 0;
		public MeasurementValues[] ArrayAcc = null;
		public MeasurementValues[] ArrayGyro = null;
		public MeasurementValues[] ArrayMagn = null;
		public double average = 0;
		public int[] rrData = null;
		public double Measurement = 0;

	}
	[Serializable]
	public class MeasurementValues {
		public double x = 0;
		public double y = 0;
		public double z = 0;
	}
}