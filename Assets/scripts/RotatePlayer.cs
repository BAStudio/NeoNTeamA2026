using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatePlayer : MonoBehaviour
{
    Vector3 CameraRotation;
    public Vector3 CameraDirection;
    void Start()
    {
    }

    void Update()
    {
       
    }

    public void RotatePlayerToCamera(Transform cameraTransform)
    {
     GetComponentInChildren<Camera>().transform.rotation = cameraTransform.rotation;
        CameraRotation = GetComponentInChildren<Camera>().transform.rotation.eulerAngles;
        CameraDirection = cameraTransform.forward;

    }
}