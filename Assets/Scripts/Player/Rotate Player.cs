using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatePlayer : MonoBehaviour //D'onnell`
{
    public float _mouseSensitivity = 100f; 

    private bool _canRotate = true; 

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; 
    }

    void Update()
    {
        if (!_canRotate) return; 

        float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.deltaTime;

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