using UnityEngine;


public class Pulsing : MonoBehaviour {
	private static Pulsing	instance = null;
	private PulsingObject	_PulsingObject = new PulsingObject(false, 0);
	private float			bpmRate = 0;
	private float			scaleFactor = 1.3F;
	private Vector3			originScale = new Vector3();


	public class PulsingObject {
		public bool	IsPulsing = false;
		public int	BPM = 0;
		public PulsingObject(bool isPulsing, int bpm) {
			IsPulsing = isPulsing;
			BPM = bpm;
		}
	}

	public static PulsingObject ObjectTransform {
		get {
			return instance._PulsingObject;
		}
		set {
			instance._PulsingObject = value;
		}
	}

	void Awake() {
		instance = this;
	}

	void Start () {
		_PulsingObject.IsPulsing = false;
		originScale = transform.localScale;
	}
	
	void Update () {
		if (_PulsingObject.IsPulsing) {
			if (bpmRate <= 0) {
				if (_PulsingObject.BPM > 20) {
					bpmRate = 60000 / _PulsingObject.BPM;
					transform.localScale = new Vector3(originScale.x*scaleFactor,originScale.y*scaleFactor,originScale.z*scaleFactor);
				}
			} else {
				transform.localScale =	new Vector3(Mathf.Lerp(transform.localScale.x, originScale.x, 3*Time.deltaTime),
													Mathf.Lerp(transform.localScale.y, originScale.y, 3*Time.deltaTime),
													Mathf.Lerp(transform.localScale.z, originScale.z, 3*Time.deltaTime));	
			}
			bpmRate -= 1000*Time.deltaTime;
		}
	}
}
