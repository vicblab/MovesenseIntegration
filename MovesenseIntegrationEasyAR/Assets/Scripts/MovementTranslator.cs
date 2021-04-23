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
using System.Collections;
using UnityEngine.SocialPlatforms;
using System.Collections.Generic;
using System.Linq;

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
    private float lba=-2; //leftBorderAcc
    private float rba=2; //rightBorderAcc
    float dt = 0;
    int count = 0;

    private Rigidbody rgd;

    Vector3 linAcc = Vector3.zero;

    const float beta = 0.1f;

    const float invSampleFreq = 1/52.0f;

    Quaternion q;


    public float minXangle=90;
    public float maxXangle=90;

    public float minYangle=90;
    public float maxYangle=90;

    public float minZangle=20;
    public float maxZangle=20;


    public float maxRightDistance = 1;
    public float maxLeftDistance = 1;
    public float maxForwardDistance = 1;
    public float maxBackwardDistance = 1;
    public float maxUpDistance = 1;
    public float maxDownDistance = 0.1f;



    private Vector3 initialEulerAngles = Vector3.zero;

    public Vector3 rotation = Vector3.zero;
    public Vector3 acceleration = Vector3.zero;
    private Vector3 uncorrectedAcceleration = Vector3.zero;

    private bool rotCorrected = false;

    Vector3 lastAcceleration = Vector3.zero;
    List<Vector3> lastAccelerationsEvenIfZero = new List<Vector3>();

    int changeCount = 0;

    bool lastAccWasNotConsidered = false;
    bool nowWeMustConsiderThisOne = true;

    void Awake()
    {
        ScanController.Event += OnScanControllerCallbackEvent;
        MovesenseController.Event += OnMovesenseControllerCallbackEvent;

        
        q[0] = 1.0f;
        q[1] = 0.0f;
        q[2] = 0.0f;
        q[3] = 0.0f;

    }


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

        

        upvector = new Vector3(0, 1, 0) * 9.8f;


        //------------------------------------------------------------------

        // rotation thresholds in degrees per second
        lb = -10 * DEG_TO_RAD; //leftBorder
        rb = 10 * DEG_TO_RAD; //rightBorder

        rgd = foot.gameObject.GetComponent<Rigidbody>();
        initialRotationR = rgd.rotation;
        initialPositionR = rgd.position;
        initialEulerAngles = rgd.transform.eulerAngles;

        text.text = "x: " + initialEulerAngles.x + " y: " + initialEulerAngles.y + " z: " + initialEulerAngles.z;

        q = initialRotation;
    }


    IEnumerator StartScanning()
    {
        if (ScanController.IsInitialized)
        {
            yield return 0;
            ScanController.StartScan();
        }
        else
        {
            yield return new WaitForSeconds(0.1F); // wait for ScanController to be initialized
            ScanController.StartScan();
        }
    }

    IEnumerator Connect(string macID)
    {
        if (MovesenseController.isInitialized)
        {
            yield return 0;
            MovesenseController.Connect(macID);
        }
        else
        {
            yield return new WaitForSeconds(0.1F); // wait for MovesenseController to be initialized
            MovesenseController.Connect(macID);
        }
    }

    private void FixedUpdate()
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
        rgd.useGravity = false;
        rgd.velocity = Vector3.zero;
        rgd.angularVelocity = Vector3.zero;

        q = initialRotation;
    }




    void OnScanControllerCallbackEvent(object sender, ScanController.EventArgs e) 
    {
		switch (e.Type) 
        {
			case ScanController.EventType.NEW_DEVICE:
                text.text = "Connecting to the sensor...";
                if(sensor.Mac == e.MacID)
                {
                    text.text += "Connecting to the sensor...";
				    StartCoroutine(Connect(e.MacID));
                }
            break;
		}
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
                            Vector3 rotVector1 = new Vector3((float)notificationFieldArgs.Values[0].x, (float)notificationFieldArgs.Values[0].z, (float)notificationFieldArgs.Values[0].y) * DEG_TO_RAD;
                            Vector3 rotVector2 = new Vector3((float)notificationFieldArgs.Values[1].x, (float)notificationFieldArgs.Values[1].z, (float)notificationFieldArgs.Values[1].y) * DEG_TO_RAD;

                            // ignoring noise-like disturbances (which are too subtle and don't reflect real rotation)
                            rotation = (-rotVector1 + -rotVector2) / 2;

                            if (rotation.x > lb && rotation.x < rb && rotation.y > lb && rotation.y < rb && rotation.z > lb && rotation.z < rb) rotation = Vector3.zero;



                            //rotation = (rotVector1 + rotVector2) / 2;
                            //rgd.angularVelocity = foot.transform.TransformDirection((rotVector1 + rotVector2) / 2);



                            //foot.transform.rotation = foot.transform.rotation * Quaternion.Euler(foot.transform.TransformDirection((rotVector1 + rotVector2) / 2) * Time.deltaTime);
                        }

                        //------------------------------------MOVEMENT------------------------------------------------------
                        if (string.Compare(notificationFieldArgs.Subscriptionpath, "Meas/Acc/") == 0)
                        {
                            float correction = 30;

                            Vector3 linVector1 = new Vector3((float)notificationFieldArgs.Values[0].x, (float)notificationFieldArgs.Values[0].z, (float)notificationFieldArgs.Values[0].y);
                            Vector3 linVector2 = new Vector3((float)notificationFieldArgs.Values[1].x, (float)notificationFieldArgs.Values[1].z, (float)notificationFieldArgs.Values[1].y);
                            linAcc += (-linVector1 + -linVector2) / 2;

                            uncorrectedAcceleration = (-linVector1 + -linVector2) / 2;

                            Vector3 correctedLinAcc = (-linVector1 + -linVector2) / 2 + foot.transform.GetChild(0).transform.InverseTransformDirection(upvector);

                            

                            if (correctedLinAcc.x > lba && correctedLinAcc.x < rba && correctedLinAcc.y > lba && correctedLinAcc.y < rba && correctedLinAcc.z > lba && correctedLinAcc.z < rba) correctedLinAcc = Vector3.zero;

                            text.text = "Acc: " + $"{uncorrectedAcceleration.x:F2},{uncorrectedAcceleration.y:F2},{uncorrectedAcceleration.z:F2}";
                            text.text += "\nVector: " + $"{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).x:F2},{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).y:F2},{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).z:F2}";
                            
                            acceleration = correctedLinAcc;

                            //upvector = Quaternion.AngleAxis(-rgd.rotation.z,Vector3.up) * upvector;
                            //text.text = "Acc: " + $"{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).x:F2},{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).y:F2},{foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).z:F2}"+ "\n AccReal: " + $"{((linVector1 + linVector2) / 2).x:F2},{((linVector1 + linVector2) / 2).y:F2},{((linVector1 + linVector2) / 2).z:F2}";
                        }


                         rgd.angularVelocity = foot.transform.TransformDirection((rotation));


                        //    limitRotation();

                        if (rotation == Vector3.zero)
                        {
                            rgd.angularVelocity = Vector3.zero;
                            Vector3 vectorOfRotation = Vector3.Cross(foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).normalized, uncorrectedAcceleration.normalized).normalized;
                            float angleOfNeededRotation = Vector3.Angle(-foot.transform.GetChild(0).transform.InverseTransformDirection(upvector).normalized, uncorrectedAcceleration.normalized);
                            correctRotation(angleOfNeededRotation, vectorOfRotation);
                        }
                        

                        

                        

                        //rgd.velocity += acceleration * SampleTime;

                        //if (lastAccelerationEvenIfZero != Vector3.zero && acceleration == Vector3.zero) changeCount++;

                        
                        

                        if(nowWeMustConsiderThisOne && acceleration != Vector3.zero)
                        {
                            nowWeMustConsiderThisOne = false;
                            lastAcceleration = acceleration;
                        }

                        // if there is a change of acceleration preceded by a movement, ignore the last acceleration since it is caused by the stopping force
                        if (Vector3.Dot(lastAcceleration, acceleration) < 0 )
                        {

                            if (acceleration != Vector3.zero)
                            {
                                rgd.velocity = Vector3.zero;
                                acceleration = lastAcceleration; //Vector3.zero
                                lastAccWasNotConsidered = true;
                            }

                        }
                        else
                        {
                            if (acceleration != Vector3.zero)
                            {
                                lastAcceleration = acceleration;
                                lastAccWasNotConsidered = false;
                            }
                        }
                        
                        
                        //rgd.MovePosition(rgd.position+acceleration*SampleTime*SampleTime/2);

                        if (acceleration == Vector3.zero) rgd.velocity = Vector3.zero;




                        if(lastAccelerationsEvenIfZero.Count > 9) lastAccelerationsEvenIfZero.RemoveAt(0);
                        lastAccelerationsEvenIfZero.Add(acceleration);

                       if(lastAccWasNotConsidered && lastAccelerationsEvenIfZero.Where(v => v != null && v == Vector3.zero).Count() > 8) nowWeMustConsiderThisOne = true;

                        //rgd.AddRelativeForce(acceleration);
                        rgd.velocity += acceleration * SampleTime;

                        limitPosition();

                        SampleTime = 0;

                    }
                }
                break;
        }


        

        float invSqrt(float x)
        {
            return 1 / Mathf.Sqrt(x);
        }



        Quaternion UpdateIMU(Vector3 guro, Vector3 acc)
        {
            float recipNorm;
            float s0, s1, s2, s3;
            float qDot1, qDot2, qDot3, qDot4;
            float _2q0, _2q1, _2q2, _2q3, _4q0, _4q1, _4q2, _8q1, _8q2, q0q0, q1q1, q2q2, q3q3;

            // Convert gyroscope degrees/sec to radians/sec
            guro.x *= Mathf.Deg2Rad;
            guro.y *= Mathf.Deg2Rad;
            guro.z *= Mathf.Deg2Rad;

            // Rate of change of quaternion from gyroscope
            qDot1 = 0.5f * (-q[1] * guro.x - q[2] * guro.y - q[3] * guro.z);
            qDot2 = 0.5f * (q[0] * guro.x + q[2] * guro.z - q[3] * guro.y);
            qDot3 = 0.5f * (q[0] * guro.y - q[1] * guro.z + q[3] * guro.x);
            qDot4 = 0.5f * (q[0] * guro.z + q[1] * guro.y - q[2] * guro.x);

            if (!((acc.x == 0.0f) && (acc.y == 0.0f) && (acc.z == 0.0f)))
            {

                // Normalise accelerometer measurement
                recipNorm = invSqrt(acc.x * acc.x + acc.y * acc.y + acc.z * acc.z);
                acc.x *= recipNorm;
                acc.y *= recipNorm;
                acc.z *= recipNorm;

                // Auxiliary variables to avoid repeated arithmetic
                _2q0 = 2.0f * q[0];
                _2q1 = 2.0f * q[1];
                _2q2 = 2.0f * q[2];
                _2q3 = 2.0f * q[3];
                _4q0 = 4.0f * q[0];
                _4q1 = 4.0f * q[1];
                _4q2 = 4.0f * q[2];
                _8q1 = 8.0f * q[1];
                _8q2 = 8.0f * q[2];
                q0q0 = q[0] * q[0];
                q1q1 = q[1] * q[1];
                q2q2 = q[2] * q[2];
                q3q3 = q[3] * q[3];

                // Gradient decent algorithm corrective step
                s0 = _4q0 * q2q2 + _2q2 * acc.x + _4q0 * q1q1 - _2q1 * acc.y;
                s1 = _4q1 * q3q3 - _2q3 * acc.x + 4.0f * q0q0 * q[1] - _2q0 * acc.y - _4q1 + _8q1 * q1q1 + _8q1 * q2q2 + _4q1 * acc.z;
                s2 = 4.0f * q0q0 * q[2] + _2q0 * acc.x + _4q2 * q3q3 - _2q3 * acc.y - _4q2 + _8q2 * q1q1 + _8q2 * q2q2 + _4q2 * acc.z;
                s3 = 4.0f * q1q1 * q[3] - _2q1 * acc.x + 4.0f * q2q2 * q[3] - _2q2 * acc.y;
                recipNorm = invSqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
                s0 *= recipNorm;
                s1 *= recipNorm;
                s2 *= recipNorm;
                s3 *= recipNorm;

                // Apply feedback step
                qDot1 -= beta * s0;
                qDot2 -= beta * s1;
                qDot3 -= beta * s2;
                qDot4 -= beta * s3;
            }

            // Integrate rate of change of quaternion to yield quaternion
            q[0] += qDot1 * invSampleFreq;
            q[1] += qDot2 * invSampleFreq;
            q[2] += qDot3 * invSampleFreq;
            q[3] += qDot4 * invSampleFreq;

            // Normalise quaternion
            recipNorm = invSqrt(q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]);
            q[0] *= recipNorm;
            q[1] *= recipNorm;
            q[2] *= recipNorm;
            q[3] *= recipNorm;
            //anglesComputed = false;
            return q;
        }
    }

    bool limitRotation()
    {

        Vector3 currentEulerAngles = rgd.transform.eulerAngles;

        float xAngle = currentEulerAngles.x;
        float yAngle = currentEulerAngles.y;
        float zAngle = currentEulerAngles.z;

        if ((xAngle > initialEulerAngles.x + maxXangle)
            || (xAngle < initialEulerAngles.x - minXangle)
            || (yAngle > initialEulerAngles.y + maxYangle)
            || (yAngle < initialEulerAngles.y - minYangle)
            || (zAngle > initialEulerAngles.z + maxZangle)
            || (zAngle < initialEulerAngles.z - minZangle)) return false;

        return true;

            /*
            if (xAngle > initialEulerAngles.x + maxXangle)
            {
                xAngle -= 0.0001f;
                rgd.angularVelocity = Vector3.zero;
            }
            else if (xAngle < initialEulerAngles.x - maxXangle)
            {
                xAngle += 0.0001f;
                rgd.angularVelocity = Vector3.zero;
            }

            if (yAngle > initialEulerAngles.y + maxYangle)
            {
                yAngle -= 0.001f;
                rgd.angularVelocity = Vector3.zero;
            }
            else if (yAngle < initialEulerAngles.y - maxYangle)
            {
                yAngle +=  0.001f;
                rgd.angularVelocity = Vector3.zero;
            }

            if (zAngle > initialEulerAngles.z + maxZangle)
            {
                zAngle -= 0.001f;
                rgd.angularVelocity = Vector3.zero;
            }
            else if (zAngle < initialEulerAngles.z - maxZangle)
            {
                zAngle += + 0.001f;
                rgd.angularVelocity = Vector3.zero;
            }
            */
            Vector3 correctedEulerAngles = new Vector3(xAngle, yAngle, zAngle);

            //text.text = "x: " + currentEulerAngles.x + " y: " + currentEulerAngles.y + " z: " + currentEulerAngles.z;

            if (correctedEulerAngles!= currentEulerAngles) rgd.transform.localEulerAngles = correctedEulerAngles;

        }


    void correctRotation( float angleOfNeededRotation, Vector3 vectorOfRotation)
    {
        if(uncorrectedAcceleration.magnitude > 9.7 && Mathf.Abs(angleOfNeededRotation) > 1 )
        {
           
            //Now we rotate the body such that the uncorrectedAcceleration vector is pointing down

            //the gravity vector direction is uncorrectedAcceleration.normalized
            // foot.transform.GetChild(0).transform.InverseTransformDirection(upvector) is our local vector of antigravity
            
            

            rgd.transform.Rotate(vectorOfRotation,angleOfNeededRotation);

            button.GetComponentInChildren<TMP_Text>().text = angleOfNeededRotation.ToString();

            rotCorrected = true;

        }
    }

    void limitPosition()
    {
        Vector3 displacement = rgd.position - initialPositionR;

        if(displacement.x > maxRightDistance || displacement.x < -maxLeftDistance || displacement.y > maxUpDistance || displacement.y < -maxDownDistance || displacement.z > maxForwardDistance || displacement.z < -maxBackwardDistance)
        {
            float xVariation=0;
            float yVariation=0;
            float zVariation=0;

            rgd.velocity = Vector3.zero;
            if(displacement.x > 0)
            {
                xVariation = -0.001f;
            }
            else
            {
                xVariation = 0.001f;
            }

            if (displacement.y > 0)
            {
                yVariation = -0.001f;
            }
            else
            {
                yVariation = 0.001f;
            }

            if (displacement.z > 0)
            {
                zVariation = -0.001f;
            }
            else
            {
                zVariation = 0.001f;
            }

            // move a little bit to the inside of the mobility space
            rgd.position = new Vector3(rgd.position.x + xVariation, rgd.position.y + yVariation, rgd.position.z + zVariation);

        }
    }



}