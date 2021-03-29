using UnityEngine;

public class AccelerometerUtil
{
    public float alpha = 0.2f;
    public float[] gravity = new float[3];

    public AccelerometerUtil(Vector3 currentAcc)
    {
        Debug.Log("AccelerometerUtil Init");
        
        gravity[0] = currentAcc.x;
        gravity[1] = currentAcc.y;
        gravity[2] = currentAcc.z;
    }

    public Vector3 LowPassFiltered(Vector3 currentAcc)
    {
        /*
         https://developer.android.com/reference/android/hardware/SensorEvent.html

          gravity[0] = alpha * gravity[0] + (1 - alpha) * event.values[0];
          gravity[1] = alpha * gravity[1] + (1 - alpha) * event.values[1];
          gravity[2] = alpha * gravity[2] + (1 - alpha) * event.values[2];

          linear_acceleration[0] = event.values[0] - gravity[0];
          linear_acceleration[1] = event.values[1] - gravity[1];
          linear_acceleration[2] = event.values[2] - gravity[2];
         */


        
        gravity[0] = alpha * gravity[0] + (1 - alpha) * currentAcc.x;
        gravity[1] = alpha * gravity[1] + (1 - alpha) * currentAcc.y;
        gravity[2] = alpha * gravity[2] + (1 - alpha) * currentAcc.z;

        Vector3 linearAcceleration =
            new Vector3(currentAcc.x - gravity[0],
                currentAcc.y - gravity[1],
                currentAcc.z - gravity[2]);

        return linearAcceleration;
    }
}