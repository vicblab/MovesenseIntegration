using System;
using UnityEngine;

public class ResponseCallback
		#if UNITY_ANDROID && !UNITY_EDITOR
			: AndroidJavaProxy
		#endif
{
	private const string	TAG = "ResponseCallback; ";
	private const bool		isLogging = false;
	#if UNITY_ANDROID && !UNITY_EDITOR
	private readonly string method;
	#endif

	[Serializable]
	public class EventArgs : System.EventArgs {
		public string Uri { get; private set; }
		public string Method { get; private set; }
		public string Data { get; private set; }
		public EventArgs (string uri, string method, string data) {
			Uri = uri;
			Method = method;
			Data = data;
		}
	}
	public static event    EventHandler<EventArgs> Event;
	
	public ResponseCallback(
		#if UNITY_ANDROID && !UNITY_EDITOR
			string method
		#endif
	)
		#if UNITY_ANDROID && !UNITY_EDITOR
			: base("com.movesense.mds.MdsResponseListener")
		#endif
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		this.method = method;
		#endif
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "assigned");
		#pragma warning restore CS0162
	}

	/// <summary>Called when Mds operation has been succesfully finished</summary>
	public void onSuccess(string data,
	#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJavaObject header
	#else
		string method,
		string header
	#endif
	){
		string uri = null;
		#if UNITY_ANDROID && !UNITY_EDITOR
			uri = header.Call<string>("getUri");
			string method = this.method;
		#elif UNITY_IOS && !UNITY_EDITOR
			uri = header;
		#endif
		#pragma warning disable CS0162
		if (isLogging) Debug.Log(TAG + "onSuccess, uri: " + uri + ", method: " + method + ", data: " + data);
		#pragma warning restore CS0162
		if (Event != null) {
			Event(null, new EventArgs(uri, method, data));
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
}