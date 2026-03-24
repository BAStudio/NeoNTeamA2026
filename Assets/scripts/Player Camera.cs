using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float mouseSensitivity = 100f; // Sensitivity of the mouse
    public float verticalLookLimit = 90f; // Maximum up/down look angle

    private float xRotation = 0f; // Tracks up/down rotation

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Locks the cursor to the center
    }

    void Update()
    {
        // Get mouse input (only vertical movement)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Adjust vertical rotation (pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit); // Limit up/down rotation

        // Apply rotation to the camera (up/down only)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}