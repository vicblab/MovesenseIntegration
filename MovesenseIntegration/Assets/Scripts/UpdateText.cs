using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AHRS;
public class UpdateText : MonoBehaviour
{

    [SerializeField] private Button button;
    [SerializeField] private Button reset;
    [SerializeField] private TMP_Text text;
    [SerializeField] private SensorObject sensor;
    [SerializeField] private GameObject leftFoot;

    static MadgwickAHRS ahrs = new MadgwickAHRS(1f / 256f, 0.1f);

    private Vector3 upvector;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private void Start()
    {
        button.onClick.AddListener(() => MovesenseController.Connect(sensor.Mac));
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
                    //Debug.Log("Epic debug");
                    //Debug.Log("OnMovesenseControllerCallbackEvent, NOTIFICATION for " + ne.Serial + ", SubscriptionPath: " + ne.Subscriptionpath + ", Data: " + ne.Data);
                    Debug.Log("NOTIFICATION for " + ne.Serial);
                    string serial = ne.Serial;
                    var notificationFieldArgs = (NotificationCallback.FieldArgs)ne;
                    int index = 0;
                    if (sensor.SerialId == serial)
                    {
                        

                        Vector3 linVector = new Vector3((float)notificationFieldArgs.Values[index].x/20, (float)notificationFieldArgs.Values[index].y/20, (float)notificationFieldArgs.Values[index].z/20);
                        Vector3 rotVector = new Vector3((float)notificationFieldArgs.Values[1].x/ 57.3f, 0.75f*(float)notificationFieldArgs.Values[1].y / 57.3f, (float)notificationFieldArgs.Values[1].z / 57.3f); // angular velocity
                        //argsVector=leftFoot.transform.TransformVector(argsVector);
                        //linVector += leftFoot.transform.InverseTransformDirection(leftFoot.transform.parent.transform.up )*-9.8f;//delete from here
                        //linVector += leftFoot.transform.parent.up * -9.8f;
                        text.text = $"{linVector.x:F2},{linVector.y:F2},{linVector.z:F2}"+"|||||"+ $"{rotVector.x:F2},{rotVector.y:F2},{rotVector.z:F2}";

                        if (linVector.x < 0.3 && linVector.x > -0.3) linVector.x = 0;
                        if (linVector.y < 0.3 && linVector.y > -0.3) linVector.y = 0;
                        if (linVector.z < 0.3 && linVector.z > -0.3) linVector.z = 0;

                        //leftFoot.gameObject.GetComponent<Rigidbody>().useGravity = true;

                        
                        //leftFoot.transform.Translate(linVector);
                        //leftFoot.gameObject.GetComponent<Rigidbody>().velocity += linVector * Time.deltaTime;


                        leftFoot.gameObject.GetComponent<Rigidbody>().AddRelativeForce(linVector/10);

                        if (linVector == Vector3.zero)
                        {
                            leftFoot.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                            leftFoot.gameObject.GetComponent<Rigidbody>().useGravity = false;

                        }

                        rotVector.y += 0.01f; // correction
                        if (rotVector.x < 0.15 && rotVector.x > -0.15) rotVector.x = 0;
                        if (rotVector.y < 0.15 && rotVector.y > -0.15) rotVector.y = 0;
                        if (rotVector.z < 0.15 && rotVector.z > -0.15) rotVector.z = 0;

                        
                        leftFoot.gameObject.GetComponent<Rigidbody>().angularVelocity= leftFoot.transform.TransformDirection(rotVector);


                        /*--------------------------------------------------
                        ahrs.Update(rotVector.x, rotVector.y, rotVector.z, linVector.x, linVector.y, linVector.z);
                        var _q = ahrs.Quaternion;

                        Quaternion q = new Quaternion(_q[0], _q[1], _q[2], _q[3]);
                        var qEualr = q.eulerAngles;

                        leftFoot.transform.rotation= Quaternion.Euler(qEualr.z, -qEualr.y, qEualr.x);*/

                    }
                }
                break;
        }
    }
}
