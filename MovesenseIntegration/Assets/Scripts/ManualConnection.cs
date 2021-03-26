using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualConnection : MonoBehaviour
{
    [SerializeField] private string connectToMac;

    public void Connect()
    {
        MovesenseController.Connect(connectToMac);
    }
}
