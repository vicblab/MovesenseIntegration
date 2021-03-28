using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorDataManager : MonoBehaviour
{

    //public GameObject leftFoot;
    public class SensorData
    {
        public Vector3 acceleration;

        public SensorData()
        {
            this.acceleration = Vector3.zero;
        }
    }
    public List<SensorObject> usedSensors = new List<SensorObject>();
    public Dictionary<SensorObject, SensorData> data = new Dictionary<SensorObject, SensorData>();

    private void Start()
    {
        MovesenseController.Event += OnMovesenseControllerCallbackEvent;
        void OnMovesenseControllerCallbackEvent(object sender, MovesenseController.EventArgs e)
        {
            //Debug.Log("OnMovesenseControllerCallbackEvent, Type: " + e.Type + ", invoked by: " + e.InvokeMethod);
            switch (e.Type)
            {
                case MovesenseController.EventType.NOTIFICATION:
                    for (int i = 0; i < e.OriginalEventArgs.Count; i++)
                    {
                        var ne = (NotificationCallback.EventArgs)e.OriginalEventArgs[i];
                        //Debug.Log("Epic debug");
                        //Debug.Log("OnMovesenseControllerCallbackEvent, NOTIFICATION for " + ne.Serial + ", SubscriptionPath: " + ne.Subscriptionpath + ", Data: " + ne.Data);
                        string serial = ne.Serial;
                        var notificationFieldArgs = (NotificationCallback.FieldArgs)ne;
                        int index = 0;
                        foreach (SensorObject sensor in usedSensors)
                        {
                            if (sensor.SerialId == serial)
                            {
                                Vector3 argsVector = new Vector3((float)notificationFieldArgs.Values[index].x, (float)notificationFieldArgs.Values[index].y, (float)notificationFieldArgs.Values[index].z);
                                if (!data.ContainsKey(sensor))
                                {
                                    data.Add(sensor, new SensorData());
                                }
                                data[sensor].acceleration = argsVector;

                                //leftFoot.transform.Translate(argsVector);
                            }
                        }

                    }
                    break;
            }
        }
    }
}
