using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatePlayer : MonoBehaviour
{
    public float mouseSensitivity = 100f; 

    private bool _canRotate = true; 

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; 
    }

    void Update()
    {
        if (!_canRotate) return; 

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);
    }

    public void DisableRotation()
    {
        _canRotate = false;
    }

    public void EnableRotation()
    {
        _canRotate = true;
    }
}