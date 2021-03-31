
//-----------------------------------------------------------------------------------------------------------
// Script for controlling the 3D foot movement based on the info recieved from the sensors. It will be attached to "Sensor 1 info" and "Sensor 2 info"
// 
// Victor Blanco Bataller
// 2021/03/29
// Turku University of Applied Sciences
//----------------------------------------------------------------------------------------------------------

using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class MovementTranslator : MonoBehaviour
{

    [SerializeField] private Button button; // the button for connecting to the sensor (left foot or right foot)
    [SerializeField] private Button reset; // button for resetting the foot's position and rotation
    [SerializeField] private TMP_Text text; // text to display sensor's information
    [SerializeField] private SensorObject sensor; // the scriptable object sensor
    [SerializeField] private GameObject foot; // the sensor which represents the foot

    //---------------Under construction-----------------------

    /*private bool isFirstTime = true;
    private float time = 0.0f;*/
    
        

    //private AccelerometerUtil accelerometerUtil; //exported from AntiGravity.cs

    //private Vector3 upvector;

    //-----------------------------------------------------------

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float linealError = 0;


    private void Start()
    {
        //We add a listener to the button so when you press it the device connects to the sensor
        button.onClick.AddListener(() =>
        {
            text.text = "Connecting to the sensor...";

            MovesenseController.Connect(sensor.Mac);
        });
        reset.onClick.AddListener(() => ResetPosition());
        MovesenseController.Event += OnMovesenseControllerCallbackEvent;

        //Physics.gravity = new Vector3(0, 0, 9.8f)/20;
        foot.gameObject.GetComponent<Rigidbody>().useGravity = false;

        //upvector = leftFoot.transform.parent.transform.up * -9.8f/20;
        initialRotation = foot.transform.rotation;
        initialPosition = foot.transform.localPosition;
    }


    //---- Reset the position, rotation and velocity of the foot-------
    private void ResetPosition()
    {
        foot.transform.localPosition = initialPosition;
        foot.transform.rotation = initialRotation;
        foot.gameObject.GetComponent<Rigidbody>().useGravity = false;
        foot.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }



    // This function gets called each time some change on the sensors' connection happens
    void OnMovesenseControllerCallbackEvent(object sender, MovesenseController.EventArgs e)
    {
        //Debug.Log("OnMovesenseControllerCallbackEvent, Type: " + e.Type + ", invoked by: " + e.InvokeMethod);
        switch (e.Type)
        {
            //in the case that the sensor is sending data

            case MovesenseController.EventType.NOTIFICATION:

                for (int i = 0; i < e.OriginalEventArgs.Count; i++)
                {
                    var ne = (NotificationCallback.EventArgs)e.OriginalEventArgs[i];
                    
                    Debug.Log("NOTIFICATION for " + ne.Serial);
                    string serial = ne.Serial;
                    var notificationFieldArgs = (NotificationCallback.FieldArgs)ne;
                    

                    // if the Id of the sensor sending the data is the same as the id of our sensor (scriptable object)
                    // we get the acceleration and angular velocity vectors, and do stuff with them

                    if (sensor.SerialId == serial)
                    {


                        //time += Time.deltaTime;

                        // here we extract linear acceleration (linVector) and angular velocity (rotVector)

                        Vector3 linVector = new Vector3((float)notificationFieldArgs.Values[0].x, (float)notificationFieldArgs.Values[0].y, (float)notificationFieldArgs.Values[0].z);
                        Vector3 rotVector = new Vector3((float)notificationFieldArgs.Values[1].x/ 57.3f, (float)notificationFieldArgs.Values[1].y / 57.3f, (float)notificationFieldArgs.Values[1].z / 57.3f); // angular velocity
                        
                                                                                                            //*0.75

                        //.......................Under construction.............................

                        /*if (isFirstTime)
                        {
                            isFirstTime = false;
                            accelerometerUtil = new AccelerometerUtil(linVector);
                        }

                        linVector = accelerometerUtil.LowPassFiltered(accelerometerUtil.LowPassFiltered(linVector));
                        linVector = accelerometerUtil.LowPassFiltered(linVector);
                        linVector = accelerometerUtil.LowPassFiltered(linVector);*/

                        //...........................................................................


                        

                        //------------------ROTATION----------------------------------------

                        rotVector.y += 0.01f; // correction
                        if (rotVector.magnitude < 0.15) rotVector = Vector3.zero;

                        /*
                        if (rotVector.x < 0.15 && rotVector.x > -0.15) rotVector.x = 0;
                        if (rotVector.y < 0.15 && rotVector.y > -0.15) rotVector.y = 0;
                        if (rotVector.z < 0.15 && rotVector.z > -0.15) rotVector.z = 0;*/




                        foot.gameObject.GetComponent<Rigidbody>().angularVelocity = foot.transform.TransformDirection(rotVector);



                        //----------POSITION-------------------------

                        // threshold for linear acceleration

                        if (linVector.magnitude < 0.2) linVector = Vector3.zero;
                        /*
                        if (linVector.x < 0.1 && linVector.x > -0.1) linVector.x = 0;
                        if (linVector.y < 0.1 && linVector.y > -0.1) linVector.y = 0;
                        if (linVector.z < 0.1 && linVector.z > -0.1) linVector.z = 0;*/

                        

                        //linVector+= 9.82f* foot.transform.up;


                        //foot.gameObject.GetComponent<Rigidbody>().AddRelativeForce(linVector*10, ForceMode.Acceleration);

                        
                        // if the sensor is still
                        if (linVector == Vector3.zero)
                        {
                            foot.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                            foot.gameObject.GetComponent<Rigidbody>().useGravity = false;
                            //time = 0;

                        }


                        //foot.transform.position += correctedAcc * ((Time.deltaTime * Time.deltaTime / 2) + linealError);



                        //display the data as text

                        text.text = "Acc: " + $"{linVector.x:F2},{linVector.y:F2},{linVector.z:F2}" + "\n Angular vel: " + $"{rotVector.x:F2},{rotVector.y:F2},{rotVector.z:F2}";


                    }
                }
                break;
        }
    }
}
