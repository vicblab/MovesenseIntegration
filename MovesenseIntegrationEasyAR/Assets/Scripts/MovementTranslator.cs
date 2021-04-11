
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


    private float SampleTime = 0.0f;

    //---------------Under construction-----------------------

    /*private bool isFirstTime = true;
    */



    //private AccelerometerUtil accelerometerUtil; //exported from AntiGravity.cs

    private Vector3 upvector;

    //-----------------------------------------------------------

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialPositionR;
    private Quaternion initialRotationR;
    private float linealError = 0;


    //---------------------------------------------------------------

    private float DEG_TO_RAD = Mathf.PI / 180;
    private float lb; //leftBorder
    private float rb; //rightBorder
    float dt = 0;
    int count = 0;

    private Rigidbody rgd;
    
    Vector3 linAcc = Vector3.zero;

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

        

        upvector = new Vector3(0,1,0) * 10;


        //------------------------------------------------------------------

        // rotation thresholds in degrees per second
        lb = -3 * DEG_TO_RAD; //leftBorder
        rb = 3 * DEG_TO_RAD; //rightBorder

        rgd = foot.gameObject.GetComponent<Rigidbody>();
        initialRotationR = rgd.rotation;
        initialPositionR = rgd.position;


    }

    private void Update()
    {
        SampleTime += Time.deltaTime;
    }

    //---- Reset the position, rotation and velocity of the foot-------
    private void ResetPosition()
    {
        foot.transform.localPosition = initialPosition;
        foot.transform.rotation = initialRotation;
        rgd.rotation = initialRotationR;
        rgd.position = initialPositionR;
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


                        //---------------ROTATION----------------------------------
                        if (string.Compare(notificationFieldArgs.Subscriptionpath, "Meas/Gyro/") == 0)
                        {
                            // obtaining both rotation vectors
                            Vector3 rotVector1 = new Vector3((float)notificationFieldArgs.Values[0].x, (float)notificationFieldArgs.Values[0].y, (float)notificationFieldArgs.Values[0].z) * DEG_TO_RAD;
                            Vector3 rotVector2 = new Vector3((float)notificationFieldArgs.Values[1].x, (float)notificationFieldArgs.Values[1].y, (float)notificationFieldArgs.Values[1].z) * DEG_TO_RAD;

                            // ignoring noise-like disturbances (which are too subtle and don't reflect real rotation)
                            if (rotVector1.x > lb && rotVector1.x < rb) rotVector1.x = 0;
                            if (rotVector1.y > lb && rotVector1.y < rb) rotVector1.y = 0;
                            if (rotVector1.z > lb && rotVector1.z < rb) rotVector1.z = 0;
                            if (rotVector2.x > lb && rotVector2.x < rb) rotVector2.x = 0;
                            if (rotVector2.y > lb && rotVector2.y < rb) rotVector2.y = 0;
                            if (rotVector2.z > lb && rotVector2.z < rb) rotVector2.z = 0;


                            rgd.angularVelocity = foot.transform.TransformDirection((rotVector1 + rotVector2) / 2);

                            foot.transform.rotation = foot.transform.rotation * Quaternion.Euler(foot.transform.TransformDirection((rotVector1 + rotVector2) / 2) * Time.deltaTime);
                        }

                        //------------------------------------MOVEMENT------------------------------------------------------
                        if (string.Compare(notificationFieldArgs.Subscriptionpath, "Meas/Acc/") == 0)
                        {
                            float correction = 30;

                            Vector3 linVector1 = new Vector3((float)notificationFieldArgs.Values[0].x, (float)notificationFieldArgs.Values[0].y, (float)notificationFieldArgs.Values[0].z);
                            Vector3 linVector2 = new Vector3((float)notificationFieldArgs.Values[1].x, (float)notificationFieldArgs.Values[1].y, (float)notificationFieldArgs.Values[1].z);
                            linAcc += (linVector1 + linVector2) / 2;

                            Vector3 correctedLinAcc = (linVector1 + linVector2) / 2 + foot.transform.GetChild(0).transform.InverseTransformDirection(upvector);

                            text.text = "Acc: " + $"{correctedLinAcc.x:F2},{correctedLinAcc.y:F2},{correctedLinAcc.z:F2}";

                            //upvector = Quaternion.AngleAxis(-rgd.rotation.z,Vector3.up) * upvector;
                            //text.text = "Acc: " + $"{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).x:F2},{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).y:F2},{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).z:F2}"+ "\n AccReal: " + $"{((linVector1 + linVector2) / 2).x:F2},{((linVector1 + linVector2) / 2).y:F2},{((linVector1 + linVector2) / 2).z:F2}";
                        }
                        //time += Time.deltaTime;

                        // here we extract linear acceleration (linVector) and angular velocity (rotVector)

                        // Vector3 linVector = new Vector3((float)notificationFieldArgs.Values[0].x, (float)notificationFieldArgs.Values[0].y, (float)notificationFieldArgs.Values[0].z);
                        //Vector3 rotVector = new Vector3((float)notificationFieldArgs.Values[1].x/ 57.3f, (float)notificationFieldArgs.Values[1].y / 57.3f, (float)notificationFieldArgs.Values[1].z / 57.3f); // angular velocity
                        //Vector3 rotVector = new Vector3((float)notificationFieldArgs.Values[0].x/ 57.3f, (float)notificationFieldArgs.Values[0].y / 57.3f, (float)notificationFieldArgs.Values[0].z / 57.3f); // angular velocity

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

                        // Get rid of gravity:

                        //linVector += transform.InverseTransformDirection(upvector);//(Vector3.up * -9.8f);



                        //------------------ROTATION----------------------------------------

                        //rotVector.y += 0.01f; // correction
                        //if (rotVector.magnitude < 0.15) rotVector = Vector3.zero;

                        /*
                        if (rotVector.x < 0.15 && rotVector.x > -0.15) rotVector.x = 0;
                        if (rotVector.y < 0.15 && rotVector.y > -0.15) rotVector.y = 0;
                        if (rotVector.z < 0.15 && rotVector.z > -0.15) rotVector.z = 0;*/




                        //foot.gameObject.GetComponent<Rigidbody>().angularVelocity = foot.transform.TransformDirection(rotVector);



                        //----------POSITION-------------------------

                        // threshold for linear acceleration

                        //if (linVector.magnitude < 0.2) linVector = Vector3.zero;
                        /*
                        if (linVector.x < 0.1 && linVector.x > -0.1) linVector.x = 0;
                        if (linVector.y < 0.1 && linVector.y > -0.1) linVector.y = 0;
                        if (linVector.z < 0.1 && linVector.z > -0.1) linVector.z = 0;*/



                        //linVector+= 9.82f* foot.transform.up;


                        //foot.gameObject.GetComponent<Rigidbody>().AddRelativeForce(linVector*10, ForceMode.Acceleration);


                        // if the sensor is still
                        //if (linVector == Vector3.zero)
                        //{
                        //   foot.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                        //   foot.gameObject.GetComponent<Rigidbody>().useGravity = false;
                        //time = 0;

                        //}


                        //foot.transform.position += linVector * ((SampleTime * SampleTime / 2) + linealError);





                        //display the data as text

                        // text.text = "Acc: " + $"{linVector.x:F2},{linVector.y:F2},{linVector.z:F2}" + "\n Angular vel: " + $"{rotVector.x:F2},{rotVector.y:F2},{rotVector.z:F2}";


                    }
                }
                break;
        }
    }
}
