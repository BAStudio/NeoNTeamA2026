using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate_Player: MonoBehaviour // yussien
{
    public float mouseSensitivity = 100f;

    private float currentRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        currentRotation += mouseX;

        transform.rotation = Quaternion.Euler(0f, currentRotation, 0f);
    }
}