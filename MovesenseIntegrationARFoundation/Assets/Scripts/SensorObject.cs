using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sensor", menuName = "ScriptableObjects/SensorObject", order = 1)]
public class SensorObject : ScriptableObject
{
    [SerializeField] private string sensorName;
    [SerializeField] private string mac;
    [SerializeField] private string serialId;

    public string SensorName { get => sensorName; }
    public string Mac { get => mac; }
    public string SerialId { get => serialId; }
}

