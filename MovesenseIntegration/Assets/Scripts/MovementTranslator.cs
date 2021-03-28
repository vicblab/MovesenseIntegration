using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class MovementTranslator : MonoBehaviour
{

    [SerializeField] private Button button;
    [SerializeField] private Button reset;
    [SerializeField] private TMP_Text text;
    [SerializeField] private SensorObject sensor;
    [SerializeField] private GameObject leftFoot;

    /*private bool isFirstTime = true;
    private float time = 0.0f;*/
    
        

    //private AccelerometerUtil accelerometerUtil; //exported from AntiGravity.cs

    private Vector3 upvector;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private void Start()
    {
        button.onClick.AddListener(() =>
        {
            text.text = "Connecting to the sensor...";

            MovesenseController.Connect(sensor.Mac);
        });
        reset.onClick.AddListener(() => ResetPosition());
        MovesenseController.Event += OnMovesenseControllerCallbackEvent;

        Physics.gravity = new Vector3(0, 0, 9.8f)/20;
        leftFoot.gameObject.GetComponent<Rigidbody>().useGravity = false;

        upvector = leftFoot.transform.parent.transform.up * -9.8f/20;
        initialRotation = leftFoot.transform.rotation;
        initialPosition = leftFoot.transform.localPosition;
    }


    private void ResetPosition()
    {
        leftFoot.transform.localPosition = initialPosition;
        leftFoot.transform.rotation = initialRotation;
        leftFoot.gameObject.GetComponent<Rigidbody>().useGravity = false;
        leftFoot.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
    void OnMovesenseControllerCallbackEvent(object sender, MovesenseController.EventArgs e)
    {
        //Debug.Log("OnMovesenseControllerCallbackEvent, Type: " + e.Type + ", invoked by: " + e.InvokeMethod);
        switch (e.Type)
        {
            case MovesenseController.EventType.NOTIFICATION:
                for (int i = 0; i < e.OriginalEventArgs.Count; i++)
                {
                    var ne = (NotificationCallback.EventArgs)e.OriginalEventArgs[i];
                    
                    Debug.Log("NOTIFICATION for " + ne.Serial);
                    string serial = ne.Serial;
                    var notificationFieldArgs = (NotificationCallback.FieldArgs)ne;
                    int index = 0;
                    if (sensor.SerialId == serial)
                    {


                        //time += Time.deltaTime;

                        Vector3 linVector = new Vector3((float)notificationFieldArgs.Values[index].x, (float)notificationFieldArgs.Values[index].y, (float)notificationFieldArgs.Values[index].z);
                        Vector3 rotVector = new Vector3((float)notificationFieldArgs.Values[1].x/ 57.3f, 0.75f*(float)notificationFieldArgs.Values[1].y / 57.3f, (float)notificationFieldArgs.Values[1].z / 57.3f); // angular velocity
                        


                        /*if (isFirstTime)
                        {
                            isFirstTime = false;
                            accelerometerUtil = new AccelerometerUtil(linVector);
                        }

                        linVector = accelerometerUtil.LowPassFiltered(accelerometerUtil.LowPassFiltered(linVector));
                        linVector = accelerometerUtil.LowPassFiltered(linVector);
                        linVector = accelerometerUtil.LowPassFiltered(linVector);*/

                        text.text = "Acc: "+$"{linVector.x:F2},{linVector.y:F2},{linVector.z:F2}"+"\n Angular vel: "+ $"{rotVector.x:F2},{rotVector.y:F2},{rotVector.z:F2}";

                        if (linVector.x < 0.1 && linVector.x > -0.1) linVector.x = 0;
                        if (linVector.y < 0.1 && linVector.y > -0.1) linVector.y = 0;
                        if (linVector.z < 0.1 && linVector.z > -0.1) linVector.z = 0;

                        


                        //leftFoot.gameObject.GetComponent<Rigidbody>().AddRelativeForce(linVector*10, ForceMode.Acceleration);

                        

                        if (linVector == Vector3.zero)
                        {
                            leftFoot.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                            leftFoot.gameObject.GetComponent<Rigidbody>().useGravity = false;
                            //time = 0;

                        }

                        rotVector.y += 0.01f; // correction
                        if (rotVector.x < 0.15 && rotVector.x > -0.15) rotVector.x = 0;
                        if (rotVector.y < 0.15 && rotVector.y > -0.15) rotVector.y = 0;
                        if (rotVector.z < 0.15 && rotVector.z > -0.15) rotVector.z = 0;

                        
                        leftFoot.gameObject.GetComponent<Rigidbody>().angularVelocity= leftFoot.transform.TransformDirection(rotVector);


                     

                    }
                }
                break;
        }
    }
}
