using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatePlayer : MonoBehaviour
{
    public float mouseSensitivity = 100f; // Sensitivity of the mouse

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Locks the cursor to the center
    }

    void Update()
    {
        // Get horizontal mouse movement
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        // Rotate the entire player left and right
        transform.Rotate(Vector3.up * mouseX);
    }
}