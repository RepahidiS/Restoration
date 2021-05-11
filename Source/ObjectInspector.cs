using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInspector : MonoBehaviour
{
    public float rotationSpeed = 10.0f;

    void Start()
    {
        enabled = false;
    }

    void FixedUpdate()
    {
        Vector3 currentRotation = transform.rotation.eulerAngles;
        currentRotation.y += rotationSpeed;
        transform.rotation = Quaternion.Euler(currentRotation);
    }
}